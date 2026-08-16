namespace Application.DTOs;

/// <summary>
/// Представляет краткую карточку клиента в изолированном списке текущего юриста.
/// Модель содержит только необходимые контактные данные и не раскрывает внутренний идентификатор владельца.
/// </summary>
/// <param name="Id">Идентификатор клиента.</param>
/// <param name="FullName">Имя физического лица.</param>
/// <param name="CompanyName">Название организации.</param>
/// <param name="ContactPhone">Контактный телефон.</param>
/// <param name="ContactEmail">Контактный адрес электронной почты.</param>
/// <param name="CreatedAt">Дата создания карточки в UTC.</param>
public sealed record GetClientDto(
    Guid Id,
    string? FullName,
    string? CompanyName,
    string? ContactPhone,
    string? ContactEmail,
    DateTimeOffset CreatedAt);

/// <summary>
/// Представляет полную карточку клиента для владельца, включая заметки и состояние удаления персональных данных.
/// </summary>
/// <param name="Id">Идентификатор клиента.</param>
/// <param name="FullName">Имя физического лица.</param>
/// <param name="CompanyName">Название организации.</param>
/// <param name="ContactPhone">Контактный телефон.</param>
/// <param name="ContactEmail">Контактный адрес электронной почты.</param>
/// <param name="Notes">Рабочие заметки юриста.</param>
/// <param name="DeletedAt">Дата физического стирания персональных данных.</param>
/// <param name="CreatedAt">Дата создания карточки.</param>
public sealed record ClientDetailDto(
    Guid Id,
    string? FullName,
    string? CompanyName,
    string? ContactPhone,
    string? ContactEmail,
    string? Notes,
    DateTimeOffset? DeletedAt,
    DateTimeOffset CreatedAt);

/// <summary>
/// Представляет данные для создания клиента текущего юриста.
/// Идентификатор владельца берётся из проверенного Identity-контекста и не принимается от клиента API.
/// </summary>
/// <param name="FullName">Имя физического лица.</param>
/// <param name="CompanyName">Название организации.</param>
/// <param name="ContactPhone">Контактный телефон.</param>
/// <param name="ContactEmail">Контактный адрес электронной почты.</param>
/// <param name="Notes">Необязательные рабочие заметки.</param>
public sealed record CreateClientDto(
    string? FullName,
    string? CompanyName,
    string? ContactPhone,
    string? ContactEmail,
    string? Notes);

/// <summary>
/// Представляет полный набор разрешённых изменений контактных данных клиента.
/// Удаление персональных данных выполняется отдельным workflow и не моделируется этим DTO.
/// </summary>
/// <param name="FullName">Новое имя физического лица.</param>
/// <param name="CompanyName">Новое название организации.</param>
/// <param name="ContactPhone">Новый контактный телефон.</param>
/// <param name="ContactEmail">Новый контактный адрес электронной почты.</param>
/// <param name="Notes">Обновлённые рабочие заметки.</param>
public sealed record UpdateClientDto(
    string? FullName,
    string? CompanyName,
    string? ContactPhone,
    string? ContactEmail,
    string? Notes);
