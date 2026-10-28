using Bolt.Core.Model;

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
            throw new InstallerException($"A csomag nem olvasható: {Path.GetFileName(file)}\n{query.Tail}");

        await RunElevatedAsync(InstallCommand(manager, file), "A telepítés", ct).ConfigureAwait(false);
        return new InstalledPackage(name);
    }

    public Task UninstallAsync(Installation installation, CancellationToken ct = default) =>
        RunElevatedAsync(RemoveCommand(manager, installation.PackageId), "Az eltávolítás", ct);

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
        _ => throw new InstallerException($"Linuxon nem telepíthető formátum: {format}"),
    };

    internal static string[] InstallCommand(PackageManager manager, string file) => manager switch
    {
        PackageManager.Apt => ["env", "DEBIAN_FRONTEND=noninteractive", "apt-get", "install", "-y", file],
        PackageManager.Dnf => ["dnf", "install", "-y", file],
        PackageManager.Zypper => ["zypper", "--non-interactive", "install", "--allow-unsigned-rpm", file],
        _ => throw new InstallerException($"Nem támogatott csomagkezelő: {manager}"),
    };

    internal static string[] RemoveCommand(PackageManager manager, string package) => manager switch
    {
        PackageManager.Apt => ["env", "DEBIAN_FRONTEND=noninteractive", "apt-get", "remove", "-y", package],
        PackageManager.Dnf => ["dnf", "remove", "-y", package],
        PackageManager.Zypper => ["zypper", "--non-interactive", "remove", package],
        _ => throw new InstallerException($"Nem támogatott csomagkezelő: {manager}"),
    };

    private async Task RunElevatedAsync(string[] command, string what, CancellationToken ct)
    {
        var result = await runner.RunAsync(Pkexec, command, ct).ConfigureAwait(false);
        switch (result.ExitCode)
        {
            case 0:
                return;
            case 126:
                throw new InstallerException($"{what} megszakítva: a jogosultság-kérés el lett utasítva.");
            case 127 when result.StandardError.Contains("Not authorized", StringComparison.OrdinalIgnoreCase):
                throw new InstallerException($"{what} sikertelen: nincs jogosultság.");
            default:
                throw new InstallerException($"{what} sikertelen (kilépési kód: {result.ExitCode}).\n{result.Tail}");
        }
    }
}
