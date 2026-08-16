using Application.DTOs;
using AutoMapper;
using Domain.Entities;

namespace Application.Mappings;

/// <summary>
/// Определяет преобразования двуязычного договорного пункта в краткие и детальные модели библиотеки.
/// </summary>
public sealed class ClauseBlockProfile : Profile
{
    /// <summary>
    /// Инициализирует односторонние преобразования пункта и модель допустимого редактирования содержимого.
    /// </summary>
    public ClauseBlockProfile()
    {
        CreateMap<ClauseBlock, GetClauseBlockDto>();
        CreateMap<ClauseBlock, ClauseBlockDetailDto>();
        CreateMap<ClauseBlock, UpdateClauseBlockDto>();
    }
}
