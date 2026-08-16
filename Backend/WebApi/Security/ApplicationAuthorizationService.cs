using Application.Common.Security;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;

namespace WebApi.Security;

/// <summary>
/// Адаптирует ASP.NET Core authorization policies к транспортно-независимому порту Application-слоя.
/// Обработчики запрашивают бизнес-разрешение, не завися от строковых имён ролей или HttpContext.
/// </summary>
public sealed class ApplicationAuthorizationService : IApplicationAuthorizationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuthorizationService _authorizationService;

    /// <summary>Инициализирует адаптер текущим principal и стандартным движком политик.</summary>
    /// <param name="httpContextAccessor">Доступ к аутентифицированному principal.</param>
    /// <param name="authorizationService">Служба оценки зарегистрированных политик.</param>
    public ApplicationAuthorizationService(
        IHttpContextAccessor httpContextAccessor,
        IAuthorizationService authorizationService)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        ArgumentNullException.ThrowIfNull(authorizationService);
        _httpContextAccessor = httpContextAccessor;
        _authorizationService = authorizationService;
    }

    /// <summary>
    /// Сопоставляет строгое перечисление разрешения с зарегистрированной политикой и оценивает её для текущего principal.
    /// </summary>
    /// <param name="permission">Прикладное разрешение.</param>
    /// <param name="cancellationToken">Токен отмены до обращения к механизму политик.</param>
    /// <returns>Признак успешной авторизации.</returns>
    public async Task<bool> HasPermissionAsync(
        ApplicationPermission permission,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var policyName = permission switch
        {
            ApplicationPermission.ManageTemplateLibrary => AuthorizationPolicies.ManageTemplateLibrary,
            ApplicationPermission.ApproveFirmDrafts => AuthorizationPolicies.ApproveFirmDrafts,
            ApplicationPermission.ManageLegislationMonitoring => AuthorizationPolicies.ManageLegislationMonitoring,
            ApplicationPermission.ExecuteDataDeletion => AuthorizationPolicies.ExecuteDataDeletion,
            _ => throw new ArgumentOutOfRangeException(nameof(permission), permission, "Разрешение не поддерживается.")
        };
        var result = await _authorizationService.AuthorizeAsync(user, resource: null, policyName);
        return result.Succeeded;
    }
}
