using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Определяет хранение формальных запросов на необратимое удаление или анонимизацию данных.
/// </summary>
public interface IDataDeletionRequestRepository : IBaseRepository<DataDeletionRequest>
{
    /// <summary>
    /// Возвращает ожидающий запрос для полиморфной цели, если он уже зарегистрирован.
    /// </summary>
    /// <param name="targetType">Тип целевой сущности.</param>
    /// <param name="targetId">Идентификатор целевой сущности.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Ожидающий запрос или <see langword="null"/>.</returns>
    Task<DataDeletionRequest?> GetPendingByTargetAsync(
        DeletionTargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Проверяет наличие ожидающего запроса для предотвращения дублирующего workflow.
    /// </summary>
    /// <param name="targetType">Тип целевой сущности.</param>
    /// <param name="targetId">Идентификатор целевой сущности.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns><see langword="true"/>, если запрос уже ожидает исполнения.</returns>
    Task<bool> ExistsPendingForTargetAsync(
        DeletionTargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Возвращает ограниченную пачку ожидающих запросов для фоновой обработки.
    /// </summary>
    /// <param name="batchSize">Максимальный размер пачки.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Запросы в порядке их регистрации.</returns>
    Task<IReadOnlyList<DataDeletionRequest>> GetPendingBatchAsync(
        int batchSize,
        CancellationToken cancellationToken);
}
