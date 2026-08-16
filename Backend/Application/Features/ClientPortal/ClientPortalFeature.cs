using System.Net;
using Application.Common.CQRS;
using Application.Common.Models;
using Application.Common.Notifications;
using Application.Common.Security;
using Application.Common.Validation;
using Application.DTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.ClientPortal;

/// <summary>Отправляет собственный черновик клиенту и назначает обязательный срок ответа.</summary>
public sealed record SendDraftToClientCommand(Guid DraftId, DateTimeOffset DueRespondByDate)
    : IApplicationRequest<bool>;

/// <summary>Проверяет идентификатор и непустую дату срока ответа.</summary>
public sealed class SendDraftToClientCommandValidator : AbstractValidator<SendDraftToClientCommand>
{
    /// <summary>Создаёт структурные правила отправки; хронология проверяется доменом по серверному времени.</summary>
    public SendDraftToClientCommandValidator()
    {
        RuleFor(command => command.DraftId).NotEmpty().WithMessage("Идентификатор черновика обязателен.");
        RuleFor(command => command.DueRespondByDate)
            .NotEqual(default(DateTimeOffset)).WithMessage("Срок ответа клиента обязателен.");
    }
}

/// <summary>Открывает документ клиентскому порталу только после tenant-проверки и ставит проверку просрочки.</summary>
public sealed class SendDraftToClientCommandHandler(
    ICurrentUserContext currentUser,
    IClock clock,
    IDraftRepository draftRepository,
    IAuditLogEntryRepository auditRepository,
    IUnitOfWork unitOfWork,
    IPublisher publisher) : IRequestHandler<SendDraftToClientCommand, Response<bool>>
{
    /// <summary>Атомарно изменяет статус, задаёт срок и пишет аудит, затем публикует долговечное фоновое задание.</summary>
    public async Task<Response<bool>> Handle(
        SendDraftToClientCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.LawyerId is not Guid lawyerId || lawyerId == Guid.Empty)
        {
            return Response<bool>.Fail("Требуется профиль юриста.", HttpStatusCode.Unauthorized);
        }

        var draft = await draftRepository.GetByIdForLawyerAsync(request.DraftId, lawyerId, cancellationToken);
        if (draft is null)
        {
            return Response<bool>.Fail("Черновик не найден.", HttpStatusCode.NotFound);
        }

        var now = clock.UtcNow;
        draft.ChangeStatus(DocumentStatus.SentToClient, now);
        draft.SetResponseDueDate(request.DueRespondByDate);
        await draftRepository.UpdateAsync(draft, cancellationToken);
        await auditRepository.AddAsync(CreateStatusAudit(draft.Id, lawyerId, now), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await publisher.Publish(
            new DraftExpirationScheduledNotification(draft.Id, request.DueRespondByDate), cancellationToken);
        return Response<bool>.Success(true, "Документ отправлен клиенту.");
    }

    /// <summary>Создаёт запись смены статуса без содержимого документа и персональных данных.</summary>
    private static AuditLogEntry CreateStatusAudit(Guid draftId, Guid lawyerId, DateTimeOffset occurredAt)
    {
        return new AuditLogEntry(
            AuditActorType.Lawyer,
            lawyerId,
            AuditAction.StatusChanged,
            nameof(Draft),
            draftId,
            null,
            occurredAt);
    }
}

/// <summary>Передаёт собственный черновик на внутреннее согласование партнёру юридической фирмы.</summary>
public sealed record SubmitForFirmApprovalCommand(Guid DraftId) : IApplicationRequest<bool>;

/// <summary>Проверяет идентификатор документа, отправляемого на согласование.</summary>
public sealed class SubmitForFirmApprovalCommandValidator
    : AbstractValidator<SubmitForFirmApprovalCommand>
{
    /// <summary>Создаёт правило непустого идентификатора.</summary>
    public SubmitForFirmApprovalCommandValidator()
    {
        RuleFor(command => command.DraftId).NotEmpty().WithMessage("Идентификатор черновика обязателен.");
    }
}

