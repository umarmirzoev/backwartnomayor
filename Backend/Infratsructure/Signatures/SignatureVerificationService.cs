using Application.Common.Models;
using Application.Interfaces.Services;
using Domain.Enums;
using Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Infrastructure.Signatures;

/// <summary>
/// Проверяет простую электронную подпись по свежей парольной аутентификации и точной версии соглашения.
/// Метод AccreditedCA отклоняется до подключения проверяемого криптографического доказательства удостоверяющего центра,
/// поскольку одного клиентского флага недостаточно для юридически значимой подписи.
/// </summary>
public sealed class SignatureVerificationService : ISignatureVerificationService
{
    private readonly ICurrentUserContext _currentUser;
    private readonly IClock _clock;
    private readonly SignatureOptions _options;

    /// <summary>Инициализирует серверную проверку подписи доверенным контекстом и конфигурацией соглашения.</summary>
    /// <param name="currentUser">Контекст claims текущей стороны.</param>
    /// <param name="clock">Источник UTC-времени.</param>
    /// <param name="options">Версия соглашения и окно повторной аутентификации.</param>
    public SignatureVerificationService(
        ICurrentUserContext currentUser,
        IClock clock,
        IOptions<SignatureOptions> options)
    {
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(options);
        _currentUser = currentUser;
        _clock = clock;
        _options = options.Value;
    }

    /// <summary>
    /// Сопоставляет доверенную сторону с запросом, проверяет свежесть исходного парольного входа и версию соглашения.
    /// </summary>
    /// <param name="draftId">Идентификатор уже авторизованного черновика.</param>
    /// <param name="documentVersionId">Идентификатор уже авторизованной текущей версии.</param>
    /// <param name="signerType">Тип подписанта из доверенного контекста.</param>
    /// <param name="signerId">Идентификатор подписанта из доверенного контекста.</param>
    /// <param name="method">Запрошенный способ подписи.</param>
    /// <param name="consentAgreementVersion">Явно принятая версия соглашения.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Успешное доказательство простой подписи либо контролируемый отказ.</returns>
    public Task<ServiceResult<bool>> VerifyAsync(
        Guid draftId,
        Guid documentVersionId,
        PartyType signerType,
        Guid signerId,
        SignatureMethod method,
        string consentAgreementVersion,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (method == SignatureMethod.AccreditedCA)
        {
            return Task.FromResult(ServiceResult<bool>.Failure(
                ["Интеграция с аккредитованным удостоверяющим центром ещё не настроена."]));
        }

        if (method != SignatureMethod.Simple
            || draftId == Guid.Empty
            || documentVersionId == Guid.Empty
            || _currentUser.PartyType != signerType
            || _currentUser.PartyId != signerId)
        {
            return Task.FromResult(ServiceResult<bool>.Failure(["Доказательство подписи недействительно."]));
        }

        if (!string.Equals(
                consentAgreementVersion,
                _options.ConsentAgreementVersion,
                StringComparison.Ordinal))
        {
            return Task.FromResult(ServiceResult<bool>.Failure(
                ["Версия соглашения о простой электронной подписи устарела."]));
        }

        if (_options.ReauthenticationWindowMinutes is < 1 or > 30
            || _currentUser.AuthenticatedAt is not DateTimeOffset authenticatedAt
            || authenticatedAt > _clock.UtcNow
            || _clock.UtcNow - authenticatedAt > TimeSpan.FromMinutes(_options.ReauthenticationWindowMinutes))
        {
            return Task.FromResult(ServiceResult<bool>.Failure(
                ["Для подписи требуется повторный вход с паролем."]));
        }

        return Task.FromResult(ServiceResult<bool>.Success(true));
    }
}
