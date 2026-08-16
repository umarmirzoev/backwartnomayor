using Application.DTOs;
using AutoMapper;
using Domain.Entities;

namespace Application.Mappings;

/// <summary>
/// Определяет преобразования главного агрегата черновика в read-модели.
/// Текст версии и название шаблона заполняются обработчиком из разрешённых внешних источников.
/// </summary>
public sealed class DraftProfile : Profile
{
    /// <summary>
    /// Инициализирует преобразования черновика и исключает обратный маппинг,
    /// поскольку ручная правка должна создать новую immutable-версию через агрегат.
    /// </summary>
    public DraftProfile()
    {
        CreateMap<Draft, GetDraftDto>()
            .ForCtorParam(
                nameof(GetDraftDto.TemplateName),
                options => options.MapFrom(_ => (string?)null))
            .ForCtorParam(
                nameof(GetDraftDto.Status),
                options => options.MapFrom(source => source.Status.ToString()));

        CreateMap<Draft, DraftDetailDto>()
            .ForCtorParam(
                nameof(DraftDetailDto.Status),
                options => options.MapFrom(source => source.Status.ToString()))
            .ForCtorParam(
                nameof(DraftDetailDto.CurrentVersion),
                options => options.MapFrom(_ => (GetDocumentVersionDto?)null))
            .ForCtorParam(
                nameof(DraftDetailDto.CurrentContent),
                options => options.MapFrom(_ => (string?)null));
    }
}
