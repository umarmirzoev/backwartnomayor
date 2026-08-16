using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Реализует хранение состояния прочтения законодательного уведомления по делу.
/// </summary>
public sealed class CaseLegislationAlertRepository
    : BaseRepository<CaseLegislationAlert>, ICaseLegislationAlertRepository
{
    /// <summary>
    /// Инициализирует репозиторий общим scoped-контекстом.
    /// </summary>
    /// <param name="context">Контекст данных приложения.</param>
    public CaseLegislationAlertRepository(AppDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<CaseLegislationAlert?> GetByIdForLawyerAsync(
        Guid linkId,
        Guid lawyerId,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(linkId, nameof(linkId));
        RepositoryGuards.EnsureNotEmpty(lawyerId, nameof(lawyerId));

        var query =
            from link in Context.CaseLegislationAlerts.AsNoTracking()
            join caseItem in Context.Cases.AsNoTracking()
                on link.CaseId equals caseItem.Id
            where link.Id == linkId && caseItem.LawyerId == lawyerId
            select link;

        return await query.SingleOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CaseLegislationAlert?> GetByCaseAndAlertAsync(
        Guid caseId,
        Guid alertId,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(caseId, nameof(caseId));
        RepositoryGuards.EnsureNotEmpty(alertId, nameof(alertId));

        return await Entities
            .AsNoTracking()
            .SingleOrDefaultAsync(
                link => link.CaseId == caseId && link.LegislationAlertId == alertId,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(
        Guid caseId,
        Guid alertId,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(caseId, nameof(caseId));
        RepositoryGuards.EnsureNotEmpty(alertId, nameof(alertId));

        return await Entities.AnyAsync(
            link => link.CaseId == caseId && link.LegislationAlertId == alertId,
            cancellationToken);
    }
}
