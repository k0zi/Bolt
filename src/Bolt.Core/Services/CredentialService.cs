using Bolt.Core.Data;
using Bolt.Core.Forges;
using Bolt.Core.Model;
using Bolt.Core.Resources;
using Microsoft.EntityFrameworkCore;

namespace Bolt.Core.Services;

/// <summary>API tokens per forge host, stored in the database.</summary>
public sealed class CredentialService(IDbContextFactory<BoltDbContext> dbFactory) : ITokenProvider
{
    public async Task<string?> GetTokenAsync(string host, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var key = NormalizeHost(host);
        return await db.HostCredentials.Where(c => c.Host == key).Select(c => c.Token)
                       .FirstOrDefaultAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<HostCredential>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await db.HostCredentials.OrderBy(c => c.Host).ToListAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Adds or replaces the token of a host. Accepts a host name or a URL.</summary>
    public async Task SetAsync(string host, string token, CancellationToken ct = default)
    {
        var key = NormalizeHost(host);
        if (key.Length == 0 || string.IsNullOrWhiteSpace(token))
            throw new ArgumentException(Strings.TokenHostEmpty);

        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var existing = await db.HostCredentials.AsTracking().FirstOrDefaultAsync(c => c.Host == key, ct)
                               .ConfigureAwait(false);
        if (existing is null)
            db.HostCredentials.Add(new HostCredential { Host = key, Token = token.Trim() });
        else
            existing.Token = token.Trim();
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task RemoveAsync(int id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await db.HostCredentials.Where(c => c.Id == id).ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }

    /// <summary><c>https://GitHub.com/x</c> → <c>github.com</c>.</summary>
    public static string NormalizeHost(string host)
    {
        var text = host.Trim();
        if (text.Contains("://", StringComparison.Ordinal) && Uri.TryCreate(text, UriKind.Absolute, out var uri))
            return uri.Authority.ToLowerInvariant();
        return text.Split('/')[0].ToLowerInvariant();
    }
}
