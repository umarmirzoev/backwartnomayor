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

namespace Application.Features.ArtificialIntelligence;

/// <summary>
/// Анализирует входящий документ как самостоятельную ИИ-операцию без создания черновика
/// и возвращает структурированный перечень рисков для профессионального решения юриста.
/// </summary>
public sealed record ReviewIncomingDocumentCommand(string Content)
    : IApplicationRequest<IReadOnlyList<DocumentReviewItemDto>>, IAiMeteredRequest
{
    /// <summary>Получает тип тарифицируемой операции анализа входящего документа.</summary>
    public AiRequestType RequestType => AiRequestType.ReviewIncomingDocument;
}

/// <summary>Проверяет обязательность и защитный предел анализируемого документа.</summary>
public sealed class ReviewIncomingDocumentCommandValidator
    : AbstractValidator<ReviewIncomingDocumentCommand>
{
    /// <summary>Создаёт правила входящего текста, предотвращающие пустые и чрезмерные payload.</summary>
    public ReviewIncomingDocumentCommandValidator()
    {
        RuleFor(command => command.Content)
            .NotEmpty().WithMessage("Текст входящего документа обязателен.")
            .MaximumLength(ValidationRules.MaximumDocumentTextLength)
            .WithMessage($"Текст документа не должен превышать {ValidationRules.MaximumDocumentTextLength} символов.");
    }
}

/// <summary>Резервирует квоту, вызывает ИИ и сохраняет append-only факт успешной или неуспешной операции.</summary>
public sealed class ReviewIncomingDocumentCommandHandler(
    ICurrentUserContext currentUser,
    IClock clock,
    IAiUsageQuotaRepository quotaRepository,
    IAiUsageRecordRepository usageRepository,
    IAiQuotaCounter quotaCounter,
    IAiDraftingService aiService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReviewIncomingDocumentCommand, Response<IReadOnlyList<DocumentReviewItemDto>>>
{
    /// <summary>Не сохраняет исходный документ и синхронизирует Redis только после фиксации PostgreSQL-счётчика.</summary>
    public async Task<Response<IReadOnlyList<DocumentReviewItemDto>>> Handle(
        ReviewIncomingDocumentCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.LawyerId is not Guid lawyerId || lawyerId == Guid.Empty)
        {
            return Response<IReadOnlyList<DocumentReviewItemDto>>.Fail(
                "Требуется профиль юриста.", HttpStatusCode.Unauthorized);
        }

        var now = clock.UtcNow;
        var quota = await quotaRepository.GetCurrentForUpdateAsync(lawyerId, now, cancellationToken);
        if (quota is null)
        {
            return Response<IReadOnlyList<DocumentReviewItemDto>>.Fail(
                "Квота ИИ для текущего периода не найдена.", HttpStatusCode.Conflict);
        }

        if (!await quotaCounter.TryReserveAsync(
                lawyerId, quota.Id, quota.RequestsUsed, quota.RequestsLimit, quota.PeriodEnd, cancellationToken))
        {
            return Response<IReadOnlyList<DocumentReviewItemDto>>.Fail(
                "Лимит ИИ-запросов за текущий период исчерпан.", HttpStatusCode.TooManyRequests);
        }

        var review = await aiService.ReviewIncomingDocumentAsync(request.Content, cancellationToken);
        var usage = quota.RegisterUsage(
            AiRequestType.ReviewIncomingDocument,
            null,
            review.Succeeded,
            now);
        await usageRepository.AddAsync(usage, cancellationToken);
        await quotaRepository.UpdateAsync(quota, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await quotaCounter.SynchronizeAsync(
            lawyerId, quota.Id, quota.RequestsUsed, quota.PeriodEnd, cancellationToken);

        if (!review.Succeeded || review.Value is null)
        {
            return Response<IReadOnlyList<DocumentReviewItemDto>>.Fail(
                review.GetErrorsOrDefault("ИИ-сервис не вернул корректный результат анализа."),
                HttpStatusCode.BadGateway);
        }

        return Response<IReadOnlyList<DocumentReviewItemDto>>.Success(
            review.Value, "Входящий документ проанализирован.");
    }
}

/// <summary>Запрашивает персистентный остаток ИИ-квоты текущего периода для аутентифицированного юриста.</summary>
public sealed record GetAiUsageQuery : IApplicationRequest<GetAiUsageQuotaDto>;

/// <summary>Подтверждает отсутствие клиентских параметров у запроса текущей квоты.</summary>
public sealed class GetAiUsageQueryValidator : AbstractValidator<GetAiUsageQuery>
{
    /// <summary>Создаёт валидатор-маркер для полного покрытия каждого CQRS-запроса.</summary>
    public GetAiUsageQueryValidator()
    {
    }
}

/// <summary>Читает источник истины PostgreSQL, не подменяя его быстрым Redis-счётчиком.</summary>
public sealed class GetAiUsageQueryHandler(
    ICurrentUserContext currentUser,
    IClock clock,
    IAiUsageQuotaRepository repository,
    IMapper mapper) : IRequestHandler<GetAiUsageQuery, Response<GetAiUsageQuotaDto>>
{
    /// <summary>Возвращает текущую квоту и вычисленный остаток либо ожидаемую ошибку отсутствия периода.</summary>
    public async Task<Response<GetAiUsageQuotaDto>> Handle(
        GetAiUsageQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.LawyerId is not Guid lawyerId || lawyerId == Guid.Empty)
        {
            return Response<GetAiUsageQuotaDto>.Fail("Требуется профиль юриста.", HttpStatusCode.Unauthorized);
        }

        var quota = await repository.GetCurrentAsync(lawyerId, clock.UtcNow, cancellationToken);
        if (quota is null)
        {
            return Response<GetAiUsageQuotaDto>.Fail(
                "Квота ИИ для текущего периода не найдена.", HttpStatusCode.NotFound);
        }

        return Response<GetAiUsageQuotaDto>.Success(mapper.Map<GetAiUsageQuotaDto>(quota));
    }
}
