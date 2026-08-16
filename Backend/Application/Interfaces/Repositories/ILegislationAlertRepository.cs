using Domain.Entities;
using CaseEntity = Domain.Entities.Case;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Определяет append-only хранение уведомлений об изменениях законодательства.
/// </summary>
public interface ILegislationAlertRepository
{
    /// <summary>
    /// Добавляет обнаруженное уведомление в текущую единицу работы.
    /// </summary>
    /// <param name="alert">Новое уведомление.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Задача постановки уведомления на добавление.</returns>
    Task AddAsync(LegislationAlert alert, CancellationToken cancellationToken);

    /// <summary>
    /// Возвращает уведомление по идентификатору для системной обработки.
    /// </summary>
    /// <param name="alertId">Идентификатор уведомления.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Уведомление или <see langword="null"/>.</returns>
    Task<LegislationAlert?> GetByIdAsync(Guid alertId, CancellationToken cancellationToken);

    /// <summary>
    /// Возвращает последние уведомления, обнаруженные не ранее указанного момента.
    /// </summary>
    /// <param name="detectedFrom">Нижняя граница времени обнаружения.</param>
    /// <param name="take">Максимальное количество уведомлений.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Последние уведомления без отслеживания.</returns>
    Task<IReadOnlyList<LegislationAlert>> GetRecentAsync(
        DateTimeOffset detectedFrom,
        int take,
        CancellationToken cancellationToken);

    /// <summary>
    /// Возвращает непрочитанные пары «уведомление — дело» текущего юриста одним запросом.
    /// </summary>
    /// <param name="lawyerId">Идентификатор профиля владельца дел.</param>
    /// <param name="detectedFrom">Необязательная нижняя граница времени обнаружения.</param>
    /// <param name="skip">Количество пропускаемых строк.</param>
    /// <param name="take">Максимальное количество строк.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Пары уведомлений и затронутых дел.</returns>
    Task<IReadOnlyList<(CaseLegislationAlert Link, LegislationAlert Alert, CaseEntity Case)>> GetUnreadForLawyerAsync(
        Guid lawyerId,
        DateTimeOffset? detectedFrom,
        int skip,
        int take,
        CancellationToken cancellationToken);

    /// <summary>
    /// Подсчитывает непрочитанные связи уведомлений с делами юриста.
    /// </summary>
    /// <param name="lawyerId">Идентификатор профиля владельца дел.</param>
    /// <param name="detectedFrom">Необязательная нижняя граница времени обнаружения.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Количество непрочитанных связей.</returns>
    Task<int> CountUnreadForLawyerAsync(
        Guid lawyerId,
        DateTimeOffset? detectedFrom,
        CancellationToken cancellationToken);
}
