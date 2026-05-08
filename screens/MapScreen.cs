// ============================================================
// FILE: screens/MapScreen.cs — Procedural dungeon map screen
// ============================================================
using System;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DungeonCrawler.models;
using DungeonCrawler.logic;
using DungeonCrawler.utils;

namespace DungeonCrawler.screens;

public class MapScreen : IGameScreen
{
    private readonly GameContext         _ctx;
    private readonly Action<IGameScreen> _setScreen;

    // ── Grid layout (pixels) ────────────────────────────────
    private const int RoomW  = 100;   // room box width
    private const int RoomH  =  58;   // room box height
    private const int GapX   =  18;   // horizontal gap between boxes
    private const int GapY   =  18;   // vertical gap between boxes
    private const int HeaderH =  80;  // space reserved for header text

    // Derived — recomputed each frame from ScreenW/ScreenH
    private int OriginX => (Game1.ScreenW - (_ctx.CurrentMap.Width  * RoomW + (_ctx.CurrentMap.Width  - 1) * GapX)) / 2;
    private int OriginY => HeaderH;

    // ── Cursor state ─────────────────────────────────────────
    private int _cursorX;
    private int _cursorY;
    private readonly KeyboardWatcher _kb = new();

    // ── Feedback flash (e.g. can't enter that room) ──────────
    private string _flashMsg   = "";
    private float  _flashTimer = 0f;

    // ── Constant-string measurements (cached once per font) ──
    private static readonly (string Label, Color Color)[] LegendEntries =
    {
        ("BATTLE",  new Color(160, 160, 170)),
        ("ELITE",   new Color(255, 160,  30)),
        ("BOSS",    new Color(220,  50,  50)),
        ("CHEST",   new Color(255, 210,  40)),
        ("REST",    new Color(60,  200,  80)),
        ("EXIT",    new Color(80,  220, 220)),
    };
    private static float[]         _legendWidths;
    private static float           _legendTotalWidth;
    private static float           _youWidth;
    private static SpriteFontBase  _measureFont;

    public MapScreen(GameContext ctx, Action<IGameScreen> setScreen)
    {
        _ctx       = ctx;
        _setScreen = setScreen;

        // Cursor starts at the player's current position
        _cursorX = _ctx.CurrentMap.PlayerX;
        _cursorY = _ctx.CurrentMap.PlayerY;
    }

    // ── Update ───────────────────────────────────────────────
    public void Update(float dt)
    {
        if (_flashTimer > 0f) _flashTimer -= dt;

        _kb.Update();
        var map = _ctx.CurrentMap;

        // Cursor movement: bounded by the grid AND prevented from sliding onto
        // Hidden rooms, since those aren't drawn — the cursor would appear to
        // vanish into unexplored space.
        if (_kb.WasPressed(Keys.Up)    && _cursorY > 0              && map.GetRoom(_cursorX, _cursorY - 1).IsVisible) _cursorY--;
        if (_kb.WasPressed(Keys.Down)  && _cursorY < map.Height - 1 && map.GetRoom(_cursorX, _cursorY + 1).IsVisible) _cursorY++;
        if (_kb.WasPressed(Keys.Left)  && _cursorX > 0              && map.GetRoom(_cursorX - 1, _cursorY).IsVisible) _cursorX--;
        if (_kb.WasPressed(Keys.Right) && _cursorX < map.Width  - 1 && map.GetRoom(_cursorX + 1, _cursorY).IsVisible) _cursorX++;

        // Confirm — try to enter the room at cursor
        if (_kb.WasPressed(Keys.Enter) || _kb.WasPressed(Keys.Space))
            TryEnterRoom(_cursorX, _cursorY);

        // Stats shortcut
        if (_kb.WasPressed(Keys.Tab))
            _setScreen(new StatsScreen(_ctx, _setScreen, this));

        // Save & quit to title (Escape) — include map so the floor is restored on continue
        if (_kb.WasPressed(Keys.Escape))
        {
            SaveSystem.Save(_ctx.Player, _ctx.Depth, _ctx.CurrentMap);
            _setScreen(new TitleScreen(_ctx, _setScreen));
        }
    }

