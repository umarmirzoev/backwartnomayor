using Application.Common.Models;
using Application.DTOs;
using Application.Features.Legislation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Contracts;
using WebApi.Seeds;

namespace WebApi.Controllers;

/// <summary>Предоставляет загрузку результатов мониторинга и tenant-безопасные уведомления юриста.</summary>
[Authorize]
public sealed class LegislationController : ApiControllerBase
{
    /// <summary>Инициализирует контроллер MediatR-диспетчером.</summary>
    /// <param name="sender">MediatR-диспетчер.</param>
    public LegislationController(ISender sender) : base(sender)
    {
    }

    /// <summary>Создаёт append-only уведомление и связи с подтверждёнными делами доверенным мониторингом.</summary>
    /// <param name="request">Содержимое уведомления и уникальные идентификаторы дел.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 201 с идентификатором уведомления.</returns>
    [Authorize(Roles = DefaultRoles.LegislationManagers)]
    [HttpPost("alerts")]
    public async Task<ActionResult<Response<Guid>>> IngestAlert(
        [FromBody] IngestLegislationAlertRequest request,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(
            new IngestLegislationAlertCommand(request.Data, request.CaseIds),
            cancellationToken);
        return ToActionResult(response);
    }

    /// <summary>Отмечает конкретную связь уведомления с собственным делом прочитанной.</summary>
    /// <param name="linkId">Идентификатор связи CaseLegislationAlert.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 204 либо HTTP 404.</returns>
    [Authorize(Roles = DefaultRoles.Lawyer)]
    [HttpPost("alerts/{linkId:guid}/read")]
    public async Task<ActionResult<Response<bool>>> MarkRead(
        Guid linkId,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(new MarkLegislationAlertReadCommand(linkId), cancellationToken);
        return ToActionResult(response, noContentOnSuccess: true);
    }

    /// <summary>Возвращает страницу уведомлений только по делам текущего юриста.</summary>
    /// <param name="filter">Непрочитанность, нижняя дата и пагинация.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 200 с PagedResult связей и уведомлений.</returns>
    [Authorize(Roles = DefaultRoles.Lawyer)]
    [HttpGet("alerts")]
    public async Task<ActionResult<Response<PagedResult<CaseLegislationAlertDetailDto>>>> GetAlerts(
        [FromQuery] LegislationAlertFilterParam filter,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(new GetLegislationAlertsQuery(filter), cancellationToken);
        return ToActionResult(response);
    }
}
