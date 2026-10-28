using Bolt.Core.Model;
using Bolt.Core.Platform;

namespace Bolt.Core.Tests.Platform;

public class LinuxInstallerTests
{
    private sealed class FakeRunner(params ProcessResult[] results) : IProcessRunner
    {
        private readonly Queue<ProcessResult> _results = new(results);

        public List<string[]> Calls { get; } = [];

        public Task<ProcessResult> RunAsync(string fileName, IEnumerable<string> arguments, CancellationToken ct = default)
        {
            Calls.Add([fileName, .. arguments]);
            return Task.FromResult(_results.Dequeue());
        }
    }

    [Fact]
    public async Task Installs_deb_with_apt_through_pkexec()
    {
        var runner = new FakeRunner(new(0, "bat\n", ""), new(0, "", ""));
        var package = await new LinuxInstaller(PackageManager.Apt, runner)
            .InstallAsync("/tmp/bolt/x/bat.deb", PackageFormat.Deb, "bat");

        Assert.Equal("bat", package.PackageId);
        Assert.Equal(["dpkg-deb", "-f", "/tmp/bolt/x/bat.deb", "Package"], runner.Calls[0]);
        Assert.Equal(["pkexec", "env", "DEBIAN_FRONTEND=noninteractive", "apt-get", "install", "-y", "/tmp/bolt/x/bat.deb"],
                     runner.Calls[1]);
    }

    [Fact]
    public async Task Rpm_uses_rpm_query_and_zypper()
    {
        var runner = new FakeRunner(new(0, "app", ""), new(0, "", ""));
        await new LinuxInstaller(PackageManager.Zypper, runner).InstallAsync("/f/app.rpm", PackageFormat.Rpm, "App");

        Assert.Equal(["rpm", "-qp", "--qf", "%{NAME}", "/f/app.rpm"], runner.Calls[0]);
        Assert.Equal(["pkexec", "zypper", "--non-interactive", "install", "--allow-unsigned-rpm", "/f/app.rpm"],
                     runner.Calls[1]);
    }

    [Fact]
    public async Task Dismissed_password_prompt_reports_cancel()
    {
        var runner = new FakeRunner(new ProcessResult(126, "", ""));
        var installation = new Installation { InstalledVersion = "1", PackageId = "app", PackageFormat = PackageFormat.Rpm };
        var ex = await Assert.ThrowsAsync<InstallerException>(
            () => new LinuxInstaller(PackageManager.Dnf, runner).UninstallAsync(installation));

        Assert.Contains("megszakítva", ex.Message);
        Assert.Equal(["pkexec", "dnf", "remove", "-y", "app"], runner.Calls[0]);
    }

    [Theory]
    [InlineData(0, "install ok installed|0.25.0-1", "0.25.0-1")]
    [InlineData(0, "deinstall ok config-files|0.25.0", null)]
    [InlineData(0, "unknown ok not-installed|", null)]
    [InlineData(1, "", null)]
    public async Task Reads_deb_installed_version(int exitCode, string output, string? expected)
    {
        var runner = new FakeRunner(new ProcessResult(exitCode, output, ""));
        var installation = new Installation { InstalledVersion = "1", PackageId = "bat", PackageFormat = PackageFormat.Deb };
        Assert.Equal(expected, await new LinuxInstaller(PackageManager.Apt, runner).GetInstalledVersionAsync(installation));
    }
}
