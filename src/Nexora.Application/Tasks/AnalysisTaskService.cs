using System.Text.Json;
using Nexora.Domain.Entities;

namespace Nexora.Application.Tasks;

public sealed record TaskInputSpec(string InputType, string InputRole, Guid? AssetFileId, Guid? ArtifactId);
public sealed record TaskPage(IReadOnlyList<AnalysisTask> Items, int Page, int PageSize, int TotalCount);

public sealed class AnalysisTaskService(ITaskRepository repository)
{
    public async Task<AnalysisTask> CreateAsync(Guid ownerId, string analysisType, int priority, JsonDocument? payload,
        string? idempotencyKey, IReadOnlyList<TaskInputSpec> inputs, CancellationToken cancellationToken)
    {
        if (ownerId == Guid.Empty) throw new ArgumentException("Owner is required.", nameof(ownerId));
        if (inputs is null || inputs.Count is < 1 or > 32) throw new ArgumentException("Provide 1–32 inputs.", nameof(inputs));
        if (analysisType?.Trim() != AnalysisTask.MediaAnalysisType)
            throw new ArgumentException("Unsupported analysis type. Use media_analysis.", nameof(analysisType));
        if (inputs.Count != 1 || inputs[0].InputType != TaskInput.AssetFileType ||
            inputs[0].InputRole?.Trim() != "source" || inputs[0].AssetFileId is null)
            throw new ArgumentException("media_analysis requires one source asset file.", nameof(inputs));
        var key = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim();
        if (key is not null)
        {
            var existing = await repository.GetByIdempotencyKeyAsync(ownerId, key, cancellationToken);
            if (existing is not null)
            {
                var requestedInputs = inputs.Select(x => (x.InputType, x.InputRole.Trim(), x.AssetFileId, x.ArtifactId))
                    .OrderBy(x => x.InputType).ThenBy(x => x.Item2).ThenBy(x => x.AssetFileId).ThenBy(x => x.ArtifactId);
                var savedInputs = existing.Inputs.Select(x => (x.InputType, x.InputRole, x.AssetFileId, x.ArtifactId))
                    .OrderBy(x => x.InputType).ThenBy(x => x.InputRole).ThenBy(x => x.AssetFileId).ThenBy(x => x.ArtifactId);
                if (existing.AnalysisType != analysisType?.Trim() || existing.Priority != priority ||
                    !requestedInputs.SequenceEqual(savedInputs) ||
                    (existing.RequestPayload?.RootElement.GetRawText() ?? "") != (payload?.RootElement.GetRawText() ?? ""))
                    throw new InvalidOperationException("Idempotency key was already used for a different task.");
                return existing;
            }
        }
        var task = new AnalysisTask(ownerId, analysisType, priority, payload, key);
        var seen = new HashSet<(string, string, Guid)>();
        foreach (var input in inputs)
        {
            var item = new TaskInput(task.Id, input.InputType, input.InputRole, input.AssetFileId, input.ArtifactId);
            var targetId = item.AssetFileId ?? item.ArtifactId!.Value;
            if (!seen.Add((item.InputType, item.InputRole, targetId)))
                throw new ArgumentException("Duplicate task input.", nameof(inputs));
            var owned = item.InputType == TaskInput.AssetFileType
                ? await repository.OwnsReadyAssetFileAsync(ownerId, targetId, cancellationToken)
                : await repository.OwnsReadyArtifactAsync(ownerId, targetId, cancellationToken);
            if (!owned) throw new ArgumentException("Input does not exist, is not ready, or is not owned by this user.", nameof(inputs));
            task.Inputs.Add(item);
        }
        task.Queue();
        repository.AddTask(task);
        await repository.SaveChangesAsync(cancellationToken);
        return task;
    }

    public async Task<TaskPage> ListAsync(Guid ownerId, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (ownerId == Guid.Empty) throw new ArgumentException("Owner is required.", nameof(ownerId));
        if (page < 1 || pageSize is < 1 or > 100 || (long)(page - 1) * pageSize > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(page), "Invalid pagination.");
        var items = await repository.ListAsync(ownerId, (page - 1) * pageSize, pageSize, cancellationToken);
        return new TaskPage(items, page, pageSize, await repository.CountAsync(ownerId, cancellationToken));
    }

