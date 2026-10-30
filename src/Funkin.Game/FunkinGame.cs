using Funkin.Flixel;
using Funkin.Game.States;
using Funkin.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Funkin;

/// <summary>
/// The MonoGame entry point. Equivalent to the Haxe original's `Main`/`InitState` bootstrap:
/// it sets up the graphics device, the FlxG globals, then hands control to the state machine.
/// </summary>
public sealed class FunkinGame : Microsoft.Xna.Framework.Game
{
    public const int GameWidth = 1280;
    public const int GameHeight = 720;

    private readonly GraphicsDeviceManager _graphics;

    public FunkinGame()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = GameWidth,
            PreferredBackBufferHeight = GameHeight,
            SynchronizeWithVerticalRetrace = true,
            GraphicsProfile = GraphicsProfile.HiDef,
        };

        Content.RootDirectory = "assets";
        IsMouseVisible = true;
        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0);
        Window.Title = Constants.TITLE;
        Window.AllowUserResizing = true;
    }

    protected override void Initialize()
    {
        Window.Title = $"{Constants.TITLE} v{Constants.VERSION}";
        FlxG.Initialize(this, _graphics);
        base.Initialize();
    }

    protected override void LoadContent()
    {
        FlxG.LoadContent(GraphicsDevice, Content);
        FlxG.SwitchState(new InitState());
        base.LoadContent();
    }

    private float _elapsed;

    protected override void Update(GameTime gameTime)
    {
        FlxG.Update(gameTime);

        if (CommandLine.IsSmokeTest)
        {
            _elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_elapsed > CommandLine.SmokeTimeout)
            {
                Logger.Error($"Smoke test timed out after {CommandLine.SmokeTimeout}s.");
                Environment.Exit(2);
            }
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(FlxG.BackgroundColor);
        FlxG.Draw(gameTime);
        base.Draw(gameTime);
    }

    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        FlxG.OnExit();
        base.OnExiting(sender, args);
    }
}
