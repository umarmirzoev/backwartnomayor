using Application.DTOs;
using AutoMapper;
using Domain.Entities;

namespace Application.Mappings;

/// <summary>
/// Определяет преобразования шаблона договора, включая строковое представление языка
/// и доменную константу запрета нотариальных типов документов.
/// </summary>
public sealed class TemplateProfile : Profile
{
    /// <summary>
    /// Инициализирует явные преобразования шаблона без обратного присваивания закрытым свойствам сущности.
    /// </summary>
    public TemplateProfile()
    {
        CreateMap<Template, GetTemplateDto>()
            .ForCtorParam(
                nameof(GetTemplateDto.Language),
                options => options.MapFrom(source => source.Language.ToString()));

        CreateMap<Template, TemplateDetailDto>()
            .ForCtorParam(
                nameof(TemplateDetailDto.Language),
                options => options.MapFrom(source => source.Language.ToString()))
            .ForCtorParam(
                nameof(TemplateDetailDto.RequiresNotary),
                options => options.MapFrom(_ => Template.RequiresNotary));

        CreateMap<Template, UpdateTemplateDto>();
    }
}
