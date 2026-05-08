// ============================================================
// FILE: screens/LootScreen.cs — Post-battle loot selection
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

public class LootScreen : IGameScreen
{
    private readonly GameContext         _ctx;
    private readonly Action<IGameScreen> _setScreen;
    private readonly MenuSelector        _menu;
    private readonly Equipment[]         _lootChoices;
    private readonly Func<IGameScreen>?  _afterLoot;
    private readonly int                 _treasureGold;   // 0 unless this is a treasure pickup
    private readonly int                 _battleGold;     // 0 unless this came from a battle win

    // Layout constants — used to keep names/stats inside their boxes
    private const int CardW         = 260;
    private const int CardH         = 140;
    private const int CardSpacing   = 280;
    private const int CardPad       = 10;
    private const int CardTextWidth = CardW - CardPad * 2;     // = 240

    private const int EquippedW         = 300;
    private const int EquippedTextWidth = EquippedW - CardPad * 2;  // = 280

    // Wait for Enter/Space to release before transitioning to the after-loot
    // screen (typically MapScreen), so MapScreen doesn't see the same Enter
    // as a fresh "enter cursor's room" press.
    private bool _exitArmed;

    /// <summary>
    /// <paramref name="afterLoot"/> is called once the player picks or skips loot.
    /// Defaults to <see cref="VictoryScreen"/> when null (backwards-compatible).
    /// <paramref name="context"/> controls reroll behaviour and rarity boosts —
    /// see <see cref="LootContext"/>.
    /// </summary>
    public LootScreen(
        GameContext         ctx,
        Action<IGameScreen> setScreen,
        Func<IGameScreen>?  afterLoot = null,
        LootContext         context   = LootContext.Battle)
    {
        _ctx         = ctx;
        _setScreen   = setScreen;
        _afterLoot   = afterLoot;
        _lootChoices = LootFactory.GenerateChoices(
            ctx.Depth.CurrentDepth, ctx.Rng, ctx.Player.Equipment, context);
        // Guard MenuSelector against an empty loot list — Update will
        // immediately bail to GoToVictory before any indexing happens.
        _menu        = new MenuSelector(Math.Max(1, _lootChoices?.Length ?? 0));

        // Treasure rooms drop a pile of gold alongside the equipment roll.
        // Awarded once at construction so the amount is stable while the
        // screen is open, and so it doesn't double-award if Update redraws.
        if (context == LootContext.Treasure)
        {
            _treasureGold = ctx.Depth.AwardTreasureGold(ctx.Rng);
        }
        else
        {
            // For battle/elite/boss loot screens, the gold was already awarded
            // by DepthManager.OnVictory. Surface that amount here so the player
            // sees what the kill earned them, mirroring the treasure flow.
            _battleGold = ctx.Depth.LastGoldAwarded;
        }
    }

    public void Update(float dt)
    {
        // Defensive: if loot generation produced nothing, don't sit on a dead
        // screen — skip straight to whatever comes next.
        if (_lootChoices == null || _lootChoices.Length == 0)
        {
            GoToVictory();
            return;
        }

        // Advance the menu's keyboard snapshot first, then check extra keys.
        _menu.UpdateHorizontal();

        // Release-gate the Enter/Space exits so the after-loot screen
        // doesn't see the same key as a fresh press. 'S' (skip) doesn't
        // need gating — MapScreen has no S handler — but we route it
        // through the same path for consistency.
        if (_exitArmed)
        {
            if (!_menu.IsDown(Keys.Enter) && !_menu.IsDown(Keys.Space) && !_menu.IsDown(Keys.S))
                GoToVictory();
            return;
        }

        if (_menu.WasPressed(Keys.S))
        {
            _exitArmed = true;
            return;
        }

        if (_menu.Confirmed)
        {
            _ctx.Player.Equipment.Equip(_lootChoices[_menu.Index]);
            _exitArmed = true;
        }
    }

