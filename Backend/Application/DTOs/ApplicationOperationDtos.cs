namespace Application.DTOs;

/// <summary>
/// Представляет пару JWT access/refresh-токенов и срок действия короткоживущего access-токена.
/// </summary>
/// <param name="AccessToken">Подписанный короткоживущий access-токен.</param>
/// <param name="RefreshToken">Криптографически случайный refresh-токен.</param>
/// <param name="AccessTokenExpiresAt">Момент истечения access-токена в UTC.</param>
public sealed record AuthenticationTokensDto(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt);

/// <summary>
/// Представляет безопасные сведения Identity, необходимые Application-слою без зависимости от ASP.NET Core Identity.
/// </summary>
/// <param name="UserId">Идентификатор учётной записи Identity.</param>
/// <param name="Email">Подтверждённый или зарегистрированный адрес электронной почты.</param>
/// <param name="PhoneNumber">Необязательный номер телефона.</param>
public sealed record IdentityUserDto(Guid UserId, string Email, string? PhoneNumber);

/// <summary>
/// Представляет результат создания или обновления черновика вместе с текстом новой immutable-версии.
/// </summary>
/// <param name="DraftId">Идентификатор агрегата черновика.</param>
/// <param name="VersionId">Идентификатор созданной версии.</param>
/// <param name="VersionNumber">Последовательный номер версии.</param>
/// <param name="Content">Текст сохранённой версии.</param>
public sealed record DraftOperationDto(
    Guid DraftId,
    Guid VersionId,
    int VersionNumber,
    string Content);

/// <summary>
/// Представляет один риск или пункт входящего документа, который ИИ рекомендует обсудить юристу.
/// </summary>
/// <param name="Clause">Название или выдержка анализируемого пункта.</param>
/// <param name="Explanation">Пояснение юридического или коммерческого риска.</param>
/// <param name="Recommendation">Рекомендуемое действие юриста.</param>
public sealed record DocumentReviewItemDto(
    string Clause,
    string Explanation,
    string Recommendation);

/// <summary>
/// Представляет экспортированный файл без раскрытия пути файловой системы или ключа объектного хранилища.
/// </summary>
/// <param name="FileName">Безопасное имя файла.</param>
/// <param name="ContentType">MIME-тип экспортированного документа.</param>
/// <param name="Content">Байтовое содержимое файла.</param>
public sealed record ExportedDocumentDto(
    string FileName,
    string ContentType,
    byte[] Content);

/// <summary>
/// Представляет итог Post-MVP операции подписи и количество уже зафиксированных подписей.
/// </summary>
/// <param name="DraftId">Идентификатор подписываемого черновика.</param>
/// <param name="Status">Строковое имя состояния документа после операции.</param>
/// <param name="SignaturesCount">Количество уникальных записей подписи.</param>
public sealed record SignatureStatusDto(
    Guid DraftId,
    string Status,
    int SignaturesCount);
