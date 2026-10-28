using Bolt.Core.Data;
using Bolt.Core.Forges;
using Bolt.Core.Metadata;
using Bolt.Core.Model;
using Bolt.Core.Platform;
using Bolt.Core.Resources;
using Microsoft.EntityFrameworkCore;

namespace Bolt.Core.Services;

public enum AddRepositoryStatus
{
    Added,
    AlreadyExists,
    NoRelease,
    NoCompatibleAsset,
    Failed,
}

public sealed record AddRepositoryResult(AddRepositoryStatus Status, string Message, StoreApp? App = null);

public enum OperationStage
{
    Downloading,
    Installing,
    Uninstalling,
}

/// <param name="Fraction">0–1 while downloading with a known size, otherwise <c>null</c>.</param>
public sealed record OperationProgress(OperationStage Stage, double? Fraction = null);

/// <summary>Result of an update check of one application.</summary>
public sealed record UpdateCheckResult(StoreApp App, bool UpdateAvailable, string? Error = null);

public sealed class StoreService(
    IDbContextFactory<BoltDbContext> dbFactory,
    ForgeResolver forges,
    BoltManifestReader manifestReader,
    AssetDownloader downloader,
    IPackageInstaller installer,
    PlatformInfo platform,
    AppPaths paths)
{
    public PlatformInfo Platform => platform;

    public async Task<IReadOnlyList<StoreApp>> GetAppsAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await db.Apps.Include(a => a.Installation).OrderBy(a => a.Name).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<StoreApp> GetAppAsync(int appId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await db.Apps.Include(a => a.Installation).FirstOrDefaultAsync(a => a.Id == appId, ct)
                            .ConfigureAwait(false)
               ?? throw new InvalidOperationException(Strings.AppNotFound);
    }

    /// <summary>
    /// Adds a repository if it has a release with a package for this OS; otherwise nothing is saved and the result
    /// tells why.
    /// </summary>
    public async Task<AddRepositoryResult> AddRepositoryAsync(string url, CancellationToken ct = default)
    {
        RepoRef repo;
        IReadOnlyList<Release> releases;
        try
        {
            repo = await forges.ResolveAsync(url, ct).ConfigureAwait(false);
            await using (var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false))
            {
                if (await db.Apps.AnyAsync(a => a.RepoUrl == repo.Url, ct).ConfigureAwait(false))
                    return new(AddRepositoryStatus.AlreadyExists, string.Format(Strings.RepoAlreadyExists, repo.Url));
            }
            releases = await forges.GetClient(repo.Kind).GetReleasesAsync(repo, ct).ConfigureAwait(false);
        }
        catch (ForgeException ex)
        {
            return new(AddRepositoryStatus.Failed, ex.Message);
        }

        if (!releases.Any(r => !r.IsPrerelease))
            return new(AddRepositoryStatus.NoRelease,
                       string.Format(Strings.RepoHasNoReleases, repo.Url));

        if (!platform.IsSupported)
            return new(AddRepositoryStatus.NoCompatibleAsset,
                       string.Format(Strings.OsNotSupportedNamed, platform.OsName));

        if (AssetMatcher.SelectLatest(releases, platform) is not ({ } release, { } asset))
            return new(AddRepositoryStatus.NoCompatibleAsset,
                       string.Format(Strings.RepoHasNoMatchingPackage, repo.Url, FormatList(), platform.Architecture));

        var app = new StoreApp
        {
            ForgeKind = repo.Kind,
            Host = repo.Host,
            ProjectPath = repo.ProjectPath,
            RepoUrl = repo.Url,
            Name = repo.RepoName,
            Author = repo.Owner,
            AddedAt = DateTimeOffset.UtcNow,
        };
        ApplyRelease(app, release, asset);

        await using (var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false))
        {
            db.Apps.Add(app);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        try
        {
            await RefreshMetadataAsync(app.Id, ct).ConfigureAwait(false);
        }
        catch (ForgeException)
        {
            // .bolt is optional: it is read again with the next update check
        }
        app = await GetAppAsync(app.Id, ct).ConfigureAwait(false);
        return new(AddRepositoryStatus.Added, string.Format(Strings.AppAdded, app.Name, app.LatestVersion), app);
    }

    /// <summary>Removes the application from the store (the installed program stays).</summary>
    public async Task RemoveAppAsync(int appId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var app = await db.Apps.AsTracking().Include(a => a.Installation).FirstOrDefaultAsync(a => a.Id == appId, ct)
                               .ConfigureAwait(false);
        if (app is null)
            return;
        db.Apps.Remove(app);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        DeleteIcons(appId);
    }

    /// <summary>Installs the latest release, or updates an installed application to it.</summary>
    public async Task<StoreApp> InstallAsync(int appId, IProgress<OperationProgress>? progress = null,
                                             CancellationToken ct = default)
    {
        var app = await GetAppAsync(appId, ct).ConfigureAwait(false);
        var repo = ToRepo(app);
        var client = forges.GetClient(repo.Kind);

        var releases = await client.GetReleasesAsync(repo, ct).ConfigureAwait(false);
        if (AssetMatcher.SelectLatest(releases, platform) is not ({ } release, { } asset))
            throw new InstallerException(Strings.NoInstallablePackage);
        var format = AssetMatcher.FormatOf(asset.Name)!.Value;

        progress?.Report(new(OperationStage.Downloading, 0));
        var file = await downloader.DownloadAsync(client, repo, asset,
                                                  new Progress<double>(f => progress?.Report(
                                                      new(OperationStage.Downloading, f))), ct)
                                   .ConfigureAwait(false);
        InstalledPackage package;
        try
        {
            progress?.Report(new(OperationStage.Installing));
            package = await installer.InstallAsync(file, format, app.Name, ct).ConfigureAwait(false);
        }
        finally
        {
            AssetDownloader.TryDelete(Path.GetDirectoryName(file)!);
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var tracked = await db.Apps.AsTracking().Include(a => a.Installation).FirstAsync(a => a.Id == appId, ct)
                                   .ConfigureAwait(false);
        ApplyRelease(tracked, release, asset);
        tracked.Installation ??= new Installation { InstalledVersion = "", PackageId = "" };
        tracked.Installation.InstalledVersion = release.TagName;
        tracked.Installation.PackageFormat = format;
        tracked.Installation.PackageId = package.PackageId;
        tracked.Installation.UninstallCommand = package.UninstallCommand;
        tracked.Installation.InstalledAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return await GetAppAsync(appId, ct).ConfigureAwait(false);
    }

    public async Task<StoreApp> UninstallAsync(int appId, IProgress<OperationProgress>? progress = null,
                                               CancellationToken ct = default)
    {
        var app = await GetAppAsync(appId, ct).ConfigureAwait(false);
        if (app.Installation is { } installation)
        {
            progress?.Report(new(OperationStage.Uninstalling));
            await installer.UninstallAsync(installation, ct).ConfigureAwait(false);
            await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            await db.Installations.Where(i => i.Id == installation.Id).ExecuteDeleteAsync(ct).ConfigureAwait(false);
        }
        return await GetAppAsync(appId, ct).ConfigureAwait(false);
    }

    /// <summary>Looks up the latest release (and refreshes the <c>.bolt</c> metadata).</summary>
    public async Task<UpdateCheckResult> CheckForUpdateAsync(int appId, CancellationToken ct = default)
    {
        var app = await GetAppAsync(appId, ct).ConfigureAwait(false);
        try
        {
            var repo = ToRepo(app);
            var releases = await forges.GetClient(repo.Kind).GetReleasesAsync(repo, ct).ConfigureAwait(false);
            var latest = AssetMatcher.SelectLatest(releases, platform);

            await using (var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false))
            {
                var tracked = await db.Apps.AsTracking().FirstAsync(a => a.Id == appId, ct).ConfigureAwait(false);
                if (latest is ({ } release, { } asset))
                    ApplyRelease(tracked, release, asset);
                tracked.LatestCheckedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
            }

            await RefreshMetadataAsync(appId, ct).ConfigureAwait(false);
            app = await GetAppAsync(appId, ct).ConfigureAwait(false);
            return new(app, IsUpdateAvailable(app));
        }
        catch (ForgeException ex)
        {
            return new(app, IsUpdateAvailable(app), ex.Message);
        }
    }

    /// <summary>Checks every installed application.</summary>
    public async Task<IReadOnlyList<UpdateCheckResult>> CheckForUpdatesAsync(CancellationToken ct = default)
    {
        var results = new List<UpdateCheckResult>();
        foreach (var app in (await GetAppsAsync(ct).ConfigureAwait(false)).Where(a => a.Installation is not null))
            results.Add(await CheckForUpdateAsync(app.Id, ct).ConfigureAwait(false));
        return results;
    }

    public static bool IsUpdateAvailable(StoreApp app) =>
        app.Installation is { } installation && VersionComparer.IsNewer(app.LatestVersion, installation.InstalledVersion);

    /// <summary>
    /// Aligns the database with the system: applications removed outside the store are marked not installed.
    /// </summary>
    public async Task SyncInstalledStateAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var installations = await db.Installations.AsTracking().ToListAsync(ct).ConfigureAwait(false);
        foreach (var installation in installations)
        {
            string? systemVersion;
            try
            {
                systemVersion = await installer.GetInstalledVersionAsync(installation, ct).ConfigureAwait(false);
            }
            catch (InstallerException)
            {
                continue; // query tool missing: keep the record
            }

            if (systemVersion is null)
                db.Installations.Remove(installation);
            else if (VersionComparer.IsNewer(StripPackageRevision(systemVersion), installation.InstalledVersion))
                installation.InstalledVersion = StripPackageRevision(systemVersion);
        }
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Uses a user chosen icon; from now on the <c>.bolt</c> folder is not read for this app.</summary>
    public async Task SetCustomIconAsync(int appId, string sourceFile, CancellationToken ct = default)
    {
        var bytes = await File.ReadAllBytesAsync(sourceFile, ct).ConfigureAwait(false);
        var iconPath = await SaveIconAsync(appId, bytes, Path.GetExtension(sourceFile), ct).ConfigureAwait(false);

        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await db.Apps.Where(a => a.Id == appId)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.IconPath, iconPath)
                                          .SetProperty(a => a.HasCustomIcon, true), ct)
                .ConfigureAwait(false);
    }

    /// <summary>Drops the user chosen icon and reads the <c>.bolt</c> folder again.</summary>
    public async Task ResetIconAsync(int appId, CancellationToken ct = default)
    {
        DeleteIcons(appId);
        await using (var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false))
        {
            await db.Apps.Where(a => a.Id == appId)
                    .ExecuteUpdateAsync(s => s.SetProperty(a => a.IconPath, (string?)null)
                                              .SetProperty(a => a.HasCustomIcon, false), ct)
                    .ConfigureAwait(false);
        }
        try
        {
            await RefreshMetadataAsync(appId, ct).ConfigureAwait(false);
        }
        catch (ForgeException)
        {
            // offline: the .bolt icon comes with the next update check
        }
    }

    /// <summary>Reads <c>.bolt/bolt.json</c> unless the user chose an icon for the app.</summary>
    private async Task RefreshMetadataAsync(int appId, CancellationToken ct)
    {
        var app = await GetAppAsync(appId, ct).ConfigureAwait(false);
        if (app.HasCustomIcon)
            return;

        var repo = ToRepo(app);
        var metadata = await manifestReader.ReadAsync(forges.GetClient(repo.Kind), repo, ct).ConfigureAwait(false);
        if (metadata is null)
            return;

        string? iconPath = null;
        if (metadata.Icon is { } icon)
            iconPath = await SaveIconAsync(appId, icon, metadata.IconExtension!, ct).ConfigureAwait(false);

        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var tracked = await db.Apps.AsTracking().FirstAsync(a => a.Id == appId, ct).ConfigureAwait(false);
        if (tracked.HasCustomIcon)
            return; // the user chose an icon meanwhile
        tracked.Name = metadata.Manifest.Name ?? repo.RepoName;
        tracked.Author = metadata.Manifest.Author ?? repo.Owner;
        tracked.BoltVersion = metadata.Manifest.Version;
        if (iconPath is not null)
            tracked.IconPath = iconPath;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private async Task<string> SaveIconAsync(int appId, byte[] bytes, string extension, CancellationToken ct)
    {
        Directory.CreateDirectory(paths.IconDirectory);
        DeleteIcons(appId);
        // A new name each time, so image caches never show the old icon.
        var path = Path.Combine(paths.IconDirectory, $"{appId}-{DateTime.UtcNow.Ticks}{extension.ToLowerInvariant()}");
        await File.WriteAllBytesAsync(path, bytes, ct).ConfigureAwait(false);
        return path;
    }

    private void DeleteIcons(int appId)
    {
        if (!Directory.Exists(paths.IconDirectory))
            return;
        foreach (var file in Directory.EnumerateFiles(paths.IconDirectory, $"{appId}-*"))
        {
            try
            {
                File.Delete(file);
            }
            catch (IOException)
            {
            }
        }
    }

    private static void ApplyRelease(StoreApp app, Release release, ReleaseAsset asset)
    {
        app.LatestVersion = release.TagName;
        app.LatestAssetName = asset.Name;
        app.LatestAssetUrl = asset.DownloadUrl;
        app.LatestCheckedAt = DateTimeOffset.UtcNow;
    }

    private static RepoRef ToRepo(StoreApp app)
    {
        var uri = new Uri(app.RepoUrl);
        return new(app.ForgeKind, $"{uri.Scheme}://{uri.Authority}", app.ProjectPath);
    }

    private string FormatList() =>
        string.Join("/", platform.Formats.Select(f => f.ToString().ToLowerInvariant()));

    /// <summary>"1:1.2.3-1ubuntu1" → "1.2.3": the package epoch and revision are not part of the release version.</summary>
    internal static string StripPackageRevision(string version)
    {
        var colon = version.IndexOf(':');
        if (colon >= 0)
            version = version[(colon + 1)..];
        var dash = version.IndexOf('-');
        return dash < 0 ? version : version[..dash];
    }
}
