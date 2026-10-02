using Microsoft.EntityFrameworkCore;
using Nexora.Application.MediaCatalog;
using Nexora.Domain.Entities;

namespace Nexora.Infrastructure.Persistence;

public sealed class MediaCatalogRepository(NexoraDbContext dbContext) : IMediaCatalogRepository
{
    public async Task<IReadOnlyList<MediaAsset>> ListAssetsAsync(
        Guid ownerId, int skip, int take, bool includeArchived, CancellationToken cancellationToken)
    {
        var query = dbContext.MediaAssets
            .AsNoTracking()
            .Where(x => x.OwnerId == ownerId && x.DeletedAt == null);
        if (!includeArchived)
            query = query.Where(x => x.Status == MediaAsset.StatusActive);

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAssetsAsync(
        Guid ownerId, bool includeArchived, CancellationToken cancellationToken)
    {
        var query = dbContext.MediaAssets.Where(x => x.OwnerId == ownerId && x.DeletedAt == null);
        if (!includeArchived)
            query = query.Where(x => x.Status == MediaAsset.StatusActive);
        return query.CountAsync(cancellationToken);
    }

    public Task<MediaAsset?> GetAssetAsync(Guid ownerId, Guid assetId, CancellationToken cancellationToken) =>
        dbContext.MediaAssets.FirstOrDefaultAsync(
            x => x.OwnerId == ownerId && x.Id == assetId && x.DeletedAt == null,
            cancellationToken);

    public void AddAsset(MediaAsset asset) => dbContext.MediaAssets.Add(asset);

    public async Task<IReadOnlyList<AssetFile>> ListAssetFilesAsync(
        Guid ownerId, Guid assetId, int skip, int take, CancellationToken cancellationToken) =>
        await dbContext.AssetFiles
            .AsNoTracking()
            .Where(x => x.AssetId == assetId && x.Asset.OwnerId == ownerId &&
                        x.Asset.DeletedAt == null && x.DeletedAt == null)
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<int> CountAssetFilesAsync(Guid ownerId, Guid assetId, CancellationToken cancellationToken) =>
        dbContext.AssetFiles.CountAsync(
            x => x.AssetId == assetId && x.Asset.OwnerId == ownerId &&
                 x.Asset.DeletedAt == null && x.DeletedAt == null,
            cancellationToken);

    public Task<AssetFile?> GetAssetFileAsync(
        Guid ownerId, Guid assetId, Guid fileId, CancellationToken cancellationToken) =>
        dbContext.AssetFiles
            .Include(x => x.Metadata)
            .FirstOrDefaultAsync(
                x => x.Id == fileId && x.AssetId == assetId && x.Asset.OwnerId == ownerId &&
                     x.Asset.DeletedAt == null && x.DeletedAt == null,
                cancellationToken);

    public void AddAssetFile(AssetFile file) => dbContext.AssetFiles.Add(file);

    public Task<MediaMetadata?> GetMetadataAsync(Guid fileId, CancellationToken cancellationToken) =>
        dbContext.MediaMetadatas.FirstOrDefaultAsync(x => x.AssetFileId == fileId, cancellationToken);

    public void AddMetadata(MediaMetadata metadata) => dbContext.MediaMetadatas.Add(metadata);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
