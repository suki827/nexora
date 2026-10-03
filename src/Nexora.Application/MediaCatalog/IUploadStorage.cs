namespace Nexora.Application.MediaCatalog;

public sealed record UploadManifest(long TotalBytes, int ChunkSize, int ChunkCount, string? ExpectedSha256);
public sealed record UploadProgress(UploadManifest Manifest, IReadOnlyList<int> UploadedChunks);
public sealed record CompletedUpload(long SizeBytes, string Sha256);

public interface IUploadStorage
{
    Task CreateSessionAsync(Guid fileId, UploadManifest manifest, CancellationToken cancellationToken);
    Task<UploadProgress> GetProgressAsync(Guid fileId, CancellationToken cancellationToken);
    Task SaveChunkAsync(Guid fileId, int index, Stream content, CancellationToken cancellationToken);
    Task<CompletedUpload> CompleteAsync(Guid fileId, string storageKey, CancellationToken cancellationToken);
    Task DeleteSessionAsync(Guid fileId, CancellationToken cancellationToken);
    IReadOnlyList<Guid> FindExpiredSessions(TimeSpan olderThan);
    Stream OpenFile(string storageKey);
    Stream? OpenWaveform(string storageKey);
    Task SaveWaveformAsync(string storageKey, Stream content, CancellationToken cancellationToken);
    string GetFilePath(string storageKey);
}
