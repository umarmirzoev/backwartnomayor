using System.Net;
using Application.Common.CQRS;
using Application.Common.Models;
using Application.Common.Validation;
using Application.DTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using AutoMapper;
using Domain.Entities;
using FluentValidation;
using MediatR;
using CaseEntity = Domain.Entities.Case;

namespace Application.Features.ClientsAndCases;

/// <summary>Создаёт клиента под текущим юристом без принимаемого извне LawyerId.</summary>
/// <param name="Data">Контактные данные нового клиента.</param>
public sealed record CreateClientCommand(CreateClientDto Data) : IApplicationRequest<Guid>;

/// <summary>Проверяет взаимоисключающие типы клиента и ограничения контактных полей.</summary>
public sealed class CreateClientCommandValidator : AbstractValidator<CreateClientCommand>
{
    /// <summary>Инициализирует правила создания клиента из доменной модели и Fluent API.</summary>
    public CreateClientCommandValidator()
    {
        RuleFor(command => command.Data).NotNull().WithMessage("Данные клиента обязательны.");
        When(command => command.Data is not null, () =>
        {
            RuleFor(command => command.Data)
                .Must(data => string.IsNullOrWhiteSpace(data.FullName) != string.IsNullOrWhiteSpace(data.CompanyName))
                .WithMessage("Необходимо указать либо полное имя, либо название организации, но не оба значения.");
            RuleFor(command => command.Data.FullName)
                .MaximumLength(200).WithMessage("Полное имя не должно превышать 200 символов.");
            RuleFor(command => command.Data.CompanyName)
                .MaximumLength(300).WithMessage("Название организации не должно превышать 300 символов.");
            RuleFor(command => command.Data.ContactPhone)
                .MaximumLength(30).WithMessage("Телефон не должен превышать 30 символов.")
                .Matches("^\\+?[0-9 ()-]{7,30}$").WithMessage("Телефон имеет некорректный формат.")
                .When(command => !string.IsNullOrWhiteSpace(command.Data.ContactPhone));
            RuleFor(command => command.Data.ContactEmail)
                .MaximumLength(256).WithMessage("Email не должен превышать 256 символов.")
                .EmailAddress().WithMessage("Email клиента имеет некорректный формат.")
                .When(command => !string.IsNullOrWhiteSpace(command.Data.ContactEmail));
        });
    }
}

