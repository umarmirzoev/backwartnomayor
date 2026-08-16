using Domain.Enums;

namespace Application.DTOs;

/// <summary>
/// Представляет остаток лимита ИИ текущего периода согласно контракту <c>GetAiUsageQuery</c>.
/// </summary>
/// <param name="Id">Идентификатор персистентной квоты.</param>
/// <param name="RequestsUsed">Количество учтённых обращений.</param>
/// <param name="RequestsLimit">Лимит или отсутствие лимита для платного тарифа.</param>
/// <param name="RemainingRequests">Остаток или отсутствие ограничения для платного тарифа.</param>
/// <param name="PeriodEnd">Конец текущего периода.</param>
public sealed record GetAiUsageQuotaDto(
    Guid Id,
    int RequestsUsed,
    int? RequestsLimit,
    int? RemainingRequests,
    DateTimeOffset PeriodEnd);

/// <summary>
/// Представляет полный персистентный снимок квоты для аудита и будущего биллинга.
/// </summary>
/// <param name="Id">Идентификатор квоты.</param>
/// <param name="LawyerId">Идентификатор профиля юриста.</param>
/// <param name="PeriodStart">Начало периода.</param>
/// <param name="PeriodEnd">Конец периода.</param>
/// <param name="Tier">Строковое имя снимка тарифа.</param>
/// <param name="RequestsUsed">Количество использованных запросов.</param>
/// <param name="RequestsLimit">Лимит запросов.</param>
/// <param name="RemainingRequests">Остаток запросов.</param>
public sealed record AiUsageQuotaDetailDto(
    Guid Id,
    Guid LawyerId,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    string Tier,
    int RequestsUsed,
    int? RequestsLimit,
    int? RemainingRequests);

/// <summary>
/// Представляет внутренние параметры создания квоты текущего юриста на новый период.
/// Идентификатор владельца берётся из доверенного контекста, а использованный счётчик начинается с нуля.
/// </summary>
/// <param name="PeriodStart">Начало периода включительно.</param>
/// <param name="PeriodEnd">Конец периода исключительно.</param>
/// <param name="Tier">Снимок тарифа на период.</param>
/// <param name="RequestsLimit">Положительный лимит Free-тарифа или отсутствие лимита Paid-тарифа.</param>
public sealed record CreateAiUsageQuotaDto(
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    SubscriptionTier Tier,
    int? RequestsLimit);

/// <summary>
/// Маркер отсутствующего общего обновления квоты.
/// Счётчик изменяется только атомарной доменной операцией регистрации использования,
/// что предотвращает произвольную подмену лимита или количества запросов через API.
/// </summary>
public sealed record UpdateAiUsageQuotaDto;
