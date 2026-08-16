using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Реализует append-only хранение неизменяемых версий и tenant-безопасную историю документа.
/// </summary>
public sealed class DocumentVersionRepository : IDocumentVersionRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Инициализирует репозиторий общим scoped-контекстом.
    /// </summary>
    /// <param name="context">Контекст данных приложения.</param>
    public DocumentVersionRepository(AppDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc />
    public async Task AddAsync(
        DocumentVersion version,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(version);
        await _context.DocumentVersions.AddAsync(version, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentVersion?> GetByIdForLawyerAsync(
        Guid versionId,
        Guid lawyerId,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(versionId, nameof(versionId));
        RepositoryGuards.EnsureNotEmpty(lawyerId, nameof(lawyerId));

        return await BuildLawyerQuery(lawyerId)
            .SingleOrDefaultAsync(version => version.Id == versionId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DocumentVersion>> GetHistoryForDraftForLawyerAsync(
        Guid draftId,
        Guid lawyerId,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(draftId, nameof(draftId));
        RepositoryGuards.EnsureNotEmpty(lawyerId, nameof(lawyerId));

        return await BuildLawyerQuery(lawyerId)
            .Where(version => version.DraftId == draftId)
            .OrderByDescending(version => version.VersionNumber)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentVersion?> GetCurrentForDraftForLawyerAsync(
        Guid draftId,
        Guid lawyerId,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(draftId, nameof(draftId));
        RepositoryGuards.EnsureNotEmpty(lawyerId, nameof(lawyerId));

        var query =
            from draft in _context.Drafts.AsNoTracking()
            join caseItem in _context.Cases.AsNoTracking()
                on draft.CaseId equals caseItem.Id
            join version in _context.DocumentVersions.AsNoTracking()
                on draft.CurrentVersionId equals (Guid?)version.Id
            where draft.Id == draftId && caseItem.LawyerId == lawyerId
            select version;

        return await query.SingleOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int?> GetNextVersionNumberForLawyerAsync(
        Guid draftId,
        Guid lawyerId,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(draftId, nameof(draftId));
        RepositoryGuards.EnsureNotEmpty(lawyerId, nameof(lawyerId));

        var isAccessible = await (
            from draft in _context.Drafts
            join caseItem in _context.Cases on draft.CaseId equals caseItem.Id
            where draft.Id == draftId && caseItem.LawyerId == lawyerId
            select draft.Id)
            .AnyAsync(cancellationToken);

        if (!isAccessible)
        {
            return null;
        }

        var maximumVersion = await _context.DocumentVersions
            .Where(version => version.DraftId == draftId)
            .Select(version => (int?)version.VersionNumber)
            .MaxAsync(cancellationToken);

        return (maximumVersion ?? 0) + 1;
    }

    /// <summary>
    /// Формирует tenant-безопасную выборку версий через Draft → Case без N+1.
    /// </summary>
    /// <param name="lawyerId">Идентификатор профиля владельца.</param>
    /// <returns>Неотслеживаемый запрос версий.</returns>
    private IQueryable<DocumentVersion> BuildLawyerQuery(Guid lawyerId)
    {
        return
            from version in _context.DocumentVersions.AsNoTracking()
            join draft in _context.Drafts.AsNoTracking()
                on version.DraftId equals draft.Id
            join caseItem in _context.Cases.AsNoTracking()
                on draft.CaseId equals caseItem.Id
            where caseItem.LawyerId == lawyerId
            select version;
    }
}
