namespace Infrastructure.Repositories;

/// <summary>
/// Содержит единообразные защитные проверки технических параметров репозиториев.
/// Бизнес-валидация остаётся в Domain и будущих FluentValidation-валидаторах Application.
/// </summary>
internal static class RepositoryGuards
{
    /// <summary>
    /// Проверяет, что идентификатор не является пустым Guid.
    /// </summary>
    /// <param name="value">Проверяемый идентификатор.</param>
    /// <param name="parameterName">Имя параметра вызывающего метода.</param>
    public static void EnsureNotEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Идентификатор не может быть пустым.", parameterName);
        }
    }

    /// <summary>
    /// Проверяет неотрицательное смещение и положительный размер страницы.
    /// </summary>
    /// <param name="skip">Количество пропускаемых строк.</param>
    /// <param name="take">Максимальное количество строк.</param>
    public static void EnsurePagination(int skip, int take)
    {
        if (skip < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skip),
                skip,
                "Количество пропускаемых строк не может быть отрицательным.");
        }

        EnsurePositive(take, nameof(take));
    }

    /// <summary>
    /// Проверяет положительное числовое значение.
    /// </summary>
    /// <param name="value">Проверяемое значение.</param>
    /// <param name="parameterName">Имя параметра вызывающего метода.</param>
    public static void EnsurePositive(int value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Значение должно быть больше нуля.");
        }
    }

    /// <summary>
    /// Проверяет корректность полуоткрытого временного интервала.
    /// </summary>
    /// <param name="periodStart">Начало интервала включительно.</param>
    /// <param name="periodEnd">Конец интервала исключительно.</param>
    public static void EnsurePeriod(DateTimeOffset periodStart, DateTimeOffset periodEnd)
    {
        if (periodEnd <= periodStart)
        {
            throw new ArgumentException("Конец периода должен быть позже его начала.");
        }
    }

    /// <summary>
    /// Нормализует необязательный текстовый фильтр без изменения его регистра.
    /// </summary>
    /// <param name="value">Исходное значение фильтра.</param>
    /// <returns>Обрезанное значение или <see langword="null"/>.</returns>
    public static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
