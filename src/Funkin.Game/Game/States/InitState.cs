using Funkin.Data.Character;
using Funkin.Data.Freeplay;
using Funkin.Data.Song;
using Funkin.Data.Stage;
using Funkin.Data.Story;
using Funkin.Flixel;
using Funkin.Modding;
using Funkin.Paths;
using Funkin.Save;
using Funkin.Util;
using Microsoft.Xna.Framework;

namespace Funkin.Game.States;

/// <summary>
/// Port of funkin.InitState: the boot screen. Resolves the game root, loads mods, the save file and
/// every data registry, then transitions to the title screen.
/// </summary>
public sealed class InitState : FlxState
{
    private FlxText _status = null!;
    private int _step;
    private float _timer;

    public override void Create()
    {
        FlxG.BackgroundColor = new Color(Constants.COLOR_PRELOADER_BG);

        _status = new FlxText(0, FunkinGame.GameHeight / 2f - 40, FunkinGame.GameWidth, "Loading...", 32)
        {
            Alignment = TextAlign.Center,
            ScrollFactor = Vector2.Zero,
        };
        Add(_status);
    }

    public override void Update(float elapsed)
    {
        _timer += elapsed;

        // One initialisation step per frame keeps the loading screen responsive.
        if (_timer < 0.05f) return;
        _timer = 0f;

        switch (_step++)
        {
            case 0:
                _status.Text = "Resolving asset paths...";
                FunkinPaths.Initialize(CommandLine.GameRoot ?? FindGameRoot());
                break;

            case 1:
                _status.Text = "Loading mods...";
                Mods.Instance.ScanMods(FunkinPaths.Instance.ModsRoot, SaveManager.Instance.Data.EnabledModDirs);
                break;

            case 2:
                _status.Text = "Loading save data...";
                SaveManager.Instance.Initialize(FunkinPaths.Instance.SaveRoot);
                break;

            case 3:
                _status.Text = "Loading characters...";
                CharacterRegistry.Instance.LoadAll();
                break;

            case 4:
                _status.Text = "Loading stages...";
                StageRegistry.Instance.LoadAll();
                break;

            case 5:
                _status.Text = "Loading note kinds...";
                Play.Notes.NoteKindManager.Instance.LoadAll();
                break;

            case 6:
                _status.Text = "Loading albums and weeks...";
                AlbumRegistry.Instance.LoadAll();
                LevelRegistry.Instance.LoadAll();
                break;

            case 7:
                _status.Text = "Loading songs...";
                SongRegistry.Instance.LoadAll();
                break;

            default:
                Logger.Info($"Initialised {Constants.TITLE} v{Constants.VERSION}.");

                if (CommandLine.IsSmokeTest)
                {
                    // Automated playthrough: force botplay so the run needs no input.
                    SaveManager.Instance.Data.Options.Botplay = true;
                    Logger.Info($"Smoke test: playing '{CommandLine.SmokeSong}' ({CommandLine.SmokeDifficulty}).");
                    FlxG.SwitchState(ConversationState.CreateForSong(CommandLine.SmokeSong!, CommandLine.SmokeDifficulty));
                }
                else
                {
                    FlxG.SwitchState(new TitleState());
                }

                break;
        }
    }

    /// <summary>
    /// Finds the folder containing `assets/`. Works from a published build (next to the executable)
    /// and from a development run (walking up out of bin/).
    /// </summary>
    private static string FindGameRoot()
    {
        var candidates = new List<string> { AppContext.BaseDirectory, Directory.GetCurrentDirectory() };

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (int i = 0; i < 6 && dir is not null; i++)
        {
            candidates.Add(dir.FullName);
            dir = dir.Parent;
        }

        foreach (var candidate in candidates)
        {
            if (Directory.Exists(Path.Combine(candidate, "assets"))) return candidate;
        }

        Logger.Warn("No 'assets' folder found; running with placeholder art and no audio.");
        return Directory.GetCurrentDirectory();
    }
}
