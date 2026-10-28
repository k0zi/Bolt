using Avalonia;
using KD.Avalonia.Rice;
using KD.Avalonia.Rice.Options;
using KD.Avalonia.Rice.Theming;

namespace Bolt.App;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseRice(options =>
            {
                options.AppInfo = new RiceAppInfo
                {
                    AppId = "bolt",
                    Name = "Bolt",
                    Author = "k0zi",
                    Description = "Szoftver store GitHub, GitLab és Codeberg kiadásokból, a rendszer saját telepítőjével.",
                    IconUri = "avares://Bolt/Assets/bolt.png",
                };
                options.SupportedThemes = BuiltInThemes.All;
                options.DefaultThemeId = DefaultTheme().Id;
            });

    // The theme of the running distribution, if there is one.
    private static RiceTheme DefaultTheme()
    {
        if (OperatingSystem.IsWindows())
            return BuiltInThemes.Windows11;
        var osRelease = File.Exists("/etc/os-release") ? File.ReadAllText("/etc/os-release").ToLowerInvariant() : "";
        if (osRelease.Contains("fedora"))
            return BuiltInThemes.Fedora;
        if (osRelease.Contains("opensuse") || osRelease.Contains("suse"))
            return BuiltInThemes.OpenSuse;
        if (osRelease.Contains("manjaro"))
            return BuiltInThemes.Manjaro;
        return BuiltInThemes.Ubuntu;
    }
}
