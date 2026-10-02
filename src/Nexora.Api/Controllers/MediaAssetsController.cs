using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Nexora.Api.Contracts;
using Nexora.Api.Security;
using Nexora.Application.MediaCatalog;

namespace Nexora.Api.Controllers;

[ApiController]
[Route("api/assets")]
public sealed class MediaAssetsController(
    MediaCatalogService catalog,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PageResponse<MediaAssetResponse>>> ListAssets(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } ownerId)
            return Unauthorized();

        var result = await catalog.ListAssetsAsync(ownerId, page, pageSize, includeArchived, cancellationToken);
        return Ok(result.ToResponse(x => x.ToResponse()));
    }

    [HttpGet("{assetId:guid}")]
    public async Task<ActionResult<MediaAssetResponse>> GetAsset(Guid assetId, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } ownerId)
            return Unauthorized();

        var asset = await catalog.GetAssetAsync(ownerId, assetId, cancellationToken);
        return asset is null ? NotFound() : Ok(asset.ToResponse());
    }

    [HttpPost]
    public async Task<ActionResult<MediaAssetResponse>> CreateAsset(
        CreateMediaAssetRequest request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } ownerId)
            return Unauthorized();

        var asset = await catalog.CreateAssetAsync(
            ownerId, request.Name, request.Description, request.AssetType, cancellationToken);
        return CreatedAtAction(nameof(GetAsset), new { assetId = asset.Id }, asset.ToResponse());
    }

    [HttpPut("{assetId:guid}")]
    public async Task<ActionResult<MediaAssetResponse>> UpdateAsset(
        Guid assetId, UpdateMediaAssetRequest request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } ownerId)
            return Unauthorized();

        var asset = await catalog.UpdateAssetAsync(
            ownerId, assetId, request.Name, request.Description,
            request.AssetType, request.Status, cancellationToken);
        return asset is null ? NotFound() : Ok(asset.ToResponse());
    }

    [HttpDelete("{assetId:guid}")]
    public async Task<IActionResult> DeleteAsset(Guid assetId, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } ownerId)
            return Unauthorized();

        return await catalog.DeleteAssetAsync(ownerId, assetId, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    [HttpGet("{assetId:guid}/files")]
    public async Task<ActionResult<PageResponse<AssetFileResponse>>> ListFiles(
        Guid assetId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } ownerId)
            return Unauthorized();

        var result = await catalog.ListAssetFilesAsync(ownerId, assetId, page, pageSize, cancellationToken);
        return result is null
            ? NotFound()
            : Ok(result.ToResponse(x => x.ToResponse()));
    }

    [HttpPost("{assetId:guid}/files")]
    public async Task<ActionResult<AssetFileResponse>> CreateFile(
        Guid assetId, CreateAssetFileRequest request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } ownerId)
            return Unauthorized();

        var file = await catalog.CreateAssetFileAsync(
            ownerId,
            assetId,
            new CreateAssetFile(
                request.FileRole, request.OriginalName, request.StorageProvider,
                request.StorageBucket, request.StorageKey, request.ContentType,
                request.SizeBytes, request.ContentHash),
            cancellationToken);
        return file is null
            ? NotFound()
            : CreatedAtAction(nameof(GetFile), new { assetId, fileId = file.Id }, file.ToResponse());
    }

    [HttpGet("{assetId:guid}/files/{fileId:guid}")]
    public async Task<ActionResult<AssetFileResponse>> GetFile(
        Guid assetId, Guid fileId, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } ownerId)
            return Unauthorized();

        var file = await catalog.GetAssetFileAsync(ownerId, assetId, fileId, cancellationToken);
        return file is null ? NotFound() : Ok(file.ToResponse());
    }

    [HttpDelete("{assetId:guid}/files/{fileId:guid}")]
    public async Task<IActionResult> DeleteFile(
        Guid assetId, Guid fileId, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } ownerId)
            return Unauthorized();

        return await catalog.DeleteAssetFileAsync(ownerId, assetId, fileId, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    [HttpGet("{assetId:guid}/files/{fileId:guid}/metadata")]
    public async Task<ActionResult<MediaMetadataResponse>> GetMetadata(
        Guid assetId, Guid fileId, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } ownerId)
            return Unauthorized();

        var metadata = await catalog.GetMetadataAsync(ownerId, assetId, fileId, cancellationToken);
        return metadata is null ? NotFound() : Ok(metadata.ToResponse());
    }

    [HttpPut("{assetId:guid}/files/{fileId:guid}/metadata")]
    public async Task<ActionResult<MediaMetadataResponse>> UpdateMetadata(
        Guid assetId,
        Guid fileId,
        UpdateMediaMetadataRequest request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } ownerId)
            return Unauthorized();

        using var probeJson = request.ProbeJson is { } element
            ? JsonDocument.Parse(element.GetRawText())
            : null;
        var metadata = await catalog.UpdateMetadataAsync(
            ownerId,
            assetId,
            fileId,
            new ProbeMetadata(
                request.DurationSeconds, request.Width, request.Height,
                request.FpsNum, request.FpsDen, request.FrameCount,
                request.IsVariableFps, request.VideoCodec, request.AudioCodec,
                request.AudioSampleRate, request.AudioChannels, request.FormatName,
                probeJson),
            cancellationToken);
        return metadata is null ? NotFound() : Ok(metadata.ToResponse());
    }
}
