namespace Application.DTOs;

/// <summary>
/// Представляет документ в списке дела с требуемыми спецификацией типом, состоянием и датой.
/// Название шаблона заполняется проекцией обработчика и не хранится в агрегате черновика.
/// </summary>
/// <param name="Id">Идентификатор черновика.</param>
/// <param name="CaseId">Идентификатор дела.</param>
/// <param name="TemplateId">Идентификатор шаблона.</param>
/// <param name="TemplateName">Человекочитаемый тип документа.</param>
/// <param name="Status">Строковое имя состояния документа.</param>
/// <param name="CurrentVersionId">Идентификатор текущей версии.</param>
/// <param name="CreatedAt">Дата создания документа.</param>
/// <param name="UpdatedAt">Дата последнего изменения.</param>
public sealed record GetDraftDto(
    Guid Id,
    Guid CaseId,
    Guid TemplateId,
    string? TemplateName,
    string Status,
    Guid? CurrentVersionId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Представляет полную карточку черновика вместе с метаданными и расшифрованным текстом текущей версии.
/// Содержимое загружается обработчиком из объектного хранилища, поэтому ключ S3 не раскрывается клиенту.
/// </summary>
/// <param name="Id">Идентификатор черновика.</param>
/// <param name="CaseId">Идентификатор дела.</param>
/// <param name="TemplateId">Идентификатор шаблона.</param>
/// <param name="Status">Строковое имя состояния.</param>
/// <param name="CurrentVersionId">Идентификатор текущей версии.</param>
/// <param name="ResponsibilityConfirmedAt">Дата подтверждения ответственности перед экспортом.</param>
/// <param name="DueRespondByDate">Крайний срок ответа клиента.</param>
/// <param name="ArchivedAt">Дата архивирования.</param>
/// <param name="CreatedAt">Дата создания.</param>
/// <param name="UpdatedAt">Дата последнего изменения.</param>
/// <param name="CurrentVersion">Метаданные текущей версии.</param>
/// <param name="CurrentContent">Расшифрованный текст текущей версии.</param>
public sealed record DraftDetailDto(
    Guid Id,
    Guid CaseId,
    Guid TemplateId,
    string Status,
    Guid? CurrentVersionId,
    DateTimeOffset? ResponsibilityConfirmedAt,
    DateTimeOffset? DueRespondByDate,
    DateTimeOffset? ArchivedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    GetDocumentVersionDto? CurrentVersion,
    string? CurrentContent);

/// <summary>
/// Представляет входные данные сценария генерации первого черновика.
/// Описание сделки используется ИИ-сервисом для выбора пунктов из библиотеки и не сохраняется полем агрегата.
/// </summary>
/// <param name="CaseId">Идентификатор принадлежащего юристу дела.</param>
/// <param name="TemplateId">Идентификатор активного ненотариального шаблона.</param>
/// <param name="DealDescription">Описание сделки на естественном языке.</param>
public sealed record CreateDraftDto(
    Guid CaseId,
    Guid TemplateId,
    string DealDescription);

/// <summary>
/// Представляет ручную правку текста черновика.
/// Обновление не изменяет существующую версию, а создаёт новый immutable-снимок через доменный агрегат.
/// </summary>
/// <param name="Content">Полный новый текст документа.</param>
/// <param name="ChangeSummary">Краткое описание внесённых изменений.</param>
public sealed record UpdateDraftDto(
    string Content,
    string? ChangeSummary);
