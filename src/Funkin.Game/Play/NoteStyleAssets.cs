using Funkin.Flixel;
using Funkin.Graphics;
using Funkin.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Funkin.Play;

/// <summary>
/// Resolves and caches the arrow artwork for a note style.
/// Mirrors `funkin.play.notes.notestyle.NoteStyle`, but degrades to coloured rectangles when the
/// spritesheet is missing so the port can run without the art assets.
/// </summary>
public sealed class NoteStyleAssets
{
    public static NoteStyleAssets Instance { get; } = new();

    private readonly Dictionary<string, NoteStyleAssetsSet> _cache = new(StringComparer.OrdinalIgnoreCase);

    private static readonly string[] Lanes = { "left", "down", "up", "right" };

    public NoteStyleAssetsSet Get(string styleId)
    {
        if (_cache.TryGetValue(styleId, out var cached)) return cached;

        var set = new NoteStyleAssetsSet(styleId);
        set.Load();
        _cache[styleId] = set;
        return set;
    }

    /// <summary>Arrow artwork: one frame per press state, per lane.</summary>
    public sealed class NoteStyleAssetsSet
    {
        public string StyleId { get; }

        /// <summary>[lane, state] where state 0 = static, 1 = pressed, 2 = confirm.</summary>
        public FrameRegion?[,] Arrows { get; } = new FrameRegion?[4, 3];

        public FrameRegion?[,] Sustains { get; } = new FrameRegion?[4, 2];

        public bool HasArtwork { get; private set; }

        public NoteStyleAssetsSet(string styleId) => StyleId = styleId;

        public void Load()
        {
            // The base game ships `noteSkins/NOTE_assets` and newer styles under `noteSkins/<id>`.
            string[] candidates =
            {
                $"noteSkins/{StyleId}/NOTE_assets",
                $"noteSkins/{StyleId}/arrow",
                "noteSkins/NOTE_assets",
                "NOTE_assets",
            };

            foreach (var candidate in candidates)
            {
                if (TryLoad(candidate)) return;
            }

            Logger.Warn($"No artwork found for note style '{StyleId}'; using placeholders.");
        }

        private bool TryLoad(string path)
        {
            var png = Paths.FunkinPaths.Instance.Image(path);
            var xml = Paths.FunkinPaths.Instance.Xml(path);
            var atlas = FlxAtlasFrames.Load(png, xml, FlxG.Textures);
            if (atlas is null) return false;

            bool any = false;
            for (int lane = 0; lane < 4; lane++)
            {
                string laneName = Lanes[lane];

                foreach (var (suffix, index) in new[] { (" static", 0), (" press", 1), (" confirm", 2) })
                {
                    var frames = atlas.GetFramesByPrefix($"{laneName}{suffix}");
                    if (frames.Count == 0) continue;
                    Arrows[lane, index] = frames[0];
                    any = true;
                }

                foreach (var (suffix, index) in new[] { (" hold piece", 0), (" hold end", 1) })
                {
                    var frames = atlas.GetFramesByPrefix($"{laneName}{suffix}");
                    if (frames.Count == 0) continue;
                    Sustains[lane, index] = frames[0];
                }
            }

            HasArtwork = any;
            return any;
        }

        public static Texture2D LaneTexture(int lane) => FlxG.Textures.CreateSolid(1, 1, LaneColor(lane));

        public static Color LaneColor(int lane) => lane switch
        {
            0 => new Color(194, 75, 153),
            1 => new Color(0, 255, 255),
            2 => new Color(18, 250, 5),
            _ => new Color(249, 57, 63),
        };
    }
}
