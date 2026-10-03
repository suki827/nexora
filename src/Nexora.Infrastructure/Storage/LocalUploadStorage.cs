using System.Security.Cryptography;
using System.Text.Json;
using Nexora.Application.MediaCatalog;

namespace Nexora.Infrastructure.Storage;

public sealed class LocalUploadStorage : IUploadStorage
{
    public static string DefaultRootPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nexora", "media");

    private readonly string _uploads;
    private readonly string _files;

    public LocalUploadStorage(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
            throw new ArgumentException("A media storage root is required.", nameof(rootPath));
        var root = Path.GetFullPath(rootPath);
        _uploads = Path.Combine(root, "uploads");
        _files = Path.Combine(root, "files");
        Directory.CreateDirectory(_uploads);
        Directory.CreateDirectory(_files);
    }

    public async Task CreateSessionAsync(Guid fileId, UploadManifest manifest, CancellationToken cancellationToken)
    {
        var sessionPath = SessionPath(fileId);
        Directory.CreateDirectory(sessionPath);
        await using var output = new FileStream(
            Path.Combine(sessionPath, "manifest.json"), FileMode.CreateNew, FileAccess.Write,
            FileShare.None, 4096, FileOptions.Asynchronous);
        await JsonSerializer.SerializeAsync(output, manifest, cancellationToken: cancellationToken);
    }

    public async Task<UploadProgress> GetProgressAsync(Guid fileId, CancellationToken cancellationToken)
    {
        var manifest = await ReadManifestAsync(fileId, cancellationToken);
        var uploaded = new List<int>();
        for (var index = 0; index < manifest.ChunkCount; index++)
        {
            if (File.Exists(ChunkPath(fileId, index)))
                uploaded.Add(index);
        }
        return new UploadProgress(manifest, uploaded);
    }

