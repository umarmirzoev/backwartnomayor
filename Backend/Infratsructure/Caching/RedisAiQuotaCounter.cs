using Application.Interfaces.Services;
using Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Infrastructure.Caching;

/// <summary>
/// Реализует атомарный Redis-счётчик ИИ-квоты по схеме Cache-Aside.
/// Lua-скрипты не допускают гонку параллельных запросов, а PostgreSQL-снимок используется только для начальной синхронизации.
/// Redis — ускоряющий кэш, а не обязательная зависимость: если он не настроен или недоступен, счётчик
/// откатывается на персистентные значения из PostgreSQL, переданные вызывающей стороной, вместо того чтобы
/// валить всю генерацию черновика необработанным исключением (что раньше превращало каждый запрос в 500).
/// </summary>
public sealed class RedisAiQuotaCounter : IAiQuotaCounter, IDisposable
{
    private const string ReserveScript = """
        local current = redis.call('GET', KEYS[1])
        if not current then
            current = tonumber(ARGV[1])
            redis.call('SET', KEYS[1], current, 'EX', ARGV[3], 'NX')
            current = tonumber(redis.call('GET', KEYS[1]))
        else
            current = tonumber(current)
        end
        local limit = tonumber(ARGV[2])
        if current >= limit then
            return -1
        end
        local updated = redis.call('INCR', KEYS[1])
        redis.call('EXPIRE', KEYS[1], ARGV[3])
        return updated
        """;

    private const string SynchronizeScript = """
        local current = redis.call('GET', KEYS[1])
        local persisted = tonumber(ARGV[1])
        if (not current) or tonumber(current) < persisted then
            redis.call('SET', KEYS[1], persisted, 'EX', ARGV[2])
            return persisted
        end
        redis.call('EXPIRE', KEYS[1], ARGV[2])
        return tonumber(current)
        """;

    private readonly Lazy<IConnectionMultiplexer>? _connection;
    private readonly RedisOptions _options;
    private readonly ILogger<RedisAiQuotaCounter> _logger;

    /// <summary>
    /// Инициализирует быстрый счётчик общим подключением Redis и изолированным пространством ключей.
    /// Пустая строка подключения не считается ошибкой конфигурации — счётчик просто работает
    /// в режиме отката на PostgreSQL-значения без Redis.
    /// </summary>
    /// <param name="options">Настройки префикса ключей.</param>
    /// <param name="logger">Журнал для диагностики недоступности Redis без падения запроса.</param>
    public RedisAiQuotaCounter(IOptions<RedisOptions> options, ILogger<RedisAiQuotaCounter> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _options = options.Value;
        _logger = logger;
        ArgumentException.ThrowIfNullOrWhiteSpace(_options.KeyPrefix);
        _connection = string.IsNullOrWhiteSpace(_options.ConnectionString)
            ? null
            : new Lazy<IConnectionMultiplexer>(CreateConnection);
    }

    /// <summary>
    /// Инициализирует отсутствующий ключ персистентным значением и проверяет остаток без резервирования.
    /// Безлимитный тариф не обращается к Redis и не создаёт лишних ключей.
    /// </summary>
    /// <param name="lawyerId">Идентификатор владельца квоты.</param>
    /// <param name="quotaId">Идентификатор периода квоты.</param>
    /// <param name="requestsUsed">Персистентное количество использований.</param>
    /// <param name="requestsLimit">Лимит либо отсутствие лимита.</param>
    /// <param name="periodEnd">Конец периода и срок жизни ключа.</param>
    /// <param name="cancellationToken">Токен отмены ожидания Redis.</param>
    /// <returns>Признак наличия доступного запроса.</returns>
    public async Task<bool> IsAvailableAsync(
        Guid lawyerId,
        Guid quotaId,
        int requestsUsed,
        int? requestsLimit,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken)
    {
        if (!requestsLimit.HasValue)
        {
            return true;
        }

        if (_connection is null)
        {
            return requestsUsed < requestsLimit.Value;
        }

        try
        {
            var database = _connection.Value.GetDatabase();
            var key = BuildKey(lawyerId, quotaId);
            var lifetime = CalculateLifetime(periodEnd);
            await database.StringSetAsync(key, requestsUsed, lifetime, When.NotExists)
                .WaitAsync(cancellationToken);
            var value = await database.StringGetAsync(key).WaitAsync(cancellationToken);
            return value.HasValue && (long)value < requestsLimit.Value;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Redis недоступен при проверке ИИ-квоты — используется значение из PostgreSQL.");
            return requestsUsed < requestsLimit.Value;
        }
    }

