using System.Net;
using Application.Common.CQRS;
using Application.Common.Models;
using Application.DTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.Authentication;

/// <summary>
/// Регистрирует учётную запись Identity, доменный профиль юриста и начальную квоту Free-тарифа.
/// </summary>
/// <param name="Data">Проверенные регистрационные данные.</param>
public sealed record RegisterLawyerCommand(CreateLawyerProfileDto Data) : IApplicationRequest<Guid>;

/// <summary>
/// Проверяет регистрационные поля до обращения к Identity и доменным конструкторам.
/// </summary>
public sealed class RegisterLawyerCommandValidator : AbstractValidator<RegisterLawyerCommand>
{
    /// <summary>Инициализирует исчерпывающие правила регистрации юриста.</summary>
    public RegisterLawyerCommandValidator()
    {
        RuleFor(command => command.Data)
            .NotNull()
            .WithMessage("Данные регистрации обязательны.");

        When(command => command.Data is not null, () =>
        {
            RuleFor(command => command.Data.Email)
                .NotEmpty().WithMessage("Email обязателен.")
                .EmailAddress().WithMessage("Email имеет некорректный формат.")
                .MaximumLength(256).WithMessage("Email не должен превышать 256 символов.");

            RuleFor(command => command.Data.Password)
                .NotEmpty().WithMessage("Пароль обязателен.")
                .MinimumLength(12).WithMessage("Пароль должен содержать не менее 12 символов.")
                .MaximumLength(128).WithMessage("Пароль не должен превышать 128 символов.")
                .Matches("[A-Z]").WithMessage("Пароль должен содержать заглавную латинскую букву.")
                .Matches("[a-z]").WithMessage("Пароль должен содержать строчную латинскую букву.")
                .Matches("[0-9]").WithMessage("Пароль должен содержать цифру.")
                .Matches("[^a-zA-Z0-9]").WithMessage("Пароль должен содержать специальный символ.");

            RuleFor(command => command.Data.FullName)
                .NotEmpty().WithMessage("Полное имя юриста обязательно.")
                .MaximumLength(200).WithMessage("Полное имя не должно превышать 200 символов.");

            RuleFor(command => command.Data.LawFirmName)
                .MaximumLength(300).WithMessage("Название юридической фирмы не должно превышать 300 символов.")
                .When(command => !string.IsNullOrWhiteSpace(command.Data.LawFirmName));

            RuleFor(command => command.Data.PhoneNumber)
                .MaximumLength(30).WithMessage("Номер телефона не должен превышать 30 символов.")
                .Matches("^\\+?[0-9 ()-]{7,30}$").WithMessage("Номер телефона имеет некорректный формат.")
                .When(command => !string.IsNullOrWhiteSpace(command.Data.PhoneNumber));
        });
    }
}

