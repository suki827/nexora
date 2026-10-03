using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexora.Api.Contracts.Tasks;
using Nexora.Api.Security;
using Nexora.Application.Tasks;

namespace Nexora.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tasks")]
public sealed class TasksController(AnalysisTaskService tasks, ICurrentUser currentUser) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TaskDetail>> Create(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } ownerId) return Unauthorized();
        var payload = request.RequestPayload is { } value ? JsonDocument.Parse(value.GetRawText()) : null;
        var task = await tasks.CreateAsync(ownerId, request.AnalysisType, request.Priority, payload,
            request.IdempotencyKey, request.Inputs.Select(x => new TaskInputSpec(x.InputType, x.InputRole,
                x.AssetFileId, x.ArtifactId)).ToArray(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = task.Id }, task.ToDetail());
    }

    [HttpGet]
    public async Task<ActionResult<object>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } ownerId) return Unauthorized();
        var result = await tasks.ListAsync(ownerId, page, pageSize, cancellationToken);
        return Ok(new { items = result.Items.Select(x => x.ToSummary()).ToArray(), result.Page, result.PageSize, result.TotalCount });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskDetail>> Get(Guid id, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } ownerId) return Unauthorized();
        var task = await tasks.GetAsync(ownerId, id, cancellationToken);
        return task is null ? NotFound() : Ok(task.ToDetail());
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<TaskSummary>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } ownerId) return Unauthorized();
        var task = await tasks.CancelAsync(ownerId, id, cancellationToken);
        return task is null ? NotFound() : Ok(task.ToSummary());
    }

    [HttpGet("{id:guid}/inputs")]
    public async Task<IActionResult> Inputs(Guid id, CancellationToken ct) => await Subresource(id, "inputs", ct);
    [HttpGet("{id:guid}/attempts")]
    public async Task<IActionResult> Attempts(Guid id, CancellationToken ct) => await Subresource(id, "attempts", ct);
    [HttpGet("{id:guid}/results")]
    public async Task<IActionResult> Results(Guid id, CancellationToken ct) => await Subresource(id, "results", ct);
    [HttpGet("{id:guid}/artifacts")]
    public async Task<IActionResult> Artifacts(Guid id, CancellationToken ct) => await Subresource(id, "artifacts", ct);

    private async Task<IActionResult> Subresource(Guid id, string kind, CancellationToken ct)
    {
        if (currentUser.UserId is not { } ownerId) return Unauthorized();
        var task = await tasks.GetAsync(ownerId, id, ct);
        if (task is null) return NotFound();
        var detail = task.ToDetail();
        return kind switch
        {
            "inputs" => Ok(detail.Inputs),
            "attempts" => Ok(detail.Attempts),
            "results" => Ok(detail.Results),
            _ => Ok(detail.Artifacts)
        };
    }
}
