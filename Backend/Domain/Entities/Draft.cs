using Domain.Common;
using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities;

/// <summary>
/// Представляет главный агрегат договорного документа.
/// Агрегат управляет жизненным циклом документа, указателем на неизменяемую текущую версию
/// и обязательным подтверждением ответственности перед экспортом.
/// </summary>
public sealed class Draft : AggregateRoot
{
    /// <summary>
    /// Инициализирует черновик при материализации сохранённых данных ORM.
    /// </summary>
    private Draft()
    {
    }

    /// <summary>
    /// Создаёт пустой агрегат черновика до добавления первой версии содержимого.
    /// Временное отсутствие <see cref="CurrentVersionId"/> необходимо для корректной вставки
    /// взаимосвязанных строк черновика и первой версии в одной транзакции.
    /// </summary>
    /// <param name="caseId">Идентификатор дела.</param>
    /// <param name="templateId">Идентификатор выбранного шаблона.</param>
    /// <param name="createdAt">Момент создания черновика в UTC.</param>
    public Draft(Guid caseId, Guid templateId, DateTimeOffset createdAt)
        : base(Guid.NewGuid())
    {
        CaseId = Guard.AgainstEmpty(caseId, "идентификатор дела");
        TemplateId = Guard.AgainstEmpty(templateId, "идентификатор шаблона");
        CreatedAt = Guard.AgainstDefault(createdAt, "дата создания");
        UpdatedAt = CreatedAt;
        Status = DocumentStatus.Draft;
    }

    /// <summary>Получает идентификатор дела, к которому относится документ.</summary>
    public Guid CaseId { get; private set; }

    /// <summary>Получает идентификатор шаблона, использованного для сборки документа.</summary>
    public Guid TemplateId { get; private set; }

    /// <summary>Получает текущее состояние жизненного цикла документа.</summary>
    public DocumentStatus Status { get; private set; }

    /// <summary>Получает идентификатор текущей неизменяемой версии документа.</summary>
    public Guid? CurrentVersionId { get; private set; }

    /// <summary>Получает момент подтверждения ответственности за текущую версию.</summary>
    public DateTimeOffset? ResponsibilityConfirmedAt { get; private set; }

    /// <summary>Получает предельный срок ответа клиента в Post-MVP-сценарии.</summary>
    public DateTimeOffset? DueRespondByDate { get; private set; }

    /// <summary>Получает момент архивации документа.</summary>
    public DateTimeOffset? ArchivedAt { get; private set; }

    /// <summary>Получает момент создания агрегата.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Получает момент последнего доменного изменения агрегата.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Создаёт первую ИИ-версию документа и делает её текущей.
    /// Операция разрешена ровно один раз для каждого черновика.
    /// </summary>
    /// <param name="contentStorageKey">Ключ содержимого в объектном хранилище.</param>
    /// <param name="createdByLawyerId">Идентификатор профиля юриста-создателя.</param>
    /// <param name="createdAt">Момент создания версии в UTC.</param>
    /// <returns>Новая неизменяемая первая версия.</returns>
    public DocumentVersion CreateInitialVersion(
        string contentStorageKey,
        Guid createdByLawyerId,
        DateTimeOffset createdAt)
    {
        if (CurrentVersionId.HasValue)
        {
            throw new DomainException("Первая версия документа уже создана.");
        }

        EnsureDraftCanBeEdited();
        createdAt = ValidateChangeTime(createdAt, "дата создания версии");

        var version = new DocumentVersion(
            Id,
            1,
            contentStorageKey,
            null,
            DocumentVersionSource.AiGenerated,
            createdByLawyerId,
            createdAt);

        CurrentVersionId = version.Id;
        ResponsibilityConfirmedAt = null;
        UpdatedAt = createdAt;
        return version;
    }

