using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// Настраивает формальный запрос на удаление данных с полиморфными идентификаторами без внешних ключей.
/// Отсутствие связей необходимо, чтобы запись запроса переживала уничтожение целевых данных.
/// </summary>
public sealed class DataDeletionRequestConfiguration : IEntityTypeConfiguration<DataDeletionRequest>
{
    /// <summary>
    /// Применяет строковое хранение типов, согласованность финального статуса и поисковые индексы.
    /// </summary>
    /// <param name="builder">Построитель конфигурации запроса на удаление.</param>
    public void Configure(EntityTypeBuilder<DataDeletionRequest> builder)
    {
        builder.ToTable(
            "DataDeletionRequests",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_DataDeletionRequests_CompletionState",
                    "(\"Status\" = 'Completed' AND \"CompletedAt\" IS NOT NULL) OR (\"Status\" <> 'Completed' AND \"CompletedAt\" IS NULL)");
                table.HasCheckConstraint(
                    "CK_DataDeletionRequests_CompletionTime",
                    "\"CompletedAt\" IS NULL OR \"CompletedAt\" >= \"RequestedAt\"");
            });

        builder.HasKey(request => request.Id);

        builder.Property(request => request.Id)
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(request => request.RequestedByType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(request => request.RequestedById)
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(request => request.TargetEntityType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(request => request.TargetEntityId)
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(request => request.RequestedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(request => request.CompletedAt)
            .HasColumnType("timestamptz")
            .IsRequired(false);

        builder.Property(request => request.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(DataDeletionStatus.Pending)
            .IsRequired();

        builder.HasIndex(request => new { request.TargetEntityType, request.TargetEntityId });
        builder.HasIndex(request => request.Status);
    }
}