/// <summary>Применяет tenant-безопасный переход Draft → PendingFirmApproval.</summary>
public sealed class SubmitForFirmApprovalCommandHandler(
    ICurrentUserContext currentUser,
    IClock clock,
    IDraftRepository repository,
    IUnitOfWork unitOfWork) : IRequestHandler<SubmitForFirmApprovalCommand, Response<bool>>
{
    /// <summary>Изменяет состояние только документа текущего юриста.</summary>
    public async Task<Response<bool>> Handle(
        SubmitForFirmApprovalCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.LawyerId is not Guid lawyerId || lawyerId == Guid.Empty)
        {
            return Response<bool>.Fail("Требуется профиль юриста.", HttpStatusCode.Unauthorized);
        }

        var draft = await repository.GetByIdForLawyerAsync(request.DraftId, lawyerId, cancellationToken);
        if (draft is null)
        {
            return Response<bool>.Fail("Черновик не найден.", HttpStatusCode.NotFound);
        }

        draft.ChangeStatus(DocumentStatus.PendingFirmApproval, clock.UtcNow);
        await repository.UpdateAsync(draft, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Response<bool>.Success(true, "Черновик передан на внутреннее согласование.");
    }
}

/// <summary>Одобряет документ партнёром фирмы, отправляет клиенту и назначает срок ответа.</summary>
public sealed record ApproveDraftCommand(Guid DraftId, DateTimeOffset DueRespondByDate)
    : IApplicationRequest<bool>;

/// <summary>Проверяет идентификатор и дату ответа для одобряемого документа.</summary>
public sealed class ApproveDraftCommandValidator : AbstractValidator<ApproveDraftCommand>
{
    /// <summary>Создаёт структурные правила одобрения.</summary>
    public ApproveDraftCommandValidator()
    {
        RuleFor(command => command.DraftId).NotEmpty().WithMessage("Идентификатор черновика обязателен.");
        RuleFor(command => command.DueRespondByDate)
            .NotEqual(default(DateTimeOffset)).WithMessage("Срок ответа клиента обязателен.");
    }
}

/// <summary>Проверяет разрешение партнёра и ресурсный доступ до перехода PendingFirmApproval → SentToClient.</summary>
public sealed class ApproveDraftCommandHandler(
    ICurrentUserContext currentUser,
    IApplicationAuthorizationService authorization,
    IResourceAuthorizationService resourceAuthorization,
    IClock clock,
    IDraftRepository repository,
    IUnitOfWork unitOfWork,
    IPublisher publisher) : IRequestHandler<ApproveDraftCommand, Response<bool>>
{
    /// <summary>Одобряет доступный партнёру документ и ставит проверку срока в устойчивую очередь.</summary>
    public async Task<Response<bool>> Handle(ApproveDraftCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.PartyType is not PartyType.Lawyer
            || currentUser.PartyId is not Guid partyId
            || partyId == Guid.Empty)
        {
            return Response<bool>.Fail("Требуется профиль юриста-партнёра.", HttpStatusCode.Unauthorized);
        }

        if (!await authorization.HasPermissionAsync(ApplicationPermission.ApproveFirmDrafts, cancellationToken))
        {
            return Response<bool>.Fail("Недостаточно прав для согласования документов фирмы.", HttpStatusCode.Forbidden);
        }

        if (!await resourceAuthorization.CanAccessDraftAsync(
                request.DraftId, PartyType.Lawyer, partyId, cancellationToken))
        {
            return Response<bool>.Fail("Черновик не найден.", HttpStatusCode.NotFound);
        }

        var draft = await repository.GetByIdAsync(request.DraftId, cancellationToken);
        if (draft is null)
        {
            return Response<bool>.Fail("Черновик не найден.", HttpStatusCode.NotFound);
        }

        var now = clock.UtcNow;
        draft.ChangeStatus(DocumentStatus.SentToClient, now);
        draft.SetResponseDueDate(request.DueRespondByDate);
        await repository.UpdateAsync(draft, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await publisher.Publish(
            new DraftExpirationScheduledNotification(draft.Id, request.DueRespondByDate), cancellationToken);
        return Response<bool>.Success(true, "Документ одобрен и отправлен клиенту.");
    }
}

/// <summary>Возвращает документ партнёром фирмы на доработку владельцу-юристу.</summary>
public sealed record RequestDraftChangesCommand(Guid DraftId) : IApplicationRequest<bool>;

