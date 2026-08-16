using System.Globalization;
using System.Security.Claims;
using Application.Interfaces.Services;
using Domain.Enums;
using WebApi.Seeds;

namespace WebApi.Security;

/// <summary>
/// Преобразует только валидированные JWT-claims и серверный HttpContext в доверенный контекст Application-слоя.
/// Идентификаторы владельца никогда не читаются из тела запроса, что является основной защитой от IDOR.
/// </summary>
public sealed class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly SystemExecutionContext _systemExecutionContext;

    /// <summary>Инициализирует scoped-представление текущего субъекта и системного фонового контекста.</summary>
    /// <param name="httpContextAccessor">Серверный доступ к аутентифицированному HTTP-контексту.</param>
    /// <param name="systemExecutionContext">Недоступный клиенту признак фонового исполнителя.</param>
    public CurrentUserContext(
        IHttpContextAccessor httpContextAccessor,
        SystemExecutionContext systemExecutionContext)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        ArgumentNullException.ThrowIfNull(systemExecutionContext);
        _httpContextAccessor = httpContextAccessor;
        _systemExecutionContext = systemExecutionContext;
    }

    /// <summary>Получает идентификатор Identity из стандартного claim NameIdentifier.</summary>
    public Guid? IdentityUserId => GetGuidClaim(ClaimTypes.NameIdentifier);

    /// <summary>Получает tenant-идентификатор профиля юриста из подписанного claim.</summary>
    public Guid? LawyerId => GetGuidClaim("lawyer_id");

    /// <summary>Получает тип стороны только при наличии согласованного идентификатора стороны.</summary>
    public PartyType? PartyType
    {
        get
        {
            var rawValue = User.FindFirstValue("party_type");
            return Enum.TryParse<PartyType>(rawValue, ignoreCase: false, out var partyType)
                ? partyType
                : null;
        }
    }

    /// <summary>Получает идентификатор стороны из tenant-claims без принятия значения от клиента.</summary>
    public Guid? PartyId => PartyType switch
    {
        Domain.Enums.PartyType.Lawyer => LawyerId,
        Domain.Enums.PartyType.Client => GetGuidClaim("client_id"),
        _ => null
    };

    /// <summary>Получает признак внутреннего BackgroundService или аутентифицированной сервисной роли.</summary>
    public bool IsSystem => _systemExecutionContext.IsSystem || User.IsInRole(DefaultRoles.System);

    /// <summary>Получает сетевой IP из серверного соединения без доверия произвольному заголовку клиента.</summary>
    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    /// <summary>Получает исходный момент парольной аутентификации из Unix-time claim.</summary>
    public DateTimeOffset? AuthenticatedAt
    {
        get
        {
            var rawValue = User.FindFirstValue("auth_time");
            return long.TryParse(rawValue, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds)
                ? DateTimeOffset.FromUnixTimeSeconds(seconds)
                : null;
        }
    }

    /// <summary>Получает текущий principal либо пустой неаутентифицированный principal вне HTTP-запроса.</summary>
    private ClaimsPrincipal User =>
        _httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());

    /// <summary>Извлекает Guid только из подписанного claim и отклоняет пустое значение.</summary>
    /// <param name="claimType">Тип claim.</param>
    /// <returns>Непустой идентификатор либо отсутствие значения.</returns>
    private Guid? GetGuidClaim(string claimType)
    {
        var rawValue = User.FindFirstValue(claimType);
        return Guid.TryParse(rawValue, out var value) && value != Guid.Empty ? value : null;
    }
}
