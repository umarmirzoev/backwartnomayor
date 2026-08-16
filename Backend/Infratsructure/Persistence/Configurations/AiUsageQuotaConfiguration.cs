using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// Настраивает персистентный снимок периодической квоты ИИ-запросов юриста.
/// Ограничения базы дублируют критические инварианты лимита для защиты от обхода доменной модели.
/// </summary>
public sealed class AiUsageQuotaConfiguration : IEntityTypeConfiguration<AiUsageQuota>
{
    /// <summary>
    /// Применяет ограничения периода, тарифа, счётчиков и уникальность квоты юриста за период.
    /// </summary>
    /// <param name="builder">Построитель конфигурации квоты ИИ.</param>
    public void Configure(EntityTypeBuilder<AiUsageQuota> builder)
    {
        builder.ToTable(
            "AiUsageQuotas",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_AiUsageQuotas_Period",
                    "\"PeriodEnd\" > \"PeriodStart\"");
                table.HasCheckConstraint(
                    "CK_AiUsageQuotas_RequestsUsed_NonNegative",
                    "\"RequestsUsed\" >= 0");
                table.HasCheckConstraint(
                    "CK_AiUsageQuotas_TierLimit",
                    "(\"Tier\" = 'Free' AND \"RequestsLimit\" IS NOT NULL AND \"RequestsLimit\" > 0) OR (\"Tier\" = 'Paid' AND \"RequestsLimit\" IS NULL)");
                table.HasCheckConstraint(
                    "CK_AiUsageQuotas_RequestsWithinLimit",
                    "\"RequestsLimit\" IS NULL OR \"RequestsUsed\" <= \"RequestsLimit\"");
            });

        builder.HasKey(quota => quota.Id);

        builder.Property(quota => quota.Id)
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(quota => quota.LawyerId)
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(quota => quota.PeriodStart)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(quota => quota.PeriodEnd)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(quota => quota.Tier)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(SubscriptionTier.Free)
            .IsRequired();

        builder.Property(quota => quota.RequestsUsed)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(quota => quota.RequestsLimit)
            .IsRequired(false);

        builder.HasOne<LawyerProfile>()
            .WithMany()
            .HasForeignKey(quota => quota.LawyerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(quota => new { quota.LawyerId, quota.PeriodStart, quota.PeriodEnd })
            .IsUnique();
    }
}
