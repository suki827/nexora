using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexora.Domain.Entities;

namespace Nexora.Infrastructure.Persistence.Configurations;

public class MediaMetadataConfiguration
    : IEntityTypeConfiguration<MediaMetadata>
{
    public void Configure(EntityTypeBuilder<MediaMetadata> builder)
    {
        builder.ToTable("media_metadata");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.AssetFileId)
            .HasColumnName("asset_file_id")
            .IsRequired();

        builder.HasIndex(x => x.AssetFileId)
            .IsUnique();

        builder.Property(x => x.DurationSeconds)
            .HasColumnName("duration_seconds");

        builder.Property(x => x.Width)
            .HasColumnName("width");

        builder.Property(x => x.Height)
            .HasColumnName("height");

        builder.Property(x => x.VideoCodec)
            .HasColumnName("video_codec");

        builder.Property(x => x.AudioCodec)
            .HasColumnName("audio_codec");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");
    }
}