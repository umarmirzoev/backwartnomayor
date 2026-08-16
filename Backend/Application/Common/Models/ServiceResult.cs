namespace Application.Common.Models;

/// <summary>
/// Представляет транспортно-независимый результат внешнего порта Application-слоя.
/// Он не содержит HTTP-кодов и позволяет адаптерам Identity, ИИ, S3 и экспорта сообщать ожидаемые ошибки
/// без выбрасывания исключений для обычного отказа внешней системы.
/// </summary>
/// <typeparam name="T">Тип успешного значения внешней операции.</typeparam>
public sealed class ServiceResult<T>
{
    private ServiceResult(bool succeeded, T? value, IReadOnlyList<string> errors)
    {
        Succeeded = succeeded;
        Value = value;
        Errors = errors;
    }

    /// <summary>Получает признак успешного выполнения внешней операции.</summary>
    public bool Succeeded { get; }

    /// <summary>Получает успешное значение или значение по умолчанию при отказе.</summary>
    public T? Value { get; }

    /// <summary>Получает безопасные ошибки адаптера без секретов и трассировок стека.</summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// Создаёт успешный результат внешнего порта.
    /// </summary>
    /// <param name="value">Полученное значение.</param>
    /// <returns>Успешный результат.</returns>
    public static ServiceResult<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new ServiceResult<T>(true, value, []);
    }

    /// <summary>
    /// Создаёт неуспешный результат внешнего порта.
    /// </summary>
    /// <param name="errors">Безопасные сообщения ошибок.</param>
    /// <returns>Неуспешный результат.</returns>
    public static ServiceResult<T> Failure(IEnumerable<string> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        var errorList = errors.Where(error => !string.IsNullOrWhiteSpace(error)).ToArray();
        if (errorList.Length == 0)
        {
            throw new ArgumentException("Список ошибок внешней операции не может быть пустым.", nameof(errors));
        }

        return new ServiceResult<T>(false, default, errorList);
    }
}

/// <summary>
/// Предоставляет безопасное резервное сообщение для некорректного адаптера,
/// который сообщил неуспех или недопустимое значение без описания ошибки.
/// </summary>
public static class ServiceResultExtensions
{
    /// <summary>Возвращает ошибки адаптера либо одно контролируемое сообщение без выбрасывания исключения.</summary>
    public static IReadOnlyList<string> GetErrorsOrDefault<T>(
        this ServiceResult<T> result,
        string fallbackError)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(fallbackError);
        return result.Errors.Count > 0 ? result.Errors : [fallbackError];
    }
}
