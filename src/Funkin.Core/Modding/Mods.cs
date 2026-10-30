using System.Text.RegularExpressions;
using Funkin.Util;

namespace Funkin.Modding;

/// <summary>
/// Port of Polymod's mod discovery / loading order.
/// Mods are loaded from the `mods/` folder; a mod folder may be a raw folder, or a zip which is extracted to a cache folder on first run.
/// Asset lookups check mods in reverse load order before falling back to the base game's assets.
/// </summary>
public sealed class Mods
{
    private readonly List<ModMetadata> _loaded = new();

    public IReadOnlyList<ModMetadata> Loaded => _loaded;

    public static Mods Instance { get; } = new();

    /// <summary>Mods which are known but disabled, mapped by folder name.</summary>
    public HashSet<string> DisabledMods { get; } = new(StringComparer.OrdinalIgnoreCase);

    public string ModsDirectory { get; private set; } = string.Empty;

    public bool IsModLoaded(string id) => _loaded.Any(m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase));

    /// <summary>Scans the mods folder and returns the metadata found, sorted by folder name (Polymod's load order).</summary>
    public void ScanMods(string modsDirectory, IEnumerable<string>? enabledOnly = null)
    {
        ModsDirectory = modsDirectory;
        _loaded.Clear();

        if (!Directory.Exists(modsDirectory)) return;

        var enabled = enabledOnly?.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var folder in Directory.GetDirectories(modsDirectory).OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
        {
            string name = Path.GetFileName(folder);
            if (name.StartsWith('.')) continue;
            if (enabled is not null && !enabled.Contains(name)) continue;
            if (DisabledMods.Contains(name)) continue;

            var metadata = ReadMetadata(folder);
            if (metadata is null) continue;

            _loaded.Add(metadata);
            Logger.Info($"Loaded mod '{metadata.Title}' ({metadata.Id}) v{metadata.ModVersion}");
        }

        // Zipped mods are supported by extracting them into mods/.cache/<name>/.
        foreach (var archive in Directory.GetFiles(modsDirectory, "*.zip").OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
        {
            string name = Path.GetFileNameWithoutExtension(archive);
            if (enabled is not null && !enabled.Contains(name)) continue;
            if (DisabledMods.Contains(name)) continue;

            string cacheDir = Path.Combine(modsDirectory, ".cache", name);
            try
            {
                if (Directory.Exists(cacheDir)) Directory.Delete(cacheDir, true);
                Directory.CreateDirectory(cacheDir);
                System.IO.Compression.ZipFile.ExtractToDirectory(archive, cacheDir, true);

                // If the zip has a single top level folder, treat that as the mod root.
                var entries = Directory.GetDirectories(cacheDir);
                if (entries.Length == 1 && !Directory.Exists(Path.Combine(cacheDir, "assets")))
                {
                    cacheDir = entries[0];
                }

                var metadata = ReadMetadata(cacheDir, name);
                if (metadata is not null) _loaded.Add(metadata);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to load zipped mod {name}: {ex.Message}");
            }
        }
    }

    private static ModMetadata? ReadMetadata(string folder, string? forcedId = null)
    {
        string metaPath = Path.Combine(folder, "_polymod_meta.json");
        if (!File.Exists(metaPath)) return null;

        try
        {
            var metadata = Json.Deserialize<ModMetadata>(File.ReadAllText(metaPath));
            if (metadata is null) return null;
            metadata.Id = forcedId ?? SanitiseId(Path.GetFileName(folder));
            metadata.FolderPath = folder;
            return metadata;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to read mod metadata at {metaPath}: {ex.Message}");
            return null;
        }
    }

    private static string SanitiseId(string name) => Regex.Replace(name, "[^A-Za-z0-9_-]", "-");

    /// <summary>
    /// Returns the first existing path for an asset relative to a mod's `assets/` folder, checking the
    /// most recently loaded mod first (matching Polymod's override behaviour).
    /// </summary>
    public string? FindModAsset(string relativePath)
    {
        for (int i = _loaded.Count - 1; i >= 0; i--)
        {
            var candidate = Path.Combine(_loaded[i].AssetsPath, relativePath);
            if (File.Exists(candidate)) return candidate;
        }

        return null;
    }

    /// <summary>Enumerates every mod's copy of a directory, most recent first.</summary>
    public IEnumerable<string> EnumerateModDirectories(string relativePath)
    {
        for (int i = _loaded.Count - 1; i >= 0; i--)
        {
            var candidate = Path.Combine(_loaded[i].AssetsPath, relativePath);
            if (Directory.Exists(candidate)) yield return candidate;
        }
    }
}
