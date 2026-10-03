namespace Nexora.Domain.Entities;

public sealed class TaskInput
{
    public const string AssetFileType = "asset_file", ArtifactType = "artifact";
    public Guid Id { get; private set; }
    public Guid TaskId { get; private set; }
    public string InputType { get; private set; } = string.Empty;
    public string InputRole { get; private set; } = string.Empty;
    public Guid? AssetFileId { get; private set; }
    public Guid? ArtifactId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public AnalysisTask Task { get; private set; } = null!;
    public AssetFile? AssetFile { get; private set; }
    public GeneratedArtifact? Artifact { get; private set; }
    private TaskInput() { }
    public TaskInput(Guid taskId, string inputType, string inputRole, Guid? assetFileId, Guid? artifactId)
    {
        if (taskId == Guid.Empty) throw new ArgumentException("Task is required.", nameof(taskId));
        if (string.IsNullOrWhiteSpace(inputRole) || inputRole.Length > 100) throw new ArgumentException("Invalid input role.", nameof(inputRole));
        if (!((inputType == AssetFileType && assetFileId is { } fileId && fileId != Guid.Empty && artifactId is null) ||
              (inputType == ArtifactType && artifactId is { } outputId && outputId != Guid.Empty && assetFileId is null)))
            throw new ArgumentException("Input must reference exactly one object matching its type.", nameof(inputType));
        Id = Guid.NewGuid(); TaskId = taskId; InputType = inputType; InputRole = inputRole.Trim();
        AssetFileId = assetFileId; ArtifactId = artifactId; CreatedAt = DateTime.UtcNow;
    }
}
