using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Представляет упорядоченную связь шаблона с переиспользуемым пунктом договора.
/// Сущность хранит позицию и признак включения по умолчанию, поэтому не является объектом-значением.
/// </summary>
public sealed class TemplateClauseBlock : BaseEntity
{
    /// <summary>
    /// Инициализирует связь шаблона и пункта при материализации сохранённых данных ORM.
    /// </summary>
    private TemplateClauseBlock()
    {
    }

    /// <summary>
    /// Создаёт связь пункта с шаблоном.
    /// </summary>
    /// <param name="templateId">Идентификатор шаблона.</param>
    /// <param name="clauseBlockId">Идентификатор блока пункта.</param>
    /// <param name="isDefault">Признак включения пункта в документ по умолчанию.</param>
    /// <param name="order">Неотрицательная позиция пункта внутри шаблона.</param>
    public TemplateClauseBlock(Guid templateId, Guid clauseBlockId, bool isDefault, int order)
        : base(Guid.NewGuid())
    {
        TemplateId = Guard.AgainstEmpty(templateId, "идентификатор шаблона");
        ClauseBlockId = Guard.AgainstEmpty(clauseBlockId, "идентификатор пункта");
        IsDefault = isDefault;
        Order = Guard.AgainstNegative(order, "порядок пункта");
    }

    /// <summary>Получает идентификатор шаблона.</summary>
    public Guid TemplateId { get; private set; }

    /// <summary>Получает идентификатор блока пункта.</summary>
    public Guid ClauseBlockId { get; private set; }

    /// <summary>Получает признак включения пункта по умолчанию.</summary>
    public bool IsDefault { get; private set; }

    /// <summary>Получает позицию пункта внутри шаблона.</summary>
    public int Order { get; private set; }

    /// <summary>
    /// Изменяет позицию пункта внутри шаблона.
    /// Уникальность позиции в конкретном шаблоне дополнительно обеспечивается инфраструктурным слоем.
    /// </summary>
    /// <param name="order">Новая неотрицательная позиция.</param>
    public void ChangeOrder(int order)
    {
        Order = Guard.AgainstNegative(order, "порядок пункта");
    }

    /// <summary>
    /// Изменяет признак автоматического включения пункта в новый документ.
    /// </summary>
    /// <param name="isDefault">Новое значение признака.</param>
    public void SetDefault(bool isDefault)
    {
        IsDefault = isDefault;
    }
}
