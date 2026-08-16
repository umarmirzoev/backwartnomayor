using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// Настраивает дело клиента, денормализованного владельца и индексы tenant-изоляции.
/// Соответствие владельца дела владельцу клиента проверяется Application-слоем, поскольку CHECK
/// PostgreSQL не может безопасно ссылаться на другую таблицу без триггера.
/// </summary>
public sealed class CaseConfiguration : IEntityTypeConfiguration<Case>
{
    /// <summary>
    /// Применяет ограничения дела, строковое хранение статуса и запрещающие каскад связи.
    /// </summary>
    /// <param name="builder">Построитель конфигурации дела.</param>
    public void Configure(EntityTypeBuilder<Case> builder)
    {
        builder.ToTable("Cases");

        builder.HasKey(caseItem => caseItem.Id);

        builder.Property(caseItem => caseItem.Id)
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(caseItem => caseItem.ClientId)
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(caseItem => caseItem.LawyerId)
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(caseItem => caseItem.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(caseItem => caseItem.Description)
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(caseItem => caseItem.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(CaseStatus.Open)
            .IsRequired();

        builder.Property(caseItem => caseItem.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(caseItem => caseItem.ClosedAt)
            .HasColumnType("timestamptz")
            .IsRequired(false);

        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(caseItem => caseItem.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<LawyerProfile>()
            .WithMany()
            .HasForeignKey(caseItem => caseItem.LawyerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(caseItem => caseItem.ClientId);
        builder.HasIndex(caseItem => caseItem.LawyerId);
        builder.HasIndex(caseItem => new { caseItem.LawyerId, caseItem.Status });
    }
}