    private void TryEnterRoom(int tx, int ty)
    {
        var map  = _ctx.CurrentMap;
        var room = map.GetRoom(tx, ty);

        // If cursor is on the current room — nothing to do, except for the
        // Exit, which the player can re-enter to advance the floor (handy
        // right after a Boss → Exit conversion, since they're standing on it).
        if (tx == map.PlayerX && ty == map.PlayerY)
        {
            if (room.Type == RoomType.Exit)
            {
                _setScreen(new VictoryScreen(_ctx, _setScreen));
                return;
            }
            Flash("You are already here.");
            return;
        }

        // Must be adjacent
        if (!map.IsAdjacentToPlayer(tx, ty))
        {
            Flash("Move one room at a time.");
            return;
        }

        // Must be visible (accessible or visited)
        if (!map.CanEnter(tx, ty))
        {
            Flash("That room is hidden in darkness.");
            return;
        }

        bool wasVisited = room.State == RoomState.Visited;

        // Move player in the map (reveals adjacent rooms, marks old room Visited)
        map.MovePlayer(tx, ty);

        // Sync cursor to new player position
        _cursorX = tx;
        _cursorY = ty;

        // Skip the event when re-entering an already-cleared room.
        // Exit is the exception — it always fires so the player can use it
        // to descend, even after coming back from another room.
        if (wasVisited && room.Type != RoomType.Exit)
            return;

        // ── Trigger room-specific event ──────────────────────
        switch (room.Type)
        {
            case RoomType.Entrance:
                // No event; player can revisit entrance freely
                break;

            case RoomType.Exit:
                // Walking onto the Exit (a defeated Boss room) clears the floor.
                _setScreen(new VictoryScreen(_ctx, _setScreen));
                break;

            case RoomType.Battle:
                _setScreen(new BattleScreen(_ctx, _setScreen, RoomType.Battle,
                    afterBattle: () => new MapScreen(_ctx, _setScreen)));
                break;

            case RoomType.Elite:
                _setScreen(new BattleScreen(_ctx, _setScreen, RoomType.Elite,
                    afterBattle: () => new MapScreen(_ctx, _setScreen)));
                break;

            case RoomType.Boss:
                // Defeating the boss turns this room into the floor's Exit.
                _setScreen(new BattleScreen(_ctx, _setScreen, RoomType.Boss,
                    afterBattle: () =>
                    {
                        _ctx.CurrentMap.GetRoom(tx, ty).Type = RoomType.Exit;
                        return new MapScreen(_ctx, _setScreen);
                    }));
                break;

            case RoomType.Treasure:
                _setScreen(new LootScreen(_ctx, _setScreen,
                    afterLoot: () => new MapScreen(_ctx, _setScreen),
                    context:   LootContext.Treasure));
                break;

            case RoomType.Rest:
                _setScreen(new RestScreen(_ctx, _setScreen,
                    returnTo: new MapScreen(_ctx, _setScreen)));
                break;
        }
    }

    // ── Draw ─────────────────────────────────────────────────
    public void Draw(SpriteBatch sb)
    {
        // Refresh constant-string measurements once per frame (no-op after
        // the first call unless the font reference changes).
        EnsureCachedMeasurements(Game1.Resources.Font);

        DrawHeader(sb);
        DrawGrid(sb);
        DrawLegend(sb);
        DrawFlash(sb);
    }

