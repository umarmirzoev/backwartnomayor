namespace Domain.Enums;

/// <summary>
/// Определяет источник появления неизменяемой версии документа.
/// </summary>
public enum DocumentVersionSource
{
    /// <summary>Первая версия сформирована ИИ из библиотеки пунктов.</summary>
    AiGenerated,

    /// <summary>Версия повторно сформирована ИИ по новым указаниям.</summary>
    AiRegenerated,

    /// <summary>Версия создана ручной правкой юриста.</summary>
    ManualEdit
}
