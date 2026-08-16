using System.Net;
using Application.Common.CQRS;
using Application.Common.Models;
using Application.Common.Notifications;
using Application.Common.Validation;
using Application.DTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.Documents;

/// <summary>
/// Создаёт первый договорный черновик посредством контролируемой ИИ-сборки из активных пунктов шаблона.
/// Идентификатор владельца берётся только из доверенного контекста, а команда тарифицируется конвейером квоты.
/// </summary>
public sealed record CreateDraftCommand(CreateDraftDto Data)
    : IApplicationRequest<DraftOperationDto>, IAiMeteredRequest
{
    /// <summary>Получает тип тарифицируемой операции для Redis-проверки и персистентного аудита.</summary>
    public AiRequestType RequestType => AiRequestType.GenerateDraft;
}

/// <summary>Проверяет ссылки на дело/шаблон и безопасный размер описания сделки.</summary>
public sealed class CreateDraftCommandValidator : AbstractValidator<CreateDraftCommand>
{
    /// <summary>Создаёт правила первой генерации документа.</summary>
    public CreateDraftCommandValidator()
    {
        RuleFor(command => command.Data).NotNull().WithMessage("Данные для создания черновика обязательны.");
        When(command => command.Data is not null, () =>
        {
            RuleFor(command => command.Data.CaseId).NotEmpty().WithMessage("Идентификатор дела обязателен.");
            RuleFor(command => command.Data.TemplateId).NotEmpty().WithMessage("Идентификатор шаблона обязателен.");
            RuleFor(command => command.Data.DealDescription)
                .NotEmpty().WithMessage("Описание сделки обязательно.")
                .MaximumLength(ValidationRules.MaximumAiPromptLength)
                .WithMessage($"Описание сделки не должно превышать {ValidationRules.MaximumAiPromptLength} символов.");
        });
    }
}

