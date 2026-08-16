using Application.Common.Models;
using Application.DTOs;
using Application.Features.TemplateLibrary;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Seeds;

namespace WebApi.Controllers;

/// <summary>Предоставляет чтение активной библиотеки и защищённое кураторское управление шаблонами.</summary>
[Authorize]
public sealed class TemplatesController : ApiControllerBase
{
    /// <summary>Инициализирует контроллер MediatR-диспетчером.</summary>
    /// <param name="sender">MediatR-диспетчер.</param>
    public TemplatesController(ISender sender) : base(sender)
    {
    }

    /// <summary>Создаёт новый активный шаблон только ролью куратора или администратора.</summary>
    /// <param name="data">Двуязычные метаданные шаблона.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 201 с идентификатором либо ошибка разрешения/валидации.</returns>
    [Authorize(Roles = DefaultRoles.TemplateManagers)]
    [HttpPost]
    public async Task<ActionResult<Response<Guid>>> Create(
        [FromBody] CreateTemplateDto data,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(new CreateTemplateCommand(data), cancellationToken);
        return ToActionResult(response);
    }

    /// <summary>Изменяет метаданные шаблона без обхода Application-разрешения.</summary>
    /// <param name="templateId">Идентификатор шаблона.</param>
    /// <param name="data">Разрешённые изменения.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 200 с обновлённой моделью.</returns>
    [Authorize(Roles = DefaultRoles.TemplateManagers)]
    [HttpPut("{templateId:guid}")]
    public async Task<ActionResult<Response<TemplateDetailDto>>> Update(
        Guid templateId,
        [FromBody] UpdateTemplateDto data,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(new UpdateTemplateCommand(templateId, data), cancellationToken);
        return ToActionResult(response);
    }

    /// <summary>Деактивирует шаблон вместо удаления юридически значимой библиотечной истории.</summary>
    /// <param name="templateId">Идентификатор шаблона.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 204 либо Response ошибки.</returns>
    [Authorize(Roles = DefaultRoles.TemplateManagers)]
    [HttpDelete("{templateId:guid}")]
    public async Task<ActionResult<Response<bool>>> Deactivate(
        Guid templateId,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(new DeactivateTemplateCommand(templateId), cancellationToken);
        return ToActionResult(response, noContentOnSuccess: true);
    }

    /// <summary>Возвращает страницу активных шаблонов любому аутентифицированному пользователю.</summary>
    /// <param name="filter">Язык, пагинация и сортировка.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 200 с PagedResult.</returns>
    [HttpGet]
    public async Task<ActionResult<Response<PagedResult<GetTemplateDto>>>> GetPage(
        [FromQuery] TemplateFilterParam filter,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(new GetTemplatesQuery(filter), cancellationToken);
        return ToActionResult(response);
    }

    /// <summary>Возвращает упорядоченные пункты конкретного активного шаблона.</summary>
    /// <param name="templateId">Идентификатор шаблона.</param>
    /// <param name="defaultOnly">Ограничение пунктами по умолчанию.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 200 со списком пунктов.</returns>
    [HttpGet("{templateId:guid}/clause-blocks")]
    public async Task<ActionResult<Response<IReadOnlyList<ClauseBlockDetailDto>>>> GetClauseBlocks(
        Guid templateId,
        [FromQuery] bool defaultOnly,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(
            new GetTemplateClauseBlocksQuery(templateId, defaultOnly),
            cancellationToken);
        return ToActionResult(response);
    }

    /// <summary>Прикрепляет существующий пункт к шаблону с серверной фиксацией route-id.</summary>
    /// <param name="templateId">Идентификатор шаблона из маршрута.</param>
    /// <param name="data">Пункт, порядок и признак включения.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 201 с идентификатором связи.</returns>
    [Authorize(Roles = DefaultRoles.TemplateManagers)]
    [HttpPost("{templateId:guid}/clause-blocks")]
    public async Task<ActionResult<Response<Guid>>> AttachClauseBlock(
        Guid templateId,
        [FromBody] CreateTemplateClauseBlockDto data,
        CancellationToken cancellationToken)
    {
        var routeBoundData = data with { TemplateId = templateId };
        var response = await Sender.Send(
            new AttachClauseBlockToTemplateCommand(routeBoundData),
            cancellationToken);
        return ToActionResult(response);
    }

    /// <summary>Удаляет связь пункта с шаблоном, не удаляя сам переиспользуемый пункт.</summary>
    /// <param name="templateId">Идентификатор шаблона.</param>
    /// <param name="clauseBlockId">Идентификатор пункта.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 204 либо Response ошибки.</returns>
    [Authorize(Roles = DefaultRoles.TemplateManagers)]
    [HttpDelete("{templateId:guid}/clause-blocks/{clauseBlockId:guid}")]
    public async Task<ActionResult<Response<bool>>> DetachClauseBlock(
        Guid templateId,
        Guid clauseBlockId,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(
            new DetachClauseBlockFromTemplateCommand(templateId, clauseBlockId),
            cancellationToken);
        return ToActionResult(response, noContentOnSuccess: true);
    }
}
