using Microsoft.Xna.Framework.Graphics;

namespace Funkin.Graphics;

/// <summary>
/// Loads and caches textures from disk. Equivalent to openfl's Assets cache + `Paths.getSparrowAtlas`.
/// Loaded textures are kept alive for the lifetime of the game since Flixel sprites reuse them heavily.
/// </summary>
public sealed class TextureCache
{
    private readonly Dictionary<string, Texture2D?> _cache = new(StringComparer.OrdinalIgnoreCase);
    private GraphicsDevice? _device;

    public void Initialize(GraphicsDevice device) => _device = device;

    /// <summary>Loads a PNG (or any MonoGame-supported image) from an absolute path.</summary>
    public Texture2D? Load(string? absolutePath)
    {
        if (string.IsNullOrEmpty(absolutePath)) return null;
        if (_cache.TryGetValue(absolutePath, out var cached)) return cached;
        if (_device is null) throw new InvalidOperationException("TextureCache has not been initialized.");

        Texture2D? texture = null;
        try
        {
            if (File.Exists(absolutePath))
            {
                using var stream = File.OpenRead(absolutePath);
                texture = Texture2D.FromStream(_device, stream);
                PremultiplyAlpha(texture);
            }
        }
        catch (Exception ex)
        {
            Util.Logger.Error($"Failed to load texture '{absolutePath}': {ex.Message}");
            texture = null;
        }

        _cache[absolutePath] = texture;
        return texture;
    }

    /// <summary>MonoGame's default blend state expects premultiplied alpha; PNGs are straight alpha.</summary>
    private static void PremultiplyAlpha(Texture2D texture)
    {
        var data = new Microsoft.Xna.Framework.Color[texture.Width * texture.Height];
        texture.GetData(data);

        bool premultiplied = true;
        for (int i = 0; i < data.Length; i++)
        {
            if (data[i].A == 0 || data[i].A == 255) continue;
            premultiplied = false;
            break;
        }

        if (premultiplied) return;

        for (int i = 0; i < data.Length; i++)
        {
            var c = data[i];
            data[i] = new Microsoft.Xna.Framework.Color(
                (byte)(c.R * c.A / 255),
                (byte)(c.G * c.A / 255),
                (byte)(c.B * c.A / 255),
                c.A);
        }

        texture.SetData(data);
    }

    /// <summary>Creates a 1x1 white texture, used for rectangles and the health bar.</summary>
    public Texture2D WhitePixel
    {
        get
        {
            if (_white is not null) return _white;
            if (_device is null) throw new InvalidOperationException("TextureCache has not been initialized.");

            _white = new Texture2D(_device, 1, 1);
            _white.SetData(new[] { Microsoft.Xna.Framework.Color.White });
            return _white;
        }
    }

    private Texture2D? _white;

    /// <summary>Renders a solid colour texture of the given size (used by colour stage props).</summary>
    public Texture2D CreateSolid(int width, int height, Microsoft.Xna.Framework.Color color)
    {
        if (_device is null) throw new InvalidOperationException("TextureCache has not been initialized.");

        var texture = new Texture2D(_device, Math.Max(1, width), Math.Max(1, height));
        var data = new Microsoft.Xna.Framework.Color[texture.Width * texture.Height];
        Array.Fill(data, color);
        texture.SetData(data);
        return texture;
    }
}
