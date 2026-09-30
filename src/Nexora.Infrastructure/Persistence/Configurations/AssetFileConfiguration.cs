using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexora.Domain.Entities;

namespace Nexora.Infrastructure.Persistence.Configurations;

public class AssetFileConfiguration
    : IEntityTypeConfiguration<AssetFile>
{
    public void Configure(EntityTypeBuilder<AssetFile> builder)
    {
        builder.ToTable("asset_files");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.AssetId)
            .HasColumnName("asset_id")
            .IsRequired();

        builder.Property(x => x.OriginalFilename)
            .HasColumnName("original_filename")
            .IsRequired();

        builder.Property(x => x.StorageKey)
            .HasColumnName("storage_key")
            .IsRequired();

        builder.Property(x => x.ContentType)
            .HasColumnName("content_type");

        builder.Property(x => x.SizeBytes)
            .HasColumnName("size_bytes");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(x => x.AssetId);

        builder.HasOne(x => x.Metadata)
            .WithOne(x => x.AssetFile)
            .HasForeignKey<MediaMetadata>(x => x.AssetFileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}