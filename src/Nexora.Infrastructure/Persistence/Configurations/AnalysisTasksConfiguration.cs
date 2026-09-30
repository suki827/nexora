using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexora.Domain.Entities;

namespace Nexora.Infrastructure.Persistence.Configurations;

// 用来映射 AnalysisTask Entity 和 analysis_tasks 表字段
public class AnalysisTasksConfiguration
    : IEntityTypeConfiguration<AnalysisTask>
{
    public void Configure(EntityTypeBuilder<AnalysisTask> builder)
    {
        builder.ToTable("analysis_tasks");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.TaskNumber)
            .HasColumnName("task_number")
            .ValueGeneratedOnAdd()
            .IsRequired();

        builder.Property(x => x.AnalysisType)
            .HasColumnName("analysis_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.InputFileUrl)
            .HasColumnName("input_file_url")
            .HasColumnType("text");

        builder.Property(x => x.RequestPayload)
            .HasColumnName("request_payload")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.Provider)
            .HasColumnName("provider")
            .HasMaxLength(50);

        builder.Property(x => x.AttemptCount)
            .HasColumnName("attempt_count")
            .IsRequired();

        builder.Property(x => x.MaxAttempts)
            .HasColumnName("max_attempts")
            .IsRequired();

        builder.Property(x => x.ErrorCode)
            .HasColumnName("error_code")
            .HasMaxLength(100);

        builder.Property(x => x.ErrorMessage)
            .HasColumnName("error_message")
            .HasColumnType("text");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.StartedAt)
            .HasColumnName("started_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.CompletedAt)
            .HasColumnName("completed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasMany(x => x.Results)
            .WithOne(x => x.AnalysisTask)
            .HasForeignKey(x => x.AnalysisTaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}