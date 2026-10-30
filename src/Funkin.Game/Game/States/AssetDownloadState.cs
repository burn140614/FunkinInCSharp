using Funkin.Assets;
using Funkin.Flixel;
using Funkin.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Funkin.Game.States;

/// <summary>
/// Downloads the asset repository and unpacks it into `assets/`, showing live progress.
/// The download itself runs off the game thread; this state only renders its status.
/// </summary>
public sealed class AssetDownloadState : FlxState
{
    private readonly AssetDownloader _downloader;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly object _sync = new();

    private Task<AssetInstallResult>? _task;
    private AssetDownloadProgress? _progress;
    private AssetInstallResult? _result;

    private FlxSprite _barBackground = null!;
    private FlxSprite _barFill = null!;
    private FlxText _titleText = null!;
    private FlxText _statusText = null!;
    private FlxText _detailText = null!;
    private FlxText _hintText = null!;

    private const int BarWidth = 860;
    private const int BarHeight = 34;

    private float _displayedFraction;
    private bool _cancelling;

    public AssetDownloadState()
    {
        _downloader = new AssetDownloader(CommandLine.BuildAssetSource(), Paths.FunkinPaths.Instance.GameRoot);
    }

    public override void Create()
    {
        FlxG.BackgroundColor = new Color(12, 14, 22);

        int centerX = FunkinGame.GameWidth / 2;

        _titleText = new FlxText(0, 150, FunkinGame.GameWidth, "DOWNLOADING ASSETS", 64)
        {
            Alignment = TextAlign.Center,
            ScrollFactor = Vector2.Zero,
            Color = new Color(255, 90, 160),
        };

        _barBackground = new FlxSprite(centerX - BarWidth / 2f, 330)
            .MakeGraphic(BarWidth, BarHeight, new Color(38, 42, 56));
        _barBackground.ScrollFactor = Vector2.Zero;

        _barFill = new FlxSprite(centerX - BarWidth / 2f, 330)
            .MakeGraphic(BarWidth, BarHeight, new Color(164, 255, 17));
        _barFill.ScrollFactor = Vector2.Zero;
        _barFill.Scale = new Vector2(0.0001f, 1f);

        _statusText = new FlxText(0, 390, FunkinGame.GameWidth, "Preparing...", 30)
        {
            Alignment = TextAlign.Center,
            ScrollFactor = Vector2.Zero,
        };

        _detailText = new FlxText(0, 440, FunkinGame.GameWidth, string.Empty, 24)
        {
            Alignment = TextAlign.Center,
            ScrollFactor = Vector2.Zero,
            Color = new Color(180, 186, 200),
        };

        _hintText = new FlxText(0, FunkinGame.GameHeight - 90, FunkinGame.GameWidth, "ESC to cancel", 22)
        {
            Alignment = TextAlign.Center,
            ScrollFactor = Vector2.Zero,
            Color = new Color(150, 154, 168),
        };

        Add(_titleText);
        Add(_barBackground);
        Add(_barFill);
        Add(_statusText);
        Add(_detailText);
        Add(_hintText);

        Logger.Info($"Downloading assets from {CommandLine.BuildAssetSource().ArchiveUrl}");

        var progress = new Progress<AssetDownloadProgress>(snapshot =>
        {
            lock (_sync) _progress = snapshot;
        });

        _task = Task.Run(() => _downloader.InstallAsync(progress, _cancellation.Token));
    }

    public override void Update(float elapsed)
    {
        if (!_cancelling && FlxG.Input.JustPressed(Funkin.Input.GameAction.Back))
        {
            Cancel();
        }

        AssetDownloadProgress? snapshot;
        lock (_sync) snapshot = _progress;

        if (snapshot is not null) UpdateFromProgress(snapshot, elapsed);

        if (_task is not null && _task.IsCompleted)
        {
            HandleCompletion(_task.Result);
            _task = null;
            return;
        }

        if (_result is not null)
        {
            if (FlxG.Input.JustPressed(Funkin.Input.GameAction.Accept)) FlxG.SwitchState(new InitState());
            else if (FlxG.Input.JustPressed(Funkin.Input.GameAction.Back)) FlxG.SwitchState(new AssetsPromptState());
        }

        base.Update(elapsed);
    }

