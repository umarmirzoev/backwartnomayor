using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Реализует хранение workflow полного удаления и идемпотентный поиск ожидающего запроса.
/// </summary>
public sealed class DataDeletionRequestRepository
    : BaseRepository<DataDeletionRequest>, IDataDeletionRequestRepository
{
    /// <summary>
    /// Инициализирует репозиторий общим scoped-контекстом.
    /// </summary>
    /// <param name="context">Контекст данных приложения.</param>
    public DataDeletionRequestRepository(AppDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<DataDeletionRequest?> GetPendingByTargetAsync(
        DeletionTargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(targetId, nameof(targetId));

        return await Entities
            .AsNoTracking()
            .OrderBy(request => request.RequestedAt)
            .FirstOrDefaultAsync(
                request => request.TargetEntityType == targetType &&
                           request.TargetEntityId == targetId &&
                           request.Status == DataDeletionStatus.Pending,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsPendingForTargetAsync(
        DeletionTargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(targetId, nameof(targetId));

        return await Entities.AnyAsync(
            request => request.TargetEntityType == targetType &&
                       request.TargetEntityId == targetId &&
                       request.Status == DataDeletionStatus.Pending,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DataDeletionRequest>> GetPendingBatchAsync(
        int batchSize,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsurePositive(batchSize, nameof(batchSize));

        return await Entities
            .AsNoTracking()
            .Where(request => request.Status == DataDeletionStatus.Pending)
            .OrderBy(request => request.RequestedAt)
            .ThenBy(request => request.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }
}
