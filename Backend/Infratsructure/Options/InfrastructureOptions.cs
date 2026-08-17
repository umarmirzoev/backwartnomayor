namespace Infrastructure.Options;

/// <summary>
/// Определяет криптографические и временные параметры JWT-аутентификации.
/// Значение ключа подписи является секретом и должно поступать из переменной окружения или хранилища секретов.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>Получает имя секции конфигурации.</summary>
    public const string SectionName = "Jwt";

    /// <summary>Получает или задаёт доверенного издателя токена.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Получает или задаёт целевую аудиторию токена.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>Получает или задаёт секрет подписи длиной не менее 32 байтов.</summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Получает или задаёт короткий срок действия access-токена в минутах.</summary>
    public int AccessTokenLifetimeMinutes { get; set; } = 15;

    /// <summary>Получает или задаёт срок действия ротируемого refresh-токена в днях.</summary>
    public int RefreshTokenLifetimeDays { get; set; } = 14;

    /// <summary>Получает или задаёт роль, выдаваемую при самостоятельной регистрации юриста.</summary>
    public string DefaultRegistrationRole { get; set; } = "Lawyer";
}

/// <summary>
/// Определяет конфигурируемые лимиты ИИ-тарифа без жёстко заданных значений в обработчиках.
/// </summary>
public sealed class AiQuotaOptions
{
    /// <summary>Получает имя секции конфигурации.</summary>
    public const string SectionName = "AiQuota";

    /// <summary>Получает или задаёт месячный лимит запросов бесплатного тарифа.</summary>
    public int FreeMonthlyLimit { get; set; } = 20;
}

/// <summary>
/// Определяет подключение и пространство ключей Redis для атомарного Cache-Aside-счётчика квот.
/// </summary>
public sealed class RedisOptions
{
    /// <summary>Получает имя секции конфигурации.</summary>
    public const string SectionName = "Redis";

    /// <summary>Получает или задаёт строку подключения Redis.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Получает или задаёт изолирующий префикс ключей приложения.</summary>
    public string KeyPrefix { get; set; } = "shartnoma";
}

/// <summary>
/// Определяет безопасное подключение к Gemini API и ограничения одного запроса.
/// API-ключ не должен находиться в отслеживаемых файлах конфигурации.
/// </summary>
public sealed class GeminiOptions
{
    /// <summary>Получает имя секции конфигурации.</summary>
    public const string SectionName = "Gemini";

    /// <summary>Получает или задаёт секретный API-ключ Google AI Studio.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Получает или задаёт точное имя стабильной модели.</summary>
    public string Model { get; set; } = "gemini-3.6-flash";

    /// <summary>Получает или задаёт сетевой тайм-аут в секундах.</summary>
    public int TimeoutSeconds { get; set; } = 90;

    /// <summary>Получает или задаёт верхнюю границу ответа модели.</summary>
    public int MaxOutputTokens { get; set; } = 8192;
}

/// <summary>
/// Определяет S3-совместимое объектное хранилище и ключ прикладного AES-шифрования.
/// Секреты доступа и ключ шифрования передаются только через защищённую конфигурацию среды.
/// </summary>
public sealed class DocumentStorageOptions
{
    /// <summary>Получает имя секции конфигурации.</summary>
    public const string SectionName = "DocumentStorage";

    /// <summary>Получает или задаёт URL S3-совместимого сервиса.</summary>
    public string ServiceUrl { get; set; } = string.Empty;

    /// <summary>Получает или задаёт идентификатор доступа S3.</summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>Получает или задаёт секрет доступа S3.</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Получает или задаёт имя приватного контейнера документов.</summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>Получает или задаёт регион подписи S3; для MinIO обычно используется us-east-1.</summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>Получает или задаёт Base64-ключ AES-256 длиной ровно 32 байта.</summary>
    public string EncryptionKey { get; set; } = string.Empty;

    /// <summary>Получает или задаёт необходимость path-style адресации для MinIO и аналогов.</summary>
    public bool ForcePathStyle { get; set; } = true;
}

/// <summary>
/// Определяет выбранную владельцем проекта лицензию QuestPDF.
/// Значение должно соответствовать фактическому коммерческому статусу организации.
/// </summary>
public sealed class DocumentExportOptions
{
    /// <summary>Получает имя секции конфигурации.</summary>
    public const string SectionName = "DocumentExport";

    /// <summary>Получает или задаёт имя лицензии QuestPDF: Community, Professional или Enterprise.</summary>
    public string QuestPdfLicense { get; set; } = "Community";
}

/// <summary>
/// Определяет правила простой электронной подписи, не подменяя интеграцию с аккредитованным удостоверяющим центром.
/// </summary>
public sealed class SignatureOptions
{
    /// <summary>Получает имя секции конфигурации.</summary>
    public const string SectionName = "Signature";

    /// <summary>Получает или задаёт единственную принимаемую версию пользовательского соглашения.</summary>
    public string ConsentAgreementVersion { get; set; } = "1.0";

    /// <summary>Получает или задаёт окно после парольного входа, в течение которого разрешена простая подпись.</summary>
    public int ReauthenticationWindowMinutes { get; set; } = 5;
}
