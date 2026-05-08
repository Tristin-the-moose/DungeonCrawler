// ============================================================
// FILE: screens/RestScreen.cs — Safe room: recover HP
// ============================================================
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DungeonCrawler.utils;

namespace DungeonCrawler.screens;

public class RestScreen : IGameScreen
{
    private readonly GameContext          _ctx;
    private readonly Action<IGameScreen>  _setScreen;
    private readonly IGameScreen          _returnTo;
    private readonly int                  _healAmount;
    private readonly MenuSelector         _input = new(1);

    public RestScreen(GameContext ctx, Action<IGameScreen> setScreen, IGameScreen returnTo)
    {
        _ctx       = ctx;
        _setScreen = setScreen;
        _returnTo  = returnTo;

        // Heal a percentage of effective max HP (configured via RestHealPercent)
        float pct   = GameConfig.Instance.RestHealPercent;
        _healAmount = Math.Max(1, (int)(_ctx.Player.EffectiveMaxHealth * pct));
        _ctx.Player.Heal(_healAmount);
    }

    public void Update(float dt)
    {
        _input.Update();
        if (_input.Confirmed)
            _setScreen(_returnTo);
    }

    public void Draw(SpriteBatch sb)
    {
        DrawHelpers.CenterTextLarge(sb, "Rest", 120, Color.LimeGreen);

        DrawHelpers.CenterText(sb, "You find a quiet alcove and catch your breath...", 200, Color.LightGray);
        DrawHelpers.CenterText(sb, $"+{_healAmount} HP restored", 240, new Color(60, 220, 80));

        string hp = $"HP: {_ctx.Player.Stats.Hp} / {_ctx.Player.EffectiveMaxHealth}";
        DrawHelpers.CenterText(sb, hp, 285, Color.White);

        DrawHelpers.DrawHpBar(sb,
            Game1.ScreenW / 2 - 150, 315,
            300, 20, _ctx.Player);

        DrawHelpers.CenterText(sb, "Press Enter to return to the map", 380, Color.Gray);
    }
}