/// <summary>
/// Оркестрирует согласованное создание Identity-пользователя, профиля и квоты,
/// компенсируя создание Identity при невозможности сохранить доменные данные.
/// </summary>
/// <param name="identityService">Порт ASP.NET Core Identity.</param>
/// <param name="profileRepository">Репозиторий профилей юристов.</param>
/// <param name="quotaRepository">Репозиторий квот ИИ.</param>
/// <param name="unitOfWork">Единица атомарной фиксации доменных данных.</param>
/// <param name="clock">Источник UTC-времени.</param>
/// <param name="quotaPolicy">Конфигурируемая политика лимитов.</param>
public sealed class RegisterLawyerCommandHandler(
    IIdentityService identityService,
    ILawyerProfileRepository profileRepository,
    IAiUsageQuotaRepository quotaRepository,
    IUnitOfWork unitOfWork,
    IClock clock,
    IAiQuotaPolicy quotaPolicy)
    : IRequestHandler<RegisterLawyerCommand, Response<Guid>>
{
    /// <summary>
    /// Создаёт аккаунт и возвращает доменный идентификатор юриста без раскрытия Identity-ошибок сверх безопасного набора.
    /// </summary>
    /// <param name="request">Команда регистрации.</param>
    /// <param name="cancellationToken">Токен отмены всех операций.</param>
    /// <returns>Созданный идентификатор либо ожидаемая ошибка.</returns>
    public async Task<Response<Guid>> Handle(
        RegisterLawyerCommand request,
        CancellationToken cancellationToken)
    {
        var identityResult = await identityService.CreateUserAsync(
            request.Data.Email.Trim(),
            request.Data.Password,
            request.Data.PhoneNumber,
            cancellationToken);

        if (!identityResult.Succeeded || identityResult.Value == Guid.Empty)
        {
            return Response<Guid>.Fail(
                identityResult.GetErrorsOrDefault("Не удалось создать учётную запись."),
                HttpStatusCode.Conflict);
        }

        var identityUserId = identityResult.Value;

        try
        {
            var now = clock.UtcNow;
            var profile = new LawyerProfile(
                identityUserId,
                request.Data.FullName,
                request.Data.LawFirmName,
                now);

            var periodStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
            var periodEnd = periodStart.AddMonths(1);
            var quota = new AiUsageQuota(
                profile.Id,
                periodStart,
                periodEnd,
                SubscriptionTier.Free,
                quotaPolicy.GetRequestsLimit(SubscriptionTier.Free));

            await profileRepository.AddAsync(profile, cancellationToken);
            await quotaRepository.AddAsync(quota, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Response<Guid>.Success(
                profile.Id,
                "Профиль юриста успешно зарегистрирован.",
                HttpStatusCode.Created);
        }
        catch
        {
            await identityService.DeleteUserAsync(identityUserId, CancellationToken.None);
            throw;
        }
    }
}

/// <summary>
/// Аутентифицирует юриста по email и паролю без передачи credentials в доменный слой.
/// </summary>
/// <param name="Email">Email учётной записи.</param>
/// <param name="Password">Исходный пароль.</param>
public sealed record LoginCommand(string Email, string Password)
    : IApplicationRequest<AuthenticationTokensDto>;

/// <summary>Проверяет формат credentials до обращения к Identity.</summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    /// <summary>Инициализирует правила входа без различимых ошибок существования аккаунта.</summary>
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage("Email обязателен.")
            .EmailAddress().WithMessage("Email имеет некорректный формат.")
            .MaximumLength(256).WithMessage("Email не должен превышать 256 символов.");
        RuleFor(command => command.Password)
            .NotEmpty().WithMessage("Пароль обязателен.")
            .MaximumLength(128).WithMessage("Пароль не должен превышать 128 символов.");
    }
}

/// <summary>Выполняет безопасную проверку credentials через Identity-порт.</summary>
/// <param name="identityService">Порт аутентификации и выдачи токенов.</param>
public sealed class LoginCommandHandler(IIdentityService identityService)
    : IRequestHandler<LoginCommand, Response<AuthenticationTokensDto>>
{
    /// <summary>
    /// Возвращает токены при успехе или одинаковую ошибку для неверного email, пароля и неактивного аккаунта.
    /// </summary>
    public async Task<Response<AuthenticationTokensDto>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var result = await identityService.AuthenticateAsync(
            request.Email.Trim(),
            request.Password,
            cancellationToken);

        return result.Succeeded && result.Value is not null
            ? Response<AuthenticationTokensDto>.Success(result.Value, "Аутентификация выполнена успешно.")
            : Response<AuthenticationTokensDto>.Fail(
                "Неверные учётные данные или доступ к аккаунту запрещён.",
                HttpStatusCode.Unauthorized);
    }
}

/// <summary>
/// Ротирует refresh-токен и выдаёт новую пару токенов.
/// </summary>
/// <param name="RefreshToken">Одноразовый refresh-токен.</param>
public sealed record RefreshTokenCommand(string RefreshToken)
    : IApplicationRequest<AuthenticationTokensDto>;

