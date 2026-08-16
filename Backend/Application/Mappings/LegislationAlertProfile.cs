using Application.DTOs;
using AutoMapper;
using Domain.Entities;

namespace Application.Mappings;

/// <summary>
/// Определяет преобразования append-only уведомления законодательства в краткую и детальную read-модели.
/// </summary>
public sealed class LegislationAlertProfile : Profile
{
    /// <summary>
    /// Инициализирует односторонние преобразования уведомления без разрешения редактировать результат мониторинга.
    /// </summary>
    public LegislationAlertProfile()
    {
        CreateMap<LegislationAlert, GetLegislationAlertDto>();
        CreateMap<LegislationAlert, LegislationAlertDetailDto>();
    }
}
