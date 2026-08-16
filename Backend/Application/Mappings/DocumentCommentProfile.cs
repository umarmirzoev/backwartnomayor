using Application.DTOs;
using AutoMapper;
using Domain.Entities;

namespace Application.Mappings;

/// <summary>
/// Определяет преобразования Post-MVP комментария документа с текстовым типом автора.
/// </summary>
public sealed class DocumentCommentProfile : Profile
{
    /// <summary>
    /// Инициализирует краткую и детальную read-модели комментария без обратного редактирования текста.
    /// </summary>
    public DocumentCommentProfile()
    {
        CreateMap<DocumentComment, GetDocumentCommentDto>()
            .ForCtorParam(
                nameof(GetDocumentCommentDto.AuthorType),
                options => options.MapFrom(source => source.AuthorType.ToString()));

        CreateMap<DocumentComment, DocumentCommentDetailDto>()
            .ForCtorParam(
                nameof(DocumentCommentDetailDto.AuthorType),
                options => options.MapFrom(source => source.AuthorType.ToString()));
    }
}
