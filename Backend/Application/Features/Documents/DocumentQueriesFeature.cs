using System.Net;
using Application.Common.CQRS;
using Application.Common.Models;
using Application.Common.Validation;
using Application.DTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Application.Features.Documents;

/// <summary>Запрашивает принадлежащий юристу черновик вместе с текущей версией и расшифрованным текстом.</summary>
public sealed record GetDraftByIdQuery(Guid DraftId) : IApplicationRequest<DraftDetailDto>;

/// <summary>Проверяет обязательный идентификатор запрашиваемого черновика.</summary>
public sealed class GetDraftByIdQueryValidator : AbstractValidator<GetDraftByIdQuery>
{
    /// <summary>Создаёт правило непустого идентификатора.</summary>
    public GetDraftByIdQueryValidator()
    {
        RuleFor(query => query.DraftId).NotEmpty().WithMessage("Идентификатор черновика обязателен.");
    }
}

/// <summary>Возвращает карточку документа без раскрытия внутреннего ключа объектного хранилища.</summary>
public sealed class GetDraftByIdQueryHandler(
    ICurrentUserContext currentUser,
    IDraftRepository repository,
    IDocumentStorageService storageService,
    IMapper mapper) : IRequestHandler<GetDraftByIdQuery, Response<DraftDetailDto>>
{
    /// <summary>Применяет tenant-фильтр в репозитории и загружает содержимое только после успешной авторизации.</summary>
    public async Task<Response<DraftDetailDto>> Handle(
        GetDraftByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.LawyerId is not Guid lawyerId || lawyerId == Guid.Empty)
        {
            return Response<DraftDetailDto>.Fail("Требуется профиль юриста.", HttpStatusCode.Unauthorized);
        }

        var aggregate = await repository.GetWithCurrentVersionForLawyerAsync(
            request.DraftId, lawyerId, cancellationToken);
        if (aggregate is null || aggregate.Value.CurrentVersion is null)
        {
            return Response<DraftDetailDto>.Fail("Черновик не найден.", HttpStatusCode.NotFound);
        }

        var content = await storageService.GetTextAsync(
            aggregate.Value.CurrentVersion.ContentStorageKey, cancellationToken);
        if (!content.Succeeded || content.Value is null)
        {
            return Response<DraftDetailDto>.Fail(
                content.GetErrorsOrDefault("Хранилище не вернуло содержимое текущей версии."),
                HttpStatusCode.BadGateway);
        }

        var dto = mapper.Map<DraftDetailDto>(aggregate.Value.Draft) with
        {
            CurrentVersion = mapper.Map<GetDocumentVersionDto>(aggregate.Value.CurrentVersion),
            CurrentContent = content.Value
        };
        return Response<DraftDetailDto>.Success(dto);
    }
}

/// <summary>Запрашивает страницу метаданных immutable-версий конкретного черновика без полного текста.</summary>
public sealed record GetDocumentVersionHistoryQuery(Guid DraftId, DocumentVersionFilterParam Filter)
    : IApplicationRequest<PagedResult<GetDocumentVersionDto>>;

/// <summary>Проверяет идентификатор черновика и безопасные параметры страницы истории.</summary>
public sealed class GetDocumentVersionHistoryQueryValidator
    : AbstractValidator<GetDocumentVersionHistoryQuery>
{
    /// <summary>Создаёт правила истории версий.</summary>
    public GetDocumentVersionHistoryQueryValidator()
    {
        RuleFor(query => query.DraftId).NotEmpty().WithMessage("Идентификатор черновика обязателен.");
        RuleFor(query => query.Filter).NotNull().WithMessage("Параметры пагинации обязательны.");
        When(query => query.Filter is not null, () =>
        {
            this.AddPaginationRules(
                query => query.Filter.PageNumber,
                query => query.Filter.PageSize,
                query => query.Filter.SortBy);
            RuleFor(query => query.Filter.DraftId)
                .Must((query, filterDraftId) => !filterDraftId.HasValue || filterDraftId.Value == query.DraftId)
                .WithMessage("Идентификатор черновика в фильтре не должен отличаться от маршрута.");
        });
    }
}

