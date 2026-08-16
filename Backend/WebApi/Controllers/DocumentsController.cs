using Application.Common.Models;
using Application.DTOs;
using Application.Features.ClientPortal;
using Application.Features.Documents;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Contracts;
using WebApi.Seeds;

namespace WebApi.Controllers;

/// <summary>
/// Предоставляет жизненный цикл договорных черновиков, immutable-версий, экспорта и Post-MVP согласования.
/// Каждая операция делегируется MediatR, а проверка владения повторяется в Application-обработчике.
/// </summary>
[Authorize]
public sealed class DocumentsController : ApiControllerBase
{
    /// <summary>Инициализирует контроллер единственным MediatR-диспетчером.</summary>
    /// <param name="sender">MediatR-диспетчер.</param>
    public DocumentsController(ISender sender) : base(sender)
    {
    }

    /// <summary>Создаёт первую ИИ-версию документа из собственного дела и активного шаблона.</summary>
    /// <param name="data">Описание сделки и ссылки на дело/шаблон.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 201 с DraftOperationDto и Location.</returns>
    [Authorize(Roles = DefaultRoles.Lawyer)]
    [HttpPost]
    public async Task<ActionResult<Response<DraftOperationDto>>> Create(
        [FromBody] CreateDraftDto data,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(new CreateDraftCommand(data), cancellationToken);
        return ToActionResult(response, nameof(GetById), new { draftId = response.Data?.DraftId });
    }

    /// <summary>Сохраняет ручную правку как новую immutable-версию собственного документа.</summary>
    /// <param name="draftId">Идентификатор документа.</param>
    /// <param name="data">Полный новый текст и описание изменения.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 200 с созданной версией.</returns>
    [Authorize(Roles = DefaultRoles.Lawyer)]
    [HttpPut("{draftId:guid}")]
    public async Task<ActionResult<Response<DraftOperationDto>>> Update(
        Guid draftId,
        [FromBody] UpdateDraftDto data,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(new UpdateDraftCommand(draftId, data), cancellationToken);
        return ToActionResult(response);
    }

    /// <summary>Повторно генерирует текущую версию по инструкции юриста с атомарным учётом ИИ-квоты.</summary>
    /// <param name="draftId">Идентификатор документа.</param>
    /// <param name="request">Инструкция и описание отличий.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 200 с новой immutable-версией.</returns>
    [Authorize(Roles = DefaultRoles.Lawyer)]
    [HttpPost("{draftId:guid}/regenerate")]
    public async Task<ActionResult<Response<DraftOperationDto>>> Regenerate(
        Guid draftId,
        [FromBody] RegenerateDraftRequest request,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(
            new RegenerateDraftCommand(draftId, request.Instructions, request.ChangeSummary),
            cancellationToken);
        return ToActionResult(response);
    }

    /// <summary>Фиксирует подтверждение ответственности юриста за итоговый текст.</summary>
    /// <param name="draftId">Идентификатор собственного документа.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 204 либо Response ошибки.</returns>
    [Authorize(Roles = DefaultRoles.Lawyer)]
    [HttpPost("{draftId:guid}/responsibility-confirmation")]
    public async Task<ActionResult<Response<bool>>> ConfirmResponsibility(
        Guid draftId,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(new ConfirmResponsibilityCommand(draftId), cancellationToken);
        return ToActionResult(response, noContentOnSuccess: true);
    }

    /// <summary>Экспортирует текущую авторизованную версию в DOCX или PDF.</summary>
    /// <param name="draftId">Идентификатор собственного документа.</param>
    /// <param name="request">Выбранный формат.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 200 с именем, MIME-типом и байтами файла.</returns>
    [Authorize(Roles = DefaultRoles.Lawyer)]
    [HttpPost("{draftId:guid}/exports")]
    public async Task<ActionResult<Response<ExportedDocumentDto>>> Export(
        Guid draftId,
        [FromBody] ExportDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(
            new ExportDocumentCommand(draftId, request.Format),
            cancellationToken);
        return ToActionResult(response);
    }

