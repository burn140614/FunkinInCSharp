namespace Funkin.Input;

/// <summary>A logical game action which can be bound to keys or buttons.</summary>
public enum GameAction
{
    NoteLeft,
    NoteDown,
    NoteUp,
    NoteRight,
    NoteLeftAlt,
    NoteDownAlt,
    NoteUpAlt,
    NoteRightAlt,
    Accept,
    Back,
    Pause,
    Reset,
    ToggleBotplay,
    Screenshot,
    Fullscreen,
    Up,
    Down,
    Left,
    Right,
}

/// <summary>Platform-agnostic key identifiers, mirroring openfl.ui.Keyboard values we care about.</summary>
public enum GameKey
{
    None = 0,
    A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
    Number0, Number1, Number2, Number3, Number4, Number5, Number6, Number7, Number8, Number9,
    Up, Down, Left, Right,
    Space, Enter, Escape, Backspace, Tab, Shift, Control, Alt,
    Semicolon, Apostrophe, Comma, Period, Slash, Backslash, BracketLeft, BracketRight, Minus, Equal, Grave,
    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
    Plus, Numpad0, Numpad1, Numpad2, Numpad3, Numpad4, Numpad5, Numpad6, Numpad7, Numpad8, Numpad9,
}

/// <summary>
/// Port of the parts of funkin.input.Controls the game needs: the key bindings for each action,
/// with two slots per action so players can use e.g. both WASD and the arrow keys.
/// </summary>
public static class Controls
{
    private static readonly Dictionary<GameAction, GameKey[]> Bindings = new()
    {
        [GameAction.NoteLeft] = new[] { GameKey.A, GameKey.Left },
        [GameAction.NoteDown] = new[] { GameKey.S, GameKey.Down },
        [GameAction.NoteUp] = new[] { GameKey.W, GameKey.Up },
        [GameAction.NoteRight] = new[] { GameKey.D, GameKey.Right },
        [GameAction.NoteLeftAlt] = new[] { GameKey.Left },
        [GameAction.NoteDownAlt] = new[] { GameKey.Down },
        [GameAction.NoteUpAlt] = new[] { GameKey.Up },
        [GameAction.NoteRightAlt] = new[] { GameKey.Right },
        [GameAction.Accept] = new[] { GameKey.Enter, GameKey.Space },
        [GameAction.Back] = new[] { GameKey.Escape, GameKey.Backspace },
        [GameAction.Pause] = new[] { GameKey.Enter, GameKey.P },
        [GameAction.Reset] = new[] { GameKey.R },
        [GameAction.ToggleBotplay] = new[] { GameKey.B },
        [GameAction.Screenshot] = new[] { GameKey.F2 },
        [GameAction.Fullscreen] = new[] { GameKey.F11 },
        [GameAction.Up] = new[] { GameKey.W, GameKey.Up },
        [GameAction.Down] = new[] { GameKey.S, GameKey.Down },
        [GameAction.Left] = new[] { GameKey.A, GameKey.Left },
        [GameAction.Right] = new[] { GameKey.D, GameKey.Right },
    };

    public static IReadOnlyList<GameKey> GetKeys(GameAction action) => Bindings.TryGetValue(action, out var keys) ? keys : Array.Empty<GameKey>();

    public static void Bind(GameAction action, params GameKey[] keys) => Bindings[action] = keys;

    public static bool IsBoundTo(GameAction action, GameKey key) => GetKeys(action).Contains(key);

    /// <summary>Returns the note lane actions in order: left, down, up, right, and their alternates.</summary>
    public static readonly GameAction[] NoteActions =
    {
        GameAction.NoteLeft, GameAction.NoteDown, GameAction.NoteUp, GameAction.NoteRight,
    };

    public static readonly GameAction[] NoteActionsAlt =
    {
        GameAction.NoteLeftAlt, GameAction.NoteDownAlt, GameAction.NoteUpAlt, GameAction.NoteRightAlt,
    };

    /// <summary>Maps a key press to a note lane index, or -1 when the key is not a lane key.</summary>
    public static int GetNoteLaneFor(GameKey key)
    {
        for (int lane = 0; lane < NoteActions.Length; lane++)
        {
            if (IsBoundTo(NoteActions[lane], key)) return lane;
        }

        return -1;
    }
}
