using Domain.Entities;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Определяет append-only хранение фактов обращения к ИИ для аудита и будущего биллинга.
/// </summary>
public interface IAiUsageRecordRepository
{
    /// <summary>
    /// Добавляет неизменяемую запись расходования квоты в текущую единицу работы.
    /// </summary>
    /// <param name="record">Созданная квотой запись использования.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Задача постановки записи на добавление.</returns>
    Task AddAsync(AiUsageRecord record, CancellationToken cancellationToken);

    /// <summary>
    /// Возвращает страницу ИИ-запросов юриста за полуоткрытый временной интервал.
    /// </summary>
    /// <param name="lawyerId">Идентификатор профиля юриста.</param>
    /// <param name="periodStart">Начало интервала включительно.</param>
    /// <param name="periodEnd">Конец интервала исключительно.</param>
    /// <param name="skip">Количество пропускаемых строк.</param>
    /// <param name="take">Максимальное количество строк.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Страница фактов использования в обратной хронологии.</returns>
    Task<IReadOnlyList<AiUsageRecord>> GetByLawyerAsync(
        Guid lawyerId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        int skip,
        int take,
        CancellationToken cancellationToken);

    /// <summary>
    /// Подсчитывает ИИ-запросы юриста за временной интервал.
    /// </summary>
    /// <param name="lawyerId">Идентификатор профиля юриста.</param>
    /// <param name="periodStart">Начало интервала включительно.</param>
    /// <param name="periodEnd">Конец интервала исключительно.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Количество фактов использования.</returns>
    Task<int> CountByLawyerAsync(
        Guid lawyerId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken);
}
