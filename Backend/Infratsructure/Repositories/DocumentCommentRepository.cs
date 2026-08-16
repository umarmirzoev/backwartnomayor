using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Реализует Post-MVP хранение комментариев с проверкой владельца через Version → Draft → Case.
/// </summary>
public sealed class DocumentCommentRepository
    : BaseRepository<DocumentComment>, IDocumentCommentRepository
{
    /// <summary>
    /// Инициализирует репозиторий общим scoped-контекстом.
    /// </summary>
    /// <param name="context">Контекст данных приложения.</param>
    public DocumentCommentRepository(AppDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<DocumentComment?> GetByIdForLawyerAsync(
        Guid commentId,
        Guid lawyerId,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(commentId, nameof(commentId));
        RepositoryGuards.EnsureNotEmpty(lawyerId, nameof(lawyerId));

        return await BuildLawyerQuery(lawyerId)
            .SingleOrDefaultAsync(comment => comment.Id == commentId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DocumentComment>> GetByVersionForLawyerAsync(
        Guid documentVersionId,
        Guid lawyerId,
        bool includeResolved,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(documentVersionId, nameof(documentVersionId));
        RepositoryGuards.EnsureNotEmpty(lawyerId, nameof(lawyerId));
        RepositoryGuards.EnsurePagination(skip, take);

        return await BuildVersionQuery(documentVersionId, lawyerId, includeResolved)
            .OrderBy(comment => comment.CreatedAt)
            .ThenBy(comment => comment.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountByVersionForLawyerAsync(
        Guid documentVersionId,
        Guid lawyerId,
        bool includeResolved,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(documentVersionId, nameof(documentVersionId));
        RepositoryGuards.EnsureNotEmpty(lawyerId, nameof(lawyerId));

        return await BuildVersionQuery(documentVersionId, lawyerId, includeResolved)
            .CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DocumentComment>> GetByVersionAsync(
        Guid documentVersionId,
        bool includeResolved,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(documentVersionId, nameof(documentVersionId));
        RepositoryGuards.EnsurePagination(skip, take);
        return await BuildAuthorizedVersionQuery(documentVersionId, includeResolved)
            .OrderBy(comment => comment.CreatedAt)
            .ThenBy(comment => comment.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountByVersionAsync(
        Guid documentVersionId,
        bool includeResolved,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(documentVersionId, nameof(documentVersionId));
        return await BuildAuthorizedVersionQuery(documentVersionId, includeResolved)
            .CountAsync(cancellationToken);
    }

    /// <summary>
    /// Формирует tenant-безопасную выборку комментариев через связанные таблицы.
    /// </summary>
    /// <param name="lawyerId">Идентификатор профиля владельца дела.</param>
    /// <returns>Неотслеживаемый запрос комментариев.</returns>
    private IQueryable<DocumentComment> BuildLawyerQuery(Guid lawyerId)
    {
        return
            from comment in Context.DocumentComments.AsNoTracking()
            join version in Context.DocumentVersions.AsNoTracking()
                on comment.DocumentVersionId equals version.Id
            join draft in Context.Drafts.AsNoTracking()
                on version.DraftId equals draft.Id
            join caseItem in Context.Cases.AsNoTracking()
                on draft.CaseId equals caseItem.Id
            where caseItem.LawyerId == lawyerId
            select comment;
    }

    /// <summary>
    /// Дополняет tenant-фильтр идентификатором версии и состоянием разрешения.
    /// </summary>
    /// <param name="documentVersionId">Идентификатор версии.</param>
    /// <param name="lawyerId">Идентификатор профиля владельца дела.</param>
    /// <param name="includeResolved">Признак включения разрешённых комментариев.</param>
    /// <returns>Запрос комментариев версии.</returns>
    private IQueryable<DocumentComment> BuildVersionQuery(
        Guid documentVersionId,
        Guid lawyerId,
        bool includeResolved)
    {
        var query = BuildLawyerQuery(lawyerId)
            .Where(comment => comment.DocumentVersionId == documentVersionId);

        return includeResolved
            ? query
            : query.Where(comment => comment.ResolvedAt == null);
    }

    /// <summary>Формирует выборку версии, доступ к которой уже подтверждён прикладной политикой.</summary>
    private IQueryable<DocumentComment> BuildAuthorizedVersionQuery(
        Guid documentVersionId,
        bool includeResolved)
    {
        var query = Context.DocumentComments
            .AsNoTracking()
            .Where(comment => comment.DocumentVersionId == documentVersionId);
        return includeResolved
            ? query
            : query.Where(comment => comment.ResolvedAt == null);
    }
}
