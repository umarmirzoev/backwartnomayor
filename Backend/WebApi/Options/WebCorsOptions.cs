namespace WebApi.Options;

/// <summary>
/// Определяет точный белый список источников браузерных клиентов.
/// Wildcard не используется, чтобы случайно не открыть API произвольным сайтам.
/// </summary>
public sealed class WebCorsOptions
{
    /// <summary>Получает имя секции конфигурации.</summary>
    public const string SectionName = "Cors";

    /// <summary>Получает или задаёт разрешённые HTTPS/локальные источники фронтенда.</summary>
    public string[] AllowedOrigins { get; set; } = [];
}
