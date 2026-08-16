namespace Application.DTOs;

/// <summary>
/// Представляет Post-MVP комментарий стороны к конкретной версии или пункту документа.
/// </summary>
/// <param name="Id">Идентификатор комментария.</param>
/// <param name="AuthorType">Строковое имя типа автора.</param>
/// <param name="AuthorId">Идентификатор автора.</param>
/// <param name="ClauseBlockReference">Идентификатор обсуждаемого пункта.</param>
/// <param name="Text">Текст комментария.</param>
/// <param name="CreatedAt">Дата создания.</param>
/// <param name="ResolvedAt">Дата разрешения замечания.</param>
public sealed record GetDocumentCommentDto(
    Guid Id,
    string AuthorType,
    Guid AuthorId,
    Guid? ClauseBlockReference,
    string Text,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt);

/// <summary>
/// Представляет полную карточку комментария с явной привязкой к immutable-версии документа.
/// </summary>
/// <param name="Id">Идентификатор комментария.</param>
/// <param name="DocumentVersionId">Идентификатор версии документа.</param>
/// <param name="AuthorType">Строковое имя типа автора.</param>
/// <param name="AuthorId">Идентификатор автора.</param>
/// <param name="ClauseBlockReference">Идентификатор обсуждаемого пункта.</param>
/// <param name="Text">Текст комментария.</param>
/// <param name="CreatedAt">Дата создания.</param>
/// <param name="ResolvedAt">Дата разрешения.</param>
public sealed record DocumentCommentDetailDto(
    Guid Id,
    Guid DocumentVersionId,
    string AuthorType,
    Guid AuthorId,
    Guid? ClauseBlockReference,
    string Text,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt);

/// <summary>
/// Представляет данные для добавления комментария к принадлежащей субъекту версии документа.
/// Тип и идентификатор автора берутся из доверенного контекста аутентификации.
/// </summary>
/// <param name="DocumentVersionId">Идентификатор комментируемой версии.</param>
/// <param name="ClauseBlockReference">Необязательная ссылка на конкретный пункт.</param>
/// <param name="Text">Текст замечания.</param>
public sealed record CreateDocumentCommentDto(
    Guid DocumentVersionId,
    Guid? ClauseBlockReference,
    string Text);

/// <summary>
/// Маркер отсутствующего общего обновления комментария.
/// Спецификация разрешает только одностороннее разрешение замечания отдельной командой с серверной датой.
/// </summary>
public sealed record UpdateDocumentCommentDto;
