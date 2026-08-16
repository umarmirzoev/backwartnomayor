using Domain.Common;
using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities;

/// <summary>
/// Представляет персистентный снимок лимита ИИ-запросов юриста за конкретный период.
/// Redis используется как быстрый счётчик, а эта сущность сохраняет доменную историю для аудита.
/// </summary>
public sealed class AiUsageQuota : BaseEntity
{
    /// <summary>
    /// Инициализирует квоту при материализации сохранённых данных ORM.
    /// </summary>
    private AiUsageQuota()
    {
    }

    /// <summary>
    /// Создаёт квоту юриста на период с нулевым количеством использованных запросов.
    /// </summary>
    /// <param name="lawyerId">Идентификатор доменного профиля юриста.</param>
    /// <param name="periodStart">Начало периода включительно в UTC.</param>
    /// <param name="periodEnd">Конец периода исключительно в UTC.</param>
    /// <param name="tier">Снимок тарифа на период.</param>
    /// <param name="requestsLimit">Положительный лимит для Free или <see langword="null"/> для Paid.</param>
    public AiUsageQuota(
        Guid lawyerId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        SubscriptionTier tier,
        int? requestsLimit)
        : base(Guid.NewGuid())
    {
        LawyerId = Guard.AgainstEmpty(lawyerId, "идентификатор юриста");
        PeriodStart = Guard.AgainstDefault(periodStart, "начало периода");
        PeriodEnd = Guard.AgainstDefault(periodEnd, "конец периода");
        Guard.Against(PeriodEnd <= PeriodStart, "Конец периода квоты должен быть позже его начала.");

        Tier = Guard.AgainstInvalidEnum(tier, "тариф квоты");
        ValidateLimit(tier, requestsLimit);
        RequestsLimit = requestsLimit;
        RequestsUsed = 0;
    }

    /// <summary>Получает идентификатор доменного профиля юриста.</summary>
    public Guid LawyerId { get; private set; }

    /// <summary>Получает начало периода действия квоты.</summary>
    public DateTimeOffset PeriodStart { get; private set; }

    /// <summary>Получает конец периода действия квоты.</summary>
    public DateTimeOffset PeriodEnd { get; private set; }

    /// <summary>Получает снимок тарифа на момент создания периода.</summary>
    public SubscriptionTier Tier { get; private set; }

    /// <summary>Получает количество учтённых обращений к ИИ.</summary>
    public int RequestsUsed { get; private set; }

    /// <summary>Получает лимит обращений или отсутствие лимита для платного тарифа.</summary>
    public int? RequestsLimit { get; private set; }

    /// <summary>
    /// Получает оставшееся количество запросов или <see langword="null"/> для безлимитного тарифа.
    /// </summary>
    /// <returns>Неотрицательный остаток лимита либо <see langword="null"/>.</returns>
    public int? GetRemainingRequests()
    {
        return RequestsLimit.HasValue
            ? Math.Max(0, RequestsLimit.Value - RequestsUsed)
            : null;
    }

    /// <summary>
    /// Регистрирует одно фактическое обращение к ИИ и возвращает неизменяемую запись использования.
    /// Операция отклоняется до увеличения счётчика, если бесплатный лимит исчерпан.
    /// </summary>
    /// <param name="requestType">Тип ИИ-операции.</param>
    /// <param name="draftId">Идентификатор черновика для генерации или регенерации.</param>
    /// <param name="succeeded">Признак успешного завершения обращения.</param>
    /// <param name="createdAt">Момент обращения в UTC.</param>
    /// <returns>Append-only запись одного обращения к ИИ.</returns>
    public AiUsageRecord RegisterUsage(
        AiRequestType requestType,
        Guid? draftId,
        bool succeeded,
        DateTimeOffset createdAt)
    {
        requestType = Guard.AgainstInvalidEnum(requestType, "тип ИИ-запроса");
        createdAt = Guard.AgainstDefault(createdAt, "дата ИИ-запроса");
        Guard.Against(
            createdAt < PeriodStart || createdAt >= PeriodEnd,
            "Дата ИИ-запроса находится вне периода выбранной квоты.");

        if (RequestsLimit.HasValue && RequestsUsed >= RequestsLimit.Value)
        {
            throw new DomainException("Лимит ИИ-запросов за текущий период исчерпан.");
        }

        var usageRecord = new AiUsageRecord(
            LawyerId,
            Id,
            requestType,
            draftId,
            succeeded,
            createdAt);

        RequestsUsed++;
        return usageRecord;
    }

    /// <summary>
    /// Проверяет согласованность тарифа и значения лимита.
    /// </summary>
    /// <param name="tier">Тариф периода.</param>
    /// <param name="requestsLimit">Проверяемый лимит.</param>
    private static void ValidateLimit(SubscriptionTier tier, int? requestsLimit)
    {
        if (tier == SubscriptionTier.Free)
        {
            if (!requestsLimit.HasValue || requestsLimit.Value <= 0)
            {
                throw new DomainValidationException("Для бесплатного тарифа требуется положительный лимит ИИ-запросов.");
            }

            return;
        }

        if (requestsLimit.HasValue)
        {
            throw new DomainValidationException("Для платного тарифа лимит ИИ-запросов должен отсутствовать.");
        }
    }
}
