using Domain.Entities;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Определяет безопасные выборки клиентов с обязательной проверкой владельца-юриста.
/// </summary>
public interface IClientRepository : IBaseRepository<Client>
{
    /// <summary>
    /// Возвращает активную карточку клиента только при её принадлежности указанному юристу.
    /// </summary>
    /// <param name="clientId">Идентификатор клиента.</param>
    /// <param name="lawyerId">Идентификатор профиля владельца.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Карточка клиента или <see langword="null"/> при отсутствии доступа.</returns>
    Task<Client?> GetByIdForLawyerAsync(
        Guid clientId,
        Guid lawyerId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Проверяет существование активного клиента у конкретного юриста.
    /// </summary>
    /// <param name="clientId">Идентификатор клиента.</param>
    /// <param name="lawyerId">Идентификатор профиля владельца.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns><see langword="true"/>, если клиент существует и принадлежит юристу.</returns>
    Task<bool> ExistsForLawyerAsync(
        Guid clientId,
        Guid lawyerId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Возвращает страницу активных клиентов юриста с поиском по имени, компании и контактам.
    /// </summary>
    /// <param name="lawyerId">Идентификатор профиля владельца.</param>
    /// <param name="searchTerm">Необязательная поисковая строка.</param>
    /// <param name="skip">Количество пропускаемых строк.</param>
    /// <param name="take">Максимальное количество возвращаемых строк.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Упорядоченная страница клиентов без отслеживания.</returns>
    Task<IReadOnlyList<Client>> GetPageByLawyerAsync(
        Guid lawyerId,
        string? searchTerm,
        int skip,
        int take,
        CancellationToken cancellationToken);

    /// <summary>
    /// Подсчитывает активных клиентов юриста с учётом поискового фильтра.
    /// </summary>
    /// <param name="lawyerId">Идентификатор профиля владельца.</param>
    /// <param name="searchTerm">Необязательная поисковая строка.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Количество строк для пагинации.</returns>
    Task<int> CountByLawyerAsync(
        Guid lawyerId,
        string? searchTerm,
        CancellationToken cancellationToken);
}
