namespace Nexora.Domain.Entities;

public class MediaAsset
{
    public Guid Id { get; private set; }

    public Guid OwnerId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public DateTime? DeletedAt { get; private set; }

    public ICollection<AssetFile> Files { get; private set; }
        = new List<AssetFile>();

    private MediaAsset()
    {
        // EF Core
    }

    public MediaAsset(Guid ownerId, string name)
    {
        if (ownerId == Guid.Empty)
            throw new ArgumentException(
                "Owner ID cannot be empty.", nameof(ownerId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Asset name is required.", nameof(name));

        Id = Guid.NewGuid();
        OwnerId = ownerId;
        Name = name.Trim();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Asset name is required.", nameof(name));

        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        if (DeletedAt.HasValue)
            return;

        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DeletedAt.Value;
    }
}