    private void DrawHeader(SpriteBatch sb)
    {
        string title = $"DUNGEON MAP  —  Floor {_ctx.Depth.CurrentDepth}";
        DrawHelpers.CenterTextLarge(sb, title, 14, Color.Gold);

        string hp = $"HP: {_ctx.Player.Stats.Hp} / {_ctx.Player.EffectiveMaxHealth}";
        float hpPct = _ctx.Player.EffectiveMaxHealth > 0
            ? (float)_ctx.Player.Stats.Hp / _ctx.Player.EffectiveMaxHealth : 0f;
        var hpColor = hpPct > 0.5f  ? Color.LimeGreen
                    : hpPct > 0.25f ? Color.Yellow
                    : Color.Red;
        sb.DrawString(Game1.Resources.Font, hp,
            new Vector2(Game1.ScreenW - 160, 18), hpColor);

        string score = $"Score: {_ctx.Depth.Score}";
        sb.DrawString(Game1.Resources.Font, score,
            new Vector2(14, 18), Color.Gray);
    }

    private void DrawGrid(SpriteBatch sb)
    {
        var map = _ctx.CurrentMap;
        int ox  = OriginX;
        int oy  = OriginY;

        // ── Connection lines ────────────────────────────────
        for (int x = 0; x < map.Width; x++)
        {
            for (int y = 0; y < map.Height; y++)
            {
                var room = map.GetRoom(x, y);
                if (!room.IsVisible) continue;

                int rx = ox + x * (RoomW + GapX);
                int ry = oy + y * (RoomH + GapY);

                // Horizontal connector to the right
                if (x < map.Width - 1 && map.GetRoom(x + 1, y).IsVisible)
                    DrawHelpers.DrawRect(sb, rx + RoomW, ry + RoomH / 2 - 2, GapX, 4, new Color(80, 80, 80));

                // Vertical connector downward
                if (y < map.Height - 1 && map.GetRoom(x, y + 1).IsVisible)
                    DrawHelpers.DrawRect(sb, rx + RoomW / 2 - 2, ry + RoomH, 4, GapY, new Color(80, 80, 80));
            }
        }

        // ── Room boxes ──────────────────────────────────────
        for (int x = 0; x < map.Width; x++)
        {
            for (int y = 0; y < map.Height; y++)
            {
                var room = map.GetRoom(x, y);
                if (!room.IsVisible) continue;

                int rx = ox + x * (RoomW + GapX);
                int ry = oy + y * (RoomH + GapY);

                bool isCurrent  = (x == map.PlayerX && y == map.PlayerY);
                bool isCursor   = (x == _cursorX    && y == _cursorY);
                bool isAdjacent = map.IsAdjacentToPlayer(x, y) && room.State == RoomState.Accessible;

                // Box background
                Color bg;
                if (isCurrent)
                    bg = room.TypeColor * 0.55f;
                else if (room.State == RoomState.Visited)
                    bg = new Color(35, 35, 35);
                else if (isAdjacent)
                    bg = room.TypeColor * 0.25f;
                else
                    bg = new Color(25, 25, 25);

                DrawHelpers.DrawRect(sb, rx, ry, RoomW, RoomH, bg);

                // Cursor highlight (yellow border)
                if (isCursor)
                {
                    DrawHelpers.DrawRect(sb, rx - 3,       ry - 3,       RoomW + 6, 3,       Color.Yellow);
                    DrawHelpers.DrawRect(sb, rx - 3,       ry + RoomH,   RoomW + 6, 3,       Color.Yellow);
                    DrawHelpers.DrawRect(sb, rx - 3,       ry - 3,       3,         RoomH + 6, Color.Yellow);
                    DrawHelpers.DrawRect(sb, rx + RoomW,   ry - 3,       3,         RoomH + 6, Color.Yellow);
                }
                // Current room border (white)
                else if (isCurrent)
                {
                    DrawHelpers.DrawRect(sb, rx - 2,       ry - 2,       RoomW + 4, 2,       Color.White);
                    DrawHelpers.DrawRect(sb, rx - 2,       ry + RoomH,   RoomW + 4, 2,       Color.White);
                    DrawHelpers.DrawRect(sb, rx - 2,       ry - 2,       2,         RoomH + 4, Color.White);
                    DrawHelpers.DrawRect(sb, rx + RoomW,   ry - 2,       2,         RoomH + 4, Color.White);
                }
                // Accessible room — dim border in type colour
                else if (isAdjacent)
                {
                    DrawHelpers.DrawRect(sb, rx - 1,       ry - 1,       RoomW + 2, 1,       room.TypeColor * 0.7f);
                    DrawHelpers.DrawRect(sb, rx - 1,       ry + RoomH,   RoomW + 2, 1,       room.TypeColor * 0.7f);
                    DrawHelpers.DrawRect(sb, rx - 1,       ry - 1,       1,         RoomH + 2, room.TypeColor * 0.7f);
                    DrawHelpers.DrawRect(sb, rx + RoomW,   ry - 1,       1,         RoomH + 2, room.TypeColor * 0.7f);
                }

                // Room label
                Color labelColor;
                if (room.State == RoomState.Visited)
                    labelColor = new Color(60, 60, 60);
                else if (isCurrent || isAdjacent)
                    labelColor = room.TypeColor;
                else
                    labelColor = room.TypeColor * 0.5f;

                var font = Game1.Resources.Font;
                var textSize = font.MeasureString(room.Label);
                sb.DrawString(font, room.Label,
                    new Vector2(rx + (RoomW - textSize.X) / 2, ry + (RoomH - textSize.Y) / 2),
                    labelColor);

                // "YOU" marker on current room (width cached by EnsureCachedMeasurements)
                if (isCurrent)
                {
                    sb.DrawString(font, "YOU",
                        new Vector2(rx + (RoomW - _youWidth) / 2, ry + 4),
                        Color.White);
                }

                // Visited tick
                if (room.State == RoomState.Visited && room.Type != RoomType.Entrance)
                {
                    sb.DrawString(font, "✓",
                        new Vector2(rx + RoomW - 16, ry + 4),
                        new Color(50, 50, 50));
                }
            }
        }
    }

