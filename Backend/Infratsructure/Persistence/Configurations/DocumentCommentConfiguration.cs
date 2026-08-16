using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// Настраивает Post-MVP-комментарий к неизменяемой версии документа.
/// Автор хранится полиморфно, а необязательная ссылка на блок безопасно обнуляется при его удалении.
/// </summary>
public sealed class DocumentCommentConfiguration : IEntityTypeConfiguration<DocumentComment>
{
    /// <summary>
    /// Применяет ограничения текста, временные поля, связи и индексы обсуждения версии.
    /// </summary>
    /// <param name="builder">Построитель конфигурации комментария.</param>
    public void Configure(EntityTypeBuilder<DocumentComment> builder)
    {
        builder.ToTable(
            "DocumentComments",
            table => table.HasCheckConstraint(
                "CK_DocumentComments_ResolutionTime",
                "\"ResolvedAt\" IS NULL OR \"ResolvedAt\" >= \"CreatedAt\""));

        builder.HasKey(comment => comment.Id);

        builder.Property(comment => comment.Id)
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(comment => comment.DocumentVersionId)
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(comment => comment.AuthorType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(comment => comment.AuthorId)
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(comment => comment.ClauseBlockReference)
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(comment => comment.Text)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(comment => comment.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(comment => comment.ResolvedAt)
            .HasColumnType("timestamptz")
            .IsRequired(false);

        builder.HasOne<DocumentVersion>()
            .WithMany()
            .HasForeignKey(comment => comment.DocumentVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ClauseBlock>()
            .WithMany()
            .HasForeignKey(comment => comment.ClauseBlockReference)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(comment => comment.DocumentVersionId);
        builder.HasIndex(comment => comment.ClauseBlockReference);
    }
}
