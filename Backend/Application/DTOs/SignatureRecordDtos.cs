using Domain.Enums;

namespace Application.DTOs;

/// <summary>
/// Представляет Post-MVP состояние подписи стороны без сетевых и иных чувствительных технических данных.
/// </summary>
/// <param name="Id">Идентификатор записи подписи.</param>
/// <param name="SignerType">Строковое имя типа подписанта.</param>
/// <param name="SignerId">Идентификатор подписанта.</param>
/// <param name="Method">Строковое имя способа подписи.</param>
/// <param name="SignedAt">Дата подписания.</param>
public sealed record GetSignatureRecordDto(
    Guid Id,
    string SignerType,
    Guid SignerId,
    string Method,
    DateTimeOffset SignedAt);

/// <summary>
/// Представляет полную юридически значимую запись подписи для авторизованного аудита.
/// IP-адрес включён только в детальную модель и должен возвращаться после строгой проверки доступа.
/// </summary>
/// <param name="Id">Идентификатор записи.</param>
/// <param name="DraftId">Идентификатор черновика.</param>
/// <param name="DocumentVersionId">Идентификатор подписанной версии.</param>
/// <param name="SignerType">Строковое имя типа подписанта.</param>
/// <param name="SignerId">Идентификатор подписанта.</param>
/// <param name="Method">Строковое имя способа подписи.</param>
/// <param name="ConsentAgreementVersion">Принятая версия пользовательского соглашения.</param>
/// <param name="SignedAt">Дата подписания.</param>
/// <param name="IpAddress">Зафиксированный IP-адрес.</param>
public sealed record SignatureRecordDetailDto(
    Guid Id,
    Guid DraftId,
    Guid DocumentVersionId,
    string SignerType,
    Guid SignerId,
    string Method,
    string ConsentAgreementVersion,
    DateTimeOffset SignedAt,
    string IpAddress);

/// <summary>
/// Представляет безопасные данные запроса подписи.
/// Тип, идентификатор подписанта, время и IP-адрес вычисляются сервером и не могут быть подменены клиентом.
/// </summary>
/// <param name="DraftId">Идентификатор подписываемого черновика.</param>
/// <param name="DocumentVersionId">Идентификатор финальной версии.</param>
/// <param name="Method">Способ подписи.</param>
/// <param name="ConsentAgreementVersion">Версия явно принятого соглашения.</param>
public sealed record CreateSignatureRecordDto(
    Guid DraftId,
    Guid DocumentVersionId,
    SignatureMethod Method,
    string ConsentAgreementVersion);

/// <summary>
/// Маркер отсутствующего сценария обновления подписи.
/// Запись является append-only юридическим фактом и не допускает последующего редактирования.
/// </summary>
public sealed record UpdateSignatureRecordDto;
