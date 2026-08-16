using Application.Common.Models;
using Application.DTOs;
using Application.Features.ArtificialIntelligence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Contracts;
using WebApi.Seeds;

namespace WebApi.Controllers;

/// <summary>Предоставляет тарифицируемый анализ входящих документов и чтение собственной ИИ-квоты.</summary>
[Authorize(Roles = DefaultRoles.Lawyer)]
public sealed class ArtificialIntelligenceController : ApiControllerBase
{
    /// <summary>Инициализирует контроллер MediatR-диспетчером.</summary>
    /// <param name="sender">MediatR-диспетчер.</param>
    public ArtificialIntelligenceController(ISender sender) : base(sender)
    {
    }

    /// <summary>Анализирует входящий текст через Gemini без создания Draft и с атомарным учётом квоты.</summary>
    /// <param name="request">Текст входящего документа.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 200 со структурированными рисками либо контролируемая ошибка провайдера.</returns>
    [HttpPost("document-reviews")]
    public async Task<ActionResult<Response<IReadOnlyList<DocumentReviewItemDto>>>> ReviewDocument(
        [FromBody] ReviewIncomingDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(
            new ReviewIncomingDocumentCommand(request.Content),
            cancellationToken);
        return ToActionResult(response);
    }

    /// <summary>Возвращает текущий персистентный снимок лимита и остатка ИИ-запросов юриста.</summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 200 с квотой либо HTTP 409 при отсутствии текущего периода.</returns>
    [HttpGet("usage")]
    public async Task<ActionResult<Response<GetAiUsageQuotaDto>>> GetUsage(
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(new GetAiUsageQuery(), cancellationToken);
        return ToActionResult(response);
    }
}
