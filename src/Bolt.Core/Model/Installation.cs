namespace Bolt.Core.Model;

/// <summary>An installed store application, as installed by the host OS installer.</summary>
public class Installation
{
    public int Id { get; set; }

    public int StoreAppId { get; set; }

    public StoreApp? StoreApp { get; set; }

    /// <summary>Release version (tag) that was installed.</summary>
    public required string InstalledVersion { get; set; }

    public PackageFormat PackageFormat { get; set; }

    /// <summary>deb/rpm package name, or the Windows uninstall registry key name.</summary>
    public required string PackageId { get; set; }

    /// <summary>Windows only: the command that uninstalls the application.</summary>
    public string? UninstallCommand { get; set; }

    public DateTimeOffset InstalledAt { get; set; }
}
