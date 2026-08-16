using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Реализует tenant-безопасное хранение главного агрегата договорного черновика.
/// </summary>
public sealed class DraftRepository : BaseRepository<Draft>, IDraftRepository
{
    /// <summary>
    /// Инициализирует репозиторий общим scoped-контекстом.
    /// </summary>
    /// <param name="context">Контекст данных приложения.</param>
    public DraftRepository(AppDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<Draft?> GetByIdForLawyerAsync(
        Guid draftId,
        Guid lawyerId,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(draftId, nameof(draftId));
        RepositoryGuards.EnsureNotEmpty(lawyerId, nameof(lawyerId));

        return await BuildLawyerQuery(lawyerId)
            .SingleOrDefaultAsync(draft => draft.Id == draftId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(Draft Draft, DocumentVersion? CurrentVersion)?>
        GetWithCurrentVersionForLawyerAsync(
            Guid draftId,
            Guid lawyerId,
            CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(draftId, nameof(draftId));
        RepositoryGuards.EnsureNotEmpty(lawyerId, nameof(lawyerId));

        var query =
            from draft in Context.Drafts.AsNoTracking()
            join caseItem in Context.Cases.AsNoTracking()
                on draft.CaseId equals caseItem.Id
            join version in Context.DocumentVersions.AsNoTracking()
                on draft.CurrentVersionId equals (Guid?)version.Id into currentVersions
            from currentVersion in currentVersions.DefaultIfEmpty()
            where draft.Id == draftId && caseItem.LawyerId == lawyerId
            select new
            {
                Draft = draft,
                CurrentVersion = currentVersion
            };

        var result = await query.SingleOrDefaultAsync(cancellationToken);
        return result is null ? null : (result.Draft, result.CurrentVersion);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Draft>> GetByCaseForLawyerAsync(
        Guid caseId,
        Guid lawyerId,
        DocumentStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(caseId, nameof(caseId));
        RepositoryGuards.EnsureNotEmpty(lawyerId, nameof(lawyerId));
        RepositoryGuards.EnsurePagination(skip, take);

        return await BuildCaseQuery(caseId, lawyerId, status)
            .OrderByDescending(draft => draft.CreatedAt)
            .ThenBy(draft => draft.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountByCaseForLawyerAsync(
        Guid caseId,
        Guid lawyerId,
        DocumentStatus? status,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(caseId, nameof(caseId));
        RepositoryGuards.EnsureNotEmpty(lawyerId, nameof(lawyerId));

        return await BuildCaseQuery(caseId, lawyerId, status).CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsForLawyerAsync(
        Guid draftId,
        Guid lawyerId,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(draftId, nameof(draftId));
        RepositoryGuards.EnsureNotEmpty(lawyerId, nameof(lawyerId));

        return await (
            from draft in Context.Drafts
            join caseItem in Context.Cases on draft.CaseId equals caseItem.Id
            where draft.Id == draftId && caseItem.LawyerId == lawyerId
            select draft.Id)
            .AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Draft>> GetExpiredBatchAsync(
        DateTimeOffset utcNow,
        int batchSize,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsurePositive(batchSize, nameof(batchSize));

        return await Entities
            .AsNoTracking()
            .Where(draft =>
                draft.Status == DocumentStatus.SentToClient &&
                draft.DueRespondByDate.HasValue &&
                draft.DueRespondByDate.Value <= utcNow)
            .OrderBy(draft => draft.DueRespondByDate)
            .ThenBy(draft => draft.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Формирует выборку черновиков через денормализованного владельца дела.
    /// </summary>
    /// <param name="lawyerId">Идентификатор профиля владельца.</param>
    /// <returns>Неотслеживаемый tenant-безопасный запрос.</returns>
    private IQueryable<Draft> BuildLawyerQuery(Guid lawyerId)
    {
        return
            from draft in Context.Drafts.AsNoTracking()
            join caseItem in Context.Cases.AsNoTracking()
                on draft.CaseId equals caseItem.Id
            where caseItem.LawyerId == lawyerId
            select draft;
    }

    /// <summary>
    /// Формирует общий запрос документов дела для страницы и счётчика.
    /// </summary>
    /// <param name="caseId">Идентификатор дела.</param>
    /// <param name="lawyerId">Идентификатор профиля владельца.</param>
    /// <param name="status">Необязательный фильтр состояния.</param>
    /// <returns>Неотслеживаемый запрос документов.</returns>
    private IQueryable<Draft> BuildCaseQuery(
        Guid caseId,
        Guid lawyerId,
        DocumentStatus? status)
    {
        var query = BuildLawyerQuery(lawyerId).Where(draft => draft.CaseId == caseId);
        return status.HasValue
            ? query.Where(draft => draft.Status == status.Value)
            : query;
    }
}