/// <summary>
/// Оркестрирует tenant-проверку, атомарное резервирование квоты, обращение к ИИ, шифрованное хранение
/// и транзакционное создание агрегата, первой версии и записи использования.
/// </summary>
public sealed class CreateDraftCommandHandler(
    ICurrentUserContext currentUser,
    IClock clock,
    ICaseRepository caseRepository,
    ITemplateRepository templateRepository,
    ITemplateClauseBlockRepository templateClauseBlockRepository,
    IDraftRepository draftRepository,
    IDocumentVersionRepository versionRepository,
    IAiUsageQuotaRepository quotaRepository,
    IAiUsageRecordRepository usageRepository,
    IAiQuotaCounter quotaCounter,
    IAiDraftingService aiService,
    IDocumentStorageService storageService,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateDraftCommand, Response<DraftOperationDto>>
{
    /// <summary>Создаёт документ только внутри дела текущего юриста и не допускает свободную генерацию без библиотеки.</summary>
    public async Task<Response<DraftOperationDto>> Handle(
        CreateDraftCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.LawyerId is not Guid lawyerId || lawyerId == Guid.Empty)
        {
            return Response<DraftOperationDto>.Fail("Требуется профиль юриста.", HttpStatusCode.Unauthorized);
        }

        if (!await caseRepository.ExistsForLawyerAsync(request.Data.CaseId, lawyerId, cancellationToken))
        {
            return Response<DraftOperationDto>.Fail("Дело не найдено.", HttpStatusCode.NotFound);
        }

        var template = await templateRepository.GetActiveByIdAsync(request.Data.TemplateId, cancellationToken);
        if (template is null)
        {
            return Response<DraftOperationDto>.Fail("Активный шаблон не найден.", HttpStatusCode.NotFound);
        }

        var clauseBlocks = await templateClauseBlockRepository.GetClauseBlocksByTemplateAsync(
            template.Id, true, cancellationToken);
        if (clauseBlocks.Count == 0)
        {
            return Response<DraftOperationDto>.Fail(
                "Шаблон не содержит активных пунктов по умолчанию и не готов к генерации.",
                HttpStatusCode.Conflict);
        }

        var now = clock.UtcNow;
        var quota = await quotaRepository.GetCurrentForUpdateAsync(lawyerId, now, cancellationToken);
        if (quota is null)
        {
            return Response<DraftOperationDto>.Fail("Квота ИИ для текущего периода не найдена.", HttpStatusCode.Conflict);
        }

        if (!await quotaCounter.TryReserveAsync(
                lawyerId, quota.Id, quota.RequestsUsed, quota.RequestsLimit, quota.PeriodEnd, cancellationToken))
        {
            return Response<DraftOperationDto>.Fail(
                "Лимит ИИ-запросов за текущий период исчерпан.", HttpStatusCode.TooManyRequests);
        }

        var draft = new Draft(request.Data.CaseId, request.Data.TemplateId, now);
        var clauseContents = SelectClauseContents(template.Language, clauseBlocks);
        var generation = await aiService.GenerateDraftAsync(
            request.Data.DealDescription, clauseContents, cancellationToken);

        if (!generation.Succeeded || string.IsNullOrWhiteSpace(generation.Value))
        {
            await PersistFailedGenerationAsync(draft, quota, lawyerId, now, cancellationToken);
            await quotaCounter.SynchronizeAsync(
                lawyerId, quota.Id, quota.RequestsUsed, quota.PeriodEnd, cancellationToken);
            return Response<DraftOperationDto>.Fail(
                generation.GetErrorsOrDefault("ИИ-сервис не вернул корректный текст черновика."),
                HttpStatusCode.BadGateway);
        }

        var storage = await storageService.StoreTextAsync(generation.Value, cancellationToken);
        if (!storage.Succeeded || string.IsNullOrWhiteSpace(storage.Value))
        {
            await PersistFailedGenerationAsync(draft, quota, lawyerId, now, cancellationToken);
            await quotaCounter.SynchronizeAsync(
                lawyerId, quota.Id, quota.RequestsUsed, quota.PeriodEnd, cancellationToken);
            return Response<DraftOperationDto>.Fail(
                storage.GetErrorsOrDefault("Не удалось сохранить первую версию документа."),
                HttpStatusCode.BadGateway);
        }

        DocumentVersion? version = null;
        try
        {
            await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
            {
                await draftRepository.AddAsync(draft, transactionToken);
                await unitOfWork.SaveChangesAsync(transactionToken);

                version = draft.CreateInitialVersion(storage.Value, lawyerId, now);
                var usage = quota.RegisterUsage(AiRequestType.GenerateDraft, draft.Id, true, now);
                await versionRepository.AddAsync(version, transactionToken);
                await usageRepository.AddAsync(usage, transactionToken);
                await draftRepository.UpdateAsync(draft, transactionToken);
                await quotaRepository.UpdateAsync(quota, transactionToken);
                await unitOfWork.SaveChangesAsync(transactionToken);
            }, cancellationToken);
        }
        catch
        {
            await storageService.DeleteAsync(storage.Value, CancellationToken.None);
            throw;
        }

        await quotaCounter.SynchronizeAsync(
            lawyerId, quota.Id, quota.RequestsUsed, quota.PeriodEnd, cancellationToken);
        return Response<DraftOperationDto>.Success(
            new DraftOperationDto(draft.Id, version!.Id, version.VersionNumber, generation.Value),
            "Черновик создан.",
            HttpStatusCode.Created);
    }

    /// <summary>Формирует языковые входы RAG строго из пунктов выбранного шаблона.</summary>
    private static IReadOnlyList<string> SelectClauseContents(
        TemplateLanguage language,
        IReadOnlyList<ClauseBlock> blocks)
    {
        return language switch
        {
            TemplateLanguage.Tj => blocks.Select(block => block.ContentTj).ToArray(),
            TemplateLanguage.Ru => blocks.Select(block => block.ContentRu).ToArray(),
            TemplateLanguage.Both => blocks
                .Select(block => $"TJ:\n{block.ContentTj}\nRU:\n{block.ContentRu}")
                .ToArray(),
            _ => []
        };
    }

    /// <summary>
    /// Сохраняет факт неуспешного обращения к ИИ, не оставляя пустой агрегат черновика.
    /// Временная строка черновика нужна для соблюдения доменного FK записи генерации и удаляется в той же транзакции.
    /// </summary>
    private async Task PersistFailedGenerationAsync(
        Draft draft,
        AiUsageQuota quota,
        Guid lawyerId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
        {
            await draftRepository.AddAsync(draft, transactionToken);
            await unitOfWork.SaveChangesAsync(transactionToken);
            var usage = quota.RegisterUsage(AiRequestType.GenerateDraft, draft.Id, false, now);
            await usageRepository.AddAsync(usage, transactionToken);
            await quotaRepository.UpdateAsync(quota, transactionToken);
            await unitOfWork.SaveChangesAsync(transactionToken);
            await draftRepository.DeleteAsync(draft, transactionToken);
            await unitOfWork.SaveChangesAsync(transactionToken);
        }, cancellationToken);
    }
}

