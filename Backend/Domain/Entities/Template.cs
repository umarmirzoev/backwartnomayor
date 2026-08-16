using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Представляет курируемый тип договора верхнего уровня.
/// Нотариальные виды документов исключены из домена: свойство <see cref="RequiresNotary"/>
/// является константой и не допускает включения таких шаблонов через данные.
/// </summary>
public sealed class Template : BaseEntity
{
    /// <summary>
    /// Указывает, что каталог не поддерживает документы, требующие нотариального заверения.
    /// Значение не хранится в базе данных и не может быть изменено.
    /// </summary>
    public const bool RequiresNotary = false;

    /// <summary>
    /// Инициализирует шаблон при материализации сохранённых данных ORM.
    /// </summary>
    private Template()
    {
    }

    /// <summary>
    /// Создаёт активный шаблон договора.
    /// </summary>
    /// <param name="name">Название типа договора.</param>
    /// <param name="description">Описание назначения шаблона.</param>
    /// <param name="language">Поддерживаемый язык итогового документа.</param>
    /// <param name="maintainedByRef">Ссылка или текстовое обозначение куратора библиотеки.</param>
    /// <param name="createdAt">Момент создания шаблона в UTC.</param>
    public Template(
        string name,
        string? description,
        TemplateLanguage language,
        string? maintainedByRef,
        DateTimeOffset createdAt)
        : base(Guid.NewGuid())
    {
        Name = Guard.RequiredText(name, "название шаблона", 200);
        Description = Guard.OptionalText(description, "описание шаблона");
        Language = Guard.AgainstInvalidEnum(language, "язык шаблона");
        MaintainedByRef = Guard.OptionalText(maintainedByRef, "куратор шаблона", 300);
        CreatedAt = Guard.AgainstDefault(createdAt, "дата создания");
        UpdatedAt = CreatedAt;
        IsActive = true;
    }

    /// <summary>Получает название типа договора.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Получает описание шаблона.</summary>
    public string? Description { get; private set; }

    /// <summary>Получает язык итогового документа.</summary>
    public TemplateLanguage Language { get; private set; }

    /// <summary>Получает признак публикации шаблона в каталоге.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Получает текстовую ссылку на куратора актуальности шаблона.</summary>
    public string? MaintainedByRef { get; private set; }

    /// <summary>Получает момент создания шаблона.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Получает момент последнего изменения шаблона.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Изменяет реквизиты шаблона и фиксирует момент изменения.
    /// </summary>
    /// <param name="name">Новое название.</param>
    /// <param name="description">Новое описание.</param>
    /// <param name="language">Новый языковой режим.</param>
    /// <param name="maintainedByRef">Новая ссылка на куратора.</param>
    /// <param name="updatedAt">Момент изменения в UTC.</param>
    public void UpdateDetails(
        string name,
        string? description,
        TemplateLanguage language,
        string? maintainedByRef,
        DateTimeOffset updatedAt)
    {
        updatedAt = ValidateUpdateTime(updatedAt);
        Name = Guard.RequiredText(name, "название шаблона", 200);
        Description = Guard.OptionalText(description, "описание шаблона");
        Language = Guard.AgainstInvalidEnum(language, "язык шаблона");
        MaintainedByRef = Guard.OptionalText(maintainedByRef, "куратор шаблона", 300);
        UpdatedAt = updatedAt;
    }

    /// <summary>
    /// Снимает шаблон с публикации, не удаляя его и существующие ссылки документов.
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
        Guard.Against(updatedAt < UpdatedAt, "Дата изменения шаблона не может быть раньше предыдущего изменения.");
        return updatedAt;
    }
}
