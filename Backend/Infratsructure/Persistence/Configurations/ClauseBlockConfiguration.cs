using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// Настраивает двуязычный библиотечный пункт договора.
/// Оба текста обязательны, а категория индексируется для поиска и RAG-подбора.
/// </summary>
public sealed class ClauseBlockConfiguration : IEntityTypeConfiguration<ClauseBlock>
{
    /// <summary>
    /// Применяет ограничения текстов, категории, активности и временных меток.
    /// </summary>
    /// <param name="builder">Построитель конфигурации пункта.</param>
    public void Configure(EntityTypeBuilder<ClauseBlock> builder)
    {
        builder.ToTable("ClauseBlocks");

        builder.HasKey(block => block.Id);

        builder.Property(block => block.Id)
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(block => block.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(block => block.ContentTj)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(block => block.ContentRu)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(block => block.Category)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(block => block.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(block => block.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(block => block.UpdatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasIndex(block => block.Category);
    }
}
