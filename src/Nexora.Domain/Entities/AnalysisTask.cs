using System.Text.Json;

namespace Nexora.Domain.Entities;

public sealed class AnalysisTask
{
    public const string MediaAnalysisType = "media_analysis";
    public const string Created = "created", Queued = "queued", Processing = "processing", Succeeded = "succeeded",
        Failed = "failed", CancelRequested = "cancel_requested", Cancelled = "cancelled";
    public Guid Id { get; private set; }
    public long TaskNumber { get; private set; }
    public Guid OwnerId { get; private set; }
    public string AnalysisType { get; private set; } = string.Empty;
    public string Status { get; private set; } = Created;
    public int Priority { get; private set; }
    public JsonDocument? RequestPayload { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? QueuedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? CancelRequestedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public ICollection<TaskInput> Inputs { get; private set; } = new List<TaskInput>();
    public ICollection<AnalysisTaskAttempt> Attempts { get; private set; } = new List<AnalysisTaskAttempt>();
    public ICollection<AnalysisResult> Results { get; private set; } = new List<AnalysisResult>();
    public ICollection<GeneratedArtifact> Artifacts { get; private set; } = new List<GeneratedArtifact>();
    private AnalysisTask() { }
    public AnalysisTask(Guid ownerId, string analysisType, int priority, JsonDocument? payload, string? idempotencyKey)
    {
        if (ownerId == Guid.Empty) throw new ArgumentException("Owner is required.", nameof(ownerId));
        if (string.IsNullOrWhiteSpace(analysisType) || analysisType.Length > 100) throw new ArgumentException("Invalid analysis type.", nameof(analysisType));
        if (priority is < -100 or > 100) throw new ArgumentOutOfRangeException(nameof(priority));
        if (idempotencyKey?.Length > 200) throw new ArgumentException("Idempotency key is too long.", nameof(idempotencyKey));
        Id = Guid.NewGuid(); OwnerId = ownerId; AnalysisType = analysisType.Trim(); Priority = priority;
        RequestPayload = payload; IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim();
        CreatedAt = UpdatedAt = DateTime.UtcNow;
    }
    public void Queue() { if (Status != Created) throw new InvalidOperationException("Task is not created."); Status = Queued; QueuedAt = UpdatedAt = DateTime.UtcNow; }
    public void RequestCancel()
    {
        if (Status is Succeeded or Failed or Cancelled) throw new InvalidOperationException("Task already finished.");
        if (Status == CancelRequested) return;
        Status = CancelRequested; CancelRequestedAt = UpdatedAt = DateTime.UtcNow;
    }
    public void BeginProcessing() { if (Status != Queued) throw new InvalidOperationException("Task is not queued."); Status = Processing; StartedAt ??= DateTime.UtcNow; UpdatedAt = DateTime.UtcNow; }
    public void RequeueAfterFailure()
    {
        if (Status != Processing) throw new InvalidOperationException("Only a processing task can be retried.");
        Status = Queued;
        QueuedAt = UpdatedAt = DateTime.UtcNow;
    }
    public void Complete(bool succeeded) { if (Status is not (Processing or CancelRequested)) throw new InvalidOperationException("Task is not processing."); Status = succeeded ? Succeeded : Failed; CompletedAt = UpdatedAt = DateTime.UtcNow; }
    public void MarkCancelled() { if (Status != CancelRequested) throw new InvalidOperationException("Cancellation was not requested."); Status = Cancelled; CompletedAt = UpdatedAt = DateTime.UtcNow; }
}
