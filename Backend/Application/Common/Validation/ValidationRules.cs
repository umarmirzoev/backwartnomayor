using FluentValidation;

namespace Application.Common.Validation;

/// <summary>
/// Содержит единые пределы и повторно используемые правила входных контрактов Application-слоя.
/// Централизация исключает расхождение пагинации между CQRS-запросами и ограничивает ресурсоёмкие выборки.
/// </summary>
public static class ValidationRules
{
    /// <summary>Максимальное число элементов одной REST-страницы.</summary>
    public const int MaximumPageSize = 100;

    /// <summary>Максимальный номер страницы, предотвращающий переполнение вычисления Skip.</summary>
    public const int MaximumPageNumber = 1_000_000;

    /// <summary>Защитный предел инструкции или описания, передаваемого внешнему ИИ-провайдеру.</summary>
    public const int MaximumAiPromptLength = 20_000;

    /// <summary>Защитный предел полного текста документа, обрабатываемого в памяти и объектном хранилище.</summary>
    public const int MaximumDocumentTextLength = 1_000_000;

    /// <summary>Доменный предел описания изменений неизменяемой версии документа.</summary>
    public const int MaximumChangeSummaryLength = 1_000;

    /// <summary>
    /// Добавляет унифицированные правила номера страницы, размера и технического имени сортировки.
    /// </summary>
    /// <typeparam name="T">Тип фильтра или запроса.</typeparam>
    /// <param name="validator">Настраиваемый валидатор.</param>
    /// <param name="pageNumber">Выражение номера страницы.</param>
    /// <param name="pageSize">Выражение размера страницы.</param>
    /// <param name="sortBy">Выражение поля сортировки.</param>
    public static void AddPaginationRules<T>(
        this AbstractValidator<T> validator,
        System.Linq.Expressions.Expression<Func<T, int>> pageNumber,
        System.Linq.Expressions.Expression<Func<T, int>> pageSize,
        System.Linq.Expressions.Expression<Func<T, string?>> sortBy)
    {
        validator.RuleFor(pageNumber)
            .InclusiveBetween(1, MaximumPageNumber)
            .WithMessage($"Номер страницы должен находиться в диапазоне от 1 до {MaximumPageNumber}.");
        validator.RuleFor(pageSize)
            .InclusiveBetween(1, MaximumPageSize)
            .WithMessage($"Размер страницы должен находиться в диапазоне от 1 до {MaximumPageSize}.");
        validator.RuleFor(sortBy)
            .MaximumLength(50)
            .WithMessage("Имя поля сортировки не должно превышать 50 символов.")
            .When(instance => !string.IsNullOrWhiteSpace(sortBy.Compile()(instance)));
    }

    /// <summary>
    /// Вычисляет безопасное количество пропускаемых строк после успешной FluentValidation-проверки.
    /// </summary>
    /// <param name="pageNumber">Проверенный номер страницы.</param>
    /// <param name="pageSize">Проверенный размер страницы.</param>
    /// <returns>Неотрицательное значение Skip без переполнения.</returns>
    public static int CalculateSkip(int pageNumber, int pageSize)
    {
        return checked((pageNumber - 1) * pageSize);
    }
}
