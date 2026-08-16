using Domain.Enums;

namespace Application.Common.Models;

/// <summary>
/// Определяет фильтры списка доменных профилей юристов для внутренних административных сценариев.
/// Публичный MVP-запрос текущего профиля не использует этот фильтр и получает владельца из Identity-контекста.
/// </summary>
public sealed record LawyerProfileFilterParam : BaseFilterParam
{
    /// <summary>Получает поисковую строку по имени юриста или названию юридической фирмы.</summary>
    public string? SearchTerm { get; init; }

    /// <summary>Получает необязательный фильтр по тарифу.</summary>
    public SubscriptionTier? SubscriptionTier { get; init; }

    /// <summary>Получает необязательный фильтр по активности профиля.</summary>
    public bool? IsActive { get; init; }
}

/// <summary>
/// Определяет поиск и пагинацию клиентов текущего юриста без возможности подмены владельца в запросе.
/// Идентификатор юриста должен поступать из доверенного контекста аутентификации в CQRS-обработчике.
/// </summary>
public sealed record ClientFilterParam : BaseFilterParam
{
    /// <summary>Получает поисковую строку по имени, компании, телефону или адресу электронной почты.</summary>
    public string? SearchTerm { get; init; }
}

/// <summary>
/// Определяет фильтры дел текущего юриста с возможностью ограничить выборку конкретным клиентом и состоянием.
/// </summary>
public sealed record CaseFilterParam : BaseFilterParam
{
    /// <summary>Получает необязательный идентификатор клиента-владельца дел.</summary>
    public Guid? ClientId { get; init; }

    /// <summary>Получает необязательный фильтр по состоянию дела.</summary>
    public CaseStatus? Status { get; init; }
}

/// <summary>
/// Определяет фильтры каталога активных шаблонов, доступных при создании нового черновика.
/// </summary>
public sealed record TemplateFilterParam : BaseFilterParam
{
    /// <summary>Получает необязательное ограничение по языку шаблона.</summary>
    public TemplateLanguage? Language { get; init; }
}

/// <summary>
/// Определяет поиск по активной библиотеке договорных пунктов согласно CQRS-спецификации.
/// </summary>
public sealed record ClauseBlockFilterParam : BaseFilterParam
{
    /// <summary>Получает поисковый текст для заголовка и двуязычного содержимого пункта.</summary>
    public string? SearchTerm { get; init; }

    /// <summary>Получает необязательную категорию договорного пункта.</summary>
    public string? Category { get; init; }
}

/// <summary>
/// Определяет фильтры упорядоченного состава пунктов конкретного шаблона.
/// </summary>
public sealed record TemplateClauseBlockFilterParam : BaseFilterParam
{
    /// <summary>Получает идентификатор шаблона, состав которого требуется вернуть.</summary>
    public Guid? TemplateId { get; init; }

    /// <summary>Получает необязательный фильтр по включению пункта в черновик по умолчанию.</summary>
    public bool? IsDefault { get; init; }
}

/// <summary>
/// Определяет фильтры документов текущего юриста по делу и состоянию жизненного цикла.
/// </summary>
public sealed record DraftFilterParam : BaseFilterParam
{
    /// <summary>Получает необязательный идентификатор дела.</summary>
    public Guid? CaseId { get; init; }

    /// <summary>Получает необязательное состояние документа.</summary>
    public DocumentStatus? Status { get; init; }
}

/// <summary>
/// Определяет пагинацию истории неизменяемых версий конкретного черновика.
/// </summary>
public sealed record DocumentVersionFilterParam : BaseFilterParam
{
    /// <summary>Получает идентификатор черновика, журнал версий которого запрашивается.</summary>
    public Guid? DraftId { get; init; }
}

/// <summary>
/// Определяет фильтры уведомлений об изменениях законодательства.
/// Для пользовательского MVP-сценария по умолчанию возвращаются только непрочитанные уведомления.
/// </summary>
public sealed record LegislationAlertFilterParam : BaseFilterParam
{
    /// <summary>Получает признак ограничения выборки непрочитанными уведомлениями.</summary>
    public bool UnreadOnly { get; init; } = true;

