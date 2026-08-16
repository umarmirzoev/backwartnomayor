namespace Domain.Enums;

/// <summary>
/// Определяет источник действия, зафиксированного в журнале аудита.
/// </summary>
public enum AuditActorType
{
    /// <summary>Действие выполнил юрист.</summary>
    Lawyer,

    /// <summary>Действие выполнил клиент.</summary>
    Client,

    /// <summary>Действие выполнила система или фоновая задача.</summary>
    System
}
