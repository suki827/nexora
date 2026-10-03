using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexora.Domain.Entities;

namespace Nexora.Infrastructure.Persistence.Configurations;

public sealed class TaskInputConfiguration : IEntityTypeConfiguration<TaskInput>
{
    public void Configure(EntityTypeBuilder<TaskInput> builder)
    {
        builder.ToTable("task_inputs", table => table.HasCheckConstraint("ck_task_inputs_target",
            "(input_type = 'asset_file' AND asset_file_id IS NOT NULL AND artifact_id IS NULL) OR (input_type = 'artifact' AND artifact_id IS NOT NULL AND asset_file_id IS NULL)"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.TaskId).HasColumnName("task_id").IsRequired();
        builder.Property(x => x.InputType).HasColumnName("input_type").HasMaxLength(30).IsRequired();
        builder.Property(x => x.InputRole).HasColumnName("input_role").HasMaxLength(100).IsRequired();
        builder.Property(x => x.AssetFileId).HasColumnName("asset_file_id");
        builder.Property(x => x.ArtifactId).HasColumnName("artifact_id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.HasOne(x => x.Task).WithMany(x => x.Inputs).HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.AssetFile).WithMany().HasForeignKey(x => x.AssetFileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Artifact).WithMany(x => x.UsedByInputs).HasForeignKey(x => x.ArtifactId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.TaskId, x.CreatedAt }).HasDatabaseName("ix_task_inputs_task_created");
        builder.HasIndex(x => x.AssetFileId).HasDatabaseName("ix_task_inputs_asset_file");
        builder.HasIndex(x => x.ArtifactId).HasDatabaseName("ix_task_inputs_artifact");
    }
}