    public void Draw(SpriteBatch sb)
    {
        var font = Game1.Resources.Font;
        DrawHelpers.CenterText(sb, "Choose Your Loot!", 40, Color.Gold);
        DrawHelpers.CenterText(sb, "Left/Right to browse, Enter to equip, S to skip", 70, Color.Gray);

        // Gold message — treasure pile or battle drop. Either is awarded already;
        // this just surfaces the number so the player can see what they earned
        // before picking a piece of gear.
        if (_treasureGold > 0)
        {
            DrawHelpers.CenterText(sb,
                $"You also pocket {_treasureGold} gold!", 100,
                new Color(255, 215, 0));
        }
        else if (_battleGold > 0)
        {
            DrawHelpers.CenterText(sb,
                $"+{_battleGold} gold", 100,
                new Color(255, 215, 0));
        }

        if (_lootChoices == null || _lootChoices.Length == 0) return;

        var selected = _lootChoices[_menu.Index];
        var current = _ctx.Player.Equipment.Get(selected.EquipmentType);

        // Currently equipped panel — name truncated and stats wrapped so they
        // never escape the 300×120 box.
        int curX = 80, curY = 140;
        DrawHelpers.DrawRect(sb, curX - CardPad, curY - CardPad, EquippedW, 120, Color.Black * 0.8f);
        sb.DrawString(font, "Currently Equipped:", new Vector2(curX, curY), Color.Gray);
        sb.DrawString(font,
            DrawHelpers.TruncateToWidth(current.Name, font, EquippedTextWidth),
            new Vector2(curX, curY + 26), Color.White);

        var equippedLines = DrawHelpers.WrapCommaList(current.StatSummary(), font, EquippedTextWidth);
        for (int i = 0; i < equippedLines.Count; i++)
            sb.DrawString(font, equippedLines[i],
                new Vector2(curX, curY + 52 + i * 22), Color.Gray);

        // Loot choices
        for (int i = 0; i < _lootChoices.Length; i++)
        {
            DrawLootCard(sb, _lootChoices[i], i, 80 + i * CardSpacing, 290);
        }
    }

    private void DrawLootCard(SpriteBatch sb, Equipment item, int index, int x, int y)
    {
        var font = Game1.Resources.Font;
        bool isSelected = index == _menu.Index;

        Color bg = isSelected ? Color.Black * 0.9f : Color.Black * 0.6f;
        DrawHelpers.DrawRect(sb, x - CardPad, y - CardPad, CardW, CardH, bg);

        if (isSelected)
            DrawHelpers.DrawRect(sb, x - CardPad - 2, y - CardPad - 2,
                CardW + 4, CardH + 4, Color.Yellow * 0.3f);

        Color tierColor = DrawHelpers.GetRarityColor(item.Rarity);

        // Slot label
        sb.DrawString(font, $"[{item.EquipmentType}]", new Vector2(x, y), Color.Gray);

        // Item name — truncated with an ellipsis if it would run past the card edge.
        sb.DrawString(font,
            DrawHelpers.TruncateToWidth(item.Name, font, CardTextWidth),
            new Vector2(x, y + 26), tierColor);

        // Stat summary — wrap onto multiple lines so 4-stat items stay inside
        // their card. Comparison line follows directly below the stat lines so
        // the layout stays tight regardless of stat count.
        var statLines = DrawHelpers.WrapCommaList(item.StatSummary(), font, CardTextWidth);
        int statY = y + 52;
        const int StatLineH = 22;
        for (int i = 0; i < statLines.Count; i++)
            sb.DrawString(font, statLines[i],
                new Vector2(x, statY + i * StatLineH), Color.White);

        // Stat comparison sits below the (possibly wrapped) stat block, with a
        // floor at y+108 so cards with 0–1 stat lines still look balanced.
        int cmpY = System.Math.Max(y + 108,
            statY + statLines.Count * StatLineH + 4);

        var cur = _ctx.Player.Equipment.Get(item.EquipmentType);
        string cmp = CompareGear(item, cur);
        Color cmpColor = cmp.Contains('+') ? Color.LimeGreen
                       : cmp == "Same"     ? Color.Gray
                       : Color.Red;
        sb.DrawString(font, cmp, new Vector2(x, cmpY), cmpColor);
    }

    private static string CompareGear(Equipment newItem, Equipment current)
    {
        // Use Equipment.TotalStats so this matches LootFactory.IsUpgrade and
        // automatically picks up any new stat bonuses added to the model.
        int diff = newItem.TotalStats - current.TotalStats;
        if (diff > 0) return $"+{diff} total stats";
        if (diff < 0) return $"{diff} total stats";
        return "Same";
    }

    private void GoToVictory()
    {
        // If a custom after-loot destination was provided (e.g. MapScreen), use it.
        // Otherwise fall back to VictoryScreen for the classic flow.
        _setScreen(_afterLoot?.Invoke() ?? new VictoryScreen(_ctx, _setScreen));
    }
}