/// <summary>Создаёт новую immutable-версию посредством ручной правки юриста.</summary>
public sealed record UpdateDraftCommand(Guid DraftId, UpdateDraftDto Data)
    : IApplicationRequest<DraftOperationDto>;

/// <summary>Проверяет идентификатор, полный текст и необязательное описание ручных изменений.</summary>
public sealed class UpdateDraftCommandValidator : AbstractValidator<UpdateDraftCommand>
{
    /// <summary>Создаёт правила безопасного обновления содержимого.</summary>
    public UpdateDraftCommandValidator()
    {
        RuleFor(command => command.DraftId).NotEmpty().WithMessage("Идентификатор черновика обязателен.");
        RuleFor(command => command.Data).NotNull().WithMessage("Данные новой версии обязательны.");
        When(command => command.Data is not null, () =>
        {
            RuleFor(command => command.Data.Content)
                .NotEmpty().WithMessage("Текст документа обязателен.")
                .MaximumLength(ValidationRules.MaximumDocumentTextLength)
                .WithMessage($"Текст документа не должен превышать {ValidationRules.MaximumDocumentTextLength} символов.");
            RuleFor(command => command.Data.ChangeSummary)
                .MaximumLength(ValidationRules.MaximumChangeSummaryLength)
                .WithMessage($"Описание изменений не должно превышать {ValidationRules.MaximumChangeSummaryLength} символов.");
        });
    }
}

/// <summary>Сохраняет ручную редакцию как новый снимок, не изменяя исторические версии.</summary>
public sealed class UpdateDraftCommandHandler(
    ICurrentUserContext currentUser,
    IClock clock,
    IDraftRepository draftRepository,
    IDocumentVersionRepository versionRepository,
    IDocumentStorageService storageService,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateDraftCommand, Response<DraftOperationDto>>
{
    /// <summary>Проверяет владение, сохраняет текст и атомарно сдвигает указатель текущей версии.</summary>
    public async Task<Response<DraftOperationDto>> Handle(
        UpdateDraftCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.LawyerId is not Guid lawyerId || lawyerId == Guid.Empty)
        {
            return Response<DraftOperationDto>.Fail("Требуется профиль юриста.", HttpStatusCode.Unauthorized);
        }

        var draft = await draftRepository.GetByIdForLawyerAsync(request.DraftId, lawyerId, cancellationToken);
        if (draft is null)
        {
            return Response<DraftOperationDto>.Fail("Черновик не найден.", HttpStatusCode.NotFound);
        }

        var storage = await storageService.StoreTextAsync(request.Data.Content, cancellationToken);
        if (!storage.Succeeded || string.IsNullOrWhiteSpace(storage.Value))
        {
            return Response<DraftOperationDto>.Fail(
                storage.GetErrorsOrDefault("Не удалось сохранить ручную версию документа."),
                HttpStatusCode.BadGateway);
        }

        DocumentVersion? version = null;
        try
        {
            await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
            {
                var versionNumber = await versionRepository.GetNextVersionNumberForLawyerAsync(
                    draft.Id, lawyerId, transactionToken);
                if (!versionNumber.HasValue)
                {
                    throw new InvalidOperationException("Не удалось вычислить следующий номер версии доступного черновика.");
                }

                version = draft.CreateNextVersion(
                    versionNumber.Value,
                    storage.Value,
                    request.Data.ChangeSummary,
                    DocumentVersionSource.ManualEdit,
                    lawyerId,
                    clock.UtcNow);
                await versionRepository.AddAsync(version, transactionToken);
                await draftRepository.UpdateAsync(draft, transactionToken);
                await unitOfWork.SaveChangesAsync(transactionToken);
            }, cancellationToken);
        }
        catch
        {
            await storageService.DeleteAsync(storage.Value, CancellationToken.None);
            throw;
        }

        return Response<DraftOperationDto>.Success(
            new DraftOperationDto(draft.Id, version!.Id, version.VersionNumber, request.Data.Content),
            "Новая ручная версия сохранена.");
    }
}

