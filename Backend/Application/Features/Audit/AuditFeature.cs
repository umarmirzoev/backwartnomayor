using System.Net;
using Application.Common.CQRS;
using Application.Common.Models;
using Application.Common.Validation;
using Application.DTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using AutoMapper;
using Domain.Enums;
using FluentValidation;
using MediatR;
using CaseEntity = Domain.Entities.Case;

namespace Application.Features.Audit;

/// <summary>Запрашивает страницу неизменяемого журнала действий по конкретному документу, версии или делу.</summary>
public sealed record GetAuditLogQuery(
    string EntityType,
    Guid EntityId,
    AuditLogEntryFilterParam Filter) : IApplicationRequest<PagedResult<AuditLogEntryDetailDto>>;

/// <summary>Ограничивает полиморфный тип разрешёнными ресурсами и проверяет параметры страницы.</summary>
public sealed class GetAuditLogQueryValidator : AbstractValidator<GetAuditLogQuery>
{
    private static readonly string[] AllowedEntityTypes =
        [nameof(Domain.Entities.Draft), nameof(CaseEntity), nameof(Domain.Entities.DocumentVersion)];

    /// <summary>Создаёт правила, не позволяющие запросить аудит произвольного технического типа.</summary>
    public GetAuditLogQueryValidator()
    {
        RuleFor(query => query.EntityType)
            .NotEmpty().WithMessage("Тип сущности аудита обязателен.")
            .Must(entityType => AllowedEntityTypes.Contains(entityType, StringComparer.Ordinal))
            .WithMessage("Аудит доступен только для черновика, версии документа или дела.");
        RuleFor(query => query.EntityId).NotEmpty().WithMessage("Идентификатор сущности аудита обязателен.");
        RuleFor(query => query.Filter).NotNull().WithMessage("Параметры фильтрации обязательны.");
        When(query => query.Filter is not null, () =>
        {
            this.AddPaginationRules(
                query => query.Filter.PageNumber,
                query => query.Filter.PageSize,
                query => query.Filter.SortBy);
            RuleFor(query => query.Filter.EntityType)
                .Must((query, entityType) => entityType is null
                    || string.Equals(entityType, query.EntityType, StringComparison.Ordinal))
                .WithMessage("Тип сущности в фильтре не должен отличаться от маршрута.");
            RuleFor(query => query.Filter.EntityId)
                .Must((query, entityId) => !entityId.HasValue || entityId.Value == query.EntityId)
                .WithMessage("Идентификатор сущности в фильтре не должен отличаться от маршрута.");
            RuleFor(query => query.Filter.Action)
                .IsInEnum().When(query => query.Filter.Action.HasValue)
                .WithMessage("Указан недопустимый тип действия аудита.");
        });
    }
}

/// <summary>Проверяет владение целевым ресурсом до обращения к полиморфному журналу.</summary>
public sealed class GetAuditLogQueryHandler(
    ICurrentUserContext currentUser,
    IDraftRepository draftRepository,
    ICaseRepository caseRepository,
    IDocumentVersionRepository versionRepository,
    IAuditLogEntryRepository auditRepository,
    IMapper mapper) : IRequestHandler<GetAuditLogQuery, Response<PagedResult<AuditLogEntryDetailDto>>>
{
    /// <summary>Не раскрывает наличие чужого ресурса и применяет одинаковый фильтр действия к странице и счётчику.</summary>
    public async Task<Response<PagedResult<AuditLogEntryDetailDto>>> Handle(
        GetAuditLogQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.LawyerId is not Guid lawyerId || lawyerId == Guid.Empty)
        {
            return Response<PagedResult<AuditLogEntryDetailDto>>.Fail(
                "Требуется профиль юриста.", HttpStatusCode.Unauthorized);
        }

        var ownsEntity = request.EntityType switch
        {
            nameof(Domain.Entities.Draft) => await draftRepository.ExistsForLawyerAsync(
                request.EntityId, lawyerId, cancellationToken),
            nameof(CaseEntity) => await caseRepository.ExistsForLawyerAsync(
                request.EntityId, lawyerId, cancellationToken),
            nameof(Domain.Entities.DocumentVersion) => await versionRepository.GetByIdForLawyerAsync(
                request.EntityId, lawyerId, cancellationToken) is not null,
            _ => false
        };
        if (!ownsEntity)
        {
            return Response<PagedResult<AuditLogEntryDetailDto>>.Fail(
                "Объект аудита не найден.", HttpStatusCode.NotFound);
        }

        var skip = ValidationRules.CalculateSkip(request.Filter.PageNumber, request.Filter.PageSize);
        var entries = await auditRepository.GetByEntityAsync(
            request.EntityType,
            request.EntityId,
            request.Filter.Action,
            skip,
            request.Filter.PageSize,
            cancellationToken);
        var count = await auditRepository.CountByEntityAsync(
            request.EntityType, request.EntityId, request.Filter.Action, cancellationToken);
        var items = mapper.Map<IReadOnlyList<AuditLogEntryDetailDto>>(entries);
        return Response<PagedResult<AuditLogEntryDetailDto>>.Success(
            new PagedResult<AuditLogEntryDetailDto>(
                items, count, request.Filter.PageNumber, request.Filter.PageSize));
    }
}