/// <summary>Возвращает метаданные истории после единой tenant-проверки, не читая содержимое объектов.</summary>
public sealed class GetDocumentVersionHistoryQueryHandler(
    ICurrentUserContext currentUser,
    IDraftRepository draftRepository,
    IDocumentVersionRepository versionRepository,
    IMapper mapper) : IRequestHandler<GetDocumentVersionHistoryQuery, Response<PagedResult<GetDocumentVersionDto>>>
{
    /// <summary>Проверяет владение и применяет пагинацию к упорядоченной истории репозитория.</summary>
    public async Task<Response<PagedResult<GetDocumentVersionDto>>> Handle(
        GetDocumentVersionHistoryQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.LawyerId is not Guid lawyerId || lawyerId == Guid.Empty)
        {
            return Response<PagedResult<GetDocumentVersionDto>>.Fail(
                "Требуется профиль юриста.", HttpStatusCode.Unauthorized);
        }

        if (!await draftRepository.ExistsForLawyerAsync(request.DraftId, lawyerId, cancellationToken))
        {
            return Response<PagedResult<GetDocumentVersionDto>>.Fail(
                "Черновик не найден.", HttpStatusCode.NotFound);
        }

        var history = await versionRepository.GetHistoryForDraftForLawyerAsync(
            request.DraftId, lawyerId, cancellationToken);
        var skip = ValidationRules.CalculateSkip(request.Filter.PageNumber, request.Filter.PageSize);
        var page = history.Skip(skip).Take(request.Filter.PageSize).ToArray();
        var items = mapper.Map<IReadOnlyList<GetDocumentVersionDto>>(page);
        return Response<PagedResult<GetDocumentVersionDto>>.Success(
            new PagedResult<GetDocumentVersionDto>(
                items, history.Count, request.Filter.PageNumber, request.Filter.PageSize));
    }
}

/// <summary>Запрашивает полный текст одной immutable-версии для просмотра или сравнения.</summary>
public sealed record GetDocumentVersionByIdQuery(Guid VersionId) : IApplicationRequest<DocumentVersionDetailDto>;

/// <summary>Проверяет идентификатор запрашиваемой версии.</summary>
public sealed class GetDocumentVersionByIdQueryValidator : AbstractValidator<GetDocumentVersionByIdQuery>
{
    /// <summary>Создаёт правило непустого идентификатора.</summary>
    public GetDocumentVersionByIdQueryValidator()
    {
        RuleFor(query => query.VersionId).NotEmpty().WithMessage("Идентификатор версии документа обязателен.");
    }
}

/// <summary>Проверяет владение через цепочку Version → Draft → Case и только затем читает защищённый объект.</summary>
public sealed class GetDocumentVersionByIdQueryHandler(
    ICurrentUserContext currentUser,
    IDocumentVersionRepository repository,
    IDocumentStorageService storageService,
    IMapper mapper) : IRequestHandler<GetDocumentVersionByIdQuery, Response<DocumentVersionDetailDto>>
{
    /// <summary>Возвращает одинаковый ответ для отсутствующей и чужой версии, предотвращая IDOR-перебор.</summary>
    public async Task<Response<DocumentVersionDetailDto>> Handle(
        GetDocumentVersionByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.LawyerId is not Guid lawyerId || lawyerId == Guid.Empty)
        {
            return Response<DocumentVersionDetailDto>.Fail(
                "Требуется профиль юриста.", HttpStatusCode.Unauthorized);
        }

        var version = await repository.GetByIdForLawyerAsync(request.VersionId, lawyerId, cancellationToken);
        if (version is null)
        {
            return Response<DocumentVersionDetailDto>.Fail("Версия документа не найдена.", HttpStatusCode.NotFound);
        }

        var content = await storageService.GetTextAsync(version.ContentStorageKey, cancellationToken);
        if (!content.Succeeded || content.Value is null)
        {
            return Response<DocumentVersionDetailDto>.Fail(
                content.GetErrorsOrDefault("Хранилище не вернуло содержимое версии."),
                HttpStatusCode.BadGateway);
        }

        var dto = mapper.Map<DocumentVersionDetailDto>(version) with { Content = content.Value };
        return Response<DocumentVersionDetailDto>.Success(dto);
    }
}
