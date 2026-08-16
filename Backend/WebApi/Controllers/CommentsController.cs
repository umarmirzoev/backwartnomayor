using Application.Common.Models;
using Application.DTOs;
using Application.Features.ClientPortal;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Seeds;

namespace WebApi.Controllers;

/// <summary>Предоставляет Post-MVP комментарии сторон к конкретным immutable-версиям документа.</summary>
[Authorize(Roles = DefaultRoles.DocumentParties)]
public sealed class CommentsController : ApiControllerBase
{
    /// <summary>Инициализирует контроллер MediatR-диспетчером.</summary>
    /// <param name="sender">MediatR-диспетчер.</param>
    public CommentsController(ISender sender) : base(sender)
    {
    }

    /// <summary>Добавляет комментарий текущей стороны и принудительно связывает его с route-id версии.</summary>
    /// <param name="documentVersionId">Идентификатор версии из маршрута.</param>
    /// <param name="data">Ссылка на пункт и текст без данных автора.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 201 с идентификатором комментария.</returns>
    [HttpPost("document-versions/{documentVersionId:guid}")]
    public async Task<ActionResult<Response<Guid>>> Create(
        Guid documentVersionId,
        [FromBody] CreateDocumentCommentDto data,
        CancellationToken cancellationToken)
    {
        var routeBoundData = data with { DocumentVersionId = documentVersionId };
        var response = await Sender.Send(
            new AddDocumentCommentCommand(routeBoundData),
            cancellationToken);
        return ToActionResult(response);
    }

    /// <summary>Возвращает страницу доступных комментариев указанной версии без раскрытия чужого документа.</summary>
    /// <param name="documentVersionId">Идентификатор версии.</param>
    /// <param name="filter">Параметры разрешённости и страницы.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 200 с PagedResult либо HTTP 404.</returns>
    [HttpGet("document-versions/{documentVersionId:guid}")]
    public async Task<ActionResult<Response<PagedResult<DocumentCommentDetailDto>>>> GetPage(
        Guid documentVersionId,
        [FromQuery] DocumentCommentFilterParam filter,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(
            new GetDocumentCommentsQuery(documentVersionId, filter),
            cancellationToken);
        return ToActionResult(response);
    }
}
