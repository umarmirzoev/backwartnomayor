using Domain.Enums;

namespace Application.DTOs;

/// <summary>
/// Представляет краткую append-only запись фактического обращения к ИИ для истории использования.
/// </summary>
/// <param name="Id">Идентификатор записи.</param>
/// <param name="RequestType">Строковое имя типа ИИ-операции.</param>
/// <param name="DraftId">Идентификатор связанного черновика.</param>
/// <param name="Succeeded">Признак успешного завершения.</param>
/// <param name="CreatedAt">Дата обращения.</param>
public sealed record GetAiUsageRecordDto(
    Guid Id,
    string RequestType,
    Guid? DraftId,
    bool Succeeded,
    DateTimeOffset CreatedAt);

/// <summary>
/// Представляет полную запись использования ИИ с привязкой к юристу и квоте периода.
/// </summary>
/// <param name="Id">Идентификатор записи.</param>
/// <param name="LawyerId">Идентификатор профиля юриста.</param>
/// <param name="AiUsageQuotaId">Идентификатор квоты.</param>
/// <param name="RequestType">Строковое имя операции.</param>
/// <param name="DraftId">Идентификатор черновика.</param>
/// <param name="Succeeded">Признак успешности.</param>
/// <param name="CreatedAt">Дата обращения.</param>
public sealed record AiUsageRecordDetailDto(
    Guid Id,
    Guid LawyerId,
    Guid AiUsageQuotaId,
    string RequestType,
    Guid? DraftId,
    bool Succeeded,
    DateTimeOffset CreatedAt);

/// <summary>
/// Представляет внутренние данные результата ИИ-вызова для регистрации через агрегат квоты.
/// Владельца, квоту и время определяет серверная операция, поэтому они отсутствуют во входном контракте.
/// </summary>
/// <param name="RequestType">Тип выполненной ИИ-операции.</param>
/// <param name="DraftId">Идентификатор связанного черновика.</param>
/// <param name="Succeeded">Признак успешного результата провайдера.</param>
public sealed record CreateAiUsageRecordDto(
    AiRequestType RequestType,
    Guid? DraftId,
    bool Succeeded);

/// <summary>
/// Маркер отсутствующего сценария обновления записи использования ИИ.
/// Запись является append-only доказательством расходования квоты и не может редактироваться.
/// </summary>
public sealed record UpdateAiUsageRecordDto;
