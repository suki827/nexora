using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nexora.Api.Security;
using Nexora.Application.MediaCatalog;

namespace Nexora.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/assets/{assetId:guid}/files/{fileId:guid}")]
public sealed class MediaContentController(UploadService uploads, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("content")]
    public async Task<IActionResult> GetContent(
        Guid assetId, Guid fileId, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } ownerId)
            return Unauthorized();
        var result = await uploads.OpenFileAsync(ownerId, assetId, fileId, cancellationToken);
        return result is null
            ? NotFound()
            : File(result.Value.Content, result.Value.ContentType, enableRangeProcessing: true);
    }

    [HttpGet("waveform")]
    public async Task<IActionResult> GetWaveform(
        Guid assetId, Guid fileId, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } ownerId)
            return Unauthorized();
        var stream = await uploads.OpenWaveformAsync(ownerId, assetId, fileId, cancellationToken);
        return stream is null ? NotFound() : File(stream, "application/json");
    }
}
