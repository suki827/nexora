using System.Text.Json;

namespace Nexora.Domain.Entities;

public sealed class AnalysisResult
{
    public Guid Id { get; private set; }
    public Guid TaskId { get; private set; }
    public string ResultType { get; private set; } = string.Empty;
    public string SchemaVersion { get; private set; } = string.Empty;
    public JsonDocument ResultPayload { get; private set; } = null!;
    public decimal? ConfidenceScore { get; private set; }
    public bool IsCurrent { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public AnalysisTask Task { get; private set; } = null!;
    public ICollection<GeneratedArtifact> Artifacts { get; private set; } = new List<GeneratedArtifact>();
    private AnalysisResult() { }
    public AnalysisResult(Guid taskId, string resultType, string schemaVersion, JsonDocument payload, decimal? score)
    {
        if (taskId == Guid.Empty) throw new ArgumentException("Task is required.", nameof(taskId));
        if (string.IsNullOrWhiteSpace(resultType) || resultType.Length > 100) throw new ArgumentException("Invalid result type.", nameof(resultType));
        if (string.IsNullOrWhiteSpace(schemaVersion) || schemaVersion.Length > 20) throw new ArgumentException("Invalid schema version.", nameof(schemaVersion));
        if (score is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(score));
        Id = Guid.NewGuid(); TaskId = taskId; ResultType = resultType.Trim(); SchemaVersion = schemaVersion.Trim();
        ResultPayload = payload ?? throw new ArgumentNullException(nameof(payload)); ConfidenceScore = score;
        IsCurrent = true; CreatedAt = UpdatedAt = DateTime.UtcNow;
    }
    public void Supersede() { IsCurrent = false; UpdatedAt = DateTime.UtcNow; }
}
