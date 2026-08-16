namespace Domain.Enums;

/// <summary>
/// Определяет операцию, которая расходует лимит обращения к ИИ.
/// </summary>
public enum AiRequestType
{
    /// <summary>Создание первой версии черновика.</summary>
    GenerateDraft,

    /// <summary>Повторная генерация существующего черновика.</summary>
    RegenerateDraft,

    /// <summary>Разбор входящего документа без создания черновика.</summary>
    ReviewIncomingDocument
}
