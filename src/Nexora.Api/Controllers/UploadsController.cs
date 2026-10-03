using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nexora.Api.Contracts;
using Nexora.Api.Security;
using Nexora.Application.MediaCatalog;

namespace Nexora.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/assets/{assetId:guid}/uploads")]
public sealed class UploadsController(UploadService uploads, ICurrentUser currentUser) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<UploadSessionResponse>> StartUpload(
        Guid assetId, StartUploadRequest request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } ownerId)
            return Unauthorized();
        var session = await uploads.StartAsync(ownerId, assetId,
            new StartUpload(request.FileRole, request.OriginalName, request.ContentType,
                request.TotalBytes, request.ChunkSize, request.Sha256), cancellationToken);
        if (session is null)
            return NotFound();
        return CreatedAtAction(nameof(GetUpload), new { assetId, fileId = session.FileId },
            UploadSessionResponse.From(session));
    }

    [HttpGet("{fileId:guid}")]
    public async Task<ActionResult<UploadSessionResponse>> GetUpload(
        Guid assetId, Guid fileId, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } ownerId)
            return Unauthorized();
        var session = await uploads.GetAsync(ownerId, assetId, fileId, cancellationToken);
        return session is null ? NotFound() : Ok(UploadSessionResponse.From(session));
    }

    [HttpPut("{fileId:guid}/chunks/{index:int}")]
    [RequestSizeLimit(33 * 1024 * 1024)]
    public async Task<IActionResult> PutChunk(
        Guid assetId, Guid fileId, int index, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } ownerId)
            return Unauthorized();
        return await uploads.UploadChunkAsync(ownerId, assetId, fileId, index,
            Request.Body, cancellationToken) ? NoContent() : NotFound();
    }

    [HttpPost("{fileId:guid}/complete")]
    public async Task<IActionResult> CompleteUpload(
        Guid assetId, Guid fileId, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } ownerId)
            return Unauthorized();
        var file = await uploads.CompleteAsync(ownerId, assetId, fileId, cancellationToken);
        return file is null ? NotFound() : Ok(file.ToResponse());
    }

    [HttpDelete("{fileId:guid}")]
    public async Task<IActionResult> CancelUpload(
        Guid assetId, Guid fileId, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } ownerId)
            return Unauthorized();
        return await uploads.CancelAsync(ownerId, assetId, fileId, cancellationToken)
            ? NoContent() : NotFound();
    }
}
