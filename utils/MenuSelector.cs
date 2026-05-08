// ============================================================
// FILE: utils/MenuSelector.cs — Reusable menu navigation
// ============================================================
using Microsoft.Xna.Framework.Input;

namespace DungeonCrawler.utils;

/// <summary>
/// Handles keyboard-driven menu navigation (Up/Down/Enter).
/// Replaces the copy-pasted navigation logic that was in every screen.
/// </summary>
public class MenuSelector
{
    public int Index { get; set; }
    public bool Confirmed { get; private set; }
    public int Count { get; set; }

    private KeyboardState _prevKb;
    private readonly Keys _upKey;
    private readonly Keys _downKey;

    public MenuSelector(int count, Keys upKey = Keys.Up, Keys downKey = Keys.Down)
    {
        Count = count;
        _upKey = upKey;
        _downKey = downKey;

        _prevKb = Keyboard.GetState();
    }

    /// <summary>
    /// Call once per frame. Sets Confirmed = true on Enter/Space press.
    /// </summary>
    public void Update()
    {
        var kb = Keyboard.GetState();
        Confirmed = false;

        // Guard navigation when there are no items — avoids "% 0" crashes
        // for screens that briefly initialise the selector with Count == 0.
        if (Count > 0)
        {
            if (WasPressed(kb, _upKey))
                Index = (Index - 1 + Count) % Count;

            if (WasPressed(kb, _downKey))
                Index = (Index + 1) % Count;
        }

        if (WasPressed(kb, Keys.Enter) || WasPressed(kb, Keys.Space))
            Confirmed = true;

        _prevKb = kb;
    }

    /// <summary>
    /// Overload for Left/Right navigation (used in loot screen).
    /// </summary>
    public void UpdateHorizontal()
    {
        var kb = Keyboard.GetState();
        Confirmed = false;

        if (Count > 0)
        {
            if (WasPressed(kb, Keys.Left))
                Index = (Index - 1 + Count) % Count;

            if (WasPressed(kb, Keys.Right))
                Index = (Index + 1) % Count;
        }

        if (WasPressed(kb, Keys.Enter) || WasPressed(kb, Keys.Space))
            Confirmed = true;

        _prevKb = kb;
    }

    public bool WasPressed(Keys key)
    {
        var kb = Keyboard.GetState();
        return kb.IsKeyDown(key) && _prevKb.IsKeyUp(key);
    }

    private bool WasPressed(KeyboardState kb, Keys key)
        => kb.IsKeyDown(key) && _prevKb.IsKeyUp(key);
}