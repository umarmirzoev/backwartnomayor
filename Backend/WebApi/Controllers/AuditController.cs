using Application.Common.Models;
using Application.DTOs;
using Application.Features.Audit;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Seeds;

namespace WebApi.Controllers;

/// <summary>Предоставляет чтение append-only аудита только после проверки владения целевым ресурсом.</summary>
[Authorize(Roles = DefaultRoles.Lawyer)]
public sealed class AuditController : ApiControllerBase
{
    /// <summary>Инициализирует контроллер MediatR-диспетчером.</summary>
    /// <param name="sender">MediatR-диспетчер.</param>
    public AuditController(ISender sender) : base(sender)
    {
    }

    /// <summary>Возвращает страницу аудита Draft, Case или DocumentVersion из белого списка Application.</summary>
    /// <param name="entityType">Точное техническое имя разрешённого доменного типа.</param>
    /// <param name="entityId">Идентификатор собственного ресурса.</param>
    /// <param name="filter">Фильтр действия, страницы и сортировки.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 200 с PagedResult либо HTTP 404 без раскрытия чужого ресурса.</returns>
    [HttpGet("{entityType}/{entityId:guid}")]
    public async Task<ActionResult<Response<PagedResult<AuditLogEntryDetailDto>>>> GetPage(
        string entityType,
        Guid entityId,
        [FromQuery] AuditLogEntryFilterParam filter,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(
            new GetAuditLogQuery(entityType, entityId, filter),
            cancellationToken);
        return ToActionResult(response);
    }
}
