using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Nexora.Application.MediaCatalog;
using Nexora.Domain.Entities;

namespace Nexora.Api.Contracts;

public sealed class CreateMediaAssetRequest
{
    [Required, StringLength(255, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    [Required, RegularExpression("^(video|audio|image|document)$")]
    public string AssetType { get; init; } = MediaAsset.TypeVideo;
}

public sealed class UpdateMediaAssetRequest
{
    [Required, StringLength(255, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    [Required, RegularExpression("^(video|audio|image|document)$")]
    public string AssetType { get; init; } = MediaAsset.TypeVideo;
    [Required, RegularExpression("^(active|archived)$")]
    public string Status { get; init; } = MediaAsset.StatusActive;
}

public sealed class CreateAssetFileRequest
{
    [Required, RegularExpression("^(original|subtitle|reference_script|attachment)$")]
    public string FileRole { get; init; } = AssetFile.RoleOriginal;
    [Required, StringLength(255, MinimumLength = 1)]
    public string OriginalName { get; init; } = string.Empty;
    [Required, RegularExpression("^(local|swift|s3)$")]
    public string StorageProvider { get; init; } = AssetFile.ProviderLocal;
    [StringLength(255)]
    public string? StorageBucket { get; init; }
    [Required, MinLength(1)]
    public string StorageKey { get; init; } = string.Empty;
    [StringLength(150)]
    public string? ContentType { get; init; }
    [Range(0, long.MaxValue)]
    public long? SizeBytes { get; init; }
    [RegularExpression("^[0-9a-fA-F]{64}$")]
    public string? ContentHash { get; init; }
}

public sealed class UpdateMediaMetadataRequest
{
    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal? DurationSeconds { get; init; }
    [Range(1, int.MaxValue)]
    public int? Width { get; init; }
    [Range(1, int.MaxValue)]
    public int? Height { get; init; }
    [Range(1, int.MaxValue)]
    public int? FpsNum { get; init; }
    [Range(1, int.MaxValue)]
    public int? FpsDen { get; init; }
    [Range(0, long.MaxValue)]
    public long? FrameCount { get; init; }
    public bool? IsVariableFps { get; init; }
    [StringLength(100)]
    public string? VideoCodec { get; init; }
    [StringLength(100)]
    public string? AudioCodec { get; init; }
    [Range(1, int.MaxValue)]
    public int? AudioSampleRate { get; init; }
    [Range(1, int.MaxValue)]
    public int? AudioChannels { get; init; }
    [StringLength(100)]
    public string? FormatName { get; init; }
    public JsonElement? ProbeJson { get; init; }
}

public sealed record PageResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public sealed record MediaAssetResponse(
    Guid Id, string Name, string? Description, string AssetType, string Status,
    DateTime CreatedAt, DateTime UpdatedAt);

public sealed record AssetFileResponse(
    Guid Id, Guid AssetId, string FileRole, string OriginalName, string StorageProvider,
    string? StorageBucket, string StorageKey, string? ContentType, long? SizeBytes,
    string? ContentHash, string Status, string? ErrorCode, string? ErrorMessage,
    DateTime CreatedAt, DateTime UpdatedAt, DateTime? UploadedAt, DateTime? DeletedAt);

public sealed record MediaMetadataResponse(
    Guid Id, Guid AssetFileId, string Status, decimal? DurationSeconds, int? Width,
    int? Height, int? FpsNum, int? FpsDen, long? FrameCount, bool? IsVariableFps,
    string? VideoCodec, string? AudioCodec, int? AudioSampleRate, int? AudioChannels,
    string? FormatName, JsonDocument? ProbeJson, string? ErrorCode,
    string? ErrorMessage, DateTime? ProbedAt, DateTime CreatedAt, DateTime UpdatedAt);

internal static class MediaCatalogResponseMapper
{
    public static MediaAssetResponse ToResponse(this MediaAsset asset) => new(
        asset.Id, asset.Name, asset.Description, asset.AssetType, asset.Status,
        asset.CreatedAt, asset.UpdatedAt);

    public static AssetFileResponse ToResponse(this AssetFile file) => new(
        file.Id, file.AssetId, file.FileRole, file.OriginalName, file.StorageProvider,
        file.StorageBucket, file.StorageKey, file.ContentType, file.SizeBytes,
        file.ContentHash, file.Status, file.ErrorCode, file.ErrorMessage,
        file.CreatedAt, file.UpdatedAt, file.UploadedAt, file.DeletedAt);

    public static MediaMetadataResponse ToResponse(this MediaMetadata metadata) => new(
        metadata.Id, metadata.AssetFileId, metadata.Status, metadata.DurationSeconds,
        metadata.Width, metadata.Height, metadata.FpsNum, metadata.FpsDen,
        metadata.FrameCount, metadata.IsVariableFps, metadata.VideoCodec,
        metadata.AudioCodec, metadata.AudioSampleRate, metadata.AudioChannels,
        metadata.FormatName, metadata.ProbeJson, metadata.ErrorCode,
        metadata.ErrorMessage, metadata.ProbedAt, metadata.CreatedAt, metadata.UpdatedAt);

    public static PageResponse<TOut> ToResponse<TIn, TOut>(
        this PageResult<TIn> page, Func<TIn, TOut> map) =>
        new(page.Items.Select(map).ToArray(), page.Page, page.PageSize, page.TotalCount);
}
