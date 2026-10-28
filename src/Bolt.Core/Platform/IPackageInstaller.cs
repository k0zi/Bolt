using Bolt.Core.Model;

namespace Bolt.Core.Platform;

/// <summary>What the OS installer reports after installing a package.</summary>
/// <param name="PackageId">deb/rpm package name, or the Windows uninstall registry key.</param>
public sealed record InstalledPackage(string PackageId, string? UninstallCommand = null);

/// <summary>Installs and removes packages with the host OS installer.</summary>
public interface IPackageInstaller
{
    /// <summary>Installs (or upgrades to) the package in <paramref name="file"/>.</summary>
    /// <param name="displayName">The application name, used to find the installed entry on Windows.</param>
    Task<InstalledPackage> InstallAsync(string file, PackageFormat format, string displayName,
                                        CancellationToken ct = default);

    Task UninstallAsync(Installation installation, CancellationToken ct = default);

    /// <summary>The version the OS reports, or <c>null</c> when the package is not installed anymore.</summary>
    Task<string?> GetInstalledVersionAsync(Installation installation, CancellationToken ct = default);
}

/// <summary>Used when the OS is not supported.</summary>
public sealed class UnsupportedInstaller : IPackageInstaller
{
    public Task<InstalledPackage> InstallAsync(string file, PackageFormat format, string displayName,
                                               CancellationToken ct = default) =>
        throw new InstallerException("Ez az operációs rendszer nem támogatott.");

    public Task UninstallAsync(Installation installation, CancellationToken ct = default) =>
        throw new InstallerException("Ez az operációs rendszer nem támogatott.");

    public Task<string?> GetInstalledVersionAsync(Installation installation, CancellationToken ct = default) =>
        Task.FromResult<string?>(installation.InstalledVersion);
}
