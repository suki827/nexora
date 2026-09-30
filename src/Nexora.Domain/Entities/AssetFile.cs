namespace Nexora.Domain.Entities;

public class AssetFile
{
    public Guid Id { get; private set; }

    public Guid AssetId { get; private set; }

    public string OriginalFilename { get; private set; }
        = string.Empty;

    public string StorageKey { get; private set; }
        = string.Empty;

    public string? ContentType { get; private set; }

    public long? SizeBytes { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? DeletedAt { get; private set; }

    public MediaAsset Asset { get; private set; } = null!;

    public MediaMetadata? Metadata { get; private set; }

    private AssetFile()
    {
    }

    public AssetFile(
        Guid assetId,
        string originalFilename,
        string storageKey,
        string? contentType,
        long? sizeBytes)
    {
        if (assetId == Guid.Empty)
            throw new ArgumentException(
                "Asset ID cannot be empty.", nameof(assetId));

        if (string.IsNullOrWhiteSpace(originalFilename))
            throw new ArgumentException(
                "Filename is required.", nameof(originalFilename));

        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException(
                "Storage key is required.", nameof(storageKey));

        if (sizeBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(sizeBytes));

        Id = Guid.NewGuid();
        AssetId = assetId;
        OriginalFilename = originalFilename;
        StorageKey = storageKey;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        CreatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        DeletedAt ??= DateTime.UtcNow;
    }
}