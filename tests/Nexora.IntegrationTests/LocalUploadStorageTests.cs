using System.Security.Cryptography;
using System.Text;
using Nexora.Application.MediaCatalog;
using Nexora.Infrastructure.Storage;

namespace Nexora.IntegrationTests;

public sealed class LocalUploadStorageTests
{
    [Fact]
    public async Task Chunks_can_resume_and_complete_with_expected_hash()
    {
        var root = Path.Combine(Path.GetTempPath(), "nexora-upload-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var store = new LocalUploadStorage(root);
            var fileId = Guid.NewGuid();
            var bytes = Encoding.UTF8.GetBytes("abcdefgh");
            var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            await store.CreateSessionAsync(fileId, new UploadManifest(8, 4, 2, hash), CancellationToken.None);

            await store.SaveChunkAsync(fileId, 1, new MemoryStream(bytes[4..]), CancellationToken.None);
            var resumed = new LocalUploadStorage(root);
            var progress = await resumed.GetProgressAsync(fileId, CancellationToken.None);
            Assert.Equal([1], progress.UploadedChunks);

            await resumed.SaveChunkAsync(fileId, 0, new MemoryStream(bytes[..4]), CancellationToken.None);
            await resumed.SaveChunkAsync(fileId, 0, new MemoryStream(bytes[..4]), CancellationToken.None);
            var key = Guid.NewGuid().ToString("N");
            var result = await resumed.CompleteAsync(fileId, key, CancellationToken.None);
            Assert.Equal(8, result.SizeBytes);
            Assert.Equal(hash, result.Sha256);
            await using var stored = resumed.OpenFile(key);
            using var copied = new MemoryStream();
            await stored.CopyToAsync(copied);
            Assert.Equal(bytes, copied.ToArray());
        }
        finally
        {
            var absoluteRoot = Path.GetFullPath(root);
            var expectedParent = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "nexora-upload-tests"))
                + Path.DirectorySeparatorChar;
            if (!absoluteRoot.StartsWith(expectedParent, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Test cleanup path is outside its dedicated directory.");
            if (Directory.Exists(absoluteRoot))
                Directory.Delete(absoluteRoot, recursive: true);
        }
    }

    [Fact]
    public async Task Completion_rejects_a_different_hash()
    {
        var root = Path.Combine(Path.GetTempPath(), "nexora-upload-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var store = new LocalUploadStorage(root);
            var fileId = Guid.NewGuid();
            await store.CreateSessionAsync(fileId,
                new UploadManifest(3, 3, 1, new string('0', 64)), CancellationToken.None);
            await store.SaveChunkAsync(fileId, 0,
                new MemoryStream(Encoding.UTF8.GetBytes("abc")), CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                store.CompleteAsync(fileId, Guid.NewGuid().ToString("N"), CancellationToken.None));
        }
        finally
        {
            var absoluteRoot = Path.GetFullPath(root);
            var expectedParent = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "nexora-upload-tests"))
                + Path.DirectorySeparatorChar;
            if (!absoluteRoot.StartsWith(expectedParent, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Test cleanup path is outside its dedicated directory.");
            if (Directory.Exists(absoluteRoot))
                Directory.Delete(absoluteRoot, recursive: true);
        }
    }

    [Fact]
    public async Task Completion_requires_every_chunk_and_rejects_conflicting_retry()
    {
        var root = Path.Combine(Path.GetTempPath(), "nexora-upload-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var store = new LocalUploadStorage(root);
            var fileId = Guid.NewGuid();
            await store.CreateSessionAsync(fileId,
                new UploadManifest(6, 3, 2, null), CancellationToken.None);
            await store.SaveChunkAsync(fileId, 0,
                new MemoryStream(Encoding.UTF8.GetBytes("abc")), CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                store.SaveChunkAsync(fileId, 0,
                    new MemoryStream(Encoding.UTF8.GetBytes("xyz")), CancellationToken.None));
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                store.CompleteAsync(fileId, Guid.NewGuid().ToString("N"), CancellationToken.None));
        }
        finally
        {
            var absoluteRoot = Path.GetFullPath(root);
            var expectedParent = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "nexora-upload-tests"))
                + Path.DirectorySeparatorChar;
            if (!absoluteRoot.StartsWith(expectedParent, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Test cleanup path is outside its dedicated directory.");
            if (Directory.Exists(absoluteRoot))
                Directory.Delete(absoluteRoot, recursive: true);
        }
    }

    [Fact]
    public async Task Finds_upload_sessions_with_no_recent_activity()
    {
        var root = Path.Combine(Path.GetTempPath(), "nexora-upload-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var store = new LocalUploadStorage(root);
            var fileId = Guid.NewGuid();
            await store.CreateSessionAsync(fileId,
                new UploadManifest(3, 3, 1, null), CancellationToken.None);
            var manifestPath = Path.Combine(root, "uploads", fileId.ToString("N"), "manifest.json");
            File.SetLastWriteTimeUtc(manifestPath, DateTime.UtcNow.AddDays(-8));

            Assert.Contains(fileId, store.FindExpiredSessions(TimeSpan.FromDays(7)));
        }
        finally
        {
            var absoluteRoot = Path.GetFullPath(root);
            var expectedParent = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "nexora-upload-tests"))
                + Path.DirectorySeparatorChar;
            if (!absoluteRoot.StartsWith(expectedParent, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Test cleanup path is outside its dedicated directory.");
            if (Directory.Exists(absoluteRoot))
                Directory.Delete(absoluteRoot, recursive: true);
        }
    }
}
