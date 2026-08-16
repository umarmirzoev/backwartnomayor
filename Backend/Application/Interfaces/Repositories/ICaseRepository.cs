using Domain.Enums;
using CaseEntity = Domain.Entities.Case;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Определяет tenant-безопасные операции хранения дел клиентов.
/// </summary>
public interface ICaseRepository : IBaseRepository<CaseEntity>
{
    /// <summary>
    /// Возвращает дело только при совпадении владельца с текущим юристом.
    /// </summary>
    /// <param name="caseId">Идентификатор дела.</param>
    /// <param name="lawyerId">Идентификатор профиля владельца.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Дело или <see langword="null"/> при отсутствии доступа.</returns>
    Task<CaseEntity?> GetByIdForLawyerAsync(
        Guid caseId,
        Guid lawyerId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Проверяет наличие дела у указанного юриста.
    /// </summary>
    /// <param name="caseId">Идентификатор дела.</param>
    /// <param name="lawyerId">Идентификатор профиля владельца.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns><see langword="true"/>, если дело принадлежит юристу.</returns>
    Task<bool> ExistsForLawyerAsync(
        Guid caseId,
        Guid lawyerId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Возвращает страницу дел клиента с двойной проверкой ClientId и LawyerId.
    /// </summary>
    /// <param name="clientId">Идентификатор клиента.</param>
    /// <param name="lawyerId">Идентификатор профиля владельца.</param>
    /// <param name="status">Необязательный фильтр состояния.</param>
    /// <param name="skip">Количество пропускаемых строк.</param>
    /// <param name="take">Максимальное количество строк.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Страница дел клиента.</returns>
    Task<IReadOnlyList<CaseEntity>> GetByClientForLawyerAsync(
        Guid clientId,
        Guid lawyerId,
        CaseStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken);

    /// <summary>
    /// Подсчитывает дела клиента с теми же ограничениями владельца и состояния.
    /// </summary>
    /// <param name="clientId">Идентификатор клиента.</param>
    /// <param name="lawyerId">Идентификатор профиля владельца.</param>
    /// <param name="status">Необязательный фильтр состояния.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Количество подходящих дел.</returns>
    Task<int> CountByClientForLawyerAsync(
        Guid clientId,
        Guid lawyerId,
        CaseStatus? status,
        CancellationToken cancellationToken);

    /// <summary>
    /// Возвращает открытые дела юриста для фонового сопоставления законодательства.
    /// </summary>
    /// <param name="lawyerId">Идентификатор профиля владельца.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Список открытых дел без отслеживания.</returns>
    Task<IReadOnlyList<CaseEntity>> GetOpenByLawyerAsync(
        Guid lawyerId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Возвращает дела по набору идентификаторов для доверенного фонового сопоставления законодательства.
    /// Метод не предназначен для пользовательских запросов и требует системной авторизации обработчика.
    /// </summary>
    /// <param name="caseIds">Уникальный непустой набор идентификаторов дел.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Найденные дела без отслеживания изменений.</returns>
    Task<IReadOnlyList<CaseEntity>> GetByIdsForSystemAsync(
        IReadOnlyCollection<Guid> caseIds,
        CancellationToken cancellationToken);
}
