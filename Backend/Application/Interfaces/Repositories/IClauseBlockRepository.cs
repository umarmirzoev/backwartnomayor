using Domain.Entities;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Определяет поиск и хранение переиспользуемых блоков договорных пунктов.
/// </summary>
public interface IClauseBlockRepository : IBaseRepository<ClauseBlock>
{
    /// <summary>
    /// Возвращает активный блок, допустимый для включения в новый документ.
    /// </summary>
    /// <param name="clauseBlockId">Идентификатор блока.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Активный блок или <see langword="null"/>.</returns>
    Task<ClauseBlock?> GetActiveByIdAsync(Guid clauseBlockId, CancellationToken cancellationToken);

    /// <summary>
    /// Выполняет поиск по категории, заголовку и двуязычному тексту активных блоков.
    /// </summary>
    /// <param name="searchTerm">Необязательная поисковая строка.</param>
    /// <param name="category">Необязательная точная категория.</param>
    /// <param name="skip">Количество пропускаемых строк.</param>
    /// <param name="take">Максимальное количество строк.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Упорядоченная страница найденных блоков.</returns>
    Task<IReadOnlyList<ClauseBlock>> SearchActiveAsync(
        string? searchTerm,
        string? category,
        int skip,
        int take,
        CancellationToken cancellationToken);

    /// <summary>
    /// Подсчитывает активные блоки по тем же фильтрам, что использует поиск.
    /// </summary>
    /// <param name="searchTerm">Необязательная поисковая строка.</param>
    /// <param name="category">Необязательная точная категория.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Количество найденных блоков.</returns>
    Task<int> CountActiveAsync(
        string? searchTerm,
        string? category,
        CancellationToken cancellationToken);

    /// <summary>
    /// Возвращает активные блоки по идентификаторам для контролируемой RAG-сборки.
    /// </summary>
    /// <param name="clauseBlockIds">Набор идентификаторов выбранных блоков.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Активные блоки без отслеживания.</returns>
    Task<IReadOnlyList<ClauseBlock>> GetActiveByIdsAsync(
        IReadOnlyCollection<Guid> clauseBlockIds,
        CancellationToken cancellationToken);
}
