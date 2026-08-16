using System.Net;
using Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// Предоставляет единый маршрут API v1, MediatR-диспетчер и точное преобразование Response в HTTP-результат.
/// Контроллеры не получают репозитории и остаются тонкой транспортной границей Clean Architecture.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Инициализирует базовый контроллер единственным диспетчером CQRS.</summary>
    /// <param name="sender">MediatR-диспетчер команд и запросов.</param>
    protected ApiControllerBase(ISender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);
        Sender = sender;
    }

    /// <summary>Получает MediatR-диспетчер для отправки CQRS-операций.</summary>
    protected ISender Sender { get; }

    /// <summary>
    /// Преобразует StatusCode из Application Response в 200/201/202/204/400/404 и другие точные HTTP-коды.
    /// Для успешного создания при наличии имени GET-экшена формирует Location через CreatedAtAction.
    /// </summary>
    /// <typeparam name="T">Тип данных ответа.</typeparam>
    /// <param name="response">Единый результат Application-слоя.</param>
    /// <param name="createdActionName">Необязательное имя GET-экшена для Location.</param>
    /// <param name="routeValues">Значения маршрута созданного ресурса.</param>
    /// <param name="noContentOnSuccess">Признак преобразования успешной команды без полезного тела в HTTP 204.</param>
    /// <returns>Точный ActionResult без переинтерпретации бизнес-ошибки.</returns>
    protected ActionResult<Response<T>> ToActionResult<T>(
        Response<T> response,
        string? createdActionName = null,
        object? routeValues = null,
        bool noContentOnSuccess = false)
    {
        ArgumentNullException.ThrowIfNull(response);
        if (response.IsSuccess
            && (response.StatusCode == HttpStatusCode.NoContent || noContentOnSuccess))
        {
            return NoContent();
        }

        if (response.IsSuccess
            && response.StatusCode == HttpStatusCode.Created
            && !string.IsNullOrWhiteSpace(createdActionName))
        {
            return CreatedAtAction(createdActionName, routeValues, response);
        }

        return StatusCode((int)response.StatusCode, response);
    }
}
