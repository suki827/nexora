namespace Nexora.Domain.Entities;

public class MediaMetadata
{
    public Guid Id { get; private set; }

    public Guid AssetFileId { get; private set; }

    public double? DurationSeconds { get; private set; }

    public int? Width { get; private set; }

    public int? Height { get; private set; }

    public string? VideoCodec { get; private set; }

    public string? AudioCodec { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public AssetFile AssetFile { get; private set; } = null!;

    private MediaMetadata()
    {
    }

    public MediaMetadata(Guid assetFileId)
    {
        if (assetFileId == Guid.Empty)
            throw new ArgumentException(
                "Asset file ID cannot be empty.",
                nameof(assetFileId));

        Id = Guid.NewGuid();
        AssetFileId = assetFileId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(
        double? durationSeconds,
        int? width,
        int? height,
        string? videoCodec,
        string? audioCodec)
    {
        if (durationSeconds < 0 || width < 0 || height < 0)
            throw new ArgumentOutOfRangeException(
                nameof(durationSeconds),
                "Media measurements cannot be negative.");

        DurationSeconds = durationSeconds;
        Width = width;
        Height = height;
        VideoCodec = videoCodec;
        AudioCodec = audioCodec;
        UpdatedAt = DateTime.UtcNow;
    }
}