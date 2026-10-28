using Bolt.Core.Platform;

namespace Bolt.Core.Tests.Platform;

public class VersionComparerTests
{
    [Theory]
    [InlineData("v1.2.10", "v1.2.9")]
    [InlineData("1.10", "1.9.9")]
    [InlineData("2.0.0", "2.0.0-rc.1")]
    [InlineData("2.0.0-rc.2", "2.0.0-rc.1")]
    [InlineData("2.0.0-rc.10", "2.0.0-rc.9")]
    [InlineData("app-2.0", "app-1.9")]
    [InlineData("release-1.0.1", "v1.0")]
    public void Newer_versions_compare_greater(string newer, string older)
    {
        Assert.True(VersionComparer.Instance.Compare(newer, older) > 0);
        Assert.True(VersionComparer.Instance.Compare(older, newer) < 0);
        Assert.True(VersionComparer.IsNewer(newer, older));
    }

    [Theory]
    [InlineData("v1.2.3", "1.2.3")]
    [InlineData("1.2", "1.2.0")]
    [InlineData("1.2.3+build5", "1.2.3")]
    public void Equal_versions(string a, string b) => Assert.Equal(0, VersionComparer.Instance.Compare(a, b));

    [Fact]
    public void Null_is_never_newer() => Assert.False(VersionComparer.IsNewer(null, "1.0"));

    [Theory]
    [InlineData("v1.2.3", "1.2.3")]
    [InlineData("release-2.0-beta+abc", "2.0-beta")]
    public void Normalize_strips_prefix_and_build(string tag, string expected) =>
        Assert.Equal(expected, VersionComparer.Normalize(tag));
}
