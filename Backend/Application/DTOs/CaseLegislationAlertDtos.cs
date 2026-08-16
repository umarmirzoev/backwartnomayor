namespace Application.DTOs;

/// <summary>
/// Представляет связь уведомления законодательства с затронутым делом и её состояние прочтения.
/// </summary>
/// <param name="Id">Идентификатор связи.</param>
/// <param name="CaseId">Идентификатор дела.</param>
/// <param name="LegislationAlertId">Идентификатор уведомления.</param>
/// <param name="IsRead">Признак прочтения юристом.</param>
/// <param name="ReadAt">Дата прочтения.</param>
public sealed record GetCaseLegislationAlertDto(
    Guid Id,
    Guid CaseId,
    Guid LegislationAlertId,
    bool IsRead,
    DateTimeOffset? ReadAt);

/// <summary>
/// Представляет детальное уведомление с привязанным делом для запроса непрочитанных изменений законодательства.
/// Вложенные модели формируются оптимизированной CQRS-проекцией, не навигациями доменной сущности.
/// </summary>
/// <param name="Id">Идентификатор связи.</param>
/// <param name="CaseId">Идентификатор дела.</param>
/// <param name="LegislationAlertId">Идентификатор уведомления.</param>
/// <param name="IsRead">Признак прочтения.</param>
/// <param name="ReadAt">Дата прочтения.</param>
/// <param name="Case">Краткая карточка затронутого дела.</param>
/// <param name="Alert">Краткая карточка изменения законодательства.</param>
public sealed record CaseLegislationAlertDetailDto(
    Guid Id,
    Guid CaseId,
    Guid LegislationAlertId,
    bool IsRead,
    DateTimeOffset? ReadAt,
    GetCaseDto? Case,
    GetLegislationAlertDto? Alert);

/// <summary>
/// Представляет данные для связывания обнаруженного уведомления с затронутым делом.
/// Начальное состояние прочтения всегда назначается доменной сущностью.
/// </summary>
/// <param name="CaseId">Идентификатор затронутого дела.</param>
/// <param name="LegislationAlertId">Идентификатор уведомления.</param>
public sealed record CreateCaseLegislationAlertDto(
    Guid CaseId,
    Guid LegislationAlertId);

/// <summary>
/// Маркер отсутствующего общего обновления связи уведомления с делом.
/// Прочтение выполняется односторонним доменным методом <c>MarkRead</c> с серверной временной меткой.
/// </summary>
public sealed record UpdateCaseLegislationAlertDto;
