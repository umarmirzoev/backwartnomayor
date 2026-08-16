using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// Настраивает неизменяемую запись фактического обращения к ИИ и её аудиторские связи.
/// Удаление черновика обнуляет необязательную ссылку, но не уничтожает историю расходования квоты.
/// </summary>
public sealed class AiUsageRecordConfiguration : IEntityTypeConfiguration<AiUsageRecord>
{
    /// <summary>
    /// Применяет ограничения запроса, внешние ключи и индексы аналитики использования ИИ.
    /// </summary>
    /// <param name="builder">Построитель конфигурации записи использования ИИ.</param>
    public void Configure(EntityTypeBuilder<AiUsageRecord> builder)
    {
        builder.ToTable(
            "AiUsageRecords",
            table => table.HasCheckConstraint(
                "CK_AiUsageRecords_DraftReference",
                "\"RequestType\" IN ('GenerateDraft', 'RegenerateDraft') OR (\"RequestType\" = 'ReviewIncomingDocument' AND \"DraftId\" IS NULL)"));

        builder.HasKey(record => record.Id);

        builder.Property(record => record.Id)
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(record => record.LawyerId)
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(record => record.AiUsageQuotaId)
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(record => record.RequestType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(record => record.DraftId)
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(record => record.Succeeded)
            .IsRequired();

        builder.Property(record => record.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasOne<LawyerProfile>()
            .WithMany()
            .HasForeignKey(record => record.LawyerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AiUsageQuota>()
            .WithMany()
            .HasForeignKey(record => record.AiUsageQuotaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Draft>()
            .WithMany()
            .HasForeignKey(record => record.DraftId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(record => record.LawyerId);
        builder.HasIndex(record => record.AiUsageQuotaId);
        builder.HasIndex(record => record.DraftId);
        builder.HasIndex(record => new { record.LawyerId, record.CreatedAt });
    }
}
