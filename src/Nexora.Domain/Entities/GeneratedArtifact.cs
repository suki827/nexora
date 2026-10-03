using System.Text.RegularExpressions;

namespace Nexora.Domain.Entities;

public sealed class GeneratedArtifact
{
    public const string Generating = "generating", Ready = "ready", Failed = "failed", Deleted = "deleted";
    public Guid Id { get; private set; }
    public Guid TaskId { get; private set; }
    public Guid? ResultId { get; private set; }
    public string ArtifactType { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string StorageProvider { get; private set; } = string.Empty;
    public string? StorageBucket { get; private set; }
    public string StorageKey { get; private set; } = string.Empty;
    public string? ContentType { get; private set; }
    public long? SizeBytes { get; private set; }
    public string? ContentHash { get; private set; }
    public string Status { get; private set; } = Generating;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public AnalysisTask Task { get; private set; } = null!;
    public AnalysisResult? Result { get; private set; }
    public ICollection<TaskInput> UsedByInputs { get; private set; } = new List<TaskInput>();
    private GeneratedArtifact() { }
    public GeneratedArtifact(Guid taskId, Guid? resultId, string artifactType, string fileName,
        string storageProvider, string? storageBucket, string storageKey, string? contentType)
    {
        if (taskId == Guid.Empty) throw new ArgumentException("Task is required.", nameof(taskId));
        if (string.IsNullOrWhiteSpace(artifactType) || artifactType.Length > 100) throw new ArgumentException("Invalid artifact type.", nameof(artifactType));
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > 255) throw new ArgumentException("Invalid file name.", nameof(fileName));
        if (storageProvider is not ("local" or "swift" or "s3")) throw new ArgumentException("Invalid provider.", nameof(storageProvider));
        if (storageProvider != "local" && string.IsNullOrWhiteSpace(storageBucket)) throw new ArgumentException("Cloud bucket is required.", nameof(storageBucket));
        if (storageBucket?.Length > 255) throw new ArgumentException("Storage bucket is too long.", nameof(storageBucket));
        if (storageProvider == "local" && storageBucket is not null) throw new ArgumentException("Local bucket must be null.", nameof(storageBucket));
        if (string.IsNullOrWhiteSpace(storageKey)) throw new ArgumentException("Storage key is required.", nameof(storageKey));
        if (contentType?.Length > 150) throw new ArgumentException("Content type is too long.", nameof(contentType));
        Id = Guid.NewGuid(); TaskId = taskId; ResultId = resultId; ArtifactType = artifactType.Trim(); FileName = fileName.Trim();
        StorageProvider = storageProvider; StorageBucket = storageBucket; StorageKey = storageKey.Trim(); ContentType = contentType;
        CreatedAt = UpdatedAt = DateTime.UtcNow;
    }
    public void MarkReady(long sizeBytes, string? hash)
    {
        if (Status != Generating) throw new InvalidOperationException("Artifact is not generating.");
        if (sizeBytes < 0) throw new ArgumentOutOfRangeException(nameof(sizeBytes));
        if (hash is not null && !Regex.IsMatch(hash, "^[0-9a-f]{64}$")) throw new ArgumentException("Invalid SHA-256.", nameof(hash));
        SizeBytes = sizeBytes; ContentHash = hash; Status = Ready; UpdatedAt = DateTime.UtcNow;
    }
    public void MarkFailed() { if (Status != Generating) throw new InvalidOperationException("Artifact is not generating."); Status = Failed; UpdatedAt = DateTime.UtcNow; }
    public void MarkDeleted() { Status = Deleted; UpdatedAt = DateTime.UtcNow; }
}
