using Application.DTOs;
using AutoMapper;
using Domain.Entities;

namespace Application.Mappings;

/// <summary>
/// Определяет преобразования append-only записи обращения к ИИ с явным строковым типом операции.
/// </summary>
public sealed class AiUsageRecordProfile : Profile
{
    /// <summary>
    /// Инициализирует краткую и детальную read-модели без возможности изменения истории квоты.
    /// </summary>
    public AiUsageRecordProfile()
    {
        CreateMap<AiUsageRecord, GetAiUsageRecordDto>()
            .ForCtorParam(
                nameof(GetAiUsageRecordDto.RequestType),
                options => options.MapFrom(source => source.RequestType.ToString()));

        CreateMap<AiUsageRecord, AiUsageRecordDetailDto>()
            .ForCtorParam(
                nameof(AiUsageRecordDetailDto.RequestType),
                options => options.MapFrom(source => source.RequestType.ToString()));
    }
}
