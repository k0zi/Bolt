using Bolt.Core.Model;

namespace Bolt.Core.Forges;

/// <summary>Turns repository URLs into <see cref="RepoRef"/>s and hands out the matching client.</summary>
public sealed class ForgeResolver(HttpClient http, IEnumerable<IForgeClient> clients)
{
    private readonly Dictionary<ForgeKind, IForgeClient> _clients = clients.ToDictionary(c => c.Kind);

    public IForgeClient GetClient(ForgeKind kind) => _clients[kind];

    /// <summary>
    /// Resolves a URL; for hosts that are not known it asks the server whether it is GitLab or Forgejo/Gitea.
    /// </summary>
    /// <exception cref="ForgeException">The URL is invalid or the host is not a supported forge.</exception>
    public async Task<RepoRef> ResolveAsync(string url, CancellationToken ct = default)
    {
        if (!RepoUrlParser.TryParse(url, out var parsed) || parsed is null)
            throw new ForgeException("Érvénytelen repó URL. Példa: https://github.com/tulajdonos/repo");

        var kind = parsed.Kind ?? await DetectAsync(parsed.BaseUrl, ct).ConfigureAwait(false)
                   ?? throw new ForgeException(
                       $"A(z) {new Uri(parsed.BaseUrl).Authority} nem GitHub, GitLab vagy Forgejo/Gitea szerver.");

        var path = parsed.ProjectPath;
        if (kind == ForgeKind.Forgejo && parsed.Kind is null)
            path = string.Join('/', path.Split('/').Take(2));

        return new RepoRef(kind, parsed.BaseUrl, path);
    }

    private async Task<ForgeKind?> DetectAsync(string baseUrl, CancellationToken ct)
    {
        // GitLab answers /api/v4/version with 401 without a token, which still identifies it.
        if (await RespondsAsync($"{baseUrl}/api/v4/version", ct).ConfigureAwait(false))
            return ForgeKind.GitLab;
        if (await RespondsAsync($"{baseUrl}/api/v1/version", ct).ConfigureAwait(false))
            return ForgeKind.Forgejo;
        return null;
    }

    private async Task<bool> RespondsAsync(string url, CancellationToken ct)
    {
        try
        {
            using var response = await http.GetAsync(url, ct).ConfigureAwait(false);
            var json = response.Content.Headers.ContentType?.MediaType == "application/json";
            return json && (response.IsSuccessStatusCode ||
                            response.StatusCode == System.Net.HttpStatusCode.Unauthorized);
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return false;
        }
    }
}