/// <summary>Проверяет наличие и разумный размер refresh-токена.</summary>
public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    /// <summary>Инициализирует правила refresh-токена до криптографической проверки.</summary>
    public RefreshTokenCommandValidator()
    {
        RuleFor(command => command.RefreshToken)
            .NotEmpty().WithMessage("Refresh-токен обязателен.")
            .MaximumLength(2048).WithMessage("Refresh-токен имеет недопустимую длину.");
    }
}

/// <summary>Передаёт refresh-токен адаптеру, который проверяет хеш, срок, отзыв и выполняет ротацию.</summary>
/// <param name="identityService">Порт управления токенами Identity.</param>
public sealed class RefreshTokenCommandHandler(IIdentityService identityService)
    : IRequestHandler<RefreshTokenCommand, Response<AuthenticationTokensDto>>
{
    /// <summary>Возвращает новую пару токенов либо унифицированную ошибку авторизации.</summary>
    public async Task<Response<AuthenticationTokensDto>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var result = await identityService.RefreshTokenAsync(request.RefreshToken, cancellationToken);
        return result.Succeeded && result.Value is not null
            ? Response<AuthenticationTokensDto>.Success(result.Value, "Токены успешно обновлены.")
            : Response<AuthenticationTokensDto>.Fail(
                "Refresh-токен недействителен, истёк или уже был использован.",
                HttpStatusCode.Unauthorized);
    }
}

/// <summary>Запрашивает объединённую карточку текущего юриста из Domain и Identity.</summary>
public sealed record GetCurrentLawyerQuery : IApplicationRequest<LawyerProfileDetailDto>;

/// <summary>Фиксирует наличие валидатора для запроса без внешних параметров.</summary>
public sealed class GetCurrentLawyerQueryValidator : AbstractValidator<GetCurrentLawyerQuery>
{
    /// <summary>Создаёт валидатор; аутентификация проверяется доверенным контекстом обработчика.</summary>
    public GetCurrentLawyerQueryValidator()
    {
    }
}

/// <summary>Объединяет доменный профиль и разрешённые Identity-поля текущего пользователя.</summary>
/// <param name="currentUser">Доверенный контекст аутентификации.</param>
/// <param name="profileRepository">Репозиторий доменных профилей.</param>
/// <param name="identityService">Порт безопасных Identity-сведений.</param>
/// <param name="mapper">Явные отображения Domain в DTO.</param>
public sealed class GetCurrentLawyerQueryHandler(
    ICurrentUserContext currentUser,
    ILawyerProfileRepository profileRepository,
    IIdentityService identityService,
    IMapper mapper)
    : IRequestHandler<GetCurrentLawyerQuery, Response<LawyerProfileDetailDto>>
{
    /// <summary>Возвращает только собственный активный профиль текущего Identity-пользователя.</summary>
    public async Task<Response<LawyerProfileDetailDto>> Handle(
        GetCurrentLawyerQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.IdentityUserId is not Guid userId || userId == Guid.Empty)
        {
            return Response<LawyerProfileDetailDto>.Fail(
                "Требуется аутентификация.",
                HttpStatusCode.Unauthorized);
        }

        var profile = await profileRepository.GetByUserIdAsync(userId, cancellationToken);
        var identityUser = await identityService.GetUserAsync(userId, cancellationToken);
        if (profile is null || identityUser is null)
        {
            return Response<LawyerProfileDetailDto>.Fail(
                "Профиль текущего пользователя не найден.",
                HttpStatusCode.NotFound);
        }

        if (!profile.IsActive)
        {
            return Response<LawyerProfileDetailDto>.Fail(
                "Профиль юриста деактивирован.",
                HttpStatusCode.Forbidden);
        }

        var dto = mapper.Map<LawyerProfileDetailDto>(profile) with
        {
            Email = identityUser.Email,
            PhoneNumber = identityUser.PhoneNumber
        };

        return Response<LawyerProfileDetailDto>.Success(dto);
    }
}
