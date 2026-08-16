using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Определяет append-only хранение юридически значимого журнала аудита.
/// Контракт намеренно не предоставляет обновление или удаление записей.
/// </summary>
public interface IAuditLogEntryRepository
{
    /// <summary>
    /// Добавляет неизменяемую запись аудита в текущую единицу работы.
    /// </summary>
    /// <param name="entry">Новая запись аудита.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Задача постановки записи на добавление.</returns>
    Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken);

    /// <summary>
    /// Возвращает страницу истории указанной полиморфной сущности в обратной хронологии.
    /// Проверка права на саму сущность выполняется tenant-безопасным репозиторием до этого вызова.
    /// </summary>
    /// <param name="entityType">Техническое имя типа сущности.</param>
    /// <param name="entityId">Идентификатор сущности.</param>
    /// <param name="action">Необязательный фильтр типа аудируемого действия.</param>
    /// <param name="skip">Количество пропускаемых строк.</param>
    /// <param name="take">Максимальное количество строк.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Страница записей аудита.</returns>
    Task<IReadOnlyList<AuditLogEntry>> GetByEntityAsync(
        string entityType,
        Guid entityId,
        AuditAction? action,
        int skip,
        int take,
        CancellationToken cancellationToken);

    /// <summary>
    /// Подсчитывает записи аудита указанной полиморфной сущности.
    /// </summary>
    /// <param name="entityType">Техническое имя типа сущности.</param>
    /// <param name="entityId">Идентификатор сущности.</param>
    /// <param name="action">Необязательный фильтр типа аудируемого действия.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Количество событий аудита.</returns>
    Task<int> CountByEntityAsync(
        string entityType,
        Guid entityId,
        AuditAction? action,
        CancellationToken cancellationToken);
}
