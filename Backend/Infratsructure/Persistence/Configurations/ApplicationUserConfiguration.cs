using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// Настраивает ограничения инфраструктурной учётной записи ASP.NET Core Identity.
/// Конфигурация дополняет стандартную Identity-модель уникальностью нормализованного email
/// и ограничениями полей, перенесённых из первоначальной сущности юриста.
/// </summary>
public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    /// <summary>
    /// Применяет правила хранения учётной записи и не изменяет стандартное имя таблицы Identity.
    /// </summary>
    /// <param name="builder">Построитель конфигурации пользователя.</param>
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(user => user.Id)
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(user => user.UserName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(user => user.NormalizedUserName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(user => user.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(user => user.NormalizedEmail)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(user => user.PasswordHash)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(user => user.PhoneNumber)
            .HasMaxLength(30)
            .IsRequired(false);

        builder.Property(user => user.RefreshTokenHash)
            .HasMaxLength(64)
            .IsUnicode(false)
            .IsRequired(false);

        builder.Property(user => user.RefreshTokenExpiresAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(user => user.RefreshTokenAuthenticatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.HasIndex(user => user.NormalizedEmail)
            .HasDatabaseName("EmailIndex")
            .IsUnique();
    }
}
