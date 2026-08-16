using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Реализует tenant-безопасное хранение и поиск карточек клиентов.
/// </summary>
public sealed class ClientRepository : BaseRepository<Client>, IClientRepository
{
    /// <summary>
    /// Инициализирует репозиторий общим scoped-контекстом.
    /// </summary>
    /// <param name="context">Контекст данных приложения.</param>
    public ClientRepository(AppDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<Client?> GetByIdForLawyerAsync(
        Guid clientId,
        Guid lawyerId,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(clientId, nameof(clientId));
        RepositoryGuards.EnsureNotEmpty(lawyerId, nameof(lawyerId));

        return await Entities
            .AsNoTracking()
            .SingleOrDefaultAsync(
                client => client.Id == clientId &&
                          client.LawyerId == lawyerId &&
                          client.DeletedAt == null,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsForLawyerAsync(
        Guid clientId,
        Guid lawyerId,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(clientId, nameof(clientId));
        RepositoryGuards.EnsureNotEmpty(lawyerId, nameof(lawyerId));

        return await Entities.AnyAsync(
            client => client.Id == clientId &&
                      client.LawyerId == lawyerId &&
                      client.DeletedAt == null,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Client>> GetPageByLawyerAsync(
        Guid lawyerId,
        string? searchTerm,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(lawyerId, nameof(lawyerId));
        RepositoryGuards.EnsurePagination(skip, take);

        return await BuildFilteredQuery(lawyerId, searchTerm)
            .OrderByDescending(client => client.CreatedAt)
            .ThenBy(client => client.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountByLawyerAsync(
        Guid lawyerId,
        string? searchTerm,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(lawyerId, nameof(lawyerId));
        return await BuildFilteredQuery(lawyerId, searchTerm).CountAsync(cancellationToken);
    }

    /// <summary>
    /// Формирует единый фильтр владельца, активности и поиска для списка и счётчика.
    /// </summary>
    /// <param name="lawyerId">Идентификатор профиля владельца.</param>
    /// <param name="searchTerm">Необязательная поисковая строка.</param>
    /// <returns>Неотслеживаемый запрос клиентов.</returns>
    private IQueryable<Client> BuildFilteredQuery(Guid lawyerId, string? searchTerm)
    {
        var query = Entities
            .AsNoTracking()
            .Where(client => client.LawyerId == lawyerId && client.DeletedAt == null);

        var normalizedSearch = RepositoryGuards.NormalizeOptionalText(searchTerm);
        if (normalizedSearch is null)
        {
            return query;
        }

        return query.Where(client =>
            (client.FullName != null && client.FullName.Contains(normalizedSearch)) ||
            (client.CompanyName != null && client.CompanyName.Contains(normalizedSearch)) ||
            (client.ContactEmail != null && client.ContactEmail.Contains(normalizedSearch)) ||
            (client.ContactPhone != null && client.ContactPhone.Contains(normalizedSearch)));
    }
}
