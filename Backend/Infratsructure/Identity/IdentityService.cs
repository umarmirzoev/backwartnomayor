using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.Common.Models;
using Application.DTOs;
using Application.Interfaces.Services;
using Domain.Enums;
using Infrastructure.Options;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Identity;

/// <summary>
/// Реализует порт Identity, выдаёт короткоживущие JWT и ротирует одноразовые refresh-токены.
/// Пароли обрабатываются только ASP.NET Core Identity, а в PostgreSQL сохраняется исключительно SHA-256-хеш refresh-токена.
/// </summary>
public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly AppDbContext _dbContext;
    private readonly IClock _clock;
    private readonly JwtOptions _options;
    private readonly SymmetricSecurityKey _signingKey;

    /// <summary>
    /// Инициализирует Identity-адаптер и немедленно проверяет критичные криптографические параметры.
    /// Некорректная конфигурация останавливает выдачу токенов до начала обработки пользовательских данных.
    /// </summary>
    /// <param name="userManager">Стандартный менеджер пользователей Identity.</param>
    /// <param name="signInManager">Менеджер безопасной проверки пароля и блокировки.</param>
    /// <param name="dbContext">Контекст для tenant-профиля и атомарной ротации токена.</param>
    /// <param name="clock">Единый источник UTC-времени.</param>
    /// <param name="options">Проверяемые параметры JWT.</param>
    public IdentityService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        AppDbContext dbContext,
        IClock clock,
        IOptions<JwtOptions> options)
    {
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(signInManager);
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(options);

        _userManager = userManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
        _clock = clock;
        _options = options.Value;

        ValidateOptions(_options);
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
    }

    /// <summary>
    /// Создаёт уникального Identity-пользователя, применяет встроенное хеширование пароля и назначает базовую роль юриста.
    /// При ошибке назначения роли созданная учётная запись компенсирующе удаляется.
    /// </summary>
    /// <param name="email">Нормализуемый email пользователя.</param>
    /// <param name="password">Исходный пароль, не сохраняемый приложением.</param>
    /// <param name="phoneNumber">Необязательный номер телефона.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Идентификатор Identity либо безопасные русскоязычные ошибки.</returns>
    public async Task<ServiceResult<Guid>> CreateUserAsync(
        string email,
        string password,
        string? phoneNumber,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var normalizedEmail = email.Trim();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = normalizedEmail,
            Email = normalizedEmail,
            PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim()
        };

        var creationResult = await _userManager.CreateAsync(user, password);
        cancellationToken.ThrowIfCancellationRequested();
        if (!creationResult.Succeeded)
        {
            return ServiceResult<Guid>.Failure(MapIdentityErrors(creationResult));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, _options.DefaultRegistrationRole);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return ServiceResult<Guid>.Failure(["Не удалось назначить базовую роль учётной записи."]);
        }

        return ServiceResult<Guid>.Success(user.Id);
    }

    /// <summary>
    /// Удаляет Identity-пользователя при компенсации незавершённой регистрации профиля.
    /// Операция не удаляет доменные данные и поэтому не используется как пользовательский сценарий полного удаления.
    /// </summary>
    /// <param name="userId">Идентификатор компенсируемой учётной записи.</param>
    /// <param name="cancellationToken">Токен отмены поиска.</param>
    /// <returns>Признак успешного удаления или отсутствия пользователя.</returns>
    public async Task<ServiceResult<bool>> DeleteUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return ServiceResult<bool>.Success(true);
        }

        var result = await _userManager.DeleteAsync(user);
        return result.Succeeded
            ? ServiceResult<bool>.Success(true)
            : ServiceResult<bool>.Failure(["Не удалось удалить незавершённую учётную запись."]);
    }

    /// <summary>
    /// Проверяет credentials с учётом блокировки Identity, активность доменного профиля и выдаёт новую пару токенов.
    /// Ответ намеренно не различает неверный email, пароль и деактивированный профиль.
    /// </summary>
    /// <param name="email">Email учётной записи.</param>
    /// <param name="password">Исходный пароль.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Пара токенов либо унифицированная ошибка аутентификации.</returns>
    public async Task<ServiceResult<AuthenticationTokensDto>> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await _userManager.FindByEmailAsync(email.Trim());
        if (user is null)
        {
            return AuthenticationFailure();
        }

        var passwordResult = await _signInManager.CheckPasswordSignInAsync(
            user,
            password,
            lockoutOnFailure: true);
        cancellationToken.ThrowIfCancellationRequested();
        if (!passwordResult.Succeeded)
        {
            return AuthenticationFailure();
        }

        var lawyerProfile = await _dbContext.LawyerProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(profile => profile.UserId == user.Id, cancellationToken);
        if (lawyerProfile is null || !lawyerProfile.IsActive)
        {
            return AuthenticationFailure();
        }

        return await IssueAndPersistTokensAsync(user, lawyerProfile.Id, _clock.UtcNow, cancellationToken);
    }

    /// <summary>
    /// Находит пользователя по хешу одноразового refresh-токена, проверяет срок и выполняет обязательную ротацию.
    /// Старое значение перестаёт действовать в той же операции обновления Identity-пользователя.
    /// </summary>
    /// <param name="refreshToken">Исходный одноразовый refresh-токен.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Новая пара токенов либо унифицированная ошибка.</returns>
    public async Task<ServiceResult<AuthenticationTokensDto>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var tokenHash = HashRefreshToken(refreshToken);
        var now = _clock.UtcNow;
        var user = await _dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.RefreshTokenHash == tokenHash,
            cancellationToken);

        if (user is null || user.RefreshTokenExpiresAt is null || user.RefreshTokenExpiresAt <= now)
        {
            return ServiceResult<AuthenticationTokensDto>.Failure(["Refresh-токен недействителен."]);
        }

        var lawyerProfile = await _dbContext.LawyerProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(profile => profile.UserId == user.Id, cancellationToken);
        if (lawyerProfile is null || !lawyerProfile.IsActive)
        {
            user.RevokeRefreshToken();
            await _dbContext.SaveChangesAsync(cancellationToken);
            return ServiceResult<AuthenticationTokensDto>.Failure(["Refresh-токен недействителен."]);
        }

        if (user.RefreshTokenAuthenticatedAt is not DateTimeOffset authenticatedAt)
        {
            user.RevokeRefreshToken();
            await _dbContext.SaveChangesAsync(cancellationToken);
            return ServiceResult<AuthenticationTokensDto>.Failure(["Refresh-токен недействителен."]);
        }

        return await IssueAndPersistTokensAsync(user, lawyerProfile.Id, authenticatedAt, cancellationToken);
    }

    /// <summary>
    /// Возвращает только разрешённые Application-слою поля Identity без хешей, токенов и служебных признаков.
    /// </summary>
    /// <param name="userId">Идентификатор Identity-пользователя.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Безопасные сведения либо отсутствие пользователя.</returns>
    public async Task<IdentityUserDto?> GetUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new IdentityUserDto(user.Id, user.Email!, user.PhoneNumber))
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Создаёт access-токен с ролями и tenant-идентификатором, генерирует новый refresh-токен и сохраняет только его хеш.
    /// </summary>
    /// <param name="user">Проверенный Identity-пользователь.</param>
    /// <param name="lawyerId">Идентификатор активного доменного профиля.</param>
    /// <param name="authenticatedAt">Момент исходной парольной аутентификации сессии.</param>
    /// <param name="cancellationToken">Токен отмены сохранения.</param>
    /// <returns>Новая пара токенов либо безопасная инфраструктурная ошибка.</returns>
    private async Task<ServiceResult<AuthenticationTokensDto>> IssueAndPersistTokensAsync(
        ApplicationUser user,
        Guid lawyerId,
        DateTimeOffset authenticatedAt,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var accessTokenExpiresAt = now.AddMinutes(_options.AccessTokenLifetimeMinutes);
        var roles = await _userManager.GetRolesAsync(user);
        cancellationToken.ThrowIfCancellationRequested();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("auth_time", authenticatedAt.ToUnixTimeSeconds().ToString()),
            new("lawyer_id", lawyerId.ToString()),
            new("party_type", PartyType.Lawyer.ToString())
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            _options.Issuer,
            _options.Audience,
            claims,
            now.UtcDateTime,
            accessTokenExpiresAt.UtcDateTime,
            credentials);

        var refreshToken = GenerateRefreshToken();
        user.SetRefreshToken(
            HashRefreshToken(refreshToken),
            now.AddDays(_options.RefreshTokenLifetimeDays),
            authenticatedAt);
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return ServiceResult<AuthenticationTokensDto>.Failure(["Не удалось сохранить ротацию токена."]);
        }

        return ServiceResult<AuthenticationTokensDto>.Success(
            new AuthenticationTokensDto(
                new JwtSecurityTokenHandler().WriteToken(token),
                refreshToken,
                accessTokenExpiresAt));
    }

    /// <summary>Создаёт криптографически случайное Base64Url-значение достаточной энтропии.</summary>
    /// <returns>Новый исходный refresh-токен.</returns>
    private static string GenerateRefreshToken()
    {
        return Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
    }

    /// <summary>Вычисляет фиксированный SHA-256-хеш токена для безопасного поиска и хранения.</summary>
    /// <param name="refreshToken">Исходное значение токена.</param>
    /// <returns>Шестнадцатеричный хеш в верхнем регистре.</returns>
    private static string HashRefreshToken(string refreshToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
    }

    /// <summary>Создаёт одинаковый отказ для всех причин неуспешного входа.</summary>
    /// <returns>Безопасный результат без признака существования пользователя.</returns>
    private static ServiceResult<AuthenticationTokensDto> AuthenticationFailure()
    {
        return ServiceResult<AuthenticationTokensDto>.Failure(["Неверные учётные данные или доступ запрещён."]);
    }

    /// <summary>Преобразует внутренние коды Identity в минимальный русскоязычный набор без технических деталей.</summary>
    /// <param name="result">Неуспешный результат Identity.</param>
    /// <returns>Безопасные ошибки регистрации.</returns>
    private static IReadOnlyList<string> MapIdentityErrors(IdentityResult result)
    {
        return result.Errors.Any(error => error.Code is "DuplicateEmail" or "DuplicateUserName")
            ? ["Учётная запись с таким email уже существует."]
            : ["Учётная запись не соответствует требованиям безопасности пароля или идентификатора."];
    }

    /// <summary>Проверяет обязательные параметры подписи и допустимые сроки токенов.</summary>
    /// <param name="options">Параметры JWT.</param>
    private static void ValidateOptions(JwtOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Issuer)
            || string.IsNullOrWhiteSpace(options.Audience)
            || Encoding.UTF8.GetByteCount(options.SigningKey) < 32)
        {
            throw new InvalidOperationException(
                "JWT требует непустые Issuer/Audience и секрет подписи длиной не менее 32 байтов.");
        }

        if (options.AccessTokenLifetimeMinutes is < 5 or > 60
            || options.RefreshTokenLifetimeDays is < 1 or > 90
            || string.IsNullOrWhiteSpace(options.DefaultRegistrationRole))
        {
            throw new InvalidOperationException("Параметры срока токенов или базовой роли JWT недопустимы.");
        }
    }
}
