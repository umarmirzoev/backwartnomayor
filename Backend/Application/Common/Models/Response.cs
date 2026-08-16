using System.Net;

namespace Application.Common.Models;

/// <summary>
/// Представляет единый неизменяемый результат выполнения Application-сценария.
/// Контракт отделяет ожидаемые ошибки пользователя и бизнес-правил от исключений инфраструктуры,
/// позволяя WebAPI единообразно преобразовывать результат в HTTP-ответ без раскрытия внутренних деталей.
/// </summary>
/// <typeparam name="T">Тип полезных данных успешного результата.</typeparam>
public sealed class Response<T>
{
    private Response(
        bool isSuccess,
        HttpStatusCode statusCode,
        T? data,
        string message,
        IReadOnlyList<string> errors)
    {
        IsSuccess = isSuccess;
        StatusCode = statusCode;
        Data = data;
        Message = message;
        Errors = errors;
    }

    /// <summary>Получает признак успешного завершения сценария.</summary>
    public bool IsSuccess { get; }

    /// <summary>Получает рекомендуемый HTTP-код для транспортного слоя.</summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>Получает полезные данные успешного ответа или значение по умолчанию при ошибке.</summary>
    public T? Data { get; }

    /// <summary>Получает безопасное человекочитаемое описание результата.</summary>
    public string Message { get; }

    /// <summary>Получает неизменяемый список ошибок валидации или бизнес-правил.</summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// Создаёт успешный результат с данными и рекомендуемым HTTP-кодом.
    /// </summary>
    /// <param name="data">Полезные данные результата.</param>
    /// <param name="message">Безопасное описание результата.</param>
    /// <param name="statusCode">HTTP-код успешной операции.</param>
    /// <returns>Успешный неизменяемый ответ.</returns>
    public static Response<T> Success(
        T data,
        string message = "Операция выполнена успешно.",
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        ArgumentNullException.ThrowIfNull(data);
        return new Response<T>(true, statusCode, data, message, []);
    }

    /// <summary>
    /// Создаёт неуспешный результат с одной ожидаемой ошибкой.
    /// </summary>
    /// <param name="error">Безопасное описание ошибки.</param>
    /// <param name="statusCode">Рекомендуемый HTTP-код ошибки.</param>
    /// <returns>Неуспешный неизменяемый ответ.</returns>
    public static Response<T> Fail(
        string error,
        HttpStatusCode statusCode = HttpStatusCode.BadRequest)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        return new Response<T>(false, statusCode, default, "Операция не выполнена.", [error]);
    }

    /// <summary>
    /// Создаёт неуспешный результат с набором ошибок валидации.
    /// </summary>
    /// <param name="errors">Ошибки, безопасные для возврата клиенту.</param>
    /// <param name="statusCode">Рекомендуемый HTTP-код ошибки.</param>
    /// <returns>Неуспешный неизменяемый ответ.</returns>
    public static Response<T> Fail(
        IEnumerable<string> errors,
        HttpStatusCode statusCode = HttpStatusCode.BadRequest)
    {
        ArgumentNullException.ThrowIfNull(errors);
        var errorList = errors
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (errorList.Length == 0)
        {
            throw new ArgumentException("Список ошибок не может быть пустым.", nameof(errors));
        }

        return new Response<T>(false, statusCode, default, "Операция не выполнена.", errorList);
    }
}
