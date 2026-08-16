using Application.DTOs;
using AutoMapper;
using Domain.Entities;

namespace Application.Mappings;

/// <summary>
/// Определяет преобразования квоты ИИ и вычисляет остаток через доменный метод,
/// сохраняя единое правило для бесплатного и безлимитного тарифов.
/// </summary>
public sealed class AiUsageQuotaProfile : Profile
{
    /// <summary>
    /// Инициализирует read-модели квоты с вычисляемым остатком и строковым именем тарифа.
    /// </summary>
    public AiUsageQuotaProfile()
    {
        CreateMap<AiUsageQuota, GetAiUsageQuotaDto>()
            .ForCtorParam(
                nameof(GetAiUsageQuotaDto.RemainingRequests),
                options => options.MapFrom(source => source.GetRemainingRequests()));

        CreateMap<AiUsageQuota, AiUsageQuotaDetailDto>()
            .ForCtorParam(
                nameof(AiUsageQuotaDetailDto.Tier),
                options => options.MapFrom(source => source.Tier.ToString()))
            .ForCtorParam(
                nameof(AiUsageQuotaDetailDto.RemainingRequests),
                options => options.MapFrom(source => source.GetRemainingRequests()));
    }
}
