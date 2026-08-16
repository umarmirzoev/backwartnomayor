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
using FluentValidation;
using MediatR;

namespace Application.Features.Legislation;

/// <summary>
/// Принимает проверенный результат фонового мониторинга законодательства и связывает его
/// с конкретными затронутыми делами. Операция доступна только системному исполнителю или куратору мониторинга.
/// </summary>
public sealed record IngestLegislationAlertCommand(
    CreateLegislationAlertDto Data,
    IReadOnlyCollection<Guid> CaseIds) : IApplicationRequest<Guid>;

/// <summary>Проверяет содержимое уведомления и непустой уникальный набор затронутых дел.</summary>
public sealed class IngestLegislationAlertCommandValidator
    : AbstractValidator<IngestLegislationAlertCommand>
{
    /// <summary>Создаёт правила доверенного входа фоновой задачи.</summary>
    public IngestLegislationAlertCommandValidator()
    {
        RuleFor(command => command.Data).NotNull().WithMessage("Данные уведомления обязательны.");
        When(command => command.Data is not null, () =>
        {
            RuleFor(command => command.Data.Title)
                .NotEmpty().WithMessage("Название изменения законодательства обязательно.")
                .MaximumLength(300).WithMessage("Название изменения не должно превышать 300 символов.");
            RuleFor(command => command.Data.Summary)
                .NotEmpty().WithMessage("Сводка изменения законодательства обязательна.")
                .MaximumLength(ValidationRules.MaximumDocumentTextLength)
                .WithMessage($"Сводка не должна превышать {ValidationRules.MaximumDocumentTextLength} символов.");
            RuleFor(command => command.Data.SourceUrl)
                .MaximumLength(2000).WithMessage("Ссылка на источник не должна превышать 2000 символов.")
                .Must(url => url is null || Uri.TryCreate(url, UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
                .WithMessage("Ссылка на источник должна быть абсолютным HTTP- или HTTPS-адресом.");
        });
        RuleFor(command => command.CaseIds)
            .NotNull().WithMessage("Список затронутых дел обязателен.")
            .NotEmpty().WithMessage("Требуется хотя бы одно затронутое дело.")
            .Must(caseIds => caseIds is not null && caseIds.All(caseId => caseId != Guid.Empty))
            .WithMessage("Идентификаторы затронутых дел не могут быть пустыми.")
            .Must(caseIds => caseIds is not null && caseIds.Distinct().Count() == caseIds.Count)
            .WithMessage("Список затронутых дел не должен содержать дубликаты.")
            .Must(caseIds => caseIds is not null && caseIds.Count <= 1000)
            .WithMessage("Одно уведомление не может связываться более чем с 1000 делами за операцию.");
    }
}

/// <summary>Создаёт append-only уведомление и все связи с делами одной транзакцией.</summary>
public sealed class IngestLegislationAlertCommandHandler(
    ICurrentUserContext currentUser,
    IApplicationAuthorizationService authorization,
    IClock clock,
    ICaseRepository caseRepository,
    ILegislationAlertRepository alertRepository,
    ICaseLegislationAlertRepository linkRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<IngestLegislationAlertCommand, Response<Guid>>
{
    /// <summary>Проверяет системное разрешение и существование всех дел до добавления уведомления.</summary>
    public async Task<Response<Guid>> Handle(
        IngestLegislationAlertCommand request,
        CancellationToken cancellationToken)
    {
        var isAuthorized = currentUser.IsSystem
            || await authorization.HasPermissionAsync(
                ApplicationPermission.ManageLegislationMonitoring, cancellationToken);
        if (!isAuthorized)
        {
            return Response<Guid>.Fail(
                "Недостаточно прав для загрузки результатов мониторинга законодательства.",
                HttpStatusCode.Forbidden);
        }

        var cases = await caseRepository.GetByIdsForSystemAsync(request.CaseIds, cancellationToken);
        if (cases.Count != request.CaseIds.Count)
        {
            return Response<Guid>.Fail("Одно или несколько затронутых дел не найдены.", HttpStatusCode.NotFound);
        }

        var alert = new LegislationAlert(
            request.Data.Title,
            request.Data.Summary,
            request.Data.SourceUrl,
            request.Data.LawChangedAt,
            clock.UtcNow);
        var links = request.CaseIds
            .Select(caseId => new CaseLegislationAlert(caseId, alert.Id))
            .ToArray();

        await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
        {
            await alertRepository.AddAsync(alert, transactionToken);
            await linkRepository.AddRangeAsync(links, transactionToken);
            await unitOfWork.SaveChangesAsync(transactionToken);
        }, cancellationToken);
        return Response<Guid>.Success(alert.Id, "Уведомление законодательства загружено.", HttpStatusCode.Created);
    }
}

/// <summary>Отмечает уведомление прочитанным только в контексте конкретного дела текущего юриста.</summary>
public sealed record MarkLegislationAlertReadCommand(Guid LinkId) : IApplicationRequest<bool>;

/// <summary>Проверяет идентификатор связи уведомления с делом.</summary>
public sealed class MarkLegislationAlertReadCommandValidator
    : AbstractValidator<MarkLegislationAlertReadCommand>
{
    /// <summary>Создаёт правило непустого идентификатора связи.</summary>
    public MarkLegislationAlertReadCommandValidator()
    {
        RuleFor(command => command.LinkId).NotEmpty().WithMessage("Идентификатор уведомления по делу обязателен.");
    }
}

/// <summary>Применяет идемпотентный доменный переход прочтения с tenant-проверкой в репозитории.</summary>
public sealed class MarkLegislationAlertReadCommandHandler(
    ICurrentUserContext currentUser,
    IClock clock,
    ICaseLegislationAlertRepository repository,
    IUnitOfWork unitOfWork) : IRequestHandler<MarkLegislationAlertReadCommand, Response<bool>>
{
    /// <summary>Возвращает одинаковый ответ для чужой и отсутствующей связи, предотвращая IDOR.</summary>
    public async Task<Response<bool>> Handle(
        MarkLegislationAlertReadCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.LawyerId is not Guid lawyerId || lawyerId == Guid.Empty)
        {
            return Response<bool>.Fail("Требуется профиль юриста.", HttpStatusCode.Unauthorized);
        }

        var link = await repository.GetByIdForLawyerAsync(request.LinkId, lawyerId, cancellationToken);
        if (link is null)
        {
            return Response<bool>.Fail("Уведомление по делу не найдено.", HttpStatusCode.NotFound);
        }

        link.MarkRead(clock.UtcNow);
        await repository.UpdateAsync(link, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Response<bool>.Success(true, "Уведомление отмечено прочитанным.");
    }
}

/// <summary>Запрашивает страницу непрочитанных законодательных уведомлений по делам текущего юриста.</summary>
public sealed record GetLegislationAlertsQuery(LegislationAlertFilterParam Filter)
    : IApplicationRequest<PagedResult<CaseLegislationAlertDetailDto>>;

/// <summary>Проверяет пагинацию и границу времени непрочитанных уведомлений.</summary>
public sealed class GetLegislationAlertsQueryValidator : AbstractValidator<GetLegislationAlertsQuery>
{
    /// <summary>Создаёт правила фильтра в точном соответствии с MVP-запросом непрочитанных уведомлений.</summary>
    public GetLegislationAlertsQueryValidator()
    {
        RuleFor(query => query.Filter).NotNull().WithMessage("Параметры фильтрации обязательны.");
        When(query => query.Filter is not null, () =>
        {
            this.AddPaginationRules(
                query => query.Filter.PageNumber,
                query => query.Filter.PageSize,
                query => query.Filter.SortBy);
            RuleFor(query => query.Filter.UnreadOnly)
                .Equal(true).WithMessage("MVP-запрос возвращает только непрочитанные уведомления.");
        });
    }
}

/// <summary>Получает связи, уведомления и дела одной SQL-проекцией без N+1.</summary>
public sealed class GetLegislationAlertsQueryHandler(
    ICurrentUserContext currentUser,
    ILegislationAlertRepository repository,
    IMapper mapper)
    : IRequestHandler<GetLegislationAlertsQuery, Response<PagedResult<CaseLegislationAlertDetailDto>>>
{
    /// <summary>Строит детальные DTO с реальным идентификатором связи, необходимым команде прочтения.</summary>
    public async Task<Response<PagedResult<CaseLegislationAlertDetailDto>>> Handle(
        GetLegislationAlertsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.LawyerId is not Guid lawyerId || lawyerId == Guid.Empty)
        {
            return Response<PagedResult<CaseLegislationAlertDetailDto>>.Fail(
                "Требуется профиль юриста.", HttpStatusCode.Unauthorized);
        }

        var skip = ValidationRules.CalculateSkip(request.Filter.PageNumber, request.Filter.PageSize);
        var rows = await repository.GetUnreadForLawyerAsync(
            lawyerId,
            request.Filter.DetectedFrom,
            skip,
            request.Filter.PageSize,
            cancellationToken);
        var count = await repository.CountUnreadForLawyerAsync(
            lawyerId, request.Filter.DetectedFrom, cancellationToken);
        var items = rows.Select(row => new CaseLegislationAlertDetailDto(
            row.Link.Id,
            row.Link.CaseId,
            row.Link.LegislationAlertId,
            row.Link.IsRead,
            row.Link.ReadAt,
            mapper.Map<GetCaseDto>(row.Case),
            mapper.Map<GetLegislationAlertDto>(row.Alert))).ToArray();
        return Response<PagedResult<CaseLegislationAlertDetailDto>>.Success(
            new PagedResult<CaseLegislationAlertDetailDto>(
                items, count, request.Filter.PageNumber, request.Filter.PageSize));
    }
}
