using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

/// <summary>
/// Представляет инфраструктурную учётную запись ASP.NET Core Identity с идентификатором <see cref="Guid"/>.
/// Стандартные поля Identity отвечают за email, имя входа, хеш пароля, подтверждение телефона,
/// блокировку и другие механизмы аутентификации; бизнес-профиль хранится отдельно в домене.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>
    /// Получает криптографический SHA-256-хеш текущего одноразового refresh-токена.
    /// Исходное значение токена никогда не сохраняется в базе данных.
    /// </summary>
    public string? RefreshTokenHash { get; private set; }

    /// <summary>Получает момент истечения текущего refresh-токена в UTC.</summary>
    public DateTimeOffset? RefreshTokenExpiresAt { get; private set; }

    /// <summary>
    /// Получает момент исходной парольной аутентификации текущей refresh-сессии.
    /// Ротация токена сохраняет это значение и не имитирует повторную проверку пароля.
    /// </summary>
    public DateTimeOffset? RefreshTokenAuthenticatedAt { get; private set; }

    /// <summary>
    /// Заменяет хеш refresh-токена и срок его действия при входе или ротации.
    /// Метод хранит только проверенный хеш и тем самым ограничивает ущерб при утечке базы данных.
    /// </summary>
    /// <param name="refreshTokenHash">Шестнадцатеричный SHA-256-хеш токена.</param>
    /// <param name="expiresAt">Момент истечения токена в UTC.</param>
    /// <param name="authenticatedAt">Момент исходной парольной аутентификации сессии.</param>
    public void SetRefreshToken(
        string refreshTokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset authenticatedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshTokenHash);
        if (refreshTokenHash.Length != 64)
        {
            throw new ArgumentException("Хеш refresh-токена должен быть SHA-256-значением длиной 64 символа.", nameof(refreshTokenHash));
        }

        if (expiresAt == default)
        {
            throw new ArgumentException("Срок действия refresh-токена обязателен.", nameof(expiresAt));
        }

        if (authenticatedAt == default || authenticatedAt > expiresAt)
        {
            throw new ArgumentException("Момент аутентификации refresh-сессии недопустим.", nameof(authenticatedAt));
        }

        RefreshTokenHash = refreshTokenHash;
        RefreshTokenExpiresAt = expiresAt;
        RefreshTokenAuthenticatedAt = authenticatedAt;
    }

    /// <summary>
    /// Отзывает текущий refresh-токен после использования, выхода или компрометации учётной записи.
    /// Очистка делает повторное применение старого токена невозможным.
    /// </summary>
    public void RevokeRefreshToken()
    {
        RefreshTokenHash = null;
        RefreshTokenExpiresAt = null;
        RefreshTokenAuthenticatedAt = null;
    }
}
