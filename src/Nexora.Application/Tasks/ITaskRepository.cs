using Nexora.Domain.Entities;

namespace Nexora.Application.Tasks;

public interface ITaskRepository
{
    Task<AnalysisTask?> GetAsync(Guid ownerId, Guid taskId, CancellationToken cancellationToken);
    Task<AnalysisTask?> CancelOwnedAsync(Guid ownerId, Guid taskId, CancellationToken cancellationToken);
    Task<AnalysisTask?> GetForUpdateAsync(Guid taskId, CancellationToken cancellationToken);
    Task<AnalysisTask?> GetByIdempotencyKeyAsync(Guid ownerId, string key, CancellationToken cancellationToken);
    Task<IReadOnlyList<AnalysisTask>> ListAsync(Guid ownerId, int skip, int take, CancellationToken cancellationToken);
    Task<int> CountAsync(Guid ownerId, CancellationToken cancellationToken);
    Task<bool> OwnsReadyAssetFileAsync(Guid ownerId, Guid fileId, CancellationToken cancellationToken);
    Task<bool> OwnsReadyArtifactAsync(Guid ownerId, Guid artifactId, CancellationToken cancellationToken);
    Task<AnalysisTaskAttempt?> GetAttemptAsync(Guid taskId, Guid attemptId, CancellationToken cancellationToken);
    Task<AnalysisResult?> GetCurrentResultAsync(Guid taskId, string resultType, CancellationToken cancellationToken);
    Task<AnalysisResult?> GetResultAsync(Guid taskId, Guid resultId, CancellationToken cancellationToken);
    Task<GeneratedArtifact?> GetArtifactAsync(Guid taskId, Guid artifactId, CancellationToken cancellationToken);
    void AddTask(AnalysisTask task);
    void AddAttempt(AnalysisTaskAttempt attempt);
    Task ReplaceCurrentResultAsync(AnalysisResult? previous, AnalysisResult result, CancellationToken cancellationToken);
    void AddArtifact(GeneratedArtifact artifact);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
