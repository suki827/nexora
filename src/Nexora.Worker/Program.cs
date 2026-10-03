using Nexora.Worker;
using Microsoft.EntityFrameworkCore;
using Nexora.Application.MediaCatalog;
using Nexora.Infrastructure.Persistence;
using Nexora.Infrastructure.Storage;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration
    .AddJsonFile(Path.GetFullPath(Path.Combine(
        builder.Environment.ContentRootPath, "..", "..", "appsettings.Local.json")), optional: true)
    .AddEnvironmentVariables();
var connectionString = builder.Configuration.GetConnectionString("NexoraDatabase")
    ?? throw new InvalidOperationException("Connection string 'NexoraDatabase' was not found.");
builder.Services.AddDbContext<NexoraDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddSingleton<IUploadStorage>(_ => new LocalUploadStorage(
    builder.Configuration["MediaStorage:RootPath"] ?? LocalUploadStorage.DefaultRootPath));
builder.Services.AddHostedService<Worker>();
builder.Services.AddScoped<TaskProcessor>();
builder.Services.AddHostedService<AnalysisTaskWorker>();

var host = builder.Build();
host.Run();
