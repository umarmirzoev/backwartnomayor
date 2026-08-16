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

namespace Application.Features.TemplateLibrary;

/// <summary>
/// Создаёт курируемый шаблон договора. Команда не принимает признак нотариального заверения,
/// поскольку нотариальные документы исключены из продуктового контура доменной моделью.
/// </summary>
public sealed record CreateTemplateCommand(CreateTemplateDto Data) : IApplicationRequest<Guid>;

/// <summary>Проверяет полноту и ограничения реквизитов создаваемого шаблона до обращения к домену.</summary>
public sealed class CreateTemplateCommandValidator : AbstractValidator<CreateTemplateCommand>
{
    /// <summary>Создаёт правила, синхронизированные с ограничениями модели хранения шаблона.</summary>
    public CreateTemplateCommandValidator()
    {
        RuleFor(command => command.Data).NotNull().WithMessage("Данные шаблона обязательны.");
        When(command => command.Data is not null, () =>
        {
            RuleFor(command => command.Data.Name)
                .NotEmpty().WithMessage("Название шаблона обязательно.")
                .MaximumLength(200).WithMessage("Название шаблона не должно превышать 200 символов.");
            RuleFor(command => command.Data.Language)
                .IsInEnum().WithMessage("Указан недопустимый язык шаблона.");
            RuleFor(command => command.Data.MaintainedByRef)
                .MaximumLength(300).WithMessage("Ссылка на куратора не должна превышать 300 символов.");
        });
    }
}

