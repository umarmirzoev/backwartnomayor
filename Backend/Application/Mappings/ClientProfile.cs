using Application.DTOs;
using AutoMapper;
using Domain.Entities;

namespace Application.Mappings;

/// <summary>
/// Определяет проекции клиента в краткую, детальную и редактируемую модели без раскрытия владельца.
/// </summary>
public sealed class ClientProfile : Profile
{
    /// <summary>
    /// Инициализирует односторонние преобразования клиента; обратное создание выполняется доменным конструктором.
    /// </summary>
    public ClientProfile()
    {
        CreateMap<Client, GetClientDto>();
        CreateMap<Client, ClientDetailDto>();
        CreateMap<Client, UpdateClientDto>();
    }
}
