namespace Application.DTOs;

/// <summary>
/// Представляет краткую модель переиспользуемого договорного пункта для поиска и выбора.
/// Полные двуязычные тексты возвращаются только в детальной модели, чтобы не перегружать списочные ответы.
/// </summary>
/// <param name="Id">Идентификатор пункта.</param>
/// <param name="Title">Название пункта.</param>
/// <param name="Category">Категория пункта.</param>
/// <param name="IsActive">Признак доступности пункта для новых документов.</param>
public sealed record GetClauseBlockDto(
    Guid Id,
    string Title,
    string Category,
    bool IsActive);

/// <summary>
/// Представляет полную двуязычную карточку договорного пункта для просмотра и редактирования библиотеки.
/// </summary>
/// <param name="Id">Идентификатор пункта.</param>
/// <param name="Title">Название пункта.</param>
/// <param name="ContentTj">Текст пункта на таджикском языке.</param>
/// <param name="ContentRu">Текст пункта на русском языке.</param>
/// <param name="Category">Категория пункта.</param>
/// <param name="IsActive">Признак активности.</param>
/// <param name="CreatedAt">Дата создания.</param>
/// <param name="UpdatedAt">Дата последнего изменения.</param>
public sealed record ClauseBlockDetailDto(
    Guid Id,
    string Title,
    string ContentTj,
    string ContentRu,
    string Category,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Представляет данные для добавления проверенного двуязычного пункта в библиотеку RAG.
/// Активность и временные метки назначаются доменной сущностью.
/// </summary>
/// <param name="Title">Название пункта.</param>
/// <param name="ContentTj">Таджикская редакция текста.</param>
/// <param name="ContentRu">Русская редакция текста.</param>
/// <param name="Category">Категория пункта.</param>
public sealed record CreateClauseBlockDto(
    string Title,
    string ContentTj,
    string ContentRu,
    string Category);

/// <summary>
/// Представляет разрешённые изменения содержимого договорного пункта.
/// Деактивация выполняется отдельным сценарием и не передаётся как произвольный флаг.
/// </summary>
/// <param name="Title">Новое название.</param>
/// <param name="ContentTj">Новая таджикская редакция.</param>
/// <param name="ContentRu">Новая русская редакция.</param>
/// <param name="Category">Новая категория.</param>
public sealed record UpdateClauseBlockDto(
    string Title,
    string ContentTj,
    string ContentRu,
    string Category);
