using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Nexora.Domain.Entities;

namespace Nexora.Api.Contracts.Tasks;

public sealed class CreateTaskRequest
{
    [Required, StringLength(100, MinimumLength = 1)] public string AnalysisType { get; init; } = string.Empty;
    [Range(-100, 100)] public int Priority { get; init; }
    public JsonElement? RequestPayload { get; init; }
    [StringLength(200)] public string? IdempotencyKey { get; init; }
    [Required, MinLength(1), MaxLength(32)] public List<TaskInputRequest> Inputs { get; init; } = [];
}

public sealed class TaskInputRequest
{
    [Required, RegularExpression("^(asset_file|artifact)$")] public string InputType { get; init; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 1)] public string InputRole { get; init; } = string.Empty;
    public Guid? AssetFileId { get; init; }
    public Guid? ArtifactId { get; init; }
}

public sealed record TaskSummary(Guid Id, long TaskNumber, string AnalysisType, string Status, int Priority,
    DateTime CreatedAt, DateTime UpdatedAt);
public sealed record TaskInputView(Guid Id, string InputType, string InputRole, Guid? AssetFileId, Guid? ArtifactId, DateTime CreatedAt);
public sealed record AttemptView(Guid Id, int AttemptNumber, string Status, string? WorkerId, DateTime StartedAt,
    DateTime? CompletedAt, DateTime? HeartbeatAt, string? ErrorCode, string? ErrorMessage, long? ProcessingDurationMs);
public sealed record ResultView(Guid Id, string ResultType, string SchemaVersion, JsonElement ResultPayload,
    decimal? ConfidenceScore, bool IsCurrent, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record ArtifactView(Guid Id, Guid? ResultId, string ArtifactType, string FileName, string StorageProvider,
    string? ContentType, long? SizeBytes, string? ContentHash, string Status, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record TaskDetail(TaskSummary Task, JsonElement? RequestPayload, IReadOnlyList<TaskInputView> Inputs,
    IReadOnlyList<AttemptView> Attempts, IReadOnlyList<ResultView> Results, IReadOnlyList<ArtifactView> Artifacts);

public static class TaskResponseMapper
{
    public static TaskSummary ToSummary(this AnalysisTask task) =>
        new(task.Id, task.TaskNumber, task.AnalysisType, task.Status, task.Priority, task.CreatedAt, task.UpdatedAt);
    public static TaskInputView ToView(this TaskInput input) =>
        new(input.Id, input.InputType, input.InputRole, input.AssetFileId, input.ArtifactId, input.CreatedAt);
    public static AttemptView ToView(this AnalysisTaskAttempt attempt) =>
        new(attempt.Id, attempt.AttemptNumber, attempt.Status, attempt.WorkerId, attempt.StartedAt,
            attempt.CompletedAt, attempt.HeartbeatAt, attempt.ErrorCode, attempt.ErrorMessage, attempt.ProcessingDurationMs);
    public static ResultView ToView(this AnalysisResult result) =>
        new(result.Id, result.ResultType, result.SchemaVersion, result.ResultPayload.RootElement.Clone(),
            result.ConfidenceScore, result.IsCurrent, result.CreatedAt, result.UpdatedAt);
    public static ArtifactView ToView(this GeneratedArtifact artifact) =>
        new(artifact.Id, artifact.ResultId, artifact.ArtifactType, artifact.FileName, artifact.StorageProvider,
            artifact.ContentType, artifact.SizeBytes, artifact.ContentHash, artifact.Status, artifact.CreatedAt, artifact.UpdatedAt);
    public static TaskDetail ToDetail(this AnalysisTask task) => new(task.ToSummary(),
        task.RequestPayload?.RootElement.Clone(), task.Inputs.OrderBy(x => x.CreatedAt).Select(x => x.ToView()).ToArray(),
        task.Attempts.OrderBy(x => x.AttemptNumber).Select(x => x.ToView()).ToArray(),
        task.Results.OrderByDescending(x => x.CreatedAt).Select(x => x.ToView()).ToArray(),
        task.Artifacts.OrderByDescending(x => x.CreatedAt).Select(x => x.ToView()).ToArray());
}
