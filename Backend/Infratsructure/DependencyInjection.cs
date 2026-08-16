using Amazon.Runtime;
using Amazon.S3;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Infrastructure.ArtificialIntelligence;
using Infrastructure.Authorization;
using Infrastructure.Background;
using Infrastructure.Caching;
using Infrastructure.Export;
using Infrastructure.Identity;
using Infrastructure.Options;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Infrastructure.Signatures;
using Infrastructure.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure;

/// <summary>
/// Предоставляет единый инфраструктурный модуль Composition Root: PostgreSQL, Identity, репозитории,
/// Redis, Gemini, S3, экспорт, авторизацию ресурсов и долговечное состояние фоновых задач.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Регистрирует все реализации портов Application-слоя и привязывает их к конфигурации среды.
    /// Секретные значения не имеют кодовых значений по умолчанию и должны поступать из защищённых провайдеров.
    /// </summary>
    /// <param name="services">Коллекция сервисов приложения.</param>
    /// <param name="configuration">Объединённая конфигурация среды.</param>
    /// <returns>Та же коллекция для продолжения Composition Root.</returns>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Строка подключения DefaultConnection не настроена.");
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.EnableRetryOnFailure(maxRetryCount: 3)));

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddSignInManager()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        BindOptions(services, configuration);
        RegisterRepositories(services);

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IAiQuotaPolicy, ConfigurableAiQuotaPolicy>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IResourceAuthorizationService, ResourceAuthorizationService>();
        services.AddScoped<ISignatureVerificationService, SignatureVerificationService>();
        services.AddScoped<IBackgroundTaskScheduler, DatabaseBackedTaskScheduler>();
        services.AddSingleton<IDocumentExportService, DocumentExportService>();

        services.AddSingleton<IAiQuotaCounter, RedisAiQuotaCounter>();

        services.AddHttpClient<IAiDraftingService, GeminiAiDraftingService>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<GeminiOptions>>().Value;
            if (options.TimeoutSeconds is < 5 or > 120 || options.MaxOutputTokens is < 256 or > 65536)
            {
                throw new InvalidOperationException("Параметры тайм-аута или размера ответа Gemini недопустимы.");
            }

            client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/", UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        services.AddSingleton<IAmazonS3>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<DocumentStorageOptions>>().Value;
            if (string.IsNullOrWhiteSpace(options.ServiceUrl)
                || string.IsNullOrWhiteSpace(options.AccessKey)
                || string.IsNullOrWhiteSpace(options.SecretKey)
                || string.IsNullOrWhiteSpace(options.Region))
            {
                throw new InvalidOperationException("Параметры подключения S3 не настроены.");
            }

            var credentials = new BasicAWSCredentials(options.AccessKey, options.SecretKey);
            var s3Configuration = new AmazonS3Config
            {
                ServiceURL = options.ServiceUrl,
                ForcePathStyle = options.ForcePathStyle,
                AuthenticationRegion = options.Region
            };
            return new AmazonS3Client(credentials, s3Configuration);
        });
        services.AddScoped<IDocumentStorageService, S3EncryptedDocumentStorageService>();

        return services;
    }

    /// <summary>Привязывает все инфраструктурные настройки к именованным секциям без копирования секретов.</summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="configuration">Конфигурация среды.</param>
    private static void BindOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AiQuotaOptions>(configuration.GetSection(AiQuotaOptions.SectionName));
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
        services.Configure<GeminiOptions>(configuration.GetSection(GeminiOptions.SectionName));
        services.Configure<DocumentStorageOptions>(configuration.GetSection(DocumentStorageOptions.SectionName));
        services.Configure<DocumentExportOptions>(configuration.GetSection(DocumentExportOptions.SectionName));
        services.Configure<SignatureOptions>(configuration.GetSection(SignatureOptions.SectionName));
    }

    /// <summary>Регистрирует все интерфейсы репозиториев Application их EF Core-реализациями с одним scoped-контекстом.</summary>
    /// <param name="services">Коллекция сервисов.</param>
    private static void RegisterRepositories(IServiceCollection services)
    {
        services.AddScoped<ILawyerProfileRepository, LawyerProfileRepository>();
        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<ICaseRepository, CaseRepository>();
        services.AddScoped<ITemplateRepository, TemplateRepository>();
        services.AddScoped<IClauseBlockRepository, ClauseBlockRepository>();
        services.AddScoped<ITemplateClauseBlockRepository, TemplateClauseBlockRepository>();
        services.AddScoped<IDraftRepository, DraftRepository>();
        services.AddScoped<IDocumentVersionRepository, DocumentVersionRepository>();
        services.AddScoped<ILegislationAlertRepository, LegislationAlertRepository>();
        services.AddScoped<ICaseLegislationAlertRepository, CaseLegislationAlertRepository>();
        services.AddScoped<IAuditLogEntryRepository, AuditLogEntryRepository>();
        services.AddScoped<IAiUsageQuotaRepository, AiUsageQuotaRepository>();
        services.AddScoped<IAiUsageRecordRepository, AiUsageRecordRepository>();
        services.AddScoped<IDataDeletionRequestRepository, DataDeletionRequestRepository>();
        services.AddScoped<IDocumentCommentRepository, DocumentCommentRepository>();
        services.AddScoped<ISignatureRecordRepository, SignatureRecordRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
    }
}
