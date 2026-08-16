using Application.Common.Models;
using Application.DTOs;
using Application.Features.Signatures;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Contracts;
using WebApi.Seeds;

namespace WebApi.Controllers;

/// <summary>Предоставляет Post-MVP фиксацию подписи, статус подписантов и пересмотр из-за законодательства.</summary>
[Authorize]
public sealed class SignaturesController : ApiControllerBase
{
    /// <summary>Инициализирует контроллер MediatR-диспетчером.</summary>
    /// <param name="sender">MediatR-диспетчер.</param>
    public SignaturesController(ISender sender) : base(sender)
    {
    }

    /// <summary>Фиксирует подпись текущей стороны для route-документа и явно указанной текущей версии.</summary>
    /// <param name="draftId">Идентификатор документа из маршрута.</param>
    /// <param name="data">Версия, способ и версия соглашения без идентификатора подписанта/IP.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 201 со статусом подписей либо контролируемый отказ проверки.</returns>
    [Authorize(Roles = DefaultRoles.DocumentParties)]
    [HttpPost("documents/{draftId:guid}")]
    public async Task<ActionResult<Response<SignatureStatusDto>>> Sign(
        Guid draftId,
        [FromBody] CreateSignatureRecordDto data,
        CancellationToken cancellationToken)
    {
        var routeBoundData = data with { DraftId = draftId };
        var response = await Sender.Send(new SignDocumentCommand(routeBoundData), cancellationToken);
        return ToActionResult(response);
    }

    /// <summary>Возвращает страницу безопасных сведений о подписях доступного документа.</summary>
    /// <param name="draftId">Идентификатор документа.</param>
    /// <param name="filter">Фильтр типа стороны и страницы.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 200 с PagedResult без IP-адресов.</returns>
    [Authorize(Roles = DefaultRoles.DocumentParties)]
    [HttpGet("documents/{draftId:guid}")]
    public async Task<ActionResult<Response<PagedResult<GetSignatureRecordDto>>>> GetStatus(
        Guid draftId,
        [FromQuery] SignatureRecordFilterParam filter,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(
            new GetSignatureStatusQuery(draftId, filter),
            cancellationToken);
        return ToActionResult(response);
    }

    /// <summary>Помечает подписанный документ требующим обновления по доверенному законодательному основанию.</summary>
    /// <param name="draftId">Идентификатор документа.</param>
    /// <param name="request">Идентификатор уведомления законодательства.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 204 либо отказ политики/владения/состояния.</returns>
    [Authorize(Roles = DefaultRoles.LegislationManagers)]
    [HttpPost("documents/{draftId:guid}/requires-update")]
    public async Task<ActionResult<Response<bool>>> MarkRequiresUpdate(
        Guid draftId,
        [FromBody] MarkDraftRequiresUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(
            new MarkDraftRequiresUpdateCommand(draftId, request.LegislationAlertId),
            cancellationToken);
        return ToActionResult(response, noContentOnSuccess: true);
    }
}
