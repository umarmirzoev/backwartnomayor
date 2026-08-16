using Domain.Enums;

namespace Application.DTOs;

/// <summary>
/// Представляет активный шаблон в каталоге выбора типа договора.
/// </summary>
/// <param name="Id">Идентификатор шаблона.</param>
/// <param name="Name">Название типа договора.</param>
/// <param name="Description">Краткое описание назначения.</param>
/// <param name="Language">Строковое имя поддерживаемого языка.</param>
public sealed record GetTemplateDto(
    Guid Id,
    string Name,
    string? Description,
    string Language);

/// <summary>
/// Представляет полную карточку шаблона для административного управления каталогом.
/// Константа нотариального требования отражает доменное исключение нотариальных документов из продукта.
/// </summary>
/// <param name="Id">Идентификатор шаблона.</param>
/// <param name="Name">Название шаблона.</param>
/// <param name="Description">Описание шаблона.</param>
/// <param name="Language">Строковое имя языка.</param>
/// <param name="IsActive">Признак доступности шаблона для новых черновиков.</param>
/// <param name="RequiresNotary">Всегда ложный признак нотариального заверения.</param>
/// <param name="MaintainedByRef">Ссылка на ответственного за актуальность.</param>
/// <param name="CreatedAt">Дата создания.</param>
/// <param name="UpdatedAt">Дата последнего изменения.</param>
public sealed record TemplateDetailDto(
    Guid Id,
    string Name,
    string? Description,
    string Language,
    bool IsActive,
    bool RequiresNotary,
    string? MaintainedByRef,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Представляет данные для добавления нового ненотариального шаблона в каталог.
/// Активность и временные метки назначаются доменной сущностью.
/// </summary>
/// <param name="Name">Название шаблона.</param>
/// <param name="Description">Описание назначения шаблона.</param>
/// <param name="Language">Поддерживаемый язык документа.</param>
/// <param name="MaintainedByRef">Ссылка на ответственного куратора.</param>
public sealed record CreateTemplateDto(
    string Name,
    string? Description,
    TemplateLanguage Language,
    string? MaintainedByRef);

/// <summary>
/// Представляет разрешённые изменения метаданных шаблона.
/// Деактивация выполняется отдельным доменным методом и не смешивается с редактированием сведений.
/// </summary>
/// <param name="Name">Новое название.</param>
/// <param name="Description">Новое описание.</param>
/// <param name="Language">Новый языковой режим.</param>
/// <param name="MaintainedByRef">Новая ссылка на куратора.</param>
public sealed record UpdateTemplateDto(
    string Name,
    string? Description,
    TemplateLanguage Language,
    string? MaintainedByRef);