    /// <summary>Выполняет разрешённое мягкое удаление собственного Draft без уничтожения аудита.</summary>
    /// <param name="draftId">Идентификатор документа.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 204 либо конфликт доменного состояния.</returns>
    [Authorize(Roles = DefaultRoles.Lawyer)]
    [HttpDelete("{draftId:guid}")]
    public async Task<ActionResult<Response<bool>>> Delete(
        Guid draftId,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(new DeleteDraftCommand(draftId), cancellationToken);
        return ToActionResult(response, noContentOnSuccess: true);
    }

    /// <summary>Регистрирует долговечный workflow полного удаления документа и связанного содержимого.</summary>
    /// <param name="draftId">Идентификатор цели.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 202 с идентификатором запроса удаления.</returns>
    [Authorize(Roles = DefaultRoles.Lawyer)]
    [HttpPost("{draftId:guid}/deletion-requests")]
    public async Task<ActionResult<Response<Guid>>> RequestFullDeletion(
        Guid draftId,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(
            new RequestFullDeletionCommand(DeletionTargetType.Draft, draftId),
            cancellationToken);
        return ToActionResult(response);
    }

    /// <summary>Архивирует документ с сохранением содержимого и истории.</summary>
    /// <param name="draftId">Идентификатор документа.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 204 либо Response ошибки.</returns>
    [Authorize(Roles = DefaultRoles.Lawyer)]
    [HttpPost("{draftId:guid}/archive")]
    public async Task<ActionResult<Response<bool>>> Archive(
        Guid draftId,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(new ArchiveDraftCommand(draftId), cancellationToken);
        return ToActionResult(response, noContentOnSuccess: true);
    }

    /// <summary>Возвращает детальную карточку собственного документа и текст текущей версии.</summary>
    /// <param name="draftId">Идентификатор документа.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 200 либо одинаковый HTTP 404 для чужого и отсутствующего ресурса.</returns>
    [Authorize(Roles = DefaultRoles.Lawyer)]
    [HttpGet("{draftId:guid}")]
    public async Task<ActionResult<Response<DraftDetailDto>>> GetById(
        Guid draftId,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(new GetDraftByIdQuery(draftId), cancellationToken);
        return ToActionResult(response);
    }

    /// <summary>Возвращает страницу истории immutable-версий собственного документа.</summary>
    /// <param name="draftId">Идентификатор документа из маршрута.</param>
    /// <param name="filter">Параметры страницы и сортировки.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 200 с PagedResult версий.</returns>
    [Authorize(Roles = DefaultRoles.Lawyer)]
    [HttpGet("{draftId:guid}/versions")]
    public async Task<ActionResult<Response<PagedResult<GetDocumentVersionDto>>>> GetVersions(
        Guid draftId,
        [FromQuery] DocumentVersionFilterParam filter,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(
            new GetDocumentVersionHistoryQuery(draftId, filter),
            cancellationToken);
        return ToActionResult(response);
    }

    /// <summary>Возвращает конкретную авторизованную immutable-версию с расшифрованным текстом.</summary>
    /// <param name="versionId">Идентификатор версии.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 200 либо HTTP 404.</returns>
    [Authorize(Roles = DefaultRoles.Lawyer)]
    [HttpGet("versions/{versionId:guid}")]
    public async Task<ActionResult<Response<DocumentVersionDetailDto>>> GetVersionById(
        Guid versionId,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(new GetDocumentVersionByIdQuery(versionId), cancellationToken);
        return ToActionResult(response);
    }

    /// <summary>Отправляет собственный документ клиенту и сохраняет долговечный срок фоновой проверки.</summary>
    /// <param name="draftId">Идентификатор документа.</param>
    /// <param name="request">Будущий срок ответа.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 204 либо конфликт состояния.</returns>
    [Authorize(Roles = DefaultRoles.Lawyer)]
    [HttpPost("{draftId:guid}/send-to-client")]
    public async Task<ActionResult<Response<bool>>> SendToClient(
        Guid draftId,
        [FromBody] DraftDueDateRequest request,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(
            new SendDraftToClientCommand(draftId, request.DueRespondByDate),
            cancellationToken);
        return ToActionResult(response, noContentOnSuccess: true);
    }

