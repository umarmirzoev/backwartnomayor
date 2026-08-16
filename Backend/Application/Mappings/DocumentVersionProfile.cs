using Application.DTOs;
using AutoMapper;
using Domain.Entities;

namespace Application.Mappings;

/// <summary>
/// Определяет безопасные преобразования immutable-версии документа без раскрытия ключа объектного хранилища.
/// </summary>
public sealed class DocumentVersionProfile : Profile
{
    /// <summary>
    /// Инициализирует read-модели версии; полный текст оставляется для загрузки и расшифровки обработчиком.
    /// </summary>
    public DocumentVersionProfile()
    {
        CreateMap<DocumentVersion, GetDocumentVersionDto>()
            .ForCtorParam(
                nameof(GetDocumentVersionDto.Source),
                options => options.MapFrom(source => source.Source.ToString()));

        CreateMap<DocumentVersion, DocumentVersionDetailDto>()
            .ForCtorParam(
                nameof(DocumentVersionDetailDto.Content),
                options => options.MapFrom(_ => (string?)null))
            .ForCtorParam(
                nameof(DocumentVersionDetailDto.Source),
                options => options.MapFrom(source => source.Source.ToString()));
    }
}
