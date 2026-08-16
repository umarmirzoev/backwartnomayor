using Domain.Entities;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Определяет операции хранения доменного профиля юриста, связанного с ASP.NET Core Identity.
/// </summary>
public interface ILawyerProfileRepository : IBaseRepository<LawyerProfile>
{
    /// <summary>
    /// Возвращает профиль по идентификатору инфраструктурной учётной записи Identity.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя Identity.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Профиль юриста или <see langword="null"/>.</returns>
    Task<LawyerProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Проверяет наличие доменного профиля для учётной записи Identity.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя Identity.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns><see langword="true"/>, если профиль уже создан.</returns>
    Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken cancellationToken);
}
