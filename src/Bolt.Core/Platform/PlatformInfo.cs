using System.Runtime.InteropServices;
using Bolt.Core.Model;

namespace Bolt.Core.Platform;

public enum PackageManager
{
    /// <summary>The OS is not supported: nothing can be installed.</summary>
    None,
    Apt,
    Dnf,
    Zypper,
    Windows,
}

/// <param name="OsName">Display name, e.g. "Ubuntu 26.04".</param>
/// <param name="Formats">Installable package formats, most preferred first.</param>
public sealed record PlatformInfo(string OsName, PackageManager PackageManager, IReadOnlyList<PackageFormat> Formats,
                                  Architecture Architecture)
{
    public bool IsSupported => PackageManager != PackageManager.None;

    public override string ToString() =>
        $"{OsName} – {(IsSupported ? string.Join(", ", Formats).ToLowerInvariant() : "nem támogatott")}, " +
        $"{PackageManager.ToString().ToLowerInvariant()}, {Architecture.ToString().ToLowerInvariant()}";
}

public static class PlatformDetector
{
    public static PlatformInfo Detect()
    {
        var arch = RuntimeInformation.OSArchitecture;
        if (OperatingSystem.IsWindows())
            return new PlatformInfo(RuntimeInformation.OSDescription, PackageManager.Windows,
                                    [PackageFormat.Msi, PackageFormat.Exe], arch);
        if (OperatingSystem.IsLinux() && File.Exists("/etc/os-release"))
            return FromOsRelease(File.ReadAllText("/etc/os-release"), arch);
        return new PlatformInfo(RuntimeInformation.OSDescription, PackageManager.None, [], arch);
    }

    /// <summary>Picks the package manager from <c>/etc/os-release</c> (<c>ID</c> and <c>ID_LIKE</c>).</summary>
    public static PlatformInfo FromOsRelease(string osRelease, Architecture arch)
    {
        var values = osRelease.Split('\n')
                              .Select(l => l.Trim())
                              .Where(l => l.Length > 0 && !l.StartsWith('#') && l.Contains('='))
                              .Select(l => l.Split('=', 2))
                              .GroupBy(p => p[0])
                              .ToDictionary(g => g.Key, g => g.First()[1].Trim('"', '\''));

        var name = values.GetValueOrDefault("PRETTY_NAME") ?? values.GetValueOrDefault("NAME") ?? "Linux";
        var ids = $"{values.GetValueOrDefault("ID")} {values.GetValueOrDefault("ID_LIKE")}"
                  .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var id in ids)
        {
            switch (id)
            {
                case "debian" or "ubuntu":
                    return new PlatformInfo(name, PackageManager.Apt, [PackageFormat.Deb], arch);
                case "suse" or "opensuse" or "opensuse-leap" or "opensuse-tumbleweed" or "sles":
                    return new PlatformInfo(name, PackageManager.Zypper, [PackageFormat.Rpm], arch);
                case "fedora" or "rhel" or "centos":
                    return new PlatformInfo(name, PackageManager.Dnf, [PackageFormat.Rpm], arch);
            }
        }
        return new PlatformInfo(name, PackageManager.None, [], arch);
    }
}
