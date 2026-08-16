using System.Net;
using Application.Common.CQRS;
using Application.Common.Models;
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

namespace Application.Features.Signatures;

/// <summary>
/// Фиксирует юридически значимую подпись текущей стороны для конкретной текущей версии документа.
/// Тип и идентификатор подписанта, время и IP-адрес никогда не принимаются из тела команды.
/// </summary>
public sealed record SignDocumentCommand(CreateSignatureRecordDto Data)
    : IApplicationRequest<SignatureStatusDto>;

/// <summary>Проверяет ссылки, способ подписи и версию явно принятого соглашения.</summary>
public sealed class SignDocumentCommandValidator : AbstractValidator<SignDocumentCommand>
{
    /// <summary>Создаёт структурные правила запроса подписи.</summary>
    public SignDocumentCommandValidator()
    {
        RuleFor(command => command.Data).NotNull().WithMessage("Данные подписи обязательны.");
        When(command => command.Data is not null, () =>
        {
            RuleFor(command => command.Data.DraftId)
                .NotEmpty().WithMessage("Идентификатор документа обязателен.");
            RuleFor(command => command.Data.DocumentVersionId)
                .NotEmpty().WithMessage("Идентификатор версии документа обязателен.");
            RuleFor(command => command.Data.Method)
                .IsInEnum().WithMessage("Указан недопустимый способ подписи.");
            RuleFor(command => command.Data.ConsentAgreementVersion)
                .NotEmpty().WithMessage("Версия принятого соглашения обязательна.")
                .MaximumLength(50).WithMessage("Версия соглашения не должна превышать 50 символов.");
        });
    }
}

/// <summary>Проверяет ресурс, доказательство подписи и уникальность стороны до append-only фиксации.</summary>
public sealed class SignDocumentCommandHandler(
    ICurrentUserContext currentUser,
    IResourceAuthorizationService authorization,
    ISignatureVerificationService verificationService,
    IClock clock,
    IDraftRepository draftRepository,
    ISignatureRecordRepository signatureRepository,
    IAuditLogEntryRepository auditRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<SignDocumentCommand, Response<SignatureStatusDto>>
{
    /// <summary>Переводит принятый документ в ожидание подписей и в Signed только при наличии обеих типов сторон.</summary>
    public async Task<Response<SignatureStatusDto>> Handle(
        SignDocumentCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.PartyType is not PartyType signerType
            || currentUser.PartyId is not Guid signerId
            || signerId == Guid.Empty)
        {
            return Response<SignatureStatusDto>.Fail(
                "Требуется аутентифицированная сторона документа.", HttpStatusCode.Unauthorized);
        }

        if (string.IsNullOrWhiteSpace(currentUser.IpAddress))
        {
            return Response<SignatureStatusDto>.Fail(
                "Не удалось достоверно определить IP-адрес подписанта.", HttpStatusCode.BadRequest);
        }

        var canAccessDraft = await authorization.CanAccessDraftAsync(
            request.Data.DraftId, signerType, signerId, cancellationToken);
        var canAccessVersion = await authorization.CanAccessDocumentVersionAsync(
            request.Data.DocumentVersionId, signerType, signerId, cancellationToken);
        if (!canAccessDraft || !canAccessVersion)
        {
            return Response<SignatureStatusDto>.Fail("Документ не найден.", HttpStatusCode.NotFound);
        }

        var draft = await draftRepository.GetByIdAsync(request.Data.DraftId, cancellationToken);
        if (draft is null || draft.CurrentVersionId != request.Data.DocumentVersionId)
        {
            return Response<SignatureStatusDto>.Fail(
                "Подписывать разрешено только текущую версию документа.", HttpStatusCode.Conflict);
        }

        if (draft.Status is not (DocumentStatus.AcceptedByClient or DocumentStatus.AwaitingSignature))
        {
            return Response<SignatureStatusDto>.Fail(
                "Документ не находится в состоянии, допускающем подписание.", HttpStatusCode.Conflict);
        }

        if (await signatureRepository.ExistsForSignerAsync(
                draft.Id, signerType, signerId, cancellationToken))
        {
            return Response<SignatureStatusDto>.Fail(
                "Текущая сторона уже подписала этот документ.", HttpStatusCode.Conflict);
        }

        var verification = await verificationService.VerifyAsync(
            draft.Id,
            request.Data.DocumentVersionId,
            signerType,
            signerId,
            request.Data.Method,
            request.Data.ConsentAgreementVersion,
            cancellationToken);
        if (!verification.Succeeded || verification.Value != true)
        {
            return Response<SignatureStatusDto>.Fail(
                verification.GetErrorsOrDefault("Доказательство подписи не прошло проверку."),
                HttpStatusCode.UnprocessableEntity);
        }

        var existingSignatures = await signatureRepository.GetByDraftAsync(draft.Id, cancellationToken);
        var now = clock.UtcNow;
        if (draft.Status == DocumentStatus.AcceptedByClient)
        {
            draft.ChangeStatus(DocumentStatus.AwaitingSignature, now);
        }

        var signature = new SignatureRecord(
            draft.Id,
            request.Data.DocumentVersionId,
            signerType,
            signerId,
            request.Data.Method,
            request.Data.ConsentAgreementVersion,
            now,
            currentUser.IpAddress);
        var signedPartyTypes = existingSignatures
            .Select(existing => existing.SignerType)
            .Append(signerType)
            .Distinct()
            .ToHashSet();
        if (signedPartyTypes.Contains(PartyType.Lawyer) && signedPartyTypes.Contains(PartyType.Client))
        {
            draft.ChangeStatus(DocumentStatus.Signed, now);
        }

        await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
        {
            await signatureRepository.AddAsync(signature, transactionToken);
            await draftRepository.UpdateAsync(draft, transactionToken);
            await auditRepository.AddAsync(
                new AuditLogEntry(
                    signerType == PartyType.Lawyer ? AuditActorType.Lawyer : AuditActorType.Client,
                    signerId,
                    AuditAction.StatusChanged,
                    nameof(Draft),
                    draft.Id,
                    null,
                    now),
                transactionToken);
            await unitOfWork.SaveChangesAsync(transactionToken);
        }, cancellationToken);
        return Response<SignatureStatusDto>.Success(
            new SignatureStatusDto(draft.Id, draft.Status.ToString(), existingSignatures.Count + 1),
            "Подпись зафиксирована.",
            HttpStatusCode.Created);
    }
}

