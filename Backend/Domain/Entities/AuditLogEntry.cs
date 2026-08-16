using System.Text.Json;
using Domain.Common;
using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities;

/// <summary>
/// Представляет неизменяемую запись аудита доступа или изменения доменных данных.
/// Полиморфные идентификаторы намеренно не являются навигационными свойствами,
/// чтобы запись сохранялась даже после полного удаления исходной сущности.
/// </summary>
public sealed class AuditLogEntry : BaseEntity
{
    /// <summary>
    /// Инициализирует запись аудита при материализации сохранённых данных ORM.
    /// </summary>
    private AuditLogEntry()
    {
    }

    /// <summary>
    /// Создаёт append-only запись аудита с проверкой субъекта и JSON-метаданных.
    /// </summary>
    /// <param name="actorType">Тип инициатора действия.</param>
    /// <param name="actorId">Идентификатор инициатора или <see langword="null"/> для системы.</param>
    /// <param name="action">Тип зафиксированного действия.</param>
    /// <param name="entityType">Техническое имя типа затронутой сущности.</param>
    /// <param name="entityId">Идентификатор затронутой сущности.</param>
    /// <param name="metadata">Необязательные JSON-метаданные без секретов и лишних персональных данных.</param>
    /// <param name="occurredAt">Момент действия в UTC.</param>
    public AuditLogEntry(
        AuditActorType actorType,
        Guid? actorId,
        AuditAction action,
        string entityType,
        Guid entityId,
        string? metadata,
        DateTimeOffset occurredAt)
        : base(Guid.NewGuid())
    {
        ActorType = Guard.AgainstInvalidEnum(actorType, "тип инициатора аудита");
        Action = Guard.AgainstInvalidEnum(action, "действие аудита");
        ValidateActor(actorType, actorId);

        ActorId = actorId;
        EntityType = Guard.RequiredText(entityType, "тип сущности аудита", 100);
        EntityId = Guard.AgainstEmpty(entityId, "идентификатор сущности аудита");
        Metadata = NormalizeMetadata(action, metadata);
        OccurredAt = Guard.AgainstDefault(occurredAt, "дата события аудита");
    }

    /// <summary>Получает тип инициатора действия.</summary>
    public AuditActorType ActorType { get; private set; }

    /// <summary>Получает идентификатор инициатора или отсутствие значения для системы.</summary>
    public Guid? ActorId { get; private set; }

    /// <summary>Получает тип зафиксированного действия.</summary>
    public AuditAction Action { get; private set; }

    /// <summary>Получает техническое имя типа затронутой сущности.</summary>
    public string EntityType { get; private set; } = string.Empty;

    /// <summary>Получает идентификатор затронутой сущности.</summary>
    public Guid EntityId { get; private set; }

    /// <summary>Получает проверенные JSON-метаданные события.</summary>
    public string? Metadata { get; private set; }

    /// <summary>Получает момент возникновения события.</summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>
    /// Проверяет согласованность типа инициатора и его идентификатора.
    /// </summary>
    /// <param name="actorType">Тип инициатора.</param>
    /// <param name="actorId">Идентификатор инициатора.</param>
    private static void ValidateActor(AuditActorType actorType, Guid? actorId)
    {
        if (actorType == AuditActorType.System)
        {
            Guard.Against(actorId.HasValue, "Для системной записи аудита идентификатор инициатора должен отсутствовать.");
            return;
        }

        if (!actorId.HasValue || actorId.Value == Guid.Empty)
        {
            throw new DomainValidationException("Для записи аудита юриста или клиента требуется идентификатор инициатора.");
        }
    }

    /// <summary>
    /// Нормализует и проверяет JSON-метаданные, исключая метаданные при полном удалении данных.
    /// </summary>
    /// <param name="action">Тип действия аудита.</param>
    /// <param name="metadata">Исходные JSON-метаданные.</param>
    /// <returns>Проверенные JSON-метаданные либо <see langword="null"/>.</returns>
    private static string? NormalizeMetadata(AuditAction action, string? metadata)
    {
        var normalized = Guard.OptionalText(metadata, "метаданные аудита");

        if (action == AuditAction.FullyDeleted)
        {
            Guard.Against(
                normalized is not null,
                "Запись о полном удалении не должна содержать метаданные удалённой сущности.");
            return null;
        }

        if (normalized is null)
        {
            return null;
        }

        try
        {
            using var _ = JsonDocument.Parse(normalized);
            return normalized;
        }
        catch (JsonException)
        {
            throw new DomainValidationException("Метаданные аудита должны быть корректным JSON-документом.");
        }
    }
}
