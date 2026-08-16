using Domain.Common;
using Domain.Exceptions;

namespace Domain.Entities;

/// <summary>
/// Представляет клиента конкретного юриста: физическое лицо или организацию.
/// Сущность хранит персональные данные и поддерживает необратимую анонимизацию без удаления строки,
/// чтобы сохранить целостность связанных дел и документов.
/// </summary>
public sealed class Client : BaseEntity
{
    /// <summary>
    /// Инициализирует клиента при материализации сохранённых данных ORM.
    /// </summary>
    private Client()
    {
    }

    /// <summary>
    /// Создаёт клиента с ровно одним заполненным наименованием: именем физического лица
    /// или названием организации.
    /// </summary>
    /// <param name="lawyerId">Идентификатор доменного профиля владельца.</param>
    /// <param name="fullName">Полное имя физического лица.</param>
    /// <param name="companyName">Название организации.</param>
    /// <param name="contactPhone">Контактный телефон.</param>
    /// <param name="contactEmail">Контактный email.</param>
    /// <param name="notes">Внутренние заметки юриста.</param>
    /// <param name="createdAt">Момент создания карточки в UTC.</param>
    public Client(
        Guid lawyerId,
        string? fullName,
        string? companyName,
        string? contactPhone,
        string? contactEmail,
        string? notes,
        DateTimeOffset createdAt)
        : base(Guid.NewGuid())
    {
        LawyerId = Guard.AgainstEmpty(lawyerId, "идентификатор юриста");
        SetName(fullName, companyName);
        ContactPhone = Guard.OptionalText(contactPhone, "контактный телефон", 30);
        ContactEmail = Guard.OptionalText(contactEmail, "контактный email", 256);
        Notes = Guard.OptionalText(notes, "заметки");
        CreatedAt = Guard.AgainstDefault(createdAt, "дата создания");
    }

    /// <summary>
    /// Получает идентификатор доменного профиля юриста-владельца.
    /// </summary>
    public Guid LawyerId { get; private set; }

    /// <summary>
    /// Получает полное имя клиента-физического лица.
    /// </summary>
    public string? FullName { get; private set; }

    /// <summary>
    /// Получает название клиента-организации.
    /// </summary>
    public string? CompanyName { get; private set; }

    /// <summary>
    /// Получает контактный телефон клиента.
    /// </summary>
    public string? ContactPhone { get; private set; }

    /// <summary>
    /// Получает контактный email клиента.
    /// </summary>
    public string? ContactEmail { get; private set; }

    /// <summary>
    /// Получает внутренние заметки, которые могут содержать персональные данные.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Получает момент анонимизации персональных данных клиента.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; private set; }

    /// <summary>
    /// Получает момент создания карточки клиента.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Изменяет имя или название и контактные данные активного клиента.
    /// </summary>
    /// <param name="fullName">Новое имя физического лица.</param>
    /// <param name="companyName">Новое название организации.</param>
    /// <param name="contactPhone">Новый контактный телефон.</param>
    /// <param name="contactEmail">Новый контактный email.</param>
    /// <param name="notes">Новые внутренние заметки.</param>
    public void UpdateDetails(
        string? fullName,
        string? companyName,
        string? contactPhone,
        string? contactEmail,
        string? notes)
    {
        EnsureNotDeleted();
        SetName(fullName, companyName);
        ContactPhone = Guard.OptionalText(contactPhone, "контактный телефон", 30);
        ContactEmail = Guard.OptionalText(contactEmail, "контактный email", 256);
        Notes = Guard.OptionalText(notes, "заметки");
    }

    /// <summary>
    /// Необратимо стирает персональные данные клиента и сохраняет техническую строку для связей.
    /// Повторный вызов является идемпотентным и не меняет первоначальную дату удаления.
    /// </summary>
    /// <param name="deletedAt">Момент завершения анонимизации в UTC.</param>
    public void Anonymize(DateTimeOffset deletedAt)
    {
        if (DeletedAt.HasValue)
        {
            return;
        }

        deletedAt = Guard.AgainstDefault(deletedAt, "дата удаления");
        Guard.Against(deletedAt < CreatedAt, "Дата удаления клиента не может предшествовать дате создания.");

        FullName = null;
        CompanyName = null;
        ContactPhone = null;
        ContactEmail = null;
        Notes = null;
        DeletedAt = deletedAt;
    }

    /// <summary>
    /// Устанавливает взаимоисключающие варианты имени клиента.
    /// </summary>
    /// <param name="fullName">Имя физического лица.</param>
    /// <param name="companyName">Название организации.</param>
    private void SetName(string? fullName, string? companyName)
    {
        var normalizedFullName = Guard.OptionalText(fullName, "полное имя", 200);
        var normalizedCompanyName = Guard.OptionalText(companyName, "название организации", 300);

        if ((normalizedFullName is null) == (normalizedCompanyName is null))
        {
            throw new DomainValidationException(
                "Для активного клиента необходимо заполнить ровно одно поле: полное имя или название организации.");
        }

        FullName = normalizedFullName;
        CompanyName = normalizedCompanyName;
    }

    /// <summary>
    /// Запрещает изменение уже анонимизированной карточки клиента.
    /// </summary>
    private void EnsureNotDeleted()
    {
        if (DeletedAt.HasValue)
        {
            throw new DomainException("Нельзя изменить персональные данные уже удалённого клиента.");
        }
    }
}
