namespace Application.DTOs;

/// <summary>
/// Представляет краткую модель дела для списков клиента и текущего юриста.
/// </summary>
/// <param name="Id">Идентификатор дела.</param>
/// <param name="ClientId">Идентификатор клиента дела.</param>
/// <param name="Title">Название дела.</param>
/// <param name="Status">Строковое имя состояния дела.</param>
/// <param name="CreatedAt">Дата создания дела.</param>
/// <param name="ClosedAt">Дата закрытия дела.</param>
public sealed record GetCaseDto(
    Guid Id,
    Guid ClientId,
    string Title,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ClosedAt);

/// <summary>
/// Представляет полную карточку дела со сводкой по связанным документам,
/// требуемой запросом <c>GetCaseByIdQuery</c> без раскрытия инфраструктурных деталей хранения.
/// </summary>
/// <param name="Id">Идентификатор дела.</param>
/// <param name="ClientId">Идентификатор клиента.</param>
/// <param name="Title">Название дела.</param>
/// <param name="Description">Описание обстоятельств дела.</param>
/// <param name="Status">Строковое имя состояния.</param>
/// <param name="DocumentCount">Количество связанных документов.</param>
/// <param name="CreatedAt">Дата создания.</param>
/// <param name="ClosedAt">Дата закрытия.</param>
public sealed record CaseDetailDto(
    Guid Id,
    Guid ClientId,
    string Title,
    string? Description,
    string Status,
    int DocumentCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ClosedAt);

/// <summary>
/// Представляет данные для создания открытого дела у принадлежащего текущему юристу клиента.
/// Владелец и начальное состояние назначаются сервером после проверки доступа к клиенту.
/// </summary>
/// <param name="ClientId">Идентификатор клиента.</param>
/// <param name="Title">Название нового дела.</param>
/// <param name="Description">Необязательное описание обстоятельств.</param>
public sealed record CreateCaseDto(
    Guid ClientId,
    string Title,
    string? Description);

/// <summary>
/// Представляет редактируемые сведения дела.
/// Закрытие дела вынесено в отдельную доменную операцию и не допускается через общий маппинг.
/// </summary>
/// <param name="Title">Новое название дела.</param>
/// <param name="Description">Новое описание обстоятельств.</param>
public sealed record UpdateCaseDto(
    string Title,
    string? Description);
