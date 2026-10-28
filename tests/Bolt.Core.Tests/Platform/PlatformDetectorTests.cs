using System.Runtime.InteropServices;
using Bolt.Core.Model;
using Bolt.Core.Platform;

namespace Bolt.Core.Tests.Platform;

public class PlatformDetectorTests
{
    [Theory]
    [InlineData("ID=ubuntu\nID_LIKE=debian\nPRETTY_NAME=\"Ubuntu 26.04 LTS\"", PackageManager.Apt, PackageFormat.Deb)]
    [InlineData("ID=linuxmint\nID_LIKE=\"ubuntu debian\"", PackageManager.Apt, PackageFormat.Deb)]
    [InlineData("ID=fedora\nPRETTY_NAME=\"Fedora Linux 44\"", PackageManager.Dnf, PackageFormat.Rpm)]
    [InlineData("ID=\"opensuse-tumbleweed\"\nID_LIKE=\"opensuse suse\"", PackageManager.Zypper, PackageFormat.Rpm)]
    [InlineData("ID=rocky\nID_LIKE=\"rhel centos fedora\"", PackageManager.Dnf, PackageFormat.Rpm)]
    public void Detects_package_manager(string osRelease, PackageManager manager, PackageFormat format)
    {
        var info = PlatformDetector.FromOsRelease(osRelease, Architecture.X64);
        Assert.Equal(manager, info.PackageManager);
        Assert.Equal([format], info.Formats);
        Assert.True(info.IsSupported);
    }

    [Fact]
    public void Unknown_distribution_is_not_supported()
    {
        var info = PlatformDetector.FromOsRelease("ID=arch\nNAME=\"Arch Linux\"", Architecture.X64);
        Assert.False(info.IsSupported);
        Assert.Equal("Arch Linux", info.OsName);
    }
}
