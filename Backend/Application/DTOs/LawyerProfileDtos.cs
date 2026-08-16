namespace Application.DTOs;

/// <summary>
/// Представляет безопасную краткую модель профиля юриста без технического идентификатора Identity
/// и без данных аутентификации, пригодную для списочных и вложенных ответов API.
/// </summary>
/// <param name="Id">Идентификатор доменного профиля юриста.</param>
/// <param name="FullName">Полное имя юриста.</param>
/// <param name="LawFirmName">Название юридической фирмы или отсутствие фирмы.</param>
/// <param name="SubscriptionTier">Строковое имя тарифа для стабильного клиентского контракта.</param>
/// <param name="IsActive">Признак активности доменного профиля.</param>
public sealed record GetLawyerProfileDto(
    Guid Id,
    string FullName,
    string? LawFirmName,
    string SubscriptionTier,
    bool IsActive);

/// <summary>
/// Представляет полную карточку текущего юриста, объединяемую CQRS-обработчиком
/// из доменного профиля и безопасных полей ASP.NET Core Identity.
/// </summary>
/// <param name="Id">Идентификатор доменного профиля.</param>
/// <param name="FullName">Полное имя юриста.</param>
/// <param name="Email">Адрес электронной почты из Identity.</param>
/// <param name="PhoneNumber">Телефон из Identity.</param>
/// <param name="LawFirmName">Название юридической фирмы.</param>
/// <param name="SubscriptionTier">Строковое имя текущего тарифа.</param>
/// <param name="IsActive">Признак активности профиля.</param>
/// <param name="CreatedAt">Дата создания профиля в UTC.</param>
public sealed record LawyerProfileDetailDto(
    Guid Id,
    string FullName,
    string? Email,
    string? PhoneNumber,
    string? LawFirmName,
    string SubscriptionTier,
    bool IsActive,
    DateTimeOffset CreatedAt);

/// <summary>
/// Представляет входной контракт регистрации юриста.
/// Учётная запись Identity и доменный профиль создаются согласованно обработчиком,
/// поэтому пароль никогда не попадает в доменную сущность или выходные DTO.
/// </summary>
/// <param name="Email">Уникальный адрес электронной почты, используемый как логин.</param>
/// <param name="Password">Исходный пароль, передаваемый только Identity для безопасного хеширования.</param>
/// <param name="PhoneNumber">Необязательный номер телефона.</param>
/// <param name="FullName">Полное имя юриста.</param>
/// <param name="LawFirmName">Необязательное название юридической фирмы.</param>
public sealed record CreateLawyerProfileDto(
    string Email,
    string Password,
    string? PhoneNumber,
    string FullName,
    string? LawFirmName);

/// <summary>
/// Представляет разрешённые изменения собственного профиля юриста.
/// Тариф и активность исключены, поскольку изменяются отдельными привилегированными сценариями.
/// </summary>
/// <param name="FullName">Новое полное имя.</param>
/// <param name="LawFirmName">Новое название юридической фирмы.</param>
/// <param name="PhoneNumber">Новый телефон, сохраняемый в Identity.</param>
public sealed record UpdateLawyerProfileDto(
    string FullName,
    string? LawFirmName,
    string? PhoneNumber);
