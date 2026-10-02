using Nexora.Domain.Entities;

namespace Nexora.Application.MediaCatalog;

public interface IMediaCatalogRepository
{
    Task<IReadOnlyList<MediaAsset>> ListAssetsAsync(Guid ownerId, int skip, int take, bool includeArchived, CancellationToken cancellationToken);
    Task<int> CountAssetsAsync(Guid ownerId, bool includeArchived, CancellationToken cancellationToken);
    Task<MediaAsset?> GetAssetAsync(Guid ownerId, Guid assetId, CancellationToken cancellationToken);
    void AddAsset(MediaAsset asset);

    Task<IReadOnlyList<AssetFile>> ListAssetFilesAsync(Guid ownerId, Guid assetId, int skip, int take, CancellationToken cancellationToken);
    Task<int> CountAssetFilesAsync(Guid ownerId, Guid assetId, CancellationToken cancellationToken);
    Task<AssetFile?> GetAssetFileAsync(Guid ownerId, Guid assetId, Guid fileId, CancellationToken cancellationToken);
    void AddAssetFile(AssetFile file);

    Task<MediaMetadata?> GetMetadataAsync(Guid fileId, CancellationToken cancellationToken);
    void AddMetadata(MediaMetadata metadata);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
