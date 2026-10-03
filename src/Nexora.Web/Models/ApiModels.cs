using System.Text.Json;

namespace Nexora.Web.Models;

public sealed record CurrentUser(Guid Id, string Email, string? DisplayName);
public sealed record PageData<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
public sealed record Asset(Guid Id, string Name, string? Description, string AssetType, string Status,
    DateTime CreatedAt, DateTime UpdatedAt);
public sealed record AssetFile(Guid Id, Guid AssetId, string FileRole, string OriginalName,
    string StorageProvider, string? StorageBucket, string StorageKey, string? ContentType,
    long? SizeBytes, string? ContentHash, string Status, string? ErrorCode, string? ErrorMessage,
    DateTime CreatedAt, DateTime UpdatedAt, DateTime? UploadedAt, DateTime? DeletedAt);
public sealed record MediaMetadata(string Status, decimal? DurationSeconds, int? Width, int? Height,
    string? VideoCodec, string? AudioCodec, string? FormatName, string? ErrorMessage);
public sealed record TaskSummary(Guid Id, long TaskNumber, string AnalysisType, string Status,
    int Priority, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record TaskInput(Guid Id, string InputType, string InputRole, Guid? AssetFileId,
    Guid? ArtifactId, DateTime CreatedAt);
public sealed record TaskAttempt(Guid Id, int AttemptNumber, string Status, string? WorkerId,
    DateTime StartedAt, DateTime? CompletedAt, DateTime? HeartbeatAt, string? ErrorCode,
    string? ErrorMessage, long? ProcessingDurationMs);
public sealed record TaskResult(Guid Id, string ResultType, string SchemaVersion, JsonElement ResultPayload,
    decimal? ConfidenceScore, bool IsCurrent, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record TaskArtifact(Guid Id, Guid? ResultId, string ArtifactType, string FileName,
    string StorageProvider, string? ContentType, long? SizeBytes, string? ContentHash,
    string Status, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record TaskDetail(TaskSummary Task, JsonElement? RequestPayload,
    IReadOnlyList<TaskInput> Inputs, IReadOnlyList<TaskAttempt> Attempts,
    IReadOnlyList<TaskResult> Results, IReadOnlyList<TaskArtifact> Artifacts);