/// <summary>Создаёт клиентскую карточку в tenant-границе текущего юриста.</summary>
/// <param name="currentUser">Доверенный контекст владельца.</param>
/// <param name="repository">Репозиторий клиентов.</param>
/// <param name="unitOfWork">Единица фиксации.</param>
/// <param name="clock">Источник UTC-времени.</param>
public sealed class CreateClientCommandHandler(
    ICurrentUserContext currentUser,
    IClientRepository repository,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<CreateClientCommand, Response<Guid>>
{
    /// <summary>Создаёт клиента только для идентификатора из аутентифицированного контекста.</summary>
    public async Task<Response<Guid>> Handle(CreateClientCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.LawyerId is not Guid lawyerId || lawyerId == Guid.Empty)
        {
            return Response<Guid>.Fail("Требуется профиль юриста.", HttpStatusCode.Unauthorized);
        }

        var client = new Client(
            lawyerId,
            request.Data.FullName,
            request.Data.CompanyName,
            request.Data.ContactPhone,
            request.Data.ContactEmail,
            request.Data.Notes,
            clock.UtcNow);
        await repository.AddAsync(client, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Response<Guid>.Success(client.Id, "Клиент успешно создан.", HttpStatusCode.Created);
    }
}

/// <summary>Обновляет контактные данные принадлежащего текущему юристу клиента.</summary>
/// <param name="ClientId">Идентификатор клиента.</param>
/// <param name="Data">Новые контактные данные.</param>
public sealed record UpdateClientCommand(Guid ClientId, UpdateClientDto Data)
    : IApplicationRequest<ClientDetailDto>;

/// <summary>Проверяет идентификатор и те же доменные ограничения, что при создании клиента.</summary>
public sealed class UpdateClientCommandValidator : AbstractValidator<UpdateClientCommand>
{
    /// <summary>Инициализирует правила обновления клиентской карточки.</summary>
    public UpdateClientCommandValidator()
    {
        RuleFor(command => command.ClientId).NotEmpty().WithMessage("Идентификатор клиента обязателен.");
        RuleFor(command => command.Data).NotNull().WithMessage("Данные клиента обязательны.");
        When(command => command.Data is not null, () =>
        {
            RuleFor(command => command.Data)
                .Must(data => string.IsNullOrWhiteSpace(data.FullName) != string.IsNullOrWhiteSpace(data.CompanyName))
                .WithMessage("Необходимо указать либо полное имя, либо название организации, но не оба значения.");
            RuleFor(command => command.Data.FullName).MaximumLength(200)
                .WithMessage("Полное имя не должно превышать 200 символов.");
            RuleFor(command => command.Data.CompanyName).MaximumLength(300)
                .WithMessage("Название организации не должно превышать 300 символов.");
            RuleFor(command => command.Data.ContactPhone)
                .MaximumLength(30).WithMessage("Телефон не должен превышать 30 символов.")
                .Matches("^\\+?[0-9 ()-]{7,30}$").WithMessage("Телефон имеет некорректный формат.")
                .When(command => !string.IsNullOrWhiteSpace(command.Data.ContactPhone));
            RuleFor(command => command.Data.ContactEmail)
                .MaximumLength(256).WithMessage("Email не должен превышать 256 символов.")
                .EmailAddress().WithMessage("Email клиента имеет некорректный формат.")
                .When(command => !string.IsNullOrWhiteSpace(command.Data.ContactEmail));
        });
    }
}

/// <summary>Выполняет tenant-безопасное обновление клиента через доменный метод.</summary>
/// <param name="currentUser">Доверенный контекст владельца.</param>
/// <param name="repository">Репозиторий клиентов.</param>
/// <param name="unitOfWork">Единица фиксации.</param>
/// <param name="mapper">Маппинг результата в DTO.</param>
public sealed class UpdateClientCommandHandler(
    ICurrentUserContext currentUser,
    IClientRepository repository,
    IUnitOfWork unitOfWork,
    IMapper mapper)
    : IRequestHandler<UpdateClientCommand, Response<ClientDetailDto>>
{
    /// <summary>Не различает несуществующий и чужой идентификатор, предотвращая утечку tenant-данных.</summary>
    public async Task<Response<ClientDetailDto>> Handle(
        UpdateClientCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.LawyerId is not Guid lawyerId || lawyerId == Guid.Empty)
        {
            return Response<ClientDetailDto>.Fail("Требуется профиль юриста.", HttpStatusCode.Unauthorized);
        }

        var client = await repository.GetByIdForLawyerAsync(request.ClientId, lawyerId, cancellationToken);
        if (client is null)
        {
            return Response<ClientDetailDto>.Fail("Клиент не найден.", HttpStatusCode.NotFound);
        }

        client.UpdateDetails(
            request.Data.FullName,
            request.Data.CompanyName,
            request.Data.ContactPhone,
            request.Data.ContactEmail,
            request.Data.Notes);
        await repository.UpdateAsync(client, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Response<ClientDetailDto>.Success(mapper.Map<ClientDetailDto>(client), "Клиент успешно обновлён.");
    }
}

/// <summary>Создаёт открытое дело у принадлежащего текущему юристу клиента.</summary>
/// <param name="Data">Сведения нового дела.</param>
public sealed record CreateCaseCommand(CreateCaseDto Data) : IApplicationRequest<Guid>;

/// <summary>Проверяет идентификатор клиента и ограничения сведений дела.</summary>
public sealed class CreateCaseCommandValidator : AbstractValidator<CreateCaseCommand>
{
    /// <summary>Инициализирует правила создания дела.</summary>
    public CreateCaseCommandValidator()
    {
        RuleFor(command => command.Data).NotNull().WithMessage("Данные дела обязательны.");
        When(command => command.Data is not null, () =>
        {
            RuleFor(command => command.Data.ClientId).NotEmpty().WithMessage("Идентификатор клиента обязателен.");
            RuleFor(command => command.Data.Title)
                .NotEmpty().WithMessage("Название дела обязательно.")
                .MaximumLength(300).WithMessage("Название дела не должно превышать 300 символов.");
        });
    }
}

/// <summary>Создаёт дело после явной проверки владения клиентом.</summary>
/// <param name="currentUser">Доверенный контекст владельца.</param>
/// <param name="clientRepository">Репозиторий клиентов.</param>
/// <param name="caseRepository">Репозиторий дел.</param>
/// <param name="unitOfWork">Единица фиксации.</param>
/// <param name="clock">Источник UTC-времени.</param>
public sealed class CreateCaseCommandHandler(
    ICurrentUserContext currentUser,
    IClientRepository clientRepository,
    ICaseRepository caseRepository,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<CreateCaseCommand, Response<Guid>>
{
    /// <summary>Создаёт дело только если ClientId принадлежит текущему LawyerId.</summary>
    public async Task<Response<Guid>> Handle(CreateCaseCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.LawyerId is not Guid lawyerId || lawyerId == Guid.Empty)
        {
            return Response<Guid>.Fail("Требуется профиль юриста.", HttpStatusCode.Unauthorized);
        }

        if (!await clientRepository.ExistsForLawyerAsync(request.Data.ClientId, lawyerId, cancellationToken))
        {
            return Response<Guid>.Fail("Клиент не найден.", HttpStatusCode.NotFound);
        }

        var caseItem = new CaseEntity(
            request.Data.ClientId,
            lawyerId,
            request.Data.Title,
            request.Data.Description,
            clock.UtcNow);
        await caseRepository.AddAsync(caseItem, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Response<Guid>.Success(caseItem.Id, "Дело успешно создано.", HttpStatusCode.Created);
    }
}

/// <summary>Обновляет название и описание принадлежащего юристу дела.</summary>
/// <param name="CaseId">Идентификатор дела.</param>
/// <param name="Data">Новые сведения.</param>
public sealed record UpdateCaseCommand(Guid CaseId, UpdateCaseDto Data)
    : IApplicationRequest<CaseDetailDto>;

/// <summary>Проверяет входные данные обновления дела.</summary>
public sealed class UpdateCaseCommandValidator : AbstractValidator<UpdateCaseCommand>
{
    /// <summary>Инициализирует правила идентификатора и названия.</summary>
    public UpdateCaseCommandValidator()
    {
        RuleFor(command => command.CaseId).NotEmpty().WithMessage("Идентификатор дела обязателен.");
        RuleFor(command => command.Data).NotNull().WithMessage("Данные дела обязательны.");
        When(command => command.Data is not null, () => RuleFor(command => command.Data.Title)
            .NotEmpty().WithMessage("Название дела обязательно.")
            .MaximumLength(300).WithMessage("Название дела не должно превышать 300 символов."));
    }
}

/// <summary>Обновляет дело через доменный метод после tenant-проверки.</summary>
/// <param name="currentUser">Доверенный контекст.</param>
/// <param name="caseRepository">Репозиторий дел.</param>
/// <param name="draftRepository">Репозиторий документов для сводки.</param>
/// <param name="unitOfWork">Единица фиксации.</param>
/// <param name="mapper">Маппинг DTO.</param>
public sealed class UpdateCaseCommandHandler(
    ICurrentUserContext currentUser,
    ICaseRepository caseRepository,
    IDraftRepository draftRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper)
    : IRequestHandler<UpdateCaseCommand, Response<CaseDetailDto>>
{
    /// <summary>Возвращает обновлённую карточку со сводным числом документов.</summary>
    public async Task<Response<CaseDetailDto>> Handle(UpdateCaseCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.LawyerId is not Guid lawyerId || lawyerId == Guid.Empty)
        {
            return Response<CaseDetailDto>.Fail("Требуется профиль юриста.", HttpStatusCode.Unauthorized);
        }

        var caseItem = await caseRepository.GetByIdForLawyerAsync(request.CaseId, lawyerId, cancellationToken);
        if (caseItem is null)
        {
            return Response<CaseDetailDto>.Fail("Дело не найдено.", HttpStatusCode.NotFound);
        }

        caseItem.UpdateDetails(request.Data.Title, request.Data.Description);
        await caseRepository.UpdateAsync(caseItem, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var count = await draftRepository.CountByCaseForLawyerAsync(
            caseItem.Id, lawyerId, null, cancellationToken);
        var dto = mapper.Map<CaseDetailDto>(caseItem) with { DocumentCount = count };
        return Response<CaseDetailDto>.Success(dto, "Дело успешно обновлено.");
    }
}

/// <summary>Закрывает принадлежащее текущему юристу дело.</summary>
/// <param name="CaseId">Идентификатор дела.</param>
public sealed record CloseCaseCommand(Guid CaseId) : IApplicationRequest<bool>;

/// <summary>Проверяет идентификатор закрываемого дела.</summary>
public sealed class CloseCaseCommandValidator : AbstractValidator<CloseCaseCommand>
{
    /// <summary>Инициализирует обязательность идентификатора.</summary>
    public CloseCaseCommandValidator() =>
        RuleFor(command => command.CaseId).NotEmpty().WithMessage("Идентификатор дела обязателен.");
}

/// <summary>Закрывает дело доменным методом с проверкой владельца.</summary>
/// <param name="currentUser">Доверенный контекст.</param>
/// <param name="repository">Репозиторий дел.</param>
/// <param name="unitOfWork">Единица фиксации.</param>
/// <param name="clock">Источник UTC-времени.</param>
public sealed class CloseCaseCommandHandler(
    ICurrentUserContext currentUser,
    ICaseRepository repository,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<CloseCaseCommand, Response<bool>>
{
    /// <summary>Переводит только собственное открытое дело в Closed.</summary>
    public async Task<Response<bool>> Handle(CloseCaseCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.LawyerId is not Guid lawyerId || lawyerId == Guid.Empty)
        {
            return Response<bool>.Fail("Требуется профиль юриста.", HttpStatusCode.Unauthorized);
        }

        var caseItem = await repository.GetByIdForLawyerAsync(request.CaseId, lawyerId, cancellationToken);
        if (caseItem is null)
        {
            return Response<bool>.Fail("Дело не найдено.", HttpStatusCode.NotFound);
        }

        caseItem.Close(clock.UtcNow);
        await repository.UpdateAsync(caseItem, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Response<bool>.Success(true, "Дело успешно закрыто.");
    }
}

/// <summary>Запрашивает карточку клиента текущего юриста.</summary>
/// <param name="ClientId">Идентификатор клиента.</param>
public sealed record GetClientByIdQuery(Guid ClientId) : IApplicationRequest<ClientDetailDto>;

/// <summary>Проверяет идентификатор клиента.</summary>
public sealed class GetClientByIdQueryValidator : AbstractValidator<GetClientByIdQuery>
{
    /// <summary>Инициализирует обязательность идентификатора.</summary>
    public GetClientByIdQueryValidator() =>
        RuleFor(query => query.ClientId).NotEmpty().WithMessage("Идентификатор клиента обязателен.");
}

/// <summary>Возвращает клиентскую карточку через tenant-фильтр репозитория.</summary>
/// <param name="currentUser">Доверенный контекст.</param>
/// <param name="repository">Репозиторий клиентов.</param>
/// <param name="mapper">Маппинг DTO.</param>
public sealed class GetClientByIdQueryHandler(
    ICurrentUserContext currentUser,
    IClientRepository repository,
    IMapper mapper)
    : IRequestHandler<GetClientByIdQuery, Response<ClientDetailDto>>
{
    /// <summary>Возвращает 404 как для отсутствующего, так и для чужого клиента.</summary>
    public async Task<Response<ClientDetailDto>> Handle(
        GetClientByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.LawyerId is not Guid lawyerId || lawyerId == Guid.Empty)
        {
            return Response<ClientDetailDto>.Fail("Требуется профиль юриста.", HttpStatusCode.Unauthorized);
        }

        var client = await repository.GetByIdForLawyerAsync(request.ClientId, lawyerId, cancellationToken);
        return client is null
            ? Response<ClientDetailDto>.Fail("Клиент не найден.", HttpStatusCode.NotFound)
            : Response<ClientDetailDto>.Success(mapper.Map<ClientDetailDto>(client));
    }
}

/// <summary>Запрашивает страницу активных клиентов текущего юриста.</summary>
/// <param name="Filter">Параметры поиска и пагинации.</param>
public sealed record GetClientsQuery(ClientFilterParam Filter)
    : IApplicationRequest<PagedResult<GetClientDto>>;

/// <summary>Проверяет поиск и границы страницы клиентов.</summary>
public sealed class GetClientsQueryValidator : AbstractValidator<GetClientsQuery>
{
    /// <summary>Инициализирует правила фильтра списка клиентов.</summary>
    public GetClientsQueryValidator()
    {
        RuleFor(query => query.Filter).NotNull().WithMessage("Параметры фильтра обязательны.");
        When(query => query.Filter is not null, () =>
        {
            this.AddPaginationRules(
                query => query.Filter.PageNumber,
                query => query.Filter.PageSize,
                query => query.Filter.SortBy);
            RuleFor(query => query.Filter.SearchTerm)
                .MaximumLength(200).WithMessage("Поисковая строка не должна превышать 200 символов.");
        });
    }
}

/// <summary>Возвращает пагинированную tenant-безопасную выборку клиентов без отслеживания.</summary>
/// <param name="currentUser">Доверенный контекст.</param>
/// <param name="repository">Репозиторий клиентов.</param>
/// <param name="mapper">Маппинг DTO.</param>
public sealed class GetClientsQueryHandler(
    ICurrentUserContext currentUser,
    IClientRepository repository,
    IMapper mapper)
    : IRequestHandler<GetClientsQuery, Response<PagedResult<GetClientDto>>>
{
    /// <summary>Получает список и счётчик с одинаковым tenant/search-фильтром.</summary>
    public async Task<Response<PagedResult<GetClientDto>>> Handle(
        GetClientsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.LawyerId is not Guid lawyerId || lawyerId == Guid.Empty)
        {
            return Response<PagedResult<GetClientDto>>.Fail("Требуется профиль юриста.", HttpStatusCode.Unauthorized);
        }

        var skip = ValidationRules.CalculateSkip(request.Filter.PageNumber, request.Filter.PageSize);
        var clients = await repository.GetPageByLawyerAsync(
            lawyerId, request.Filter.SearchTerm, skip, request.Filter.PageSize, cancellationToken);
        var count = await repository.CountByLawyerAsync(lawyerId, request.Filter.SearchTerm, cancellationToken);
        var items = clients.Select(mapper.Map<GetClientDto>).ToArray();
        return Response<PagedResult<GetClientDto>>.Success(
            new PagedResult<GetClientDto>(items, count, request.Filter.PageNumber, request.Filter.PageSize));
    }
}

/// <summary>Запрашивает страницу дел конкретного клиента текущего юриста.</summary>
/// <param name="ClientId">Идентификатор клиента.</param>
/// <param name="Filter">Фильтр состояния и страницы.</param>
public sealed record GetClientCasesQuery(Guid ClientId, CaseFilterParam Filter)
    : IApplicationRequest<PagedResult<GetCaseDto>>;

/// <summary>Проверяет идентификатор клиента и параметры страницы дел.</summary>
public sealed class GetClientCasesQueryValidator : AbstractValidator<GetClientCasesQuery>
{
    /// <summary>Инициализирует правила запроса дел клиента.</summary>
    public GetClientCasesQueryValidator()
    {
        RuleFor(query => query.ClientId).NotEmpty().WithMessage("Идентификатор клиента обязателен.");
        RuleFor(query => query.Filter).NotNull().WithMessage("Параметры фильтра обязательны.");
        When(query => query.Filter is not null, () => this.AddPaginationRules(
            query => query.Filter.PageNumber,
            query => query.Filter.PageSize,
            query => query.Filter.SortBy));
    }
}

/// <summary>Возвращает дела только после отдельного подтверждения владения клиентом.</summary>
/// <param name="currentUser">Доверенный контекст.</param>
/// <param name="clientRepository">Репозиторий клиентов.</param>
/// <param name="caseRepository">Репозиторий дел.</param>
/// <param name="mapper">Маппинг DTO.</param>
public sealed class GetClientCasesQueryHandler(
    ICurrentUserContext currentUser,
    IClientRepository clientRepository,
    ICaseRepository caseRepository,
    IMapper mapper)
    : IRequestHandler<GetClientCasesQuery, Response<PagedResult<GetCaseDto>>>
{
    /// <summary>Возвращает страницу дел с двойным ограничением ClientId/LawyerId.</summary>
    public async Task<Response<PagedResult<GetCaseDto>>> Handle(
        GetClientCasesQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.LawyerId is not Guid lawyerId || lawyerId == Guid.Empty)
        {
            return Response<PagedResult<GetCaseDto>>.Fail("Требуется профиль юриста.", HttpStatusCode.Unauthorized);
        }

        if (!await clientRepository.ExistsForLawyerAsync(request.ClientId, lawyerId, cancellationToken))
        {
            return Response<PagedResult<GetCaseDto>>.Fail("Клиент не найден.", HttpStatusCode.NotFound);
        }

        var skip = ValidationRules.CalculateSkip(request.Filter.PageNumber, request.Filter.PageSize);
        var cases = await caseRepository.GetByClientForLawyerAsync(
            request.ClientId, lawyerId, request.Filter.Status, skip, request.Filter.PageSize, cancellationToken);
        var count = await caseRepository.CountByClientForLawyerAsync(
            request.ClientId, lawyerId, request.Filter.Status, cancellationToken);
        var items = cases.Select(mapper.Map<GetCaseDto>).ToArray();
        return Response<PagedResult<GetCaseDto>>.Success(
            new PagedResult<GetCaseDto>(items, count, request.Filter.PageNumber, request.Filter.PageSize));
    }
}

