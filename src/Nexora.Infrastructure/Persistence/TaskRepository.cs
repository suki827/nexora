using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Nexora.Application.Tasks;
using Nexora.Domain.Entities;

namespace Nexora.Infrastructure.Persistence;

public sealed class TaskRepository(NexoraDbContext db) : ITaskRepository
{
    public Task<AnalysisTask?> GetAsync(Guid ownerId, Guid taskId, CancellationToken ct) =>
        db.AnalysisTasks.Include(x => x.Inputs).Include(x => x.Attempts).Include(x => x.Results)
            .Include(x => x.Artifacts).AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == taskId && x.OwnerId == ownerId, ct);

    public async Task<AnalysisTask?> CancelOwnedAsync(Guid ownerId, Guid taskId, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = "SELECT id FROM analysis_tasks WHERE id = @task_id AND owner_id = @owner_id FOR UPDATE";
        var taskParameter = command.CreateParameter();
        taskParameter.ParameterName = "task_id";
        taskParameter.Value = taskId;
        command.Parameters.Add(taskParameter);
        var ownerParameter = command.CreateParameter();
        ownerParameter.ParameterName = "owner_id";
        ownerParameter.Value = ownerId;
        command.Parameters.Add(ownerParameter);
        if (await command.ExecuteScalarAsync(ct) is null) return null;

        var task = await db.AnalysisTasks.SingleAsync(x => x.Id == taskId, ct);
        var immediate = task.Status is AnalysisTask.Created or AnalysisTask.Queued;
        task.RequestCancel();
        if (immediate) task.MarkCancelled();
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return task;
    }

    public Task<AnalysisTask?> GetForUpdateAsync(Guid taskId, CancellationToken ct) =>
        db.AnalysisTasks.Include(x => x.Attempts).FirstOrDefaultAsync(x => x.Id == taskId, ct);

    public Task<AnalysisTask?> GetByIdempotencyKeyAsync(Guid ownerId, string key, CancellationToken ct) =>
        db.AnalysisTasks.Include(x => x.Inputs).Include(x => x.Attempts).Include(x => x.Results)
            .Include(x => x.Artifacts).AsSplitQuery()
            .FirstOrDefaultAsync(x => x.OwnerId == ownerId && x.IdempotencyKey == key, ct);

    public async Task<IReadOnlyList<AnalysisTask>> ListAsync(Guid ownerId, int skip, int take, CancellationToken ct) =>
        await db.AnalysisTasks.AsNoTracking().Where(x => x.OwnerId == ownerId)
            .OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Skip(skip).Take(take).ToListAsync(ct);

    public Task<int> CountAsync(Guid ownerId, CancellationToken ct) =>
        db.AnalysisTasks.CountAsync(x => x.OwnerId == ownerId, ct);

    public Task<bool> OwnsReadyAssetFileAsync(Guid ownerId, Guid fileId, CancellationToken ct) =>
        db.AssetFiles.AnyAsync(x => x.Id == fileId && x.Status == AssetFile.StatusReady && x.DeletedAt == null &&
                                   x.Asset.OwnerId == ownerId && x.Asset.DeletedAt == null, ct);

    public Task<bool> OwnsReadyArtifactAsync(Guid ownerId, Guid artifactId, CancellationToken ct) =>
        db.GeneratedArtifacts.AnyAsync(x => x.Id == artifactId && x.Status == GeneratedArtifact.Ready && x.Task.OwnerId == ownerId, ct);

    public Task<AnalysisTaskAttempt?> GetAttemptAsync(Guid taskId, Guid attemptId, CancellationToken ct) =>
        db.AnalysisTaskAttempts.FirstOrDefaultAsync(x => x.Id == attemptId && x.TaskId == taskId, ct);

    public Task<AnalysisResult?> GetCurrentResultAsync(Guid taskId, string resultType, CancellationToken ct) =>
        db.AnalysisResults.FirstOrDefaultAsync(x => x.TaskId == taskId && x.ResultType == resultType && x.IsCurrent, ct);

    public Task<AnalysisResult?> GetResultAsync(Guid taskId, Guid resultId, CancellationToken ct) =>
        db.AnalysisResults.FirstOrDefaultAsync(x => x.Id == resultId && x.TaskId == taskId, ct);

    public Task<GeneratedArtifact?> GetArtifactAsync(Guid taskId, Guid artifactId, CancellationToken ct) =>
        db.GeneratedArtifacts.FirstOrDefaultAsync(x => x.Id == artifactId && x.TaskId == taskId, ct);

    public void AddTask(AnalysisTask task) => db.AnalysisTasks.Add(task);
    public void AddAttempt(AnalysisTaskAttempt attempt) => db.AnalysisTaskAttempts.Add(attempt);
    public async Task ReplaceCurrentResultAsync(AnalysisResult? previous, AnalysisResult result, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (previous is not null)
        {
            previous.Supersede();
            await db.SaveChangesAsync(ct);
        }
        db.AnalysisResults.Add(result);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
    public void AddArtifact(GeneratedArtifact artifact) => db.GeneratedArtifacts.Add(artifact);
    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
