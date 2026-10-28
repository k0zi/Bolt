using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Bolt.Core.Forges;
using Bolt.Core.Model;

namespace Bolt.Core.Platform;

/// <summary>Picks the release asset that the host OS installer can install.</summary>
public static partial class AssetMatcher
{
    private static readonly string[] Ignored =
        [".src.rpm", "-dbgsym", "-debuginfo", "-debugsource", "debug_"];

    public static PackageFormat? FormatOf(string fileName)
    {
        var name = fileName.ToLowerInvariant();
        if (Ignored.Any(name.Contains))
            return null;
        return Path.GetExtension(name) switch
        {
            ".deb" => PackageFormat.Deb,
            ".rpm" => PackageFormat.Rpm,
            ".msi" => PackageFormat.Msi,
            ".exe" => PackageFormat.Exe,
            _ => null,
        };
    }

    /// <summary>The best asset for <paramref name="platform"/>, or <c>null</c> if none fits.</summary>
    public static ReleaseAsset? SelectAsset(IEnumerable<ReleaseAsset> assets, PlatformInfo platform)
    {
        ReleaseAsset? best = null;
        var bestScore = int.MinValue;
        foreach (var asset in assets)
        {
            var score = Score(asset.Name, platform);
            if (score is null)
                continue;
            if (score > bestScore || (score == bestScore && asset.Name.Length < best!.Name.Length))
            {
                best = asset;
                bestScore = score.Value;
            }
        }
        return best;
    }

    /// <summary>Picks the newest release (prereleases skipped) that has a matching asset; releases come newest first.</summary>
    public static (Release Release, ReleaseAsset Asset)? SelectLatest(IEnumerable<Release> releases,
                                                                      PlatformInfo platform)
    {
        foreach (var release in releases.Where(r => !r.IsPrerelease))
        {
            if (SelectAsset(release.Assets, platform) is { } asset)
                return (release, asset);
        }
        return null;
    }

    internal static int? Score(string fileName, PlatformInfo platform)
    {
        if (FormatOf(fileName) is not { } format)
            return null;
        var formatIndex = IndexOf(platform.Formats, format);
        if (formatIndex < 0)
            return null;

        var name = fileName.ToLowerInvariant();
        var mentioned = Architectures.Where(a => a.Pattern.IsMatch(name)).Select(a => a.Arch).ToList();
        int archScore;
        if (mentioned.Count == 0)
            archScore = 1; // noarch / all / no hint
        else if (mentioned.Contains(platform.Architecture))
            archScore = 2;
        else
            return null;

        var score = (platform.Formats.Count - formatIndex) * 10 + archScore;
        if (name.Contains("musl"))
            score -= 1;
        return score;
    }

    private static int IndexOf(IReadOnlyList<PackageFormat> formats, PackageFormat format)
    {
        for (var i = 0; i < formats.Count; i++)
        {
            if (formats[i] == format)
                return i;
        }
        return -1;
    }

    private static readonly (Architecture Arch, Regex Pattern)[] Architectures =
    [
        (Architecture.X64, X64Regex()),
        (Architecture.Arm64, Arm64Regex()),
        (Architecture.X86, X86Regex()),
        (Architecture.Arm, ArmRegex()),
    ];

    [GeneratedRegex(@"(?<![a-z0-9])(amd64|x86[_-]64|x64|win64)(?![a-z0-9])")]
    private static partial Regex X64Regex();

    [GeneratedRegex(@"(?<![a-z0-9])(arm64|aarch64)(?![a-z0-9])")]
    private static partial Regex Arm64Regex();

    [GeneratedRegex(@"(?<![a-z0-9])(i[3-6]86|x86(?![_-]?64)|win32|ia32)(?![a-z0-9])")]
    private static partial Regex X86Regex();

    [GeneratedRegex(@"(?<![a-z0-9])(armhf|armel|armv[67][a-z0-9]*|arm32)(?![a-z0-9])")]
    private static partial Regex ArmRegex();
}
