using Domain.Entities;
using Domain.Enums;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// Настраивает доменный профиль юриста и его однозначную внешнюю связь с ASP.NET Core Identity.
/// Бизнес-профиль не хранит пароль, email или телефон, предотвращая рассинхронизацию учётных данных.
/// </summary>
public sealed class LawyerProfileConfiguration : IEntityTypeConfiguration<LawyerProfile>
{
    /// <summary>
    /// Применяет ограничения полей, индекс внешнего пользователя и запрет каскадного удаления профиля.
    /// </summary>
    /// <param name="builder">Построитель конфигурации профиля юриста.</param>
    public void Configure(EntityTypeBuilder<LawyerProfile> builder)
    {
        builder.ToTable("LawyerProfiles");

        builder.HasKey(profile => profile.Id);

        builder.Property(profile => profile.Id)
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(profile => profile.UserId)
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(profile => profile.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(profile => profile.LawFirmName)
            .HasMaxLength(300)
            .IsRequired(false);

        builder.Property(profile => profile.SubscriptionTier)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(SubscriptionTier.Free)
            .IsRequired();

        builder.Property(profile => profile.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(profile => profile.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<LawyerProfile>(profile => profile.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(profile => profile.UserId)
            .IsUnique();
    }
}
