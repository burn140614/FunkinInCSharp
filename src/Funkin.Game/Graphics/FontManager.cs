using FontStashSharp;
using Funkin.Util;

namespace Funkin.Graphics;

/// <summary>
/// Owns the FontStashSharp font system used for all in-game text.
/// Tries the game's bundled fonts first, then any system TrueType font, so text still renders
/// when the assets submodule has not been checked out.
/// </summary>
public sealed class FontManager
{
    private FontSystem? _fontSystem;
    private readonly Dictionary<int, SpriteFontBase> _cache = new();
    private readonly List<string> _loadedFontNames = new();

    public bool IsReady => _fontSystem is not null;

    public string PrimaryFontName { get; private set; } = "default";

    public void Initialize()
    {
        _fontSystem = new FontSystem();
        _cache.Clear();
        _loadedFontNames.Clear();

        // Preferred: the game's own fonts, mirroring what `Paths.font()` would resolve.
        var bundled = FindBundledFonts();
        foreach (var path in bundled)
        {
            TryAddFont(path);
        }

        // Fallback: system fonts, so the port is usable without the assets checkout.
        if (_loadedFontNames.Count == 0)
        {
            foreach (var path in FindSystemFonts())
            {
                if (TryAddFont(path)) break;
            }
        }

        if (_loadedFontNames.Count == 0)
        {
            Logger.Warn("No usable TrueType font found; text rendering will be disabled.");
            _fontSystem = null;
            return;
        }

        PrimaryFontName = _loadedFontNames[0];
        Logger.Info($"Loaded fonts: {string.Join(", ", _loadedFontNames)}");
    }

    private static IEnumerable<string> FindBundledFonts()
    {
        var assetsRoot = Paths.FunkinPaths.Instance.AssetsRoot;
        string fontsDir = Path.Combine(assetsRoot, "fonts");
        if (!Directory.Exists(fontsDir)) yield break;

        foreach (var file in Directory.GetFiles(fontsDir, "*.ttf"))
        {
            yield return file;
        }
    }

    private static IEnumerable<string> FindSystemFonts()
    {
        string[] candidates =
        {
            "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
            "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
            "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
            "/usr/share/fonts/TTF/DejaVuSans.ttf",
            "/System/Library/Fonts/Supplemental/Arial.ttf",
            "C:\\Windows\\Fonts\\arial.ttf",
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate)) yield return candidate;
        }

        foreach (var root in new[] { "/usr/share/fonts/truetype", "/usr/share/fonts" })
        {
            if (!Directory.Exists(root)) continue;
            foreach (var file in Directory.GetFiles(root, "*.ttf", SearchOption.AllDirectories).Take(5))
            {
                yield return file;
            }
        }
    }

    private bool TryAddFont(string path)
    {
        try
        {
            if (!File.Exists(path) || _fontSystem is null) return false;
            _fontSystem.AddFont(File.ReadAllBytes(path));
            _loadedFontNames.Add(Path.GetFileNameWithoutExtension(path));
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to load font '{path}': {ex.Message}");
            return false;
        }
    }

    /// <summary>Returns (and caches) a font at the given pixel size.</summary>
    public SpriteFontBase? GetFont(int size)
    {
        if (_fontSystem is null) return null;
        if (_cache.TryGetValue(size, out var font)) return font;

        font = _fontSystem.GetFont(size);
        _cache[size] = font;
        return font;
    }
}
