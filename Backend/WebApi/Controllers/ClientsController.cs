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

/// <summary>Предоставляет tenant-безопасные операции с клиентами текущего юриста и их делами.</summary>
[Authorize(Roles = DefaultRoles.Lawyer)]
public sealed class ClientsController : ApiControllerBase
{
    /// <summary>Инициализирует контроллер диспетчером CQRS без прямого доступа к репозиториям.</summary>
    /// <param name="sender">MediatR-диспетчер.</param>
    public ClientsController(ISender sender) : base(sender)
    {
    }

    /// <summary>Создаёт клиента в tenant текущего юриста.</summary>
    /// <param name="data">Реквизиты физического лица или компании.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 201 с идентификатором и Location либо ошибка.</returns>
    [HttpPost]
    public async Task<ActionResult<Response<Guid>>> Create(
        [FromBody] CreateClientDto data,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(new CreateClientCommand(data), cancellationToken);
        return ToActionResult(response, nameof(GetById), new { clientId = response.Data });
    }

    /// <summary>Обновляет разрешённые данные собственного клиента.</summary>
    /// <param name="clientId">Идентификатор маршрута.</param>
    /// <param name="data">Новые контактные и идентификационные данные.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 200 с карточкой либо одинаковый HTTP 404 для чужого и отсутствующего клиента.</returns>
    [HttpPut("{clientId:guid}")]
    public async Task<ActionResult<Response<ClientDetailDto>>> Update(
        Guid clientId,
        [FromBody] UpdateClientDto data,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(new UpdateClientCommand(clientId, data), cancellationToken);
        return ToActionResult(response);
    }

    /// <summary>Возвращает детальную карточку собственного клиента.</summary>
    /// <param name="clientId">Идентификатор клиента.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 200 либо HTTP 404 без раскрытия чужого ресурса.</returns>
    [HttpGet("{clientId:guid}")]
    public async Task<ActionResult<Response<ClientDetailDto>>> GetById(
        Guid clientId,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(new GetClientByIdQuery(clientId), cancellationToken);
        return ToActionResult(response);
    }

    /// <summary>Возвращает страницу клиентов текущего юриста с белым списком сортировки.</summary>
    /// <param name="filter">Параметры поиска, страницы и сортировки.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 200 с PagedResult.</returns>
    [HttpGet]
    public async Task<ActionResult<Response<PagedResult<GetClientDto>>>> GetPage(
        [FromQuery] ClientFilterParam filter,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(new GetClientsQuery(filter), cancellationToken);
        return ToActionResult(response);
    }

    /// <summary>Возвращает страницу дел указанного собственного клиента.</summary>
    /// <param name="clientId">Идентификатор клиента из маршрута.</param>
    /// <param name="filter">Фильтр дел; route-id проверяется обработчиком.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 200 с делами либо HTTP 404.</returns>
    [HttpGet("{clientId:guid}/cases")]
    public async Task<ActionResult<Response<PagedResult<GetCaseDto>>>> GetCases(
        Guid clientId,
        [FromQuery] CaseFilterParam filter,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(new GetClientCasesQuery(clientId, filter), cancellationToken);
        return ToActionResult(response);
    }

    /// <summary>Регистрирует долговечный запрос полного удаления собственного клиента без синхронного уничтожения данных.</summary>
    /// <param name="clientId">Идентификатор цели.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 202 с идентификатором workflow удаления.</returns>
    [HttpPost("{clientId:guid}/deletion-requests")]
    public async Task<ActionResult<Response<Guid>>> RequestFullDeletion(
        Guid clientId,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(
            new RequestFullDeletionCommand(DeletionTargetType.Client, clientId),
            cancellationToken);
        return ToActionResult(response);
    }
}
