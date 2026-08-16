namespace WebApi.Seeds;

/// <summary>
/// Определяет опциональную первичную учётную запись суперадминистратора.
/// Пароль не имеет значения по умолчанию и должен передаваться только через переменную окружения или User Secrets.
/// </summary>
public sealed class BootstrapAdminOptions
{
    /// <summary>Получает имя секции конфигурации.</summary>
    public const string SectionName = "BootstrapAdmin";

    /// <summary>Получает или задаёт явное разрешение инициализации учётной записи.</summary>
    public bool Enabled { get; set; }

    /// <summary>Получает или задаёт уникальный email суперадминистратора.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Получает или задаёт временный сильный пароль из защищённой конфигурации.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Получает или задаёт полное имя административного профиля.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Получает или задаёт необязательное название организации.</summary>
    public string? LawFirmName { get; set; }
}