    public async Task SaveChunkAsync(Guid fileId, int index, Stream content, CancellationToken cancellationToken)
    {
        var manifest = await ReadManifestAsync(fileId, cancellationToken);
        if (index < 0 || index >= manifest.ChunkCount)
            throw new ArgumentOutOfRangeException(nameof(index), "Chunk index is outside the upload.");

        var expectedLength = index == manifest.ChunkCount - 1
            ? manifest.TotalBytes - (long)index * manifest.ChunkSize
            : manifest.ChunkSize;
        var chunkPath = ChunkPath(fileId, index);
        var tempPath = chunkPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var output = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write,
                             FileShare.None, 64 * 1024, FileOptions.Asynchronous))
            {
                var buffer = new byte[64 * 1024];
                long received = 0;
                int read;
                while ((read = await content.ReadAsync(buffer, cancellationToken)) != 0)
                {
                    received += read;
                    if (received > expectedLength)
                        throw new ArgumentException("Chunk exceeds its expected size.", nameof(content));
                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }
                if (received != expectedLength)
                    throw new ArgumentException("Chunk size does not match its expected size.", nameof(content));
            }

            try
            {
                File.Move(tempPath, chunkPath);
            }
            catch (IOException) when (File.Exists(chunkPath))
            {
                await using var existing = File.OpenRead(chunkPath);
                await using var incoming = File.OpenRead(tempPath);
                var existingHash = await SHA256.HashDataAsync(existing, cancellationToken);
                var incomingHash = await SHA256.HashDataAsync(incoming, cancellationToken);
                if (!CryptographicOperations.FixedTimeEquals(existingHash, incomingHash))
                    throw new InvalidOperationException("A different chunk already exists at this index.");
            }
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    public async Task<CompletedUpload> CompleteAsync(
        Guid fileId, string storageKey, CancellationToken cancellationToken)
    {
        var manifest = await ReadManifestAsync(fileId, cancellationToken);
        var finalPath = GetFilePath(storageKey);
        if (File.Exists(finalPath))
            return await ValidateCompletedFileAsync(finalPath, manifest, cancellationToken);

        var tempPath = finalPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            long total = 0;
            await using (var output = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write,
                             FileShare.None, 64 * 1024, FileOptions.Asynchronous))
            {
                var buffer = new byte[64 * 1024];
                for (var index = 0; index < manifest.ChunkCount; index++)
                {
                    var chunkPath = ChunkPath(fileId, index);
                    if (!File.Exists(chunkPath))
                        throw new InvalidOperationException($"Chunk {index} is missing.");
                    await using var chunk = File.OpenRead(chunkPath);
                    int read;
                    while ((read = await chunk.ReadAsync(buffer, cancellationToken)) != 0)
                    {
                        total += read;
                        hasher.AppendData(buffer, 0, read);
                        await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    }
                }
            }

            var sha256 = Convert.ToHexString(hasher.GetHashAndReset()).ToLowerInvariant();
            if (total != manifest.TotalBytes ||
                (manifest.ExpectedSha256 is not null &&
                 !string.Equals(sha256, manifest.ExpectedSha256, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Uploaded file size or SHA-256 hash does not match.");

            try
            {
                File.Move(tempPath, finalPath);
            }
            catch (IOException) when (File.Exists(finalPath))
            {
                return await ValidateCompletedFileAsync(finalPath, manifest, cancellationToken);
            }
            return new CompletedUpload(total, sha256);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    public Task DeleteSessionAsync(Guid fileId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sessionPath = SessionPath(fileId);
        if (Directory.Exists(sessionPath))
            Directory.Delete(sessionPath, recursive: true);
        return Task.CompletedTask;
    }

    public IReadOnlyList<Guid> FindExpiredSessions(TimeSpan olderThan)
    {
        if (olderThan <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(olderThan));
        var cutoff = DateTime.UtcNow - olderThan;
        var expired = new List<Guid>();
        foreach (var directory in Directory.EnumerateDirectories(_uploads))
        {
            if (!Guid.TryParseExact(Path.GetFileName(directory), "N", out var fileId))
                continue;
            var lastWrite = Directory.EnumerateFiles(directory)
                .Select(File.GetLastWriteTimeUtc)
                .DefaultIfEmpty(Directory.GetCreationTimeUtc(directory))
                .Max();
            if (lastWrite < cutoff)
                expired.Add(fileId);
        }
        return expired;
    }

    public Stream OpenFile(string storageKey) => File.OpenRead(GetFilePath(storageKey));

    public Stream? OpenWaveform(string storageKey)
    {
        var path = WaveformPath(storageKey);
        return File.Exists(path) ? File.OpenRead(path) : null;
    }

    public async Task SaveWaveformAsync(string storageKey, Stream content, CancellationToken cancellationToken)
    {
        var path = WaveformPath(storageKey);
        var tempPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var output = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write,
                             FileShare.None, 64 * 1024, FileOptions.Asynchronous))
                await content.CopyToAsync(output, cancellationToken);
            File.Move(tempPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    public string GetFilePath(string storageKey)
    {
        if (!Guid.TryParseExact(storageKey, "N", out _))
            throw new ArgumentException("Invalid local storage key.", nameof(storageKey));
        return Path.Combine(_files, storageKey);
    }

    private string WaveformPath(string storageKey) => GetFilePath(storageKey) + ".waveform.json";

    private string SessionPath(Guid fileId)
    {
        if (fileId == Guid.Empty)
            throw new ArgumentException("File ID cannot be empty.", nameof(fileId));
        var path = Path.GetFullPath(Path.Combine(_uploads, fileId.ToString("N")));
        if (!path.StartsWith(_uploads + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Upload session path is outside the storage root.");
        return path;
    }

    private string ChunkPath(Guid fileId, int index) =>
        Path.Combine(SessionPath(fileId), $"{index:D8}.part");

    private async Task<UploadManifest> ReadManifestAsync(Guid fileId, CancellationToken cancellationToken)
    {
        await using var input = File.OpenRead(Path.Combine(SessionPath(fileId), "manifest.json"));
        return await JsonSerializer.DeserializeAsync<UploadManifest>(input, cancellationToken: cancellationToken)
            ?? throw new InvalidDataException("Upload manifest is empty.");
    }

    private static async Task<CompletedUpload> ValidateCompletedFileAsync(
        string path, UploadManifest manifest, CancellationToken cancellationToken)
    {
        await using var input = File.OpenRead(path);
        if (input.Length != manifest.TotalBytes)
            throw new InvalidOperationException("Completed file has an unexpected size.");
        var sha256 = Convert.ToHexString(await SHA256.HashDataAsync(input, cancellationToken)).ToLowerInvariant();
        if (manifest.ExpectedSha256 is not null &&
            !string.Equals(sha256, manifest.ExpectedSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Completed file has an unexpected SHA-256 hash.");
        return new CompletedUpload(input.Length, sha256);
    }
}
