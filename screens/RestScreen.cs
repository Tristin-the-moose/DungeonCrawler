// ============================================================
// FILE: screens/RestScreen.cs — Safe room: recover HP
// ============================================================
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DungeonCrawler.utils;

namespace DungeonCrawler.screens;

public class RestScreen : IGameScreen
{
    private readonly GameContext          _ctx;
    private readonly Action<IGameScreen>  _setScreen;
    private readonly IGameScreen          _returnTo;
    private readonly int                  _healAmount;
    private readonly MenuSelector         _input = new(1);
    private bool                          _healed;

    // Wait for the confirm key to release before returning to the map, so the
    // map screen doesn't see the same Enter as a fresh "enter cursor's room".
    private bool _exitArmed;

    public RestScreen(GameContext ctx, Action<IGameScreen> setScreen, IGameScreen returnTo)
    {
        _ctx       = ctx;
        _setScreen = setScreen;
        _returnTo  = returnTo;

        // Pre-compute the heal amount so the on-screen label always matches
        // what gets applied. The actual Heal() runs on first Update so the
        // constructor stays side-effect-free.
        float pct   = GameConfig.Instance.RestHealPercent;
        _healAmount = Math.Max(1, (int)(_ctx.Player.EffectiveMaxHealth * pct));
    }

    public void Update(float dt)
    {
        if (!_healed)
        {
            _ctx.Player.Heal(_healAmount);
            _healed = true;
        }

        _input.Update();

        if (_exitArmed)
        {
            if (!_input.IsDown(Keys.Enter) && !_input.IsDown(Keys.Space))
                _setScreen(_returnTo);
            return;
        }

        if (_input.Confirmed)
            _exitArmed = true;
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