/// <summary>Проверяет идентификатор документа, возвращаемого на доработку.</summary>
public sealed class RequestDraftChangesCommandValidator : AbstractValidator<RequestDraftChangesCommand>
{
    /// <summary>Создаёт правило непустого идентификатора.</summary>
    public RequestDraftChangesCommandValidator()
    {
        RuleFor(command => command.DraftId).NotEmpty().WithMessage("Идентификатор черновика обязателен.");
    }
}

/// <summary>Проверяет роль партнёра и переводит доступный документ обратно в Draft.</summary>
public sealed class RequestDraftChangesCommandHandler(
    ICurrentUserContext currentUser,
    IApplicationAuthorizationService authorization,
    IResourceAuthorizationService resourceAuthorization,
    IClock clock,
    IDraftRepository repository,
    IUnitOfWork unitOfWork) : IRequestHandler<RequestDraftChangesCommand, Response<bool>>
{
    /// <summary>Возвращает одинаковый 404 для чужого и отсутствующего документа.</summary>
    public async Task<Response<bool>> Handle(
        RequestDraftChangesCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.PartyType is not PartyType.Lawyer
            || currentUser.PartyId is not Guid partyId
            || partyId == Guid.Empty)
        {
            return Response<bool>.Fail("Требуется профиль юриста-партнёра.", HttpStatusCode.Unauthorized);
        }

        if (!await authorization.HasPermissionAsync(ApplicationPermission.ApproveFirmDrafts, cancellationToken))
        {
            return Response<bool>.Fail("Недостаточно прав для согласования документов фирмы.", HttpStatusCode.Forbidden);
        }

        if (!await resourceAuthorization.CanAccessDraftAsync(
                request.DraftId, PartyType.Lawyer, partyId, cancellationToken))
        {
            return Response<bool>.Fail("Черновик не найден.", HttpStatusCode.NotFound);
        }

        var draft = await repository.GetByIdAsync(request.DraftId, cancellationToken);
        if (draft is null)
        {
            return Response<bool>.Fail("Черновик не найден.", HttpStatusCode.NotFound);
        }

        draft.ChangeStatus(DocumentStatus.Draft, clock.UtcNow);
        await repository.UpdateAsync(draft, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Response<bool>.Success(true, "Документ возвращён на доработку.");
    }
}

/// <summary>Добавляет неизменяемый комментарий текущей стороны к доступной версии документа.</summary>
public sealed record AddDocumentCommentCommand(CreateDocumentCommentDto Data) : IApplicationRequest<Guid>;

/// <summary>Проверяет версию, необязательную ссылку на пункт и безопасный размер текста комментария.</summary>
public sealed class AddDocumentCommentCommandValidator : AbstractValidator<AddDocumentCommentCommand>
{
    /// <summary>Создаёт правила добавления комментария.</summary>
    public AddDocumentCommentCommandValidator()
    {
        RuleFor(command => command.Data).NotNull().WithMessage("Данные комментария обязательны.");
        When(command => command.Data is not null, () =>
        {
            RuleFor(command => command.Data.DocumentVersionId)
                .NotEmpty().WithMessage("Идентификатор версии документа обязателен.");
            RuleFor(command => command.Data.ClauseBlockReference)
                .Must(id => !id.HasValue || id.Value != Guid.Empty)
                .WithMessage("Ссылка на договорный пункт не может быть пустым идентификатором.");
            RuleFor(command => command.Data.Text)
                .NotEmpty().WithMessage("Текст комментария обязателен.")
                .MaximumLength(ValidationRules.MaximumAiPromptLength)
                .WithMessage($"Комментарий не должен превышать {ValidationRules.MaximumAiPromptLength} символов.");
        });
    }
}

