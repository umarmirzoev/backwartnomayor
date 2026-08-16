namespace Application.Common.Security;

/// <summary>
/// Определяет прикладные разрешения, которые нельзя безопасно вывести из отсутствующей MVP-модели ролей.
/// Конкретное сопоставление claims/policies выполняется внешним слоем и не проникает в домен.
/// </summary>
public enum ApplicationPermission
{
    /// <summary>Разрешает курировать библиотеку шаблонов и договорных пунктов.</summary>
    ManageTemplateLibrary,

    /// <summary>Разрешает подтверждать документы от имени юридической фирмы в Post-MVP.</summary>
    ApproveFirmDrafts,

    /// <summary>Разрешает запускать системное сопоставление изменений законодательства.</summary>
    ManageLegislationMonitoring,

    /// <summary>Разрешает исполнять необратимые запросы удаления данных.</summary>
    ExecuteDataDeletion
}
