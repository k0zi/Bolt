namespace Bolt.Core.Model;

/// <summary>The API family of a source repository host.</summary>
public enum ForgeKind
{
    GitHub,
    GitLab,

    /// <summary>Forgejo / Gitea (Codeberg).</summary>
    Forgejo,
}

/// <summary>A package format handled by the host operating system's installer.</summary>
public enum PackageFormat
{
    Deb,
    Rpm,
    Msi,
    Exe,
}
