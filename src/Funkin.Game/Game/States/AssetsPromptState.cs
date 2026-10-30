using Funkin.Assets;
using Funkin.Flixel;
using Funkin.Util;
using Microsoft.Xna.Framework;

namespace Funkin.Game.States;

/// <summary>
/// Shown on first launch when the game's assets are missing: offers to download them from the
/// official repository, or to continue with coloured placeholders.
/// </summary>
public sealed class AssetsPromptState : MenuListState
{
    /// <summary>Approximate size of the asset repository archive, used for the prompt text.</summary>
    public const string ApproximateSize = "3.7 GB";

    private FlxText _info = null!;

    public override void Create()
    {
        FlxG.BackgroundColor = new Color(12, 14, 22);
        MenuTitle = "ASSETS REQUIRED";
        ItemStartY = 452f;
        ItemSpacing = 56f;
        ItemFontSize = 32;

        var source = CommandLine.BuildAssetSource();
        string assetsPath = Path.Combine(Paths.FunkinPaths.Instance.GameRoot, "assets");
        long freeSpace = AssetLayout.GetAvailableDiskSpace(Paths.FunkinPaths.Instance.GameRoot);

        string freeText = freeSpace == long.MaxValue ? "unknown" : AssetDownloader.FormatSize(freeSpace);

        _info = new FlxText(120, 180, FunkinGame.GameWidth - 240, string.Empty, 26)
        {
            ScrollFactor = Vector2.Zero,
            Color = new Color(210, 214, 226),
        };

        _info.Text = string.Join("\n",
            "Friday Night Funkin' needs its artwork, charts and music in order to run.",
            "They are not part of this download because they are large.",
            string.Empty,
            $"Source:  {source.DisplayName} ({source.Branch})",
            $"Size:    about {ApproximateSize}",
            $"Install: {assetsPath}",
            $"Free space: {freeText}",
            string.Empty,
            "You can play right now with coloured placeholders and silent audio,",
            "and run this download later from the Options menu.");

        Add(_info);

        AddItem($"DOWNLOAD ASSETS (~{ApproximateSize})", StartDownload, "Download the assets from GitHub. Resumable if interrupted.");
        AddItem("PLAY WITH PLACEHOLDERS", PlayWithoutAssets, "Skip the download for now; art and audio will be missing.");
        AddItem("QUIT", () => Environment.Exit(0), "Close the game.");

        BuildMenuTexts();
    }

    private static void StartDownload()
    {
        // Avoid showing this prompt again if the download fails and we return here.
        CommandLine.SuppressAssetPrompt = true;
        FlxG.SwitchState(new AssetDownloadState());
    }

    private static void PlayWithoutAssets()
    {
        Logger.Warn("Continuing without game assets; placeholder art will be used.");
        CommandLine.SuppressAssetPrompt = true;
        FlxG.SwitchState(new InitState());
    }

    protected override void OnBack() => PlayWithoutAssets();
}
