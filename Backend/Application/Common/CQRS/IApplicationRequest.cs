using System.Net;
using Application.Common.Models;
using Domain.Enums;
using MediatR;

namespace Application.Common.CQRS;

/// <summary>
/// Определяет фабрику типизированного ошибочного ответа для конвейерных обработчиков MediatR.
/// </summary>
/// <typeparam name="TResponse">Тип ответа конкретного запроса.</typeparam>
public interface IFailureResponseFactory<out TResponse>
{
    /// <summary>
    /// Создаёт ответ с ожидаемыми ошибками без использования рефлексии и исключений управления потоком.
    /// </summary>
    /// <param name="errors">Безопасные сообщения ошибок.</param>
    /// <param name="statusCode">Рекомендуемый HTTP-код.</param>
    /// <returns>Типизированный ошибочный ответ.</returns>
    TResponse CreateFailure(IEnumerable<string> errors, HttpStatusCode statusCode);
}

/// <summary>
/// Маркирует команду или запрос Application-слоя, всегда возвращающий единый <see cref="Response{T}"/>.
/// Интерфейс позволяет behaviors централизованно возвращать ошибки валидации и доменных правил.
/// </summary>
/// <typeparam name="TData">Тип полезных данных операции.</typeparam>
public interface IApplicationRequest<TData>
    : IRequest<Response<TData>>, IFailureResponseFactory<Response<TData>>
{
    /// <inheritdoc />
    Response<TData> IFailureResponseFactory<Response<TData>>.CreateFailure(
        IEnumerable<string> errors,
        HttpStatusCode statusCode)
    {
        return Response<TData>.Fail(errors, statusCode);
    }
}

/// <summary>
/// Маркирует команды, расходующие один запрос ИИ и подлежащие предварительной проверке Redis-квоты.
/// </summary>
public interface IAiMeteredRequest
{
    /// <summary>Получает тип тарифицируемой ИИ-операции.</summary>
    AiRequestType RequestType { get; }
}
