using System.Globalization;
using System.Text.RegularExpressions;

namespace Funkin.Util;

/// <summary>
/// Port of funkin.util.Version. Parses semver-like version strings such as "0.10.0" or "2.1"
/// and compares them the way the original game does (missing segments count as 0).
/// </summary>
public sealed class GameVersion : IComparable<GameVersion>, IEquatable<GameVersion>
{
    private static readonly Regex VersionRegex = new(@"^(\d+)(?:\.(\d+))?(?:\.(\d+))?(?:-([0-9A-Za-z.\-]+))?$", RegexOptions.Compiled);

    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }
    public string? PreRelease { get; }

    public static readonly GameVersion Zero = new(0, 0, 0, null);

    public GameVersion(int major, int minor = 0, int patch = 0, string? preRelease = null)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        PreRelease = preRelease;
    }

    public static GameVersion Parse(string version)
    {
        var match = VersionRegex.Match(version.Trim());
        if (!match.Success) throw new FormatException($"Invalid version string: {version}");

        int major = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        int minor = match.Groups[2].Success ? int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) : 0;
        int patch = match.Groups[3].Success ? int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture) : 0;
        string? pre = match.Groups[4].Success ? match.Groups[4].Value : null;
        return new GameVersion(major, minor, patch, pre);
    }

    public static bool TryParse(string version, out GameVersion result)
    {
        var match = VersionRegex.Match(version.Trim());
        if (!match.Success)
        {
            result = Zero;
            return false;
        }

        result = Parse(version);
        return true;
    }

    public int CompareTo(GameVersion? other)
    {
        if (other is null) return 1;
        int result = Major.CompareTo(other.Major);
        if (result != 0) return result;
        result = Minor.CompareTo(other.Minor);
        if (result != 0) return result;
        result = Patch.CompareTo(other.Patch);
        if (result != 0) return result;

        // A version without a pre-release tag is greater than one with it.
        if (PreRelease is null && other.PreRelease is null) return 0;
        if (PreRelease is null) return 1;
        if (other.PreRelease is null) return -1;
        return string.CompareOrdinal(PreRelease, other.PreRelease);
    }

    public bool Equals(GameVersion? other) => other is not null && CompareTo(other) == 0;

    public override bool Equals(object? obj) => obj is GameVersion v && Equals(v);

    public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch, PreRelease);

    public override string ToString() => PreRelease is null
        ? string.Create(CultureInfo.InvariantCulture, $"{Major}.{Minor}.{Patch}")
        : string.Create(CultureInfo.InvariantCulture, $"{Major}.{Minor}.{Patch}-{PreRelease}");

    public static bool operator >(GameVersion a, GameVersion b) => a.CompareTo(b) > 0;
    public static bool operator <(GameVersion a, GameVersion b) => a.CompareTo(b) < 0;
    public static bool operator >=(GameVersion a, GameVersion b) => a.CompareTo(b) >= 0;
    public static bool operator <=(GameVersion a, GameVersion b) => a.CompareTo(b) <= 0;
    public static bool operator ==(GameVersion? a, GameVersion? b) => a is null ? b is null : a.Equals(b);
    public static bool operator !=(GameVersion? a, GameVersion? b) => !(a == b);
}
