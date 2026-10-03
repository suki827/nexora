using Nexora.Domain.Entities;

namespace Nexora.Application.MediaCatalog;

public sealed record StartUpload(
    string FileRole, string OriginalName, string? ContentType,
    long TotalBytes, int ChunkSize, string? Sha256);

public sealed record UploadSession(Guid FileId, string Status, UploadProgress? Progress);

public sealed class UploadService(IMediaCatalogRepository repository, IUploadStorage storage)
{
    private static readonly HashSet<string> PreviewContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "video/mp4", "video/webm", "video/ogg", "audio/mpeg", "audio/mp4",
        "audio/wav", "audio/ogg", "audio/webm", "image/jpeg", "image/png", "image/webp"
    };

    public async Task<UploadSession?> StartAsync(
        Guid ownerId, Guid assetId, StartUpload request, CancellationToken cancellationToken)
    {
        if (await repository.GetAssetAsync(ownerId, assetId, cancellationToken) is null)
            return null;
        if (request.TotalBytes <= 0 || request.TotalBytes > 20L * 1024 * 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(request.TotalBytes), "File size must be between 1 byte and 20 GiB.");
        if (request.ChunkSize is < 1024 * 1024 or > 32 * 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(request.ChunkSize), "Chunk size must be between 1 and 32 MiB.");

        var chunkCount = checked((int)((request.TotalBytes - 1) / request.ChunkSize + 1));
        var file = new AssetFile(
            assetId, request.FileRole, request.OriginalName, AssetFile.ProviderLocal,
            null, Guid.NewGuid().ToString("N"), request.ContentType,
            request.TotalBytes, request.Sha256);
        file.BeginUpload();
        var manifest = new UploadManifest(request.TotalBytes, request.ChunkSize, chunkCount, file.ContentHash);
        await storage.CreateSessionAsync(file.Id, manifest, cancellationToken);
        try
        {
            repository.AddAssetFile(file);
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await storage.DeleteSessionAsync(file.Id, CancellationToken.None);
            throw;
        }
        return new UploadSession(file.Id, file.Status, new UploadProgress(manifest, []));
    }

    public async Task<UploadSession?> GetAsync(
        Guid ownerId, Guid assetId, Guid fileId, CancellationToken cancellationToken)
    {
        var file = await repository.GetAssetFileAsync(ownerId, assetId, fileId, cancellationToken);
        if (file is null)
            return null;
        var progress = file.Status == AssetFile.StatusUploading
            ? await storage.GetProgressAsync(fileId, cancellationToken)
            : null;
        return new UploadSession(fileId, file.Status, progress);
    }

    public async Task<bool> UploadChunkAsync(
        Guid ownerId, Guid assetId, Guid fileId, int index, Stream content, CancellationToken cancellationToken)
    {
        var file = await repository.GetAssetFileAsync(ownerId, assetId, fileId, cancellationToken);
        if (file is null)
            return false;
        if (file.Status != AssetFile.StatusUploading)
            throw new InvalidOperationException("File is not accepting chunks.");
        await storage.SaveChunkAsync(fileId, index, content, cancellationToken);
        return true;
    }

    public async Task<AssetFile?> CompleteAsync(
        Guid ownerId, Guid assetId, Guid fileId, CancellationToken cancellationToken)
    {
        var file = await repository.GetAssetFileAsync(ownerId, assetId, fileId, cancellationToken);
        if (file is null)
            return null;
        if (file.Status == AssetFile.StatusReady)
            return file;
        if (file.Status != AssetFile.StatusUploading)
            throw new InvalidOperationException("File cannot be completed in its current state.");

        var completed = await storage.CompleteAsync(fileId, file.StorageKey, cancellationToken);
        file.MarkReady(completed.SizeBytes, completed.Sha256);
        if (await repository.GetMetadataAsync(fileId, cancellationToken) is null)
            repository.AddMetadata(new MediaMetadata(fileId));
        await repository.SaveChangesAsync(cancellationToken);
        await storage.DeleteSessionAsync(fileId, cancellationToken);
        return file;
    }

    public async Task<bool> CancelAsync(
        Guid ownerId, Guid assetId, Guid fileId, CancellationToken cancellationToken)
    {
        var file = await repository.GetAssetFileAsync(ownerId, assetId, fileId, cancellationToken);
        if (file is null)
            return false;
        if (file.Status != AssetFile.StatusUploading)
            throw new InvalidOperationException("Only an active upload can be cancelled.");
        file.SoftDelete();
        await repository.SaveChangesAsync(cancellationToken);
        await storage.DeleteSessionAsync(fileId, cancellationToken);
        return true;
    }

    public async Task<(Stream Content, string ContentType, string FileName)?> OpenFileAsync(
        Guid ownerId, Guid assetId, Guid fileId, CancellationToken cancellationToken)
    {
        var file = await repository.GetAssetFileAsync(ownerId, assetId, fileId, cancellationToken);
        if (file is null || file.Status != AssetFile.StatusReady || file.StorageProvider != AssetFile.ProviderLocal)
            return null;
        var contentType = file.ContentType is not null && PreviewContentTypes.Contains(file.ContentType)
            ? file.ContentType : "application/octet-stream";
        return (storage.OpenFile(file.StorageKey), contentType, file.OriginalName);
    }

    public async Task<Stream?> OpenWaveformAsync(
        Guid ownerId, Guid assetId, Guid fileId, CancellationToken cancellationToken)
    {
        var file = await repository.GetAssetFileAsync(ownerId, assetId, fileId, cancellationToken);
        if (file is null || file.Status != AssetFile.StatusReady || file.StorageProvider != AssetFile.ProviderLocal)
            return null;
        return storage.OpenWaveform(file.StorageKey);
    }
}
