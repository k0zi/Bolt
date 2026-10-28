using Bolt.Core.Forges;
using Bolt.Core.Model;

namespace Bolt.Core.Tests.Forges;

public class RepoUrlParserTests
{
    [Theory]
    [InlineData("https://github.com/sharkdp/bat", "https://github.com", "sharkdp/bat", ForgeKind.GitHub)]
    [InlineData("github.com/sharkdp/bat.git", "https://github.com", "sharkdp/bat", ForgeKind.GitHub)]
    [InlineData("https://github.com/sharkdp/bat/releases/tag/v1.0", "https://github.com", "sharkdp/bat", ForgeKind.GitHub)]
    [InlineData("git@github.com:sharkdp/bat.git", "https://github.com", "sharkdp/bat", ForgeKind.GitHub)]
    [InlineData("https://www.github.com/a/b/", "https://github.com", "a/b", ForgeKind.GitHub)]
    [InlineData("https://gitlab.com/group/sub/project", "https://gitlab.com", "group/sub/project", ForgeKind.GitLab)]
    [InlineData("https://gitlab.com/group/project/-/releases", "https://gitlab.com", "group/project", ForgeKind.GitLab)]
    [InlineData("https://codeberg.org/owner/repo/releases", "https://codeberg.org", "owner/repo", ForgeKind.Forgejo)]
    public void Parses_known_hosts(string url, string baseUrl, string path, ForgeKind kind)
    {
        Assert.True(RepoUrlParser.TryParse(url, out var parsed));
        Assert.Equal(new ParsedRepoUrl(baseUrl, path, kind), parsed);
    }

    [Fact]
    public void Unknown_host_keeps_scheme_port_and_has_no_kind()
    {
        Assert.True(RepoUrlParser.TryParse("http://Git.Example.org:8080/team/tools/app", out var parsed));
        Assert.Equal(new ParsedRepoUrl("http://git.example.org:8080", "team/tools/app", null), parsed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a url")]
    [InlineData("https://github.com/onlyowner")]
    [InlineData("ftp://github.com/a/b")]
    public void Rejects_invalid(string url) => Assert.False(RepoUrlParser.TryParse(url, out _));

    [Fact]
    public void RepoRef_exposes_parts()
    {
        var repo = new RepoRef(ForgeKind.GitLab, "https://gitlab.com", "group/sub/project");
        Assert.Equal("project", repo.RepoName);
        Assert.Equal("group", repo.Owner);
        Assert.Equal("gitlab.com", repo.Host);
        Assert.Equal("https://gitlab.com/group/sub/project", repo.Url);
    }
}
