using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexora.Domain.Entities;

namespace Nexora.Infrastructure.Persistence.Configurations;

public sealed class GeneratedArtifactConfiguration : IEntityTypeConfiguration<GeneratedArtifact>
{
    public void Configure(EntityTypeBuilder<GeneratedArtifact> builder)
    {
        builder.ToTable("generated_artifacts", table =>
        {
            table.HasCheckConstraint("ck_generated_artifacts_provider", "storage_provider IN ('local','swift','s3')");
            table.HasCheckConstraint("ck_generated_artifacts_status", "status IN ('generating','ready','failed','deleted')");
            table.HasCheckConstraint("ck_generated_artifacts_storage", "(storage_provider = 'local' AND storage_bucket IS NULL) OR (storage_provider IN ('swift','s3') AND storage_bucket IS NOT NULL AND length(btrim(storage_bucket)) > 0)");
            table.HasCheckConstraint("ck_generated_artifacts_key", "length(btrim(storage_key)) > 0");
            table.HasCheckConstraint("ck_generated_artifacts_size", "size_bytes IS NULL OR size_bytes >= 0");
            table.HasCheckConstraint("ck_generated_artifacts_hash", "content_hash IS NULL OR content_hash ~ '^[0-9a-f]{64}$'");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.TaskId).HasColumnName("task_id").IsRequired();
        builder.Property(x => x.ResultId).HasColumnName("result_id");
        builder.Property(x => x.ArtifactType).HasColumnName("artifact_type").HasMaxLength(100).IsRequired();
        builder.Property(x => x.FileName).HasColumnName("file_name").HasMaxLength(255).IsRequired();
        builder.Property(x => x.StorageProvider).HasColumnName("storage_provider").HasMaxLength(30).IsRequired();
        builder.Property(x => x.StorageBucket).HasColumnName("storage_bucket").HasMaxLength(255);
        builder.Property(x => x.StorageKey).HasColumnName("storage_key").HasColumnType("text").IsRequired();
        builder.Property(x => x.ContentType).HasColumnName("content_type").HasMaxLength(150);
        builder.Property(x => x.SizeBytes).HasColumnName("size_bytes");
        builder.Property(x => x.ContentHash).HasColumnName("content_hash").HasMaxLength(64);
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.HasOne(x => x.Task).WithMany(x => x.Artifacts).HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Result).WithMany(x => x.Artifacts).HasForeignKey(x => x.ResultId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.TaskId, x.CreatedAt }).HasDatabaseName("ix_generated_artifacts_task_created");
        builder.HasIndex(x => x.ResultId).HasDatabaseName("ix_generated_artifacts_result");
        builder.HasIndex(x => x.StorageKey).IsUnique().HasFilter("storage_provider = 'local'").HasDatabaseName("ux_generated_artifacts_local_storage");
        builder.HasIndex(x => new { x.StorageProvider, x.StorageBucket, x.StorageKey }).IsUnique().HasFilter("storage_provider IN ('swift','s3')").HasDatabaseName("ux_generated_artifacts_cloud_storage");
    }
}
