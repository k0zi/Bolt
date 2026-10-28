using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;
using Bolt.Core.Model;
using Microsoft.Win32;

namespace Bolt.Core.Platform;

/// <summary>
/// Runs msi (msiexec) and exe installers. The installed program is found by comparing the "Uninstall" registry
/// entries before and after the installation, which works for any installer technology.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsInstaller : IPackageInstaller
{
    private const string UninstallKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

    // msiexec: success, success with reboot required, reboot initiated.
    private static readonly int[] MsiSuccess = [0, 3010, 1641];
    private const int MsiUserCancel = 1602;

    public async Task<InstalledPackage> InstallAsync(string file, PackageFormat format, string displayName,
                                                     CancellationToken ct = default)
    {
        var before = Snapshot();
        int exitCode;
        if (format == PackageFormat.Msi)
        {
            exitCode = await RunAsync("msiexec.exe", $"/i \"{file}\" /passive /norestart", elevate: false, ct)
                .ConfigureAwait(false);
            if (exitCode == MsiUserCancel)
                throw new InstallerException("A telepítés megszakítva.");
            if (!MsiSuccess.Contains(exitCode))
                throw new InstallerException($"Az msiexec hibával állt le (kód: {exitCode}).");
        }
        else
        {
            exitCode = await RunAsync(file, "", elevate: true, ct).ConfigureAwait(false);
            if (exitCode != 0)
                throw new InstallerException($"A telepítő hibával állt le (kód: {exitCode}).");
        }

        var after = Snapshot();
        var entry = after.Values.Where(e => !before.ContainsKey(e.Id))
                         .OrderByDescending(e => Matches(e, displayName))
                         .FirstOrDefault()
                    // An upgrade keeps the existing entry: look it up by name.
                    ?? after.Values.FirstOrDefault(e => Matches(e, displayName))
                    ?? throw new InstallerException(
                        "A telepítés lefutott, de a program nem található a telepített programok között.");
        return new InstalledPackage(entry.Id, entry.UninstallCommand);
    }

    public async Task UninstallAsync(Installation installation, CancellationToken ct = default)
    {
        var command = Snapshot().GetValueOrDefault(installation.PackageId)?.UninstallCommand
                      ?? installation.UninstallCommand
                      ?? throw new InstallerException("Nem található eltávolító parancs.");
        var exitCode = await RunAsync("cmd.exe", $"/s /c \"{command}\"", elevate: true, ct).ConfigureAwait(false);
        if (exitCode == MsiUserCancel)
            throw new InstallerException("Az eltávolítás megszakítva.");
        if (!MsiSuccess.Contains(exitCode))
            throw new InstallerException($"Az eltávolítás hibával állt le (kód: {exitCode}).");
    }

    public Task<string?> GetInstalledVersionAsync(Installation installation, CancellationToken ct = default) =>
        Task.FromResult(Snapshot().TryGetValue(installation.PackageId, out var entry)
                            ? entry.DisplayVersion ?? installation.InstalledVersion
                            : null);

    private static bool Matches(UninstallEntry entry, string displayName) =>
        entry.DisplayName?.Contains(displayName, StringComparison.OrdinalIgnoreCase) == true;

    private static async Task<int> RunAsync(string fileName, string arguments, bool elevate, CancellationToken ct)
    {
        var info = new ProcessStartInfo(fileName, arguments) { UseShellExecute = elevate };
        if (elevate)
            info.Verb = "runas";
        try
        {
            using var process = Process.Start(info) ?? throw new InstallerException($"Nem indítható: {fileName}");
            await process.WaitForExitAsync(ct).ConfigureAwait(false);
            return process.ExitCode;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223) // ERROR_CANCELLED: UAC declined
        {
            throw new InstallerException("A művelet megszakítva: a rendszergazdai jogosultság el lett utasítva.", ex);
        }
    }

    private sealed record UninstallEntry(string Id, string? DisplayName, string? DisplayVersion,
                                         string? UninstallCommand);

    private static Dictionary<string, UninstallEntry> Snapshot()
    {
        var entries = new Dictionary<string, UninstallEntry>(StringComparer.OrdinalIgnoreCase);
        Read(RegistryHive.LocalMachine, RegistryView.Registry64, "HKLM64");
        Read(RegistryHive.LocalMachine, RegistryView.Registry32, "HKLM32");
        Read(RegistryHive.CurrentUser, RegistryView.Default, "HKCU");
        return entries;

        void Read(RegistryHive hive, RegistryView view, string prefix)
        {
            using var root = RegistryKey.OpenBaseKey(hive, view);
            using var uninstall = root.OpenSubKey(UninstallKey);
            if (uninstall is null)
                return;
            foreach (var name in uninstall.GetSubKeyNames())
            {
                using var key = uninstall.OpenSubKey(name);
                if (key?.GetValue("UninstallString") is not string uninstallString)
                    continue;
                // MSI products are keyed by ProductCode: uninstall without the full UI.
                var command = key.GetValue("WindowsInstaller") is 1 && name.StartsWith('{')
                    ? $"msiexec.exe /x {name} /passive /norestart"
                    : uninstallString;
                var id = $@"{prefix}\{name}";
                entries[id] = new UninstallEntry(id, key.GetValue("DisplayName") as string,
                                                 key.GetValue("DisplayVersion") as string, command);
            }
        }
    }
}