/// <summary>Запрашивает карточку дела со сводкой документов.</summary>
/// <param name="CaseId">Идентификатор дела.</param>
public sealed record GetCaseByIdQuery(Guid CaseId) : IApplicationRequest<CaseDetailDto>;

/// <summary>Проверяет идентификатор дела.</summary>
public sealed class GetCaseByIdQueryValidator : AbstractValidator<GetCaseByIdQuery>
{
    /// <summary>Инициализирует обязательность идентификатора.</summary>
    public GetCaseByIdQueryValidator() =>
        RuleFor(query => query.CaseId).NotEmpty().WithMessage("Идентификатор дела обязателен.");
}

/// <summary>Возвращает собственное дело и отдельным агрегатом считает документы.</summary>
/// <param name="currentUser">Доверенный контекст.</param>
/// <param name="caseRepository">Репозиторий дел.</param>
/// <param name="draftRepository">Репозиторий документов.</param>
/// <param name="mapper">Маппинг DTO.</param>
public sealed class GetCaseByIdQueryHandler(
    ICurrentUserContext currentUser,
    ICaseRepository caseRepository,
    IDraftRepository draftRepository,
    IMapper mapper)
    : IRequestHandler<GetCaseByIdQuery, Response<CaseDetailDto>>
{
    /// <summary>Не раскрывает существование чужого дела и возвращает сводное число документов.</summary>
    public async Task<Response<CaseDetailDto>> Handle(
        GetCaseByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.LawyerId is not Guid lawyerId || lawyerId == Guid.Empty)
        {
            return Response<CaseDetailDto>.Fail("Требуется профиль юриста.", HttpStatusCode.Unauthorized);
        }

        var caseItem = await caseRepository.GetByIdForLawyerAsync(request.CaseId, lawyerId, cancellationToken);
        if (caseItem is null)
        {
            return Response<CaseDetailDto>.Fail("Дело не найдено.", HttpStatusCode.NotFound);
        }

        var count = await draftRepository.CountByCaseForLawyerAsync(
            caseItem.Id, lawyerId, null, cancellationToken);
        return Response<CaseDetailDto>.Success(
            mapper.Map<CaseDetailDto>(caseItem) with { DocumentCount = count });
    }
}