/// <summary>Получает автора только из claims-контекста и проверяет доступ к версии до сохранения комментария.</summary>
public sealed class AddDocumentCommentCommandHandler(
    ICurrentUserContext currentUser,
    IResourceAuthorizationService authorization,
    IClauseBlockRepository clauseBlockRepository,
    IDocumentCommentRepository commentRepository,
    IClock clock,
    IUnitOfWork unitOfWork) : IRequestHandler<AddDocumentCommentCommand, Response<Guid>>
{
    /// <summary>Предотвращает подмену автора и ссылки на несуществующий библиотечный пункт.</summary>
    public async Task<Response<Guid>> Handle(
        AddDocumentCommentCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.PartyType is not PartyType partyType
            || currentUser.PartyId is not Guid partyId
            || partyId == Guid.Empty)
        {
            return Response<Guid>.Fail("Требуется аутентифицированная сторона документа.", HttpStatusCode.Unauthorized);
        }

        if (!await authorization.CanAccessDocumentVersionAsync(
                request.Data.DocumentVersionId, partyType, partyId, cancellationToken))
        {
            return Response<Guid>.Fail("Версия документа не найдена.", HttpStatusCode.NotFound);
        }

        if (request.Data.ClauseBlockReference.HasValue
            && !await clauseBlockRepository.ExistsByIdAsync(
                request.Data.ClauseBlockReference.Value, cancellationToken))
        {
            return Response<Guid>.Fail("Указанный договорный пункт не найден.", HttpStatusCode.NotFound);
        }

        var comment = new DocumentComment(
            request.Data.DocumentVersionId,
            partyType,
            partyId,
            request.Data.ClauseBlockReference,
            request.Data.Text,
            clock.UtcNow);
        await commentRepository.AddAsync(comment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Response<Guid>.Success(comment.Id, "Комментарий добавлен.", HttpStatusCode.Created);
    }
}

/// <summary>Запрашивает страницу комментариев доступной версии документа для юриста или клиента.</summary>
public sealed record GetDocumentCommentsQuery(Guid DocumentVersionId, DocumentCommentFilterParam Filter)
    : IApplicationRequest<PagedResult<DocumentCommentDetailDto>>;

/// <summary>Проверяет согласованность идентификатора версии и параметры страницы комментариев.</summary>
public sealed class GetDocumentCommentsQueryValidator : AbstractValidator<GetDocumentCommentsQuery>
{
    /// <summary>Создаёт правила чтения комментариев.</summary>
    public GetDocumentCommentsQueryValidator()
    {
        RuleFor(query => query.DocumentVersionId)
            .NotEmpty().WithMessage("Идентификатор версии документа обязателен.");
        RuleFor(query => query.Filter).NotNull().WithMessage("Параметры фильтрации обязательны.");
        When(query => query.Filter is not null, () =>
        {
            this.AddPaginationRules(
                query => query.Filter.PageNumber,
                query => query.Filter.PageSize,
                query => query.Filter.SortBy);
            RuleFor(query => query.Filter.DocumentVersionId)
                .Must((query, id) => !id.HasValue || id.Value == query.DocumentVersionId)
                .WithMessage("Идентификатор версии в фильтре не должен отличаться от маршрута.");
        });
    }
}

/// <summary>Авторизует ресурс до вызова репозитория, который не принимает идентификатор стороны.</summary>
public sealed class GetDocumentCommentsQueryHandler(
    ICurrentUserContext currentUser,
    IResourceAuthorizationService authorization,
    IDocumentCommentRepository repository,
    IMapper mapper) : IRequestHandler<GetDocumentCommentsQuery, Response<PagedResult<DocumentCommentDetailDto>>>
{
    /// <summary>Возвращает комментарии только после успешной проверки доступа к самой версии.</summary>
    public async Task<Response<PagedResult<DocumentCommentDetailDto>>> Handle(
        GetDocumentCommentsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.PartyType is not PartyType partyType
            || currentUser.PartyId is not Guid partyId
            || partyId == Guid.Empty)
        {
            return Response<PagedResult<DocumentCommentDetailDto>>.Fail(
                "Требуется аутентифицированная сторона документа.", HttpStatusCode.Unauthorized);
        }

        if (!await authorization.CanAccessDocumentVersionAsync(
                request.DocumentVersionId, partyType, partyId, cancellationToken))
        {
            return Response<PagedResult<DocumentCommentDetailDto>>.Fail(
                "Версия документа не найдена.", HttpStatusCode.NotFound);
        }

        var skip = ValidationRules.CalculateSkip(request.Filter.PageNumber, request.Filter.PageSize);
        var comments = await repository.GetByVersionAsync(
            request.DocumentVersionId,
            request.Filter.IncludeResolved,
            skip,
            request.Filter.PageSize,
            cancellationToken);
        var count = await repository.CountByVersionAsync(
            request.DocumentVersionId, request.Filter.IncludeResolved, cancellationToken);
        var items = mapper.Map<IReadOnlyList<DocumentCommentDetailDto>>(comments);
        return Response<PagedResult<DocumentCommentDetailDto>>.Success(
            new PagedResult<DocumentCommentDetailDto>(
                items, count, request.Filter.PageNumber, request.Filter.PageSize));
    }
}

