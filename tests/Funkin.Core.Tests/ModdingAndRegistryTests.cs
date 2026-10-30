using Funkin.Data.Character;
using Funkin.Data.Song;
using Funkin.Data.Stage;
using Funkin.Modding;
using Funkin.Paths;
using Xunit;

namespace Funkin.Tests;

/// <summary>Verifies the asset-path resolution and data registries against a temporary asset tree.</summary>
public class ModdingAndRegistryTests : IDisposable
{
    private readonly string _root;

    public ModdingAndRegistryTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "funkin-assets-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_root, "assets", "data", "characters"));
        Directory.CreateDirectory(Path.Combine(_root, "assets", "data", "songs", "test"));
        Directory.CreateDirectory(Path.Combine(_root, "assets", "data", "stages"));
        Directory.CreateDirectory(Path.Combine(_root, "mods"));

        File.WriteAllText(Path.Combine(_root, "assets", "data", "characters", "bf.json"), """
        {
          "name": "Boyfriend",
          "assetPath": "characters/BOYFRIEND",
          "scale": 1,
          "animations": [
            { "name": "idle", "prefix": "BF idle dance", "frameRate": 24 },
            { "name": "singLEFT", "prefix": "BF NOTE LEFT" }
          ]
        }
        """);

        File.WriteAllText(Path.Combine(_root, "assets", "data", "stages", "mainStage.json"), """
        {
          "name": "Main Stage",
          "cameraZoom": 1.05,
          "props": [
            { "name": "stageback", "assetPath": "stages/stageback", "position": [-600, -200], "zIndex": 0 }
          ]
        }
        """);

        File.WriteAllText(Path.Combine(_root, "assets", "data", "songs", "test", "test-metadata.json"), """
        {
          "version": "2.2.3",
          "songName": "Test",
          "artist": "Tester",
          "timeChanges": [ { "t": 0, "bpm": 100, "bt": [4, 4, 4, 4] } ],
          "playData": {
            "difficulties": ["easy", "normal", "hard"],
            "characters": { "player": "bf", "girlfriend": "gf", "opponent": "dad" },
            "stage": "mainStage",
            "noteStyle": "funkin",
            "ratings": { "normal": 3 }
          }
        }
        """);

        File.WriteAllText(Path.Combine(_root, "assets", "data", "songs", "test", "test-chart.json"), """
        {
          "version": "2.2.3",
          "scrollSpeed": { "normal": 1.5 },
          "events": [],
          "notes": { "normal": [ { "t": 0, "d": 0 }, { "t": 600, "d": 1, "l": 300 } ] }
        }
        """);

        FunkinPaths.Initialize(_root);
        Mods.Instance.ScanMods(Path.Combine(_root, "mods"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    [Fact]
    public void ResolvesBaseGameAssets()
    {
        Assert.NotNull(FunkinPaths.Instance.Resolve("data/characters/bf.json"));
        Assert.Null(FunkinPaths.Instance.Resolve("data/characters/missing.json"));
    }

    [Fact]
    public void LoadsCharacterData()
    {
        var data = CharacterRegistry.Instance.Load("bf");
        Assert.NotNull(data);
        Assert.Equal("Boyfriend", data!.Name);
        Assert.Equal(2, data.Animations.Count);
        Assert.NotNull(data.GetAnimation("idle"));
    }

    [Fact]
    public void LoadsStageData()
    {
        var stage = StageRegistry.Instance.Load("mainStage");
        Assert.NotNull(stage);
        Assert.Equal(1.05f, stage!.CameraZoomOrDefault, 3);
        Assert.Single(stage.Props);
        Assert.NotNull(stage.GetProp("stageback"));
    }

    [Fact]
    public void ListsAndLoadsSongs()
    {
        var ids = SongRegistry.Instance.ListSongIds();
        Assert.Contains("test", ids);

        var song = SongRegistry.Instance.LoadSong("test");
        Assert.NotNull(song);
        Assert.Equal("Test", song!.SongName);

        var difficulty = song.GetDifficulty("normal");
        Assert.NotNull(difficulty);
        Assert.Equal(1.5f, difficulty!.ScrollSpeed, 3);
        Assert.Equal(3, difficulty.DifficultyRating);
        Assert.Equal(2, difficulty.Notes.Count);
        Assert.Equal(300, difficulty.Notes[1].Length, 3);
        Assert.Equal(2, difficulty.CountPlayerNotes());
    }

    [Fact]
    public void ModsOverrideBaseAssets()
    {
        string modDir = Path.Combine(_root, "mods", "coolmod");
        Directory.CreateDirectory(Path.Combine(modDir, "assets", "data", "characters"));
        File.WriteAllText(Path.Combine(modDir, "_polymod_meta.json"), """
        { "title": "Cool Mod", "author": "Someone", "mod_version": "1.0.0", "api_version": "1.0.0" }
        """);
        File.WriteAllText(Path.Combine(modDir, "assets", "data", "characters", "bf.json"), """
        { "name": "Modded Boyfriend", "assetPath": "characters/BOYFRIEND", "animations": [] }
        """);

        Mods.Instance.ScanMods(Path.Combine(_root, "mods"));
        CharacterRegistry.Instance.Load("bf");

        Assert.True(Mods.Instance.IsModLoaded("coolmod"));
        Assert.Contains("coolmod", FunkinPaths.Instance.Resolve("data/characters/bf.json"));
    }
}
