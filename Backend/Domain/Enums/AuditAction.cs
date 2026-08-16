namespace Domain.Enums;

/// <summary>
/// Определяет зафиксированный тип действия над доменной сущностью.
/// Список соответствует действиям, прямо указанным в спецификации.
/// </summary>
public enum AuditAction
{
    /// <summary>Сущность или документ были открыты для просмотра.</summary>
    Opened,

    /// <summary>Сущность или документ были изменены.</summary>
    Modified,

    /// <summary>Сущность была удалена обычным сценарием.</summary>
    Deleted,

    /// <summary>Персональные данные и содержимое были полностью уничтожены.</summary>
    FullyDeleted,

    /// <summary>Документ был экспортирован.</summary>
    Exported,

    /// <summary>Состояние доменной сущности было изменено.</summary>
    StatusChanged
}
