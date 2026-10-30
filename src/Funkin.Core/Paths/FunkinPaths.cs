using Funkin.Modding;
using Funkin.Util;

namespace Funkin.Paths;

/// <summary>
/// Port of funkin.Paths. Provides asset lookups relative to the game's `assets/` folder, with mods
/// (loaded through <see cref="Mods"/>) taking priority over the base game files.
/// </summary>
public sealed class FunkinPaths
{
    public static FunkinPaths Instance { get; private set; } = new(Directory.GetCurrentDirectory());

    /// <summary>Root of the game install (the folder holding `assets/` and `mods/`).</summary>
    public string GameRoot { get; }

    public string AssetsRoot => Path.Combine(GameRoot, "assets");
    public string ModsRoot => Path.Combine(GameRoot, "mods");

    /// <summary>Where save data is stored (mirrors the game's `saves/` concept, per-user in the original).</summary>
    public string SaveRoot { get; set; }

    public bool Verbose { get; set; }

    public FunkinPaths(string gameRoot)
    {
        GameRoot = Path.GetFullPath(gameRoot);
        SaveRoot = Path.Combine(GameRoot, "saves");
    }

    public static void Initialize(string gameRoot)
    {
        Instance = new FunkinPaths(gameRoot);
    }

    // ---------------------------------------------------------------------
    // Resolution
    // ---------------------------------------------------------------------

    /// <summary>
    /// Additional roots searched after `assets/`. This makes a raw checkout of the asset repository
    /// usable as-is: `preload/` holds the default library and the other folders are libraries.
    /// </summary>
    private List<string> FallbackRoots()
    {
        var roots = new List<string>();

        string preload = Path.Combine(AssetsRoot, "preload");
        if (Directory.Exists(preload)) roots.Add(preload);

        // A clone of Funkin.assets placed directly in the game root.
        string rootPreload = Path.Combine(GameRoot, "preload");
        if (Directory.Exists(rootPreload))
        {
            roots.Add(rootPreload);
            roots.Add(GameRoot);
        }

        return roots;
    }

    /// <summary>Resolves an asset path (relative to `assets/`) to an absolute path, or null when missing.</summary>
    public string? Resolve(string relativePath)
    {
        relativePath = Normalise(relativePath);

        var modPath = Mods.Instance.FindModAsset(relativePath);
        if (modPath is not null) return modPath;

        var basePath = Path.Combine(AssetsRoot, relativePath);
        if (System.IO.File.Exists(basePath)) return basePath;

        foreach (var fallback in FallbackRoots())
        {
            var candidate = Path.Combine(fallback, relativePath);
            if (System.IO.File.Exists(candidate)) return candidate;
        }

        if (Verbose) Logger.Debug($"Asset not found: {relativePath}");
        return null;
    }

    /// <summary>Resolves a data file, taking mods into account, then returns its contents.</summary>
    public string? ReadText(string relativePath)
    {
        var path = Resolve(relativePath);
        if (path is null) return null;
        try
        {
            return System.IO.File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to read {relativePath}: {ex.Message}");
            return null;
        }
    }

    /// <summary>True when an asset exists in the base game or any mod.</summary>
    public bool Exists(string relativePath) => Resolve(relativePath) is not null;

    /// <summary>Resolves a path, throwing when missing. Used for hard requirements.</summary>
    public string Require(string relativePath)
    {
        return Resolve(relativePath) ?? throw new FileNotFoundException($"Missing required asset: {relativePath}");
    }

    private static string Normalise(string path) => path.Replace('\\', '/').TrimStart('/');

    // ---------------------------------------------------------------------
    // Typed helpers (mirroring Paths.hx)
    // ---------------------------------------------------------------------

    public string? File(string path) => Resolve(path);

    public string? Image(string path) => Resolve($"images/{path}.png");

    public string? Xml(string path) => Resolve($"images/{path}.xml");

    public string? Json(string path) => Resolve($"{path}.json");

    public string? Text(string path) => Resolve($"{path}.txt");

    public string? Sound(string path) => Resolve($"sounds/{path}.ogg");

    public string? Music(string path) => Resolve($"music/{path}.ogg");

    public string? Font(string path) => Resolve($"fonts/{path}");

    public string? Video(string path) => Resolve($"videos/{path}.mp4");

    public string? Data(string path) => Resolve($"data/{path}");

    /// <summary>`Paths.inst` - the instrumental for a song.</summary>
    public string? Instrumental(string songId, string? suffix = null)
    {
        string name = string.IsNullOrEmpty(suffix) ? "Inst" : $"Inst-{suffix}";
        return Resolve($"songs/{songId}/{name}.ogg");
    }

    /// <summary>`Paths.voices` - the vocal track(s) for a song.</summary>
    public string? Voices(string songId, string suffix = "")
    {
        string name = string.IsNullOrEmpty(suffix) ? "Voices" : $"Voices-{suffix}";
        return Resolve($"songs/{songId}/{name}.ogg");
    }

    public string? VoicesForCharacter(string songId, string character)
    {
        var specific = Resolve($"songs/{songId}/Voices-{character}.ogg");
        return specific ?? Resolve($"songs/{songId}/Voices.ogg");
    }

    // ---------------------------------------------------------------------
    // Data file enumerations (used by the registries)
    // ---------------------------------------------------------------------

    /// <summary>
    /// Lists the ids of data entries in a folder, based on a filename suffix.
    /// e.g. ListDataFiles("songs/", "-metadata.json") returns every song id.
    /// </summary>
    public List<string> ListDataFiles(string folder, string suffix)
    {
        var results = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        string relative = Normalise(folder).TrimEnd('/');

        // Base game first, then mods (mods override, but the id list is a union).
        foreach (var dir in new[] { Path.Combine(AssetsRoot, relative) }.Concat(Mods.Instance.EnumerateModDirectories(relative)))
        {
            if (!Directory.Exists(dir)) continue;

            foreach (var sub in Directory.GetDirectories(dir))
            {
                string id = Path.GetFileName(sub);
                if (System.IO.File.Exists(Path.Combine(sub, id + suffix))) results.Add(id);
            }

            // Flat layout is also supported (e.g. data/characters/bf.json).
            foreach (var file in Directory.GetFiles(dir, "*" + suffix))
            {
                string name = Path.GetFileName(file);
                if (name.Length > suffix.Length) results.Add(name[..^suffix.Length]);
            }
        }

        return results.ToList();
    }
}
