using System.Text;
using Bolt.Core.Metadata;

namespace Bolt.Core.Tests.Metadata;

public class BoltManifestTests
{
    private static BoltManifest? Parse(string json) => BoltManifest.Parse(Encoding.UTF8.GetBytes(json));

    [Fact]
    public void Parses_all_fields()
    {
        var manifest = Parse("""
            { "name": "My App", "version": "1.2.3", "author": " Me ", "icon": "icon.png", // comment
            }
            """);
        Assert.Equal(new BoltManifest("My App", "1.2.3", "Me", "icon.png"), manifest);
        Assert.Equal(".bolt/icon.png", manifest!.IconRepoPath);
    }

    [Fact]
    public void Empty_values_become_null() =>
        Assert.Equal(new BoltManifest(null, null, null, null), Parse("""{ "name": "  " }"""));

    [Fact]
    public void Malformed_json_is_null() => Assert.Null(Parse("not json"));

    [Theory]
    [InlineData("./img/logo.png", ".bolt/img/logo.png")]
    [InlineData("/logo.png", ".bolt/logo.png")]
    [InlineData("../secret.png", null)]
    [InlineData("a/../../b.png", null)]
    public void Icon_path_stays_inside_folder(string icon, string? expected) =>
        Assert.Equal(expected, new BoltManifest(null, null, null, icon).IconRepoPath);
}
