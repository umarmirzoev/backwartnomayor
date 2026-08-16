using System.Net;
using System.Text.Json;
using Application.Common.Models;
using Domain.Exceptions;

namespace WebApi.Middleware;

/// <summary>
/// Является последней защитной границей WebAPI для необработанных исключений.
/// Ожидаемые ошибки некорректного HTTP-ввода преобразуются в Response с кодом 400,
/// а внутренние сбои журналируются с correlation-id и возвращают клиенту только обобщённый код 500.
/// </summary>
public sealed class GlobalExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    /// <summary>Инициализирует middleware следующим звеном конвейера и структурированным журналом.</summary>
    /// <param name="next">Следующий компонент HTTP-конвейера.</param>
    /// <param name="logger">Журнал неожиданных ошибок.</param>
    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(logger);
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Выполняет следующий компонент, сохраняет семантику отмены клиентом и формирует единый JSON-ответ при ошибке.
    /// </summary>
    /// <param name="context">Текущий HTTP-контекст.</param>
    /// <returns>Задача полного выполнения запроса.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogDebug("HTTP-запрос {TraceIdentifier} отменён клиентом.", context.TraceIdentifier);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            await WriteErrorAsync(context, exception);
        }
    }

    /// <summary>Классифицирует исключение, журналирует его с безопасным контекстом и записывает Response в JSON.</summary>
    /// <param name="context">HTTP-контекст до начала ответа.</param>
    /// <param name="exception">Перехваченное исключение.</param>
    /// <returns>Задача сериализации ответа.</returns>
    private async Task WriteErrorAsync(HttpContext context, Exception exception)
    {
        var isBadRequest = exception is DomainException
            or ArgumentException
            or JsonException
            or BadHttpRequestException;
        var statusCode = isBadRequest
            ? HttpStatusCode.BadRequest
            : HttpStatusCode.InternalServerError;
        var error = isBadRequest
            ? "Запрос содержит некорректные данные или нарушает бизнес-правило."
            : "Произошла внутренняя ошибка сервера. Повторите запрос позднее.";

        if (isBadRequest)
        {
            _logger.LogWarning(
                exception,
                "Отклонён некорректный HTTP-запрос {TraceIdentifier} к {Path}.",
                context.TraceIdentifier,
                context.Request.Path);
        }
        else
        {
            _logger.LogError(
                exception,
                "Необработанная ошибка HTTP-запроса {TraceIdentifier} к {Path}.",
                context.TraceIdentifier,
                context.Request.Path);
        }

        context.Response.Clear();
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        var response = Response<object>.Fail(error, statusCode);
        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            response,
            SerializerOptions,
            context.RequestAborted);
    }
}
