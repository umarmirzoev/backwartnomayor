using Domain.Common;
using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities;

/// <summary>
/// Представляет формальный запрос на необратимое удаление или анонимизацию доменных данных.
/// Полиморфная цель хранится как тип и идентификатор, а фактическое удаление оркестрируется Application.
/// </summary>
public sealed class DataDeletionRequest : BaseEntity
{
    /// <summary>
    /// Инициализирует запрос на удаление при материализации сохранённых данных ORM.
    /// </summary>
    private DataDeletionRequest()
    {
    }

    /// <summary>
    /// Регистрирует новый ожидающий запрос на полное удаление данных.
    /// </summary>
    /// <param name="requestedByType">Тип инициатора запроса.</param>
    /// <param name="requestedById">Идентификатор инициатора.</param>
    /// <param name="targetEntityType">Тип удаляемой сущности.</param>
    /// <param name="targetEntityId">Идентификатор удаляемой сущности.</param>
    /// <param name="requestedAt">Момент регистрации запроса в UTC.</param>
    public DataDeletionRequest(
        PartyType requestedByType,
        Guid requestedById,
        DeletionTargetType targetEntityType,
        Guid targetEntityId,
        DateTimeOffset requestedAt)
        : base(Guid.NewGuid())
    {
        RequestedByType = Guard.AgainstInvalidEnum(requestedByType, "тип инициатора удаления");
        RequestedById = Guard.AgainstEmpty(requestedById, "идентификатор инициатора удаления");
        TargetEntityType = Guard.AgainstInvalidEnum(targetEntityType, "тип цели удаления");
        TargetEntityId = Guard.AgainstEmpty(targetEntityId, "идентификатор цели удаления");
        RequestedAt = Guard.AgainstDefault(requestedAt, "дата запроса на удаление");
        Status = DataDeletionStatus.Pending;
    }

    /// <summary>Получает тип инициатора запроса.</summary>
    public PartyType RequestedByType { get; private set; }

    /// <summary>Получает идентификатор инициатора запроса.</summary>
    public Guid RequestedById { get; private set; }

    /// <summary>Получает тип сущности, данные которой требуется удалить.</summary>
    public DeletionTargetType TargetEntityType { get; private set; }

    /// <summary>Получает идентификатор сущности, данные которой требуется удалить.</summary>
    public Guid TargetEntityId { get; private set; }

    /// <summary>Получает момент регистрации запроса.</summary>
    public DateTimeOffset RequestedAt { get; private set; }

    /// <summary>Получает момент успешного завершения удаления.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Получает текущее состояние обработки запроса.</summary>
    public DataDeletionStatus Status { get; private set; }

    /// <summary>
    /// Завершает запрос после подтверждённого уничтожения или анонимизации всех требуемых данных.
    /// Повторный вызов завершённого запроса является идемпотентным.
    /// </summary>
    /// <param name="completedAt">Момент завершения удаления в UTC.</param>
    public void Complete(DateTimeOffset completedAt)
    {
        if (Status == DataDeletionStatus.Completed)
        {
            return;
        }

        EnsurePending();
        completedAt = Guard.AgainstDefault(completedAt, "дата завершения удаления");
        Guard.Against(completedAt < RequestedAt, "Дата завершения удаления не может предшествовать дате запроса.");
        CompletedAt = completedAt;
        Status = DataDeletionStatus.Completed;
    }

    /// <summary>
    /// Отклоняет ожидающий запрос. Причина отклонения должна быть зафиксирована в аудит-логе Application.
    /// </summary>
    public void Reject()
    {
        if (Status == DataDeletionStatus.Rejected)
        {
            return;
        }

        EnsurePending();
        Status = DataDeletionStatus.Rejected;
    }

    /// <summary>
    /// Проверяет, что запрос ещё не получил окончательное решение.
    /// </summary>
    private void EnsurePending()
    {
        if (Status != DataDeletionStatus.Pending)
        {
            throw new DomainException("Изменение окончательного состояния запроса на удаление запрещено.");
        }
    }
}
