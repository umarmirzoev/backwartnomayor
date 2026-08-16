using Application.Features.ClientPortal;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Options;
using WebApi.Security;

namespace WebApi.Background;

/// <summary>
/// Периодически выбирает из PostgreSQL просроченные отправленные документы и проводит их через обычную CQRS-команду.
/// Срок хранится в Draft, поэтому задача переживает перезапуск процесса, а системный AsyncLocal-контекст не доступен HTTP-клиентам.
/// </summary>
public sealed class DraftExpirationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SystemExecutionContext _systemExecutionContext;
    private readonly BackgroundProcessingOptions _options;
    private readonly ILogger<DraftExpirationBackgroundService> _logger;

    /// <summary>Инициализирует долговечный poller scoped-фабрикой, системным контекстом и проверяемыми настройками.</summary>
    /// <param name="scopeFactory">Фабрика области DbContext и MediatR.</param>
    /// <param name="systemExecutionContext">Доверенная область системного исполнения.</param>
    /// <param name="options">Интервал и размер пачки.</param>
    /// <param name="logger">Журнал технических результатов.</param>
    public DraftExpirationBackgroundService(
        IServiceScopeFactory scopeFactory,
        SystemExecutionContext systemExecutionContext,
        IOptions<BackgroundProcessingOptions> options,
        ILogger<DraftExpirationBackgroundService> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(systemExecutionContext);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _scopeFactory = scopeFactory;
        _systemExecutionContext = systemExecutionContext;
        _options = options.Value;
        _logger = logger;
        if (_options.PollIntervalSeconds is < 10 or > 3600 || _options.BatchSize is < 1 or > 500)
        {
            throw new InvalidOperationException("Параметры фоновой обработки документов недопустимы.");
        }
    }

    /// <summary>
    /// Выполняет первую проверку при запуске, затем повторяет её по PeriodicTimer до штатной остановки приложения.
    /// </summary>
    /// <param name="stoppingToken">Токен остановки хоста.</param>
    /// <returns>Долгоживущая задача фонового работника.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ProcessBatchSafelyAsync(stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.PollIntervalSeconds));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessBatchSafelyAsync(stoppingToken);
        }
    }

    /// <summary>Изолирует временный сбой одной итерации, но сохраняет отмену штатной остановки.</summary>
    /// <param name="cancellationToken">Токен остановки.</param>
    private async Task ProcessBatchSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await ProcessBatchAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Ошибка итерации фоновой проверки просроченных документов.");
        }
    }

    /// <summary>Выбирает одну ограниченную пачку и отправляет каждый идентификатор в MediatR с системным признаком.</summary>
    /// <param name="cancellationToken">Токен остановки.</param>
    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDraftRepository>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var drafts = await repository.GetExpiredBatchAsync(
            clock.UtcNow,
            _options.BatchSize,
            cancellationToken);

        using var systemScope = _systemExecutionContext.Enter();
        foreach (var draft in drafts)
        {
            var result = await sender.Send(new MarkDraftExpiredCommand(draft.Id), cancellationToken);
            if (!result.IsSuccess)
            {
                _logger.LogWarning(
                    "Фоновая команда просрочки документа {DraftId} завершилась HTTP-кодом {StatusCode}.",
                    draft.Id,
                    (int)result.StatusCode);
            }
        }
    }
}
