using Bolt.Core.Model;

namespace Bolt.Core.Forges;

/// <summary>The parts of a repository URL; <see cref="Kind"/> is <c>null</c> for unknown (self-hosted) hosts.</summary>
public sealed record ParsedRepoUrl(string BaseUrl, string ProjectPath, ForgeKind? Kind);

public static class RepoUrlParser
{
    private static readonly Dictionary<string, ForgeKind> KnownHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["github.com"] = ForgeKind.GitHub,
        ["www.github.com"] = ForgeKind.GitHub,
        ["gitlab.com"] = ForgeKind.GitLab,
        ["codeberg.org"] = ForgeKind.Forgejo,
    };

    /// <summary>
    /// Parses <c>https://host/owner/repo</c> style URLs (also without scheme, with <c>.git</c> suffix or with
    /// a sub page such as <c>/releases</c> or GitLab's <c>/-/releases</c>).
    /// </summary>
    public static bool TryParse(string? input, out ParsedRepoUrl? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var text = input.Trim();
        if (text.StartsWith("git@", StringComparison.Ordinal))
            text = "https://" + text[4..].Replace(':', '/');
        else if (!text.Contains("://", StringComparison.Ordinal))
            text = "https://" + text;

        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) ||
            string.IsNullOrEmpty(uri.Host))
            return false;

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries)
                          .Select(Uri.UnescapeDataString)
                          .ToList();

        // GitLab separates the project path from sub pages with "/-/".
        var dash = segments.IndexOf("-");
        if (dash >= 0)
            segments = segments[..dash];

        ForgeKind? kind = KnownHosts.TryGetValue(uri.Host, out var known) ? known : null;

        // GitHub and Forgejo have exactly owner/repo; GitLab may nest groups.
        if (kind is ForgeKind.GitHub or ForgeKind.Forgejo && segments.Count > 2)
            segments = segments[..2];

        if (segments.Count < 2)
            return false;

        if (segments[^1].EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            segments[^1] = segments[^1][..^4];

        if (segments.Any(s => s.Length == 0))
            return false;

        var host = kind == ForgeKind.GitHub ? "github.com" : uri.Authority.ToLowerInvariant();
        result = new ParsedRepoUrl($"{uri.Scheme}://{host}", string.Join('/', segments), kind);
        return true;
    }
}
