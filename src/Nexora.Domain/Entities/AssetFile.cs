using System.Text.RegularExpressions;

namespace Nexora.Domain.Entities;

public sealed class AssetFile
{
    public const string RoleOriginal = "original";
    public const string RoleSubtitle = "subtitle";
    public const string RoleReferenceScript = "reference_script";
    public const string RoleAttachment = "attachment";
    public const string ProviderLocal = "local";
    public const string ProviderSwift = "swift";
    public const string ProviderS3 = "s3";
    public const string StatusPending = "pending";
    public const string StatusUploading = "uploading";
    public const string StatusReady = "ready";
    public const string StatusFailed = "failed";
    public const string StatusDeleting = "deleting";
    public const string StatusDeleted = "deleted";

    private static readonly HashSet<string> FileRoles =
        [RoleOriginal, RoleSubtitle, RoleReferenceScript, RoleAttachment];
    private static readonly HashSet<string> StorageProviders =
        [ProviderLocal, ProviderSwift, ProviderS3];

    public Guid Id { get; private set; }
    public Guid AssetId { get; private set; }
    public string FileRole { get; private set; } = RoleOriginal;
    public string OriginalName { get; private set; } = string.Empty;
    public string StorageProvider { get; private set; } = ProviderLocal;
    public string? StorageBucket { get; private set; }
    public string StorageKey { get; private set; } = string.Empty;
    public string? ContentType { get; private set; }
    public long? SizeBytes { get; private set; }
    public string? ContentHash { get; private set; }
    public string Status { get; private set; } = StatusPending;
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? UploadedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public MediaAsset Asset { get; private set; } = null!;
    public MediaMetadata? Metadata { get; private set; }

    private AssetFile() { }

    public AssetFile(
        Guid assetId,
        string fileRole,
        string originalName,
        string storageProvider,
        string? storageBucket,
        string storageKey,
        string? contentType,
        long? sizeBytes,
        string? contentHash)
    {
        if (assetId == Guid.Empty)
            throw new ArgumentException("Asset ID cannot be empty.", nameof(assetId));

        Id = Guid.NewGuid();
        AssetId = assetId;
        FileRole = ValidateFileRole(fileRole);
        OriginalName = ValidateRequiredText(originalName, 255, nameof(originalName));
        StorageProvider = ValidateStorageProvider(storageProvider);
        StorageBucket = ValidateBucket(StorageProvider, storageBucket);
        StorageKey = ValidateRequiredText(storageKey, int.MaxValue, nameof(storageKey));
        ContentType = ValidateOptionalText(contentType, 150, nameof(contentType));
        SizeBytes = ValidateSize(sizeBytes);
        ContentHash = ValidateHash(contentHash);
        Status = StatusPending;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public void BeginUpload()
    {
        EnsureNotDeleted();
        if (Status is not (StatusPending or StatusFailed))
            throw new InvalidOperationException("Only pending or failed files can begin uploading.");

        Status = StatusUploading;
        ErrorCode = null;
        ErrorMessage = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkReady(long? sizeBytes = null, string? contentHash = null)
    {
        EnsureNotDeleted();
        SizeBytes = ValidateSize(sizeBytes ?? SizeBytes);
        ContentHash = ValidateHash(contentHash ?? ContentHash);
        Status = StatusReady;
        UploadedAt = DateTime.UtcNow;
        ErrorCode = null;
        ErrorMessage = null;
        UpdatedAt = UploadedAt.Value;
    }

    public void MarkFailed(string errorCode, string? errorMessage)
    {
        EnsureNotDeleted();
        ErrorCode = ValidateOptionalText(errorCode, 100, nameof(errorCode));
        if (string.IsNullOrWhiteSpace(ErrorCode))
            throw new ArgumentException("An error code is required.", nameof(errorCode));
        ErrorMessage = errorMessage;
        Status = StatusFailed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void BeginDelete()
    {
        if (Status == StatusDeleted)
            return;
        Status = StatusDeleting;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        if (Status == StatusDeleted && DeletedAt.HasValue)
            return;
        Status = StatusDeleted;
        DeletedAt ??= DateTime.UtcNow;
        UpdatedAt = DeletedAt.Value;
    }

    private void EnsureNotDeleted()
    {
        if (DeletedAt.HasValue || Status == StatusDeleted)
            throw new InvalidOperationException("A deleted file cannot be changed.");
    }

    private static string ValidateFileRole(string value)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        if (normalized is null || !FileRoles.Contains(normalized))
            throw new ArgumentException("Unsupported file role.", nameof(value));
        return normalized;
    }

    private static string ValidateStorageProvider(string value)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        if (normalized is null || !StorageProviders.Contains(normalized))
            throw new ArgumentException("Unsupported storage provider.", nameof(value));
        return normalized;
    }

    private static string? ValidateBucket(string provider, string? bucket)
    {
        if (provider == ProviderLocal)
        {
            if (!string.IsNullOrWhiteSpace(bucket))
                throw new ArgumentException("Local storage must not specify a bucket.", nameof(bucket));
            return null;
        }

        return ValidateRequiredText(bucket!, 255, nameof(bucket));
    }

    private static string ValidateRequiredText(string value, int maxLength, string parameterName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            throw new ArgumentException("A value is required.", parameterName);
        if (normalized.Length > maxLength)
            throw new ArgumentException($"Value cannot exceed {maxLength} characters.", parameterName);
        return normalized;
    }

    private static string? ValidateOptionalText(string? value, int maxLength, string parameterName)
    {
        if (value is null)
            return null;
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new ArgumentException($"Value cannot exceed {maxLength} characters.", parameterName);
        return normalized.Length == 0 ? null : normalized;
    }

    private static long? ValidateSize(long? value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "File size cannot be negative.");
        return value;
    }

    private static string? ValidateHash(string? value)
    {
        if (value is null)
            return null;
        var normalized = value.Trim().ToLowerInvariant();
        if (!Regex.IsMatch(normalized, "^[0-9a-f]{64}$"))
            throw new ArgumentException("Content hash must be a 64-character SHA-256 hex string.", nameof(value));
        return normalized;
    }
}
