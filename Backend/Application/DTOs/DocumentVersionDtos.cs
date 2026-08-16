using Domain.Enums;

namespace Application.DTOs;

/// <summary>
/// Представляет метаданные immutable-версии в журнале изменений без полного текста и внутреннего ключа хранилища.
/// </summary>
/// <param name="Id">Идентификатор версии.</param>
/// <param name="DraftId">Идентификатор черновика.</param>
/// <param name="VersionNumber">Последовательный номер версии.</param>
/// <param name="ChangeSummary">Описание отличий от предыдущей версии.</param>
/// <param name="Source">Строковое имя источника версии.</param>
/// <param name="CreatedByLawyerId">Идентификатор создавшего версию юриста.</param>
/// <param name="CreatedAt">Дата создания версии.</param>
public sealed record GetDocumentVersionDto(
    Guid Id,
    Guid DraftId,
    int VersionNumber,
    string? ChangeSummary,
    string Source,
    Guid CreatedByLawyerId,
    DateTimeOffset CreatedAt);

/// <summary>
/// Представляет полную версию документа с расшифрованным содержимым.
/// Ключ объектного хранилища остаётся внутренней инфраструктурной деталью и не включается в API-контракт.
/// </summary>
/// <param name="Id">Идентификатор версии.</param>
/// <param name="DraftId">Идентификатор черновика.</param>
/// <param name="VersionNumber">Последовательный номер версии.</param>
/// <param name="Content">Текст, загруженный из объектного хранилища.</param>
/// <param name="ChangeSummary">Описание изменений.</param>
/// <param name="Source">Строковое имя источника.</param>
/// <param name="CreatedByLawyerId">Идентификатор автора.</param>
/// <param name="CreatedAt">Дата создания.</param>
public sealed record DocumentVersionDetailDto(
    Guid Id,
    Guid DraftId,
    int VersionNumber,
    string? Content,
    string? ChangeSummary,
    string Source,
    Guid CreatedByLawyerId,
    DateTimeOffset CreatedAt);

/// <summary>
/// Представляет данные для создания новой версии через агрегат черновика.
/// Номер версии, автор, ключ хранилища и время назначаются доверенными серверными компонентами.
/// </summary>
/// <param name="DraftId">Идентификатор черновика.</param>
/// <param name="Content">Полный текст новой версии до шифрования и загрузки в хранилище.</param>
/// <param name="ChangeSummary">Описание изменений относительно предыдущей версии.</param>
/// <param name="Source">Источник появления версии.</param>
public sealed record CreateDocumentVersionDto(
    Guid DraftId,
    string Content,
    string? ChangeSummary,
    DocumentVersionSource Source);

/// <summary>
/// Маркер отсутствующего сценария обновления версии документа.
/// Тип существует для полного контрактного покрытия сущностей, но намеренно не содержит полей:
/// спецификация требует append-only хранение, поэтому изменение существующей версии запрещено.
/// </summary>
public sealed record UpdateDocumentVersionDto;
