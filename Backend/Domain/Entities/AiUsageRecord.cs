using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Представляет неизменяемую запись одного фактического обращения юриста к ИИ.
/// Запись сохраняется для аудита и будущего биллинга независимо от судьбы связанного черновика.
/// </summary>
public sealed class AiUsageRecord : BaseEntity
{
    /// <summary>
    /// Инициализирует запись использования при материализации сохранённых данных ORM.
    /// </summary>
    private AiUsageRecord()
    {
    }

    /// <summary>
    /// Создаёт запись обращения через соответствующую квоту.
    /// </summary>
    /// <param name="lawyerId">Идентификатор доменного профиля юриста.</param>
    /// <param name="aiUsageQuotaId">Идентификатор квоты периода.</param>
    /// <param name="requestType">Тип ИИ-операции.</param>
    /// <param name="draftId">Идентификатор связанного черновика.</param>
    /// <param name="succeeded">Признак успешного результата.</param>
    /// <param name="createdAt">Момент обращения в UTC.</param>
    internal AiUsageRecord(
        Guid lawyerId,
        Guid aiUsageQuotaId,
        AiRequestType requestType,
        Guid? draftId,
        bool succeeded,
        DateTimeOffset createdAt)
        : base(Guid.NewGuid())
    {
        LawyerId = Guard.AgainstEmpty(lawyerId, "идентификатор юриста");
        AiUsageQuotaId = Guard.AgainstEmpty(aiUsageQuotaId, "идентификатор квоты");
        RequestType = Guard.AgainstInvalidEnum(requestType, "тип ИИ-запроса");
        ValidateDraftReference(requestType, draftId);
        DraftId = draftId;
        Succeeded = succeeded;
        CreatedAt = Guard.AgainstDefault(createdAt, "дата ИИ-запроса");
    }

    /// <summary>Получает идентификатор доменного профиля юриста.</summary>
    public Guid LawyerId { get; private set; }

    /// <summary>Получает идентификатор квоты, к которой отнесён запрос.</summary>
    public Guid AiUsageQuotaId { get; private set; }

    /// <summary>Получает тип выполненной ИИ-операции.</summary>
    public AiRequestType RequestType { get; private set; }

    /// <summary>Получает идентификатор связанного черновика, если он применим.</summary>
    public Guid? DraftId { get; private set; }

    /// <summary>Получает признак успешного завершения обращения.</summary>
    public bool Succeeded { get; private set; }

    /// <summary>Получает момент обращения к ИИ.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Проверяет обязательность ссылки на черновик для операций генерации документа.
    /// </summary>
    /// <param name="requestType">Тип ИИ-операции.</param>
    /// <param name="draftId">Проверяемый идентификатор черновика.</param>
    private static void ValidateDraftReference(AiRequestType requestType, Guid? draftId)
    {
        if (requestType is AiRequestType.GenerateDraft or AiRequestType.RegenerateDraft)
        {
            Guard.Against(
                !draftId.HasValue || draftId.Value == Guid.Empty,
                "Для генерации или регенерации документа требуется идентификатор черновика.");
            return;
        }

        Guard.Against(
            draftId.HasValue && draftId.Value == Guid.Empty,
            "Идентификатор черновика не может быть пустым.");
    }
}
