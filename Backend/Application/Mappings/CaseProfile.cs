using Application.DTOs;
using AutoMapper;
using CaseEntity = Domain.Entities.Case;

namespace Application.Mappings;

/// <summary>
/// Определяет преобразования дела в клиентские модели с текстовым представлением состояния.
/// Сводка документов заполняется обработчиком отдельным агрегирующим запросом.
/// </summary>
public sealed class CaseProfile : Profile
{
    /// <summary>
    /// Инициализирует односторонние преобразования дела и безопасную модель редактирования сведений.
    /// </summary>
    public CaseProfile()
    {
        CreateMap<CaseEntity, GetCaseDto>()
            .ForCtorParam(
                nameof(GetCaseDto.Status),
                options => options.MapFrom(source => source.Status.ToString()));

        CreateMap<CaseEntity, CaseDetailDto>()
            .ForCtorParam(
                nameof(CaseDetailDto.Status),
                options => options.MapFrom(source => source.Status.ToString()))
            .ForCtorParam(
                nameof(CaseDetailDto.DocumentCount),
                options => options.MapFrom(_ => 0));

        CreateMap<CaseEntity, UpdateCaseDto>();
    }
}
