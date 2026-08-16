using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Представляет проверенный двуязычный пункт договора, используемый шаблонами и ИИ-сборкой.
/// Оба языковых текста обязательны, чтобы библиотека поддерживала таджикские и русские документы.
/// </summary>
public sealed class ClauseBlock : BaseEntity
{
    /// <summary>
    /// Инициализирует блок пункта при материализации сохранённых данных ORM.
    /// </summary>
    private ClauseBlock()
    {
    }

    /// <summary>
    /// Создаёт активный двуязычный блок договорного пункта.
    /// </summary>
    /// <param name="title">Краткое название пункта.</param>
    /// <param name="contentTj">Полный текст на таджикском языке.</param>
    /// <param name="contentRu">Полный текст на русском языке.</param>
    /// <param name="category">Категория для поиска и подбора.</param>
    /// <param name="createdAt">Момент создания в UTC.</param>
    public ClauseBlock(
        string title,
        string contentTj,
        string contentRu,
        string category,
        DateTimeOffset createdAt)
        : base(Guid.NewGuid())
    {
        Title = Guard.RequiredText(title, "название пункта", 300);
        ContentTj = Guard.RequiredText(contentTj, "текст пункта на таджикском языке");
        ContentRu = Guard.RequiredText(contentRu, "текст пункта на русском языке");
        Category = Guard.RequiredText(category, "категория пункта", 100);
        CreatedAt = Guard.AgainstDefault(createdAt, "дата создания");
        UpdatedAt = CreatedAt;
        IsActive = true;
    }

    /// <summary>Получает название пункта.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Получает текст пункта на таджикском языке.</summary>
    public string ContentTj { get; private set; } = string.Empty;

    /// <summary>Получает текст пункта на русском языке.</summary>
    public string ContentRu { get; private set; } = string.Empty;

    /// <summary>Получает категорию пункта для поиска и RAG-подбора.</summary>
    public string Category { get; private set; } = string.Empty;

    /// <summary>Получает признак доступности пункта для новых документов.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Получает момент создания пункта.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Получает момент последнего изменения пункта.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Обновляет оба языковых текста, название и категорию пункта.
    /// </summary>
    /// <param name="title">Новое название.</param>
    /// <param name="contentTj">Новый таджикский текст.</param>
    /// <param name="contentRu">Новый русский текст.</param>
    /// <param name="category">Новая категория.</param>
    /// <param name="updatedAt">Момент изменения в UTC.</param>
    public void UpdateContent(
        string title,
        string contentTj,
        string contentRu,
        string category,
        DateTimeOffset updatedAt)
    {
        updatedAt = ValidateUpdateTime(updatedAt);
        Title = Guard.RequiredText(title, "название пункта", 300);
        ContentTj = Guard.RequiredText(contentTj, "текст пункта на таджикском языке");
        ContentRu = Guard.RequiredText(contentRu, "текст пункта на русском языке");
        Category = Guard.RequiredText(category, "категория пункта", 100);
        UpdatedAt = updatedAt;
    }

    /// <summary>
    /// Деактивирует пункт без удаления существующих ссылок и исторических документов.
    /// </summary>
    /// <param name="updatedAt">Момент деактивации в UTC.</param>
    public void Deactivate(DateTimeOffset updatedAt)
    {
        if (!IsActive)
        {
            return;
        }

        UpdatedAt = ValidateUpdateTime(updatedAt);
        IsActive = false;
    }

    /// <summary>
    /// Проверяет хронологическую корректность момента изменения.
    /// </summary>
    /// <param name="updatedAt">Проверяемый момент изменения.</param>
    /// <returns>Проверенный момент изменения.</returns>
    private DateTimeOffset ValidateUpdateTime(DateTimeOffset updatedAt)
    {
        updatedAt = Guard.AgainstDefault(updatedAt, "дата изменения");
        Guard.Against(updatedAt < UpdatedAt, "Дата изменения пункта не может быть раньше предыдущего изменения.");
        return updatedAt;
    }
}
