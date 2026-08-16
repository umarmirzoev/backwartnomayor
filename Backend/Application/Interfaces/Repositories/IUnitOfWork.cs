namespace Application.Interfaces.Repositories;

/// <summary>
/// Определяет границу атомарной фиксации изменений нескольких репозиториев.
/// Контракт необходим сценариям создания черновика, версии и записи ИИ-квоты.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Сохраняет все накопленные изменения текущего контекста данных.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции сохранения.</param>
    /// <returns>Количество изменённых записей состояния EF Core.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Выполняет составной сценарий внутри явной транзакции базы данных.
    /// Делегат вызывает <see cref="SaveChangesAsync"/> в необходимых точках.
    /// </summary>
    /// <param name="operation">Асинхронный составной сценарий.</param>
    /// <param name="cancellationToken">Токен отмены транзакции.</param>
    /// <returns>Задача завершения транзакции.</returns>
    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken);

    /// <summary>
    /// Выполняет возвращающий результат сценарий внутри явной транзакции базы данных.
    /// </summary>
    /// <typeparam name="TResult">Тип результата сценария.</typeparam>
    /// <param name="operation">Асинхронный составной сценарий.</param>
    /// <param name="cancellationToken">Токен отмены транзакции.</param>
    /// <returns>Результат успешно зафиксированного сценария.</returns>
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken);
}