/// <summary>Повторно генерирует текущий черновик по новой инструкции и расходует одну единицу ИИ-квоты.</summary>
public sealed record RegenerateDraftCommand(Guid DraftId, string Instructions, string ChangeSummary)
    : IApplicationRequest<DraftOperationDto>, IAiMeteredRequest
{
    /// <summary>Получает тип тарифицируемой повторной генерации.</summary>
    public AiRequestType RequestType => AiRequestType.RegenerateDraft;
}

/// <summary>Проверяет идентификатор, инструкцию и обязательное описание отличий новой ИИ-версии.</summary>
public sealed class RegenerateDraftCommandValidator : AbstractValidator<RegenerateDraftCommand>
{
    /// <summary>Создаёт правила повторной генерации.</summary>
    public RegenerateDraftCommandValidator()
    {
        RuleFor(command => command.DraftId).NotEmpty().WithMessage("Идентификатор черновика обязателен.");
        RuleFor(command => command.Instructions)
            .NotEmpty().WithMessage("Инструкция для повторной генерации обязательна.")
            .MaximumLength(ValidationRules.MaximumAiPromptLength)
            .WithMessage($"Инструкция не должна превышать {ValidationRules.MaximumAiPromptLength} символов.");
        RuleFor(command => command.ChangeSummary)
            .NotEmpty().WithMessage("Описание изменений повторной генерации обязательно.")
            .MaximumLength(ValidationRules.MaximumChangeSummaryLength)
            .WithMessage($"Описание изменений не должно превышать {ValidationRules.MaximumChangeSummaryLength} символов.");
    }
}

