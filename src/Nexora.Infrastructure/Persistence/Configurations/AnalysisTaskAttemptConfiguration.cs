using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexora.Domain.Entities;

namespace Nexora.Infrastructure.Persistence.Configurations;

public sealed class AnalysisTaskAttemptConfiguration : IEntityTypeConfiguration<AnalysisTaskAttempt>
{
    public void Configure(EntityTypeBuilder<AnalysisTaskAttempt> builder)
    {
        builder.ToTable("analysis_task_attempts", table =>
        {
            table.HasCheckConstraint("ck_analysis_task_attempts_status", "status IN ('running','succeeded','failed','cancelled')");
            table.HasCheckConstraint("ck_analysis_task_attempts_number", "attempt_number > 0");
            table.HasCheckConstraint("ck_analysis_task_attempts_duration", "processing_duration_ms IS NULL OR processing_duration_ms >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.TaskId).HasColumnName("task_id").IsRequired();
        builder.Property(x => x.AttemptNumber).HasColumnName("attempt_number").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.WorkerId).HasColumnName("worker_id").HasMaxLength(200);
        builder.Property(x => x.StartedAt).HasColumnName("started_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.HeartbeatAt).HasColumnName("heartbeat_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.ErrorCode).HasColumnName("error_code").HasMaxLength(100);
        builder.Property(x => x.ErrorMessage).HasColumnName("error_message").HasColumnType("text");
        builder.Property(x => x.ProcessingDurationMs).HasColumnName("processing_duration_ms");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.HasOne(x => x.Task).WithMany(x => x.Attempts).HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.TaskId, x.AttemptNumber }).IsUnique().HasDatabaseName("ux_analysis_task_attempts_number");
    }
}
