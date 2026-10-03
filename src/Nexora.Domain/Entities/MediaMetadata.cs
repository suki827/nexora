using System.Text.Json;

namespace Nexora.Domain.Entities;

public sealed class MediaMetadata
{
    public const string StatusPending = "pending";
    public const string StatusProcessing = "processing";
    public const string StatusCompleted = "completed";
    public const string StatusFailed = "failed";

    public Guid Id { get; private set; }
    public Guid AssetFileId { get; private set; }
    public string Status { get; private set; } = StatusPending;
    public decimal? DurationSeconds { get; private set; }
    public int? Width { get; private set; }
    public int? Height { get; private set; }
    public int? FpsNum { get; private set; }
    public int? FpsDen { get; private set; }
    public long? FrameCount { get; private set; }
    public bool? IsVariableFps { get; private set; }
    public string? VideoCodec { get; private set; }
    public string? AudioCodec { get; private set; }
    public int? AudioSampleRate { get; private set; }
    public int? AudioChannels { get; private set; }
    public string? FormatName { get; private set; }
    public JsonDocument? ProbeJson { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime? ProbedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public AssetFile AssetFile { get; private set; } = null!;

    private MediaMetadata() { }

    public MediaMetadata(Guid assetFileId)
    {
        if (assetFileId == Guid.Empty)
            throw new ArgumentException("Asset file ID cannot be empty.", nameof(assetFileId));

        Id = Guid.NewGuid();
        AssetFileId = assetFileId;
        Status = StatusPending;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public void BeginProcessing()
    {
        Status = StatusProcessing;
        ErrorCode = null;
        ErrorMessage = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Retry()
    {
        if (Status != StatusFailed)
            throw new InvalidOperationException("Only failed metadata can be retried.");
        Status = StatusPending;
        ErrorCode = null;
        ErrorMessage = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void CompleteProbe(
        decimal? durationSeconds,
        int? width,
        int? height,
        int? fpsNum,
        int? fpsDen,
        long? frameCount,
        bool? isVariableFps,
        string? videoCodec,
        string? audioCodec,
        int? audioSampleRate,
        int? audioChannels,
        string? formatName,
        JsonDocument? probeJson)
    {
        ValidateMeasurements(durationSeconds, width, height, fpsNum, fpsDen, frameCount, audioSampleRate, audioChannels);
        ValidateJsonObject(probeJson);

        DurationSeconds = durationSeconds;
        Width = width;
        Height = height;
        FpsNum = fpsNum;
        FpsDen = fpsDen;
        FrameCount = frameCount;
        IsVariableFps = isVariableFps;
        VideoCodec = ValidateOptionalText(videoCodec, 100, nameof(videoCodec));
        AudioCodec = ValidateOptionalText(audioCodec, 100, nameof(audioCodec));
        AudioSampleRate = audioSampleRate;
        AudioChannels = audioChannels;
        FormatName = ValidateOptionalText(formatName, 100, nameof(formatName));
        ProbeJson = probeJson is null
            ? null
            : JsonDocument.Parse(probeJson.RootElement.GetRawText());
        Status = StatusCompleted;
        ProbedAt = DateTime.UtcNow;
        ErrorCode = null;
        ErrorMessage = null;
        UpdatedAt = ProbedAt.Value;
    }

    public void MarkFailed(string errorCode, string? errorMessage)
    {
        ErrorCode = ValidateOptionalText(errorCode, 100, nameof(errorCode));
        if (string.IsNullOrWhiteSpace(ErrorCode))
            throw new ArgumentException("An error code is required.", nameof(errorCode));
        ErrorMessage = errorMessage;
        Status = StatusFailed;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateMeasurements(
        decimal? durationSeconds,
        int? width,
        int? height,
        int? fpsNum,
        int? fpsDen,
        long? frameCount,
        int? audioSampleRate,
        int? audioChannels)
    {
        if (durationSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(durationSeconds));
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be positive when provided.");
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be positive when provided.");
        if (frameCount < 0)
            throw new ArgumentOutOfRangeException(nameof(frameCount));
        if ((fpsNum.HasValue != fpsDen.HasValue) || (fpsNum <= 0) || (fpsDen <= 0))
            throw new ArgumentException("Frame rate numerator and denominator must both be positive or both be omitted.");
        if (audioSampleRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(audioSampleRate));
        if (audioChannels <= 0)
            throw new ArgumentOutOfRangeException(nameof(audioChannels));
    }

    private static void ValidateJsonObject(JsonDocument? document)
    {
        if (document is not null && document.RootElement.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Probe JSON must be a JSON object.", nameof(document));
    }

    private static string? ValidateOptionalText(string? value, int maxLength, string parameterName)
    {
        if (value is null)
            return null;
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new ArgumentException($"Value cannot exceed {maxLength} characters.", parameterName);
        return normalized.Length == 0 ? null : normalized;
    }
}
