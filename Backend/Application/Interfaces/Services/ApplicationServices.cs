using Application.Common.Models;
using Application.Common.Security;
using Application.DTOs;
using Domain.Enums;

namespace Application.Interfaces.Services;

/// <summary>
/// Предоставляет текущий момент времени через заменяемый порт для детерминированных тестов и единообразного UTC.
/// </summary>
public interface IClock
{
    /// <summary>Получает текущий момент времени в UTC.</summary>
    DateTimeOffset UtcNow { get; }
}

/// <summary>
/// Предоставляет проверенные идентификаторы аутентифицированного субъекта из транспортного слоя.
/// Значения не принимаются из тела или маршрута запроса и служат основой защиты от IDOR.
/// </summary>
public interface ICurrentUserContext
{
    /// <summary>Получает идентификатор учётной записи Identity.</summary>
    Guid? IdentityUserId { get; }

    /// <summary>Получает идентификатор доменного профиля юриста для MVP-сценариев.</summary>
    Guid? LawyerId { get; }

    /// <summary>Получает тип стороны для Post-MVP клиентского портала.</summary>
    PartyType? PartyType { get; }

    /// <summary>Получает доменный идентификатор текущей стороны.</summary>
    Guid? PartyId { get; }

    /// <summary>Получает признак доверенного системного исполнителя фоновой задачи.</summary>
    bool IsSystem { get; }

    /// <summary>Получает проверенный удалённый IP-адрес для юридически значимой записи подписи.</summary>
    string? IpAddress { get; }

    /// <summary>
    /// Получает момент последней подтверждённой парольной аутентификации из доверенного JWT-claim.
    /// Значение не обновляется при ротации refresh-токена и используется для чувствительных операций подписи.
    /// </summary>
    DateTimeOffset? AuthenticatedAt { get; }
}

/// <summary>
/// Абстрагирует ASP.NET Core Identity и токены от Application-слоя.
/// Реализация обязана хешировать пароли средствами Identity, хранить только хеш refresh-токена
/// и выполнять ротацию refresh-токена при каждом успешном обновлении.
/// </summary>
public interface IIdentityService
{
    /// <summary>
    /// Создаёт учётную запись Identity с безопасным хешированием пароля.
    /// </summary>
    /// <param name="email">Нормализуемый уникальный email.</param>
    /// <param name="password">Исходный пароль.</param>
    /// <param name="phoneNumber">Необязательный телефон.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Идентификатор созданной учётной записи либо ожидаемые ошибки Identity.</returns>
    Task<ServiceResult<Guid>> CreateUserAsync(
        string email,
        string password,
        string? phoneNumber,
        CancellationToken cancellationToken);

    /// <summary>
    /// Удаляет только что созданную учётную запись при компенсирующем откате регистрации профиля.
    /// </summary>
    /// <param name="userId">Идентификатор Identity.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат компенсирующего удаления.</returns>
    Task<ServiceResult<bool>> DeleteUserAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Проверяет credentials без раскрытия причины отказа и выдаёт пару токенов.
    /// </summary>
    /// <param name="email">Email пользователя.</param>
    /// <param name="password">Пароль пользователя.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Токены либо унифицированная ошибка аутентификации.</returns>
    Task<ServiceResult<AuthenticationTokensDto>> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken);

    /// <summary>
    /// Проверяет и ротирует refresh-токен, отзывая использованное значение.
    /// </summary>
    /// <param name="refreshToken">Одноразовый refresh-токен.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Новая пара токенов либо унифицированная ошибка.</returns>
    Task<ServiceResult<AuthenticationTokensDto>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken);

    /// <summary>
    /// Возвращает безопасные Identity-сведения текущей учётной записи.
    /// </summary>
    /// <param name="userId">Идентификатор Identity.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Сведения пользователя или отсутствие записи.</returns>
    Task<IdentityUserDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken);
}

/// <summary>
/// Определяет конфигурируемую политику лимитов ИИ без захардкоженных чисел в CQRS-обработчиках.
/// </summary>
public interface IAiQuotaPolicy
{
    /// <summary>
    /// Возвращает лимит тарифа; для безлимитного тарифа возвращает <see langword="null"/>.
    /// </summary>
    /// <param name="tier">Тариф подписки.</param>
    /// <returns>Положительный лимит либо отсутствие лимита.</returns>
    int? GetRequestsLimit(SubscriptionTier tier);
}

/// <summary>
/// Определяет Redis-адаптер быстрого счётчика ИИ-квоты.
/// PostgreSQL остаётся источником истины, а атомарное резервирование защищает от параллельного превышения лимита.
/// </summary>
public interface IAiQuotaCounter
{
    /// <summary>
    /// Проверяет наличие доступного запроса, инициализируя кэш персистентным снимком при необходимости.
    /// </summary>
    Task<bool> IsAvailableAsync(
        Guid lawyerId,
        Guid quotaId,
        int requestsUsed,
        int? requestsLimit,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken);

    /// <summary>
    /// Атомарно резервирует один запрос непосредственно перед обращением к ИИ-провайдеру.
    /// </summary>
    Task<bool> TryReserveAsync(
        Guid lawyerId,
        Guid quotaId,
        int requestsUsed,
        int? requestsLimit,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken);

