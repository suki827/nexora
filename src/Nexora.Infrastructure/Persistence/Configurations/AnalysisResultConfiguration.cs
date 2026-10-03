using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexora.Domain.Entities;

namespace Nexora.Infrastructure.Persistence.Configurations;

public sealed class AnalysisResultConfiguration : IEntityTypeConfiguration<AnalysisResult>
{
    public void Configure(EntityTypeBuilder<AnalysisResult> builder)
    {
        builder.ToTable("analysis_results", table =>
            table.HasCheckConstraint("ck_analysis_results_score", "confidence_score IS NULL OR confidence_score BETWEEN 0 AND 1"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.TaskId).HasColumnName("task_id").IsRequired();
        builder.Property(x => x.ResultType).HasColumnName("result_type").HasMaxLength(100).IsRequired();
        builder.Property(x => x.SchemaVersion).HasColumnName("schema_version").HasMaxLength(20).IsRequired();
        builder.Property(x => x.ResultPayload).HasColumnName("result_payload").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.ConfidenceScore).HasColumnName("confidence_score").HasPrecision(5, 4);
        builder.Property(x => x.IsCurrent).HasColumnName("is_current").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.HasOne(x => x.Task).WithMany(x => x.Results).HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.TaskId, x.ResultType }).HasDatabaseName("ix_analysis_results_task_type");
        builder.HasIndex(x => new { x.TaskId, x.ResultType }).IsUnique().HasFilter("is_current = true").HasDatabaseName("ux_analysis_results_current_type");
    }
}
