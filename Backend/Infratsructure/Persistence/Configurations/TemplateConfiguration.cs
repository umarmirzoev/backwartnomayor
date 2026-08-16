using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// Настраивает курируемый шаблон договора и его двуязычный режим.
/// Константа <c>RequiresNotary</c> намеренно не маппится: нотариальные документы отсутствуют в домене.
/// </summary>
public sealed class TemplateConfiguration : IEntityTypeConfiguration<Template>
{
    /// <summary>
    /// Применяет ограничения полей, строковую конвертацию языка и индекс публикации.
    /// </summary>
    /// <param name="builder">Построитель конфигурации шаблона.</param>
    public void Configure(EntityTypeBuilder<Template> builder)
    {
        builder.ToTable("Templates");

        builder.HasKey(template => template.Id);

        builder.Property(template => template.Id)
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(template => template.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(template => template.Description)
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(template => template.Language)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(template => template.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(template => template.MaintainedByRef)
            .HasMaxLength(300)
            .IsRequired(false);

        builder.Property(template => template.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(template => template.UpdatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasIndex(template => template.IsActive);
    }
}
