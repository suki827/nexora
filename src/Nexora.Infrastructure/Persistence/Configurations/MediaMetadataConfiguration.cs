using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexora.Domain.Entities;

namespace Nexora.Infrastructure.Persistence.Configurations;

public sealed class MediaMetadataConfiguration : IEntityTypeConfiguration<MediaMetadata>
{
    public void Configure(EntityTypeBuilder<MediaMetadata> builder)
    {
        builder.ToTable("media_metadata", table =>
        {
            table.HasCheckConstraint("ck_media_metadata_audio", "(audio_sample_rate IS NULL OR audio_sample_rate > 0) AND (audio_channels IS NULL OR audio_channels > 0)");
            table.HasCheckConstraint("ck_media_metadata_dimensions", "(width IS NULL OR width > 0) AND (height IS NULL OR height > 0)");
            table.HasCheckConstraint("ck_media_metadata_duration", "duration_seconds IS NULL OR duration_seconds >= 0");
            table.HasCheckConstraint("ck_media_metadata_fps", "(fps_num IS NULL AND fps_den IS NULL) OR (fps_num > 0 AND fps_den > 0)");
            table.HasCheckConstraint("ck_media_metadata_frame_count", "frame_count IS NULL OR frame_count >= 0");
            table.HasCheckConstraint("ck_media_metadata_probe_json", "probe_json IS NULL OR jsonb_typeof(probe_json) = 'object'");
            table.HasCheckConstraint("ck_media_metadata_status", "status IN ('pending', 'processing', 'completed', 'failed')");
        });

        builder.HasKey(x => x.Id).HasName("media_metadata_pkey");
        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.AssetFileId).HasColumnName("asset_file_id").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue(MediaMetadata.StatusPending).IsRequired();
        builder.Property(x => x.DurationSeconds).HasColumnName("duration_seconds").HasColumnType("numeric");
        builder.Property(x => x.Width).HasColumnName("width");
        builder.Property(x => x.Height).HasColumnName("height");
        builder.Property(x => x.FpsNum).HasColumnName("fps_num");
        builder.Property(x => x.FpsDen).HasColumnName("fps_den");
        builder.Property(x => x.FrameCount).HasColumnName("frame_count");
        builder.Property(x => x.IsVariableFps).HasColumnName("is_variable_fps");
        builder.Property(x => x.VideoCodec).HasColumnName("video_codec").HasMaxLength(100);
        builder.Property(x => x.AudioCodec).HasColumnName("audio_codec").HasMaxLength(100);
        builder.Property(x => x.AudioSampleRate).HasColumnName("audio_sample_rate");
        builder.Property(x => x.AudioChannels).HasColumnName("audio_channels");
        builder.Property(x => x.FormatName).HasColumnName("format_name").HasMaxLength(100);
        builder.Property(x => x.ProbeJson).HasColumnName("probe_json").HasColumnType("jsonb");
        builder.Property(x => x.ErrorCode).HasColumnName("error_code").HasMaxLength(100);
        builder.Property(x => x.ErrorMessage).HasColumnName("error_message").HasColumnType("text");
        builder.Property(x => x.ProbedAt).HasColumnName("probed_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");

        builder.HasIndex(x => x.AssetFileId)
            .HasDatabaseName("uq_media_metadata_file")
            .IsUnique();
    }
}
