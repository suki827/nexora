using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.RateLimiting;
using Nexora.Api;
using Nexora.Api.Security;
using Nexora.Application.MediaCatalog;
using Nexora.Application.Tasks;
using Nexora.Infrastructure.Persistence;
using Nexora.Infrastructure.Storage;
using Nexora.Infrastructure.Identity;
using Nexora.Web;
using Nexora.Web.Services;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .AddJsonFile(Path.GetFullPath(Path.Combine(
        builder.Environment.ContentRootPath, "..", "..", "appsettings.Local.json")), optional: true)
    .AddEnvironmentVariables();

// 读取 PostgreSQL 连接字符串
var connectionString =
    builder.Configuration.GetConnectionString("NexoraDatabase")
    ?? throw new InvalidOperationException(
        "Connection string 'NexoraDatabase' was not found.");

// 注册 EF Core DbContext
builder.Services.AddDbContext<NexoraDbContext>(options =>
    options.UseNpgsql(connectionString));
if (builder.Configuration["DataProtection:KeyPath"] is { Length: > 0 } keyPath)
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keyPath));

builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddScoped<WebApiClient>();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "nexora.csrf";
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "nexora.session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        options.Events.OnValidatePrincipal = async context =>
        {
            var claim = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var stamp = context.Principal?.FindFirstValue("nexora:security_stamp");
            if (!Guid.TryParse(claim, out var userId))
            {
                context.RejectPrincipal();
                return;
            }
            var db = context.HttpContext.RequestServices.GetRequiredService<NexoraDbContext>();
            var user = await db.ApplicationUsers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
            if (user is null || user.Status != "active" || user.SecurityStamp != stamp)
                context.RejectPrincipal();
        };
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 12;
    })
    .AddUserStore<NexoraUserStore>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddScoped<IMediaCatalogRepository, MediaCatalogRepository>();
builder.Services.AddScoped<MediaCatalogService>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<AnalysisTaskService>();
builder.Services.AddScoped<UploadService>();
builder.Services.AddSingleton<IUploadStorage>(_ => new LocalUploadStorage(
    builder.Configuration["MediaStorage:RootPath"] ?? LocalUploadStorage.DefaultRootPath));

var app = builder.Build();

app.UseMiddleware<ApiExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<NexoraDbContext>();
    //await dbContext.Database.MigrateAsync();

    app.MapOpenApi();
}

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

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
