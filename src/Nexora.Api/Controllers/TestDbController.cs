using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexora.Domain.Entities;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class TestDbController : ControllerBase
{
    private readonly NexoraDbContext _dbContext;

    public TestDbController(NexoraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 测试数据库连接。
    /// GET /TestDb
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> TestConnection()
    {
        var canConnect = await _dbContext.Database.CanConnectAsync();

        if (!canConnect)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    connected = false,
                    message = "Unable to connect to PostgreSQL."
                });
        }

        var analysisCount = await _dbContext.AnalysisTasks.CountAsync();
        var resultCount = await _dbContext.AnalysisResults.CountAsync();

        return Ok(new
        {
            connected = true,
            taskAnalysisCount = analysisCount,
            taskResultCount = resultCount
        });
    }

    /// <summary>
    /// 创建一条测试 Task 和对应的 Result。
    /// POST /TestDb/create-test-data
    /// </summary>
    [HttpPost("create-test-data")]
    public async Task<IActionResult> CreateTestData()
    {
        var now = DateTimeOffset.UtcNow;

        var task = new AnalysisTask
        {
            Id = Guid.NewGuid(),

            // 如果 task_number 是数据库自增字段，这里不需要设置 TaskNumber。
            //TaskNumber = 1,
            AnalysisType = "video_summary",
            Status = "completed",
            InputFileUrl = "https://example.com/videos/test-video.mp4",

            RequestPayload = JsonDocument.Parse(
                """
                {
                    "language": "en",
                    "models": [
                        "shot_detection",
                        "seed"
                    ],
                    "generateSummary": true
                }
                """),

            Provider = "test-provider2",
            AttemptCount = 2,
            MaxAttempts = 3,
            ErrorCode = null,
            ErrorMessage = null,
            CreatedAt = now,
            StartedAt = now,
            CompletedAt = now,
            UpdatedAt = now
        };

        var result = new AnalysisResult
        {
            Id = Guid.NewGuid(),

            // 使用上面 task 的 Id，不能再生成一个无关的 Guid。
            AnalysisTaskId = task.Id,

            ResultType = "video_summary",
            SchemaVersion = "1.0",

            ResultPayload = JsonDocument.Parse(
                """
                {
                    "language": "en",
                    "summary": "This is a test video summary.",
                    "keywords": [
                        "test",
                        "video",
                        "analysis"
                    ],
                    "durationSeconds": 120
                }
                """),

            ConfidenceScore = 0.9500m,
            CreatedAt = now,
            UpdatedAt = now,

            // 建立 Entity 之间的关系。
            AnalysisTask = task
        };

        // 只添加 task 也可以。
        // 因为 result 已经放入 task.Results 中，EF Core 会一起保存。
        task.Results.Add(result);

        _dbContext.AnalysisTasks.Add(task);

        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetTask),
            new { id = task.Id },
            new
            {
                message = "Task and result created successfully.",
                task = new
                {
                    task.Id,
                    task.TaskNumber,
                    task.AnalysisType,
                    task.Status,
                    task.CreatedAt
                },
                result = new
                {
                    result.Id,
                    result.AnalysisTaskId,
                    result.ResultType,
                    result.SchemaVersion,
                    result.ConfidenceScore,
                    result.CreatedAt
                }
            });
    }

    /// <summary>
    /// 根据 Task ID 查询 Task 及其 Results。
    /// GET /TestDb/tasks/{id}
    /// </summary>
    [HttpGet("tasks/{id:guid}")]
    public async Task<IActionResult> GetTask(Guid id)
    {
        var task = await _dbContext.AnalysisTasks
            .AsNoTracking()
            .Include(x => x.Results)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (task is null)
        {
            return NotFound(new
            {
                message = "Analysis task was not found.",
                taskId = id
            });
        }

        return Ok(task);
    }
}