namespace Application.DTOs;

/// <summary>
/// Представляет упорядоченную связь шаблона с договорным пунктом.
/// </summary>
/// <param name="Id">Идентификатор связи.</param>
/// <param name="TemplateId">Идентификатор шаблона.</param>
/// <param name="ClauseBlockId">Идентификатор договорного пункта.</param>
/// <param name="IsDefault">Признак включения пункта по умолчанию.</param>
/// <param name="Order">Позиция пункта внутри шаблона.</param>
public sealed record GetTemplateClauseBlockDto(
    Guid Id,
    Guid TemplateId,
    Guid ClauseBlockId,
    bool IsDefault,
    int Order);

/// <summary>
/// Представляет детальную связь шаблона и пункта с готовыми вложенными моделями для клиентского интерфейса.
/// Вложенные данные заполняются CQRS-проекцией без N+1-запросов, поскольку доменная сущность не содержит навигаций.
/// </summary>
/// <param name="Id">Идентификатор связи.</param>
/// <param name="TemplateId">Идентификатор шаблона.</param>
/// <param name="ClauseBlockId">Идентификатор пункта.</param>
/// <param name="IsDefault">Признак включения по умолчанию.</param>
/// <param name="Order">Позиция пункта.</param>
/// <param name="Template">Краткая модель шаблона.</param>
/// <param name="ClauseBlock">Краткая модель договорного пункта.</param>
public sealed record TemplateClauseBlockDetailDto(
    Guid Id,
    Guid TemplateId,
    Guid ClauseBlockId,
    bool IsDefault,
    int Order,
    GetTemplateDto? Template,
    GetClauseBlockDto? ClauseBlock);

/// <summary>
/// Представляет данные для прикрепления пункта к шаблону.
/// Обработчик проверяет уникальность пары и позиции до создания доменной сущности.
/// </summary>
/// <param name="TemplateId">Идентификатор шаблона.</param>
/// <param name="ClauseBlockId">Идентификатор пункта.</param>
/// <param name="IsDefault">Признак включения по умолчанию.</param>
/// <param name="Order">Положительная позиция пункта.</param>
public sealed record CreateTemplateClauseBlockDto(
    Guid TemplateId,
    Guid ClauseBlockId,
    bool IsDefault,
    int Order);

/// <summary>
/// Представляет разрешённые изменения порядка и признака включения связи шаблона с пунктом.
/// Идентификаторы сторон связи неизменяемы; замена пункта выполняется отсоединением и новым присоединением.
/// </summary>
/// <param name="IsDefault">Новый признак включения по умолчанию.</param>
/// <param name="Order">Новая положительная позиция.</param>
public sealed record UpdateTemplateClauseBlockDto(
    bool IsDefault,
    int Order);
