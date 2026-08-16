using Application.Common.Models;
using Application.DTOs;
using Application.Features.ClientsAndCases;
using Application.Features.Documents;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Seeds;

namespace WebApi.Controllers;

/// <summary>Предоставляет операции с делами и документами только в tenant текущего юриста.</summary>
[Authorize(Roles = DefaultRoles.Lawyer)]
public sealed class CasesController : ApiControllerBase
{
    /// <summary>Инициализирует контроллер MediatR-диспетчером.</summary>
    /// <param name="sender">MediatR-диспетчер.</param>
    public CasesController(ISender sender) : base(sender)
    {
    }

    /// <summary>Создаёт открытое дело для собственного клиента.</summary>
    /// <param name="data">Реквизиты дела.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 201 с идентификатором и Location.</returns>
    [HttpPost]
    public async Task<ActionResult<Response<Guid>>> Create(
        [FromBody] CreateCaseDto data,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(new CreateCaseCommand(data), cancellationToken);
        return ToActionResult(response, nameof(GetById), new { caseId = response.Data });
    }

    /// <summary>Обновляет название и описание собственного открытого дела.</summary>
    /// <param name="caseId">Идентификатор дела.</param>
    /// <param name="data">Разрешённые изменения.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 200 с карточкой либо HTTP 404.</returns>
    [HttpPut("{caseId:guid}")]
    public async Task<ActionResult<Response<CaseDetailDto>>> Update(
        Guid caseId,
        [FromBody] UpdateCaseDto data,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(new UpdateCaseCommand(caseId, data), cancellationToken);
        return ToActionResult(response);
    }

    /// <summary>Закрывает собственное дело доменным методом и возвращает HTTP 204.</summary>
    /// <param name="caseId">Идентификатор дела.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 204 либо Response ошибки.</returns>
    [HttpPost("{caseId:guid}/close")]
    public async Task<ActionResult<Response<bool>>> Close(
        Guid caseId,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(new CloseCaseCommand(caseId), cancellationToken);
        return ToActionResult(response, noContentOnSuccess: true);
    }

    /// <summary>Возвращает детальную карточку собственного дела.</summary>
    /// <param name="caseId">Идентификатор дела.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 200 либо HTTP 404.</returns>
    [HttpGet("{caseId:guid}")]
    public async Task<ActionResult<Response<CaseDetailDto>>> GetById(
        Guid caseId,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(new GetCaseByIdQuery(caseId), cancellationToken);
        return ToActionResult(response);
    }

    /// <summary>Возвращает страницу документов собственного дела.</summary>
    /// <param name="caseId">Идентификатор дела из маршрута.</param>
    /// <param name="filter">Фильтр статуса и пагинации.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 200 с PagedResult.</returns>
    [HttpGet("{caseId:guid}/documents")]
    public async Task<ActionResult<Response<PagedResult<GetDraftDto>>>> GetDocuments(
        Guid caseId,
        [FromQuery] DraftFilterParam filter,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(new GetCaseDocumentsQuery(caseId, filter), cancellationToken);
        return ToActionResult(response);
    }

    /// <summary>Регистрирует долговечный workflow полного удаления собственного дела.</summary>
    /// <param name="caseId">Идентификатор цели.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 202 с идентификатором запроса.</returns>
    [HttpPost("{caseId:guid}/deletion-requests")]
    public async Task<ActionResult<Response<Guid>>> RequestFullDeletion(
        Guid caseId,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(
            new RequestFullDeletionCommand(DeletionTargetType.Case, caseId),
            cancellationToken);
        return ToActionResult(response);
    }
}
