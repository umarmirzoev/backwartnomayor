using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// Настраивает упорядоченную связь шаблона с переиспользуемым блоком пункта.
/// Уникальные ограничения защищают шаблон от повторного блока и двух элементов с одной позицией.
/// </summary>
public sealed class TemplateClauseBlockConfiguration : IEntityTypeConfiguration<TemplateClauseBlock>
{
    /// <summary>
    /// Применяет ключи, каскадные связи и ограничения порядка элемента внутри шаблона.
    /// </summary>
    /// <param name="builder">Построитель конфигурации связи шаблона и блока.</param>
    public void Configure(EntityTypeBuilder<TemplateClauseBlock> builder)
    {
        builder.ToTable(
            "TemplateClauseBlocks",
            table => table.HasCheckConstraint(
                "CK_TemplateClauseBlocks_Order_NonNegative",
                "\"Order\" >= 0"));

        builder.HasKey(link => link.Id);

        builder.Property(link => link.Id)
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(link => link.TemplateId)
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(link => link.ClauseBlockId)
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(link => link.IsDefault)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(link => link.Order)
            .IsRequired();

        builder.HasOne<Template>()
            .WithMany()
            .HasForeignKey(link => link.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ClauseBlock>()
            .WithMany()
            .HasForeignKey(link => link.ClauseBlockId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(link => new { link.TemplateId, link.ClauseBlockId })
            .IsUnique();

        builder.HasIndex(link => new { link.TemplateId, link.Order })
            .IsUnique();
    }
}
