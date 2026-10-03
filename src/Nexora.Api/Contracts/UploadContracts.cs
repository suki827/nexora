using System.ComponentModel.DataAnnotations;
using Nexora.Application.MediaCatalog;

namespace Nexora.Api.Contracts;

public sealed class StartUploadRequest
{
    [Required, RegularExpression("^(original|subtitle|reference_script|attachment)$")]
    public string FileRole { get; init; } = "original";
    [Required, StringLength(255, MinimumLength = 1)]
    public string OriginalName { get; init; } = string.Empty;
    [StringLength(150)]
    public string? ContentType { get; init; }
    [Range(1, 20L * 1024 * 1024 * 1024)]
    public long TotalBytes { get; init; }
    [Range(1024 * 1024, 32 * 1024 * 1024)]
    public int ChunkSize { get; init; } = 8 * 1024 * 1024;
    [RegularExpression("^[0-9a-fA-F]{64}$")]
    public string? Sha256 { get; init; }
}

public sealed record UploadSessionResponse(
    Guid FileId, string Status, long? TotalBytes, int? ChunkSize,
    int? ChunkCount, IReadOnlyList<int>? UploadedChunks)
{
    public static UploadSessionResponse From(UploadSession session) => new(
        session.FileId, session.Status,
        session.Progress?.Manifest.TotalBytes,
        session.Progress?.Manifest.ChunkSize,
        session.Progress?.Manifest.ChunkCount,
        session.Progress?.UploadedChunks);
}
