// ============================================================
// FILE: screens/StatsScreen.cs — Character stats & equipment view
// ============================================================
using System;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DungeonCrawler.models;
using DungeonCrawler.utils;

namespace DungeonCrawler.screens;

public class StatsScreen : IGameScreen
{
    private readonly GameContext _ctx;
    private readonly Action<IGameScreen> _setScreen;
    private readonly IGameScreen _returnScreen;
    private readonly MenuSelector _menu = new(1); // just listening for Enter/Escape

    public StatsScreen(GameContext ctx, Action<IGameScreen> setScreen, IGameScreen returnScreen)
    {
        _ctx = ctx;
        _setScreen = setScreen;
        _returnScreen = returnScreen;
    }

    public void Update(float dt)
    {
        _menu.Update();

        if (_menu.Confirmed || _menu.WasPressed(Microsoft.Xna.Framework.Input.Keys.Escape))
            _setScreen(_returnScreen);
    }

    public void Draw(SpriteBatch sb)
    {
        var font = Game1.Resources.Font;
        var player = _ctx.Player;
        var stats = player.Stats;
        var gear = player.Equipment;

        // ── Title ──
        DrawHelpers.CenterTextLarge(sb, $"{stats.Name} — Depth {_ctx.Depth.CurrentDepth}", 20, Color.Gold);

        // ── Stats Panel (left side) ──
        int sx = 60, sy = 80;
        DrawHelpers.DrawRect(sb, sx - 10, sy - 10, 320, 280, Color.Black * 0.8f);

        sb.DrawString(font, "BASE STATS", new Vector2(sx, sy), Color.Yellow);
        sy += 30;

        DrawStatLine(sb, sx, ref sy, "HP",      stats.Hp, stats.MaxHp, player.EffectiveMaxHealth, Color.LimeGreen);
        DrawStatLine(sb, sx, ref sy, "Attack",  stats.Attack,  player.EffectiveAttack,  Color.OrangeRed);
        DrawStatLine(sb, sx, ref sy, "Defense", stats.Defense, player.EffectiveDefense, Color.CornflowerBlue);
        DrawStatLine(sb, sx, ref sy, "Magic",   stats.Magic,   player.EffectiveMagic,   Color.MediumPurple);
        DrawStatLine(sb, sx, ref sy, "Speed",   stats.Speed,   player.EffectiveSpeed,   Color.Yellow);

        sy += 10;
        string atkType = player.UsesMagicAttack ? "Magic" : "Physical";
        sb.DrawString(font, $"Attack Type: {atkType}", new Vector2(sx, sy), Color.White);
        sy += 26;
        sb.DrawString(font, $"Protection: {player.EffectiveProtection}", new Vector2(sx, sy), Color.Gray);
        sy += 26;
        sb.DrawString(font, $"Kills: {_ctx.Depth.TotalKills}   Score: {_ctx.Depth.Score}", new Vector2(sx, sy), Color.White);

        // ── Equipment Panel (right side) ──
        int ex = 440, ey = 80;
        DrawHelpers.DrawRect(sb, ex - 10, ey - 10, 470, 360, Color.Black * 0.8f);

        sb.DrawString(font, "EQUIPMENT", new Vector2(ex, ey), Color.Yellow);
        ey += 30;

        if (gear != null)
        {
            DrawEquipSlot(sb, ex, ref ey, "Head",     gear.HeadPiece);
            DrawEquipSlot(sb, ex, ref ey, "Chest",    gear.ChestPiece);
            DrawEquipSlot(sb, ex, ref ey, "Legs",     gear.Leggings);
            DrawEquipSlot(sb, ex, ref ey, "Boots",    gear.Booties);
            DrawEquipSlot(sb, ex, ref ey, "Weapon",   gear.Weapon);
            DrawEquipSlot(sb, ex, ref ey, "Ring",     gear.Ring);
            DrawEquipSlot(sb, ex, ref ey, "Necklace", gear.Necklace);
        }

        // ── Footer ──
        DrawHelpers.CenterText(sb, "Press ENTER or ESC to go back", Game1.ScreenH - 40, Color.Gray);
    }

    /// <summary>
    /// Draws a stat line showing base value and effective value with gear bonus.
    /// e.g. "Attack: 12 → 18 (+6)"
    /// </summary>
    private void DrawStatLine(SpriteBatch sb, int x, ref int y,
                               string label, int baseVal, int effectiveVal, Color color)
    {
        var font = Game1.Resources.Font;
        int bonus = effectiveVal - baseVal;

        if (bonus > 0)
            sb.DrawString(font, $"{label}: {baseVal} -> {effectiveVal} (+{bonus})",
                new Vector2(x, y), color);
        else
            sb.DrawString(font, $"{label}: {baseVal}",
                new Vector2(x, y), color);

        y += 26;
    }

    /// <summary>
    /// Overload for HP which shows current/max and effective max.
    /// e.g. "HP: 75/100 → 120 (+20)"
    /// </summary>
    private void DrawStatLine(SpriteBatch sb, int x, ref int y,
                               string label, int current, int max, int effectiveMax, Color color)
    {
        var font = Game1.Resources.Font;
        int bonus = effectiveMax - max;

        if (bonus > 0)
            sb.DrawString(font, $"{label}: {current}/{max} -> {effectiveMax} (+{bonus})",
                new Vector2(x, y), color);
        else
            sb.DrawString(font, $"{label}: {current}/{max}",
                new Vector2(x, y), color);

        y += 26;
    }

    /// <summary>
    /// Draws an equipment slot with name, rarity color, and stat summary.
    /// </summary>
    private void DrawEquipSlot(SpriteBatch sb, int x, ref int y, string slotLabel, Equipment item)
    {
        var font = Game1.Resources.Font;

        if (item == null)
        {
            sb.DrawString(font, $"{slotLabel}: (empty)", new Vector2(x, y), Color.DarkGray);
            y += 40;
            return;
        }

        Color nameColor = DrawHelpers.GetRarityColor(item.Rarity);
        string weaponTag = item.Weapon.HasValue ? $" [{item.Weapon.Value}]" : "";
        sb.DrawString(font, $"{slotLabel}: {item.Name}{weaponTag}", new Vector2(x, y), nameColor);
        y += 22;

        string summary = item.StatSummary();
        sb.DrawString(font, $"  {summary}", new Vector2(x, y), Color.Gray);
        y += 24;
    }
}