    public Task<AnalysisTask?> GetAsync(Guid ownerId, Guid taskId, CancellationToken cancellationToken) =>
        repository.GetAsync(ownerId, taskId, cancellationToken);

    public async Task<AnalysisTask?> CancelAsync(Guid ownerId, Guid taskId, CancellationToken cancellationToken)
    {
        if (ownerId == Guid.Empty) throw new ArgumentException("Owner is required.", nameof(ownerId));
        return await repository.CancelOwnedAsync(ownerId, taskId, cancellationToken);
    }

    // Trusted executor methods. User-facing HTTP endpoints never call these with client supplied state.
    public async Task<AnalysisTaskAttempt> StartAttemptAsync(Guid taskId, string? workerId, CancellationToken cancellationToken)
    {
        var task = await repository.GetForUpdateAsync(taskId, cancellationToken) ?? throw new KeyNotFoundException("Task not found.");
        if (task.Status != AnalysisTask.Queued) throw new InvalidOperationException("Task is not queued.");
        var attempt = new AnalysisTaskAttempt(taskId, task.Attempts.Count + 1, workerId);
        task.BeginProcessing(); repository.AddAttempt(attempt);
        await repository.SaveChangesAsync(cancellationToken);
        return attempt;
    }

    public async Task FinishAttemptAsync(Guid taskId, Guid attemptId, string status, string? errorCode,
        string? errorMessage, CancellationToken cancellationToken)
    {
        var task = await repository.GetForUpdateAsync(taskId, cancellationToken) ?? throw new KeyNotFoundException("Task not found.");
        var attempt = await repository.GetAttemptAsync(taskId, attemptId, cancellationToken) ?? throw new KeyNotFoundException("Attempt not found.");
        attempt.Finish(status, errorCode, errorMessage);
        if (status == AnalysisTaskAttempt.Cancelled) { task.RequestCancel(); task.MarkCancelled(); }
        else task.Complete(status == AnalysisTaskAttempt.Succeeded);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<AnalysisResult> AddResultAsync(Guid taskId, string resultType, string schemaVersion,
        JsonDocument payload, decimal? confidenceScore, CancellationToken cancellationToken)
    {
        _ = await repository.GetForUpdateAsync(taskId, cancellationToken) ?? throw new KeyNotFoundException("Task not found.");
        var previous = await repository.GetCurrentResultAsync(taskId, resultType, cancellationToken);
        var result = new AnalysisResult(taskId, resultType, schemaVersion, payload, confidenceScore);
        await repository.ReplaceCurrentResultAsync(previous, result, cancellationToken);
        return result;
    }

    public async Task<GeneratedArtifact> AddArtifactAsync(Guid taskId, Guid? resultId, string artifactType,
        string fileName, string storageProvider, string? bucket, string storageKey, string? contentType, CancellationToken cancellationToken)
    {
        _ = await repository.GetForUpdateAsync(taskId, cancellationToken) ?? throw new KeyNotFoundException("Task not found.");
        if (resultId is { } id && await repository.GetResultAsync(taskId, id, cancellationToken) is null)
            throw new ArgumentException("Result does not belong to this task.", nameof(resultId));
        var artifact = new GeneratedArtifact(taskId, resultId, artifactType, fileName, storageProvider, bucket, storageKey, contentType);
        repository.AddArtifact(artifact);
        await repository.SaveChangesAsync(cancellationToken);
        return artifact;
    }

    public async Task<GeneratedArtifact> MarkArtifactReadyAsync(Guid taskId, Guid artifactId, long sizeBytes,
        string? hash, CancellationToken cancellationToken)
    {
        var artifact = await repository.GetArtifactAsync(taskId, artifactId, cancellationToken) ?? throw new KeyNotFoundException("Artifact not found.");
        artifact.MarkReady(sizeBytes, hash);
        await repository.SaveChangesAsync(cancellationToken);
        return artifact;
    }
}
