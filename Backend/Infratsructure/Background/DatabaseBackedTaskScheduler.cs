using Application.Interfaces.Services;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Background;

/// <summary>
/// Подтверждает долговечную постановку фоновых задач через уже сохранённое состояние PostgreSQL.
/// Запрос удаления хранится отдельной сущностью, а срок документа — полем Draft; фоновые работники могут безопасно
/// повторно опрашивать эти записи после перезапуска процесса без ненадёжного fire-and-forget.
/// </summary>
public sealed class DatabaseBackedTaskScheduler : IBackgroundTaskScheduler
{
    private readonly AppDbContext _dbContext;

    /// <summary>Инициализирует планировщик scoped-контекстом, в котором уведомление публикуется после фиксации данных.</summary>
    /// <param name="dbContext">Контекст долговечного состояния задач.</param>
    public DatabaseBackedTaskScheduler(AppDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <summary>
    /// Проверяет существование сохранённого pending-запроса; сама запись является идемпотентным элементом очереди.
    /// </summary>
    /// <param name="requestId">Идентификатор запроса удаления.</param>
    /// <param name="cancellationToken">Токен отмены проверки.</param>
    /// <returns>Завершённая задача после подтверждения долговечности записи.</returns>
    public async Task ScheduleDataDeletionAsync(Guid requestId, CancellationToken cancellationToken)
    {
        if (!await _dbContext.DataDeletionRequests
                .AsNoTracking()
                .AnyAsync(request => request.Id == requestId, cancellationToken))
        {
            throw new InvalidOperationException("Сохранённый запрос удаления для фоновой обработки не найден.");
        }
    }

    /// <summary>
    /// Проверяет сохранённый срок черновика; периодический работник выбирает его по индексу после наступления срока.
    /// </summary>
    /// <param name="draftId">Идентификатор отправленного документа.</param>
    /// <param name="dueAt">Ожидаемый сохранённый срок ответа.</param>
    /// <param name="cancellationToken">Токен отмены проверки.</param>
    /// <returns>Завершённая задача после подтверждения долговечного расписания.</returns>
    public async Task ScheduleDraftExpirationAsync(
        Guid draftId,
        DateTimeOffset dueAt,
        CancellationToken cancellationToken)
    {
        if (!await _dbContext.Drafts
                .AsNoTracking()
                .AnyAsync(
                    draft => draft.Id == draftId && draft.DueRespondByDate == dueAt,
                    cancellationToken))
        {
            throw new InvalidOperationException("Сохранённый срок документа для фоновой проверки не найден.");
        }
    }
}
