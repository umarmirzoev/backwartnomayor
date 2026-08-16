using Domain.Entities;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Определяет хранение периодических квот ИИ-запросов юриста.
/// </summary>
public interface IAiUsageQuotaRepository : IBaseRepository<AiUsageQuota>
{
    /// <summary>
    /// Возвращает текущую квоту без отслеживания для отображения остатка лимита.
    /// </summary>
    /// <param name="lawyerId">Идентификатор профиля юриста.</param>
    /// <param name="moment">Момент, который должен попадать в период квоты.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Квота периода или <see langword="null"/>.</returns>
    Task<AiUsageQuota?> GetCurrentAsync(
        Guid lawyerId,
        DateTimeOffset moment,
        CancellationToken cancellationToken);

    /// <summary>
    /// Возвращает отслеживаемую текущую квоту для атомарной регистрации ИИ-запроса.
    /// </summary>
    /// <param name="lawyerId">Идентификатор профиля юриста.</param>
    /// <param name="moment">Момент расходования квоты.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Отслеживаемая квота или <see langword="null"/>.</returns>
    Task<AiUsageQuota?> GetCurrentForUpdateAsync(
        Guid lawyerId,
        DateTimeOffset moment,
        CancellationToken cancellationToken);

    /// <summary>
    /// Проверяет наличие квоты юриста с точными границами периода.
    /// </summary>
    /// <param name="lawyerId">Идентификатор профиля юриста.</param>
    /// <param name="periodStart">Начало периода включительно.</param>
    /// <param name="periodEnd">Конец периода исключительно.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns><see langword="true"/>, если квота уже существует.</returns>
    Task<bool> ExistsForPeriodAsync(
        Guid lawyerId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken);
}
