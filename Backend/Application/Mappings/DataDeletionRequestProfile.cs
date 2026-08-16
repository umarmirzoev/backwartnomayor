using Application.DTOs;
using AutoMapper;
using Domain.Entities;

namespace Application.Mappings;

/// <summary>
/// Определяет преобразования workflow полного удаления данных с текстовыми именами полиморфных типов и состояния.
/// </summary>
public sealed class DataDeletionRequestProfile : Profile
{
    /// <summary>
    /// Инициализирует односторонние read-модели, не позволяя DTO обойти команды завершения или отклонения.
    /// </summary>
    public DataDeletionRequestProfile()
    {
        CreateMap<DataDeletionRequest, GetDataDeletionRequestDto>()
            .ForCtorParam(
                nameof(GetDataDeletionRequestDto.TargetEntityType),
                options => options.MapFrom(source => source.TargetEntityType.ToString()))
            .ForCtorParam(
                nameof(GetDataDeletionRequestDto.Status),
                options => options.MapFrom(source => source.Status.ToString()));

        CreateMap<DataDeletionRequest, DataDeletionRequestDetailDto>()
            .ForCtorParam(
                nameof(DataDeletionRequestDetailDto.RequestedByType),
                options => options.MapFrom(source => source.RequestedByType.ToString()))
            .ForCtorParam(
                nameof(DataDeletionRequestDetailDto.TargetEntityType),
                options => options.MapFrom(source => source.TargetEntityType.ToString()))
            .ForCtorParam(
                nameof(DataDeletionRequestDetailDto.Status),
                options => options.MapFrom(source => source.Status.ToString()));
    }
}
