using System.Net;
using System.Text.Json;
using Application.Common.Models;

namespace WebApi.Security;

/// <summary>Формирует единый Response для отказов JWT до достижения MVC-контроллера.</summary>
public static class AuthenticationResponseWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Записывает русскоязычный отказ аутентификации или авторизации без деталей проверки токена.</summary>
    /// <param name="context">Текущий HTTP-контекст.</param>
    /// <param name="statusCode">Код 401 или 403.</param>
    /// <param name="error">Безопасное сообщение клиенту.</param>
    /// <returns>Задача сериализации JSON.</returns>
    public static async Task WriteAsync(
        HttpContext context,
        HttpStatusCode statusCode,
        string error)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            Response<object>.Fail(error, statusCode),
            SerializerOptions,
            context.RequestAborted);
    }
}
