using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using CaseEntity = Domain.Entities.Case;

namespace Infrastructure.Repositories;

/// <summary>
/// Реализует append-only хранение законодательных уведомлений и агрегированную tenant-выборку.
/// </summary>
public sealed class LegislationAlertRepository : ILegislationAlertRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Инициализирует репозиторий общим scoped-контекстом.
    /// </summary>
    /// <param name="context">Контекст данных приложения.</param>
    public LegislationAlertRepository(AppDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc />
    public async Task AddAsync(
        LegislationAlert alert,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(alert);
        await _context.LegislationAlerts.AddAsync(alert, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<LegislationAlert?> GetByIdAsync(
        Guid alertId,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(alertId, nameof(alertId));

        return await _context.LegislationAlerts
            .AsNoTracking()
            .SingleOrDefaultAsync(alert => alert.Id == alertId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LegislationAlert>> GetRecentAsync(
        DateTimeOffset detectedFrom,
        int take,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsurePositive(take, nameof(take));

        return await _context.LegislationAlerts
            .AsNoTracking()
            .Where(alert => alert.DetectedAt >= detectedFrom)
            .OrderByDescending(alert => alert.DetectedAt)
            .ThenBy(alert => alert.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<(CaseLegislationAlert Link, LegislationAlert Alert, CaseEntity Case)>>
        GetUnreadForLawyerAsync(
            Guid lawyerId,
            DateTimeOffset? detectedFrom,
            int skip,
            int take,
            CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(lawyerId, nameof(lawyerId));
        RepositoryGuards.EnsurePagination(skip, take);

        var query =
            from link in _context.CaseLegislationAlerts.AsNoTracking()
            join caseItem in _context.Cases.AsNoTracking()
                on link.CaseId equals caseItem.Id
            join alert in _context.LegislationAlerts.AsNoTracking()
                on link.LegislationAlertId equals alert.Id
            where caseItem.LawyerId == lawyerId
                && !link.IsRead
                && (!detectedFrom.HasValue || alert.DetectedAt >= detectedFrom.Value)
            orderby alert.DetectedAt descending, alert.Id, caseItem.Id
            select new
            {
                Link = link,
                Alert = alert,
                Case = caseItem
            };

        var rows = await query.Skip(skip).Take(take).ToListAsync(cancellationToken);
        return rows.Select(row => (row.Link, row.Alert, row.Case)).ToList();
    }

    /// <inheritdoc />
    public async Task<int> CountUnreadForLawyerAsync(
        Guid lawyerId,
        DateTimeOffset? detectedFrom,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(lawyerId, nameof(lawyerId));

        return await (
            from link in _context.CaseLegislationAlerts
            join caseItem in _context.Cases on link.CaseId equals caseItem.Id
            join alert in _context.LegislationAlerts on link.LegislationAlertId equals alert.Id
            where caseItem.LawyerId == lawyerId
                && !link.IsRead
                && (!detectedFrom.HasValue || alert.DetectedAt >= detectedFrom.Value)
            select link.Id)
            .CountAsync(cancellationToken);
    }
}
