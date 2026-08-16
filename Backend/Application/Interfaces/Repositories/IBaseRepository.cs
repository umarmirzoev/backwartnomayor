using Domain.Common;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Определяет общий асинхронный контракт хранения доменных сущностей.
/// Контракт предоставляет минимальные CRUD-операции, а проверки владения и специализированные
/// выборки объявляются в репозиториях конкретных сущностей.
/// </summary>
/// <typeparam name="T">Тип доменной сущности с идентификатором <see cref="Guid"/>.</typeparam>
public interface IBaseRepository<T>
    where T : BaseEntity
{
    /// <summary>
    /// Возвращает сущность по идентификатору без отслеживания изменений.
    /// Для tenant-зависимых сценариев применяется специализированный метод с проверкой владельца.
    /// </summary>
    /// <param name="id">Идентификатор сущности.</param>
    /// <param name="cancellationToken">Токен отмены операции чтения.</param>
    /// <returns>Найденная сущность или <see langword="null"/>.</returns>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Возвращает полный набор сущностей без отслеживания изменений.
    /// Метод допустим только для небольших справочников и системных сценариев.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции чтения.</param>
    /// <returns>Снимок полного набора сущностей.</returns>
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Проверяет существование сущности с указанным идентификатором без материализации.
    /// </summary>
    /// <param name="id">Идентификатор проверяемой сущности.</param>
    /// <param name="cancellationToken">Токен отмены операции проверки.</param>
    /// <returns><see langword="true"/>, если строка существует.</returns>
    Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Добавляет новую сущность в единицу работы без немедленной фиксации транзакции.
    /// </summary>
    /// <param name="entity">Новая доменная сущность.</param>
    /// <param name="cancellationToken">Токен отмены операции добавления.</param>
    /// <returns>Задача постановки сущности на добавление.</returns>
    Task AddAsync(T entity, CancellationToken cancellationToken);

    /// <summary>
    /// Добавляет набор новых сущностей в единицу работы одним вызовом EF Core.
    /// </summary>
    /// <param name="entities">Непустой набор новых сущностей.</param>
    /// <param name="cancellationToken">Токен отмены операции добавления.</param>
    /// <returns>Задача постановки сущностей на добавление.</returns>
    Task AddRangeAsync(IReadOnlyCollection<T> entities, CancellationToken cancellationToken);

    /// <summary>
    /// Помечает изменённую доменную сущность для сохранения через единицу работы.
    /// </summary>
    /// <param name="entity">Сущность после выполнения доменного метода.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Задача регистрации изменения.</returns>
    Task UpdateAsync(T entity, CancellationToken cancellationToken);

    /// <summary>
    /// Помечает сущность для физического удаления через единицу работы.
    /// Метод допустим только в явно разрешённом доменном сценарии hard-delete.
    /// </summary>
    /// <param name="entity">Удаляемая сущность.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Задача регистрации удаления.</returns>
    Task DeleteAsync(T entity, CancellationToken cancellationToken);
}
