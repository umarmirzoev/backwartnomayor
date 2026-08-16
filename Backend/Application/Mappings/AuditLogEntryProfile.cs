using Application.DTOs;
using AutoMapper;
using Domain.Entities;

namespace Application.Mappings;

/// <summary>
/// Определяет безопасные преобразования append-only записи аудита,
/// включая явное строковое представление типа инициатора и действия.
/// </summary>
public sealed class AuditLogEntryProfile : Profile
{
    /// <summary>
    /// Инициализирует краткую и детальную read-модели журнала без обратного редактирования.
    /// </summary>
    public AuditLogEntryProfile()
    {
        CreateMap<AuditLogEntry, GetAuditLogEntryDto>()
            .ForCtorParam(
                nameof(GetAuditLogEntryDto.ActorType),
                options => options.MapFrom(source => source.ActorType.ToString()))
            .ForCtorParam(
                nameof(GetAuditLogEntryDto.Action),
                options => options.MapFrom(source => source.Action.ToString()));

        CreateMap<AuditLogEntry, AuditLogEntryDetailDto>()
            .ForCtorParam(
                nameof(AuditLogEntryDetailDto.ActorType),
                options => options.MapFrom(source => source.ActorType.ToString()))
            .ForCtorParam(
                nameof(AuditLogEntryDetailDto.Action),
                options => options.MapFrom(source => source.Action.ToString()));
    }
}
