// ============================================================
// FILE: utils/KeyboardWatcher.cs — Shared one-shot key detection
// ============================================================
using Microsoft.Xna.Framework.Input;

namespace DungeonCrawler.utils;

/// <summary>
/// Tracks the previous and current keyboard state so callers can detect
/// up→down transitions (one-shot presses) without each screen re-implementing
/// the _prevKb dance.
///
/// Usage per frame:
///   1. Call Update() once at the top of your own Update method.
///   2. Read WasPressed(key) and IsDown(key) freely.
/// </summary>
public class KeyboardWatcher
{
    private KeyboardState _prevKb;
    private KeyboardState _curKb;

    public KeyboardWatcher()
    {
        // Seed both snapshots from the construction-time keyboard so the
        // first Update() doesn't see currently-held keys as "newly pressed".
        _prevKb = Keyboard.GetState();
        _curKb  = _prevKb;
    }

    /// <summary>Roll the snapshots forward: previous = current, current = now.</summary>
    public void Update()
    {
        _prevKb = _curKb;
        _curKb  = Keyboard.GetState();
    }

    /// <summary>True the frame <paramref name="key"/> transitions from up→down.</summary>
    public bool WasPressed(Keys key)
        => _curKb.IsKeyDown(key) && _prevKb.IsKeyUp(key);

    /// <summary>True while <paramref name="key"/> is currently held down.</summary>
    public bool IsDown(Keys key) => _curKb.IsKeyDown(key);
}
