using Funkin.Flixel;
using Funkin.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Funkin.Game.States;

/// <summary>
/// Shared implementation for the game's vertical menus (main menu, story, freeplay, options).
/// Ports the cursor/selection behaviour from flixel's FlxTypedGroup-based menus.
/// </summary>
public abstract class MenuListState : FlxState
{
    protected sealed class MenuItem
    {
        public required string Label { get; init; }
        public string? Help { get; init; }
        public Action? OnSelect { get; init; }
        public Func<string>? DynamicLabel { get; init; }
        public Func<bool>? IsEnabled { get; init; }

        public string DisplayLabel => DynamicLabel?.Invoke() ?? Label;
        public bool Enabled => IsEnabled?.Invoke() ?? true;
    }

    protected readonly List<MenuItem> Items = new();
    protected int Cursor;
    protected float CursorY;

    protected readonly List<FlxText> ItemTexts = new();
    protected FlxText? CursorText;
    protected FlxText? HelpText;
    protected FlxText? TitleText;

    public string MenuTitle { get; set; } = string.Empty;

    protected float ItemStartY { get; set; } = 240f;
    protected float ItemSpacing { get; set; } = 64f;
    protected int ItemFontSize { get; set; } = 40;

    protected void AddItem(string label, Action? onSelect, string? help = null, Func<string>? dynamicLabel = null, Func<bool>? isEnabled = null)
    {
        Items.Add(new MenuItem
        {
            Label = label,
            OnSelect = onSelect,
            Help = help,
            DynamicLabel = dynamicLabel,
            IsEnabled = isEnabled,
        });
    }

    protected void BuildMenuTexts()
    {
        TitleText = new FlxText(0, 80, FunkinGame.GameWidth, MenuTitle, 72)
        {
            Alignment = TextAlign.Center,
            ScrollFactor = Vector2.Zero,
        };
        Add(TitleText);

        for (int i = 0; i < Items.Count; i++)
        {
            var text = new FlxText(FunkinGame.GameWidth / 2f - 260, ItemStartY + i * ItemSpacing, 700, Items[i].DisplayLabel, ItemFontSize)
            {
                Alignment = TextAlign.Left,
                ScrollFactor = Vector2.Zero,
            };
            ItemTexts.Add(text);
            Add(text);
        }

        CursorText = new FlxText(FunkinGame.GameWidth / 2f - 320, ItemStartY, 200, ">", ItemFontSize)
        {
            ScrollFactor = Vector2.Zero,
            Color = new Color(255, 220, 60),
        };
        Add(CursorText);

        HelpText = new FlxText(0, FunkinGame.GameHeight - 90, FunkinGame.GameWidth, string.Empty, 24)
        {
            Alignment = TextAlign.Center,
            ScrollFactor = Vector2.Zero,
            Color = new Color(200, 200, 200),
        };
        Add(HelpText);

        RefreshLabels();
    }

    protected void RefreshLabels()
    {
        for (int i = 0; i < Items.Count && i < ItemTexts.Count; i++)
        {
            ItemTexts[i].Text = Items[i].DisplayLabel;
            ItemTexts[i].Color = Items[i].Enabled ? Color.White : new Color(130, 130, 130);
        }

        if (HelpText is not null && Items.Count > 0)
        {
            HelpText.Text = Items[Cursor].Help ?? string.Empty;
        }
    }

    protected void MoveCursor(int delta)
    {
        if (Items.Count == 0) return;
        Cursor = (Cursor + delta + Items.Count) % Items.Count;
        FlxG.Sound.PlaySfx(Paths.FunkinPaths.Instance.Sound("scrollMenu"));
        RefreshLabels();
    }

    protected void Accept()
    {
        if (Items.Count == 0) return;

        if (!Items[Cursor].Enabled)
        {
            FlxG.Sound.PlaySfx(Paths.FunkinPaths.Instance.Sound("cancelMenu"));
            return;
        }

        FlxG.Sound.PlaySfx(Paths.FunkinPaths.Instance.Sound("confirmMenu"));
        Items[Cursor].OnSelect?.Invoke();
    }

    public override void Update(float elapsed)
    {
        if (FlxG.Input.JustPressed(Funkin.Input.GameAction.Up) || FlxG.Input.JustPressed(Microsoft.Xna.Framework.Input.Keys.Up))
        {
            MoveCursor(-1);
        }

        if (FlxG.Input.JustPressed(Funkin.Input.GameAction.Down) || FlxG.Input.JustPressed(Microsoft.Xna.Framework.Input.Keys.Down))
        {
            MoveCursor(1);
        }

        if (FlxG.Input.JustPressed(Microsoft.Xna.Framework.Input.Keys.Enter) || FlxG.Input.JustPressed(Microsoft.Xna.Framework.Input.Keys.Space))
        {
            Accept();
        }

        if (FlxG.Input.JustPressed(Microsoft.Xna.Framework.Input.Keys.Back) || FlxG.Input.JustPressed(Microsoft.Xna.Framework.Input.Keys.Escape))
        {
            OnBack();
        }

        UpdateCursorVisual(elapsed);
        base.Update(elapsed);
    }

    private void UpdateCursorVisual(float elapsed)
    {
        if (CursorText is null || Items.Count == 0) return;

        float targetY = ItemStartY + Cursor * ItemSpacing;
        CursorY = MathUtil.Lerp(CursorY == 0 ? targetY : CursorY, targetY, Math.Min(1f, elapsed * 18f));
        CursorText.Position = new Vector2(CursorText.Position.X, CursorY);

        // Gentle pulse so the selection is obvious even without menu art.
        float pulse = 1f + MathF.Sin((float)FlxG.Time * 6f) * 0.08f;
        CursorText.Scale = new Vector2(pulse);
    }

    protected virtual void OnBack()
    {
    }
}
