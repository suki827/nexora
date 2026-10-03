using Microsoft.EntityFrameworkCore;
using Nexora.Application.MediaCatalog;
using Nexora.Domain.Entities;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Worker;

public sealed class Worker(
    IServiceScopeFactory scopeFactory,
    IUploadStorage storage,
    IConfiguration configuration,
    ILogger<Worker> logger) : BackgroundService
{
    private DateTime _nextCleanup = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOneAsync(stoppingToken);
                if (DateTime.UtcNow >= _nextCleanup)
                {
                    await CleanupExpiredUploadsAsync(stoppingToken);
                    _nextCleanup = DateTime.UtcNow.AddHours(1);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Media processing poll failed");
            }
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task CleanupExpiredUploadsAsync(CancellationToken cancellationToken)
    {
        var expired = storage.FindExpiredSessions(TimeSpan.FromDays(7));
        if (expired.Count == 0)
            return;
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NexoraDbContext>();
        foreach (var fileId in expired)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var file = await db.AssetFiles.FirstOrDefaultAsync(x => x.Id == fileId, cancellationToken);
            if (file is { Status: AssetFile.StatusUploading })
            {
                file.SoftDelete();
                await db.SaveChangesAsync(cancellationToken);
            }
            await storage.DeleteSessionAsync(fileId, cancellationToken);
            logger.LogInformation("Removed expired upload session {FileId}", fileId);
        }
    }

    private async Task ProcessOneAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NexoraDbContext>();
        var staleBefore = DateTime.UtcNow.AddMinutes(-30);
        var metadata = await db.MediaMetadatas
            .Include(x => x.AssetFile)
            .Where(x => x.AssetFile.Status == AssetFile.StatusReady &&
                        x.AssetFile.DeletedAt == null &&
                        x.AssetFile.StorageProvider == AssetFile.ProviderLocal &&
                        (x.Status == MediaMetadata.StatusPending ||
                         (x.Status == MediaMetadata.StatusProcessing && x.UpdatedAt < staleBefore)))
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (metadata is null)
            return;

        metadata.BeginProcessing();
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var filePath = storage.GetFilePath(metadata.AssetFile.StorageKey);
            using var probe = await MediaProbe.RunAsync(
                configuration["MediaProcessing:FfprobePath"] ?? "ffprobe",
                filePath, cancellationToken);
            var result = MediaProbe.Read(probe);
            if (result.HasAudio)
            {
                await using var waveform = await WaveformGenerator.GenerateAsync(
                    configuration["MediaProcessing:FfmpegPath"] ?? "ffmpeg",
                    filePath, result.DurationSeconds, cancellationToken);
                await storage.SaveWaveformAsync(metadata.AssetFile.StorageKey, waveform, cancellationToken);
            }
            metadata.CompleteProbe(
                result.DurationSeconds, result.Width, result.Height,
                result.FpsNum, result.FpsDen, result.FrameCount,
                null, result.VideoCodec, result.AudioCodec,
                result.AudioSampleRate, result.AudioChannels,
                result.FormatName, probe);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Processed media file {FileId}", metadata.AssetFileId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Media processing failed for file {FileId}", metadata.AssetFileId);
            metadata.MarkFailed("media_processing_failed", exception.Message);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
