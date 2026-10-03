using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace Nexora.Worker;

internal sealed record ProbeResult(
    decimal? DurationSeconds, int? Width, int? Height, int? FpsNum, int? FpsDen,
    long? FrameCount, string? VideoCodec, string? AudioCodec,
    int? AudioSampleRate, int? AudioChannels, string? FormatName, bool HasAudio);

internal static class MediaProbe
{
    public static async Task<JsonDocument> RunAsync(
        string executable, string filePath, CancellationToken cancellationToken)
    {
        var start = new ProcessStartInfo(executable)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in new[] { "-v", "error", "-of", "json", "-show_format", "-show_streams", filePath })
            start.ArgumentList.Add(argument);
        using var process = new Process { StartInfo = start };
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(5));
        process.Start();
        try
        {
            var output = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var error = process.StandardError.ReadToEndAsync(timeout.Token);
            await process.WaitForExitAsync(timeout.Token);
            var stderr = await error;
            if (process.ExitCode != 0)
                throw new InvalidDataException($"ffprobe failed: {stderr[..Math.Min(stderr.Length, 500)]}");
            return JsonDocument.Parse(await output);
        }
        finally
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
    }

    public static ProbeResult Read(JsonDocument document)
    {
        var root = document.RootElement;
        var format = root.TryGetProperty("format", out var formatValue) ? formatValue : default;
        var duration = ReadDecimal(format, "duration");
        var formatName = ReadString(format, "format_name");
        JsonElement video = default;
        JsonElement audio = default;
        if (root.TryGetProperty("streams", out var streams) && streams.ValueKind == JsonValueKind.Array)
        {
            foreach (var stream in streams.EnumerateArray())
            {
                var type = ReadString(stream, "codec_type");
                if (type == "video" && video.ValueKind == JsonValueKind.Undefined)
                    video = stream;
                else if (type == "audio" && audio.ValueKind == JsonValueKind.Undefined)
                    audio = stream;
            }
        }
        var (fpsNum, fpsDen) = ReadRate(video, "avg_frame_rate");
        if (fpsNum is null)
            (fpsNum, fpsDen) = ReadRate(video, "r_frame_rate");
        return new ProbeResult(
            duration ?? ReadDecimal(video, "duration") ?? ReadDecimal(audio, "duration"),
            ReadInt(video, "width"), ReadInt(video, "height"), fpsNum, fpsDen,
            ReadLong(video, "nb_frames"), ReadString(video, "codec_name"),
            ReadString(audio, "codec_name"), ReadInt(audio, "sample_rate"),
            ReadInt(audio, "channels"), formatName,
            audio.ValueKind != JsonValueKind.Undefined);
    }

    private static string? ReadString(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value)
            ? value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString()
            : null;

    private static int? ReadInt(JsonElement element, string name) =>
        int.TryParse(ReadString(element, name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value : null;

    private static long? ReadLong(JsonElement element, string name) =>
        long.TryParse(ReadString(element, name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value : null;

    private static decimal? ReadDecimal(JsonElement element, string name) =>
        decimal.TryParse(ReadString(element, name), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value : null;

    private static (int? Numerator, int? Denominator) ReadRate(JsonElement element, string name)
    {
        var pieces = ReadString(element, name)?.Split('/');
        if (pieces is { Length: 2 } &&
            int.TryParse(pieces[0], out var numerator) && numerator > 0 &&
            int.TryParse(pieces[1], out var denominator) && denominator > 0)
            return (numerator, denominator);
        return (null, null);
    }
}
