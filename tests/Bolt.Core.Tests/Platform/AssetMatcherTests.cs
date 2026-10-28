using System.Runtime.InteropServices;
using Bolt.Core.Forges;
using Bolt.Core.Model;
using Bolt.Core.Platform;

namespace Bolt.Core.Tests.Platform;

public class AssetMatcherTests
{
    private static readonly PlatformInfo UbuntuX64 = new("Ubuntu", PackageManager.Apt, [PackageFormat.Deb], Architecture.X64);
    private static readonly PlatformInfo FedoraArm = new("Fedora", PackageManager.Dnf, [PackageFormat.Rpm], Architecture.Arm64);
    private static readonly PlatformInfo WindowsX64 =
        new("Windows", PackageManager.Windows, [PackageFormat.Msi, PackageFormat.Exe], Architecture.X64);

    private static ReleaseAsset[] Assets(params string[] names) =>
        names.Select(n => new ReleaseAsset(n, "https://example.org/" + n)).ToArray();

    private static readonly ReleaseAsset[] Bat = Assets(
        "bat-musl_0.25.0_amd64.deb", "bat_0.25.0_amd64.deb", "bat_0.25.0_arm64.deb", "bat_0.25.0_armhf.deb",
        "bat_0.25.0_i686.deb", "bat-v0.25.0-x86_64-unknown-linux-gnu.tar.gz", "bat-v0.25.0-x86_64-pc-windows-msvc.zip",
        "app-1.0.x86_64.rpm", "app-1.0.aarch64.rpm", "app-1.0.src.rpm", "app-debuginfo-1.0.aarch64.rpm",
        "Setup-x64.exe", "App-1.0-win64.msi", "App-1.0-win32.msi");

    [Fact]
    public void Picks_native_arch_deb_and_prefers_glibc() =>
        Assert.Equal("bat_0.25.0_amd64.deb", AssetMatcher.SelectAsset(Bat, UbuntuX64)?.Name);

    [Fact]
    public void Picks_arm_rpm_and_skips_source_and_debug() =>
        Assert.Equal("app-1.0.aarch64.rpm", AssetMatcher.SelectAsset(Bat, FedoraArm)?.Name);

    [Fact]
    public void Windows_prefers_msi_of_matching_arch() =>
        Assert.Equal("App-1.0-win64.msi", AssetMatcher.SelectAsset(Bat, WindowsX64)?.Name);

    [Fact]
    public void Windows_falls_back_to_exe() =>
        Assert.Equal("Setup-x64.exe", AssetMatcher.SelectAsset(Assets("Setup-x64.exe", "Setup-arm64.exe"), WindowsX64)?.Name);

    [Fact]
    public void Arch_neutral_package_matches() =>
        Assert.Equal("tool_1.0_all.deb", AssetMatcher.SelectAsset(Assets("tool_1.0_all.deb", "tool_1.0_arm64.deb"), UbuntuX64)?.Name);

    [Fact]
    public void Native_arch_beats_neutral() =>
        Assert.Equal("tool_amd64.deb", AssetMatcher.SelectAsset(Assets("tool.deb", "tool_amd64.deb"), UbuntuX64)?.Name);

    [Fact]
    public void No_match_for_other_arch_or_format()
    {
        Assert.Null(AssetMatcher.SelectAsset(Assets("tool_arm64.deb", "tool.tar.gz", "tool.AppImage"), UbuntuX64));
        Assert.Null(AssetMatcher.SelectAsset([], UbuntuX64));
    }

    [Fact]
    public void Latest_skips_prereleases_and_releases_without_package()
    {
        Release[] releases =
        [
            new("v3.0-beta", null, true, null, Assets("t_amd64.deb")),
            new("v2.0", null, false, null, Assets("t.tar.gz")),
            new("v1.0", null, false, null, Assets("t_amd64.deb")),
        ];
        var latest = AssetMatcher.SelectLatest(releases, UbuntuX64);
        Assert.Equal("v1.0", latest?.Release.TagName);
    }
}
