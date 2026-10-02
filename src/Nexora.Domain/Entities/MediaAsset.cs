namespace Nexora.Domain.Entities;

public sealed class MediaAsset
{
    public const string TypeVideo = "video";
    public const string TypeAudio = "audio";
    public const string TypeImage = "image";
    public const string TypeDocument = "document";
    public const string StatusActive = "active";
    public const string StatusArchived = "archived";

    private static readonly HashSet<string> AssetTypes =
        [TypeVideo, TypeAudio, TypeImage, TypeDocument];

    public Guid Id { get; private set; }
    public Guid OwnerId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string AssetType { get; private set; } = TypeVideo;
    public string Status { get; private set; } = StatusActive;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public ICollection<AssetFile> Files { get; private set; } = new List<AssetFile>();

    private MediaAsset() { }

    public MediaAsset(Guid ownerId, string name, string? description = null, string assetType = TypeVideo)
    {
        if (ownerId == Guid.Empty)
            throw new ArgumentException("Owner ID cannot be empty.", nameof(ownerId));

        OwnerId = ownerId;
        Id = Guid.NewGuid();
        Name = ValidateName(name);
        Description = description;
        AssetType = ValidateAssetType(assetType);
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public void Update(string name, string? description, string assetType)
    {
        EnsureNotDeleted();
        Name = ValidateName(name);
        Description = description;
        AssetType = ValidateAssetType(assetType);
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetStatus(string status)
    {
        EnsureNotDeleted();
        if (status is not (StatusActive or StatusArchived))
            throw new ArgumentException("Asset status must be 'active' or 'archived'.", nameof(status));

        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        if (DeletedAt.HasValue)
            return;

        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DeletedAt.Value;
    }

    private void EnsureNotDeleted()
    {
        if (DeletedAt.HasValue)
            throw new InvalidOperationException("A deleted asset cannot be changed.");
    }

    private static string ValidateName(string name)
    {
        var value = name?.Trim();
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Asset name is required.", nameof(name));
        if (value.Length > 255)
            throw new ArgumentException("Asset name cannot exceed 255 characters.", nameof(name));
        return value;
    }

    private static string ValidateAssetType(string assetType)
    {
        var value = assetType?.Trim().ToLowerInvariant();
        if (value is null || !AssetTypes.Contains(value))
            throw new ArgumentException("Unsupported asset type.", nameof(assetType));
        return value;
    }
}
