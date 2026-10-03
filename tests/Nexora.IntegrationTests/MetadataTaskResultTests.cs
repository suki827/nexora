using Nexora.Domain.Entities;
using Nexora.Worker;

namespace Nexora.IntegrationTests;

public sealed class MetadataTaskResultTests
{
    [Fact]
    public async Task Builds_result_from_configured_video()
    {
        var video = Environment.GetEnvironmentVariable("NEXORA_TEST_VIDEO");
        if (string.IsNullOrWhiteSpace(video)) return; // Optional external fixture.
        Assert.True(File.Exists(video), $"Video not found: {video}");

        using var probe = await MediaProbe.RunAsync("ffprobe", video, CancellationToken.None);
        var parsed = MediaProbe.Read(probe);
        var metadata = new MediaMetadata(Guid.NewGuid());
        metadata.CompleteProbe(parsed.DurationSeconds, parsed.Width, parsed.Height,
            parsed.FpsNum, parsed.FpsDen, parsed.FrameCount, null, parsed.VideoCodec,
            parsed.AudioCodec, parsed.AudioSampleRate, parsed.AudioChannels,
            parsed.FormatName, probe);
        using var result = MetadataTaskResult.Create(metadata);
        var payload = result.RootElement;

        Assert.Equal(960, payload.GetProperty("width").GetInt32());
        Assert.Equal(540, payload.GetProperty("height").GetInt32());
        Assert.Equal("h264", payload.GetProperty("videoCodec").GetString());
        Assert.Equal("aac", payload.GetProperty("audioCodec").GetString());
        Assert.True(payload.GetProperty("durationSeconds").GetDecimal() > 1400);
    }
}
