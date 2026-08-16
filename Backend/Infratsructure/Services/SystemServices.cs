using Application.Interfaces.Services;
using Domain.Enums;
using Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>
/// Предоставляет единый системный источник UTC-времени для доменных операций и инфраструктурных адаптеров.
/// Централизация времени сохраняет возможность детерминированной подмены в тестах Application-слоя.
/// </summary>
public sealed class SystemClock : IClock
{
    /// <summary>Получает текущий момент времени с нулевым смещением UTC.</summary>
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

/// <summary>
/// Преобразует тариф юриста в лимит ИИ-запросов из проверенной конфигурации.
/// PostgreSQL хранит снимок возвращённого значения на период, поэтому изменение настройки не переписывает историю.
/// </summary>
public sealed class ConfigurableAiQuotaPolicy : IAiQuotaPolicy
{
    private readonly AiQuotaOptions _options;

    /// <summary>
    /// Инициализирует политику и отклоняет небезопасный неположительный лимит бесплатного тарифа.
    /// </summary>
    /// <param name="options">Настройки лимитов ИИ.</param>
    public ConfigurableAiQuotaPolicy(IOptions<AiQuotaOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        if (_options.FreeMonthlyLimit <= 0)
        {
            throw new InvalidOperationException("Месячный лимит ИИ для бесплатного тарифа должен быть положительным.");
        }
    }

    /// <summary>
    /// Возвращает ограниченный лимит Free и отсутствие лимита Paid, сохраняя инвариант доменной квоты.
    /// </summary>
    /// <param name="tier">Поддерживаемый тариф подписки.</param>
    /// <returns>Положительный лимит либо отсутствие лимита для платного тарифа.</returns>
    public int? GetRequestsLimit(SubscriptionTier tier)
    {
        return tier switch
        {
            SubscriptionTier.Free => _options.FreeMonthlyLimit,
            SubscriptionTier.Paid => null,
            _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, "Указан неизвестный тариф подписки.")
        };
    }
}
