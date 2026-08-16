namespace WebApi.Seeds;

/// <summary>
/// Содержит канонические имена транспортных ролей ASP.NET Core Identity.
/// Роли не являются доменными сущностями MVP и используются только на границе WebAPI для защиты системных и Post-MVP операций.
/// </summary>
public static class DefaultRoles
{
    /// <summary>Получает роль владельца всей платформы с доступом к аварийному администрированию.</summary>
    public const string SuperAdministrator = "SuperAdministrator";

    /// <summary>Получает роль операционного администратора без владения бизнес-ресурсами юристов.</summary>
    public const string Administrator = "Administrator";

    /// <summary>Получает роль куратора библиотечных шаблонов и мониторинга законодательства.</summary>
    public const string Curator = "Curator";

    /// <summary>Получает Post-MVP роль партнёра юридической фирмы, одобряющего документы.</summary>
    public const string FirmPartner = "FirmPartner";

    /// <summary>Получает основную MVP-роль юриста с tenant-доступом только к собственным данным.</summary>
    public const string Lawyer = "Lawyer";

    /// <summary>Получает Post-MVP роль клиента с доступом только к документам своего дела.</summary>
    public const string Client = "Client";

    /// <summary>Получает роль сервисной учётной записи доверенных фоновых интеграций.</summary>
    public const string System = "System";

    /// <summary>Получает полный неизменяемый набор ролей для идемпотентной инициализации.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        SuperAdministrator,
        Administrator,
        Curator,
        FirmPartner,
        Lawyer,
        Client,
        System
    ];

    /// <summary>Получает список ролей администрирования для атрибутов Authorize.</summary>
    public const string Administrators = SuperAdministrator + "," + Administrator;

    /// <summary>Получает роли управления библиотекой шаблонов.</summary>
    public const string TemplateManagers = SuperAdministrator + "," + Administrator + "," + Curator;

    /// <summary>Получает роли утверждения документов юридической фирмы.</summary>
    public const string FirmApprovers = SuperAdministrator + "," + Administrator + "," + FirmPartner;

    /// <summary>Получает роли загрузки результатов мониторинга законодательства.</summary>
    public const string LegislationManagers = SuperAdministrator + "," + Administrator + "," + Curator + "," + System;

    /// <summary>Получает обе пользовательские стороны Post-MVP документа.</summary>
    public const string DocumentParties = Lawyer + "," + Client;
}
