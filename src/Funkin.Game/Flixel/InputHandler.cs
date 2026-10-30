using Funkin.Input;
using Microsoft.Xna.Framework.Input;

namespace Funkin.Flixel;

/// <summary>
/// Port of flixel.input.FlxInput (keyboard + gamepad part). Tracks the current and previous
/// keyboard/gamepad state so code can ask for `Pressed`/`JustPressed`/`JustReleased` on GameActions.
/// </summary>
public sealed class InputHandler
{
    private KeyboardState _currentKeys;
    private KeyboardState _previousKeys;
    private GamePadState _currentPad;
    private GamePadState _previousPad;

    public MouseState CurrentMouse { get; private set; }
    public MouseState PreviousMouse { get; private set; }

    public void Update()
    {
        _previousKeys = _currentKeys;
        _currentKeys = Keyboard.GetState();

        _previousPad = _currentPad;
        _currentPad = GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.One);

        PreviousMouse = CurrentMouse;
        CurrentMouse = Mouse.GetState();
    }

    public static GameKey ToGameKey(Keys key) => key switch
    {
        Keys.A => GameKey.A, Keys.B => GameKey.B, Keys.C => GameKey.C, Keys.D => GameKey.D,
        Keys.E => GameKey.E, Keys.F => GameKey.F, Keys.G => GameKey.G, Keys.H => GameKey.H,
        Keys.I => GameKey.I, Keys.J => GameKey.J, Keys.K => GameKey.K, Keys.L => GameKey.L,
        Keys.M => GameKey.M, Keys.N => GameKey.N, Keys.O => GameKey.O, Keys.P => GameKey.P,
        Keys.Q => GameKey.Q, Keys.R => GameKey.R, Keys.S => GameKey.S, Keys.T => GameKey.T,
        Keys.U => GameKey.U, Keys.V => GameKey.V, Keys.W => GameKey.W, Keys.X => GameKey.X,
        Keys.Y => GameKey.Y, Keys.Z => GameKey.Z,
        Keys.D0 => GameKey.Number0, Keys.D1 => GameKey.Number1, Keys.D2 => GameKey.Number2,
        Keys.D3 => GameKey.Number3, Keys.D4 => GameKey.Number4, Keys.D5 => GameKey.Number5,
        Keys.D6 => GameKey.Number6, Keys.D7 => GameKey.Number7, Keys.D8 => GameKey.Number8,
        Keys.D9 => GameKey.Number9,
        Keys.Up => GameKey.Up, Keys.Down => GameKey.Down, Keys.Left => GameKey.Left, Keys.Right => GameKey.Right,
        Keys.Space => GameKey.Space, Keys.Enter => GameKey.Enter, Keys.Escape => GameKey.Escape,
        Keys.Back => GameKey.Backspace, Keys.Tab => GameKey.Tab,
        Keys.LeftShift or Keys.RightShift => GameKey.Shift,
        Keys.LeftControl or Keys.RightControl => GameKey.Control,
        Keys.LeftAlt or Keys.RightAlt => GameKey.Alt,
        Keys.OemSemicolon => GameKey.Semicolon, Keys.OemQuotes => GameKey.Apostrophe,
        Keys.OemComma => GameKey.Comma, Keys.OemPeriod => GameKey.Period, Keys.OemQuestion => GameKey.Slash,
        Keys.OemBackslash => GameKey.Backslash, Keys.OemOpenBrackets => GameKey.BracketLeft,
        Keys.OemCloseBrackets => GameKey.BracketRight, Keys.OemMinus => GameKey.Minus, Keys.OemPlus => GameKey.Equal,
        Keys.OemTilde => GameKey.Grave, Keys.OemPipe => GameKey.Backslash,
        Keys.F1 => GameKey.F1, Keys.F2 => GameKey.F2, Keys.F3 => GameKey.F3, Keys.F4 => GameKey.F4,
        Keys.F5 => GameKey.F5, Keys.F6 => GameKey.F6, Keys.F7 => GameKey.F7, Keys.F8 => GameKey.F8,
        Keys.F9 => GameKey.F9, Keys.F10 => GameKey.F10, Keys.F11 => GameKey.F11, Keys.F12 => GameKey.F12,
        Keys.Add => GameKey.Plus,
        Keys.NumPad0 => GameKey.Numpad0, Keys.NumPad1 => GameKey.Numpad1, Keys.NumPad2 => GameKey.Numpad2,
        Keys.NumPad3 => GameKey.Numpad3, Keys.NumPad4 => GameKey.Numpad4, Keys.NumPad5 => GameKey.Numpad5,
        Keys.NumPad6 => GameKey.Numpad6, Keys.NumPad7 => GameKey.Numpad7, Keys.NumPad8 => GameKey.Numpad8,
        Keys.NumPad9 => GameKey.Numpad9,
        _ => GameKey.None,
    };

    /// <summary>Convenience overload so callers can use MonoGame's key enum directly.</summary>
    public bool Pressed(Microsoft.Xna.Framework.Input.Keys key) => _currentKeys.IsKeyDown(key);

    public bool JustPressed(Microsoft.Xna.Framework.Input.Keys key) => _currentKeys.IsKeyDown(key) && _previousKeys.IsKeyUp(key);

    public bool JustReleased(Microsoft.Xna.Framework.Input.Keys key) => _currentKeys.IsKeyUp(key) && _previousKeys.IsKeyDown(key);

    public bool Pressed(GameKey key)
    {
        var monoKey = ToMonoGameKey(key);
        return monoKey is { } pressed && _currentKeys.IsKeyDown(pressed);
    }

    public bool JustPressed(GameKey key)
    {
        var monoKey = ToMonoGameKey(key);
        return monoKey is { } pressed && _currentKeys.IsKeyDown(pressed) && _previousKeys.IsKeyUp(pressed);
    }

    public bool JustReleased(GameKey key)
    {
        var monoKey = ToMonoGameKey(key);
        return monoKey is { } released && _currentKeys.IsKeyUp(released) && _previousKeys.IsKeyDown(released);
    }

    public bool Pressed(GameAction action)
    {
        foreach (var key in Controls.GetKeys(action))
        {
            if (Pressed(key)) return true;
        }

        return action switch
        {
            GameAction.Accept => _currentPad.Buttons.A == ButtonState.Pressed || _currentPad.Buttons.Start == ButtonState.Pressed,
            GameAction.Back => _currentPad.Buttons.B == ButtonState.Pressed || _currentPad.Buttons.Back == ButtonState.Pressed,
            GameAction.Pause => _currentPad.Buttons.Start == ButtonState.Pressed,
            _ => false,
        };
    }

    public bool JustPressed(GameAction action)
    {
        foreach (var key in Controls.GetKeys(action))
        {
            if (JustPressed(key)) return true;
        }

        return action switch
        {
            GameAction.Accept => JustPressedPad(_currentPad.Buttons.A) || JustPressedPad(_currentPad.Buttons.Start),
            GameAction.Back => JustPressedPad(_currentPad.Buttons.B) || JustPressedPad(_currentPad.Buttons.Back),
            GameAction.Pause => JustPressedPad(_currentPad.Buttons.Start),
            _ => false,
        };
    }

    private bool JustPressedPad(ButtonState state) => state == ButtonState.Pressed;

    public bool AnyKeyJustPressed => _currentKeys.GetPressedKeyCount() > 0 && _previousKeys.GetPressedKeyCount() == 0;

    /// <summary>All keys pressed since the previous frame.</summary>
    public IEnumerable<GameKey> KeysJustPressed()
    {
        foreach (var key in _currentKeys.GetPressedKeys())
        {
            if (_previousKeys.IsKeyUp(key))
            {
                yield return ToGameKey(key);
            }
        }
    }

    /// <summary>True when any of the four directional note buttons on the gamepad just went down.</summary>
    public int GamepadNoteJustPressed()
    {
        // D-pad / face buttons map to lanes in the same order as the keyboard.
        if (JustPressedPad(_currentPad.DPad.Left) || JustPressedPad(_currentPad.Buttons.X)) return 0;
        if (JustPressedPad(_currentPad.DPad.Down) || JustPressedPad(_currentPad.Buttons.A)) return 1;
        if (JustPressedPad(_currentPad.DPad.Up) || JustPressedPad(_currentPad.Buttons.Y)) return 2;
        if (JustPressedPad(_currentPad.DPad.Right) || JustPressedPad(_currentPad.Buttons.B)) return 3;
        return -1;
    }

    public int GamepadNoteReleased()
    {
        if (_currentPad.DPad.Left == ButtonState.Released && _previousPad.DPad.Left == ButtonState.Pressed) return 0;
        if (_currentPad.DPad.Down == ButtonState.Released && _previousPad.DPad.Down == ButtonState.Pressed) return 1;
        if (_currentPad.DPad.Up == ButtonState.Released && _previousPad.DPad.Up == ButtonState.Pressed) return 2;
        if (_currentPad.DPad.Right == ButtonState.Released && _previousPad.DPad.Right == ButtonState.Pressed) return 3;
        return -1;
    }

    private static Keys? ToMonoGameKey(GameKey key) => key switch
    {
        GameKey.A => Keys.A, GameKey.B => Keys.B, GameKey.C => Keys.C, GameKey.D => Keys.D,
        GameKey.E => Keys.E, GameKey.F => Keys.F, GameKey.G => Keys.G, GameKey.H => Keys.H,
        GameKey.I => Keys.I, GameKey.J => Keys.J, GameKey.K => Keys.K, GameKey.L => Keys.L,
        GameKey.M => Keys.M, GameKey.N => Keys.N, GameKey.O => Keys.O, GameKey.P => Keys.P,
        GameKey.Q => Keys.Q, GameKey.R => Keys.R, GameKey.S => Keys.S, GameKey.T => Keys.T,
        GameKey.U => Keys.U, GameKey.V => Keys.V, GameKey.W => Keys.W, GameKey.X => Keys.X,
        GameKey.Y => Keys.Y, GameKey.Z => Keys.Z,
        GameKey.Number0 => Keys.D0, GameKey.Number1 => Keys.D1, GameKey.Number2 => Keys.D2,
        GameKey.Number3 => Keys.D3, GameKey.Number4 => Keys.D4, GameKey.Number5 => Keys.D5,
        GameKey.Number6 => Keys.D6, GameKey.Number7 => Keys.D7, GameKey.Number8 => Keys.D8,
        GameKey.Number9 => Keys.D9,
        GameKey.Up => Keys.Up, GameKey.Down => Keys.Down, GameKey.Left => Keys.Left, GameKey.Right => Keys.Right,
        GameKey.Space => Keys.Space, GameKey.Enter => Keys.Enter, GameKey.Escape => Keys.Escape,
        GameKey.Backspace => Keys.Back, GameKey.Tab => Keys.Tab, GameKey.Shift => Keys.LeftShift,
        GameKey.Control => Keys.LeftControl, GameKey.Alt => Keys.LeftAlt,
        GameKey.Semicolon => Keys.OemSemicolon, GameKey.Apostrophe => Keys.OemQuotes,
        GameKey.Comma => Keys.OemComma, GameKey.Period => Keys.OemPeriod, GameKey.Slash => Keys.OemQuestion,
        GameKey.Backslash => Keys.OemBackslash, GameKey.BracketLeft => Keys.OemOpenBrackets,
        GameKey.BracketRight => Keys.OemCloseBrackets, GameKey.Minus => Keys.OemMinus, GameKey.Equal => Keys.OemPlus,
        GameKey.Grave => Keys.OemTilde, GameKey.Plus => Keys.Add,
        GameKey.F1 => Keys.F1, GameKey.F2 => Keys.F2, GameKey.F3 => Keys.F3, GameKey.F4 => Keys.F4,
        GameKey.F5 => Keys.F5, GameKey.F6 => Keys.F6, GameKey.F7 => Keys.F7, GameKey.F8 => Keys.F8,
        GameKey.F9 => Keys.F9, GameKey.F10 => Keys.F10, GameKey.F11 => Keys.F11, GameKey.F12 => Keys.F12,
        GameKey.Numpad0 => Keys.NumPad0, GameKey.Numpad1 => Keys.NumPad1, GameKey.Numpad2 => Keys.NumPad2,
        GameKey.Numpad3 => Keys.NumPad3, GameKey.Numpad4 => Keys.NumPad4, GameKey.Numpad5 => Keys.NumPad5,
        GameKey.Numpad6 => Keys.NumPad6, GameKey.Numpad7 => Keys.NumPad7, GameKey.Numpad8 => Keys.NumPad8,
        GameKey.Numpad9 => Keys.NumPad9,
        _ => null,
    };
}
