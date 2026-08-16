using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Представляет дело клиента и группирует связанные договорные документы.
/// Денормализованный идентификатор юриста обеспечивает явную проверку владения в запросах
/// и должен совпадать с владельцем указанного клиента.
/// </summary>
public sealed class Case : BaseEntity
{
    /// <summary>
    /// Инициализирует дело при материализации сохранённых данных ORM.
    /// </summary>
    private Case()
    {
    }

    /// <summary>
    /// Создаёт открытое дело для клиента конкретного юриста.
    /// </summary>
    /// <param name="clientId">Идентификатор клиента.</param>
    /// <param name="lawyerId">Идентификатор доменного профиля юриста-владельца.</param>
    /// <param name="title">Название дела.</param>
    /// <param name="description">Описание обстоятельств дела.</param>
    /// <param name="createdAt">Момент создания дела в UTC.</param>
    public Case(
        Guid clientId,
        Guid lawyerId,
        string title,
        string? description,
        DateTimeOffset createdAt)
        : base(Guid.NewGuid())
    {
        ClientId = Guard.AgainstEmpty(clientId, "идентификатор клиента");
        LawyerId = Guard.AgainstEmpty(lawyerId, "идентификатор юриста");
        Title = Guard.RequiredText(title, "название дела", 300);
        Description = Guard.OptionalText(description, "описание дела");
        CreatedAt = Guard.AgainstDefault(createdAt, "дата создания");
        Status = CaseStatus.Open;
    }

    /// <summary>Получает идентификатор клиента, которому принадлежит дело.</summary>
    public Guid ClientId { get; private set; }

    /// <summary>Получает идентификатор доменного профиля юриста-владельца.</summary>
    public Guid LawyerId { get; private set; }

    /// <summary>Получает название дела.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Получает описание обстоятельств дела.</summary>
    public string? Description { get; private set; }

    /// <summary>Получает текущее состояние дела.</summary>
    public CaseStatus Status { get; private set; }

    /// <summary>Получает момент создания дела.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Получает момент закрытия дела.</summary>
    public DateTimeOffset? ClosedAt { get; private set; }

    /// <summary>
    /// Изменяет название и описание дела без смены клиента или владельца.
    /// </summary>
    /// <param name="title">Новое название дела.</param>
    /// <param name="description">Новое описание дела.</param>
    public void UpdateDetails(string title, string? description)
    {
        Title = Guard.RequiredText(title, "название дела", 300);
        Description = Guard.OptionalText(description, "описание дела");
    }

    /// <summary>
    /// Закрывает дело и фиксирует момент закрытия. Повторный вызов идемпотентен.
    /// </summary>
    /// <param name="closedAt">Момент закрытия дела в UTC.</param>
    public void Close(DateTimeOffset closedAt)
    {
        if (Status == CaseStatus.Closed)
        {
            return;
        }

        closedAt = Guard.AgainstDefault(closedAt, "дата закрытия");
        Guard.Against(closedAt < CreatedAt, "Дата закрытия дела не может предшествовать дате создания.");

        Status = CaseStatus.Closed;
        ClosedAt = closedAt;
    }
}
