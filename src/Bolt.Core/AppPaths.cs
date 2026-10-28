namespace Bolt.Core;

/// <summary>Locations of the store's local files.</summary>
public sealed class AppPaths
{
    public AppPaths(string? dataDirectory = null)
    {
        DataDirectory = dataDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData,
                                      Environment.SpecialFolderOption.Create),
            "bolt");
    }

    /// <summary>Linux: <c>~/.local/share/bolt</c>, Windows: <c>%LOCALAPPDATA%\bolt</c>.</summary>
    public string DataDirectory { get; }

    public string DatabasePath => Path.Combine(DataDirectory, "bolt.db");

    public string IconDirectory => Path.Combine(DataDirectory, "icons");

    public string DownloadDirectory => Path.Combine(Path.GetTempPath(), "bolt");

    public void EnsureCreated()
    {
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(IconDirectory);
        Directory.CreateDirectory(DownloadDirectory);
    }
}
