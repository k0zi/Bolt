using Bolt.Core.Model;
using Bolt.Core.Resources;

namespace Bolt.Core.Platform;

/// <summary>Installs deb packages with apt and rpm packages with dnf or zypper, elevated through pkexec.</summary>
public sealed class LinuxInstaller(PackageManager manager, IProcessRunner runner) : IPackageInstaller
{
    private const string Pkexec = "pkexec";

    public async Task<InstalledPackage> InstallAsync(string file, PackageFormat format, string displayName,
                                                     CancellationToken ct = default)
    {
        var (queryTool, queryArgs) = PackageNameQuery(file, format);
        var query = await runner.RunAsync(queryTool, queryArgs, ct).ConfigureAwait(false);
        var name = query.StandardOutput.Trim();
        if (!query.Succeeded || name.Length == 0)
            throw new InstallerException(string.Format(Strings.PackageUnreadable, Path.GetFileName(file), query.Tail));

        await RunElevatedAsync(InstallCommand(manager, file), install: true, ct).ConfigureAwait(false);
        return new InstalledPackage(name);
    }

    public Task UninstallAsync(Installation installation, CancellationToken ct = default) =>
        RunElevatedAsync(RemoveCommand(manager, installation.PackageId), install: false, ct);

    public async Task<string?> GetInstalledVersionAsync(Installation installation, CancellationToken ct = default)
    {
        var (tool, args) = installation.PackageFormat == PackageFormat.Deb
            ? ("dpkg-query", new[] { "-W", "-f=${Status}|${Version}", installation.PackageId })
            : ("rpm", new[] { "-q", "--qf", "installed|%{VERSION}", installation.PackageId });
        var result = await runner.RunAsync(tool, args, ct).ConfigureAwait(false);
        if (!result.Succeeded)
            return null;

        // dpkg keeps removed packages with config files as "deinstall ok config-files".
        var parts = result.StandardOutput.Trim().Split('|', 2);
        return parts.Length == 2 && parts[0].EndsWith("installed", StringComparison.Ordinal) &&
               !parts[0].Contains("not-installed", StringComparison.Ordinal) &&
               !parts[0].Contains("config-files", StringComparison.Ordinal)
            ? parts[1]
            : null;
    }

    internal static (string Tool, string[] Args) PackageNameQuery(string file, PackageFormat format) => format switch
    {
        PackageFormat.Deb => ("dpkg-deb", ["-f", file, "Package"]),
        PackageFormat.Rpm => ("rpm", ["-qp", "--qf", "%{NAME}", file]),
        _ => throw new InstallerException(string.Format(Strings.FormatNotInstallableOnLinux, format)),
    };

    internal static string[] InstallCommand(PackageManager manager, string file) => manager switch
    {
        PackageManager.Apt => ["env", "DEBIAN_FRONTEND=noninteractive", "apt-get", "install", "-y", file],
        PackageManager.Dnf => ["dnf", "install", "-y", file],
        PackageManager.Zypper => ["zypper", "--non-interactive", "install", "--allow-unsigned-rpm", file],
        _ => throw new InstallerException(string.Format(Strings.UnsupportedPackageManager, manager)),
    };

    internal static string[] RemoveCommand(PackageManager manager, string package) => manager switch
    {
        PackageManager.Apt => ["env", "DEBIAN_FRONTEND=noninteractive", "apt-get", "remove", "-y", package],
        PackageManager.Dnf => ["dnf", "remove", "-y", package],
        PackageManager.Zypper => ["zypper", "--non-interactive", "remove", package],
        _ => throw new InstallerException(string.Format(Strings.UnsupportedPackageManager, manager)),
    };

    private async Task RunElevatedAsync(string[] command, bool install, CancellationToken ct)
    {
        var result = await runner.RunAsync(Pkexec, command, ct).ConfigureAwait(false);
        switch (result.ExitCode)
        {
            case 0:
                return;
            case 126:
                throw new InstallerException(install ? Strings.InstallDenied : Strings.UninstallDenied);
            case 127 when result.StandardError.Contains("Not authorized", StringComparison.OrdinalIgnoreCase):
                throw new InstallerException(install ? Strings.InstallNotAuthorized : Strings.UninstallNotAuthorized);
            default:
                throw new InstallerException(string.Format(install ? Strings.InstallFailed : Strings.UninstallFailed,
                                                          result.ExitCode, result.Tail));
        }
    }
}
