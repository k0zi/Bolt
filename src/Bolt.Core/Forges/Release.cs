using Bolt.Core.Model;

namespace Bolt.Core.Forges;

public sealed record ReleaseAsset(string Name, string DownloadUrl, long? Size = null);

public sealed record Release(string TagName, string? Name, bool IsPrerelease, DateTimeOffset? PublishedAt,
                             IReadOnlyList<ReleaseAsset> Assets);

/// <summary>A repository on a forge host.</summary>
/// <param name="BaseUrl">Scheme and host, e.g. <c>https://gitlab.example.org</c>.</param>
/// <param name="ProjectPath">Owner/repository, or the full GitLab namespace path.</param>
public sealed record RepoRef(ForgeKind Kind, string BaseUrl, string ProjectPath)
{
    public string Host => new Uri(BaseUrl).Authority;

    public string Url => $"{BaseUrl}/{ProjectPath}";

    /// <summary>Last path segment, the repository name.</summary>
    public string RepoName => ProjectPath[(ProjectPath.LastIndexOf('/') + 1)..];

    /// <summary>First path segment, the owner (user or group).</summary>
    public string Owner => ProjectPath[..ProjectPath.IndexOf('/')];
}

public interface IForgeClient
{
    ForgeKind Kind { get; }

    /// <summary>Published releases, newest first, without drafts.</summary>
    Task<IReadOnlyList<Release>> GetReleasesAsync(RepoRef repo, CancellationToken ct = default);

    /// <summary>Raw content of a file on the default branch, or <c>null</c> if it does not exist.</summary>
    Task<byte[]?> TryGetFileAsync(RepoRef repo, string path, CancellationToken ct = default);

    /// <summary>Starts downloading a release asset (response headers read, content streamed by the caller).</summary>
    Task<HttpResponseMessage> OpenDownloadAsync(RepoRef repo, string url, CancellationToken ct = default);
}

public sealed class ForgeException(string message, Exception? inner = null) : Exception(message, inner);