/// <summary>Создаёт шаблон только для субъекта с разрешением управления библиотекой.</summary>
public sealed class CreateTemplateCommandHandler(
    ITemplateRepository repository,
    IApplicationAuthorizationService authorization,
    IClock clock,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateTemplateCommand, Response<Guid>>
{
    /// <summary>Проверяет кураторское разрешение, создаёт активный шаблон и атомарно сохраняет его.</summary>
    public async Task<Response<Guid>> Handle(CreateTemplateCommand request, CancellationToken cancellationToken)
    {
        if (!await authorization.HasPermissionAsync(ApplicationPermission.ManageTemplateLibrary, cancellationToken))
        {
            return Response<Guid>.Fail("Недостаточно прав для управления библиотекой шаблонов.", HttpStatusCode.Forbidden);
        }

        var template = new Template(
            request.Data.Name,
            request.Data.Description,
            request.Data.Language,
            request.Data.MaintainedByRef,
            clock.UtcNow);
        await repository.AddAsync(template, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Response<Guid>.Success(template.Id, "Шаблон создан.", HttpStatusCode.Created);
    }
}

/// <summary>Изменяет реквизиты существующего шаблона без произвольного управления его активностью.</summary>
public sealed record UpdateTemplateCommand(Guid TemplateId, UpdateTemplateDto Data)
    : IApplicationRequest<TemplateDetailDto>;

/// <summary>Проверяет идентификатор и разрешённые поля изменения шаблона.</summary>
public sealed class UpdateTemplateCommandValidator : AbstractValidator<UpdateTemplateCommand>
{
    /// <summary>Создаёт правила обновления, исключающие пустые идентификаторы и переполнение полей.</summary>
    public UpdateTemplateCommandValidator()
    {
        RuleFor(command => command.TemplateId).NotEmpty().WithMessage("Идентификатор шаблона обязателен.");
        RuleFor(command => command.Data).NotNull().WithMessage("Данные шаблона обязательны.");
        When(command => command.Data is not null, () =>
        {
            RuleFor(command => command.Data.Name)
                .NotEmpty().WithMessage("Название шаблона обязательно.")
                .MaximumLength(200).WithMessage("Название шаблона не должно превышать 200 символов.");
            RuleFor(command => command.Data.Language)
                .IsInEnum().WithMessage("Указан недопустимый язык шаблона.");
            RuleFor(command => command.Data.MaintainedByRef)
                .MaximumLength(300).WithMessage("Ссылка на куратора не должна превышать 300 символов.");
        });
    }
}

/// <summary>Применяет доменный метод обновления шаблона после авторизации куратора.</summary>
public sealed class UpdateTemplateCommandHandler(
    ITemplateRepository repository,
    IApplicationAuthorizationService authorization,
    IClock clock,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IRequestHandler<UpdateTemplateCommand, Response<TemplateDetailDto>>
{
    /// <summary>Возвращает одинаковый ответ об отсутствии для неизвестного шаблона и сохраняет проверенные изменения.</summary>
    public async Task<Response<TemplateDetailDto>> Handle(
        UpdateTemplateCommand request,
        CancellationToken cancellationToken)
    {
        if (!await authorization.HasPermissionAsync(ApplicationPermission.ManageTemplateLibrary, cancellationToken))
        {
            return Response<TemplateDetailDto>.Fail(
                "Недостаточно прав для управления библиотекой шаблонов.", HttpStatusCode.Forbidden);
        }

        var template = await repository.GetByIdAsync(request.TemplateId, cancellationToken);
        if (template is null)
        {
            return Response<TemplateDetailDto>.Fail("Шаблон не найден.", HttpStatusCode.NotFound);
        }

        template.UpdateDetails(
            request.Data.Name,
            request.Data.Description,
            request.Data.Language,
            request.Data.MaintainedByRef,
            clock.UtcNow);
        await repository.UpdateAsync(template, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Response<TemplateDetailDto>.Success(mapper.Map<TemplateDetailDto>(template), "Шаблон обновлён.");
    }
}

/// <summary>Снимает шаблон с публикации, сохраняя исторические связи и документы.</summary>
public sealed record DeactivateTemplateCommand(Guid TemplateId) : IApplicationRequest<bool>;

/// <summary>Проверяет обязательный идентификатор деактивируемого шаблона.</summary>
public sealed class DeactivateTemplateCommandValidator : AbstractValidator<DeactivateTemplateCommand>
{
    /// <summary>Создаёт правило непустого идентификатора.</summary>
    public DeactivateTemplateCommandValidator()
    {
        RuleFor(command => command.TemplateId).NotEmpty().WithMessage("Идентификатор шаблона обязателен.");
    }
}

/// <summary>Выполняет идемпотентную доменную деактивацию после проверки кураторского разрешения.</summary>
public sealed class DeactivateTemplateCommandHandler(
    ITemplateRepository repository,
    IApplicationAuthorizationService authorization,
    IClock clock,
    IUnitOfWork unitOfWork) : IRequestHandler<DeactivateTemplateCommand, Response<bool>>
{
    /// <summary>Деактивирует найденный шаблон без физического удаления строки.</summary>
    public async Task<Response<bool>> Handle(DeactivateTemplateCommand request, CancellationToken cancellationToken)
    {
        if (!await authorization.HasPermissionAsync(ApplicationPermission.ManageTemplateLibrary, cancellationToken))
        {
            return Response<bool>.Fail("Недостаточно прав для управления библиотекой шаблонов.", HttpStatusCode.Forbidden);
        }

        var template = await repository.GetByIdAsync(request.TemplateId, cancellationToken);
        if (template is null)
        {
            return Response<bool>.Fail("Шаблон не найден.", HttpStatusCode.NotFound);
        }

        template.Deactivate(clock.UtcNow);
        await repository.UpdateAsync(template, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Response<bool>.Success(true, "Шаблон снят с публикации.");
    }
}

/// <summary>Добавляет проверенный двуязычный пункт в курируемую библиотеку RAG.</summary>
public sealed record CreateClauseBlockCommand(CreateClauseBlockDto Data) : IApplicationRequest<Guid>;

/// <summary>Проверяет обязательность обоих языковых текстов и ограничения индексируемых полей.</summary>
public sealed class CreateClauseBlockCommandValidator : AbstractValidator<CreateClauseBlockCommand>
{
    /// <summary>Создаёт исчерпывающие правила создания библиотечного пункта.</summary>
    public CreateClauseBlockCommandValidator()
    {
        RuleFor(command => command.Data).NotNull().WithMessage("Данные договорного пункта обязательны.");
        When(command => command.Data is not null, () =>
        {
            RuleFor(command => command.Data.Title)
                .NotEmpty().WithMessage("Название договорного пункта обязательно.")
                .MaximumLength(300).WithMessage("Название договорного пункта не должно превышать 300 символов.");
            RuleFor(command => command.Data.ContentTj)
                .NotEmpty().WithMessage("Таджикский текст договорного пункта обязателен.");
            RuleFor(command => command.Data.ContentRu)
                .NotEmpty().WithMessage("Русский текст договорного пункта обязателен.");
            RuleFor(command => command.Data.Category)
                .NotEmpty().WithMessage("Категория договорного пункта обязательна.")
                .MaximumLength(100).WithMessage("Категория договорного пункта не должна превышать 100 символов.");
        });
    }
}

/// <summary>Создаёт активный двуязычный пункт после проверки кураторских прав.</summary>
public sealed class CreateClauseBlockCommandHandler(
    IClauseBlockRepository repository,
    IApplicationAuthorizationService authorization,
    IClock clock,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateClauseBlockCommand, Response<Guid>>
{
    /// <summary>Формирует доменную сущность пункта и сохраняет её одной единицей работы.</summary>
    public async Task<Response<Guid>> Handle(CreateClauseBlockCommand request, CancellationToken cancellationToken)
    {
        if (!await authorization.HasPermissionAsync(ApplicationPermission.ManageTemplateLibrary, cancellationToken))
        {
            return Response<Guid>.Fail("Недостаточно прав для управления библиотекой пунктов.", HttpStatusCode.Forbidden);
        }

        var block = new ClauseBlock(
            request.Data.Title,
            request.Data.ContentTj,
            request.Data.ContentRu,
            request.Data.Category,
            clock.UtcNow);
        await repository.AddAsync(block, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Response<Guid>.Success(block.Id, "Договорный пункт создан.", HttpStatusCode.Created);
    }
}

/// <summary>Изменяет двуязычное содержимое и категорию существующего пункта.</summary>
public sealed record UpdateClauseBlockCommand(Guid ClauseBlockId, UpdateClauseBlockDto Data)
    : IApplicationRequest<ClauseBlockDetailDto>;

/// <summary>Проверяет идентификатор и обе языковые редакции обновляемого пункта.</summary>
public sealed class UpdateClauseBlockCommandValidator : AbstractValidator<UpdateClauseBlockCommand>
{
    /// <summary>Создаёт правила обновления, соответствующие доменным ограничениям пункта.</summary>
    public UpdateClauseBlockCommandValidator()
    {
        RuleFor(command => command.ClauseBlockId).NotEmpty().WithMessage("Идентификатор договорного пункта обязателен.");
        RuleFor(command => command.Data).NotNull().WithMessage("Данные договорного пункта обязательны.");
        When(command => command.Data is not null, () =>
        {
            RuleFor(command => command.Data.Title)
                .NotEmpty().WithMessage("Название договорного пункта обязательно.")
                .MaximumLength(300).WithMessage("Название договорного пункта не должно превышать 300 символов.");
            RuleFor(command => command.Data.ContentTj)
                .NotEmpty().WithMessage("Таджикский текст договорного пункта обязателен.");
            RuleFor(command => command.Data.ContentRu)
                .NotEmpty().WithMessage("Русский текст договорного пункта обязателен.");
            RuleFor(command => command.Data.Category)
                .NotEmpty().WithMessage("Категория договорного пункта обязательна.")
                .MaximumLength(100).WithMessage("Категория договорного пункта не должна превышать 100 символов.");
        });
    }
}

/// <summary>Обновляет пункт через доменный метод и возвращает актуальную детальную модель.</summary>
public sealed class UpdateClauseBlockCommandHandler(
    IClauseBlockRepository repository,
    IApplicationAuthorizationService authorization,
    IClock clock,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IRequestHandler<UpdateClauseBlockCommand, Response<ClauseBlockDetailDto>>
{
    /// <summary>Проверяет кураторское разрешение и сохраняет новую редакцию пункта.</summary>
    public async Task<Response<ClauseBlockDetailDto>> Handle(
        UpdateClauseBlockCommand request,
        CancellationToken cancellationToken)
    {
        if (!await authorization.HasPermissionAsync(ApplicationPermission.ManageTemplateLibrary, cancellationToken))
        {
            return Response<ClauseBlockDetailDto>.Fail(
                "Недостаточно прав для управления библиотекой пунктов.", HttpStatusCode.Forbidden);
        }

        var block = await repository.GetByIdAsync(request.ClauseBlockId, cancellationToken);
        if (block is null)
        {
            return Response<ClauseBlockDetailDto>.Fail("Договорный пункт не найден.", HttpStatusCode.NotFound);
        }

        block.UpdateContent(
            request.Data.Title,
            request.Data.ContentTj,
            request.Data.ContentRu,
            request.Data.Category,
            clock.UtcNow);
        await repository.UpdateAsync(block, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Response<ClauseBlockDetailDto>.Success(
            mapper.Map<ClauseBlockDetailDto>(block), "Договорный пункт обновлён.");
    }
}

/// <summary>Прикрепляет пункт к шаблону с уникальной позицией и признаком включения по умолчанию.</summary>
public sealed record AttachClauseBlockToTemplateCommand(CreateTemplateClauseBlockDto Data)
    : IApplicationRequest<Guid>;

/// <summary>Проверяет стороны связи и неотрицательный порядок пункта в шаблоне.</summary>
public sealed class AttachClauseBlockToTemplateCommandValidator
    : AbstractValidator<AttachClauseBlockToTemplateCommand>
{
    /// <summary>Создаёт правила входного контракта связи шаблона и пункта.</summary>
    public AttachClauseBlockToTemplateCommandValidator()
    {
        RuleFor(command => command.Data).NotNull().WithMessage("Данные связи шаблона и пункта обязательны.");
        When(command => command.Data is not null, () =>
        {
            RuleFor(command => command.Data.TemplateId).NotEmpty().WithMessage("Идентификатор шаблона обязателен.");
            RuleFor(command => command.Data.ClauseBlockId).NotEmpty().WithMessage("Идентификатор пункта обязателен.");
            RuleFor(command => command.Data.Order).GreaterThanOrEqualTo(0)
                .WithMessage("Порядок пункта не может быть отрицательным.");
        });
    }
}

/// <summary>Создаёт связь после проверок активности сторон, уникальности пары и позиции.</summary>
public sealed class AttachClauseBlockToTemplateCommandHandler(
    ITemplateRepository templateRepository,
    IClauseBlockRepository clauseBlockRepository,
    ITemplateClauseBlockRepository linkRepository,
    IApplicationAuthorizationService authorization,
    IUnitOfWork unitOfWork) : IRequestHandler<AttachClauseBlockToTemplateCommand, Response<Guid>>
{
    /// <summary>Предотвращает дублирование пункта и конфликт порядка внутри одного шаблона.</summary>
    public async Task<Response<Guid>> Handle(
        AttachClauseBlockToTemplateCommand request,
        CancellationToken cancellationToken)
    {
        if (!await authorization.HasPermissionAsync(ApplicationPermission.ManageTemplateLibrary, cancellationToken))
        {
            return Response<Guid>.Fail("Недостаточно прав для управления составом шаблона.", HttpStatusCode.Forbidden);
        }

        var template = await templateRepository.GetActiveByIdAsync(request.Data.TemplateId, cancellationToken);
        var block = await clauseBlockRepository.GetActiveByIdAsync(request.Data.ClauseBlockId, cancellationToken);
        if (template is null || block is null)
        {
            return Response<Guid>.Fail("Активный шаблон или договорный пункт не найден.", HttpStatusCode.NotFound);
        }

        if (await linkRepository.ExistsAsync(request.Data.TemplateId, request.Data.ClauseBlockId, cancellationToken))
        {
            return Response<Guid>.Fail("Договорный пункт уже прикреплён к этому шаблону.", HttpStatusCode.Conflict);
        }

        if (await linkRepository.IsOrderOccupiedAsync(
                request.Data.TemplateId, request.Data.Order, null, cancellationToken))
        {
            return Response<Guid>.Fail("Указанная позиция уже занята другим пунктом шаблона.", HttpStatusCode.Conflict);
        }

        var link = new TemplateClauseBlock(
            request.Data.TemplateId,
            request.Data.ClauseBlockId,
            request.Data.IsDefault,
            request.Data.Order);
        await linkRepository.AddAsync(link, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Response<Guid>.Success(link.Id, "Договорный пункт прикреплён к шаблону.", HttpStatusCode.Created);
    }
}

/// <summary>Удаляет конкретную связь пункта с шаблоном, не удаляя сам пункт и шаблон.</summary>
public sealed record DetachClauseBlockFromTemplateCommand(Guid TemplateId, Guid ClauseBlockId)
    : IApplicationRequest<bool>;

/// <summary>Проверяет оба обязательных идентификатора удаляемой связи.</summary>
public sealed class DetachClauseBlockFromTemplateCommandValidator
    : AbstractValidator<DetachClauseBlockFromTemplateCommand>
{
    /// <summary>Создаёт правила непустых идентификаторов сторон связи.</summary>
    public DetachClauseBlockFromTemplateCommandValidator()
    {
        RuleFor(command => command.TemplateId).NotEmpty().WithMessage("Идентификатор шаблона обязателен.");
        RuleFor(command => command.ClauseBlockId).NotEmpty().WithMessage("Идентификатор пункта обязателен.");
    }
}

/// <summary>Физически удаляет только join-сущность после проверки кураторского разрешения.</summary>
public sealed class DetachClauseBlockFromTemplateCommandHandler(
    ITemplateClauseBlockRepository repository,
    IApplicationAuthorizationService authorization,
    IUnitOfWork unitOfWork) : IRequestHandler<DetachClauseBlockFromTemplateCommand, Response<bool>>
{
    /// <summary>Находит точную связь и удаляет её без каскадного изменения библиотечных сущностей.</summary>
    public async Task<Response<bool>> Handle(
        DetachClauseBlockFromTemplateCommand request,
        CancellationToken cancellationToken)
    {
        if (!await authorization.HasPermissionAsync(ApplicationPermission.ManageTemplateLibrary, cancellationToken))
        {
            return Response<bool>.Fail("Недостаточно прав для управления составом шаблона.", HttpStatusCode.Forbidden);
        }

        var link = await repository.GetByTemplateAndClauseBlockAsync(
            request.TemplateId, request.ClauseBlockId, cancellationToken);
        if (link is null)
        {
            return Response<bool>.Fail("Связь шаблона и договорного пункта не найдена.", HttpStatusCode.NotFound);
        }

        await repository.DeleteAsync(link, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Response<bool>.Success(true, "Договорный пункт отвязан от шаблона.");
    }
}

/// <summary>Запрашивает страницу активных шаблонов с фильтрацией по языку.</summary>
public sealed record GetTemplatesQuery(TemplateFilterParam Filter)
    : IApplicationRequest<PagedResult<GetTemplateDto>>;

/// <summary>Проверяет фильтр, пагинацию и допустимость языкового перечисления.</summary>
public sealed class GetTemplatesQueryValidator : AbstractValidator<GetTemplatesQuery>
{
    /// <summary>Создаёт безопасные пределы каталожного запроса.</summary>
    public GetTemplatesQueryValidator()
    {
        RuleFor(query => query.Filter).NotNull().WithMessage("Параметры фильтрации обязательны.");
        When(query => query.Filter is not null, () =>
        {
            this.AddPaginationRules(
                query => query.Filter.PageNumber,
                query => query.Filter.PageSize,
                query => query.Filter.SortBy);
            RuleFor(query => query.Filter.Language)
                .IsInEnum().When(query => query.Filter.Language.HasValue)
                .WithMessage("Указан недопустимый язык шаблона.");
        });
    }
}

/// <summary>Возвращает только опубликованные шаблоны без отслеживания через специализированный репозиторий.</summary>
public sealed class GetTemplatesQueryHandler(
    ITemplateRepository repository,
    ICurrentUserContext currentUser,
    IMapper mapper) : IRequestHandler<GetTemplatesQuery, Response<PagedResult<GetTemplateDto>>>
{
    /// <summary>Требует аутентифицированного юриста и строит согласованную страницу каталога.</summary>
    public async Task<Response<PagedResult<GetTemplateDto>>> Handle(
        GetTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.LawyerId is not Guid lawyerId || lawyerId == Guid.Empty)
        {
            return Response<PagedResult<GetTemplateDto>>.Fail(
                "Требуется профиль юриста.", HttpStatusCode.Unauthorized);
        }

        var skip = ValidationRules.CalculateSkip(request.Filter.PageNumber, request.Filter.PageSize);
        var templates = await repository.GetActivePageAsync(
            request.Filter.Language, skip, request.Filter.PageSize, cancellationToken);
        var count = await repository.CountActiveAsync(request.Filter.Language, cancellationToken);
        var items = mapper.Map<IReadOnlyList<GetTemplateDto>>(templates);
        return Response<PagedResult<GetTemplateDto>>.Success(
            new PagedResult<GetTemplateDto>(items, count, request.Filter.PageNumber, request.Filter.PageSize));
    }
}

/// <summary>Запрашивает упорядоченный состав активных пунктов опубликованного шаблона.</summary>
public sealed record GetTemplateClauseBlocksQuery(Guid TemplateId, bool DefaultOnly)
    : IApplicationRequest<IReadOnlyList<ClauseBlockDetailDto>>;

/// <summary>Проверяет идентификатор шаблона, состав которого запрошен.</summary>
public sealed class GetTemplateClauseBlocksQueryValidator
    : AbstractValidator<GetTemplateClauseBlocksQuery>
{
    /// <summary>Создаёт правило непустого идентификатора.</summary>
    public GetTemplateClauseBlocksQueryValidator()
    {
        RuleFor(query => query.TemplateId).NotEmpty().WithMessage("Идентификатор шаблона обязателен.");
    }
}

/// <summary>Возвращает пункты одним специализированным запросом и тем самым исключает N+1.</summary>
public sealed class GetTemplateClauseBlocksQueryHandler(
    ITemplateRepository templateRepository,
    ITemplateClauseBlockRepository linkRepository,
    ICurrentUserContext currentUser,
    IMapper mapper) : IRequestHandler<GetTemplateClauseBlocksQuery, Response<IReadOnlyList<ClauseBlockDetailDto>>>
{
    /// <summary>Проверяет доступность шаблона и проецирует его активные пункты в доменном порядке.</summary>
    public async Task<Response<IReadOnlyList<ClauseBlockDetailDto>>> Handle(
        GetTemplateClauseBlocksQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.LawyerId is not Guid lawyerId || lawyerId == Guid.Empty)
        {
            return Response<IReadOnlyList<ClauseBlockDetailDto>>.Fail(
                "Требуется профиль юриста.", HttpStatusCode.Unauthorized);
        }

        if (await templateRepository.GetActiveByIdAsync(request.TemplateId, cancellationToken) is null)
        {
            return Response<IReadOnlyList<ClauseBlockDetailDto>>.Fail("Шаблон не найден.", HttpStatusCode.NotFound);
        }

        var blocks = await linkRepository.GetClauseBlocksByTemplateAsync(
            request.TemplateId, request.DefaultOnly, cancellationToken);
        return Response<IReadOnlyList<ClauseBlockDetailDto>>.Success(
            mapper.Map<IReadOnlyList<ClauseBlockDetailDto>>(blocks));
    }
}

/// <summary>Выполняет полнотекстовый и категорийный поиск по активным библиотечным пунктам.</summary>
public sealed record GetClauseBlocksQuery(ClauseBlockFilterParam Filter)
    : IApplicationRequest<PagedResult<ClauseBlockDetailDto>>;

/// <summary>Ограничивает поисковый ввод и размер страницы библиотечного запроса.</summary>
public sealed class GetClauseBlocksQueryValidator : AbstractValidator<GetClauseBlocksQuery>
{
    /// <summary>Создаёт правила пагинации, поиска и категории.</summary>
    public GetClauseBlocksQueryValidator()
    {
        RuleFor(query => query.Filter).NotNull().WithMessage("Параметры фильтрации обязательны.");
        When(query => query.Filter is not null, () =>
        {
            this.AddPaginationRules(
                query => query.Filter.PageNumber,
                query => query.Filter.PageSize,
                query => query.Filter.SortBy);
            RuleFor(query => query.Filter.SearchTerm)
                .MaximumLength(300).WithMessage("Поисковая строка не должна превышать 300 символов.");
            RuleFor(query => query.Filter.Category)
                .MaximumLength(100).WithMessage("Категория не должна превышать 100 символов.");
        });
    }
}

/// <summary>Возвращает согласованную страницу результатов поиска без загрузки неактивных пунктов.</summary>
public sealed class GetClauseBlocksQueryHandler(
    IClauseBlockRepository repository,
    ICurrentUserContext currentUser,
    IMapper mapper) : IRequestHandler<GetClauseBlocksQuery, Response<PagedResult<ClauseBlockDetailDto>>>
{
    /// <summary>Требует профиль юриста и использует одинаковые фильтры для страницы и общего количества.</summary>
    public async Task<Response<PagedResult<ClauseBlockDetailDto>>> Handle(
        GetClauseBlocksQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.LawyerId is not Guid lawyerId || lawyerId == Guid.Empty)
        {
            return Response<PagedResult<ClauseBlockDetailDto>>.Fail(
                "Требуется профиль юриста.", HttpStatusCode.Unauthorized);
        }

        var skip = ValidationRules.CalculateSkip(request.Filter.PageNumber, request.Filter.PageSize);
        var blocks = await repository.SearchActiveAsync(
            request.Filter.SearchTerm,
            request.Filter.Category,
            skip,
            request.Filter.PageSize,
            cancellationToken);
        var count = await repository.CountActiveAsync(
            request.Filter.SearchTerm, request.Filter.Category, cancellationToken);
        var items = mapper.Map<IReadOnlyList<ClauseBlockDetailDto>>(blocks);
        return Response<PagedResult<ClauseBlockDetailDto>>.Success(
            new PagedResult<ClauseBlockDetailDto>(items, count, request.Filter.PageNumber, request.Filter.PageSize));
    }
}