    /// <summary>
    /// Синхронизирует быстрый счётчик с успешно зафиксированным персистентным значением.
    /// </summary>
    Task SynchronizeAsync(
        Guid lawyerId,
        Guid quotaId,
        int requestsUsed,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken);
}

/// <summary>
/// Абстрагирует RAG-ориентированную работу ИИ от Application-слоя.
/// Реализация не должна использовать переданные документы для обучения и обязана применять отмену/таймауты.
/// </summary>
public interface IAiDraftingService
{
    /// <summary>Формирует первую версию по описанию сделки и проверенным библиотечным пунктам.</summary>
    Task<ServiceResult<string>> GenerateDraftAsync(
        string dealDescription,
        IReadOnlyList<string> clauseContents,
        CancellationToken cancellationToken);

    /// <summary>Перегенерирует текущий текст с учётом новых указаний юриста.</summary>
    Task<ServiceResult<string>> RegenerateDraftAsync(
        string currentContent,
        string instructions,
        CancellationToken cancellationToken);

    /// <summary>Анализирует входящий документ без сохранения его как доменного черновика.</summary>
    Task<ServiceResult<IReadOnlyList<DocumentReviewItemDto>>> ReviewIncomingDocumentAsync(
        string content,
        CancellationToken cancellationToken);
}

/// <summary>
/// Абстрагирует шифрованное S3-совместимое хранилище содержимого документов.
/// Ключи хранилища остаются внутри Application/Infrastructure и никогда не возвращаются API-клиенту.
/// </summary>
public interface IDocumentStorageService
{
    /// <summary>Шифрует и сохраняет текст, возвращая внутренний ключ объекта.</summary>
    Task<ServiceResult<string>> StoreTextAsync(string content, CancellationToken cancellationToken);

    /// <summary>Загружает и расшифровывает текст по внутреннему ключу.</summary>
    Task<ServiceResult<string>> GetTextAsync(string storageKey, CancellationToken cancellationToken);

    /// <summary>Необратимо удаляет объект содержимого.</summary>
    Task<ServiceResult<bool>> DeleteAsync(string storageKey, CancellationToken cancellationToken);
}

/// <summary>
/// Определяет генерацию экспортируемого документа из уже авторизованного и расшифрованного текста.
/// </summary>
public interface IDocumentExportService
{
    /// <summary>Создаёт DOCX или PDF без записи временного пути в публичный контракт.</summary>
    Task<ServiceResult<ExportedDocumentDto>> ExportAsync(
        Guid draftId,
        string content,
        DocumentExportFormat format,
        CancellationToken cancellationToken);
}

/// <summary>
/// Проверяет юридически значимое доказательство подписи до создания append-only записи.
/// Реализация для аккредитованного центра обязана валидировать внешнюю криптографическую подпись,
/// а для простой подписи — подтверждённую версию пользовательского соглашения и повторную аутентификацию.
/// </summary>
public interface ISignatureVerificationService
{
    /// <summary>Проверяет доказательство подписи текущей стороны без передачи доверенных идентификаторов из тела запроса.</summary>
    Task<ServiceResult<bool>> VerifyAsync(
        Guid draftId,
        Guid documentVersionId,
        PartyType signerType,
        Guid signerId,
        SignatureMethod method,
        string consentAgreementVersion,
        CancellationToken cancellationToken);
}

/// <summary>
/// Определяет поддерживаемые форматы экспорта текущей версии документа.
/// </summary>
public enum DocumentExportFormat
{
    /// <summary>Документ Microsoft Word Open XML.</summary>
    Docx,

    /// <summary>Документ PDF.</summary>
    Pdf
}

/// <summary>
/// Проверяет прикладные разрешения, поступающие из политик/claims внешнего слоя.
/// </summary>
public interface IApplicationAuthorizationService
{
    /// <summary>Проверяет наличие конкретного разрешения у текущего субъекта.</summary>
    Task<bool> HasPermissionAsync(
        ApplicationPermission permission,
        CancellationToken cancellationToken);
}

/// <summary>
/// Проверяет Post-MVP доступ юриста или клиента к конкретному документу без раскрытия его существования.
/// </summary>
public interface IResourceAuthorizationService
{
    /// <summary>Проверяет доступ стороны к черновику.</summary>
    Task<bool> CanAccessDraftAsync(
        Guid draftId,
        PartyType partyType,
        Guid partyId,
        CancellationToken cancellationToken);

    /// <summary>Проверяет доступ стороны к immutable-версии документа.</summary>
    Task<bool> CanAccessDocumentVersionAsync(
        Guid documentVersionId,
        PartyType partyType,
        Guid partyId,
        CancellationToken cancellationToken);
}

/// <summary>
/// Абстрагирует долговечную постановку бизнес-критичных фоновых заданий.
/// Реализация должна быть идемпотентной и использовать устойчивую очередь, а не fire-and-forget задачу процесса WebAPI.
/// </summary>
public interface IBackgroundTaskScheduler
{
    /// <summary>Ставит исполнение запроса удаления в устойчивую очередь.</summary>
    Task ScheduleDataDeletionAsync(Guid requestId, CancellationToken cancellationToken);

    /// <summary>Ставит проверку просрочки отправленного документа на указанное время.</summary>
    Task ScheduleDraftExpirationAsync(
        Guid draftId,
        DateTimeOffset dueAt,
        CancellationToken cancellationToken);
}