/// <summary>Оркестрирует безопасную повторную генерацию и согласованную фиксацию квоты с новой версией.</summary>
public sealed class RegenerateDraftCommandHandler(
    ICurrentUserContext currentUser,
    IClock clock,
    IDraftRepository draftRepository,
    IDocumentVersionRepository versionRepository,
    IAiUsageQuotaRepository quotaRepository,
    IAiUsageRecordRepository usageRepository,
    IAiQuotaCounter quotaCounter,
    IAiDraftingService aiService,
    IDocumentStorageService storageService,
    IUnitOfWork unitOfWork) : IRequestHandler<RegenerateDraftCommand, Response<DraftOperationDto>>
{
    /// <summary>Проверяет владельца, резервирует квоту и создаёт версию с источником AiRegenerated.</summary>
    public async Task<Response<DraftOperationDto>> Handle(
        RegenerateDraftCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.LawyerId is not Guid lawyerId || lawyerId == Guid.Empty)
        {
            return Response<DraftOperationDto>.Fail("Требуется профиль юриста.", HttpStatusCode.Unauthorized);
        }

        var aggregate = await draftRepository.GetWithCurrentVersionForLawyerAsync(
            request.DraftId, lawyerId, cancellationToken);
        if (aggregate is null || aggregate.Value.CurrentVersion is null)
        {
            return Response<DraftOperationDto>.Fail("Черновик не найден.", HttpStatusCode.NotFound);
        }

        var currentContent = await storageService.GetTextAsync(
            aggregate.Value.CurrentVersion.ContentStorageKey, cancellationToken);
        if (!currentContent.Succeeded || string.IsNullOrWhiteSpace(currentContent.Value))
        {
            return Response<DraftOperationDto>.Fail(
                currentContent.GetErrorsOrDefault("Не удалось загрузить текущую версию документа."),
                HttpStatusCode.BadGateway);
        }

        var now = clock.UtcNow;
        var quota = await quotaRepository.GetCurrentForUpdateAsync(lawyerId, now, cancellationToken);
        if (quota is null)
        {
            return Response<DraftOperationDto>.Fail("Квота ИИ для текущего периода не найдена.", HttpStatusCode.Conflict);
        }

        if (!await quotaCounter.TryReserveAsync(
                lawyerId, quota.Id, quota.RequestsUsed, quota.RequestsLimit, quota.PeriodEnd, cancellationToken))
        {
            return Response<DraftOperationDto>.Fail(
                "Лимит ИИ-запросов за текущий период исчерпан.", HttpStatusCode.TooManyRequests);
        }

        var generation = await aiService.RegenerateDraftAsync(
            currentContent.Value, request.Instructions, cancellationToken);
        if (!generation.Succeeded || string.IsNullOrWhiteSpace(generation.Value))
        {
            var failedUsage = quota.RegisterUsage(AiRequestType.RegenerateDraft, request.DraftId, false, now);
            await usageRepository.AddAsync(failedUsage, cancellationToken);
            await quotaRepository.UpdateAsync(quota, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await quotaCounter.SynchronizeAsync(
                lawyerId, quota.Id, quota.RequestsUsed, quota.PeriodEnd, cancellationToken);
            return Response<DraftOperationDto>.Fail(
                generation.GetErrorsOrDefault("ИИ-сервис не вернул корректный текст новой версии."),
                HttpStatusCode.BadGateway);
        }

        var storage = await storageService.StoreTextAsync(generation.Value, cancellationToken);
        if (!storage.Succeeded || string.IsNullOrWhiteSpace(storage.Value))
        {
            var failedUsage = quota.RegisterUsage(AiRequestType.RegenerateDraft, request.DraftId, false, now);
            await usageRepository.AddAsync(failedUsage, cancellationToken);
            await quotaRepository.UpdateAsync(quota, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await quotaCounter.SynchronizeAsync(
                lawyerId, quota.Id, quota.RequestsUsed, quota.PeriodEnd, cancellationToken);
            return Response<DraftOperationDto>.Fail(
                storage.GetErrorsOrDefault("Не удалось сохранить повторно сгенерированную версию."),
                HttpStatusCode.BadGateway);
        }

        var draft = aggregate.Value.Draft;
        DocumentVersion? version = null;
        try
        {
            await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
            {
                var versionNumber = await versionRepository.GetNextVersionNumberForLawyerAsync(
                    draft.Id, lawyerId, transactionToken);
                if (!versionNumber.HasValue)
                {
                    throw new InvalidOperationException("Не удалось вычислить следующий номер версии доступного черновика.");
                }

                version = draft.CreateNextVersion(
                    versionNumber.Value,
                    storage.Value,
                    request.ChangeSummary,
                    DocumentVersionSource.AiRegenerated,
                    lawyerId,
                    now);
                var usage = quota.RegisterUsage(AiRequestType.RegenerateDraft, draft.Id, true, now);
                await versionRepository.AddAsync(version, transactionToken);
                await usageRepository.AddAsync(usage, transactionToken);
                await draftRepository.UpdateAsync(draft, transactionToken);
                await quotaRepository.UpdateAsync(quota, transactionToken);
                await unitOfWork.SaveChangesAsync(transactionToken);
            }, cancellationToken);
        }
        catch
        {
            await storageService.DeleteAsync(storage.Value, CancellationToken.None);
            throw;
        }

        await quotaCounter.SynchronizeAsync(
            lawyerId, quota.Id, quota.RequestsUsed, quota.PeriodEnd, cancellationToken);
        return Response<DraftOperationDto>.Success(
            new DraftOperationDto(draft.Id, version!.Id, version.VersionNumber, generation.Value),
            "Черновик повторно сгенерирован.");
    }
}

