using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexora.Domain.Entities;

namespace Nexora.Infrastructure.Persistence.Configurations;

public class AnalysisResultConfiguration
    : IEntityTypeConfiguration<AnalysisResult>
{
    public void Configure(EntityTypeBuilder<AnalysisResult> builder)
    {
        builder.ToTable("analysis_results");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.AnalysisTaskId)
            .HasColumnName("analysis_task_id")
            .IsRequired();

        builder.Property(x => x.ResultType)
            .HasColumnName("result_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.SchemaVersion)
            .HasColumnName("schema_version")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.ResultPayload)
            .HasColumnName("result_payload")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.ConfidenceScore)
            .HasColumnName("confidence_score")
            .HasPrecision(5, 4);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne(x => x.AnalysisTask)
            .WithMany(x => x.Results)
            .HasForeignKey(x => x.AnalysisTaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}