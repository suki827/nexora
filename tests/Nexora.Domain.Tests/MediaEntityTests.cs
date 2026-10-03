using System.Text.Json;
using Nexora.Domain.Entities;

namespace Nexora.Domain.Tests;

public class MediaEntityTests
{
    [Fact]
    public void MediaAsset_TrimsNameAndSoftDeletesOnce()
    {
        var asset = new MediaAsset(Guid.NewGuid(), "  Demo  ", assetType: "VIDEO");

        Assert.Equal("Demo", asset.Name);
        Assert.Equal(MediaAsset.TypeVideo, asset.AssetType);
        Assert.Equal(MediaAsset.StatusActive, asset.Status);

        asset.SetStatus(MediaAsset.StatusArchived);
        asset.SoftDelete();
        var deletedAt = asset.DeletedAt;
        asset.SoftDelete();

        Assert.Equal(MediaAsset.StatusArchived, asset.Status);
        Assert.NotNull(deletedAt);
        Assert.Equal(deletedAt, asset.DeletedAt);
        Assert.Throws<InvalidOperationException>(() => asset.SetStatus(MediaAsset.StatusActive));
    }

    [Fact]
    public void MediaAsset_RejectsUnknownTypesAndBlankNames()
    {
        Assert.Throws<ArgumentException>(() => new MediaAsset(Guid.NewGuid(), " "));
        Assert.Throws<ArgumentException>(() => new MediaAsset(Guid.NewGuid(), "Video", assetType: "archive"));
    }

    [Fact]
    public void AssetFile_ValidatesStorageAndTransitionsToReady()
    {
        var file = NewLocalFile();

        file.BeginUpload();
        file.MarkReady(12, new string('A', 64));

        Assert.Equal(AssetFile.StatusReady, file.Status);
        Assert.Equal(12, file.SizeBytes);
        Assert.Equal(new string('a', 64), file.ContentHash);
        Assert.NotNull(file.UploadedAt);
        Assert.Throws<ArgumentException>(() => new AssetFile(
            Guid.NewGuid(), AssetFile.RoleOriginal, "clip.mp4", AssetFile.ProviderS3,
            null, "clips/clip.mp4", null, null, null));
    }

    [Fact]
    public void AssetFile_DeletionSetsRequiredDeletedAt()
    {
        var file = NewLocalFile();

        file.SoftDelete();

        Assert.Equal(AssetFile.StatusDeleted, file.Status);
        Assert.NotNull(file.DeletedAt);
        Assert.Throws<InvalidOperationException>(() => file.BeginUpload());
    }

    [Fact]
    public void MediaMetadata_StoresFullProbeAndRejectsInvalidMeasurements()
    {
        var metadata = new MediaMetadata(Guid.NewGuid());
        using var probe = JsonDocument.Parse("""{"streams": []}""");
        metadata.CompleteProbe(
            12.5m, 1920, 1080, 30000, 1001, 360, false,
            "h264", "aac", 48000, 2, "mov,mp4", probe);

        Assert.Equal(MediaMetadata.StatusCompleted, metadata.Status);
        Assert.Equal(30000, metadata.FpsNum);
        Assert.Equal(1001, metadata.FpsDen);
        Assert.Equal(JsonValueKind.Object, metadata.ProbeJson?.RootElement.ValueKind);
        Assert.Throws<ArgumentException>(() => metadata.CompleteProbe(
            null, null, null, 30, null, null, null,
            null, null, null, null, null, null));
        Assert.Throws<ArgumentException>(() => metadata.CompleteProbe(
            null, null, null, null, null, null, null,
            null, null, null, null, null,
            JsonDocument.Parse("[]")));
    }

    private static AssetFile NewLocalFile() => new(
        Guid.NewGuid(), AssetFile.RoleOriginal, "clip.mp4", AssetFile.ProviderLocal,
        null, "clips/clip.mp4", "video/mp4", null, null);
}
