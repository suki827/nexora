using System.Buffers.Binary;
using System.Diagnostics;
using System.Text.Json;

namespace Nexora.Worker;

internal static class WaveformGenerator
{
    private const int SampleRate = 8000;
    private const int TargetBuckets = 2000;

    public static async Task<MemoryStream> GenerateAsync(
        string executable, string filePath, decimal? durationSeconds, CancellationToken cancellationToken)
    {
        var samplesPerBucket = durationSeconds is > 0
            ? Math.Max(1L, (long)Math.Ceiling((double)durationSeconds.Value * SampleRate / TargetBuckets))
            : SampleRate;
        var start = new ProcessStartInfo(executable)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in new[]
                 { "-v", "error", "-i", filePath, "-map", "0:a:0", "-vn", "-ac", "1", "-ar", "8000", "-f", "s16le", "pipe:1" })
            start.ArgumentList.Add(argument);
        using var process = new Process { StartInfo = start };
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(30));
        process.Start();
        try
        {
            var error = process.StandardError.ReadToEndAsync(timeout.Token);
            var accumulator = new PeakAccumulator(samplesPerBucket);
            var buffer = new byte[64 * 1024 + 1];
            var carry = 0;
            int read;
            while ((read = await process.StandardOutput.BaseStream.ReadAsync(
                       buffer.AsMemory(carry, buffer.Length - carry), timeout.Token)) != 0)
            {
                var bytes = read + carry;
                var evenBytes = bytes & ~1;
                for (var offset = 0; offset < evenBytes; offset += 2)
                {
                    var sample = BinaryPrimitives.ReadInt16LittleEndian(buffer.AsSpan(offset, 2));
                    accumulator.Add(sample);
                }
                carry = bytes - evenBytes;
                if (carry == 1)
                    buffer[0] = buffer[evenBytes];
            }
            if (carry != 0)
                throw new InvalidDataException("Decoded audio contains an incomplete sample.");
            var peaks = accumulator.Finish();
            await process.WaitForExitAsync(timeout.Token);
            var stderr = await error;
            if (process.ExitCode != 0)
                throw new InvalidDataException($"ffmpeg failed: {stderr[..Math.Min(stderr.Length, 500)]}");

            var result = new MemoryStream();
            await JsonSerializer.SerializeAsync(result, new
            {
                schemaVersion = 1,
                sampleRate = SampleRate,
                samplesPerBucket,
                peaks
            }, cancellationToken: timeout.Token);
            result.Position = 0;
            return result;
        }
        finally
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
    }
}

internal sealed class PeakAccumulator(long samplesPerBucket)
{
    private readonly List<float[]> _peaks = [];
    private long _samples;
    private short _minimum = short.MaxValue;
    private short _maximum = short.MinValue;

    public void Add(short sample)
    {
        _minimum = Math.Min(_minimum, sample);
        _maximum = Math.Max(_maximum, sample);
        if (++_samples == samplesPerBucket)
            Flush();
    }

    public IReadOnlyList<float[]> Finish()
    {
        if (_samples > 0)
            Flush();
        return _peaks;
    }

    private void Flush()
    {
        _peaks.Add([_minimum / 32768f, _maximum / 32768f]);
        if (_peaks.Count > 100_000)
            throw new InvalidDataException("Waveform exceeds the supported length.");
        _samples = 0;
        _minimum = short.MaxValue;
        _maximum = short.MinValue;
    }
}
