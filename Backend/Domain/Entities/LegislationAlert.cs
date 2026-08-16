using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Представляет неизменяемое уведомление об обнаруженном изменении законодательства.
/// Сущность создаётся фоновым мониторингом и далее связывается с затронутыми делами.
/// </summary>
public sealed class LegislationAlert : BaseEntity
{
    /// <summary>
    /// Инициализирует уведомление при материализации сохранённых данных ORM.
    /// </summary>
    private LegislationAlert()
    {
    }

    /// <summary>
    /// Создаёт уведомление об изменении законодательства.
    /// </summary>
    /// <param name="title">Краткое название изменения.</param>
    /// <param name="summary">Содержательная сводка для юриста.</param>
    /// <param name="sourceUrl">Ссылка на официальный или проверенный источник.</param>
    /// <param name="lawChangedAt">Дата вступления изменения в силу, если известна.</param>
    /// <param name="detectedAt">Момент обнаружения изменения в UTC.</param>
    public LegislationAlert(
        string title,
        string summary,
        string? sourceUrl,
        DateTimeOffset? lawChangedAt,
        DateTimeOffset detectedAt)
        : base(Guid.NewGuid())
    {
        Title = Guard.RequiredText(title, "название изменения законодательства", 300);
        Summary = Guard.RequiredText(summary, "сводка изменения законодательства");
        SourceUrl = Guard.OptionalText(sourceUrl, "ссылка на источник", 2000);
        DetectedAt = Guard.AgainstDefault(detectedAt, "дата обнаружения");

        if (lawChangedAt.HasValue)
        {
            Guard.AgainstDefault(lawChangedAt.Value, "дата изменения законодательства");
        }

        LawChangedAt = lawChangedAt;
    }

    /// <summary>Получает краткое название изменения законодательства.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Получает сводку изменения и его возможного влияния.</summary>
    public string Summary { get; private set; } = string.Empty;

    /// <summary>Получает ссылку на источник сведений.</summary>
    public string? SourceUrl { get; private set; }

    /// <summary>Получает дату вступления изменения в силу, если она известна.</summary>
    public DateTimeOffset? LawChangedAt { get; private set; }

    /// <summary>Получает момент обнаружения изменения системой.</summary>
    public DateTimeOffset DetectedAt { get; private set; }
}
