using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Реализует хранение и активные выборки каталога шаблонов договоров.
/// </summary>
public sealed class TemplateRepository : BaseRepository<Template>, ITemplateRepository
{
    /// <summary>
    /// Инициализирует репозиторий общим scoped-контекстом.
    /// </summary>
    /// <param name="context">Контекст данных приложения.</param>
    public TemplateRepository(AppDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<Template?> GetActiveByIdAsync(
        Guid templateId,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(templateId, nameof(templateId));

        return await Entities
            .AsNoTracking()
            .SingleOrDefaultAsync(
                template => template.Id == templateId && template.IsActive,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Template>> GetActivePageAsync(
        TemplateLanguage? language,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsurePagination(skip, take);

        return await BuildActiveQuery(language)
            .OrderBy(template => template.Name)
            .ThenBy(template => template.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountActiveAsync(
        TemplateLanguage? language,
        CancellationToken cancellationToken)
    {
        return await BuildActiveQuery(language).CountAsync(cancellationToken);
    }

    /// <summary>
    /// Формирует единый фильтр активных шаблонов по языку.
    /// </summary>
    /// <param name="language">Язык или отсутствие фильтра.</param>
    /// <returns>Неотслеживаемый запрос шаблонов.</returns>
    private IQueryable<Template> BuildActiveQuery(TemplateLanguage? language)
    {
        var query = Entities.AsNoTracking().Where(template => template.IsActive);
        return language.HasValue
            ? query.Where(template =>
                template.Language == language.Value || template.Language == TemplateLanguage.Both)
            : query;
    }
}
