using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexora.Domain.Entities;

namespace Nexora.Infrastructure.Persistence.Configurations;

public sealed class AssetFileConfiguration : IEntityTypeConfiguration<AssetFile>
{
    public void Configure(EntityTypeBuilder<AssetFile> builder)
    {
        builder.ToTable("asset_files", table =>
        {
            table.HasCheckConstraint("ck_asset_files_deleted", "status <> 'deleted' OR deleted_at IS NOT NULL");
            table.HasCheckConstraint("ck_asset_files_hash", "content_hash IS NULL OR content_hash ~ '^[0-9a-f]{64}$'");
            table.HasCheckConstraint("ck_asset_files_key", "length(btrim(storage_key)) > 0");
            table.HasCheckConstraint("ck_asset_files_name", "length(btrim(original_name)) > 0");
            table.HasCheckConstraint("ck_asset_files_provider", "storage_provider IN ('local', 'swift', 's3')");
            table.HasCheckConstraint("ck_asset_files_role", "file_role IN ('original', 'subtitle', 'reference_script', 'attachment')");
            table.HasCheckConstraint("ck_asset_files_size", "size_bytes IS NULL OR size_bytes >= 0");
            table.HasCheckConstraint("ck_asset_files_status", "status IN ('pending', 'uploading', 'ready', 'failed', 'deleting', 'deleted')");
            table.HasCheckConstraint("ck_asset_files_storage", "(storage_provider = 'local' AND storage_bucket IS NULL) OR (storage_provider IN ('swift', 's3') AND storage_bucket IS NOT NULL AND length(btrim(storage_bucket)) > 0)");
        });

        builder.HasKey(x => x.Id).HasName("asset_files_pkey");
        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.AssetId).HasColumnName("asset_id").IsRequired();
        builder.Property(x => x.FileRole).HasColumnName("file_role").HasMaxLength(30).IsRequired();
        builder.Property(x => x.OriginalName).HasColumnName("original_name").HasMaxLength(255).IsRequired();
        builder.Property(x => x.StorageProvider).HasColumnName("storage_provider").HasMaxLength(30).HasDefaultValue(AssetFile.ProviderLocal).IsRequired();
        builder.Property(x => x.StorageBucket).HasColumnName("storage_bucket").HasMaxLength(255);
        builder.Property(x => x.StorageKey).HasColumnName("storage_key").HasColumnType("text").IsRequired();
        builder.Property(x => x.ContentType).HasColumnName("content_type").HasMaxLength(150);
        builder.Property(x => x.SizeBytes).HasColumnName("size_bytes");
        builder.Property(x => x.ContentHash).HasColumnName("content_hash").HasMaxLength(64);
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue(AssetFile.StatusPending).IsRequired();
        builder.Property(x => x.ErrorCode).HasColumnName("error_code").HasMaxLength(100);
        builder.Property(x => x.ErrorMessage).HasColumnName("error_message").HasColumnType("text");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
        builder.Property(x => x.UploadedAt).HasColumnName("uploaded_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamp with time zone");

        builder.HasIndex(x => new { x.AssetId, x.CreatedAt })
            .HasDatabaseName("ix_asset_files_asset")
            .IsDescending(false, true)
            .HasFilter("deleted_at IS NULL");
        builder.HasIndex(x => x.DeletedAt)
            .HasDatabaseName("ix_asset_files_cleanup")
            .HasFilter("status = 'deleting'");
        builder.HasIndex(x => new { x.StorageProvider, x.StorageBucket, x.StorageKey })
            .HasDatabaseName("ux_asset_files_cloud_storage")
            .IsUnique()
            .HasFilter("storage_provider IN ('swift', 's3')");
        builder.HasIndex(x => x.StorageKey)
            .HasDatabaseName("ux_asset_files_local_storage")
            .IsUnique()
            .HasFilter("storage_provider = 'local'");
        builder.HasIndex(x => x.AssetId)
            .HasDatabaseName("ux_asset_files_original")
            .IsUnique()
            .HasFilter("file_role = 'original' AND deleted_at IS NULL");

        builder.HasOne(x => x.Metadata)
            .WithOne(x => x.AssetFile)
            .HasForeignKey<MediaMetadata>(x => x.AssetFileId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_media_metadata_file");
    }
}
