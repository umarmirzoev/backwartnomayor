using System.Reflection;
using Domain.Entities;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

/// <summary>
/// Представляет единый контекст хранения доменных данных и таблиц ASP.NET Core Identity.
/// Контекст объединяет инфраструктурную учётную запись с изолированным доменным профилем юриста,
/// а все правила доменных таблиц подключает из отдельных Fluent API конфигураций текущей сборки.
/// </summary>
public sealed class AppDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    /// <summary>
    /// Инициализирует контекст с параметрами, зарегистрированными контейнером зависимостей.
    /// </summary>
    /// <param name="options">Параметры EF Core, включая провайдер и строку подключения.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    /// <summary>Получает набор доменных профилей юристов.</summary>
    public DbSet<LawyerProfile> LawyerProfiles => Set<LawyerProfile>();

    /// <summary>Получает набор клиентов.</summary>
    public DbSet<Client> Clients => Set<Client>();

    /// <summary>Получает набор дел клиентов.</summary>
    public DbSet<Case> Cases => Set<Case>();

    /// <summary>Получает набор шаблонов договоров.</summary>
    public DbSet<Template> Templates => Set<Template>();

    /// <summary>Получает набор переиспользуемых блоков пунктов.</summary>
    public DbSet<ClauseBlock> ClauseBlocks => Set<ClauseBlock>();

    /// <summary>Получает набор упорядоченных связей шаблонов и пунктов.</summary>
    public DbSet<TemplateClauseBlock> TemplateClauseBlocks => Set<TemplateClauseBlock>();

    /// <summary>Получает набор договорных черновиков.</summary>
    public DbSet<Draft> Drafts => Set<Draft>();

    /// <summary>Получает набор неизменяемых версий документов.</summary>
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();

    /// <summary>Получает набор уведомлений об изменениях законодательства.</summary>
    public DbSet<LegislationAlert> LegislationAlerts => Set<LegislationAlert>();

    /// <summary>Получает набор связей уведомлений с затронутыми делами.</summary>
    public DbSet<CaseLegislationAlert> CaseLegislationAlerts => Set<CaseLegislationAlert>();

    /// <summary>Получает неизменяемый журнал аудита.</summary>
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    /// <summary>Получает набор периодических квот ИИ-запросов.</summary>
    public DbSet<AiUsageQuota> AiUsageQuotas => Set<AiUsageQuota>();

    /// <summary>Получает набор записей фактического использования ИИ.</summary>
    public DbSet<AiUsageRecord> AiUsageRecords => Set<AiUsageRecord>();

    /// <summary>Получает набор формальных запросов на удаление данных.</summary>
    public DbSet<DataDeletionRequest> DataDeletionRequests => Set<DataDeletionRequest>();

    /// <summary>Получает набор Post-MVP-комментариев к версиям документов.</summary>
    public DbSet<DocumentComment> DocumentComments => Set<DocumentComment>();

    /// <summary>Получает набор Post-MVP-записей электронной подписи.</summary>
    public DbSet<SignatureRecord> SignatureRecords => Set<SignatureRecord>();

    /// <summary>
    /// Применяет стандартную модель Identity, а затем все конфигурации доменных сущностей
    /// и инфраструктурной учётной записи из текущей сборки.
    /// </summary>
    /// <param name="builder">Построитель метаданных модели EF Core.</param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
