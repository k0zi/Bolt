using Bolt.Core.Forges;

namespace Bolt.Core.Tests.Forges;

public class ReleaseParsingTests
{
    [Fact]
    public void GitHub_skips_drafts_and_maps_assets()
    {
        const string json = """
            [
              { "tag_name": "v2.0", "name": "Draft", "draft": true, "prerelease": false, "assets": [] },
              { "tag_name": "v1.1", "name": "One one", "draft": false, "prerelease": true,
                "published_at": "2026-01-02T03:04:05Z", "assets": [] },
              { "tag_name": "v1.0", "name": null, "draft": false, "prerelease": false,
                "published_at": "2026-01-01T00:00:00Z",
                "assets": [ { "name": "app_1.0_amd64.deb", "size": 1234,
                              "browser_download_url": "https://github.com/o/r/releases/download/v1.0/app_1.0_amd64.deb" } ] }
            ]
            """;
        var releases = GitHubClient.ParseReleases(json);

        Assert.Equal(["v1.1", "v1.0"], releases.Select(r => r.TagName));
        Assert.True(releases[0].IsPrerelease);
        var asset = Assert.Single(releases[1].Assets);
        Assert.Equal("app_1.0_amd64.deb", asset.Name);
        Assert.Equal(1234, asset.Size);
        Assert.EndsWith("/app_1.0_amd64.deb", asset.DownloadUrl);
    }

    [Fact]
    public void GitLab_uses_links_and_prefers_direct_asset_url()
    {
        const string json = """
            [
              { "tag_name": "v3.0", "name": "Soon", "upcoming_release": true, "assets": { "links": [] } },
              { "tag_name": "v2.0", "name": "Two", "upcoming_release": false, "released_at": "2026-02-01T00:00:00Z",
                "assets": { "count": 3, "sources": [ { "format": "zip", "url": "https://gitlab.com/src.zip" } ],
                            "links": [
                              { "name": "app.rpm", "url": "https://gitlab.com/o/r/-/package_files/1/download",
                                "direct_asset_url": "https://gitlab.com/o/r/-/releases/v2.0/downloads/app.rpm" },
                              { "name": "app.deb", "url": "https://example.org/app.deb" } ] } }
            ]
            """;
        var release = Assert.Single(GitLabClient.ParseReleases(json));

        Assert.Equal("v2.0", release.TagName);
        Assert.Equal("https://gitlab.com/o/r/-/releases/v2.0/downloads/app.rpm", release.Assets[0].DownloadUrl);
        Assert.Equal("https://example.org/app.deb", release.Assets[1].DownloadUrl);
    }

    [Fact]
    public void Forgejo_maps_assets()
    {
        const string json = """
            [ { "tag_name": "1.0", "name": "1.0", "draft": false, "prerelease": false,
                "assets": [ { "name": "app.x86_64.rpm", "size": 5,
                              "browser_download_url": "https://codeberg.org/o/r/releases/download/1.0/app.x86_64.rpm" } ] },
              { "tag_name": "0.9", "draft": false, "prerelease": false, "assets": null } ]
            """;
        var releases = ForgejoClient.ParseReleases(json);

        Assert.Equal("app.x86_64.rpm", Assert.Single(releases[0].Assets).Name);
        Assert.Empty(releases[1].Assets);
    }

    [Fact]
    public void Invalid_json_throws_forge_exception() =>
        Assert.Throws<ForgeException>(() => GitHubClient.ParseReleases("{ not json"));
}
