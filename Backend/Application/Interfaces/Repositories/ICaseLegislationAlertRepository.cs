using Domain.Entities;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Определяет хранение связи уведомления с делом и состояния её прочтения.
/// </summary>
public interface ICaseLegislationAlertRepository : IBaseRepository<CaseLegislationAlert>
{
    /// <summary>
    /// Возвращает связь по идентификатору только для дела указанного юриста.
    /// </summary>
    /// <param name="linkId">Идентификатор связи.</param>
    /// <param name="lawyerId">Идентификатор профиля владельца дела.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Связь или <see langword="null"/> при отсутствии доступа.</returns>
    Task<CaseLegislationAlert?> GetByIdForLawyerAsync(
        Guid linkId,
        Guid lawyerId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Возвращает связь конкретного дела и уведомления.
    /// </summary>
    /// <param name="caseId">Идентификатор дела.</param>
    /// <param name="alertId">Идентификатор уведомления.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Связь или <see langword="null"/>.</returns>
    Task<CaseLegislationAlert?> GetByCaseAndAlertAsync(
        Guid caseId,
        Guid alertId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Проверяет существование связи для идемпотентного фонового импорта.
    /// </summary>
    /// <param name="caseId">Идентификатор дела.</param>
    /// <param name="alertId">Идентификатор уведомления.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns><see langword="true"/>, если связь уже существует.</returns>
    Task<bool> ExistsAsync(Guid caseId, Guid alertId, CancellationToken cancellationToken);
}
