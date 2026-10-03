using System.Text.Json;
using Nexora.Domain.Entities;

namespace Nexora.Worker;

internal static class MetadataTaskResult
{
    public const string ResultType = "technical_metadata";
    public const string SchemaVersion = "1.0";

    public static JsonDocument Create(MediaMetadata metadata)
    {
        if (metadata.Status != MediaMetadata.StatusCompleted)
            throw new InvalidOperationException("Media metadata is not complete.");

        return JsonSerializer.SerializeToDocument(new
        {
            assetFileId = metadata.AssetFileId,
            durationSeconds = metadata.DurationSeconds,
            width = metadata.Width,
            height = metadata.Height,
            fpsNumerator = metadata.FpsNum,
            fpsDenominator = metadata.FpsDen,
            frameCount = metadata.FrameCount,
            variableFrameRate = metadata.IsVariableFps,
            videoCodec = metadata.VideoCodec,
            audioCodec = metadata.AudioCodec,
            audioSampleRate = metadata.AudioSampleRate,
            audioChannels = metadata.AudioChannels,
            formatName = metadata.FormatName,
            probedAt = metadata.ProbedAt
        });
    }
}