/// <summary>Фиксирует принятие клиентом текущей редакции документа.</summary>
public sealed record ClientAcceptDraftCommand(Guid DraftId) : IApplicationRequest<bool>;

/// <summary>Проверяет идентификатор принимаемого документа.</summary>
public sealed class ClientAcceptDraftCommandValidator : AbstractValidator<ClientAcceptDraftCommand>
{
    /// <summary>Создаёт правило непустого идентификатора.</summary>
    public ClientAcceptDraftCommandValidator()
    {
        RuleFor(command => command.DraftId).NotEmpty().WithMessage("Идентификатор документа обязателен.");
    }
}

/// <summary>Разрешает переход в AcceptedByClient только клиенту с ресурсным доступом.</summary>
public sealed class ClientAcceptDraftCommandHandler(
    ICurrentUserContext currentUser,
    IResourceAuthorizationService authorization,
    IClock clock,
    IDraftRepository repository,
    IUnitOfWork unitOfWork) : IRequestHandler<ClientAcceptDraftCommand, Response<bool>>
{
    /// <summary>Не принимает ClientId из команды и защищает документ от IDOR.</summary>
    public async Task<Response<bool>> Handle(ClientAcceptDraftCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.PartyType is not PartyType.Client
            || currentUser.PartyId is not Guid clientId
            || clientId == Guid.Empty)
        {
            return Response<bool>.Fail("Требуется профиль клиента.", HttpStatusCode.Unauthorized);
        }

        if (!await authorization.CanAccessDraftAsync(
                request.DraftId, PartyType.Client, clientId, cancellationToken))
        {
            return Response<bool>.Fail("Документ не найден.", HttpStatusCode.NotFound);
        }

        var draft = await repository.GetByIdAsync(request.DraftId, cancellationToken);
        if (draft is null)
        {
            return Response<bool>.Fail("Документ не найден.", HttpStatusCode.NotFound);
        }

        draft.ChangeStatus(DocumentStatus.AcceptedByClient, clock.UtcNow);
        await repository.UpdateAsync(draft, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Response<bool>.Success(true, "Текущая редакция принята клиентом.");
    }
}

/// <summary>Фиксирует отказ клиента от документа или сделки.</summary>
public sealed record ClientRejectDraftCommand(Guid DraftId) : IApplicationRequest<bool>;

/// <summary>Проверяет идентификатор отклоняемого документа.</summary>
public sealed class ClientRejectDraftCommandValidator : AbstractValidator<ClientRejectDraftCommand>
{
    /// <summary>Создаёт правило непустого идентификатора.</summary>
    public ClientRejectDraftCommandValidator()
    {
        RuleFor(command => command.DraftId).NotEmpty().WithMessage("Идентификатор документа обязателен.");
    }
}

/// <summary>Разрешает отказ только аутентифицированному клиенту с доступом к документу.</summary>
public sealed class ClientRejectDraftCommandHandler(
    ICurrentUserContext currentUser,
    IResourceAuthorizationService authorization,
    IClock clock,
    IDraftRepository repository,
    IUnitOfWork unitOfWork) : IRequestHandler<ClientRejectDraftCommand, Response<bool>>
{
    /// <summary>Переводит доступный документ в RejectedByClient через доменный переход.</summary>
    public async Task<Response<bool>> Handle(ClientRejectDraftCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.PartyType is not PartyType.Client
            || currentUser.PartyId is not Guid clientId
            || clientId == Guid.Empty)
        {
            return Response<bool>.Fail("Требуется профиль клиента.", HttpStatusCode.Unauthorized);
        }

        if (!await authorization.CanAccessDraftAsync(
                request.DraftId, PartyType.Client, clientId, cancellationToken))
        {
            return Response<bool>.Fail("Документ не найден.", HttpStatusCode.NotFound);
        }

        var draft = await repository.GetByIdAsync(request.DraftId, cancellationToken);
        if (draft is null)
        {
            return Response<bool>.Fail("Документ не найден.", HttpStatusCode.NotFound);
        }

        draft.ChangeStatus(DocumentStatus.RejectedByClient, clock.UtcNow);
        await repository.UpdateAsync(draft, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Response<bool>.Success(true, "Документ отклонён клиентом.");
    }
}

