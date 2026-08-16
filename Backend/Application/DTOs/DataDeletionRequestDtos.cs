using Domain.Enums;

namespace Application.DTOs;

/// <summary>
/// Представляет краткое состояние запроса на полное удаление доменных данных.
/// </summary>
/// <param name="Id">Идентификатор запроса.</param>
/// <param name="TargetEntityType">Строковое имя типа удаляемого объекта.</param>
/// <param name="TargetEntityId">Идентификатор удаляемого объекта.</param>
/// <param name="Status">Строковое имя состояния обработки.</param>
/// <param name="RequestedAt">Дата регистрации запроса.</param>
/// <param name="CompletedAt">Дата завершения уничтожения данных.</param>
public sealed record GetDataDeletionRequestDto(
    Guid Id,
    string TargetEntityType,
    Guid TargetEntityId,
    string Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset? CompletedAt);

/// <summary>
/// Представляет полную карточку workflow удаления для авторизованного контроля и аудита.
/// </summary>
/// <param name="Id">Идентификатор запроса.</param>
/// <param name="RequestedByType">Строковое имя типа инициатора.</param>
/// <param name="RequestedById">Идентификатор инициатора.</param>
/// <param name="TargetEntityType">Строковое имя типа цели.</param>
/// <param name="TargetEntityId">Идентификатор цели.</param>
/// <param name="RequestedAt">Дата регистрации.</param>
/// <param name="CompletedAt">Дата завершения.</param>
/// <param name="Status">Строковое имя состояния.</param>
public sealed record DataDeletionRequestDetailDto(
    Guid Id,
    string RequestedByType,
    Guid RequestedById,
    string TargetEntityType,
    Guid TargetEntityId,
    DateTimeOffset RequestedAt,
    DateTimeOffset? CompletedAt,
    string Status);

/// <summary>
/// Представляет безопасный публичный запрос на полное удаление.
/// Инициатор определяется по аутентифицированному субъекту, что предотвращает подделку авторства запроса.
/// </summary>
/// <param name="TargetEntityType">Тип удаляемого объекта.</param>
/// <param name="TargetEntityId">Идентификатор принадлежащего субъекту объекта.</param>
public sealed record CreateDataDeletionRequestDto(
    DeletionTargetType TargetEntityType,
    Guid TargetEntityId);

/// <summary>
/// Маркер отсутствующего общего обновления запроса на удаление.
/// Завершение и отклонение являются отдельными командами с проверкой полномочий и допустимого состояния.
/// </summary>
public sealed record UpdateDataDeletionRequestDto;
