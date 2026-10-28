using Microsoft.EntityFrameworkCore;

namespace Bolt.Core.Data;

/// <summary>Creates a short-lived <see cref="BoltDbContext"/> per unit of work.</summary>
public sealed class BoltDbFactory(DbContextOptions<BoltDbContext> options) : IDbContextFactory<BoltDbContext>
{
    public BoltDbContext CreateDbContext() => new(options);

    public static BoltDbFactory ForFile(string databasePath) => new(BoltDbContext.CreateOptions(databasePath));
}