/// <summary>Запрашивает страницу безопасных сведений о подписях доступного документа.</summary>
public sealed record GetSignatureStatusQuery(Guid DraftId, SignatureRecordFilterParam Filter)
    : IApplicationRequest<PagedResult<GetSignatureRecordDto>>;

/// <summary>Проверяет идентификатор, пагинацию и необязательный тип подписанта.</summary>
public sealed class GetSignatureStatusQueryValidator : AbstractValidator<GetSignatureStatusQuery>
{
    /// <summary>Создаёт правила чтения статуса подписей.</summary>
    public GetSignatureStatusQueryValidator()
    {
        RuleFor(query => query.DraftId).NotEmpty().WithMessage("Идентификатор документа обязателен.");
        RuleFor(query => query.Filter).NotNull().WithMessage("Параметры фильтрации обязательны.");
        When(query => query.Filter is not null, () =>
        {
            this.AddPaginationRules(
                query => query.Filter.PageNumber,
                query => query.Filter.PageSize,
                query => query.Filter.SortBy);
            RuleFor(query => query.Filter.DraftId)
                .Must((query, id) => !id.HasValue || id.Value == query.DraftId)
                .WithMessage("Идентификатор документа в фильтре не должен отличаться от маршрута.");
            RuleFor(query => query.Filter.SignerType)
                .IsInEnum().When(query => query.Filter.SignerType.HasValue)
                .WithMessage("Указан недопустимый тип подписанта.");
        });
    }
}

