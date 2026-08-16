using Application.Interfaces.Services;
using Infrastructure.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Infrastructure.Caching;

/// <summary>
/// Реализует атомарный Redis-счётчик ИИ-квоты по схеме Cache-Aside.
/// Lua-скрипты не допускают гонку параллельных запросов, а PostgreSQL-снимок используется только для начальной синхронизации.
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

    private readonly Lazy<IConnectionMultiplexer> _connection;
    private readonly RedisOptions _options;

    /// <summary>
    /// Инициализирует быстрый счётчик общим подключением Redis и изолированным пространством ключей.
    /// </summary>
    /// <param name="options">Настройки префикса ключей.</param>
    public RedisAiQuotaCounter(IOptions<RedisOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        ArgumentException.ThrowIfNullOrWhiteSpace(_options.KeyPrefix);
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new InvalidOperationException("Строка подключения Redis не настроена.");
        }

        _connection = new Lazy<IConnectionMultiplexer>(CreateConnection);
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

        var database = _connection.Value.GetDatabase();
        var key = BuildKey(lawyerId, quotaId);
        var lifetime = CalculateLifetime(periodEnd);
        await database.StringSetAsync(key, requestsUsed, lifetime, When.NotExists)
            .WaitAsync(cancellationToken);
        var value = await database.StringGetAsync(key).WaitAsync(cancellationToken);
        return value.HasValue && (long)value < requestsLimit.Value;
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

        var result = await _connection.Value.GetDatabase()
            .ScriptEvaluateAsync(
                ReserveScript,
                [BuildKey(lawyerId, quotaId)],
                [requestsUsed, requestsLimit.Value, CalculateLifetimeSeconds(periodEnd)])
            .WaitAsync(cancellationToken);
        return (long)result >= 0;
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
        await _connection.Value.GetDatabase()
            .ScriptEvaluateAsync(
                SynchronizeScript,
                [BuildKey(lawyerId, quotaId)],
                [requestsUsed, CalculateLifetimeSeconds(periodEnd)])
            .WaitAsync(cancellationToken);
    }

    /// <summary>
    /// Освобождает подключение Redis только если оно действительно потребовалось хотя бы одной ИИ-операции.
    /// </summary>
    public void Dispose()
    {
        if (_connection.IsValueCreated)
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
