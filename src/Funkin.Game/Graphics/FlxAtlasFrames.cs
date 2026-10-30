using System.Globalization;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Funkin.Graphics;

/// <summary>A single frame inside a texture atlas.</summary>
public readonly struct FrameRegion
{
    public required Texture2D Texture { get; init; }
    public required Rectangle Source { get; init; }

    /// <summary>Offset applied when drawing, from the Sparrow `frameX`/`frameY` attributes.</summary>
    public required Point Offset { get; init; }

    public required string Name { get; init; }
}

/// <summary>A named animation built from a set of frames.</summary>
public sealed class FrameCollection
{
    public string Name { get; }
    public List<FrameRegion> Frames { get; } = new();

    public FrameCollection(string name) => Name = name;
}

/// <summary>
/// Port of FlxAtlasFrames: parses a Sparrow (Starling) XML atlas next to a PNG and groups
/// its frames into animations by their name prefix.
/// </summary>
public sealed class FlxAtlasFrames
{
    public Texture2D Texture { get; }
    public List<FrameRegion> Frames { get; } = new();

    private FlxAtlasFrames(Texture2D texture) => Texture = texture;

    public static FlxAtlasFrames? Load(string? pngPath, string? xmlPath, TextureCache cache)
    {
        var texture = cache.Load(pngPath);
        if (texture is null || xmlPath is null || !File.Exists(xmlPath)) return null;

        try
        {
            var doc = XDocument.Load(xmlPath);
            var atlas = new FlxAtlasFrames(texture);

            var root = doc.Root;
            if (root is null) return atlas;

            // Sparrow atlases wrap frames in <SubTexture>, TexturePacker uses <sprite>.
            foreach (var element in root.Descendants())
            {
                string tag = element.Name.LocalName.ToLowerInvariant();
                if (tag != "subtexture" && tag != "sprite") continue;

                string name = element.Attribute("name")?.Value ?? string.Empty;
                if (name.Length == 0) continue;

                int x = ReadInt(element, "x");
                int y = ReadInt(element, "y");
                int width = ReadInt(element, "width");
                int height = ReadInt(element, "height");

                // Trimmed frames store their original size in frameWidth/frameHeight.
                int offsetX = -ReadInt(element, "frameX");
                int offsetY = -ReadInt(element, "frameY");

                width = Math.Clamp(width, 1, texture.Width - Math.Max(0, x));
                height = Math.Clamp(height, 1, texture.Height - Math.Max(0, y));

                atlas.Frames.Add(new FrameRegion
                {
                    Texture = texture,
                    Source = new Rectangle(x, y, width, height),
                    Offset = new Point(offsetX, offsetY),
                    Name = name,
                });
            }

            return atlas;
        }
        catch (Exception ex)
        {
            Util.Logger.Error($"Failed to parse atlas XML '{xmlPath}': {ex.Message}");
            return null;
        }
    }

    private static int ReadInt(XElement element, string attribute)
    {
        var value = element.Attribute(attribute)?.Value;
        if (value is null) return 0;
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
    }

    /// <summary>Returns the frames whose name starts with the given prefix, in atlas order.</summary>
    public List<FrameRegion> GetFramesByPrefix(string prefix)
    {
        return Frames.Where(f => f.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public List<FrameRegion> GetFramesByName(IEnumerable<string> names)
    {
        var wanted = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
        return Frames.Where(f => wanted.Contains(f.Name)).ToList();
    }

    /// <summary>The atlas `frames.txt`-style single-frame fallback when no XML is present.</summary>
    public static FlxAtlasFrames FromWholeTexture(Texture2D texture)
    {
        var atlas = new FlxAtlasFrames(texture);
        atlas.Frames.Add(new FrameRegion
        {
            Texture = texture,
            Source = new Rectangle(0, 0, texture.Width, texture.Height),
            Offset = Point.Zero,
            Name = string.Empty,
        });
        return atlas;
    }
}
