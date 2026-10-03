namespace Nexora.Domain.Entities;

public sealed class AnalysisTaskAttempt
{
    public const string Running = "running", Succeeded = "succeeded", Failed = "failed", Cancelled = "cancelled";
    public Guid Id { get; private set; }
    public Guid TaskId { get; private set; }
    public int AttemptNumber { get; private set; }
    public string Status { get; private set; } = Running;
    public string? WorkerId { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? HeartbeatAt { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public long? ProcessingDurationMs { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public AnalysisTask Task { get; private set; } = null!;
    private AnalysisTaskAttempt() { }
    public AnalysisTaskAttempt(Guid taskId, int number, string? workerId)
    {
        if (taskId == Guid.Empty) throw new ArgumentException("Task is required.", nameof(taskId));
        if (number <= 0) throw new ArgumentOutOfRangeException(nameof(number));
        if (workerId?.Length > 200) throw new ArgumentException("Worker ID is too long.", nameof(workerId));
        Id = Guid.NewGuid(); TaskId = taskId; AttemptNumber = number; WorkerId = workerId;
        StartedAt = CreatedAt = DateTime.UtcNow;
    }
    public void Heartbeat() { if (Status == Running) HeartbeatAt = DateTime.UtcNow; }
    public void Finish(string status, string? errorCode = null, string? errorMessage = null)
    {
        if (Status != Running || status is not (Succeeded or Failed or Cancelled)) throw new InvalidOperationException("Invalid attempt transition.");
        if (errorCode?.Length > 100) throw new ArgumentException("Error code is too long.", nameof(errorCode));
        Status = status; CompletedAt = DateTime.UtcNow;
        ProcessingDurationMs = Math.Max(0, (long)(CompletedAt.Value - StartedAt).TotalMilliseconds);
        ErrorCode = status == Failed ? errorCode : null; ErrorMessage = status == Failed ? errorMessage : null;
    }
}
