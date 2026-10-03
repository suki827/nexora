using System.Text.Json;
using Nexora.Worker;

namespace Nexora.IntegrationTests;

public sealed class MediaProbeTests
{
    [Fact]
    public void Reads_video_and_audio_stream_metadata()
    {
        using var document = JsonDocument.Parse("""
            {
              "format": { "duration": "12.5", "format_name": "mov,mp4,m4a,3gp,3g2,mj2" },
              "streams": [
                { "codec_type": "video", "codec_name": "h264", "width": 1920,
                  "height": 1080, "avg_frame_rate": "30000/1001", "nb_frames": "375" },
                { "codec_type": "audio", "codec_name": "aac", "sample_rate": "48000",
                  "channels": 2 }
              ]
            }
            """);

        var result = MediaProbe.Read(document);

        Assert.Equal(12.5m, result.DurationSeconds);
        Assert.Equal(1920, result.Width);
        Assert.Equal(1080, result.Height);
        Assert.Equal(30000, result.FpsNum);
        Assert.Equal(1001, result.FpsDen);
        Assert.Equal(375, result.FrameCount);
        Assert.Equal("h264", result.VideoCodec);
        Assert.Equal("aac", result.AudioCodec);
        Assert.Equal(48000, result.AudioSampleRate);
        Assert.Equal(2, result.AudioChannels);
        Assert.True(result.HasAudio);
    }
}