/// <summary>Фиксирует принятие юристом ответственности за текущую редакцию перед экспортом.</summary>
public sealed record ConfirmResponsibilityCommand(Guid DraftId) : IApplicationRequest<bool>;

/// <summary>Проверяет обязательный идентификатор подтверждаемого черновика.</summary>
public sealed class ConfirmResponsibilityCommandValidator : AbstractValidator<ConfirmResponsibilityCommand>
{
    /// <summary>Создаёт правило непустого идентификатора.</summary>
    public ConfirmResponsibilityCommandValidator()
    {
        RuleFor(command => command.DraftId).NotEmpty().WithMessage("Идентификатор черновика обязателен.");
    }
}

/// <summary>Применяет доменный инвариант подтверждения только к документу текущего юриста.</summary>
public sealed class ConfirmResponsibilityCommandHandler(
    ICurrentUserContext currentUser,
    IClock clock,
    IDraftRepository repository,
    IUnitOfWork unitOfWork) : IRequestHandler<ConfirmResponsibilityCommand, Response<bool>>
{
    /// <summary>Фиксирует момент первого подтверждения и сохраняет агрегат.</summary>
    public async Task<Response<bool>> Handle(
        ConfirmResponsibilityCommand request,
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

        draft.ConfirmResponsibility(clock.UtcNow);
        await repository.UpdateAsync(draft, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Response<bool>.Success(true, "Ответственность за текущую версию подтверждена.");
    }
}

/// <summary>Экспортирует подтверждённую текущую версию в DOCX или PDF и пишет неизменяемый аудит.</summary>
public sealed record ExportDocumentCommand(Guid DraftId, DocumentExportFormat Format)
    : IApplicationRequest<ExportedDocumentDto>;

/// <summary>Проверяет идентификатор документа и поддерживаемый формат экспорта.</summary>
public sealed class ExportDocumentCommandValidator : AbstractValidator<ExportDocumentCommand>
{
    /// <summary>Создаёт правила экспорта.</summary>
    public ExportDocumentCommandValidator()
    {
        RuleFor(command => command.DraftId).NotEmpty().WithMessage("Идентификатор черновика обязателен.");
        RuleFor(command => command.Format).IsInEnum().WithMessage("Указан неподдерживаемый формат экспорта.");
    }
}

/// <summary>Проверяет доменное подтверждение, получает расшифрованный текст и фиксирует факт экспорта.</summary>
public sealed class ExportDocumentCommandHandler(
    ICurrentUserContext currentUser,
    IClock clock,
    IDraftRepository draftRepository,
    IDocumentStorageService storageService,
    IDocumentExportService exportService,
    IAuditLogEntryRepository auditRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<ExportDocumentCommand, Response<ExportedDocumentDto>>
{
    /// <summary>Не раскрывает ключ хранилища и возвращает клиенту только готовый безопасный файл.</summary>
    public async Task<Response<ExportedDocumentDto>> Handle(
        ExportDocumentCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.LawyerId is not Guid lawyerId || lawyerId == Guid.Empty)
        {
            return Response<ExportedDocumentDto>.Fail("Требуется профиль юриста.", HttpStatusCode.Unauthorized);
        }

        var aggregate = await draftRepository.GetWithCurrentVersionForLawyerAsync(
            request.DraftId, lawyerId, cancellationToken);
        if (aggregate is null || aggregate.Value.CurrentVersion is null)
        {
            return Response<ExportedDocumentDto>.Fail("Черновик не найден.", HttpStatusCode.NotFound);
        }

        aggregate.Value.Draft.EnsureCanExport();
        var content = await storageService.GetTextAsync(
            aggregate.Value.CurrentVersion.ContentStorageKey, cancellationToken);
        if (!content.Succeeded || string.IsNullOrWhiteSpace(content.Value))
        {
            return Response<ExportedDocumentDto>.Fail(
                content.GetErrorsOrDefault("Не удалось загрузить текущую версию для экспорта."),
                HttpStatusCode.BadGateway);
        }

        var export = await exportService.ExportAsync(
            request.DraftId, content.Value, request.Format, cancellationToken);
        if (!export.Succeeded || export.Value is null)
        {
            return Response<ExportedDocumentDto>.Fail(
                export.GetErrorsOrDefault("Не удалось сформировать экспортируемый документ."),
                HttpStatusCode.BadGateway);
        }

        var audit = new AuditLogEntry(
            AuditActorType.Lawyer,
            lawyerId,
            AuditAction.Exported,
            nameof(Draft),
            request.DraftId,
            null,
            clock.UtcNow);
        await auditRepository.AddAsync(audit, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Response<ExportedDocumentDto>.Success(export.Value, "Документ экспортирован.");
    }
}

/// <summary>Безвозвратно удаляет черновик, который ещё не был отправлен клиенту.</summary>
public sealed record DeleteDraftCommand(Guid DraftId) : IApplicationRequest<bool>;

/// <summary>Проверяет идентификатор удаляемого черновика.</summary>
public sealed class DeleteDraftCommandValidator : AbstractValidator<DeleteDraftCommand>
{
    /// <summary>Создаёт правило непустого идентификатора.</summary>
    public DeleteDraftCommandValidator()
    {
        RuleFor(command => command.DraftId).NotEmpty().WithMessage("Идентификатор черновика обязателен.");
    }
}

/// <summary>Удаляет объекты версий и строку агрегата только в состоянии Draft.</summary>
public sealed class DeleteDraftCommandHandler(
    ICurrentUserContext currentUser,
    IDraftRepository draftRepository,
    IDocumentVersionRepository versionRepository,
    IDocumentStorageService storageService,
    IAuditLogEntryRepository auditRepository,
    IClock clock,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteDraftCommand, Response<bool>>
{
    /// <summary>Проверяет владение и не допускает немедленное удаление документа, уже видимого клиенту.</summary>
    public async Task<Response<bool>> Handle(DeleteDraftCommand request, CancellationToken cancellationToken)
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

        if (draft.Status != DocumentStatus.Draft)
        {
            return Response<bool>.Fail(
                "Немедленное удаление разрешено только до отправки документа клиенту.", HttpStatusCode.Conflict);
        }

        var versions = await versionRepository.GetHistoryForDraftForLawyerAsync(
            request.DraftId, lawyerId, cancellationToken);
        foreach (var version in versions)
        {
            var deletion = await storageService.DeleteAsync(version.ContentStorageKey, cancellationToken);
            if (!deletion.Succeeded)
            {
                return Response<bool>.Fail(
                    deletion.GetErrorsOrDefault("Не удалось удалить содержимое версии из хранилища."),
                    HttpStatusCode.BadGateway);
            }
        }

        await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
        {
            await draftRepository.DeleteAsync(draft, transactionToken);
            await auditRepository.AddAsync(
                new AuditLogEntry(
                    AuditActorType.Lawyer,
                    lawyerId,
                    AuditAction.Deleted,
                    nameof(Draft),
                    draft.Id,
                    null,
                    clock.UtcNow),
                transactionToken);
            await unitOfWork.SaveChangesAsync(transactionToken);
        }, cancellationToken);
        return Response<bool>.Success(true, "Черновик удалён.");
    }
}

/// <summary>Регистрирует формальный запрос на полное удаление клиента, дела или документа.</summary>
public sealed record RequestFullDeletionCommand(DeletionTargetType TargetType, Guid TargetId)
    : IApplicationRequest<Guid>;

/// <summary>Проверяет тип и идентификатор полиморфной цели удаления.</summary>
public sealed class RequestFullDeletionCommandValidator : AbstractValidator<RequestFullDeletionCommand>
{
    /// <summary>Создаёт правила workflow полного удаления.</summary>
    public RequestFullDeletionCommandValidator()
    {
        RuleFor(command => command.TargetType).IsInEnum().WithMessage("Указан недопустимый тип цели удаления.");
        RuleFor(command => command.TargetId).NotEmpty().WithMessage("Идентификатор цели удаления обязателен.");
    }
}

/// <summary>Проверяет владение целью, предотвращает дублирование и запускает долговечную фоновую обработку.</summary>
public sealed class RequestFullDeletionCommandHandler(
    ICurrentUserContext currentUser,
    IClock clock,
    IClientRepository clientRepository,
    ICaseRepository caseRepository,
    IDraftRepository draftRepository,
    IDataDeletionRequestRepository deletionRepository,
    IUnitOfWork unitOfWork,
    IPublisher publisher) : IRequestHandler<RequestFullDeletionCommand, Response<Guid>>
{
    /// <summary>Создаёт ожидающий запрос только для ресурса текущего юриста и публикует событие постановки в очередь.</summary>
    public async Task<Response<Guid>> Handle(
        RequestFullDeletionCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.LawyerId is not Guid lawyerId || lawyerId == Guid.Empty)
        {
            return Response<Guid>.Fail("Требуется профиль юриста.", HttpStatusCode.Unauthorized);
        }

        var ownsTarget = request.TargetType switch
        {
            DeletionTargetType.Client => await clientRepository.ExistsForLawyerAsync(
                request.TargetId, lawyerId, cancellationToken),
            DeletionTargetType.Case => await caseRepository.ExistsForLawyerAsync(
                request.TargetId, lawyerId, cancellationToken),
            DeletionTargetType.Draft => await draftRepository.ExistsForLawyerAsync(
                request.TargetId, lawyerId, cancellationToken),
            _ => false
        };
        if (!ownsTarget)
        {
            return Response<Guid>.Fail("Объект удаления не найден.", HttpStatusCode.NotFound);
        }

        var existingRequest = await deletionRepository.GetPendingByTargetAsync(
            request.TargetType, request.TargetId, cancellationToken);
        if (existingRequest is not null)
        {
            await publisher.Publish(
                new DataDeletionRequestedNotification(existingRequest.Id), cancellationToken);
            return Response<Guid>.Success(
                existingRequest.Id,
                "Существующий запрос на полное удаление повторно поставлен в обработку.",
                HttpStatusCode.Accepted);
        }

        var deletionRequest = new DataDeletionRequest(
            PartyType.Lawyer,
            lawyerId,
            request.TargetType,
            request.TargetId,
            clock.UtcNow);
        await deletionRepository.AddAsync(deletionRequest, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await publisher.Publish(new DataDeletionRequestedNotification(deletionRequest.Id), cancellationToken);
        return Response<Guid>.Success(
            deletionRequest.Id, "Запрос на полное удаление зарегистрирован.", HttpStatusCode.Accepted);
    }
}

/// <summary>Архивирует документ без удаления его содержимого и юридически значимой истории.</summary>
public sealed record ArchiveDraftCommand(Guid DraftId) : IApplicationRequest<bool>;

/// <summary>Проверяет идентификатор архивируемого документа.</summary>
public sealed class ArchiveDraftCommandValidator : AbstractValidator<ArchiveDraftCommand>
{
    /// <summary>Создаёт правило непустого идентификатора.</summary>
    public ArchiveDraftCommandValidator()
    {
        RuleFor(command => command.DraftId).NotEmpty().WithMessage("Идентификатор черновика обязателен.");
    }
}

/// <summary>Изменяет состояние собственного документа через доменную диаграмму переходов.</summary>
public sealed class ArchiveDraftCommandHandler(
    ICurrentUserContext currentUser,
    IClock clock,
    IDraftRepository draftRepository,
    IAuditLogEntryRepository auditRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<ArchiveDraftCommand, Response<bool>>
{
    /// <summary>Архивирует документ и атомарно фиксирует изменение статуса в журнале аудита.</summary>
    public async Task<Response<bool>> Handle(ArchiveDraftCommand request, CancellationToken cancellationToken)
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

        draft.ChangeStatus(DocumentStatus.Archived, clock.UtcNow);
        await draftRepository.UpdateAsync(draft, cancellationToken);
        await auditRepository.AddAsync(
            new AuditLogEntry(
                AuditActorType.Lawyer,
                lawyerId,
                AuditAction.StatusChanged,
                nameof(Draft),
                draft.Id,
                null,
                clock.UtcNow),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Response<bool>.Success(true, "Документ архивирован.");
    }
}
