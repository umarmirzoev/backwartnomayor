using Application.Interfaces.Repositories;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using CaseEntity = Domain.Entities.Case;

namespace Infrastructure.Repositories;

/// <summary>
/// Реализует tenant-безопасное хранение дел и выборки для карточек клиентов.
/// </summary>
public sealed class CaseRepository : BaseRepository<CaseEntity>, ICaseRepository
{
    /// <summary>
    /// Инициализирует репозиторий общим scoped-контекстом.
    /// </summary>
    /// <param name="context">Контекст данных приложения.</param>
    public CaseRepository(AppDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<CaseEntity?> GetByIdForLawyerAsync(
        Guid caseId,
        Guid lawyerId,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(caseId, nameof(caseId));
        RepositoryGuards.EnsureNotEmpty(lawyerId, nameof(lawyerId));

        return await Entities
            .AsNoTracking()
            .SingleOrDefaultAsync(
                caseItem => caseItem.Id == caseId && caseItem.LawyerId == lawyerId,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsForLawyerAsync(
        Guid caseId,
        Guid lawyerId,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(caseId, nameof(caseId));
        RepositoryGuards.EnsureNotEmpty(lawyerId, nameof(lawyerId));

        return await Entities.AnyAsync(
            caseItem => caseItem.Id == caseId && caseItem.LawyerId == lawyerId,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CaseEntity>> GetByClientForLawyerAsync(
        Guid clientId,
        Guid lawyerId,
        CaseStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(clientId, nameof(clientId));
        RepositoryGuards.EnsureNotEmpty(lawyerId, nameof(lawyerId));
        RepositoryGuards.EnsurePagination(skip, take);

        return await BuildClientQuery(clientId, lawyerId, status)
            .OrderByDescending(caseItem => caseItem.CreatedAt)
            .ThenBy(caseItem => caseItem.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountByClientForLawyerAsync(
        Guid clientId,
        Guid lawyerId,
        CaseStatus? status,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(clientId, nameof(clientId));
        RepositoryGuards.EnsureNotEmpty(lawyerId, nameof(lawyerId));

        return await BuildClientQuery(clientId, lawyerId, status).CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CaseEntity>> GetOpenByLawyerAsync(
        Guid lawyerId,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(lawyerId, nameof(lawyerId));

        return await Entities
            .AsNoTracking()
            .Where(caseItem =>
                caseItem.LawyerId == lawyerId && caseItem.Status == CaseStatus.Open)
            .OrderBy(caseItem => caseItem.Id)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CaseEntity>> GetByIdsForSystemAsync(
        IReadOnlyCollection<Guid> caseIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(caseIds);
        if (caseIds.Count == 0 || caseIds.Any(caseId => caseId == Guid.Empty))
        {
            throw new ArgumentException("Набор идентификаторов дел должен содержать непустые значения.", nameof(caseIds));
        }

        return await Entities
            .AsNoTracking()
            .Where(caseItem => caseIds.Contains(caseItem.Id))
            .OrderBy(caseItem => caseItem.Id)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Формирует единый tenant-фильтр дел клиента для списка и счётчика.
    /// </summary>
    /// <param name="clientId">Идентификатор клиента.</param>
    /// <param name="lawyerId">Идентификатор профиля владельца.</param>
    /// <param name="status">Необязательный фильтр состояния.</param>
    /// <returns>Неотслеживаемый запрос дел.</returns>
    private IQueryable<CaseEntity> BuildClientQuery(
        Guid clientId,
        Guid lawyerId,
        CaseStatus? status)
    {
        var query = Entities
            .AsNoTracking()
            .Where(caseItem =>
                caseItem.ClientId == clientId && caseItem.LawyerId == lawyerId);

        return status.HasValue
            ? query.Where(caseItem => caseItem.Status == status.Value)
            : query;
    }
}
