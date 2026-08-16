using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Реализует хранение периодических квот ИИ и разделяет чтение от операции изменения счётчика.
/// </summary>
public sealed class AiUsageQuotaRepository : BaseRepository<AiUsageQuota>, IAiUsageQuotaRepository
{
    /// <summary>
    /// Инициализирует репозиторий общим scoped-контекстом.
    /// </summary>
    /// <param name="context">Контекст данных приложения.</param>
    public AiUsageQuotaRepository(AppDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<AiUsageQuota?> GetCurrentAsync(
        Guid lawyerId,
        DateTimeOffset moment,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(lawyerId, nameof(lawyerId));

        return await BuildCurrentQuery(lawyerId, moment)
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AiUsageQuota?> GetCurrentForUpdateAsync(
        Guid lawyerId,
        DateTimeOffset moment,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(lawyerId, nameof(lawyerId));
        return await BuildCurrentQuery(lawyerId, moment).SingleOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsForPeriodAsync(
        Guid lawyerId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(lawyerId, nameof(lawyerId));
        RepositoryGuards.EnsurePeriod(periodStart, periodEnd);

        return await Entities.AnyAsync(
            quota => quota.LawyerId == lawyerId &&
                     quota.PeriodStart == periodStart &&
                     quota.PeriodEnd == periodEnd,
            cancellationToken);
    }

    /// <summary>
    /// Формирует запрос квоты, период которой содержит указанный момент.
    /// </summary>
    /// <param name="lawyerId">Идентификатор профиля юриста.</param>
    /// <param name="moment">Проверяемый момент.</param>
    /// <returns>Запрос текущей квоты с настраиваемым режимом отслеживания.</returns>
    private IQueryable<AiUsageQuota> BuildCurrentQuery(Guid lawyerId, DateTimeOffset moment)
    {
        return Entities.Where(quota =>
            quota.LawyerId == lawyerId &&
            quota.PeriodStart <= moment &&
            moment < quota.PeriodEnd);
    }
}
