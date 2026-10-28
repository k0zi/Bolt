using System.Runtime.InteropServices;
using System.Text;
using Bolt.Core.Data;
using Bolt.Core.Forges;
using Bolt.Core.Metadata;
using Bolt.Core.Model;
using Bolt.Core.Platform;
using Bolt.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace Bolt.Core.Tests.Services;

public sealed class StoreServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "bolt-tests-" + Guid.NewGuid().ToString("N"));
    private readonly FakeForge _forge = new();
    private readonly FakeInstaller _installer = new();
    private readonly StoreService _store;
    private readonly BoltDbFactory _db;

    public StoreServiceTests()
    {
        var paths = new AppPaths(_dir);
        paths.EnsureCreated();
        _db = BoltDbFactory.ForFile(paths.DatabasePath);
        using (var db = _db.CreateDbContext())
            db.Database.Migrate();

        var platform = new PlatformInfo("Ubuntu", PackageManager.Apt, [PackageFormat.Deb], Architecture.X64);
        _store = new StoreService(_db, new ForgeResolver(new HttpClient(), [_forge]), new BoltManifestReader(),
                                  new AssetDownloader(paths), _installer, platform, paths);
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        Directory.Delete(_dir, recursive: true);
    }

    private static Release Release(string tag, params string[] assets) =>
        new(tag, tag, false, null, assets.Select(a => new ReleaseAsset(a, "https://github.com/dl/" + a)).ToList());

    [Fact]
    public async Task Repository_without_release_is_not_added()
    {
        var result = await _store.AddRepositoryAsync("https://github.com/o/empty");

        Assert.Equal(AddRepositoryStatus.NoRelease, result.Status);
        Assert.Empty(await _store.GetAppsAsync());
    }

    [Fact]
    public async Task Repository_without_package_for_this_os_is_not_added()
    {
        _forge.Releases["o/r"] = [Release("v1", "r.tar.gz", "r_arm64.deb")];

        var result = await _store.AddRepositoryAsync("https://github.com/o/r");

        Assert.Equal(AddRepositoryStatus.NoCompatibleAsset, result.Status);
        Assert.Empty(await _store.GetAppsAsync());
    }

    [Fact]
    public async Task Adds_repository_with_bolt_metadata_and_rejects_duplicate()
    {
        _forge.Releases["o/r"] = [Release("v1.0", "r_1.0_amd64.deb")];
        _forge.Files["o/r/.bolt/bolt.json"] = Encoding.UTF8.GetBytes(
            """{ "name": "Rocket", "version": "1.0", "author": "Team", "icon": "logo.png" }""");
        _forge.Files["o/r/.bolt/logo.png"] = [1, 2, 3];

        var result = await _store.AddRepositoryAsync("github.com/o/r");

        Assert.Equal(AddRepositoryStatus.Added, result.Status);
        var app = result.App!;
        Assert.Equal(("Rocket", "Team", "1.0", "v1.0", "r_1.0_amd64.deb"),
                     (app.Name, app.Author, app.BoltVersion, app.LatestVersion, app.LatestAssetName));
        Assert.Equal([1, 2, 3], await File.ReadAllBytesAsync(app.IconPath!));

        Assert.Equal(AddRepositoryStatus.AlreadyExists, (await _store.AddRepositoryAsync("https://github.com/o/r.git")).Status);
    }

    [Fact]
    public async Task Without_bolt_folder_the_repository_names_the_app()
    {
        _forge.Releases["owner/tool"] = [Release("v1", "tool_amd64.deb")];

        var app = (await _store.AddRepositoryAsync("https://github.com/owner/tool")).App!;

        Assert.Equal(("tool", "owner", null), (app.Name, app.Author, app.IconPath));
    }

    [Fact]
    public async Task Custom_icon_stops_reading_bolt_folder()
    {
        _forge.Releases["o/r"] = [Release("v1", "r_amd64.deb")];
        _forge.Files["o/r/.bolt/bolt.json"] = Encoding.UTF8.GetBytes("""{ "name": "Rocket", "icon": "i.png" }""");
        _forge.Files["o/r/.bolt/i.png"] = [1];
        var app = (await _store.AddRepositoryAsync("https://github.com/o/r")).App!;

        var custom = Path.Combine(_dir, "mine.png");
        await File.WriteAllBytesAsync(custom, [9, 9]);
        await _store.SetCustomIconAsync(app.Id, custom);
        _forge.FileReads.Clear();
        _forge.Releases["o/r"] = [Release("v2", "r_amd64.deb")];
        var checkedApp = (await _store.CheckForUpdateAsync(app.Id)).App;

        Assert.True(checkedApp.HasCustomIcon);
        Assert.Equal([9, 9], await File.ReadAllBytesAsync(checkedApp.IconPath!));
        Assert.Empty(_forge.FileReads);
        Assert.Equal("v2", checkedApp.LatestVersion);

        await _store.ResetIconAsync(app.Id);
        var reset = await _store.GetAppAsync(app.Id);
        Assert.False(reset.HasCustomIcon);
        Assert.Equal([1], await File.ReadAllBytesAsync(reset.IconPath!));
    }

    [Fact]
    public async Task Install_check_update_and_uninstall()
    {
        _forge.Releases["o/r"] = [Release("v1.0", "r_1.0_amd64.deb")];
        var app = (await _store.AddRepositoryAsync("https://github.com/o/r")).App!;

        var stages = new List<OperationStage>();
        var installed = await _store.InstallAsync(app.Id, new SyncProgress(p => stages.Add(p.Stage)));

        Assert.Equal("v1.0", installed.Installation?.InstalledVersion);
        Assert.Equal("r", installed.Installation?.PackageId);
        Assert.Equal("r_1.0_amd64.deb", Path.GetFileName(_installer.InstalledFiles.Single()));
        Assert.Contains(OperationStage.Installing, stages);
        Assert.False(StoreService.IsUpdateAvailable(installed));

        _forge.Releases["o/r"] = [Release("v1.1", "r_1.1_amd64.deb"), Release("v1.0", "r_1.0_amd64.deb")];
        var check = Assert.Single(await _store.CheckForUpdatesAsync());
        Assert.True(check.UpdateAvailable);

        var updated = await _store.InstallAsync(app.Id);
        Assert.Equal("v1.1", updated.Installation?.InstalledVersion);
        Assert.False(StoreService.IsUpdateAvailable(updated));

        var removed = await _store.UninstallAsync(app.Id);
        Assert.Null(removed.Installation);
        Assert.Equal(["r"], _installer.Uninstalled);
        Assert.All(_installer.InstalledFiles, f => Assert.False(File.Exists(f))); // downloads are cleaned up
    }

    [Fact]
    public async Task Sync_drops_packages_removed_outside_the_store()
    {
        _forge.Releases["o/r"] = [Release("v1.0", "r_amd64.deb")];
        var app = (await _store.AddRepositoryAsync("https://github.com/o/r")).App!;
        await _store.InstallAsync(app.Id);

        _installer.SystemVersion = null;
        await _store.SyncInstalledStateAsync();

        Assert.Null((await _store.GetAppAsync(app.Id)).Installation);
    }

    [Theory]
    [InlineData("1:1.2.3-1ubuntu1", "1.2.3")]
    [InlineData("2.0", "2.0")]
    public void Strips_package_revision(string version, string expected) =>
        Assert.Equal(expected, StoreService.StripPackageRevision(version));

    private sealed class SyncProgress(Action<OperationProgress> report) : IProgress<OperationProgress>
    {
        public void Report(OperationProgress value) => report(value);
    }

    private sealed class FakeForge : IForgeClient
    {
        public Dictionary<string, IReadOnlyList<Release>> Releases { get; } = [];
        public Dictionary<string, byte[]> Files { get; } = [];
        public List<string> FileReads { get; } = [];

        public ForgeKind Kind => ForgeKind.GitHub;

        public Task<IReadOnlyList<Release>> GetReleasesAsync(RepoRef repo, CancellationToken ct = default) =>
            Task.FromResult(Releases.GetValueOrDefault(repo.ProjectPath) ?? []);

        public Task<byte[]?> TryGetFileAsync(RepoRef repo, string path, CancellationToken ct = default)
        {
            FileReads.Add(path);
            return Task.FromResult(Files.GetValueOrDefault($"{repo.ProjectPath}/{path}"));
        }

        public Task<HttpResponseMessage> OpenDownloadAsync(RepoRef repo, string url, CancellationToken ct = default) =>
            Task.FromResult(new HttpResponseMessage { Content = new ByteArrayContent(new byte[1000]) });
    }

    private sealed class FakeInstaller : IPackageInstaller
    {
        public List<string> InstalledFiles { get; } = [];
        public List<string> Uninstalled { get; } = [];
        public string? SystemVersion { get; set; } = "1.0";

        public Task<InstalledPackage> InstallAsync(string file, PackageFormat format, string displayName,
                                                   CancellationToken ct = default)
        {
            Assert.True(File.Exists(file));
            InstalledFiles.Add(file);
            return Task.FromResult(new InstalledPackage("r"));
        }

        public Task UninstallAsync(Installation installation, CancellationToken ct = default)
        {
            Uninstalled.Add(installation.PackageId);
            return Task.CompletedTask;
        }

        public Task<string?> GetInstalledVersionAsync(Installation installation, CancellationToken ct = default) =>
            Task.FromResult(SystemVersion);
    }
}
