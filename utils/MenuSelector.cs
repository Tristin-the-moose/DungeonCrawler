// ============================================================
// FILE: utils/MenuSelector.cs — Reusable menu navigation
// ============================================================
using Microsoft.Xna.Framework.Input;

namespace DungeonCrawler.utils;

/// <summary>
/// Handles keyboard-driven menu navigation (Up/Down/Enter).
/// Replaces the copy-pasted navigation logic that was in every screen.
///
/// Usage per frame:
///   1. Call Update() (or UpdateHorizontal()) once — this advances the
///      keyboard snapshot.
///   2. Read Confirmed and call WasPressed(key) for any extra keys you
///      care about. Both are valid for the rest of the frame.
/// </summary>
public class MenuSelector
{
    public int Index { get; set; }
    public bool Confirmed { get; private set; }
    public int Count { get; set; }

    // Two snapshots: previous frame's keyboard and this frame's. WasPressed
    // detects an up→down transition between them.
    private KeyboardState _prevKb;
    private KeyboardState _curKb;
    private readonly Keys _upKey;
    private readonly Keys _downKey;

    public MenuSelector(int count, Keys upKey = Keys.Up, Keys downKey = Keys.Down)
    {
        Count = count;
        _upKey = upKey;
        _downKey = downKey;

        // Seed both snapshots from the construction-time keyboard so the
        // first Update() doesn't see currently-held keys as "newly pressed".
        _prevKb = Keyboard.GetState();
        _curKb  = _prevKb;
    }

    /// <summary>
    /// Call once per frame. Sets Confirmed = true on Enter/Space press.
    /// </summary>
    public void Update()
    {
        AdvanceKeyboard();
        Confirmed = false;

        // Guard navigation when there are no items — avoids "% 0" crashes
        // for screens that briefly initialise the selector with Count == 0.
        if (Count > 0)
        {
            if (WasPressed(_upKey))
                Index = (Index - 1 + Count) % Count;

            if (WasPressed(_downKey))
                Index = (Index + 1) % Count;
        }

        if (WasPressed(Keys.Enter) || WasPressed(Keys.Space))
            Confirmed = true;
    }

    /// <summary>
    /// Overload for Left/Right navigation (used in loot screen).
    /// </summary>
    public void UpdateHorizontal()
    {
        AdvanceKeyboard();
        Confirmed = false;

        if (Count > 0)
        {
            if (WasPressed(Keys.Left))
                Index = (Index - 1 + Count) % Count;

            if (WasPressed(Keys.Right))
                Index = (Index + 1) % Count;
        }

        if (WasPressed(Keys.Enter) || WasPressed(Keys.Space))
            Confirmed = true;
    }

    /// <summary>
    /// True the frame <paramref name="key"/> transitions from up→down.
    /// Call after Update()/UpdateHorizontal() so the snapshot is current.
    /// </summary>
    public bool WasPressed(Keys key)
        => _curKb.IsKeyDown(key) && _prevKb.IsKeyUp(key);

    /// <summary>Roll the snapshots forward: previous = current, current = now.</summary>
    private void AdvanceKeyboard()
    {
        _prevKb = _curKb;
        _curKb  = Keyboard.GetState();
    }
}
