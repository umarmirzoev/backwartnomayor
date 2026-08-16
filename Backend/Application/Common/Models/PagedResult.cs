namespace Application.Common.Models;

/// <summary>
/// Представляет неизменяемый результат постраничного Application-запроса.
/// Обёртка отделяет транспортные метаданные от конкретного DTO и подходит для возврата
/// из MediatR-обработчиков и последующей сериализации REST-контроллерами.
/// </summary>
/// <typeparam name="T">Тип элемента безопасной модели ответа.</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>
    /// Инициализирует проверенный постраничный результат и вычисляет производные признаки навигации.
    /// </summary>
    /// <param name="items">Элементы текущей страницы.</param>
    /// <param name="totalCount">Общее количество элементов до применения пагинации.</param>
    /// <param name="pageNumber">Номер текущей страницы, начиная с единицы.</param>
    /// <param name="pageSize">Положительный размер страницы.</param>
    /// <exception cref="ArgumentNullException">Выбрасывается при отсутствии коллекции элементов.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Выбрасывается при недопустимых метаданных страницы.</exception>
    public PagedResult(
        IReadOnlyList<T> items,
        int totalCount,
        int pageNumber,
        int pageSize)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (totalCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalCount),
                "Общее количество элементов не может быть отрицательным.");
        }

        if (pageNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageNumber),
                "Номер страницы должен быть больше нуля.");
        }

        if (pageSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                "Размер страницы должен быть больше нуля.");
        }

        if (items.Count > pageSize)
        {
            throw new ArgumentException(
                "Количество элементов текущей страницы не может превышать размер страницы.",
                nameof(items));
        }

        if (items.Count > totalCount)
        {
            throw new ArgumentException(
                "Количество элементов текущей страницы не может превышать общее количество элементов.",
                nameof(items));
        }

        Items = [.. items];
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalPages = totalCount == 0
            ? 0
            : checked((int)Math.Ceiling(totalCount / (double)pageSize));
    }

    /// <summary>
    /// Получает неизменяемый список элементов текущей страницы.
    /// </summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>
    /// Получает общее количество элементов с учётом фильтра, но до пагинации.
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// Получает номер текущей страницы, начиная с единицы.
    /// </summary>
    public int PageNumber { get; }

    /// <summary>
    /// Получает запрошенный размер страницы.
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// Получает вычисленное общее количество страниц; для пустого результата значение равно нулю.
    /// </summary>
    public int TotalPages { get; }

    /// <summary>
    /// Получает признак наличия предыдущей непустой страницы.
    /// </summary>
    public bool HasPreviousPage => TotalPages > 0 && PageNumber > 1;

    /// <summary>
    /// Получает признак наличия следующей страницы.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;
}
