using Funkin.Audio;
using Funkin.Game.States;
using Funkin.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Funkin.Flixel;

/// <summary>
/// Port of flixel.FlxG: the global game state. Holds the graphics device, the current state,
/// the camera, input, sound and timing data.
/// </summary>
public static class FlxG
{
    public static GraphicsDevice GraphicsDevice { get; private set; } = null!;
    public static ContentManager Content { get; private set; } = null!;
    public static SpriteBatch SpriteBatch { get; private set; } = null!;
    public static Microsoft.Xna.Framework.Game Game { get; private set; } = null!;

    public static TextureCache Textures { get; } = new();
    public static FontManager Fonts { get; } = new();
    public static InputHandler Input { get; } = new();
    public static SoundManager Sound { get; } = new();

    public static FlxState? State { get; private set; }

    public static Color BackgroundColor { get; set; } = Color.Black;

    /// <summary>Frame delta in seconds.</summary>
    public static float Elapsed { get; private set; }

    /// <summary>Total elapsed time since startup, in seconds.</summary>
    public static double Time { get; private set; }

    public static int Ticks { get; private set; }

    public static int Width => GraphicsDevice.PresentationParameters.BackBufferWidth;
    public static int Height => GraphicsDevice.PresentationParameters.BackBufferHeight;

    public static FlxCamera Camera => State?.Camera ?? FallbackCamera;

    private static readonly FlxCamera FallbackCamera = new(FunkinGame.GameWidth, FunkinGame.GameHeight);

    public static readonly Random Random = new();

    private static Microsoft.Xna.Framework.GraphicsDeviceManager? _graphics;

    private static bool _initialized;

    public static void Initialize(Microsoft.Xna.Framework.Game game, Microsoft.Xna.Framework.GraphicsDeviceManager graphics)
    {
        Game = game;
        _graphics = graphics;
    }

    public static void LoadContent(GraphicsDevice device, ContentManager content)
    {
        GraphicsDevice = device;
        Content = content;

        SpriteBatch = new SpriteBatch(device);
        Textures.Initialize(device);
        Fonts.Initialize();
        Sound.Initialize();

        game_config_apply();
        _initialized = true;
    }

    private static void game_config_apply()
    {
        Game.Window.ClientSizeChanged += (_, _) => State?.OnResize(Width, Height);
    }

    public static bool IsInitialized => _initialized;

    /// <summary>Switches the active state, destroying the previous one.</summary>
    public static void SwitchState(FlxState? newState)
    {
        var old = State;
        State = newState;

        old?.Destroy();

        if (newState is not null)
        {
            newState.Camera.Width = Width;
            newState.Camera.Height = Height;
            BackgroundColor = Color.Black;
            newState.Create();
        }
    }

    /// <summary>Opens a substate on top of the current state (pause menu, game over screen, ...).</summary>
    public static void OpenSubState(FlxState substate)
    {
        State?.AddSubState(substate);
    }

    public static void CloseSubState(FlxState substate)
    {
        State?.RemoveSubState(substate);
    }

    public static void ResetState()
    {
        if (State is null) return;
        var type = State.GetType();
        SwitchState((FlxState?)Activator.CreateInstance(type));
    }

    public static void Update(GameTime gameTime)
    {
        Elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Time += Elapsed;
        Ticks++;

        Input.Update();
        Sound.Update();

        if (Input.JustPressed(Funkin.Input.GameAction.Fullscreen))
        {
            _graphics?.ToggleFullScreen();
        }

        State?.Update(Elapsed);
    }

    public static void Draw(GameTime gameTime)
    {
        if (State is null) return;

        SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearClamp);
        State.Draw(SpriteBatch);
        State.DrawSubStates(SpriteBatch);
        SpriteBatch.End();
    }

    public static void OnExit()
    {
        Save.SaveManager.Instance.Flush(true);
        State?.Destroy();
    }
}
