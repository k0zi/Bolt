namespace Bolt.Core.Platform;

/// <summary>
/// Compares release tags such as <c>v1.2.10</c>, <c>1.2.3-beta.1</c> or <c>app-2.0</c>: the prefix before the first
/// digit is ignored, numeric parts compare as numbers and a pre-release suffix (<c>-…</c>) sorts before the release.
/// </summary>
public sealed class VersionComparer : IComparer<string?>
{
    public static VersionComparer Instance { get; } = new();

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y))
            return 0;
        if (x is null)
            return -1;
        if (y is null)
            return 1;

        var (xCore, xPre) = Split(x);
        var (yCore, yPre) = Split(y);

        var result = CompareCore(xCore, yCore);
        if (result != 0)
            return result;
        if (xPre is null || yPre is null)
            return xPre is null ? (yPre is null ? 0 : 1) : -1;
        return CompareParts(xPre.Split('.'), yPre.Split('.'));
    }

    /// <summary><c>true</c> when <paramref name="candidate"/> is a newer version than <paramref name="current"/>.</summary>
    public static bool IsNewer(string? candidate, string? current) =>
        candidate is not null && Instance.Compare(candidate, current) > 0;

    /// <summary>Strips the prefix ("v", "release-") and returns the version without build metadata.</summary>
    public static string Normalize(string tag)
    {
        var (core, pre) = Split(tag);
        return pre is null ? core : $"{core}-{pre}";
    }

    private static (string Core, string? Pre) Split(string version)
    {
        var start = 0;
        while (start < version.Length && !char.IsAsciiDigit(version[start]))
            start++;
        var text = start == version.Length ? version : version[start..];

        var plus = text.IndexOf('+');
        if (plus >= 0)
            text = text[..plus];
        var dash = text.IndexOf('-');
        return dash < 0 ? (text, null) : (text[..dash], text[(dash + 1)..]);
    }

    private static int CompareCore(string x, string y)
    {
        var xs = x.Split('.');
        var ys = y.Split('.');
        for (var i = 0; i < Math.Max(xs.Length, ys.Length); i++)
        {
            var a = i < xs.Length ? xs[i] : "0";
            var b = i < ys.Length ? ys[i] : "0";
            var result = ComparePart(a, b);
            if (result != 0)
                return result;
        }
        return 0;
    }

    private static int CompareParts(string[] xs, string[] ys)
    {
        for (var i = 0; i < Math.Min(xs.Length, ys.Length); i++)
        {
            var result = ComparePart(xs[i], ys[i]);
            if (result != 0)
                return result;
        }
        return xs.Length.CompareTo(ys.Length);
    }

    private static int ComparePart(string a, string b)
    {
        var aNumeric = ulong.TryParse(a, out var an);
        var bNumeric = ulong.TryParse(b, out var bn);
        if (aNumeric && bNumeric)
            return an.CompareTo(bn);
        if (aNumeric != bNumeric)
            return aNumeric ? -1 : 1; // semver: numeric identifiers sort before alphanumeric ones
        return string.CompareOrdinal(a, b);
    }
}
