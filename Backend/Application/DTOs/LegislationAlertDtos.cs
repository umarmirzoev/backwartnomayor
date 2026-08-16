namespace Application.DTOs;

/// <summary>
/// Представляет краткое уведомление об изменении законодательства для списочного ответа юристу.
/// </summary>
/// <param name="Id">Идентификатор уведомления.</param>
/// <param name="Title">Название изменения.</param>
/// <param name="Summary">Краткое объяснение влияния изменения.</param>
/// <param name="LawChangedAt">Дата вступления изменения в силу.</param>
/// <param name="DetectedAt">Дата обнаружения системой.</param>
public sealed record GetLegislationAlertDto(
    Guid Id,
    string Title,
    string Summary,
    DateTimeOffset? LawChangedAt,
    DateTimeOffset DetectedAt);

/// <summary>
/// Представляет полную карточку уведомления с проверенной ссылкой на первоисточник.
/// </summary>
/// <param name="Id">Идентификатор уведомления.</param>
/// <param name="Title">Название изменения.</param>
/// <param name="Summary">Краткое содержание.</param>
/// <param name="SourceUrl">Ссылка на источник законодательства.</param>
/// <param name="LawChangedAt">Дата вступления изменения в силу.</param>
/// <param name="DetectedAt">Дата обнаружения.</param>
public sealed record LegislationAlertDetailDto(
    Guid Id,
    string Title,
    string Summary,
    string? SourceUrl,
    DateTimeOffset? LawChangedAt,
    DateTimeOffset DetectedAt);

/// <summary>
/// Представляет проверенный результат фонового мониторинга для создания append-only уведомления.
/// Время обнаружения назначается серверной фоновой задачей и не принимается от внешнего клиента.
/// </summary>
/// <param name="Title">Название изменения.</param>
/// <param name="Summary">Краткое содержание и ожидаемое влияние.</param>
/// <param name="SourceUrl">Ссылка на официальный источник.</param>
/// <param name="LawChangedAt">Дата вступления изменения в силу.</param>
public sealed record CreateLegislationAlertDto(
    string Title,
    string Summary,
    string? SourceUrl,
    DateTimeOffset? LawChangedAt);

/// <summary>
/// Маркер отсутствующего сценария редактирования уведомления.
/// Уведомления являются append-only результатами мониторинга; исправление создаёт новую запись.
/// </summary>
public sealed record UpdateLegislationAlertDto;
