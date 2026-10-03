using Microsoft.EntityFrameworkCore;
using Nexora.Domain.Entities;
using Nexora.Infrastructure.Identity;

namespace Nexora.Infrastructure.Persistence;

public class NexoraDbContext : DbContext
{
    public NexoraDbContext(DbContextOptions<NexoraDbContext> options)
        : base(options)
    {
    }

    public DbSet<AnalysisTask> AnalysisTasks => Set<AnalysisTask>();

    public DbSet<AnalysisResult> AnalysisResults => Set<AnalysisResult>();
    public DbSet<TaskInput> TaskInputs => Set<TaskInput>();
    public DbSet<AnalysisTaskAttempt> AnalysisTaskAttempts => Set<AnalysisTaskAttempt>();
    public DbSet<GeneratedArtifact> GeneratedArtifacts => Set<GeneratedArtifact>();

    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();

    public DbSet<AssetFile> AssetFiles => Set<AssetFile>();

    public DbSet<MediaMetadata> MediaMetadatas => Set<MediaMetadata>();

    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(NexoraDbContext).Assembly);
    }
}
