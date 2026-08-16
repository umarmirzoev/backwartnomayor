using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// Настраивает связь законодательного уведомления с делом и индивидуальное состояние прочтения.
/// </summary>
public sealed class CaseLegislationAlertConfiguration : IEntityTypeConfiguration<CaseLegislationAlert>
{
    /// <summary>
    /// Применяет каскадные связи, уникальность пары и согласованность отметки прочтения.
    /// </summary>
    /// <param name="builder">Построитель конфигурации связи уведомления с делом.</param>
    public void Configure(EntityTypeBuilder<CaseLegislationAlert> builder)
    {
        builder.ToTable(
            "CaseLegislationAlerts",
            table => table.HasCheckConstraint(
                "CK_CaseLegislationAlerts_ReadState",
                "(\"IsRead\" = FALSE AND \"ReadAt\" IS NULL) OR (\"IsRead\" = TRUE AND \"ReadAt\" IS NOT NULL)"));

        builder.HasKey(link => link.Id);

        builder.Property(link => link.Id)
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(link => link.CaseId)
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(link => link.LegislationAlertId)
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(link => link.IsRead)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(link => link.ReadAt)
            .HasColumnType("timestamptz")
            .IsRequired(false);

        builder.HasOne<Case>()
            .WithMany()
            .HasForeignKey(link => link.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<LegislationAlert>()
            .WithMany()
            .HasForeignKey(link => link.LegislationAlertId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(link => new { link.CaseId, link.LegislationAlertId })
            .IsUnique();

        builder.HasIndex(link => new { link.CaseId, link.IsRead });
    }
}
