using Funkin.Assets;
using Funkin.Util;

namespace Funkin;

/// <summary>
/// Command-line switches. They exist so the port can be smoke-tested
/// (`--smoke-song=tutorial --smoke-timeout=120`) inside a headless X server in CI.
/// </summary>
public static class CommandLine
{
    /// <summary>Overrides the game root (the folder that contains `assets/`).</summary>
    public static string? GameRoot { get; private set; }

    /// <summary>When set, the game boots straight into a song with Botplay enabled.</summary>
    public static string? SmokeSong { get; private set; }

    public static string SmokeDifficulty { get; private set; } = Constants.DEFAULT_DIFFICULTY;

    /// <summary>Abort with a failure exit code after this many seconds.</summary>
    public static float SmokeTimeout { get; private set; } = 120f;

    /// <summary>Skip the "download assets?" prompt and run with placeholders.</summary>
    public static bool SuppressAssetPrompt { get; set; }

    /// <summary>Start the asset download immediately instead of asking.</summary>
    public static bool AutoDownloadAssets { get; private set; }

    /// <summary>Overrides the archive URL (mirrors/local tests).</summary>
    public static string? AssetArchiveUrl { get; private set; }

    /// <summary>Overrides the assets repository, as `owner/name`.</summary>
    public static string? AssetRepo { get; private set; }

    public static string AssetBranch { get; private set; } = "main";

    /// <summary>Builds the asset source configured on the command line.</summary>
    public static AssetSource BuildAssetSource()
    {
        if (!string.IsNullOrEmpty(AssetArchiveUrl)) return new AssetSource("local", "archive", AssetBranch, AssetArchiveUrl);

        if (!string.IsNullOrEmpty(AssetRepo))
        {
            var parts = AssetRepo.Split('/', 2);
            if (parts.Length == 2) return new AssetSource(parts[0], parts[1], AssetBranch);
        }

        return new AssetSource(AssetSource.Default.Owner, AssetSource.Default.Repo, AssetBranch);
    }

    /// <summary>True when running an automated playthrough.</summary>
    public static bool IsSmokeTest => SmokeSong is not null;

    /// <summary>Prints usage information and exits when `--help`/`--version` is passed.</summary>
    public static void HandleInformationalSwitches(string[] args)
    {
        if (args.Contains("--help") || args.Contains("-h"))
        {
            Console.WriteLine($"""
                {Constants.TITLE} v{Constants.VERSION} - C# port (MonoGame)

                Usage: Funkin [options]

                Options:
                  --root=<folder>          Game root containing the assets/ folder.
                  --smoke-song=<songId>    Boot straight into a song with Botplay enabled.
                  --smoke-difficulty=<d>   Difficulty for the automated playthrough.
                  --smoke-timeout=<secs>   Abort the automated playthrough after N seconds.
                  --download-assets        Download the game's assets without prompting.
                  --skip-asset-prompt      Never ask for the assets; use placeholders.
                  --asset-url=<url>        Download assets from a custom archive URL.
                  --asset-repo=<owner/name> Asset repository to download from.
                  --asset-branch=<branch>  Branch of the asset repository (default: main).
                  --verbose                Enable debug logging.
                  --help, -h               Show this message.
                  --version                Print the version and exit.
                """);
            Environment.Exit(0);
        }

        if (args.Contains("--version"))
        {
            Console.WriteLine($"{Constants.TITLE} v{Constants.VERSION} (C# port, MonoGame)");
            Environment.Exit(0);
        }
    }

    public static void Parse(string[] args)
    {
        foreach (var arg in args)
        {
            var parts = arg.Split('=', 2);
            string key = parts[0].TrimStart('-');
            string? value = parts.Length > 1 ? parts[1] : null;

            switch (key)
            {
                case "root" or "game-root":
                    GameRoot = value;
                    break;

                case "smoke-song" or "song":
                    SmokeSong = value;
                    break;

                case "smoke-difficulty" or "difficulty":
                    SmokeDifficulty = value ?? Constants.DEFAULT_DIFFICULTY;
                    break;

                case "smoke-timeout" or "timeout":
                    if (float.TryParse(value, out var timeout)) SmokeTimeout = timeout;
                    break;

                case "download-assets":
                    AutoDownloadAssets = true;
                    break;

                case "skip-asset-prompt" or "no-asset-prompt":
                    SuppressAssetPrompt = true;
                    break;

                case "asset-url":
                    AssetArchiveUrl = value;
                    break;

                case "asset-repo":
                    AssetRepo = value;
                    break;

                case "asset-branch":
                    AssetBranch = value ?? "main";
                    break;

                case "verbose":
                    Logger.MinimumLevel = LogLevel.Debug;
                    break;
            }
        }

        if (GameRoot is not null) Logger.Info($"Using game root '{GameRoot}' from the command line.");
    }
}
