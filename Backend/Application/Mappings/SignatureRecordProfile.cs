using Application.DTOs;
using AutoMapper;
using Domain.Entities;

namespace Application.Mappings;

/// <summary>
/// Определяет преобразования Post-MVP юридически значимой подписи с явными строковыми enum-контрактами.
/// </summary>
public sealed class SignatureRecordProfile : Profile
{
    /// <summary>
    /// Инициализирует краткую и детальную read-модели append-only записи подписи.
    /// </summary>
    public SignatureRecordProfile()
    {
        CreateMap<SignatureRecord, GetSignatureRecordDto>()
            .ForCtorParam(
                nameof(GetSignatureRecordDto.SignerType),
                options => options.MapFrom(source => source.SignerType.ToString()))
            .ForCtorParam(
                nameof(GetSignatureRecordDto.Method),
                options => options.MapFrom(source => source.Method.ToString()));

        CreateMap<SignatureRecord, SignatureRecordDetailDto>()
            .ForCtorParam(
                nameof(SignatureRecordDetailDto.SignerType),
                options => options.MapFrom(source => source.SignerType.ToString()))
            .ForCtorParam(
                nameof(SignatureRecordDetailDto.Method),
                options => options.MapFrom(source => source.Method.ToString()));
    }
}
