using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Реализует хранение состава шаблонов и SQL-выборку пунктов без N+1.
/// </summary>
public sealed class TemplateClauseBlockRepository
    : BaseRepository<TemplateClauseBlock>, ITemplateClauseBlockRepository
{
    /// <summary>
    /// Инициализирует репозиторий общим scoped-контекстом.
    /// </summary>
    /// <param name="context">Контекст данных приложения.</param>
    public TemplateClauseBlockRepository(AppDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TemplateClauseBlock>> GetByTemplateAsync(
        Guid templateId,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(templateId, nameof(templateId));

        return await Entities
            .AsNoTracking()
            .Where(link => link.TemplateId == templateId)
            .OrderBy(link => link.Order)
            .ThenBy(link => link.Id)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TemplateClauseBlock?> GetByTemplateAndClauseBlockAsync(
        Guid templateId,
        Guid clauseBlockId,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(templateId, nameof(templateId));
        RepositoryGuards.EnsureNotEmpty(clauseBlockId, nameof(clauseBlockId));

        return await Entities
            .AsNoTracking()
            .SingleOrDefaultAsync(
                link => link.TemplateId == templateId && link.ClauseBlockId == clauseBlockId,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(
        Guid templateId,
        Guid clauseBlockId,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(templateId, nameof(templateId));
        RepositoryGuards.EnsureNotEmpty(clauseBlockId, nameof(clauseBlockId));

        return await Entities.AnyAsync(
            link => link.TemplateId == templateId && link.ClauseBlockId == clauseBlockId,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsOrderOccupiedAsync(
        Guid templateId,
        int order,
        Guid? excludedLinkId,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(templateId, nameof(templateId));

        if (order < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(order),
                order,
                "Позиция пункта не может быть отрицательной.");
        }

        return await Entities.AnyAsync(
            link => link.TemplateId == templateId &&
                    link.Order == order &&
                    (!excludedLinkId.HasValue || link.Id != excludedLinkId.Value),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ClauseBlock>> GetClauseBlocksByTemplateAsync(
        Guid templateId,
        bool defaultOnly,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(templateId, nameof(templateId));

        var query =
            from link in Context.TemplateClauseBlocks.AsNoTracking()
            join block in Context.ClauseBlocks.AsNoTracking()
                on link.ClauseBlockId equals block.Id
            where link.TemplateId == templateId &&
                  block.IsActive &&
                  (!defaultOnly || link.IsDefault)
            orderby link.Order, link.Id
            select block;

        return await query.ToListAsync(cancellationToken);
    }
}
