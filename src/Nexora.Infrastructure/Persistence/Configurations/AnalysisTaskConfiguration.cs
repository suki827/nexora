using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexora.Domain.Entities;
using Nexora.Infrastructure.Identity;

namespace Nexora.Infrastructure.Persistence.Configurations;

public sealed class AnalysisTaskConfiguration : IEntityTypeConfiguration<AnalysisTask>
{
    public void Configure(EntityTypeBuilder<AnalysisTask> builder)
    {
        builder.ToTable("analysis_tasks", table =>
        {
            table.HasCheckConstraint("ck_analysis_tasks_status", "status IN ('created','queued','processing','succeeded','failed','cancel_requested','cancelled')");
            table.HasCheckConstraint("ck_analysis_tasks_priority", "priority BETWEEN -100 AND 100");
            table.HasCheckConstraint("ck_analysis_tasks_type", "length(btrim(analysis_type)) > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.TaskNumber).HasColumnName("task_number").UseIdentityByDefaultColumn();
        builder.Property(x => x.OwnerId).HasColumnName("owner_id").IsRequired();
        builder.Property(x => x.AnalysisType).HasColumnName("analysis_type").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.Priority).HasColumnName("priority").IsRequired();
        builder.Property(x => x.RequestPayload).HasColumnName("request_payload").HasColumnType("jsonb");
        builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(200);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.QueuedAt).HasColumnName("queued_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.StartedAt).HasColumnName("started_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CancelRequestedAt).HasColumnName("cancel_requested_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.TaskNumber).IsUnique().HasDatabaseName("ux_analysis_tasks_number");
        builder.HasIndex(x => new { x.OwnerId, x.IdempotencyKey }).IsUnique().HasFilter("idempotency_key IS NOT NULL").HasDatabaseName("ux_analysis_tasks_owner_idempotency");
        builder.HasIndex(x => new { x.OwnerId, x.CreatedAt }).IsDescending(false, true).HasDatabaseName("ix_analysis_tasks_owner_created");
        builder.HasIndex(x => new { x.Status, x.CreatedAt }).HasDatabaseName("ix_analysis_tasks_status_created");
    }
}