    private void DrawLegend(SpriteBatch sb)
    {
        int y = OriginY + _ctx.CurrentMap.Height * (RoomH + GapY) + 14;
        var font = Game1.Resources.Font;

        float lx = (Game1.ScreenW - _legendTotalWidth) / 2;
        for (int i = 0; i < LegendEntries.Length; i++)
        {
            sb.DrawString(font, LegendEntries[i].Label, new Vector2(lx, y), LegendEntries[i].Color);
            lx += _legendWidths[i] + 24;
        }

        // Controls hint
        DrawHelpers.CenterText(sb,
            "Arrows: move cursor   Enter: enter room   Tab: stats   Esc: save & quit",
            y + 26, new Color(70, 70, 70));
    }

    /// <summary>
    /// Measure constant strings (legend labels + the "YOU" marker) once per
    /// font. The font reference doesn't change during a session, so this
    /// only runs on the first draw.
    /// </summary>
    private static void EnsureCachedMeasurements(SpriteFontBase font)
    {
        if (_measureFont == font && _legendWidths != null) return;

        _measureFont = font;
        _legendWidths = new float[LegendEntries.Length];
        float total = 0;
        for (int i = 0; i < LegendEntries.Length; i++)
        {
            float w = font.MeasureString(LegendEntries[i].Label).X;
            _legendWidths[i] = w;
            total += w + 24;
        }
        _legendTotalWidth = total - 12; // remove trailing gap
        _youWidth = font.MeasureString("YOU").X;
    }

    private void DrawFlash(SpriteBatch sb)
    {
        if (_flashTimer <= 0f || string.IsNullOrEmpty(_flashMsg)) return;
        float alpha = Math.Min(_flashTimer / 0.8f, 1f);
        DrawHelpers.CenterText(sb, _flashMsg,
            OriginY + _ctx.CurrentMap.Height * (RoomH + GapY) / 2 - 14,
            Color.Yellow * alpha);
    }

    private void Flash(string msg)
    {
        _flashMsg   = msg;
        _flashTimer = 1.4f;
    }

}
