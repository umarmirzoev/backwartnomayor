using Domain.Enums;

namespace Application.DTOs;

/// <summary>
/// Представляет безопасную строку истории действий по конкретному документу или делу.
/// Метаданные исключены из краткой модели, чтобы не раскрывать лишние персональные данные.
/// </summary>
/// <param name="Id">Идентификатор записи аудита.</param>
/// <param name="ActorType">Строковое имя типа инициатора.</param>
/// <param name="Action">Строковое имя действия.</param>
/// <param name="EntityType">Технический тип затронутой сущности.</param>
/// <param name="EntityId">Идентификатор затронутой сущности.</param>
/// <param name="OccurredAt">Дата события.</param>
public sealed record GetAuditLogEntryDto(
    Guid Id,
    string ActorType,
    string Action,
    string EntityType,
    Guid EntityId,
    DateTimeOffset OccurredAt);

/// <summary>
/// Представляет полную запись аудита для авторизованного просмотра владельцем объекта или системного расследования.
/// </summary>
/// <param name="Id">Идентификатор записи.</param>
/// <param name="ActorType">Строковое имя типа инициатора.</param>
/// <param name="ActorId">Идентификатор инициатора или отсутствие значения для системы.</param>
/// <param name="Action">Строковое имя действия.</param>
/// <param name="EntityType">Тип сущности.</param>
/// <param name="EntityId">Идентификатор сущности.</param>
/// <param name="Metadata">Проверенные JSON-метаданные без секретов.</param>
/// <param name="OccurredAt">Дата события.</param>
public sealed record AuditLogEntryDetailDto(
    Guid Id,
    string ActorType,
    Guid? ActorId,
    string Action,
    string EntityType,
    Guid EntityId,
    string? Metadata,
    DateTimeOffset OccurredAt);

/// <summary>
/// Представляет внутренние данные для фиксации события аудита pipeline-поведением или системным обработчиком.
/// Время события назначается сервером, а метаданные проходят доменную JSON-проверку.
/// </summary>
/// <param name="ActorType">Тип инициатора.</param>
/// <param name="ActorId">Идентификатор инициатора.</param>
/// <param name="Action">Тип действия.</param>
/// <param name="EntityType">Тип затронутой сущности.</param>
/// <param name="EntityId">Идентификатор затронутой сущности.</param>
/// <param name="Metadata">Необязательные JSON-метаданные.</param>
public sealed record CreateAuditLogEntryDto(
    AuditActorType ActorType,
    Guid? ActorId,
    AuditAction Action,
    string EntityType,
    Guid EntityId,
    string? Metadata);

/// <summary>
/// Маркер отсутствующего сценария обновления записи аудита.
/// Журнал является append-only и не допускает исправления либо удаления ранее зафиксированных событий.
/// </summary>
public sealed record UpdateAuditLogEntryDto;
