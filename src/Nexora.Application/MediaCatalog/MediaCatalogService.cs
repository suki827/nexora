using Nexora.Domain.Entities;

namespace Nexora.Application.MediaCatalog;

public sealed record PageResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public sealed record ProbeMetadata(
    decimal? DurationSeconds,
    int? Width,
    int? Height,
    int? FpsNum,
    int? FpsDen,
    long? FrameCount,
    bool? IsVariableFps,
    string? VideoCodec,
    string? AudioCodec,
    int? AudioSampleRate,
    int? AudioChannels,
    string? FormatName,
    System.Text.Json.JsonDocument? ProbeJson);

public sealed class MediaCatalogService(IMediaCatalogRepository repository)
{
    public async Task<PageResult<MediaAsset>> ListAssetsAsync(
        Guid ownerId, int page, int pageSize, bool includeArchived, CancellationToken cancellationToken)
    {
        ValidateOwner(ownerId);
        ValidatePage(page, pageSize);
        var skip = checked((page - 1) * pageSize);
        var items = await repository.ListAssetsAsync(ownerId, skip, pageSize, includeArchived, cancellationToken);
        var count = await repository.CountAssetsAsync(ownerId, includeArchived, cancellationToken);
        return new PageResult<MediaAsset>(items, page, pageSize, count);
    }

    public Task<MediaAsset?> GetAssetAsync(Guid ownerId, Guid assetId, CancellationToken cancellationToken)
    {
        ValidateOwner(ownerId);
        return repository.GetAssetAsync(ownerId, assetId, cancellationToken);
    }

    public async Task<MediaAsset> CreateAssetAsync(
        Guid ownerId, string name, string? description, string assetType, CancellationToken cancellationToken)
    {
        ValidateOwner(ownerId);
        var asset = new MediaAsset(ownerId, name, description, assetType);
        repository.AddAsset(asset);
        await repository.SaveChangesAsync(cancellationToken);
        return asset;
    }

    public async Task<MediaAsset?> UpdateAssetAsync(
        Guid ownerId, Guid assetId, string name, string? description, string assetType, string status,
        CancellationToken cancellationToken)
    {
        var asset = await GetAssetAsync(ownerId, assetId, cancellationToken);
        if (asset is null)
            return null;

        asset.Update(name, description, assetType);
        asset.SetStatus(status);
        await repository.SaveChangesAsync(cancellationToken);
        return asset;
    }

    public async Task<bool> DeleteAssetAsync(Guid ownerId, Guid assetId, CancellationToken cancellationToken)
    {
        var asset = await GetAssetAsync(ownerId, assetId, cancellationToken);
        if (asset is null)
            return false;

        asset.SoftDelete();
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PageResult<AssetFile>?> ListAssetFilesAsync(
        Guid ownerId, Guid assetId, int page, int pageSize, CancellationToken cancellationToken)
    {
        ValidatePage(page, pageSize);
        if (await GetAssetAsync(ownerId, assetId, cancellationToken) is null)
            return null;

        var skip = checked((page - 1) * pageSize);
        var items = await repository.ListAssetFilesAsync(ownerId, assetId, skip, pageSize, cancellationToken);
        var count = await repository.CountAssetFilesAsync(ownerId, assetId, cancellationToken);
        return new PageResult<AssetFile>(items, page, pageSize, count);
    }

    public async Task<AssetFile?> GetAssetFileAsync(
        Guid ownerId, Guid assetId, Guid fileId, CancellationToken cancellationToken) =>
        await repository.GetAssetFileAsync(ownerId, assetId, fileId, cancellationToken);

    public async Task<bool> DeleteAssetFileAsync(
        Guid ownerId, Guid assetId, Guid fileId, CancellationToken cancellationToken)
    {
        var file = await repository.GetAssetFileAsync(ownerId, assetId, fileId, cancellationToken);
        if (file is null)
            return false;

        file.SoftDelete();
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<MediaMetadata?> GetMetadataAsync(
        Guid ownerId, Guid assetId, Guid fileId, CancellationToken cancellationToken)
    {
        if (await repository.GetAssetFileAsync(ownerId, assetId, fileId, cancellationToken) is null)
            return null;
        return await repository.GetMetadataAsync(fileId, cancellationToken);
    }

    public async Task<MediaMetadata?> UpdateMetadataAsync(
        Guid ownerId, Guid assetId, Guid fileId, ProbeMetadata probe, CancellationToken cancellationToken)
    {
        if (await repository.GetAssetFileAsync(ownerId, assetId, fileId, cancellationToken) is null)
            return null;

        var metadata = await repository.GetMetadataAsync(fileId, cancellationToken);
        if (metadata is null)
        {
            metadata = new MediaMetadata(fileId);
            repository.AddMetadata(metadata);
        }

        metadata.CompleteProbe(
            probe.DurationSeconds, probe.Width, probe.Height, probe.FpsNum, probe.FpsDen,
            probe.FrameCount, probe.IsVariableFps, probe.VideoCodec, probe.AudioCodec,
            probe.AudioSampleRate, probe.AudioChannels, probe.FormatName, probe.ProbeJson);
        await repository.SaveChangesAsync(cancellationToken);
        return metadata;
    }

    public async Task<bool> RetryMetadataAsync(
        Guid ownerId, Guid assetId, Guid fileId, CancellationToken cancellationToken)
    {
        if (await repository.GetAssetFileAsync(ownerId, assetId, fileId, cancellationToken) is null)
            return false;
        var metadata = await repository.GetMetadataAsync(fileId, cancellationToken);
        if (metadata is null)
            return false;
        metadata.Retry();
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void ValidateOwner(Guid ownerId)
    {
        if (ownerId == Guid.Empty)
            throw new ArgumentException("Authenticated user ID is missing or invalid.", nameof(ownerId));
    }

    private static void ValidatePage(int page, int pageSize)
    {
        if (page < 1)
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than zero.");
        if (pageSize is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 1 and 100.");
        if (page > int.MaxValue / pageSize)
            throw new ArgumentOutOfRangeException(nameof(page), "Page is too large.");
    }
}
