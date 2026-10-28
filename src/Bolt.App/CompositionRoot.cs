using Bolt.App.Services;
using Bolt.App.ViewModels;
using Bolt.Core;
using Bolt.Core.Data;
using Bolt.Core.Forges;
using Bolt.Core.Metadata;
using Bolt.Core.Platform;
using Bolt.Core.Services;
using KD.Avalonia.Rice.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bolt.App;

internal static class CompositionRoot
{
    public static ServiceProvider Build(RiceWindow mainWindow)
    {
        var paths = new AppPaths();
        paths.EnsureCreated();
        var dbFactory = BoltDbFactory.ForFile(paths.DatabasePath);
        using (var db = dbFactory.CreateDbContext())
            db.Database.Migrate();

        // The default timeout ends when the response headers arrive; downloads stream after that.
        var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd($"Bolt/{typeof(App).Assembly.GetName().Version?.ToString(3)}");

        var platform = PlatformDetector.Detect();

        var services = new ServiceCollection();
        services.AddSingleton(paths);
        services.AddSingleton(platform);
        services.AddSingleton(http);
        services.AddSingleton<IDbContextFactory<BoltDbContext>>(dbFactory);
        services.AddSingleton<CredentialService>();
        services.AddSingleton<ITokenProvider>(sp => sp.GetRequiredService<CredentialService>());
        services.AddSingleton<IForgeClient, GitHubClient>();
        services.AddSingleton<IForgeClient, GitLabClient>();
        services.AddSingleton<IForgeClient, ForgejoClient>();
        services.AddSingleton<ForgeResolver>();
        services.AddSingleton<BoltManifestReader>();
        services.AddSingleton<AssetDownloader>();
        services.AddSingleton<IProcessRunner, ProcessRunner>();
        services.AddSingleton(sp => CreateInstaller(platform, sp.GetRequiredService<IProcessRunner>()));
        services.AddSingleton<StoreService>();

        services.AddSingleton<IDialogService>(new DialogService(mainWindow));
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<StoreViewModel>();
        services.AddSingleton<SettingsViewModel>();
        return services.BuildServiceProvider();
    }

    private static IPackageInstaller CreateInstaller(PlatformInfo platform, IProcessRunner runner) =>
        platform.PackageManager switch
        {
            PackageManager.Windows when OperatingSystem.IsWindows() => new WindowsInstaller(),
            PackageManager.Apt or PackageManager.Dnf or PackageManager.Zypper =>
                new LinuxInstaller(platform.PackageManager, runner),
            _ => new UnsupportedInstaller(),
        };
}
