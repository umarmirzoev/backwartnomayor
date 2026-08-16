namespace Domain.Enums;

/// <summary>
/// Определяет язык итогового документа, собираемого по шаблону.
/// </summary>
public enum TemplateLanguage
{
    /// <summary>Документ формируется на таджикском языке.</summary>
    Tj,

    /// <summary>Документ формируется на русском языке.</summary>
    Ru,

    /// <summary>Документ поддерживает обе языковые версии.</summary>
    Both
}
