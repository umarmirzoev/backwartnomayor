namespace WebApi.Background;

/// <summary>Определяет частоту и размер идемпотентной обработки долговечных фоновых записей PostgreSQL.</summary>
public sealed class BackgroundProcessingOptions
{
    /// <summary>Получает имя секции конфигурации.</summary>
    public const string SectionName = "BackgroundProcessing";

    /// <summary>Получает или задаёт интервал проверки просроченных документов в секундах.</summary>
    public int PollIntervalSeconds { get; set; } = 60;

    /// <summary>Получает или задаёт максимальный размер одной SQL-пачки.</summary>
    public int BatchSize { get; set; } = 50;
}
