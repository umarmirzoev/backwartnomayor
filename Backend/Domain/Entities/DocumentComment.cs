using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Представляет Post-MVP-комментарий юриста или клиента к неизменяемой версии документа.
/// Комментарий может ссылаться на конкретный блок пункта, но не изменяет текст версии напрямую.
/// </summary>
public sealed class DocumentComment : BaseEntity
{
    /// <summary>
    /// Инициализирует комментарий при материализации сохранённых данных ORM.
    /// </summary>
    private DocumentComment()
    {
    }

    /// <summary>
    /// Создаёт неразрешённый комментарий к версии документа.
    /// </summary>
    /// <param name="documentVersionId">Идентификатор комментируемой версии.</param>
    /// <param name="authorType">Тип автора комментария.</param>
    /// <param name="authorId">Идентификатор автора.</param>
    /// <param name="clauseBlockReference">Идентификатор конкретного пункта, если комментарий адресный.</param>
    /// <param name="text">Текст комментария.</param>
    /// <param name="createdAt">Момент создания комментария в UTC.</param>
    public DocumentComment(
        Guid documentVersionId,
        PartyType authorType,
        Guid authorId,
        Guid? clauseBlockReference,
        string text,
        DateTimeOffset createdAt)
        : base(Guid.NewGuid())
    {
        DocumentVersionId = Guard.AgainstEmpty(documentVersionId, "идентификатор версии документа");
        AuthorType = Guard.AgainstInvalidEnum(authorType, "тип автора комментария");
        AuthorId = Guard.AgainstEmpty(authorId, "идентификатор автора комментария");
        Guard.Against(
            clauseBlockReference.HasValue && clauseBlockReference.Value == Guid.Empty,
            "Ссылка на пункт договора не может содержать пустой идентификатор.");
        ClauseBlockReference = clauseBlockReference;
        Text = Guard.RequiredText(text, "текст комментария");
        CreatedAt = Guard.AgainstDefault(createdAt, "дата создания комментария");
    }

    /// <summary>Получает идентификатор комментируемой версии.</summary>
    public Guid DocumentVersionId { get; private set; }

    /// <summary>Получает тип автора комментария.</summary>
    public PartyType AuthorType { get; private set; }

    /// <summary>Получает идентификатор автора комментария.</summary>
    public Guid AuthorId { get; private set; }

    /// <summary>Получает ссылку на конкретный блок пункта договора.</summary>
    public Guid? ClauseBlockReference { get; private set; }

    /// <summary>Получает неизменяемый текст комментария.</summary>
    public string Text { get; private set; } = string.Empty;

    /// <summary>Получает момент создания комментария.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Получает момент разрешения замечания.</summary>
    public DateTimeOffset? ResolvedAt { get; private set; }

    /// <summary>
    /// Отмечает замечание разрешённым. Повторный вызов сохраняет первоначальный момент разрешения.
    /// </summary>
    /// <param name="resolvedAt">Момент разрешения в UTC.</param>
    public void Resolve(DateTimeOffset resolvedAt)
    {
        if (ResolvedAt.HasValue)
        {
            return;
        }

        resolvedAt = Guard.AgainstDefault(resolvedAt, "дата разрешения комментария");
        Guard.Against(resolvedAt < CreatedAt, "Дата разрешения комментария не может предшествовать дате создания.");
        ResolvedAt = resolvedAt;
    }
}
