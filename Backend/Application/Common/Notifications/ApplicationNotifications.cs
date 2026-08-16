using Application.Interfaces.Services;
using MediatR;

namespace Application.Common.Notifications;

/// <summary>
/// Уведомляет о сохранённом запросе полного удаления, который должен быть исполнен устойчивой фоновой задачей.
/// </summary>
/// <param name="RequestId">Идентификатор запроса удаления.</param>
public sealed record DataDeletionRequestedNotification(Guid RequestId) : INotification;

/// <summary>
/// Уведомляет об отправке документа клиенту и необходимости запланировать проверку срока ответа.
/// </summary>
/// <param name="DraftId">Идентификатор отправленного документа.</param>
/// <param name="DueAt">Крайний срок ответа клиента.</param>
public sealed record DraftExpirationScheduledNotification(
    Guid DraftId,
    DateTimeOffset DueAt) : INotification;

/// <summary>
/// Ставит исполнение сохранённого запроса удаления в устойчивую очередь.
/// </summary>
/// <param name="scheduler">Абстракция долговечного планировщика фоновых задач.</param>
public sealed class DataDeletionRequestedNotificationHandler(IBackgroundTaskScheduler scheduler)
    : INotificationHandler<DataDeletionRequestedNotification>
{
    /// <summary>
    /// Планирует идемпотентное исполнение удаления после фиксации запроса в базе данных.
    /// </summary>
    /// <param name="notification">Сохранённый запрос удаления.</param>
    /// <param name="cancellationToken">Токен отмены постановки в очередь.</param>
    /// <returns>Задача постановки фоновой операции.</returns>
    public Task Handle(
        DataDeletionRequestedNotification notification,
        CancellationToken cancellationToken)
    {
        return scheduler.ScheduleDataDeletionAsync(notification.RequestId, cancellationToken);
    }
}

/// <summary>
/// Ставит проверку просрочки отправленного документа в устойчивую очередь.
/// </summary>
/// <param name="scheduler">Абстракция долговечного планировщика.</param>
public sealed class DraftExpirationScheduledNotificationHandler(IBackgroundTaskScheduler scheduler)
    : INotificationHandler<DraftExpirationScheduledNotification>
{
    /// <summary>
    /// Планирует проверку статуса документа точно на установленный срок ответа.
    /// </summary>
    /// <param name="notification">Данные документа и срока.</param>
    /// <param name="cancellationToken">Токен отмены постановки в очередь.</param>
    /// <returns>Задача постановки фоновой операции.</returns>
    public Task Handle(
        DraftExpirationScheduledNotification notification,
        CancellationToken cancellationToken)
    {
        return scheduler.ScheduleDraftExpirationAsync(
            notification.DraftId,
            notification.DueAt,
            cancellationToken);
    }
}