    /// <summary>Получает необязательную нижнюю границу времени обнаружения уведомления.</summary>
    public DateTimeOffset? DetectedFrom { get; init; }
}

/// <summary>
/// Определяет фильтры связей уведомлений законодательства с делами текущего юриста.
/// </summary>
public sealed record CaseLegislationAlertFilterParam : BaseFilterParam
{
    /// <summary>Получает необязательный идентификатор затронутого дела.</summary>
    public Guid? CaseId { get; init; }

    /// <summary>Получает необязательный фильтр по признаку прочтения.</summary>
    public bool? IsRead { get; init; }
}

/// <summary>
/// Определяет безопасные фильтры append-only журнала аудита по конкретному доменному объекту.
/// Права текущего юриста на целевой объект должны проверяться до выполнения запроса к журналу.
/// </summary>
public sealed record AuditLogEntryFilterParam : BaseFilterParam
{
    /// <summary>Получает техническое имя типа сущности.</summary>
    public string? EntityType { get; init; }

    /// <summary>Получает идентификатор целевой сущности.</summary>
    public Guid? EntityId { get; init; }

    /// <summary>Получает необязательный фильтр по типу действия.</summary>
    public AuditAction? Action { get; init; }
}

/// <summary>
/// Определяет период выборки персистентных снимков квоты ИИ для текущего юриста.
/// </summary>
public sealed record AiUsageQuotaFilterParam : BaseFilterParam
{
    /// <summary>Получает необязательную нижнюю границу начала периода квоты.</summary>
    public DateTimeOffset? PeriodStart { get; init; }

    /// <summary>Получает необязательную верхнюю границу конца периода квоты.</summary>
    public DateTimeOffset? PeriodEnd { get; init; }
}

/// <summary>
/// Определяет фильтры истории фактических обращений текущего юриста к ИИ.
/// </summary>
public sealed record AiUsageRecordFilterParam : BaseFilterParam
{
    /// <summary>Получает необязательный тип ИИ-операции.</summary>
    public AiRequestType? RequestType { get; init; }

    /// <summary>Получает необязательный фильтр по успешности обращения.</summary>
    public bool? Succeeded { get; init; }

    /// <summary>Получает необязательную нижнюю границу времени обращения.</summary>
    public DateTimeOffset? CreatedFrom { get; init; }

    /// <summary>Получает необязательную верхнюю границу времени обращения.</summary>
    public DateTimeOffset? CreatedTo { get; init; }
}

/// <summary>
/// Определяет фильтры workflow запросов на полное удаление данных.
/// </summary>
public sealed record DataDeletionRequestFilterParam : BaseFilterParam
{
    /// <summary>Получает необязательный тип удаляемого доменного объекта.</summary>
    public DeletionTargetType? TargetEntityType { get; init; }

    /// <summary>Получает необязательный идентификатор удаляемого объекта.</summary>
    public Guid? TargetEntityId { get; init; }

    /// <summary>Получает необязательное состояние обработки запроса.</summary>
    public DataDeletionStatus? Status { get; init; }
}

/// <summary>
/// Определяет фильтры Post-MVP комментариев по версиям документов.
/// </summary>
public sealed record DocumentCommentFilterParam : BaseFilterParam
{
    /// <summary>Получает необязательный идентификатор версии документа.</summary>
    public Guid? DocumentVersionId { get; init; }

    /// <summary>Получает признак включения уже разрешённых комментариев.</summary>
    public bool IncludeResolved { get; init; }
}

/// <summary>
/// Определяет фильтры Post-MVP записей электронной подписи по черновику и типу подписанта.
/// </summary>
public sealed record SignatureRecordFilterParam : BaseFilterParam
{
    /// <summary>Получает необязательный идентификатор подписываемого черновика.</summary>
    public Guid? DraftId { get; init; }

    /// <summary>Получает необязательный тип подписанта.</summary>
    public PartyType? SignerType { get; init; }
}