    /// <summary>Передаёт документ на внутреннее утверждение юридической фирмы.</summary>
    /// <param name="draftId">Идентификатор документа.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 204 либо конфликт состояния.</returns>
    [Authorize(Roles = DefaultRoles.Lawyer)]
    [HttpPost("{draftId:guid}/submit-for-firm-approval")]
    public async Task<ActionResult<Response<bool>>> SubmitForFirmApproval(
        Guid draftId,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(new SubmitForFirmApprovalCommand(draftId), cancellationToken);
        return ToActionResult(response, noContentOnSuccess: true);
    }

    /// <summary>Утверждает документ от имени фирмы и устанавливает срок ответа клиента.</summary>
    /// <param name="draftId">Идентификатор документа.</param>
    /// <param name="request">Будущий срок ответа.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 204 либо отказ политики/состояния.</returns>
    [Authorize(Roles = DefaultRoles.FirmApprovers)]
    [HttpPost("{draftId:guid}/firm-approval")]
    public async Task<ActionResult<Response<bool>>> ApproveByFirm(
        Guid draftId,
        [FromBody] DraftDueDateRequest request,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(
            new ApproveDraftCommand(draftId, request.DueRespondByDate),
            cancellationToken);
        return ToActionResult(response, noContentOnSuccess: true);
    }

    /// <summary>Возвращает документ юристу на доработку от имени уполномоченного партнёра.</summary>
    /// <param name="draftId">Идентификатор документа.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 204 либо отказ политики/состояния.</returns>
    [Authorize(Roles = DefaultRoles.FirmApprovers)]
    [HttpPost("{draftId:guid}/request-changes")]
    public async Task<ActionResult<Response<bool>>> RequestChanges(
        Guid draftId,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(new RequestDraftChangesCommand(draftId), cancellationToken);
        return ToActionResult(response, noContentOnSuccess: true);
    }

    /// <summary>Фиксирует принятие документа аутентифицированным клиентом-владельцем дела.</summary>
    /// <param name="draftId">Идентификатор документа.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 204 либо HTTP 404/409.</returns>
    [Authorize(Roles = DefaultRoles.Client)]
    [HttpPost("{draftId:guid}/client-acceptance")]
    public async Task<ActionResult<Response<bool>>> AcceptByClient(
        Guid draftId,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(new ClientAcceptDraftCommand(draftId), cancellationToken);
        return ToActionResult(response, noContentOnSuccess: true);
    }

    /// <summary>Фиксирует отклонение документа аутентифицированным клиентом-владельцем дела.</summary>
    /// <param name="draftId">Идентификатор документа.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 204 либо HTTP 404/409.</returns>
    [Authorize(Roles = DefaultRoles.Client)]
    [HttpPost("{draftId:guid}/client-rejection")]
    public async Task<ActionResult<Response<bool>>> RejectByClient(
        Guid draftId,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(new ClientRejectDraftCommand(draftId), cancellationToken);
        return ToActionResult(response, noContentOnSuccess: true);
    }

    /// <summary>Отзывает ранее отправленный собственный документ до принятия клиентом.</summary>
    /// <param name="draftId">Идентификатор документа.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>HTTP 204 либо конфликт состояния.</returns>
    [Authorize(Roles = DefaultRoles.Lawyer)]
    [HttpPost("{draftId:guid}/revoke")]
    public async Task<ActionResult<Response<bool>>> Revoke(
        Guid draftId,
        CancellationToken cancellationToken)
    {
        var response = await Sender.Send(new RevokeDraftCommand(draftId), cancellationToken);
        return ToActionResult(response, noContentOnSuccess: true);
    }
}
