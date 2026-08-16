using Domain.Entities;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Определяет append-only хранение неизменяемых версий документов.
/// Контракт намеренно не содержит обновление и удаление отдельной версии.
/// </summary>
public interface IDocumentVersionRepository
{
    /// <summary>
    /// Добавляет новую неизменяемую версию в текущую единицу работы.
    /// </summary>
    /// <param name="version">Созданная агрегатом версия.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Задача постановки версии на добавление.</returns>
    Task AddAsync(DocumentVersion version, CancellationToken cancellationToken);

    /// <summary>
    /// Возвращает конкретную версию после проверки владельца через Draft → Case.
    /// </summary>
    /// <param name="versionId">Идентификатор версии.</param>
    /// <param name="lawyerId">Идентификатор профиля владельца.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Версия или <see langword="null"/> при отсутствии доступа.</returns>
    Task<DocumentVersion?> GetByIdForLawyerAsync(
        Guid versionId,
        Guid lawyerId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Возвращает историю версий черновика в обратном порядке номеров после проверки владельца.
    /// </summary>
    /// <param name="draftId">Идентификатор черновика.</param>
    /// <param name="lawyerId">Идентификатор профиля владельца.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Неизменяемая история версий.</returns>
    Task<IReadOnlyList<DocumentVersion>> GetHistoryForDraftForLawyerAsync(
        Guid draftId,
        Guid lawyerId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Возвращает текущую версию черновика по его указателю после проверки владельца.
    /// </summary>
    /// <param name="draftId">Идентификатор черновика.</param>
    /// <param name="lawyerId">Идентификатор профиля владельца.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Текущая версия или <see langword="null"/>.</returns>
    Task<DocumentVersion?> GetCurrentForDraftForLawyerAsync(
        Guid draftId,
        Guid lawyerId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Вычисляет следующий номер версии только для доступного юристу черновика.
    /// Вызов и вставка должны выполняться внутри одной транзакции;
    /// уникальный индекс остаётся защитой от конкурентной гонки.
    /// </summary>
    /// <param name="draftId">Идентификатор черновика.</param>
    /// <param name="lawyerId">Идентификатор профиля владельца.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Следующий номер либо <see langword="null"/> при отсутствии доступа.</returns>
    Task<int?> GetNextVersionNumberForLawyerAsync(
        Guid draftId,
        Guid lawyerId,
        CancellationToken cancellationToken);
}
