using Microsoft.EntityFrameworkCore;
using Nexora.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// 读取 PostgreSQL 连接字符串
var connectionString =
    builder.Configuration.GetConnectionString("NexoraDatabase")
    ?? throw new InvalidOperationException(
        "Connection string 'NexoraDatabase' was not found.");

// 注册 EF Core DbContext
builder.Services.AddDbContext<NexoraDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<NexoraDbContext>();
    //await dbContext.Database.MigrateAsync();

    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();

// 临时数据库连接与映射测试接口
//app.MapGet("/api/test/database", async (
//    NexoraDbContext dbContext,
//    CancellationToken cancellationToken) =>
//{
//    var canConnect =
//        await dbContext.Database.CanConnectAsync(cancellationToken);

//    if (!canConnect)
//    {
//        return Results.Problem(
//            title: "Database connection failed",
//            detail: "Unable to connect to the Nexora PostgreSQL database.",
//            statusCode: StatusCodes.Status500InternalServerError);
//    }

//    var analysisTaskCount =
//        await dbContext.AnalysisTasks.CountAsync(cancellationToken);

//    var analysisResultCount =
//        await dbContext.AnalysisResults.CountAsync(cancellationToken);

//    return Results.Ok(new
//    {
//        connected = true,
//        database = dbContext.Database.GetDbConnection().Database,
//        analysisTaskCount,
//        analysisResultCount
//    });
//});

app.Run();