/// <summary>Авторизует документ до чтения подписей и не раскрывает IP-адреса в списочной модели.</summary>
public sealed class GetSignatureStatusQueryHandler(
    ICurrentUserContext currentUser,
    IResourceAuthorizationService authorization,
    ISignatureRecordRepository repository,
    IMapper mapper) : IRequestHandler<GetSignatureStatusQuery, Response<PagedResult<GetSignatureRecordDto>>>
{
    /// <summary>Возвращает одинаковый 404 для чужого и отсутствующего документа.</summary>
    public async Task<Response<PagedResult<GetSignatureRecordDto>>> Handle(
        GetSignatureStatusQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.PartyType is not PartyType partyType
            || currentUser.PartyId is not Guid partyId
            || partyId == Guid.Empty)
        {
            return Response<PagedResult<GetSignatureRecordDto>>.Fail(
                "Требуется аутентифицированная сторона документа.", HttpStatusCode.Unauthorized);
        }

        if (!await authorization.CanAccessDraftAsync(
                request.DraftId, partyType, partyId, cancellationToken))
        {
            return Response<PagedResult<GetSignatureRecordDto>>.Fail(
                "Документ не найден.", HttpStatusCode.NotFound);
        }

        var signatures = await repository.GetByDraftAsync(request.DraftId, cancellationToken);
        var filtered = request.Filter.SignerType.HasValue
            ? signatures.Where(signature => signature.SignerType == request.Filter.SignerType.Value).ToArray()
            : signatures.ToArray();
        var skip = ValidationRules.CalculateSkip(request.Filter.PageNumber, request.Filter.PageSize);
        var page = filtered.Skip(skip).Take(request.Filter.PageSize).ToArray();
        var items = mapper.Map<IReadOnlyList<GetSignatureRecordDto>>(page);
        return Response<PagedResult<GetSignatureRecordDto>>.Success(
            new PagedResult<GetSignatureRecordDto>(
                items, filtered.Length, request.Filter.PageNumber, request.Filter.PageSize));
    }
}

/// <summary>Помечает подписанный документ требующим обновления из-за конкретного изменения законодательства.</summary>
public sealed record MarkDraftRequiresUpdateCommand(Guid DraftId, Guid LegislationAlertId)
    : IApplicationRequest<bool>;

/// <summary>Проверяет идентификаторы документа и законодательного основания.</summary>
public sealed class MarkDraftRequiresUpdateCommandValidator
    : AbstractValidator<MarkDraftRequiresUpdateCommand>
{
    /// <summary>Создаёт правила системной команды пересмотра.</summary>
    public MarkDraftRequiresUpdateCommandValidator()
    {
        RuleFor(command => command.DraftId).NotEmpty().WithMessage("Идентификатор документа обязателен.");
        RuleFor(command => command.LegislationAlertId)
            .NotEmpty().WithMessage("Идентификатор уведомления законодательства обязателен.");
    }
}

/// <summary>Проверяет системное разрешение и переводит только подписанный документ в RequiresUpdate.</summary>
public sealed class MarkDraftRequiresUpdateCommandHandler(
    ICurrentUserContext currentUser,
    IApplicationAuthorizationService authorization,
    IResourceAuthorizationService resourceAuthorization,
    IClock clock,
    IDraftRepository draftRepository,
    ILegislationAlertRepository alertRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<MarkDraftRequiresUpdateCommand, Response<bool>>
{
    /// <summary>Проверяет существование законодательного основания и доступ несистемного куратора к документу.</summary>
    public async Task<Response<bool>> Handle(
        MarkDraftRequiresUpdateCommand request,
        CancellationToken cancellationToken)
    {
        var hasPermission = currentUser.IsSystem
            || await authorization.HasPermissionAsync(
                ApplicationPermission.ManageLegislationMonitoring, cancellationToken);
        if (!hasPermission)
        {
            return Response<bool>.Fail(
                "Недостаточно прав для пометки документа на пересмотр.", HttpStatusCode.Forbidden);
        }

        if (!currentUser.IsSystem)
        {
            if (currentUser.PartyType is not PartyType.Lawyer
                || currentUser.PartyId is not Guid lawyerId
                || lawyerId == Guid.Empty
                || !await resourceAuthorization.CanAccessDraftAsync(
                    request.DraftId, PartyType.Lawyer, lawyerId, cancellationToken))
            {
                return Response<bool>.Fail("Документ не найден.", HttpStatusCode.NotFound);
            }
        }

        if (await alertRepository.GetByIdAsync(request.LegislationAlertId, cancellationToken) is null)
        {
            return Response<bool>.Fail("Уведомление законодательства не найдено.", HttpStatusCode.NotFound);
        }

        var draft = await draftRepository.GetByIdAsync(request.DraftId, cancellationToken);
        if (draft is null)
        {
            return Response<bool>.Fail("Документ не найден.", HttpStatusCode.NotFound);
        }

        draft.ChangeStatus(DocumentStatus.RequiresUpdate, clock.UtcNow);
        await draftRepository.UpdateAsync(draft, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Response<bool>.Success(true, "Документ помечен как требующий обновления.");
    }
}
