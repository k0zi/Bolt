using Bolt.Core.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Bolt.Core.Data;

public class BoltDbContext(DbContextOptions<BoltDbContext> options) : DbContext(options)
{
    public DbSet<StoreApp> Apps => Set<StoreApp>();

    public DbSet<Installation> Installations => Set<Installation>();

    public DbSet<HostCredential> HostCredentials => Set<HostCredential>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StoreApp>(e =>
        {
            e.HasIndex(a => a.RepoUrl).IsUnique();
            e.Property(a => a.ForgeKind).HasConversion<string>();
            e.HasOne(a => a.Installation)
             .WithOne(i => i.StoreApp)
             .HasForeignKey<Installation>(i => i.StoreAppId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Installation>(e =>
        {
            e.Property(i => i.PackageFormat).HasConversion<string>();
        });

        modelBuilder.Entity<HostCredential>(e =>
        {
            e.HasIndex(c => c.Host).IsUnique();
        });
    }

    public static DbContextOptions<BoltDbContext> CreateOptions(string databasePath) =>
        new DbContextOptionsBuilder<BoltDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;
}

/// <summary>Used by <c>dotnet ef</c> to create migrations.</summary>
internal sealed class BoltDbContextDesignFactory : IDesignTimeDbContextFactory<BoltDbContext>
{
    public BoltDbContext CreateDbContext(string[] args) =>
        new(BoltDbContext.CreateOptions(Path.Combine(Path.GetTempPath(), "bolt-design.db")));
}
