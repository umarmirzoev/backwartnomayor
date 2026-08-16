using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Реализует хранение доменного профиля юриста и связь с Identity-пользователем.
/// </summary>
public sealed class LawyerProfileRepository : BaseRepository<LawyerProfile>, ILawyerProfileRepository
{
    /// <summary>
    /// Инициализирует репозиторий общим scoped-контекстом.
    /// </summary>
    /// <param name="context">Контекст данных приложения.</param>
    public LawyerProfileRepository(AppDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<LawyerProfile?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(userId, nameof(userId));

        return await Entities
            .AsNoTracking()
            .SingleOrDefaultAsync(profile => profile.UserId == userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(userId, nameof(userId));
        return await Entities.AnyAsync(profile => profile.UserId == userId, cancellationToken);
    }
}