    /// <summary>
    /// Атомарно резервирует один запрос до обращения к Gemini и отклоняет увеличение при достигнутом лимите.
    /// </summary>
    /// <param name="lawyerId">Идентификатор владельца квоты.</param>
    /// <param name="quotaId">Идентификатор периода квоты.</param>
    /// <param name="requestsUsed">Персистентное начальное значение.</param>
    /// <param name="requestsLimit">Лимит либо отсутствие лимита.</param>
    /// <param name="periodEnd">Конец периода квоты.</param>
    /// <param name="cancellationToken">Токен отмены ожидания Redis.</param>
    /// <returns>Признак успешного резервирования.</returns>
    public async Task<bool> TryReserveAsync(
        Guid lawyerId,
        Guid quotaId,
        int requestsUsed,
        int? requestsLimit,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken)
    {
        if (!requestsLimit.HasValue)
        {
            return true;
        }

        if (_connection is null)
        {
            return requestsUsed < requestsLimit.Value;
        }

        try
        {
            var result = await _connection.Value.GetDatabase()
                .ScriptEvaluateAsync(
                    ReserveScript,
                    [BuildKey(lawyerId, quotaId)],
                    [requestsUsed, requestsLimit.Value, CalculateLifetimeSeconds(periodEnd)])
                .WaitAsync(cancellationToken);
            return (long)result >= 0;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Redis недоступен при резервировании ИИ-квоты — используется значение из PostgreSQL.");
            return requestsUsed < requestsLimit.Value;
        }
    }

    /// <summary>
    /// Поднимает Redis-счётчик до успешно сохранённого PostgreSQL-значения, не уменьшая уже выполненные параллельные резервы.
    /// </summary>
    /// <param name="lawyerId">Идентификатор владельца квоты.</param>
    /// <param name="quotaId">Идентификатор периода квоты.</param>
    /// <param name="requestsUsed">Зафиксированное персистентное значение.</param>
    /// <param name="periodEnd">Конец периода квоты.</param>
    /// <param name="cancellationToken">Токен отмены ожидания Redis.</param>
    /// <returns>Задача синхронизации быстрого счётчика.</returns>
    public async Task SynchronizeAsync(
        Guid lawyerId,
        Guid quotaId,
        int requestsUsed,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken)
    {
        if (_connection is null)
        {
            return;
        }

        try
        {
            await _connection.Value.GetDatabase()
                .ScriptEvaluateAsync(
                    SynchronizeScript,
                    [BuildKey(lawyerId, quotaId)],
                    [requestsUsed, CalculateLifetimeSeconds(periodEnd)])
                .WaitAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Redis недоступен при синхронизации ИИ-квоты — пропущено без ошибки.");
        }
    }

    /// <summary>
    /// Освобождает подключение Redis только если оно действительно потребовалось хотя бы одной ИИ-операции.
    /// </summary>
    public void Dispose()
    {
        if (_connection is { IsValueCreated: true })
        {
            _connection.Value.Dispose();
        }
    }

    /// <summary>Лениво создаёт потокобезопасное подключение и разрешает восстановление после временной недоступности Redis.</summary>
    /// <returns>Единое подключение Redis для всего процесса.</returns>
    private IConnectionMultiplexer CreateConnection()
    {
        var configuration = ConfigurationOptions.Parse(_options.ConnectionString);
        configuration.AbortOnConnectFail = false;
        return ConnectionMultiplexer.Connect(configuration);
    }

    /// <summary>Формирует tenant-изолированный ключ конкретного периода квоты.</summary>
    /// <param name="lawyerId">Идентификатор юриста.</param>
    /// <param name="quotaId">Идентификатор квоты.</param>
    /// <returns>Стабильный Redis-ключ.</returns>
    private RedisKey BuildKey(Guid lawyerId, Guid quotaId)
    {
        return $"{_options.KeyPrefix}:ai-quota:{lawyerId:N}:{quotaId:N}";
    }

    /// <summary>Вычисляет положительный срок жизни ключа до конца квоты.</summary>
    /// <param name="periodEnd">Конец периода.</param>
    /// <returns>Положительный срок жизни.</returns>
    private static TimeSpan CalculateLifetime(DateTimeOffset periodEnd)
    {
        return TimeSpan.FromSeconds(CalculateLifetimeSeconds(periodEnd));
    }

    /// <summary>Вычисляет целое число секунд для Redis EX без нулевого значения.</summary>
    /// <param name="periodEnd">Конец периода.</param>
    /// <returns>Не менее одной секунды.</returns>
    private static long CalculateLifetimeSeconds(DateTimeOffset periodEnd)
    {
        return Math.Max(1, (long)Math.Ceiling((periodEnd - DateTimeOffset.UtcNow).TotalSeconds));
    }
}
