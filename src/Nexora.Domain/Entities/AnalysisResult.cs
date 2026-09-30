using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nexora.Domain.Entities;

[Table("analysis_results")]
public class AnalysisResult
{
    /// <summary>
    /// UUID primary key of the analysis result record.
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// UUID of the analysis task that produced this result.
    /// </summary>
    [Required]
    [Column("analysis_task_id")]
    public Guid AnalysisTaskId { get; set; }

    /// <summary>
    /// Type of result, such as video_summary or shot_detection.
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column("result_type")]
    public string ResultType { get; set; } = string.Empty;

    /// <summary>
    /// Version of the JSON result structure.
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column("schema_version")]
    public string SchemaVersion { get; set; } = string.Empty;

    /// <summary>
    /// Structured JSON analysis output.
    /// </summary>
    [Required]
    [Column("result_payload", TypeName = "jsonb")]
    public JsonDocument ResultPayload { get; set; } = null!;

    /// <summary>
    /// Optional confidence or quality score.
    /// </summary>
    [Column("confidence_score", TypeName = "numeric(5,4)")]
    public decimal? ConfidenceScore { get; set; }

    /// <summary>
    /// Date and time when the result was created.
    /// </summary>
    [Required]
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Date and time when the result was last updated.
    /// </summary>
    [Required]
    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property for the related analysis task.
    /// </summary>
    [ForeignKey(nameof(AnalysisTaskId))]
    [JsonIgnore]
    public AnalysisTask? AnalysisTask { get; set; }
}