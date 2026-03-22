// ============================================================
// FILE: screens/LootScreen.cs — Post-battle loot selection
// ============================================================
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DungeonCrawler.models;
using DungeonCrawler.logic;
using DungeonCrawler.utils;

using FontStashSharp;

namespace DungeonCrawler.screens;

public class LootScreen : IGameScreen
{
    private readonly GameContext _ctx;
    private readonly Action<IGameScreen> _setScreen;
    private readonly MenuSelector _menu;
    private readonly Equipment[] _lootChoices;

    public LootScreen(GameContext ctx, Action<IGameScreen> setScreen)
    {
        _ctx = ctx;
        _setScreen = setScreen;
        _lootChoices = LootFactory.GenerateChoices(ctx.Depth.CurrentDepth, ctx.Rng);
        _menu = new MenuSelector(_lootChoices.Length);
    }

    public void Update(float dt)
    {
        _menu.UpdateHorizontal();

        if (_menu.Confirmed)
        {
            _ctx.Player.Equipment.Equip(_lootChoices[_menu.Index]);
            GoToVictory();
        }

        // S to skip loot
        if (_menu.WasPressed(Keys.S))
            GoToVictory();
    }

    public void Draw(SpriteBatch sb)
    {
        var font = Game1.Resources.Font;
        DrawHelpers.CenterText(sb, "Choose Your Loot!", 40, Color.Gold);
        DrawHelpers.CenterText(sb, "Left/Right to browse, Enter to equip, S to skip", 70, Color.Gray);

        if (_lootChoices == null || _lootChoices.Length == 0) return;

        var selected = _lootChoices[_menu.Index];
        var current = _ctx.Player.Equipment.Get(selected.EquipmentType);

        // Current equipped item
        int curX = 80, curY = 140;
        DrawHelpers.DrawRect(sb, curX - 10, curY - 10, 300, 120, Color.Black * 0.8f);
        sb.DrawString(font, "Currently Equipped:", new Vector2(curX, curY), Color.Gray);
        sb.DrawString(font, current.Name, new Vector2(curX, curY + 26), Color.White);
        sb.DrawString(font, current.StatSummary(), new Vector2(curX, curY + 52), Color.Gray);

        // Loot choices
        for (int i = 0; i < _lootChoices.Length; i++)
        {
            DrawLootCard(sb, _lootChoices[i], i, 80 + i * 280, 290);
        }
    }

    private void DrawLootCard(SpriteBatch sb, Equipment item, int index, int x, int y)
    {
        var font = Game1.Resources.Font;
        bool isSelected = index == _menu.Index;

        Color bg = isSelected ? Color.Black * 0.9f : Color.Black * 0.6f;
        DrawHelpers.DrawRect(sb, x - 10, y - 10, 260, 140, bg);

        if (isSelected)
            DrawHelpers.DrawRect(sb, x - 12, y - 12, 264, 144, Color.Yellow * 0.3f);

        Color tierColor = DrawHelpers.GetRarityColor(item.Rarity);

        sb.DrawString(font, $"[{item.EquipmentType}]", new Vector2(x, y), Color.Gray);
        sb.DrawString(font, item.Name, new Vector2(x, y + 26), tierColor);
        sb.DrawString(font, item.StatSummary(), new Vector2(x, y + 52), Color.White);

        // Stat comparison
        var cur = _ctx.Player.Equipment.Get(item.EquipmentType);
        string cmp = CompareGear(item, cur);
        Color cmpColor = cmp.Contains('+') ? Color.LimeGreen
                       : cmp == "Same"     ? Color.Gray
                       : Color.Red;
        sb.DrawString(font, cmp, new Vector2(x, y + 82), cmpColor);
    }

    private static string CompareGear(Equipment newItem, Equipment current)
    {
        int diff = (newItem.AttackBonus - current.AttackBonus)
                 + (newItem.DefenseBonus - current.DefenseBonus)
                 + (newItem.SpeedBoost - current.SpeedBoost)
                 + (newItem.MagicBonus - current.MagicBonus)
                 + (newItem.HealthBonus - current.HealthBonus);

        if (diff > 0) return $"+{diff} total stats";
        if (diff < 0) return $"{diff} total stats";
        return "Same";
    }

    private void GoToVictory()
    {
        _setScreen(new VictoryScreen(_ctx, _setScreen));
    }
}