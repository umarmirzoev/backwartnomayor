using Application.DTOs;
using AutoMapper;
using Domain.Entities;

namespace Application.Mappings;

/// <summary>
/// Определяет явные преобразования доменного профиля юриста в безопасные API-модели.
/// Поля Identity намеренно заполняются внешним CQRS-обработчиком после проверки текущего пользователя.
/// </summary>
public sealed class LawyerProfileProfile : Profile
{
    /// <summary>
    /// Инициализирует преобразования профиля и исключает обратный маппинг, способный обойти доменные инварианты.
    /// </summary>
    public LawyerProfileProfile()
    {
        CreateMap<LawyerProfile, GetLawyerProfileDto>()
            .ForCtorParam(
                nameof(GetLawyerProfileDto.SubscriptionTier),
                options => options.MapFrom(source => source.SubscriptionTier.ToString()));

        CreateMap<LawyerProfile, LawyerProfileDetailDto>()
            .ForCtorParam(
                nameof(LawyerProfileDetailDto.Email),
                options => options.MapFrom(_ => (string?)null))
            .ForCtorParam(
                nameof(LawyerProfileDetailDto.PhoneNumber),
                options => options.MapFrom(_ => (string?)null))
            .ForCtorParam(
                nameof(LawyerProfileDetailDto.SubscriptionTier),
                options => options.MapFrom(source => source.SubscriptionTier.ToString()));

        CreateMap<LawyerProfile, UpdateLawyerProfileDto>()
            .ForCtorParam(
                nameof(UpdateLawyerProfileDto.PhoneNumber),
                options => options.MapFrom(_ => (string?)null));
    }
}
