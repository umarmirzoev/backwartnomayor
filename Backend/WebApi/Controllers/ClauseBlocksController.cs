using Application.Common.Models;
using Application.DTOs;
using Application.Features.TemplateLibrary;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Seeds;

namespace WebApi.Controllers;

/// <summary>Предоставляет поиск утверждённых пунктов и кураторское управление двуязычной библиотекой.</summary>
[Authorize]
public sealed class ClauseBlocksController : ApiControllerBase
{
    /// <summary>Инициализирует контроллер MediatR-диспетчером.</summary>
    /// <param name="sender">MediatR-диспетчер.</param>
    public ClauseBlocksController(ISender sender) : base(sender)
    {
    }

    /// <summary>Создаёт переиспользуемый двуязычный договорный пункт.</summary>
    /// <param name="data">Категория, заголовок и оба текста.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 201 с идентификатором.</returns>
    [Authorize(Roles = DefaultRoles.TemplateManagers)]
    [HttpPost]
    public async Task<ActionResult<Response<Guid>>> Create(
        [FromBody] CreateClauseBlockDto data,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(new CreateClauseBlockCommand(data), cancellationToken);
        return ToActionResult(response);
    }

    /// <summary>Обновляет обе языковые редакции существующего пункта.</summary>
    /// <param name="clauseBlockId">Идентификатор пункта.</param>
    /// <param name="data">Разрешённые изменения.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 200 с детальной моделью.</returns>
    [Authorize(Roles = DefaultRoles.TemplateManagers)]
    [HttpPut("{clauseBlockId:guid}")]
    public async Task<ActionResult<Response<ClauseBlockDetailDto>>> Update(
        Guid clauseBlockId,
        [FromBody] UpdateClauseBlockDto data,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(
            new UpdateClauseBlockCommand(clauseBlockId, data),
            cancellationToken);
        return ToActionResult(response);
    }

    /// <summary>Возвращает страницу активных пунктов с поиском и фильтром категории.</summary>
    /// <param name="filter">Параметры поиска, страницы и сортировки.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 200 с PagedResult.</returns>
    [HttpGet]
    public async Task<ActionResult<Response<PagedResult<ClauseBlockDetailDto>>>> GetPage(
        [FromQuery] ClauseBlockFilterParam filter,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(new GetClauseBlocksQuery(filter), cancellationToken);
        return ToActionResult(response);
    }
}
