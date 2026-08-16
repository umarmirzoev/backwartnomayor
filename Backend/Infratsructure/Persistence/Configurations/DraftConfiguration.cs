using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// Настраивает агрегат черновика, его жизненный цикл и указатель на текущую неизменяемую версию.
/// Запрещающие каскады сохраняют юридически значимую историю документов.
/// </summary>
public sealed class DraftConfiguration : IEntityTypeConfiguration<Draft>
{
    /// <summary>
    /// Применяет ограничения полей, строковое хранение статуса, связи и поисковые индексы черновика.
    /// </summary>
    /// <param name="builder">Построитель конфигурации черновика.</param>
    public void Configure(EntityTypeBuilder<Draft> builder)
    {
        builder.ToTable("Drafts");

        builder.HasKey(draft => draft.Id);

        builder.Property(draft => draft.Id)
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(draft => draft.CaseId)
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(draft => draft.TemplateId)
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(draft => draft.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(DocumentStatus.Draft)
            .IsRequired();

        builder.Property(draft => draft.CurrentVersionId)
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(draft => draft.ResponsibilityConfirmedAt)
            .HasColumnType("timestamptz")
            .IsRequired(false);

        builder.Property(draft => draft.DueRespondByDate)
            .HasColumnType("timestamptz")
            .IsRequired(false);

        builder.Property(draft => draft.ArchivedAt)
            .HasColumnType("timestamptz")
            .IsRequired(false);

        builder.Property(draft => draft.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(draft => draft.UpdatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasOne<Case>()
            .WithMany()
            .HasForeignKey(draft => draft.CaseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Template>()
            .WithMany()
            .HasForeignKey(draft => draft.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<DocumentVersion>()
            .WithMany()
            .HasForeignKey(draft => draft.CurrentVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(draft => draft.CaseId);
        builder.HasIndex(draft => draft.TemplateId);
        builder.HasIndex(draft => draft.Status);
        builder.HasIndex(draft => new { draft.CaseId, draft.Status });
    }
}
