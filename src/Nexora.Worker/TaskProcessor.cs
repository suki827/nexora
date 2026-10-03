using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Nexora.Domain.Entities;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Worker;

public sealed class TaskProcessor(NexoraDbContext db, ILogger<TaskProcessor> logger)
{
    private const int MaxAttempts = 3;
    private sealed record Claim(Guid TaskId, Guid AttemptId, Guid FileId);

    public async Task<bool> ProcessOneAsync(CancellationToken cancellationToken)
    {
        var claim = await ClaimNextAsync(cancellationToken);
        if (claim is null) return false;

        try
        {
            var metadata = await db.MediaMetadatas.AsNoTracking()
                .SingleAsync(x => x.AssetFileId == claim.FileId, cancellationToken);
            if (metadata.Status == MediaMetadata.StatusFailed)
            {
                await FinishFailureAsync(claim, "source_metadata_failed",
                    metadata.ErrorMessage ?? "Source media metadata processing failed.", retry: false, cancellationToken);
                return true;
            }

            using var payload = MetadataTaskResult.Create(metadata);
            await FinishSuccessAsync(claim, payload, cancellationToken);
            logger.LogInformation("Analysis task {TaskId} completed", claim.TaskId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Recovery will handle this attempt if the worker stops before it can finish.
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Analysis task {TaskId} failed", claim.TaskId);
            db.ChangeTracker.Clear();
            await FinishFailureAsync(claim, "task_processing_failed",
                "Task processing failed. Check worker logs.", retry: true, cancellationToken);
        }

        return true;
    }

    private async Task<Claim?> ClaimNextAsync(CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var taskId = await LockTaskIdAsync("""
            SELECT t.id
            FROM analysis_tasks AS t
            JOIN task_inputs AS i ON i.task_id = t.id
                AND i.input_type = 'asset_file' AND i.input_role = 'source'
            JOIN media_metadata AS m ON m.asset_file_id = i.asset_file_id
                AND m.status IN ('completed', 'failed')
            WHERE t.status = 'queued' AND t.analysis_type = 'media_analysis'
            ORDER BY t.priority DESC, t.queued_at, t.id
            FOR UPDATE OF t SKIP LOCKED
            LIMIT 1
            """, ct);
        if (taskId is null) return null;

        var task = await db.AnalysisTasks.Include(x => x.Attempts).Include(x => x.Inputs)
            .AsSplitQuery().SingleAsync(x => x.Id == taskId.Value, ct);
        var fileId = task.Inputs.Single(x => x.InputType == TaskInput.AssetFileType && x.InputRole == "source")
            .AssetFileId!.Value;
        var nextNumber = task.Attempts.Count == 0 ? 1 : task.Attempts.Max(x => x.AttemptNumber) + 1;
        var attempt = new AnalysisTaskAttempt(task.Id, nextNumber, Environment.MachineName);
        attempt.Heartbeat();
        task.BeginProcessing();
        db.AnalysisTaskAttempts.Add(attempt);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        db.ChangeTracker.Clear();
        logger.LogInformation("Claimed analysis task {TaskId}, attempt {AttemptNumber}", task.Id, nextNumber);
        return new Claim(task.Id, attempt.Id, fileId);
    }

    public async Task<bool> RecoverOneStaleAsync(CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var taskId = await LockTaskIdAsync("""
            SELECT t.id
            FROM analysis_tasks AS t
            WHERE t.status IN ('processing', 'cancel_requested')
              AND EXISTS (
                  SELECT 1 FROM analysis_task_attempts AS a
                  WHERE a.task_id = t.id AND a.status = 'running'
                    AND COALESCE(a.heartbeat_at, a.started_at) < now() - interval '10 minutes')
            ORDER BY t.updated_at, t.id
            FOR UPDATE OF t SKIP LOCKED
            LIMIT 1
            """, ct);
        if (taskId is null) return false;

        var task = await db.AnalysisTasks.Include(x => x.Attempts)
            .SingleAsync(x => x.Id == taskId.Value, ct);
        var attempt = task.Attempts.Where(x => x.Status == AnalysisTaskAttempt.Running)
            .OrderByDescending(x => x.AttemptNumber).First();
        if (task.Status == AnalysisTask.CancelRequested)
        {
            attempt.Finish(AnalysisTaskAttempt.Cancelled);
            task.MarkCancelled();
        }
        else
        {
            attempt.Finish(AnalysisTaskAttempt.Failed, "worker_interrupted",
                "Worker stopped before the attempt completed.");
            if (attempt.AttemptNumber < MaxAttempts) task.RequeueAfterFailure();
            else task.Complete(false);
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        db.ChangeTracker.Clear();
        logger.LogWarning("Recovered interrupted analysis task {TaskId}", taskId);
        return true;
    }

    private async Task FinishSuccessAsync(Claim claim, System.Text.Json.JsonDocument payload, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockSpecificTaskAsync(claim.TaskId, ct);
        var task = await db.AnalysisTasks.SingleAsync(x => x.Id == claim.TaskId, ct);
        var attempt = await db.AnalysisTaskAttempts.SingleAsync(x => x.Id == claim.AttemptId, ct);
        if (attempt.Status != AnalysisTaskAttempt.Running) return;
        if (task.Status == AnalysisTask.CancelRequested)
        {
            attempt.Finish(AnalysisTaskAttempt.Cancelled);
            task.MarkCancelled();
        }
        else
        {
            if (task.Status != AnalysisTask.Processing)
                throw new InvalidOperationException("Task is no longer processing.");
            var previous = await db.AnalysisResults.FirstOrDefaultAsync(x => x.TaskId == task.Id &&
                x.ResultType == MetadataTaskResult.ResultType && x.IsCurrent, ct);
            if (previous is not null)
            {
                previous.Supersede();
                await db.SaveChangesAsync(ct);
            }
            db.AnalysisResults.Add(new AnalysisResult(task.Id, MetadataTaskResult.ResultType,
                MetadataTaskResult.SchemaVersion, payload, null));
            attempt.Finish(AnalysisTaskAttempt.Succeeded);
            task.Complete(true);
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        db.ChangeTracker.Clear();
    }

    private async Task FinishFailureAsync(Claim claim, string code, string message, bool retry, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockSpecificTaskAsync(claim.TaskId, ct);
        var task = await db.AnalysisTasks.SingleAsync(x => x.Id == claim.TaskId, ct);
        var attempt = await db.AnalysisTaskAttempts.SingleAsync(x => x.Id == claim.AttemptId, ct);
        if (attempt.Status != AnalysisTaskAttempt.Running) return;
        if (task.Status == AnalysisTask.CancelRequested)
        {
            attempt.Finish(AnalysisTaskAttempt.Cancelled);
            task.MarkCancelled();
        }
        else
        {
            if (task.Status != AnalysisTask.Processing)
                throw new InvalidOperationException("Task is no longer processing.");
            attempt.Finish(AnalysisTaskAttempt.Failed, code, message);
            if (retry && attempt.AttemptNumber < MaxAttempts) task.RequeueAfterFailure();
            else task.Complete(false);
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        db.ChangeTracker.Clear();
    }

    private async Task LockSpecificTaskAsync(Guid taskId, CancellationToken ct)
    {
        await using var command = CreateCommand("SELECT id FROM analysis_tasks WHERE id = @id FOR UPDATE");
        var parameter = command.CreateParameter();
        parameter.ParameterName = "id";
        parameter.Value = taskId;
        command.Parameters.Add(parameter);
        if (await command.ExecuteScalarAsync(ct) is null)
            throw new InvalidOperationException("Task no longer exists.");
    }

    private async Task<Guid?> LockTaskIdAsync(string sql, CancellationToken ct)
    {
        await using var command = CreateCommand(sql);
        var value = await command.ExecuteScalarAsync(ct);
        return value is Guid id ? id : null;
    }

    private DbCommand CreateCommand(string sql)
    {
        var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 30;
        command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction()
            ?? throw new InvalidOperationException("Task operation requires a transaction.");
        return command;
    }
}
