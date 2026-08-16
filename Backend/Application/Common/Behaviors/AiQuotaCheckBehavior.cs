using System.Net;
using Application.Common.CQRS;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using MediatR;

namespace Application.Common.Behaviors;

/// <summary>
/// Проверяет быстрый Redis-счётчик перед тарифицируемыми ИИ-командами.
/// PostgreSQL-квота остаётся источником истины, а окончательное атомарное резервирование
/// выполняется обработчиком непосредственно перед вызовом внешнего ИИ.
/// </summary>
/// <typeparam name="TRequest">Тип команды.</typeparam>
/// <typeparam name="TResponse">Тип единого ответа команды.</typeparam>
/// <param name="currentUser">Доверенный контекст текущего пользователя.</param>
/// <param name="clock">Источник текущего UTC-времени.</param>
/// <param name="quotaRepository">Репозиторий персистентной квоты.</param>
/// <param name="quotaCounter">Быстрый атомарный счётчик Redis.</param>
public sealed class AiQuotaCheckBehavior<TRequest, TResponse>(
    ICurrentUserContext currentUser,
    IClock clock,
    IAiUsageQuotaRepository quotaRepository,
    IAiQuotaCounter quotaCounter)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IFailureResponseFactory<TResponse>
{
    /// <summary>
    /// Пропускает обычные запросы без изменений и отклоняет ИИ-команды при отсутствии квоты или исчерпанном лимите.
    /// </summary>
    /// <param name="request">Текущая команда.</param>
    /// <param name="next">Следующий делегат конвейера.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат команды либо ошибка авторизации/квоты.</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IAiMeteredRequest)
        {
            return await next(cancellationToken);
        }

        if (currentUser.LawyerId is not Guid lawyerId || lawyerId == Guid.Empty)
        {
            return request.CreateFailure(
                ["Для выполнения ИИ-операции требуется аутентифицированный профиль юриста."],
                HttpStatusCode.Unauthorized);
        }

        var quota = await quotaRepository.GetCurrentAsync(lawyerId, clock.UtcNow, cancellationToken);
        if (quota is null)
        {
            return request.CreateFailure(
                ["Квота ИИ для текущего периода не найдена."],
                HttpStatusCode.Conflict);
        }

        var isAvailable = await quotaCounter.IsAvailableAsync(
            lawyerId,
            quota.Id,
            quota.RequestsUsed,
            quota.RequestsLimit,
            quota.PeriodEnd,
            cancellationToken);

        return isAvailable
            ? await next(cancellationToken)
            : request.CreateFailure(
                ["Лимит ИИ-запросов за текущий период исчерпан."],
                HttpStatusCode.TooManyRequests);
    }
}
