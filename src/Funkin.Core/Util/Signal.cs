namespace Funkin.Util;

/// <summary>
/// Port of HaxeFlixel's FlxSignal: a tiny implementation of the observer pattern that supports
/// multiple listeners. Used for Conductor beat/step/measure dispatch and gameplay events.
/// </summary>
public class Signal<T>
{
    private readonly List<Action<T>> _listeners = new();
    private readonly List<Action<T>> _pending = new();
    private bool _dispatching;

    public int ListenerCount => _listeners.Count;

    /// <summary>Register a listener. Inline delegates can be unregistered with <see cref="Remove"/>.</summary>
    public void Add(Action<T> listener)
    {
        if (!_listeners.Contains(listener)) _listeners.Add(listener);
    }

    public void Remove(Action<T> listener) => _listeners.Remove(listener);

    public void RemoveAll() => _listeners.Clear();

    public bool Has(Action<T> listener) => _listeners.Contains(listener);

    public void Dispatch(T value)
    {
        // Copy so listeners can remove themselves during dispatch without blowing up the enumeration.
        _dispatching = true;
        foreach (var listener in _listeners.ToArray()) listener(value);
        _dispatching = false;
        foreach (var listener in _pending) _listeners.Add(listener);
        _pending.Clear();
    }

    /// <summary>Adds a listener which will be registered at the end of the current dispatch, if any.</summary>
    public void AddDeferred(Action<T> listener)
    {
        if (_dispatching) _pending.Add(listener);
        else Add(listener);
    }
}

/// <summary>A signal with no payload.</summary>
public sealed class Signal : Signal<object?>
{
    public void Dispatch() => Dispatch(null);

    public void Add(Action listener) => Add(_ => listener());

    public void Remove(Action listener) => Remove(_ => listener());
}
