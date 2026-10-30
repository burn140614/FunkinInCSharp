using Microsoft.Xna.Framework.Graphics;

namespace Funkin.Flixel;

/// <summary>
/// Port of flixel.FlxState: a screen of the game, owning a list of drawable members.
/// Substates are drawn on top of their parent and receive updates first (used by the pause menu).
/// </summary>
public abstract class FlxState
{
    public readonly List<FlxObject> Members = new();
    public readonly List<FlxState> SubStates = new();

    /// <summary>Set when this state was opened as a substate (it does not clear the screen).</summary>
    public bool IsSubState { get; internal set; }

    public FlxCamera Camera { get; } = new(FunkinGame.GameWidth, FunkinGame.GameHeight);

    public bool Active { get; set; } = true;

    public bool IsDestroyed { get; private set; }

    /// <summary>True when the parent state should not update/draw while this substate is open.</summary>
    public bool BlocksParentUpdate { get; set; }

    public virtual void Create()
    {
    }

    public void Add(FlxObject member)
    {
        if (!Members.Contains(member)) Members.Add(member);
    }

    public void AddAll(IEnumerable<FlxObject> members)
    {
        foreach (var member in members) Add(member);
    }

    public void Remove(FlxObject member) => Members.Remove(member);

    public void Insert(int index, FlxObject member) => Members.Insert(index, member);

    public T? GetFirst<T>() where T : FlxObject => Members.OfType<T>().FirstOrDefault();

    public IEnumerable<T> GetAll<T>() where T : FlxObject => Members.OfType<T>();

    public void AddSubState(FlxState substate)
    {
        substate.IsSubState = true;
        SubStates.Add(substate);
        substate.Create();
    }

    public void RemoveSubState(FlxState substate)
    {
        SubStates.Remove(substate);
        substate.Destroy();
    }

    public virtual void Update(float elapsed)
    {
        foreach (var substate in SubStates.ToArray()) substate.Update(elapsed);

        if (BlocksParentUpdate && SubStates.Count > 0) return;
        if (!Active) return;

        foreach (var member in Members.ToArray())
        {
            if (member.Active && !member.IsDestroyed) member.Update(elapsed);
        }

        Members.RemoveAll(m => m.IsDestroyed);
        Camera.Update(elapsed);
    }

    /// <summary>Draws the state. The sprite batch is already begun with the default sampler.</summary>
    public virtual void Draw(SpriteBatch batch)
    {
        foreach (var member in Members)
        {
            if (!member.Visible || member.IsDestroyed) continue;
            if (member is FlxSprite sprite) sprite.Draw(batch, Camera);
        }
    }

    public virtual void DrawSubStates(SpriteBatch batch)
    {
        foreach (var substate in SubStates.ToArray()) substate.Draw(batch);
    }

    /// <summary>Called when the state is switched away from or destroyed.</summary>
    public virtual void Destroy()
    {
        IsDestroyed = true;
        foreach (var member in Members) member.Destroy();
        Members.Clear();

        foreach (var substate in SubStates.ToArray()) substate.Destroy();
        SubStates.Clear();
    }

    /// <summary>Handles a window resize, keeping the camera in sync.</summary>
    public virtual void OnResize(int width, int height)
    {
        Camera.Width = width;
        Camera.Height = height;
    }
}