    private void UpdateFromProgress(AssetDownloadProgress snapshot, float elapsed)
    {
        bool unknownTotal = snapshot.Phase == AssetInstallPhase.Downloading && snapshot.TotalBytes is not > 0;

        if (unknownTotal)
        {
            // GitHub streams zipballs chunked, so there is no content length to divide by.
            // Pulse the bar to show activity instead of pretending to know the percentage.
            float pulse = 0.12f + 0.88f * MathF.Abs(MathF.Sin((float)FlxG.Time * 1.4f));
            _displayedFraction = pulse;
        }
        else
        {
            // Smooth the bar so it does not jump around with the throttled reports.
            _displayedFraction = MathUtil.Lerp(_displayedFraction, (float)snapshot.Fraction, Math.Min(1f, elapsed * 12f));
        }

        _barFill.Scale = new Vector2(Math.Max(0.0001f, _displayedFraction), 1f);

        float fraction = (float)snapshot.Fraction;

        _statusText.Text = snapshot.Phase switch
        {
            AssetInstallPhase.Preparing => "Preparing...",
            AssetInstallPhase.Downloading => snapshot.TotalBytes is > 0
                ? $"Downloading... {fraction * 100:0.0}%"
                : "Downloading...",
            AssetInstallPhase.Extracting => $"Installing... {fraction * 100:0.0}%",
            AssetInstallPhase.Done => "Done!",
            AssetInstallPhase.Cancelled => "Cancelled.",
            AssetInstallPhase.Failed => "Failed.",
            _ => snapshot.Message,
        };

        if (snapshot.Phase == AssetInstallPhase.Downloading)
        {
            string size = snapshot.TotalBytes is { } total
                ? $"{AssetDownloader.FormatSize(snapshot.BytesReceived)} / {AssetDownloader.FormatSize(total)}"
                : AssetDownloader.FormatSize(snapshot.BytesReceived);

            string speed = $"{AssetDownloader.FormatSize((long)snapshot.BytesPerSecond)}/s";
            string eta = snapshot.Eta is { } remaining ? $" - {FormatDuration(remaining)} left" : string.Empty;

            _detailText.Text = snapshot.TotalBytes is > 0
                ? $"{size} at {speed}{eta}"
                : $"{size} at {speed} (total size unknown; GitHub streams this archive)";
        }
        else if (snapshot.Phase == AssetInstallPhase.Extracting)
        {
            _detailText.Text = snapshot.Message;
        }
        else
        {
            _detailText.Text = snapshot.Message;
        }
    }

    private static string FormatDuration(TimeSpan span)
        => span.TotalHours >= 1 ? $"{(int)span.TotalHours}h {span.Minutes}m" : span.TotalMinutes >= 1 ? $"{(int)span.TotalMinutes}m {span.Seconds}s" : $"{span.Seconds}s";

    private void HandleCompletion(AssetInstallResult result)
    {
        _result = result;

        switch (result.Status)
        {
            case AssetInstallStatus.Success:
            case AssetInstallStatus.AlreadyPresent:
                Logger.Info($"Assets installed: {result.Message}");
                CommandLine.SuppressAssetPrompt = true;
                _titleText.Text = "ASSETS INSTALLED";
                _statusText.Text = "Starting...";
                _detailText.Text = result.Message ?? string.Empty;
                _hintText.Text = "";
                FlxG.SwitchState(new InitState());
                return;

            case AssetInstallStatus.Cancelled:
                Logger.Warn("Asset download cancelled; progress has been kept for later.");
                _titleText.Text = "DOWNLOAD CANCELLED";
                _statusText.Text = "Progress saved - you can resume at any time.";
                _detailText.Text = result.Message ?? string.Empty;
                _hintText.Text = "ENTER to resume  -  ESC to go back";
                break;

            default:
                Logger.Error($"Asset download failed ({result.Status}): {result.Message}");
                _titleText.Text = "DOWNLOAD FAILED";
                _statusText.Text = result.Message ?? result.Status.ToString();
                _detailText.Text = $"You can retry, or play without assets.";
                _hintText.Text = "ENTER to retry  -  ESC to go back";
                break;
        }
    }

    private void Cancel()
    {
        _cancelling = true;
        _cancellation.Cancel();
        _statusText.Text = "Cancelling...";
    }

    public override void Destroy()
    {
        if (!_cancellation.IsCancellationRequested) _cancellation.Cancel();
        _cancellation.Dispose();
        _downloader.Dispose();
        base.Destroy();
    }
}
