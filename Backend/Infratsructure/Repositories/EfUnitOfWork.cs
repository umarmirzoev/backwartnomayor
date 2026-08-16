using Application.Interfaces.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Реализует общую единицу работы и явные транзакции поверх AppDbContext.
/// Одна scoped-экземплярность контекста объединяет изменения всех репозиториев CQRS-сценария.
/// </summary>
public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Инициализирует единицу работы общим scoped-контекстом.
    /// </summary>
    /// <param name="context">Контекст доменных данных.</param>
    public EfUnitOfWork(AppDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await ExecuteInTransactionAsync(
            async token =>
            {
                await operation(token);
                return true;
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(cancellationToken, async token =>
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(token);

            try
            {
                var result = await operation(token);
                await transaction.CommitAsync(token);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
        });
    }
}
