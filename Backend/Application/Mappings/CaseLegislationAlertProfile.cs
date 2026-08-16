using Application.DTOs;
using AutoMapper;
using Domain.Entities;

namespace Application.Mappings;

/// <summary>
/// Определяет преобразования состояния уведомления по делу.
/// Вложенные дело и уведомление заполняются одной CQRS-проекцией для предотвращения N+1-запросов.
/// </summary>
public sealed class CaseLegislationAlertProfile : Profile
{
    /// <summary>
    /// Инициализирует read-модели связи и оставляет составные поля для обработчика.
    /// </summary>
    public CaseLegislationAlertProfile()
    {
        CreateMap<CaseLegislationAlert, GetCaseLegislationAlertDto>();

        CreateMap<CaseLegislationAlert, CaseLegislationAlertDetailDto>()
            .ForCtorParam(
                nameof(CaseLegislationAlertDetailDto.Case),
                options => options.MapFrom(_ => (GetCaseDto?)null))
            .ForCtorParam(
                nameof(CaseLegislationAlertDetailDto.Alert),
                options => options.MapFrom(_ => (GetLegislationAlertDto?)null));
    }
}
