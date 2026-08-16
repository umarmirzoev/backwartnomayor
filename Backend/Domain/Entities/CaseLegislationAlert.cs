using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Представляет связь изменения законодательства с конкретным делом
/// и хранит индивидуальное состояние прочтения уведомления юристом.
/// </summary>
public sealed class CaseLegislationAlert : BaseEntity
{
    /// <summary>
    /// Инициализирует связь уведомления и дела при материализации сохранённых данных ORM.
    /// </summary>
    private CaseLegislationAlert()
    {
    }

    /// <summary>
    /// Создаёт непрочитанную связь уведомления с делом.
    /// </summary>
    /// <param name="caseId">Идентификатор затронутого дела.</param>
    /// <param name="legislationAlertId">Идентификатор уведомления.</param>
    public CaseLegislationAlert(Guid caseId, Guid legislationAlertId)
        : base(Guid.NewGuid())
    {
        CaseId = Guard.AgainstEmpty(caseId, "идентификатор дела");
        LegislationAlertId = Guard.AgainstEmpty(legislationAlertId, "идентификатор уведомления");
        IsRead = false;
    }

    /// <summary>Получает идентификатор затронутого дела.</summary>
    public Guid CaseId { get; private set; }

    /// <summary>Получает идентификатор уведомления законодательства.</summary>
    public Guid LegislationAlertId { get; private set; }

    /// <summary>Получает признак прочтения уведомления по данному делу.</summary>
    public bool IsRead { get; private set; }

    /// <summary>Получает момент прочтения уведомления.</summary>
    public DateTimeOffset? ReadAt { get; private set; }

    /// <summary>
    /// Отмечает уведомление прочитанным. Повторный вызов сохраняет первоначальный момент прочтения.
    /// </summary>
    /// <param name="readAt">Момент прочтения в UTC.</param>
    public void MarkRead(DateTimeOffset readAt)
    {
        if (IsRead)
        {
            return;
        }

        ReadAt = Guard.AgainstDefault(readAt, "дата прочтения");
        IsRead = true;
    }
}
