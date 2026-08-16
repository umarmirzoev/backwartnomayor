namespace Application.Common.Models;

/// <summary>
/// Определяет единый контракт пагинации и сортировки для списочных запросов Application-слоя.
/// Значения проверяются валидаторами конкретных CQRS-запросов до обращения к репозиторию,
/// благодаря чему REST-контракты используют одинаковую модель страниц и предсказуемую сортировку.
/// </summary>
public abstract record BaseFilterParam
{
    /// <summary>
    /// Получает номер запрошенной страницы, начиная с единицы.
    /// </summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// Получает количество элементов на странице; верхняя граница будет проверяться в Application-валидаторе.
    /// </summary>
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Получает имя разрешённого поля сортировки или <see langword="null"/> для сортировки по умолчанию.
    /// Обработчик обязан сопоставлять значение с белым списком полей, а не строить SQL из входной строки.
    /// </summary>
    public string? SortBy { get; init; }

    /// <summary>
    /// Получает признак сортировки по убыванию; значение <see langword="false"/> означает сортировку по возрастанию.
    /// </summary>
    public bool SortDescending { get; init; }
}
