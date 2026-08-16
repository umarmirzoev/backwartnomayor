using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// Настраивает Post-MVP-запись электронной подписи конкретной неизменяемой версии.
/// Запрещающие каскады защищают юридически значимые доказательства согласия от неявного удаления.
/// </summary>
public sealed class SignatureRecordConfiguration : IEntityTypeConfiguration<SignatureRecord>
{
    /// <summary>
    /// Применяет ограничения реквизитов подписи, внешние ключи и защиту от повторной подписи стороны.
    /// </summary>
    /// <param name="builder">Построитель конфигурации записи подписи.</param>
    public void Configure(EntityTypeBuilder<SignatureRecord> builder)
    {
        builder.ToTable("SignatureRecords");

        builder.HasKey(signature => signature.Id);

        builder.Property(signature => signature.Id)
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(signature => signature.DraftId)
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(signature => signature.DocumentVersionId)
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(signature => signature.SignerType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(signature => signature.SignerId)
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(signature => signature.Method)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(signature => signature.ConsentAgreementVersion)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(signature => signature.SignedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(signature => signature.IpAddress)
            .HasMaxLength(45)
            .IsRequired();

        builder.HasOne<Draft>()
            .WithMany()
            .HasForeignKey(signature => signature.DraftId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<DocumentVersion>()
            .WithMany()
            .HasForeignKey(signature => signature.DocumentVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(signature => signature.DraftId);

        builder.HasIndex(signature => new
        {
            signature.DraftId,
            signature.SignerType,
            signature.SignerId
        })
            .IsUnique();
    }
}
