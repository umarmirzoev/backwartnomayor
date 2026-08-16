using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// Настраивает неизменяемый снимок содержимого договора и его принадлежность черновику.
/// Уникальность номера версии обеспечивает последовательную историю каждого документа.
/// </summary>
public sealed class DocumentVersionConfiguration : IEntityTypeConfiguration<DocumentVersion>
{
    /// <summary>
    /// Применяет ограничения метаданных, внешние ключи и индексы версий документа.
    /// </summary>
    /// <param name="builder">Построитель конфигурации версии документа.</param>
    public void Configure(EntityTypeBuilder<DocumentVersion> builder)
    {
        builder.ToTable(
            "DocumentVersions",
            table => table.HasCheckConstraint(
                "CK_DocumentVersions_VersionNumber_Positive",
                "\"VersionNumber\" > 0"));

        builder.HasKey(version => version.Id);

        builder.Property(version => version.Id)
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(version => version.DraftId)
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(version => version.VersionNumber)
            .IsRequired();

        builder.Property(version => version.ContentStorageKey)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(version => version.ChangeSummary)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(version => version.Source)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(version => version.CreatedByLawyerId)
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(version => version.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasOne<Draft>()
            .WithMany()
            .HasForeignKey(version => version.DraftId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<LawyerProfile>()
            .WithMany()
            .HasForeignKey(version => version.CreatedByLawyerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(version => new { version.DraftId, version.VersionNumber })
            .IsUnique();

        builder.HasIndex(version => version.CreatedByLawyerId);
    }
}
