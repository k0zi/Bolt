using Bolt.Core.Model;

namespace Bolt.Core.Forges;

/// <summary>github.com through <c>api.github.com</c>.</summary>
public sealed class GitHubClient(HttpClient http, ITokenProvider tokens) : ForgeClientBase(http, tokens)
{
    private const string ApiBase = "https://api.github.com";

    public override ForgeKind Kind => ForgeKind.GitHub;

    public override async Task<IReadOnlyList<Release>> GetReleasesAsync(RepoRef repo, CancellationToken ct = default)
    {
        var json = await GetJsonAsync(repo, $"{ApiBase}/repos/{repo.ProjectPath}/releases?per_page=30", ct)
            .ConfigureAwait(false);
        return ParseReleases(json);
    }

    public override Task<byte[]?> TryGetFileAsync(RepoRef repo, string path, CancellationToken ct = default) =>
        GetBytesOrNullAsync(repo, $"{ApiBase}/repos/{repo.ProjectPath}/contents/{EscapePath(path)}",
                            "application/vnd.github.raw", ct);

    protected override void Authorize(HttpRequestMessage request, string token) =>
        request.Headers.Authorization = new("Bearer", token);

    internal static IReadOnlyList<Release> ParseReleases(string json) =>
        Deserialize<List<Dto>>(json)
            .Where(r => !r.Draft)
            .Select(r => new Release(r.TagName, r.Name, r.Prerelease, r.PublishedAt,
                                     r.Assets.Select(a => new ReleaseAsset(a.Name, a.BrowserDownloadUrl, a.Size))
                                             .ToList()))
            .ToList();

    internal static string EscapePath(string path) =>
        string.Join('/', path.Split('/').Select(Uri.EscapeDataString));

    private sealed record Dto(string TagName, string? Name, bool Draft, bool Prerelease,
                              DateTimeOffset? PublishedAt, List<AssetDto> Assets);

    private sealed record AssetDto(string Name, string BrowserDownloadUrl, long? Size);
}
