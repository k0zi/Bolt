namespace Bolt.Core.Model;

/// <summary>An application in the store, backed by a source repository with releases.</summary>
public class StoreApp
{
    public int Id { get; set; }

    public ForgeKind ForgeKind { get; set; }

    /// <summary>Host name, e.g. <c>github.com</c> or <c>gitlab.example.org</c> (may include a port).</summary>
    public required string Host { get; set; }

    /// <summary>Owner and repository (GitLab: full namespace path), e.g. <c>sharkdp/bat</c>.</summary>
    public required string ProjectPath { get; set; }

    /// <summary>Canonical repository URL (<c>https://host/path</c>), unique.</summary>
    public required string RepoUrl { get; set; }

    public required string Name { get; set; }

    public string? Author { get; set; }

    public string? Description { get; set; }

    /// <summary>Version declared in <c>.bolt/bolt.json</c>, informational only.</summary>
    public string? BoltVersion { get; set; }

    /// <summary>Local icon file (cached from <c>.bolt</c> or chosen by the user).</summary>
    public string? IconPath { get; set; }

    /// <summary>The user chose the icon: <c>.bolt</c> is not read for this app anymore.</summary>
    public bool HasCustomIcon { get; set; }

    public string? LatestVersion { get; set; }

    public string? LatestAssetName { get; set; }

    public string? LatestAssetUrl { get; set; }

    public DateTimeOffset? LatestCheckedAt { get; set; }

    public DateTimeOffset AddedAt { get; set; }

    public Installation? Installation { get; set; }
}
