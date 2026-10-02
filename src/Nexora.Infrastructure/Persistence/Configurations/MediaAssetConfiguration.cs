using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexora.Domain.Entities;
using Nexora.Infrastructure.Identity;

namespace Nexora.Infrastructure.Persistence.Configurations;

public sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.ToTable("assets", table =>
        {
            table.HasCheckConstraint("ck_assets_name", "length(btrim(name)) > 0");
            table.HasCheckConstraint("ck_assets_type", "asset_type IN ('video', 'audio', 'image', 'document')");
            table.HasCheckConstraint("ck_assets_status", "status IN ('active', 'archived')");
        });

        builder.HasKey(x => x.Id).HasName("assets_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.OwnerId).HasColumnName("owner_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasColumnType("text");
        builder.Property(x => x.AssetType).HasColumnName("asset_type").HasMaxLength(30).HasDefaultValue(MediaAsset.TypeVideo).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue(MediaAsset.StatusActive).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamp with time zone");

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_assets_owner");

        builder.HasIndex(x => new { x.OwnerId, x.CreatedAt })
            .HasDatabaseName("ix_assets_owner_created")
            .IsDescending(false, true)
            .HasFilter("deleted_at IS NULL");
        builder.HasIndex(x => x.DeletedAt)
            .HasDatabaseName("ix_assets_deleted_at")
            .HasFilter("deleted_at IS NOT NULL");

        builder.HasMany(x => x.Files)
            .WithOne(x => x.Asset)
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_asset_files_asset");
    }
}
