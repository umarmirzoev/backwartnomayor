using Application.DTOs;
using AutoMapper;
using Domain.Entities;

namespace Application.Mappings;

/// <summary>
/// Определяет преобразования упорядоченной связи шаблона и пункта.
/// Вложенные модели намеренно оставляются для заполнения оптимизированной CQRS-проекцией.
/// </summary>
public sealed class TemplateClauseBlockProfile : Profile
{
    /// <summary>
    /// Инициализирует преобразования связи без загрузки отсутствующих доменных навигаций.
    /// </summary>
    public TemplateClauseBlockProfile()
    {
        CreateMap<TemplateClauseBlock, GetTemplateClauseBlockDto>();

        CreateMap<TemplateClauseBlock, TemplateClauseBlockDetailDto>()
            .ForCtorParam(
                nameof(TemplateClauseBlockDetailDto.Template),
                options => options.MapFrom(_ => (GetTemplateDto?)null))
            .ForCtorParam(
                nameof(TemplateClauseBlockDetailDto.ClauseBlock),
                options => options.MapFrom(_ => (GetClauseBlockDto?)null));

        CreateMap<TemplateClauseBlock, UpdateTemplateClauseBlockDto>();
    }
}