/// <summary>Запрашивает страницу документов конкретного дела.</summary>
/// <param name="CaseId">Идентификатор дела.</param>
/// <param name="Filter">Фильтр состояния и страницы.</param>
public sealed record GetCaseDocumentsQuery(Guid CaseId, DraftFilterParam Filter)
    : IApplicationRequest<PagedResult<GetDraftDto>>;

/// <summary>Проверяет идентификатор дела и параметры списка документов.</summary>
public sealed class GetCaseDocumentsQueryValidator : AbstractValidator<GetCaseDocumentsQuery>
{
    /// <summary>Инициализирует правила списка документов.</summary>
    public GetCaseDocumentsQueryValidator()
    {
        RuleFor(query => query.CaseId).NotEmpty().WithMessage("Идентификатор дела обязателен.");
        RuleFor(query => query.Filter).NotNull().WithMessage("Параметры фильтра обязательны.");
        When(query => query.Filter is not null, () => this.AddPaginationRules(
            query => query.Filter.PageNumber,
            query => query.Filter.PageSize,
            query => query.Filter.SortBy));
    }
}

/// <summary>Возвращает документы и загружает типы шаблонов одним дополнительным запросом без N+1.</summary>
/// <param name="currentUser">Доверенный контекст.</param>
/// <param name="caseRepository">Репозиторий дел.</param>
/// <param name="draftRepository">Репозиторий документов.</param>
/// <param name="templateRepository">Репозиторий типов документов.</param>
/// <param name="mapper">Маппинг DTO.</param>
public sealed class GetCaseDocumentsQueryHandler(
    ICurrentUserContext currentUser,
    ICaseRepository caseRepository,
    IDraftRepository draftRepository,
    ITemplateRepository templateRepository,
    IMapper mapper)
    : IRequestHandler<GetCaseDocumentsQuery, Response<PagedResult<GetDraftDto>>>
{
    /// <summary>Возвращает только документы собственного дела с типом, статусом и датой.</summary>
    public async Task<Response<PagedResult<GetDraftDto>>> Handle(
        GetCaseDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.LawyerId is not Guid lawyerId || lawyerId == Guid.Empty)
        {
            return Response<PagedResult<GetDraftDto>>.Fail("Требуется профиль юриста.", HttpStatusCode.Unauthorized);
        }

        if (!await caseRepository.ExistsForLawyerAsync(request.CaseId, lawyerId, cancellationToken))
        {
            return Response<PagedResult<GetDraftDto>>.Fail("Дело не найдено.", HttpStatusCode.NotFound);
        }

        var skip = ValidationRules.CalculateSkip(request.Filter.PageNumber, request.Filter.PageSize);
        var drafts = await draftRepository.GetByCaseForLawyerAsync(
            request.CaseId, lawyerId, request.Filter.Status, skip, request.Filter.PageSize, cancellationToken);
        var count = await draftRepository.CountByCaseForLawyerAsync(
            request.CaseId, lawyerId, request.Filter.Status, cancellationToken);
        var templates = await templateRepository.GetAllAsync(cancellationToken);
        var names = templates.ToDictionary(template => template.Id, template => template.Name);
        var items = drafts.Select(draft => mapper.Map<GetDraftDto>(draft) with
        {
            TemplateName = names.GetValueOrDefault(draft.TemplateId)
        }).ToArray();
        return Response<PagedResult<GetDraftDto>>.Success(
            new PagedResult<GetDraftDto>(items, count, request.Filter.PageNumber, request.Filter.PageSize));
    }
}
