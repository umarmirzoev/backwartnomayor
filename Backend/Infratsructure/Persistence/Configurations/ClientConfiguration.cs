using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// Настраивает карточку клиента, включая XOR-ограничение имени, PII-поля и безопасную связь с юристом.
/// Клиент анонимизируется обновлением полей, поэтому каскадное удаление связанных дел запрещено.
/// </summary>
public sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    /// <summary>
    /// Применяет ограничения строк, индексы владельца и CHECK-правило взаимоисключающих имён.
    /// </summary>
    /// <param name="builder">Построитель конфигурации клиента.</param>
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable(
            "Clients",
            table => table.HasCheckConstraint(
                "CK_Clients_ExactlyOneName",
                "\"DeletedAt\" IS NOT NULL OR ((\"FullName\" IS NOT NULL) <> (\"CompanyName\" IS NOT NULL))"));

        builder.HasKey(client => client.Id);

        builder.Property(client => client.Id)
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(client => client.LawyerId)
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(client => client.FullName)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(client => client.CompanyName)
            .HasMaxLength(300)
            .IsRequired(false);

        builder.Property(client => client.ContactPhone)
            .HasMaxLength(30)
            .IsRequired(false);

        builder.Property(client => client.ContactEmail)
            .HasMaxLength(256)
            .IsRequired(false);

        builder.Property(client => client.Notes)
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(client => client.DeletedAt)
            .HasColumnType("timestamptz")
            .IsRequired(false);

        builder.Property(client => client.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasOne<LawyerProfile>()
            .WithMany()
            .HasForeignKey(client => client.LawyerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(client => client.LawyerId);
        builder.HasIndex(client => new { client.LawyerId, client.DeletedAt });
    }
}
