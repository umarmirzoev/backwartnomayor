using Application.Common.Models;
using Application.DTOs;
using Application.Features.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Seeds;

namespace WebApi.Controllers;

/// <summary>
/// Предоставляет регистрацию, вход, ротацию refresh-токена и чтение собственного профиля.
/// Контроллер не обрабатывает пароли и токены самостоятельно, передавая сценарии в MediatR.
/// </summary>
public sealed class AuthenticationController : ApiControllerBase
{
    /// <summary>Инициализирует контроллер диспетчером CQRS.</summary>
    /// <param name="sender">MediatR-диспетчер.</param>
    public AuthenticationController(ISender sender) : base(sender)
    {
    }

    /// <summary>Регистрирует Identity-аккаунт, доменный профиль юриста и Free-квоту.</summary>
    /// <param name="data">Проверяемые регистрационные данные.</param>
    /// <param name="cancellationToken">Токен отмены запроса.</param>
    /// <returns>HTTP 201 с идентификатором профиля либо Response ошибки.</returns>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(Response<Guid>), StatusCodes.Status201Created)]
    public async Task<ActionResult<Response<Guid>>> Register(
        [FromBody] CreateLawyerProfileDto data,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(new RegisterLawyerCommand(data), cancellationToken);
        return ToActionResult(response);
    }

    /// <summary>Проверяет credentials и выдаёт короткий access-токен с ротируемым refresh-токеном.</summary>
    /// <param name="command">Команда с email и паролем.</param>
    /// <param name="cancellationToken">Токен отмены запроса.</param>
    /// <returns>HTTP 200 с токенами либо унифицированный HTTP 401.</returns>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<Response<AuthenticationTokensDto>>> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(command, cancellationToken);
        return ToActionResult(response);
    }

    /// <summary>Проверяет одноразовый refresh-токен и выполняет его обязательную ротацию.</summary>
    /// <param name="command">Команда с исходным refresh-токеном.</param>
    /// <param name="cancellationToken">Токен отмены запроса.</param>
    /// <returns>HTTP 200 с новой парой токенов либо HTTP 401.</returns>
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<Response<AuthenticationTokensDto>>> Refresh(
        [FromBody] RefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(command, cancellationToken);
        return ToActionResult(response);
    }

    /// <summary>Возвращает объединённый Identity/Domain-профиль текущего активного юриста.</summary>
    /// <param name="cancellationToken">Токен отмены запроса.</param>
    /// <returns>HTTP 200 с собственным профилем либо HTTP 404.</returns>
    [Authorize(Roles = DefaultRoles.Lawyer)]
    [HttpGet("me")]
    public async Task<ActionResult<Response<LawyerProfileDetailDto>>> GetCurrent(
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(new GetCurrentLawyerQuery(), cancellationToken);
        return ToActionResult(response);
    }
}