/// <summary>Отзывает отправленный документ юристом-владельцем до окончательного решения клиента.</summary>
public sealed record RevokeDraftCommand(Guid DraftId) : IApplicationRequest<bool>;

/// <summary>Проверяет идентификатор отзываемого документа.</summary>
public sealed class RevokeDraftCommandValidator : AbstractValidator<RevokeDraftCommand>
{
    /// <summary>Создаёт правило непустого идентификатора.</summary>
    public RevokeDraftCommandValidator()
    {
        RuleFor(command => command.DraftId).NotEmpty().WithMessage("Идентификатор документа обязателен.");
    }
}

/// <summary>Применяет tenant-безопасный переход SentToClient → RevokedByLawyer.</summary>
public sealed class RevokeDraftCommandHandler(
    ICurrentUserContext currentUser,
    IClock clock,
    IDraftRepository repository,
    IUnitOfWork unitOfWork) : IRequestHandler<RevokeDraftCommand, Response<bool>>
{
    /// <summary>Отзывает только документ текущего юриста.</summary>
    public async Task<Response<bool>> Handle(RevokeDraftCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.LawyerId is not Guid lawyerId || lawyerId == Guid.Empty)
        {
            return Response<bool>.Fail("Требуется профиль юриста.", HttpStatusCode.Unauthorized);
        }

        var draft = await repository.GetByIdForLawyerAsync(request.DraftId, lawyerId, cancellationToken);
        if (draft is null)
        {
            return Response<bool>.Fail("Документ не найден.", HttpStatusCode.NotFound);
        }

        draft.ChangeStatus(DocumentStatus.RevokedByLawyer, clock.UtcNow);
        await repository.UpdateAsync(draft, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Response<bool>.Success(true, "Документ отозван юристом.");
    }
}

/// <summary>Отмечает отправленный документ просроченным по системному таймеру.</summary>
public sealed record MarkDraftExpiredCommand(Guid DraftId) : IApplicationRequest<bool>;

/// <summary>Проверяет идентификатор документа фоновой проверки.</summary>
public sealed class MarkDraftExpiredCommandValidator : AbstractValidator<MarkDraftExpiredCommand>
{
    /// <summary>Создаёт правило непустого идентификатора.</summary>
    public MarkDraftExpiredCommandValidator()
    {
        RuleFor(command => command.DraftId).NotEmpty().WithMessage("Идентификатор документа обязателен.");
    }
}

/// <summary>Разрешает переход в Expired только доверенному системному исполнителю и после наступления срока.</summary>
public sealed class MarkDraftExpiredCommandHandler(
    ICurrentUserContext currentUser,
    IClock clock,
    IDraftRepository repository,
    IUnitOfWork unitOfWork) : IRequestHandler<MarkDraftExpiredCommand, Response<bool>>
{
    /// <summary>Повторный запуск после уже изменившегося статуса возвращает конфликт через доменный инвариант.</summary>
    public async Task<Response<bool>> Handle(MarkDraftExpiredCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsSystem)
        {
            return Response<bool>.Fail("Операция доступна только системной фоновой задаче.", HttpStatusCode.Forbidden);
        }

        var draft = await repository.GetByIdAsync(request.DraftId, cancellationToken);
        if (draft is null)
        {
            return Response<bool>.Fail("Документ не найден.", HttpStatusCode.NotFound);
        }

        var now = clock.UtcNow;
        if (draft.Status != DocumentStatus.SentToClient)
        {
            return Response<bool>.Success(true, "Документ уже не ожидает ответа клиента.");
        }

        if (!draft.DueRespondByDate.HasValue || draft.DueRespondByDate.Value > now)
        {
            return Response<bool>.Fail("Срок ответа клиента ещё не наступил.", HttpStatusCode.Conflict);
        }

        draft.ChangeStatus(DocumentStatus.Expired, now);
        await repository.UpdateAsync(draft, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Response<bool>.Success(true, "Документ отмечен просроченным.");
    }
}
