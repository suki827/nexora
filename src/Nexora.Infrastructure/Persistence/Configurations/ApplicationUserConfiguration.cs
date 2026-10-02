using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexora.Infrastructure.Identity;

namespace Nexora.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("users");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.UserName).HasColumnName("user_name").HasMaxLength(256);
        builder.Property(x => x.NormalizedUserName).HasColumnName("normalized_user_name").HasMaxLength(256);
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(256);
        builder.Property(x => x.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(256);
        builder.Property(x => x.EmailConfirmed).HasColumnName("email_confirmed").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.PasswordHash).HasColumnName("password_hash").HasColumnType("text");
        builder.Property(x => x.SecurityStamp).HasColumnName("security_stamp").HasColumnType("text");
        builder.Property(x => x.ConcurrencyStamp).HasColumnName("concurrency_stamp").HasColumnType("text");
        builder.Property(x => x.PhoneNumber).HasColumnName("phone_number").HasColumnType("text");
        builder.Property(x => x.PhoneNumberConfirmed).HasColumnName("phone_number_confirmed").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.TwoFactorEnabled).HasColumnName("two_factor_enabled").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.LockoutEnd).HasColumnName("lockout_end").HasColumnType("timestamp with time zone");
        builder.Property(x => x.LockoutEnabled).HasColumnName("lockout_enabled").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.AccessFailedCount).HasColumnName("access_failed_count").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(100);
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("active").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()").IsRequired();

        builder.HasIndex(x => x.NormalizedUserName)
            .HasDatabaseName("ux_users_normalized_user_name")
            .IsUnique();
        builder.HasIndex(x => x.NormalizedEmail)
            .HasDatabaseName("ux_users_normalized_email")
            .IsUnique();
    }
}
