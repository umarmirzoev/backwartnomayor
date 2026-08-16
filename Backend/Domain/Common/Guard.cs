using Domain.Exceptions;

namespace Domain.Common;

/// <summary>
/// Содержит единообразные проверки входных данных доменной модели.
/// Все нарушения преобразуются в контролируемые доменные исключения с русскими сообщениями.
/// </summary>
internal static class Guard
{
    /// <summary>
    /// Проверяет, что идентификатор не является пустым.
    /// </summary>
    /// <param name="value">Проверяемый идентификатор.</param>
    /// <param name="fieldName">Русское наименование поля для сообщения об ошибке.</param>
    /// <returns>Проверенный идентификатор.</returns>
    internal static Guid AgainstEmpty(Guid value, string fieldName)
    {
        if (value == Guid.Empty)
        {
            throw new DomainValidationException($"Поле «{fieldName}» не может содержать пустой идентификатор.");
        }

        return value;
    }

    /// <summary>
    /// Проверяет обязательную строку, удаляет внешние пробелы и контролирует длину.
    /// </summary>
    /// <param name="value">Проверяемое строковое значение.</param>
    /// <param name="fieldName">Русское наименование поля для сообщения об ошибке.</param>
    /// <param name="maxLength">Максимально допустимая длина или <see langword="null"/> без ограничения.</param>
    /// <returns>Нормализованная непустая строка.</returns>
    internal static string RequiredText(string? value, string fieldName, int? maxLength = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"Поле «{fieldName}» обязательно для заполнения.");
        }

        var normalized = value.Trim();
        EnsureLength(normalized, fieldName, maxLength);
        return normalized;
    }

    /// <summary>
    /// Нормализует необязательную строку и контролирует её максимальную длину.
    /// Пустое или состоящее из пробелов значение преобразуется в <see langword="null"/>.
    /// </summary>
    /// <param name="value">Проверяемое строковое значение.</param>
    /// <param name="fieldName">Русское наименование поля для сообщения об ошибке.</param>
    /// <param name="maxLength">Максимально допустимая длина или <see langword="null"/> без ограничения.</param>
    /// <returns>Нормализованная строка либо <see langword="null"/>.</returns>
    internal static string? OptionalText(string? value, string fieldName, int? maxLength = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        EnsureLength(normalized, fieldName, maxLength);
        return normalized;
    }

    /// <summary>
    /// Проверяет, что дата и время не равны значению по умолчанию и представлены в UTC.
    /// </summary>
    /// <param name="value">Проверяемая дата.</param>
    /// <param name="fieldName">Русское наименование поля для сообщения об ошибке.</param>
    /// <returns>Проверенная дата.</returns>
    internal static DateTimeOffset AgainstDefault(DateTimeOffset value, string fieldName)
    {
        if (value == default)
        {
            throw new DomainValidationException($"Поле «{fieldName}» должно содержать корректную дату и время.");
        }

        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainValidationException($"Поле «{fieldName}» должно быть представлено в UTC.");
        }

        return value;
    }

    /// <summary>
    /// Проверяет, что целое число не является отрицательным.
    /// </summary>
    /// <param name="value">Проверяемое число.</param>
    /// <param name="fieldName">Русское наименование поля для сообщения об ошибке.</param>
    /// <returns>Проверенное число.</returns>
    internal static int AgainstNegative(int value, string fieldName)
    {
        if (value < 0)
        {
            throw new DomainValidationException($"Поле «{fieldName}» не может быть отрицательным.");
        }

        return value;
    }

    /// <summary>
    /// Проверяет, что целое число строго больше нуля.
    /// </summary>
    /// <param name="value">Проверяемое число.</param>
    /// <param name="fieldName">Русское наименование поля для сообщения об ошибке.</param>
    /// <returns>Проверенное положительное число.</returns>
    internal static int AgainstNonPositive(int value, string fieldName)
    {
        if (value <= 0)
        {
            throw new DomainValidationException($"Поле «{fieldName}» должно быть больше нуля.");
        }

        return value;
    }

    /// <summary>
    /// Проверяет, что значение перечисления определено его типом.
    /// </summary>
    /// <typeparam name="TEnum">Тип проверяемого перечисления.</typeparam>
    /// <param name="value">Проверяемое значение.</param>
    /// <param name="fieldName">Русское наименование поля для сообщения об ошибке.</param>
    /// <returns>Проверенное значение перечисления.</returns>
    internal static TEnum AgainstInvalidEnum<TEnum>(TEnum value, string fieldName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new DomainValidationException($"Поле «{fieldName}» содержит неподдерживаемое значение.");
        }

        return value;
    }

    /// <summary>
    /// Выбрасывает доменное исключение при выполнении запрещённого условия.
    /// </summary>
    /// <param name="condition">Условие, означающее нарушение инварианта.</param>
    /// <param name="message">Русское сообщение о нарушенном правиле.</param>
    internal static void Against(bool condition, string message)
    {
        if (condition)
        {
            throw new DomainValidationException(message);
        }
    }

    /// <summary>
    /// Проверяет максимальную длину нормализованной строки.
    /// </summary>
    /// <param name="value">Нормализованная строка.</param>
    /// <param name="fieldName">Русское наименование поля.</param>
    /// <param name="maxLength">Максимально допустимая длина.</param>
    private static void EnsureLength(string value, string fieldName, int? maxLength)
    {
        if (maxLength.HasValue && value.Length > maxLength.Value)
        {
            throw new DomainValidationException(
                $"Поле «{fieldName}» не может содержать более {maxLength.Value} символов.");
        }
    }
}
