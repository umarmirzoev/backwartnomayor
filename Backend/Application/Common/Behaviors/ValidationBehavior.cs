using System.Net;
using Application.Common.CQRS;
using FluentValidation;
using MediatR;

namespace Application.Common.Behaviors;

/// <summary>
/// Выполняет все FluentValidation-валидаторы до обработчика и преобразует ожидаемые ошибки
/// в единый типизированный ответ, исключая запуск репозиториев и внешних сервисов с некорректными данными.
/// </summary>
/// <typeparam name="TRequest">Тип команды или запроса.</typeparam>
/// <typeparam name="TResponse">Тип единого ответа Application-слоя.</typeparam>
/// <param name="validators">Все валидаторы конкретной операции из контейнера зависимостей.</param>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IFailureResponseFactory<TResponse>
{
    /// <summary>
    /// Проверяет запрос параллельно всеми валидаторами и продолжает конвейер только при отсутствии ошибок.
    /// </summary>
    /// <param name="request">Проверяемая команда или запрос.</param>
    /// <param name="next">Следующий делегат конвейера MediatR.</param>
    /// <param name="cancellationToken">Токен отмены всей операции.</param>
    /// <returns>Результат обработчика либо типизированная ошибка HTTP 400.</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var validatorArray = validators as IValidator<TRequest>[] ?? validators.ToArray();
        if (validatorArray.Length == 0)
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            validatorArray.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        var errors = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .Select(failure => failure.ErrorMessage)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return errors.Length == 0
            ? await next(cancellationToken)
            : request.CreateFailure(errors, HttpStatusCode.BadRequest);
    }
}
