using Domain.Entities;
using Domain.Enums;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace WebApi.Seeds;

/// <summary>
/// Идемпотентно подготавливает схему разработки, роли Identity и опционального bootstrap-суперадминистратора.
/// Юридические шаблоны и пункты намеренно не выдумываются: их нормативное содержимое отсутствует в спецификации
/// и должно загружаться куратором через защищённые CQRS-операции с аудитом.
/// </summary>
public sealed class DbInitializer
{
    private readonly AppDbContext _dbContext;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAiQuotaPolicy _quotaPolicy;
    private readonly IClock _clock;
    private readonly BootstrapAdminOptions _adminOptions;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DbInitializer> _logger;

    /// <summary>Инициализирует все зависимости начальной подготовки базы и Identity.</summary>
    /// <param name="dbContext">Контекст схемы, Identity и доменных профилей.</param>
    /// <param name="roleManager">Менеджер ролей Identity.</param>
    /// <param name="userManager">Менеджер пользователей Identity.</param>
    /// <param name="quotaPolicy">Политика начального Free-лимита.</param>
    /// <param name="clock">Источник UTC-времени.</param>
    /// <param name="adminOptions">Защищённые параметры bootstrap-администратора.</param>
    /// <param name="environment">Текущая среда запуска.</param>
    /// <param name="logger">Журнал результатов инициализации без секретов.</param>
    public DbInitializer(
        AppDbContext dbContext,
        RoleManager<IdentityRole<Guid>> roleManager,
        UserManager<ApplicationUser> userManager,
        IAiQuotaPolicy quotaPolicy,
        IClock clock,
        IOptions<BootstrapAdminOptions> adminOptions,
        IHostEnvironment environment,
        ILogger<DbInitializer> logger)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(roleManager);
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(quotaPolicy);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(adminOptions);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(logger);
        _dbContext = dbContext;
        _roleManager = roleManager;
        _userManager = userManager;
        _quotaPolicy = quotaPolicy;
        _clock = clock;
        _adminOptions = adminOptions.Value;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Применяет миграции, создаёт все роли и при явном включении восстанавливает полный bootstrap-профиль администратора.
    /// В Development без миграций допускается EnsureCreated; Production завершается ошибкой, чтобы не закрепить немигрируемую схему.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены запуска приложения.</param>
    /// <returns>Задача полной идемпотентной инициализации.</returns>
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await InitializeSchemaAsync(cancellationToken);
        await SeedRolesAsync(cancellationToken);
        if (_adminOptions.Enabled)
        {
            await SeedSuperAdministratorAsync(cancellationToken);
        }
        else
        {
            _logger.LogWarning(
                "Bootstrap-суперадминистратор отключён. Для первичного доступа задайте защищённую секцию {SectionName}.",
                BootstrapAdminOptions.SectionName);
        }
    }

    /// <summary>Применяет миграции либо создаёт временную схему только в среде разработки.</summary>
    /// <param name="cancellationToken">Токен отмены операций базы.</param>
    private async Task InitializeSchemaAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var migrations = _dbContext.Database.GetMigrations();
        if (migrations.Any())
        {
            await _dbContext.Database.MigrateAsync(cancellationToken);
            return;
        }

        if (!_environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "Для запуска вне Development требуется создать и применить EF Core-миграцию.");
        }

        await _dbContext.Database.EnsureCreatedAsync(cancellationToken);
        _logger.LogWarning(
            "EF Core-миграции отсутствуют; в Development использована временная схема EnsureCreated.");
    }

    /// <summary>Идемпотентно создаёт канонический набор транспортных ролей Identity.</summary>
    /// <param name="cancellationToken">Токен отмены между операциями Identity.</param>
    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        foreach (var roleName in DefaultRoles.All)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await _roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var result = await _roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            EnsureIdentitySucceeded(result, $"Не удалось создать роль {roleName}.");
        }
    }

    /// <summary>
    /// Создаёт или восстанавливает Identity-пользователя, административные роли, профиль юриста и текущую Free-квоту.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены последовательности.</param>
    private async Task SeedSuperAdministratorAsync(CancellationToken cancellationToken)
    {
        ValidateBootstrapOptions();
        var normalizedEmail = _adminOptions.Email.Trim();
        var user = await _userManager.FindByEmailAsync(normalizedEmail);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = normalizedEmail,
                Email = normalizedEmail,
                EmailConfirmed = true
            };
            var creationResult = await _userManager.CreateAsync(user, _adminOptions.Password);
            EnsureIdentitySucceeded(creationResult, "Не удалось создать bootstrap-суперадминистратора.");
        }
        else
        {
            // Bootstrap-пароль — часть защищённой конфигурации, а не пользовательский секрет:
            // синхронизируем хеш с текущим значением при каждом запуске, иначе смена пароля
            // в конфиге не имеет эффекта после первого создания пользователя.
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, _adminOptions.Password);
            EnsureIdentitySucceeded(resetResult, "Не удалось синхронизировать пароль bootstrap-суперадминистратора.");
        }

        var requiredRoles = new[]
        {
            DefaultRoles.SuperAdministrator,
            DefaultRoles.Administrator,
            DefaultRoles.Curator,
            DefaultRoles.Lawyer
        };
        var currentRoles = await _userManager.GetRolesAsync(user);
        var missingRoles = requiredRoles.Except(currentRoles, StringComparer.Ordinal).ToArray();
        if (missingRoles.Length > 0)
        {
            EnsureIdentitySucceeded(
                await _userManager.AddToRolesAsync(user, missingRoles),
                "Не удалось назначить bootstrap-суперадминистратору обязательные роли.");
        }

        var profile = await _dbContext.LawyerProfiles
            .SingleOrDefaultAsync(candidate => candidate.UserId == user.Id, cancellationToken);
        if (profile is null)
        {
            var now = _clock.UtcNow;
            profile = new LawyerProfile(user.Id, _adminOptions.FullName, _adminOptions.LawFirmName, now);
            var periodStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
            var quota = new AiUsageQuota(
                profile.Id,
                periodStart,
                periodStart.AddMonths(1),
                SubscriptionTier.Free,
                _quotaPolicy.GetRequestsLimit(SubscriptionTier.Free));
            await _dbContext.LawyerProfiles.AddAsync(profile, cancellationToken);
            await _dbContext.AiUsageQuotas.AddAsync(quota, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Bootstrap-суперадминистратор и его доменный профиль готовы.");
    }

    /// <summary>Проверяет отсутствие пустых или заведомо слабых bootstrap-секретов до создания пользователя.</summary>
    private void ValidateBootstrapOptions()
    {
        if (string.IsNullOrWhiteSpace(_adminOptions.Email)
            || string.IsNullOrWhiteSpace(_adminOptions.FullName)
            || _adminOptions.Password.Length < 16)
        {
            throw new InvalidOperationException(
                "BootstrapAdmin требует email, полное имя и пароль длиной не менее 16 символов.");
        }
    }

    /// <summary>Преобразует неуспех Identity в отказ запуска без журналирования пароля или внутренних хешей.</summary>
    /// <param name="result">Результат операции Identity.</param>
    /// <param name="message">Безопасное описание этапа.</param>
    private static void EnsureIdentitySucceeded(IdentityResult result, string message)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(message);
        }
    }
}
