using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Реализует поиск по библиотеке активных двуязычных блоков договорных пунктов.
/// </summary>
public sealed class ClauseBlockRepository : BaseRepository<ClauseBlock>, IClauseBlockRepository
{
    /// <summary>
    /// Инициализирует репозиторий общим scoped-контекстом.
    /// </summary>
    /// <param name="context">Контекст данных приложения.</param>
    public ClauseBlockRepository(AppDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<ClauseBlock?> GetActiveByIdAsync(
        Guid clauseBlockId,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(clauseBlockId, nameof(clauseBlockId));

        return await Entities
            .AsNoTracking()
            .SingleOrDefaultAsync(
                block => block.Id == clauseBlockId && block.IsActive,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ClauseBlock>> SearchActiveAsync(
        string? searchTerm,
        string? category,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsurePagination(skip, take);

        return await BuildSearchQuery(searchTerm, category)
            .OrderBy(block => block.Category)
            .ThenBy(block => block.Title)
            .ThenBy(block => block.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountActiveAsync(
        string? searchTerm,
        string? category,
        CancellationToken cancellationToken)
    {
        return await BuildSearchQuery(searchTerm, category).CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ClauseBlock>> GetActiveByIdsAsync(
        IReadOnlyCollection<Guid> clauseBlockIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(clauseBlockIds);

        if (clauseBlockIds.Count == 0)
        {
            return [];
        }

        return await Entities
            .AsNoTracking()
            .Where(block => block.IsActive && clauseBlockIds.Contains(block.Id))
            .OrderBy(block => block.Id)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Формирует единый фильтр активных блоков для страницы и счётчика.
    /// </summary>
    /// <param name="searchTerm">Необязательная поисковая строка.</param>
    /// <param name="category">Необязательная точная категория.</param>
    /// <returns>Неотслеживаемый запрос блоков.</returns>
    private IQueryable<ClauseBlock> BuildSearchQuery(string? searchTerm, string? category)
    {
        var query = Entities.AsNoTracking().Where(block => block.IsActive);
        var normalizedCategory = RepositoryGuards.NormalizeOptionalText(category);
        var normalizedSearch = RepositoryGuards.NormalizeOptionalText(searchTerm);

        if (normalizedCategory is not null)
        {
            query = query.Where(block => block.Category == normalizedCategory);
        }

        if (normalizedSearch is not null)
        {
            query = query.Where(block =>
                block.Title.Contains(normalizedSearch) ||
                block.Category.Contains(normalizedSearch) ||
                block.ContentTj.Contains(normalizedSearch) ||
                block.ContentRu.Contains(normalizedSearch));
        }

        return query;
    }
}
