using System.Net;
using Domain.Common;
using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities;

/// <summary>
/// Представляет Post-MVP-запись подписи конкретной неизменяемой версии документа.
/// Запись юридически значима, после создания не изменяется и не должна удаляться каскадно.
/// </summary>
public sealed class SignatureRecord : BaseEntity
{
    /// <summary>
    /// Инициализирует запись подписи при материализации сохранённых данных ORM.
    /// </summary>
    private SignatureRecord()
    {
    }

    /// <summary>
    /// Создаёт неизменяемую запись подписи стороны документа.
    /// </summary>
    /// <param name="draftId">Идентификатор подписываемого документа.</param>
    /// <param name="documentVersionId">Идентификатор финальной подписываемой версии.</param>
    /// <param name="signerType">Тип подписанта.</param>
    /// <param name="signerId">Идентификатор подписанта.</param>
    /// <param name="method">Способ электронной подписи.</param>
    /// <param name="consentAgreementVersion">Версия явно принятого пользовательского соглашения.</param>
    /// <param name="signedAt">Момент подписания в UTC.</param>
    /// <param name="ipAddress">Корректный IPv4- или IPv6-адрес подписанта.</param>
    public SignatureRecord(
        Guid draftId,
        Guid documentVersionId,
        PartyType signerType,
        Guid signerId,
        SignatureMethod method,
        string consentAgreementVersion,
        DateTimeOffset signedAt,
        string ipAddress)
        : base(Guid.NewGuid())
    {
        DraftId = Guard.AgainstEmpty(draftId, "идентификатор документа");
        DocumentVersionId = Guard.AgainstEmpty(documentVersionId, "идентификатор версии документа");
        SignerType = Guard.AgainstInvalidEnum(signerType, "тип подписанта");
        SignerId = Guard.AgainstEmpty(signerId, "идентификатор подписанта");
        Method = Guard.AgainstInvalidEnum(method, "метод подписи");
        ConsentAgreementVersion = Guard.RequiredText(
            consentAgreementVersion,
            "версия пользовательского соглашения",
            50);
        SignedAt = Guard.AgainstDefault(signedAt, "дата подписания");
        IpAddress = ValidateIpAddress(ipAddress);
    }

    /// <summary>Получает идентификатор подписанного документа.</summary>
    public Guid DraftId { get; private set; }

    /// <summary>Получает идентификатор подписанной неизменяемой версии.</summary>
    public Guid DocumentVersionId { get; private set; }

    /// <summary>Получает тип подписанта.</summary>
    public PartyType SignerType { get; private set; }

    /// <summary>Получает идентификатор подписанта.</summary>
    public Guid SignerId { get; private set; }

    /// <summary>Получает использованный способ электронной подписи.</summary>
    public SignatureMethod Method { get; private set; }

    /// <summary>Получает версию соглашения, явно принятого подписантом.</summary>
    public string ConsentAgreementVersion { get; private set; } = string.Empty;

    /// <summary>Получает момент подписания.</summary>
    public DateTimeOffset SignedAt { get; private set; }

    /// <summary>Получает проверенный IP-адрес подписанта.</summary>
    public string IpAddress { get; private set; } = string.Empty;

    /// <summary>
    /// Проверяет и нормализует сетевой адрес подписанта.
    /// </summary>
    /// <param name="ipAddress">Исходное текстовое представление адреса.</param>
    /// <returns>Каноническое строковое представление IPv4 или IPv6.</returns>
    private static string ValidateIpAddress(string ipAddress)
    {
        var normalized = Guard.RequiredText(ipAddress, "IP-адрес", 45);

        if (!IPAddress.TryParse(normalized, out var parsedAddress))
        {
            throw new DomainValidationException("IP-адрес подписанта имеет некорректный формат.");
        }

        return parsedAddress.ToString();
    }
}
