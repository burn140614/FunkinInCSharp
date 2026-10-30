using Funkin.Flixel;
using Funkin.Save;
using Funkin.Util;
using Microsoft.Xna.Framework;

namespace Funkin.Game.States;

/// <summary>
/// Port of funkin.ui.options.OptionsState. Every entry writes straight to the save data, so
/// preferences survive a restart exactly like the original's SaveProperty bindings.
/// </summary>
public sealed class OptionsState : MenuListState
{
    private readonly Func<FlxState>? _returnState;

    /// <param name="returnState">Where BACK goes; defaults to the main menu.</param>
    public OptionsState(Func<FlxState>? returnState = null) => _returnState = returnState;

    private SaveDataOptions Options => SaveManager.Instance.Data.Options;

    private static readonly int[] FrameRates = { 60, 120, 144, 240 };

    public override void Create()
    {
        FlxG.BackgroundColor = new Color(18, 18, 26);
        MenuTitle = "OPTIONS";
        ItemStartY = 160f;
        ItemSpacing = 44f;
        ItemFontSize = 26;

        AddItem("Downscroll", () => Toggle(() => Options.Downscroll = !Options.Downscroll),
            "Put the strumline at the bottom of the screen.",
            () => $"Downscroll: {(Options.Downscroll ? "ON" : "OFF")}");

        AddItem("Ghost Tapping", () => Toggle(() => Options.GhostTapping = !Options.GhostTapping),
            "No health penalty for pressing with no note nearby.",
            () => $"Ghost Tapping: {(Options.GhostTapping ? "ON" : "OFF")}");

        AddItem("Botplay", () => Toggle(() => Options.Botplay = !Options.Botplay),
            "Let the game play the chart for you.",
            () => $"Botplay: {(Options.Botplay ? "ON" : "OFF")}");

        AddItem("Camera Zoom", () => Toggle(() => Options.ZoomCamera = !Options.ZoomCamera),
            "Zoom and bob the camera on the beat.",
            () => $"Camera Zoom: {(Options.ZoomCamera ? "ON" : "OFF")}");

        AddItem("Score Display", () => Toggle(() =>
            {
                Options.ScoreDisplayMode = Options.ScoreDisplayMode == "Score" ? "Accuracy" : "Score";
            }),
            "Show the raw score or the accuracy percentage.",
            () => $"Score Display: {Options.ScoreDisplayMode}");

        AddItem("Framerate", () => Toggle(() =>
            {
                int index = Array.IndexOf(FrameRates, Options.Framerate);
                Options.Framerate = FrameRates[(index + 1 + FrameRates.Length) % FrameRates.Length];
            }),
            "Target framerate (the game is designed around 60).",
            () => $"Framerate: {Options.Framerate}");

        AddItem("Volume", () => Toggle(() =>
            {
                double next = Math.Round(SaveManager.Instance.Data.Volume, 1) - 0.1;
                if (next < -0.001) next = 1.0;
                SaveManager.Instance.SetVolume(next);
            }),
            "Master volume.",
            () => $"Volume: {SaveManager.Instance.Data.Volume * 100:0}%");

        AddItem("Note Offset", () => Toggle(() =>
            {
                int next = Options.NoteOffset + Options.NoteOffsetStep;
                if (next > 100) next = -100;
                Options.NoteOffset = next;
            }),
            "Timing offset for note judgement, in milliseconds.",
            () => $"Note Offset: {Options.NoteOffset} ms");

        AddItem("Audio/Visual Offset", () => Toggle(() =>
            {
                int next = Options.AudioVisualOffset + 5;
                if (next > 100) next = -100;
                Options.AudioVisualOffset = next;
            }),
            "Compensate for audio latency, in milliseconds.",
            () => $"A/V Offset: {Options.AudioVisualOffset} ms");

        AddItem("Scroll Speed Override", () => Toggle(() => Options.ScrollSpeedOverride = !Options.ScrollSpeedOverride),
            "Use a fixed scroll speed instead of the chart's.",
            () => $"Scroll Speed Override: {(Options.ScrollSpeedOverride ? $"{Options.ScrollSpeed:0.0}" : "OFF")}");

        AddItem("Reset Save Data", ResetSave, "Delete all scores and restore default options.");

        AddItem("BACK", () => FlxG.SwitchState(_returnState?.Invoke() ?? new MainMenuState()), "Return to the previous screen.");

        BuildMenuTexts();
    }

    private void Toggle(Action action)
    {
        action();
        SaveManager.Instance.MarkDirty();
        SaveManager.Instance.Flush();
        RefreshLabels();
    }

    private void ResetSave()
    {
        SaveManager.Instance.ResetToDefaults();
        RefreshLabels();
    }

    public override void Update(float elapsed)
    {
        base.Update(elapsed);

        if (FlxG.Input.JustPressed(Microsoft.Xna.Framework.Input.Keys.Left) || FlxG.Input.JustPressed(Microsoft.Xna.Framework.Input.Keys.A))
        {
            MoveCursor(-1);
        }

        if (FlxG.Input.JustPressed(Microsoft.Xna.Framework.Input.Keys.Right) || FlxG.Input.JustPressed(Microsoft.Xna.Framework.Input.Keys.D))
        {
            MoveCursor(1);
        }
    }

    protected override void OnBack()
    {
        SaveManager.Instance.Flush();
        FlxG.SwitchState(_returnState?.Invoke() ?? new MainMenuState());
    }
}
