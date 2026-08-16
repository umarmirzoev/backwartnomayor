using Application.Interfaces.Repositories;
using Domain.Common;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Реализует общий набор операций EF Core для доменных сущностей.
/// Чтение выполняется без отслеживания, а изменения только регистрируются в общем AppDbContext
/// и фиксируются через <see cref="IUnitOfWork"/>.
/// </summary>
/// <typeparam name="T">Тип доменной сущности.</typeparam>
public class BaseRepository<T> : IBaseRepository<T>
    where T : BaseEntity
{
    /// <summary>Контекст общей единицы работы.</summary>
    protected readonly AppDbContext Context;

    /// <summary>Набор EF Core для текущего типа сущности.</summary>
    protected readonly DbSet<T> Entities;

    /// <summary>
    /// Инициализирует репозиторий scoped-контекстом приложения.
    /// </summary>
    /// <param name="context">Контекст доменных данных и Identity.</param>
    protected BaseRepository(AppDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Context = context;
        Entities = context.Set<T>();
    }

    /// <inheritdoc />
    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(id, nameof(id));

        return await Entities
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await Entities
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(id, nameof(id));
        return await Entities.AnyAsync(entity => entity.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await Entities.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task AddRangeAsync(
        IReadOnlyCollection<T> entities,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entities);

        if (entities.Count == 0)
        {
            return;
        }

        await Entities.AddRangeAsync(entities, cancellationToken);
    }

    /// <inheritdoc />
    public virtual Task UpdateAsync(T entity, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(entity);
        Entities.Update(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual Task DeleteAsync(T entity, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(entity);
        Entities.Remove(entity);
        return Task.CompletedTask;
    }
}
