using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Определяет tenant-безопасные операции хранения главного агрегата договорного черновика.
/// </summary>
public interface IDraftRepository : IBaseRepository<Draft>
{
    /// <summary>
    /// Возвращает черновик только при принадлежности его дела указанному юристу.
    /// </summary>
    /// <param name="draftId">Идентификатор черновика.</param>
    /// <param name="lawyerId">Идентификатор профиля владельца.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Черновик или <see langword="null"/> при отсутствии доступа.</returns>
    Task<Draft?> GetByIdForLawyerAsync(
        Guid draftId,
        Guid lawyerId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Одним запросом возвращает черновик и его текущую версию после проверки владельца.
    /// </summary>
    /// <param name="draftId">Идентификатор черновика.</param>
    /// <param name="lawyerId">Идентификатор профиля владельца.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Пара черновика и текущей версии либо <see langword="null"/>.</returns>
    Task<(Draft Draft, DocumentVersion? CurrentVersion)?> GetWithCurrentVersionForLawyerAsync(
        Guid draftId,
        Guid lawyerId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Возвращает страницу документов дела с проверкой владельца и фильтром состояния.
    /// </summary>
    /// <param name="caseId">Идентификатор дела.</param>
    /// <param name="lawyerId">Идентификатор профиля владельца.</param>
    /// <param name="status">Необязательный фильтр жизненного цикла.</param>
    /// <param name="skip">Количество пропускаемых строк.</param>
    /// <param name="take">Максимальное количество строк.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Страница черновиков дела.</returns>
    Task<IReadOnlyList<Draft>> GetByCaseForLawyerAsync(
        Guid caseId,
        Guid lawyerId,
        DocumentStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken);

    /// <summary>
    /// Подсчитывает документы дела с теми же фильтрами, что и списочная выборка.
    /// </summary>
    /// <param name="caseId">Идентификатор дела.</param>
    /// <param name="lawyerId">Идентификатор профиля владельца.</param>
    /// <param name="status">Необязательный фильтр жизненного цикла.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Количество документов дела.</returns>
    Task<int> CountByCaseForLawyerAsync(
        Guid caseId,
        Guid lawyerId,
        DocumentStatus? status,
        CancellationToken cancellationToken);

    /// <summary>
    /// Проверяет принадлежность черновика указанному юристу без материализации.
    /// </summary>
    /// <param name="draftId">Идентификатор черновика.</param>
    /// <param name="lawyerId">Идентификатор профиля владельца.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns><see langword="true"/>, если черновик доступен юристу.</returns>
    Task<bool> ExistsForLawyerAsync(
        Guid draftId,
        Guid lawyerId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Возвращает ограниченную пачку просроченных документов для Post-MVP фоновой задачи.
    /// </summary>
    /// <param name="utcNow">Текущий момент UTC.</param>
    /// <param name="batchSize">Максимальный размер обрабатываемой пачки.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Черновики, срок ответа по которым истёк.</returns>
    Task<IReadOnlyList<Draft>> GetExpiredBatchAsync(
        DateTimeOffset utcNow,
        int batchSize,
        CancellationToken cancellationToken);
}
