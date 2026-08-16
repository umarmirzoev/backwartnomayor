using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Представляет доменный профиль юриста, связанный с учётной записью ASP.NET Core Identity.
/// Профиль хранит только бизнес-данные и не дублирует email, пароль, токены или роли Identity.
/// Он является корнем агрегата, через который определяется владелец клиентов, дел и квот.
/// </summary>
public sealed class LawyerProfile : AggregateRoot
{
    /// <summary>
    /// Инициализирует профиль при материализации сохранённых данных ORM.
    /// </summary>
    private LawyerProfile()
    {
    }

    /// <summary>
    /// Создаёт активный профиль юриста на бесплатном тарифе.
    /// </summary>
    /// <param name="userId">Идентификатор учётной записи ASP.NET Core Identity.</param>
    /// <param name="fullName">Полное имя юриста.</param>
    /// <param name="lawFirmName">Название юридической фирмы или <see langword="null"/> для частного юриста.</param>
    /// <param name="createdAt">Момент создания профиля в UTC.</param>
    public LawyerProfile(
        Guid userId,
        string fullName,
        string? lawFirmName,
        DateTimeOffset createdAt)
        : base(Guid.NewGuid())
    {
        UserId = Guard.AgainstEmpty(userId, "идентификатор пользователя Identity");
        FullName = Guard.RequiredText(fullName, "полное имя", 200);
        LawFirmName = Guard.OptionalText(lawFirmName, "название юридической фирмы", 300);
        CreatedAt = Guard.AgainstDefault(createdAt, "дата создания");
        SubscriptionTier = SubscriptionTier.Free;
        IsActive = true;
    }

    /// <summary>
    /// Получает внешний идентификатор учётной записи ASP.NET Core Identity.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Получает полное имя юриста, используемое в бизнес-представлении профиля.
    /// </summary>
    public string FullName { get; private set; } = string.Empty;

    /// <summary>
    /// Получает название юридической фирмы; отсутствие значения обозначает частного юриста.
    /// </summary>
    public string? LawFirmName { get; private set; }

    /// <summary>
    /// Получает текущий тариф, определяющий применение лимита ИИ-запросов.
    /// </summary>
    public SubscriptionTier SubscriptionTier { get; private set; }

    /// <summary>
    /// Получает признак доступности профиля для работы в системе.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Получает момент создания профиля.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Изменяет бизнес-данные профиля, не затрагивая учётные данные Identity.
    /// </summary>
    /// <param name="fullName">Новое полное имя.</param>
    /// <param name="lawFirmName">Новое название юридической фирмы.</param>
    public void UpdateDetails(string fullName, string? lawFirmName)
    {
        FullName = Guard.RequiredText(fullName, "полное имя", 200);
        LawFirmName = Guard.OptionalText(lawFirmName, "название юридической фирмы", 300);
    }

    /// <summary>
    /// Изменяет тариф профиля без создания платёжной сущности, поскольку биллинг находится вне MVP.
    /// </summary>
    /// <param name="tier">Новый поддерживаемый тариф.</param>
    public void ChangeSubscriptionTier(SubscriptionTier tier)
    {
        SubscriptionTier = Guard.AgainstInvalidEnum(tier, "тариф подписки");
    }

    /// <summary>
    /// Деактивирует профиль без удаления связанных бизнес-данных.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Повторно активирует ранее деактивированный профиль.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }
}
