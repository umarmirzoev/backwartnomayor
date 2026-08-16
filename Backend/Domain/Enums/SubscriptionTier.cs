namespace Domain.Enums;

/// <summary>
/// Определяет тариф подписки юриста и правила применения лимита ИИ-запросов.
/// </summary>
public enum SubscriptionTier
{
    /// <summary>Бесплатный тариф с ограниченным количеством ИИ-запросов.</summary>
    Free,

    /// <summary>Платный тариф без лимита ИИ-запросов.</summary>
    Paid
}
