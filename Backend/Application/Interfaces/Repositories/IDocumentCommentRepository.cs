using Domain.Entities;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Определяет Post-MVP хранение комментариев к неизменяемым версиям документов.
/// </summary>
public interface IDocumentCommentRepository : IBaseRepository<DocumentComment>
{
    /// <summary>
    /// Возвращает комментарий только при принадлежности версии указанному юристу.
    /// </summary>
    /// <param name="commentId">Идентификатор комментария.</param>
    /// <param name="lawyerId">Идентификатор профиля владельца дела.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Комментарий или <see langword="null"/> при отсутствии доступа.</returns>
    Task<DocumentComment?> GetByIdForLawyerAsync(
        Guid commentId,
        Guid lawyerId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Возвращает страницу комментариев версии после проверки владельца документа.
    /// </summary>
    /// <param name="documentVersionId">Идентификатор версии.</param>
    /// <param name="lawyerId">Идентификатор профиля владельца дела.</param>
    /// <param name="includeResolved">Признак включения разрешённых комментариев.</param>
    /// <param name="skip">Количество пропускаемых строк.</param>
    /// <param name="take">Максимальное количество строк.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Страница комментариев.</returns>
    Task<IReadOnlyList<DocumentComment>> GetByVersionForLawyerAsync(
        Guid documentVersionId,
        Guid lawyerId,
        bool includeResolved,
        int skip,
        int take,
        CancellationToken cancellationToken);

    /// <summary>
    /// Подсчитывает комментарии версии с учётом владельца и состояния разрешения.
    /// </summary>
    /// <param name="documentVersionId">Идентификатор версии.</param>
    /// <param name="lawyerId">Идентификатор профиля владельца дела.</param>
    /// <param name="includeResolved">Признак включения разрешённых комментариев.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Количество доступных комментариев.</returns>
    Task<int> CountByVersionForLawyerAsync(
        Guid documentVersionId,
        Guid lawyerId,
        bool includeResolved,
        CancellationToken cancellationToken);

    /// <summary>
    /// Возвращает страницу комментариев версии после отдельной успешной ресурсной авторизации Post-MVP стороны.
    /// Обработчик обязан вызвать <c>IResourceAuthorizationService</c> до этого метода.
    /// </summary>
    Task<IReadOnlyList<DocumentComment>> GetByVersionAsync(
        Guid documentVersionId,
        bool includeResolved,
        int skip,
        int take,
        CancellationToken cancellationToken);

    /// <summary>Подсчитывает комментарии версии после отдельной успешной ресурсной авторизации стороны.</summary>
    Task<int> CountByVersionAsync(
        Guid documentVersionId,
        bool includeResolved,
        CancellationToken cancellationToken);
}