    /// <summary>
    /// Создаёт очередную ручную или повторно сгенерированную версию и делает её текущей.
    /// Подтверждение ответственности сбрасывается, поскольку оно относилось к предыдущему тексту.
    /// </summary>
    /// <param name="versionNumber">Следующий уникальный номер версии внутри черновика.</param>
    /// <param name="contentStorageKey">Ключ нового содержимого в объектном хранилище.</param>
    /// <param name="changeSummary">Описание изменений относительно предыдущей версии.</param>
    /// <param name="source">Источник новой версии.</param>
    /// <param name="createdByLawyerId">Идентификатор профиля юриста-создателя.</param>
    /// <param name="createdAt">Момент создания версии в UTC.</param>
    /// <returns>Новая неизменяемая версия документа.</returns>
    public DocumentVersion CreateNextVersion(
        int versionNumber,
        string contentStorageKey,
        string? changeSummary,
        DocumentVersionSource source,
        Guid createdByLawyerId,
        DateTimeOffset createdAt)
    {
        if (!CurrentVersionId.HasValue)
        {
            throw new DomainException("Нельзя создать следующую версию до создания первой версии документа.");
        }

        EnsureDraftCanBeEdited();
        Guard.Against(versionNumber <= 1, "Номер следующей версии документа должен быть больше единицы.");
        createdAt = ValidateChangeTime(createdAt, "дата создания версии");

        var version = new DocumentVersion(
            Id,
            versionNumber,
            contentStorageKey,
            changeSummary,
            source,
            createdByLawyerId,
            createdAt);

        CurrentVersionId = version.Id;
        ResponsibilityConfirmedAt = null;
        UpdatedAt = createdAt;
        return version;
    }

    /// <summary>
    /// Фиксирует явное подтверждение юриста, что текущая версия проверена и ответственность принята.
    /// Повторный вызов является идемпотентным и сохраняет момент первого подтверждения.
    /// </summary>
    /// <param name="confirmedAt">Момент подтверждения в UTC.</param>
    public void ConfirmResponsibility(DateTimeOffset confirmedAt)
    {
        if (ResponsibilityConfirmedAt.HasValue)
        {
            return;
        }

        if (!CurrentVersionId.HasValue)
        {
            throw new DomainException("Нельзя подтвердить ответственность до создания версии документа.");
        }

        if (Status != DocumentStatus.Draft)
        {
            throw new DomainException("Подтверждение ответственности разрешено только для проверенного черновика.");
        }

        confirmedAt = ValidateChangeTime(confirmedAt, "дата подтверждения ответственности");
        ResponsibilityConfirmedAt = confirmedAt;
        UpdatedAt = confirmedAt;
    }

    /// <summary>
    /// Проверяет обязательные доменные предусловия экспорта текущей версии документа.
    /// </summary>
    /// <exception cref="DomainException">
    /// Выбрасывается при отсутствии версии или подтверждения ответственности.
    /// </exception>
    public void EnsureCanExport()
    {
        if (Status == DocumentStatus.Deleted)
        {
            throw new DomainException("Нельзя экспортировать удалённый документ.");
        }

        if (!CurrentVersionId.HasValue)
        {
            throw new DomainException("Нельзя экспортировать документ без созданной версии.");
        }

        if (!ResponsibilityConfirmedAt.HasValue)
        {
            throw new DomainException(
                "Экспорт запрещён: юрист должен подтвердить проверку документа и принять ответственность.");
        }
    }

    /// <summary>
    /// Изменяет состояние документа только по переходам, зафиксированным продуктовым жизненным циклом.
    /// </summary>
    /// <param name="newStatus">Целевое состояние документа.</param>
    /// <param name="changedAt">Момент перехода в UTC.</param>
    public void ChangeStatus(DocumentStatus newStatus, DateTimeOffset changedAt)
    {
        newStatus = Guard.AgainstInvalidEnum(newStatus, "статус документа");

        if (Status == newStatus)
        {
            return;
        }

        if (!IsTransitionAllowed(Status, newStatus))
        {
            throw new DomainException($"Переход документа из состояния «{Status}» в «{newStatus}» запрещён.");
        }

        changedAt = ValidateChangeTime(changedAt, "дата изменения статуса");

        if (Status == DocumentStatus.Draft
            && newStatus != DocumentStatus.Deleted
            && !CurrentVersionId.HasValue)
        {
            throw new DomainException("Нельзя изменить состояние черновика до создания первой версии.");
        }

        if (newStatus == DocumentStatus.Expired)
        {
            if (!DueRespondByDate.HasValue)
            {
                throw new DomainException("Нельзя отметить документ просроченным без установленного срока ответа.");
            }

            if (changedAt < DueRespondByDate.Value)
            {
                throw new DomainException("Документ нельзя отметить просроченным до наступления срока ответа.");
            }
        }

        Status = newStatus;
        UpdatedAt = changedAt;

        if (newStatus == DocumentStatus.Archived)
        {
            ArchivedAt = changedAt;
        }

        if (newStatus == DocumentStatus.Draft)
        {
            ResponsibilityConfirmedAt = null;
        }
    }

