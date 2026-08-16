using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Реализует append-only хранение фактов расходования ИИ-квоты.
/// </summary>
public sealed class AiUsageRecordRepository : IAiUsageRecordRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Инициализирует репозиторий общим scoped-контекстом.
    /// </summary>
    /// <param name="context">Контекст данных приложения.</param>
    public AiUsageRecordRepository(AppDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc />
    public async Task AddAsync(AiUsageRecord record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        await _context.AiUsageRecords.AddAsync(record, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AiUsageRecord>> GetByLawyerAsync(
        Guid lawyerId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(lawyerId, nameof(lawyerId));
        RepositoryGuards.EnsurePeriod(periodStart, periodEnd);
        RepositoryGuards.EnsurePagination(skip, take);

        return await BuildPeriodQuery(lawyerId, periodStart, periodEnd)
            .OrderByDescending(record => record.CreatedAt)
            .ThenByDescending(record => record.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountByLawyerAsync(
        Guid lawyerId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(lawyerId, nameof(lawyerId));
        RepositoryGuards.EnsurePeriod(periodStart, periodEnd);

        return await BuildPeriodQuery(lawyerId, periodStart, periodEnd)
            .CountAsync(cancellationToken);
    }

    /// <summary>
    /// Формирует единый неотслеживаемый фильтр использования за период.
    /// </summary>
    /// <param name="lawyerId">Идентификатор профиля юриста.</param>
    /// <param name="periodStart">Начало интервала включительно.</param>
    /// <param name="periodEnd">Конец интервала исключительно.</param>
    /// <returns>Запрос фактов использования.</returns>
    private IQueryable<AiUsageRecord> BuildPeriodQuery(
        Guid lawyerId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd)
    {
        return _context.AiUsageRecords
            .AsNoTracking()
            .Where(record =>
                record.LawyerId == lawyerId &&
                record.CreatedAt >= periodStart &&
                record.CreatedAt < periodEnd);
    }
}
