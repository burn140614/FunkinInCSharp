namespace Funkin.Assets;

/// <summary>
/// Knows how the Friday Night Funkin' asset repository maps onto the runtime `assets/` tree.
///
/// The repository is organised into asset *libraries*:
/// <code>
/// preload/{data,images,music,sounds,scripts,shaders}  ->  assets/...
/// shared/{images,music,sounds}                        ->  assets/shared/...
/// songs/, fonts/, videos/, week1../weekend1/          ->  assets/&lt;same&gt;/...
/// </code>
/// This is the same layout project.hxp produces with its `addAssetPath`/`addAssetLibrary` calls.
/// </summary>
public static class AssetLayout
{
    /// <summary>Top level folders of the asset repository that become libraries under `assets/`.</summary>
    public static readonly string[] LibraryDirectories =
    {
        "shared", "songs", "fonts", "videos", "images", "music", "sounds", "data", "scripts", "shaders",
        "week1", "week2", "week3", "week4", "week5", "week6", "week7",
        "weekend1", "tutorial", "sserafim", "exclude",
    };

    /// <summary>Repository entries which are not game assets.</summary>
    private static readonly string[] IgnoredEntries =
    {
        ".github", ".gitattributes", ".gitignore", "readme.md", "license.md",
    };

    /// <summary>
    /// Maps an entry path from the assets archive onto a path relative to the runtime `assets/` folder.
    /// Returns null when the entry should be skipped (directories, dotfiles, readme/license).
    /// </summary>
    /// <param name="entryPath">Archive entry name, e.g. `Funkin.assets-main/preload/images/NOTE_assets.png`.</param>
    public static string? MapArchiveEntry(string entryPath)
    {
        if (string.IsNullOrWhiteSpace(entryPath)) return null;

        string path = entryPath.Replace('\\', '/').Trim();
        if (path.EndsWith('/')) return null; // directory entry

        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return null;

        // GitHub zipballs wrap everything in a single top level folder; drop it when present.
        int start = 0;
        if (parts.Length > 1 && !IsLibraryDirectory(parts[0]) && parts[0].Contains('.', StringComparison.Ordinal))
        {
            start = 1;
        }
        else if (parts.Length > 1 && !IsLibraryDirectory(parts[0]) && parts[0].EndsWith("-main", StringComparison.OrdinalIgnoreCase))
        {
            start = 1;
        }

        var relative = parts[start..];
        if (relative.Length == 0) return null;

        string first = relative[0];
        if (IgnoredEntries.Contains(first.ToLowerInvariant())) return null;

        // `preload/` holds the default asset library, which is flattened into `assets/` itself.
        if (string.Equals(first, "preload", StringComparison.OrdinalIgnoreCase))
        {
            if (relative.Length < 2) return null;
            return string.Join('/', relative[1..]);
        }

        return string.Join('/', relative);
    }

    public static bool IsLibraryDirectory(string name)
        => LibraryDirectories.Contains(name, StringComparer.OrdinalIgnoreCase);

    /// <summary>True when the folder contains an assembled `assets/` tree (or an unpacked repository).</summary>
    public static bool IsAssetsPresent(string gameRoot)
    {
        string assets = Path.Combine(gameRoot, "assets");

        return HasRuntimeData(assets) || HasRuntimeData(Path.Combine(assets, "preload"));
    }

    /// <summary>True when this looks like a raw clone of the asset repository rather than an assembled tree.</summary>
    public static bool IsUnassembledCheckout(string gameRoot)
    {
        string assets = Path.Combine(gameRoot, "assets");
        if (Directory.Exists(Path.Combine(assets, "preload"))) return true;

        // Some people clone the repository straight into the game root.
        return Directory.Exists(Path.Combine(gameRoot, "preload")) && Directory.Exists(Path.Combine(gameRoot, "shared"));
    }

    private static bool HasRuntimeData(string assetsRoot)
    {
        if (!Directory.Exists(assetsRoot)) return false;

        // Either real chart data or the shared note graphics must be there for the game to be usable.
        if (Directory.Exists(Path.Combine(assetsRoot, "data", "songs"))) return true;
        if (File.Exists(Path.Combine(assetsRoot, "images", "NOTE_assets.png"))) return true;

        return false;
    }

    /// <summary>Rough on-disk size of the download destination, if it exists.</summary>
    public static long GetAvailableDiskSpace(string path)
    {
        try
        {
            var full = Path.GetFullPath(path);
            var root = Path.GetPathRoot(full);
            if (string.IsNullOrEmpty(root)) return long.MaxValue;
            return new DriveInfo(root).AvailableFreeSpace;
        }
        catch
        {
            return long.MaxValue;
        }
    }
}
