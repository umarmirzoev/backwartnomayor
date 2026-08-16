using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// Настраивает уведомление об изменении законодательства и метаданные проверенного источника.
/// </summary>
public sealed class LegislationAlertConfiguration : IEntityTypeConfiguration<LegislationAlert>
{
    /// <summary>
    /// Применяет ограничения содержимого, временные типы и индекс хронологической выборки уведомлений.
    /// </summary>
    /// <param name="builder">Построитель конфигурации уведомления.</param>
    public void Configure(EntityTypeBuilder<LegislationAlert> builder)
    {
        builder.ToTable("LegislationAlerts");

        builder.HasKey(alert => alert.Id);

        builder.Property(alert => alert.Id)
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(alert => alert.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(alert => alert.Summary)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(alert => alert.SourceUrl)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(alert => alert.LawChangedAt)
            .HasColumnType("timestamptz")
            .IsRequired(false);

        builder.Property(alert => alert.DetectedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasIndex(alert => alert.DetectedAt);
    }
}
