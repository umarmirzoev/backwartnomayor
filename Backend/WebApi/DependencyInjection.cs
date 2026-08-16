using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using Application.Common.Models;
using Application.Interfaces.Services;
using Infrastructure.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using WebApi.Background;
using WebApi.Options;
using WebApi.Security;
using WebApi.Seeds;

namespace WebApi;

/// <summary>
/// Предоставляет регистрацию Presentation-слоя: контроллеров, единых HTTP-ошибок, JWT, RBAC-политик,
/// CORS, Swagger, текущего пользователя, инициализации данных и фонового работника.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Регистрирует все сервисы WebAPI и проверяет параметры, влияющие на внешнюю границу безопасности.
    /// </summary>
    /// <param name="services">Коллекция сервисов Composition Root.</param>
    /// <param name="configuration">Конфигурация приложения.</param>
    /// <returns>Та же коллекция для продолжения настройки.</returns>
    public static IServiceCollection AddWebApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddControllers()
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState.Values
                    .SelectMany(value => value.Errors)
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "Тело или параметры запроса имеют некорректный формат."
                        : error.ErrorMessage)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
                return new BadRequestObjectResult(
                    Response<object>.Fail(
                        errors.Length > 0 ? errors : ["Запрос имеет некорректный формат."],
                        HttpStatusCode.BadRequest));
            };
        });

        services.AddHttpContextAccessor();
        services.AddSingleton<SystemExecutionContext>();
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        services.AddScoped<IApplicationAuthorizationService, ApplicationAuthorizationService>();

        ConfigureAuthenticationAndAuthorization(services, configuration);
        ConfigureCors(services, configuration);
        ConfigureSwagger(services);

        services.Configure<BootstrapAdminOptions>(
            configuration.GetSection(BootstrapAdminOptions.SectionName));
        services.Configure<BackgroundProcessingOptions>(
            configuration.GetSection(BackgroundProcessingOptions.SectionName));
        services.AddScoped<DbInitializer>();
        services.AddHostedService<DraftExpirationBackgroundService>();

        return services;
    }

    /// <summary>Настраивает строгую JWT-проверку и role-based политики прикладных разрешений.</summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="configuration">Конфигурация JWT.</param>
    private static void ConfigureAuthenticationAndAuthorization(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Секция JWT отсутствует.");
        if (string.IsNullOrWhiteSpace(jwt.Issuer)
            || string.IsNullOrWhiteSpace(jwt.Audience)
            || Encoding.UTF8.GetByteCount(jwt.SigningKey) < 32)
        {
            throw new InvalidOperationException(
                "JWT требует Issuer, Audience и секрет подписи длиной не менее 32 байтов.");
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.RequireHttpsMetadata = true;
                options.SaveToken = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier,
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role
                };
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        await AuthenticationResponseWriter.WriteAsync(
                            context.HttpContext,
                            HttpStatusCode.Unauthorized,
                            "Требуется действительный JWT access-токен.");
                    },
                    OnForbidden = context => AuthenticationResponseWriter.WriteAsync(
                        context.HttpContext,
                        HttpStatusCode.Forbidden,
                        "Недостаточно прав для выполнения операции.")
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                AuthorizationPolicies.ManageTemplateLibrary,
                policy => policy.RequireRole(DefaultRoles.SuperAdministrator, DefaultRoles.Administrator, DefaultRoles.Curator));
            options.AddPolicy(
                AuthorizationPolicies.ApproveFirmDrafts,
                policy => policy.RequireRole(DefaultRoles.SuperAdministrator, DefaultRoles.Administrator, DefaultRoles.FirmPartner));
            options.AddPolicy(
                AuthorizationPolicies.ManageLegislationMonitoring,
                policy => policy.RequireRole(
                    DefaultRoles.SuperAdministrator,
                    DefaultRoles.Administrator,
                    DefaultRoles.Curator,
                    DefaultRoles.System));
            options.AddPolicy(
                AuthorizationPolicies.ExecuteDataDeletion,
                policy => policy.RequireRole(DefaultRoles.SuperAdministrator, DefaultRoles.Administrator, DefaultRoles.System));
        });
    }

    /// <summary>Настраивает именованную CORS-политику по точному белому списку источников.</summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="configuration">Конфигурация разрешённых источников.</param>
    private static void ConfigureCors(IServiceCollection services, IConfiguration configuration)
    {
        var cors = configuration.GetSection(WebCorsOptions.SectionName).Get<WebCorsOptions>()
            ?? new WebCorsOptions();
        var origins = cors.AllowedOrigins
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin.TrimEnd('/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        services.AddCors(options => options.AddPolicy(
            "FrontendClients",
            policy =>
            {
                if (origins.Length > 0)
                {
                    policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
                }
            }));
    }

    /// <summary>Регистрирует OpenAPI и Bearer-схему, отображаемую кнопкой Authorize в Swagger UI.</summary>
    /// <param name="services">Коллекция сервисов.</param>
    private static void ConfigureSwagger(IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "ShartnomaYor API",
                Version = "v1",
                Description = "Защищённый API договорной работы и ИИ-помощника юриста."
            });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Введите JWT access-токен без префикса Bearer."
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                }] = []
            });
        });
    }
}
