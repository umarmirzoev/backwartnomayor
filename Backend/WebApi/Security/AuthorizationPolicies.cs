namespace WebApi.Security;

/// <summary>
/// Содержит стабильные имена политик, сопоставляющих Application-разрешения с ролями Identity.
/// Отдельные имена исключают строковые литералы из обработчиков и Swagger-контрактов.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>Получает политику управления библиотекой шаблонов.</summary>
    public const string ManageTemplateLibrary = nameof(ManageTemplateLibrary);

    /// <summary>Получает политику утверждения документов от имени фирмы.</summary>
    public const string ApproveFirmDrafts = nameof(ApproveFirmDrafts);

    /// <summary>Получает политику мониторинга изменений законодательства.</summary>
    public const string ManageLegislationMonitoring = nameof(ManageLegislationMonitoring);

    /// <summary>Получает политику исполнения необратимого удаления данных.</summary>
    public const string ExecuteDataDeletion = nameof(ExecuteDataDeletion);
}