    /// <summary>
    /// Устанавливает предельный срок ответа клиента после отправки документа.
    /// </summary>
    /// <param name="dueRespondByDate">Будущий срок ответа клиента в UTC.</param>
    public void SetResponseDueDate(DateTimeOffset dueRespondByDate)
    {
        if (Status != DocumentStatus.SentToClient)
        {
            throw new DomainException("Срок ответа можно назначить только документу, отправленному клиенту.");
        }

        dueRespondByDate = Guard.AgainstDefault(dueRespondByDate, "срок ответа клиента");
        Guard.Against(dueRespondByDate <= UpdatedAt, "Срок ответа клиента должен быть позже момента отправки.");
        DueRespondByDate = dueRespondByDate;
    }

    /// <summary>
    /// Проверяет, что черновик находится в состоянии, допускающем изменение текста.
    /// </summary>
    private void EnsureDraftCanBeEdited()
    {
        if (Status != DocumentStatus.Draft)
        {
            throw new DomainException("Создание новой версии разрешено только для документа в состоянии черновика.");
        }
    }

    /// <summary>
    /// Проверяет хронологическую корректность доменного изменения агрегата.
    /// </summary>
    /// <param name="changedAt">Проверяемый момент изменения.</param>
    /// <param name="fieldName">Русское название даты для сообщения об ошибке.</param>
    /// <returns>Проверенный момент изменения.</returns>
    private DateTimeOffset ValidateChangeTime(DateTimeOffset changedAt, string fieldName)
    {
        changedAt = Guard.AgainstDefault(changedAt, fieldName);
        Guard.Against(changedAt < UpdatedAt, "Дата изменения документа не может быть раньше предыдущего изменения.");
        return changedAt;
    }

    /// <summary>
    /// Определяет допустимость перехода между состояниями согласно state-диаграмме продукта.
    /// </summary>
    /// <param name="current">Текущее состояние.</param>
    /// <param name="target">Целевое состояние.</param>
    /// <returns><see langword="true"/>, если переход разрешён.</returns>
    private static bool IsTransitionAllowed(DocumentStatus current, DocumentStatus target)
    {
        return (current, target) switch
        {
            (DocumentStatus.Draft, DocumentStatus.PendingFirmApproval) => true,
            (DocumentStatus.Draft, DocumentStatus.SentToClient) => true,
            (DocumentStatus.Draft, DocumentStatus.Archived) => true,
            (DocumentStatus.Draft, DocumentStatus.Deleted) => true,
            (DocumentStatus.PendingFirmApproval, DocumentStatus.Draft) => true,
            (DocumentStatus.PendingFirmApproval, DocumentStatus.SentToClient) => true,
            (DocumentStatus.SentToClient, DocumentStatus.AcceptedByClient) => true,
            (DocumentStatus.SentToClient, DocumentStatus.RevisionsRequested) => true,
            (DocumentStatus.SentToClient, DocumentStatus.RejectedByClient) => true,
            (DocumentStatus.SentToClient, DocumentStatus.Expired) => true,
            (DocumentStatus.SentToClient, DocumentStatus.RevokedByLawyer) => true,
            (DocumentStatus.RevisionsRequested, DocumentStatus.Draft) => true,
            (DocumentStatus.AcceptedByClient, DocumentStatus.AwaitingSignature) => true,
            (DocumentStatus.AwaitingSignature, DocumentStatus.Signed) => true,
            (DocumentStatus.AwaitingSignature, DocumentStatus.RejectedByClient) => true,
            (DocumentStatus.Signed, DocumentStatus.RequiresUpdate) => true,
            (DocumentStatus.Signed, DocumentStatus.Archived) => true,
            (DocumentStatus.RequiresUpdate, DocumentStatus.Draft) => true,
            (DocumentStatus.RejectedByClient, DocumentStatus.Archived) => true,
            (DocumentStatus.Expired, DocumentStatus.Archived) => true,
            (DocumentStatus.RevokedByLawyer, DocumentStatus.Archived) => true,
            _ => false
        };
    }
}
