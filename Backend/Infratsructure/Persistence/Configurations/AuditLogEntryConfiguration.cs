using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// Настраивает неизменяемую запись аудита с полиморфными ссылками без внешних ключей.
/// Такой дизайн сохраняет доказательную историю после удаления исходных доменных данных.
/// </summary>
public sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    /// <summary>
    /// Применяет строковое хранение перечислений, JSONB-метаданные и индексы расследования событий.
    /// </summary>
    /// <param name="builder">Построитель конфигурации записи аудита.</param>
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable(
            "AuditLogEntries",
            table => table.HasCheckConstraint(
                "CK_AuditLogEntries_Actor",
                "(\"ActorType\" = 'System' AND \"ActorId\" IS NULL) OR (\"ActorType\" <> 'System' AND \"ActorId\" IS NOT NULL)"));

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Id)
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(entry => entry.ActorType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(entry => entry.ActorId)
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(entry => entry.Action)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(entry => entry.EntityType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(entry => entry.EntityId)
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(entry => entry.Metadata)
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Property(entry => entry.OccurredAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasIndex(entry => new { entry.EntityType, entry.EntityId });
        builder.HasIndex(entry => entry.OccurredAt);
        builder.HasIndex(entry => new { entry.ActorType, entry.ActorId });
    }
}
