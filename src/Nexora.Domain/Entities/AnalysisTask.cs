using System.Text.Json;

namespace Nexora.Domain.Entities;

public class AnalysisTask
{
    public Guid Id { get; set; }

    /// <summary>
    /// Human-readable auto-incrementing task number.
    /// PostgreSQL bigint maps to C# long.
    /// </summary>
    public long TaskNumber { get; set; }

    /// <summary>
    /// Type of analysis requested.
    /// </summary>
    public string AnalysisType { get; set; } = string.Empty;

    /// <summary>
    /// Current task status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Location of the input media file.
    /// </summary>
    public string? InputFileUrl { get; set; }

    /// <summary>
    /// Immutable JSON snapshot of the analysis request.
    /// </summary>
    public JsonDocument RequestPayload { get; set; } = null!;

    /// <summary>
    /// Analysis provider selected for this task.
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// Number of processing attempts already made.
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// Maximum number of processing attempts.
    /// </summary>
    public int MaxAttempts { get; set; }

    /// <summary>
    /// Machine-readable error code.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public string? ErrorMessage { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Analysis results belonging to this task.
    /// </summary>
    public ICollection<AnalysisResult> Results { get; set; }
        = new List<AnalysisResult>();
}