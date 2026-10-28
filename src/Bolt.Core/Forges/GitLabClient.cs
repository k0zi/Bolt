using Bolt.Core.Model;

namespace Bolt.Core.Forges;

/// <summary>gitlab.com and self-hosted GitLab instances (<c>/api/v4</c>).</summary>
public sealed class GitLabClient(HttpClient http, ITokenProvider tokens) : ForgeClientBase(http, tokens)
{
    public override ForgeKind Kind => ForgeKind.GitLab;

    public override async Task<IReadOnlyList<Release>> GetReleasesAsync(RepoRef repo, CancellationToken ct = default)
    {
        var json = await GetJsonAsync(repo, $"{ProjectApi(repo)}/releases?per_page=30", ct).ConfigureAwait(false);
        return ParseReleases(json);
    }

    public override Task<byte[]?> TryGetFileAsync(RepoRef repo, string path, CancellationToken ct = default) =>
        GetBytesOrNullAsync(repo, $"{ProjectApi(repo)}/repository/files/{Uri.EscapeDataString(path)}/raw?ref=HEAD",
                            null, ct);

    protected override void Authorize(HttpRequestMessage request, string token) =>
        request.Headers.Add("PRIVATE-TOKEN", token);

    private static string ProjectApi(RepoRef repo) =>
        $"{repo.BaseUrl}/api/v4/projects/{Uri.EscapeDataString(repo.ProjectPath)}";

    internal static IReadOnlyList<Release> ParseReleases(string json) =>
        Deserialize<List<Dto>>(json)
            .Where(r => !r.UpcomingRelease)
            .Select(r => new Release(r.TagName, r.Name, false, r.ReleasedAt,
                                     (r.Assets?.Links ?? [])
                                     .Select(l => new ReleaseAsset(l.Name, l.DirectAssetUrl ?? l.Url))
                                     .ToList()))
            .ToList();

    private sealed record Dto(string TagName, string? Name, bool UpcomingRelease, DateTimeOffset? ReleasedAt,
                              AssetsDto? Assets);

    private sealed record AssetsDto(List<LinkDto>? Links);

    private sealed record LinkDto(string Name, string Url, string? DirectAssetUrl);
}
