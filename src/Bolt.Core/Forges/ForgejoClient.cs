using Bolt.Core.Model;

namespace Bolt.Core.Forges;

/// <summary>Forgejo / Gitea hosts such as codeberg.org (<c>/api/v1</c>).</summary>
public sealed class ForgejoClient(HttpClient http, ITokenProvider tokens) : ForgeClientBase(http, tokens)
{
    public override ForgeKind Kind => ForgeKind.Forgejo;

    public override async Task<IReadOnlyList<Release>> GetReleasesAsync(RepoRef repo, CancellationToken ct = default)
    {
        var json = await GetJsonAsync(repo, $"{repo.BaseUrl}/api/v1/repos/{repo.ProjectPath}/releases?limit=30", ct)
            .ConfigureAwait(false);
        return ParseReleases(json);
    }

    public override Task<byte[]?> TryGetFileAsync(RepoRef repo, string path, CancellationToken ct = default) =>
        GetBytesOrNullAsync(repo,
                            $"{repo.BaseUrl}/api/v1/repos/{repo.ProjectPath}/raw/{GitHubClient.EscapePath(path)}",
                            null, ct);

    protected override void Authorize(HttpRequestMessage request, string token) =>
        request.Headers.Authorization = new("token", token);

    internal static IReadOnlyList<Release> ParseReleases(string json) =>
        Deserialize<List<Dto>>(json)
            .Where(r => !r.Draft)
            .Select(r => new Release(r.TagName, r.Name, r.Prerelease, r.PublishedAt,
                                     (r.Assets ?? []).Select(a => new ReleaseAsset(a.Name, a.BrowserDownloadUrl, a.Size))
                                                     .ToList()))
            .ToList();

    private sealed record Dto(string TagName, string? Name, bool Draft, bool Prerelease,
                              DateTimeOffset? PublishedAt, List<AssetDto>? Assets);

    private sealed record AssetDto(string Name, string BrowserDownloadUrl, long? Size);
}
