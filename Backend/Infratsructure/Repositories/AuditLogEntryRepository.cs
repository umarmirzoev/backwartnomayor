using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Реализует append-only запись и хронологическое чтение журнала аудита.
/// </summary>
public sealed class AuditLogEntryRepository : IAuditLogEntryRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Инициализирует репозиторий общим scoped-контекстом.
    /// </summary>
    /// <param name="context">Контекст данных приложения.</param>
    public AuditLogEntryRepository(AppDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc />
    public async Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        await _context.AuditLogEntries.AddAsync(entry, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuditLogEntry>> GetByEntityAsync(
        string entityType,
        Guid entityId,
        AuditAction? action,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(entityId, nameof(entityId));
        RepositoryGuards.EnsurePagination(skip, take);

        var query = BuildEntityQuery(entityType, entityId);
        if (action.HasValue)
        {
            query = query.Where(entry => entry.Action == action.Value);
        }

        return await query
            .OrderByDescending(entry => entry.OccurredAt)
            .ThenByDescending(entry => entry.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountByEntityAsync(
        string entityType,
        Guid entityId,
        AuditAction? action,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(entityId, nameof(entityId));
        var query = BuildEntityQuery(entityType, entityId);
        if (action.HasValue)
        {
            query = query.Where(entry => entry.Action == action.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    /// <summary>
    /// Формирует запрос журнала для полиморфной пары типа и идентификатора сущности.
    /// </summary>
    /// <param name="entityType">Техническое имя типа сущности.</param>
    /// <param name="entityId">Идентификатор сущности.</param>
    /// <returns>Неотслеживаемый запрос событий.</returns>
    private IQueryable<AuditLogEntry> BuildEntityQuery(string entityType, Guid entityId)
    {
        var normalizedType = RepositoryGuards.NormalizeOptionalText(entityType)
            ?? throw new ArgumentException("Тип сущности аудита обязателен.", nameof(entityType));

        return _context.AuditLogEntries
            .AsNoTracking()
            .Where(entry => entry.EntityType == normalizedType && entry.EntityId == entityId);
    }
}
