using System.Net;
using Application.Common.CQRS;
using Domain.Exceptions;
using MediatR;

namespace Application.Common.Behaviors;

/// <summary>
/// Преобразует только ожидаемые доменные нарушения в безопасный Response,
/// сохраняя отмену и неожиданные инфраструктурные ошибки для глобального middleware и наблюдаемости.
/// </summary>
/// <typeparam name="TRequest">Тип Application-запроса.</typeparam>
/// <typeparam name="TResponse">Тип единого ответа.</typeparam>
public sealed class DomainExceptionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IFailureResponseFactory<TResponse>
{
    /// <summary>
    /// Выполняет следующий обработчик и перехватывает только доменные исключения управления инвариантами.
    /// </summary>
    /// <param name="request">Текущий запрос.</param>
    /// <param name="next">Следующий делегат конвейера.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат обработчика либо безопасная ошибка домена.</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next(cancellationToken);
        }
        catch (DomainValidationException exception)
        {
            return request.CreateFailure([exception.Message], HttpStatusCode.BadRequest);
        }
        catch (DomainException exception)
        {
            return request.CreateFailure([exception.Message], HttpStatusCode.Conflict);
        }
    }
}
