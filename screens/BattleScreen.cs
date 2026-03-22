// ============================================================
// FILE: screens/BattleScreen.cs — Battle gameplay screen
// ============================================================
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DungeonCrawler.models;
using DungeonCrawler.logic;
using DungeonCrawler.utils;
using FontStashSharp;

namespace DungeonCrawler.screens;

public class BattleScreen : IGameScreen
{
    private readonly GameContext _ctx;
    private readonly Action<IGameScreen> _setScreen;
    private readonly BattleSystem _battle;
    private readonly MenuSelector _menu;

    private static readonly BattleActionType[] Actions =
    {
        BattleActionType.Attack, BattleActionType.Magic,
        BattleActionType.Defend, BattleActionType.Heal
    };

    public BattleScreen(GameContext ctx, Action<IGameScreen> setScreen)
    {
        _ctx = ctx;
        _setScreen = setScreen;
        _menu = new MenuSelector(Actions.Length);

        // Create enemy and position fighters
        var res = Game1.Resources;
        var enemy = EnemyFactory.Create(_ctx.Depth.CurrentDepth, res.EnemySprites, _ctx.Rng);

        _ctx.Player.Position = new Vector2(100, Game1.ScreenH - 250);
        _ctx.Player.Scale = 2.0f;
        enemy.Position = new Vector2(Game1.ScreenW - 250, 80);
        enemy.Scale = 1.2f;
        enemy.FlipEffect = SpriteEffects.FlipHorizontally;

        _battle = new BattleSystem(_ctx.Player, enemy, _ctx.Depth.CurrentDepth, _ctx.Rng);
    }

    public void Update(float dt)
    {
        _battle.Update(dt);

        if (_battle.State == BattleTurnState.PlayerChoosing)
        {
            _menu.Update();
            if (_menu.Confirmed)
                _battle.SubmitPlayerAction(Actions[_menu.Index]);
        }

        // Transition on battle end
        if (_battle.State == BattleTurnState.BattleWon)
        {
            _ctx.Depth.OnVictory();
            _setScreen(new LootScreen(_ctx, _setScreen));
        }
        else if (_battle.State == BattleTurnState.BattleLost)
        {
            SaveSystem.Delete();
            _setScreen(new GameOverScreen(_ctx, _setScreen));
        }
    }

    public void Draw(SpriteBatch sb)
    {
        var res = Game1.Resources;

        // Background
        sb.Draw(res.BattleBackground, Vector2.Zero, Color.White);

        // Fighters
        _battle.Enemy.Draw(sb);
        _battle.Player.Draw(sb);

        // HP Bars
        DrawHelpers.DrawHpBar(sb, 20, 20, 200, 20, _battle.Player);
        DrawHelpers.DrawHpBar(sb, Game1.ScreenW - 220, 20, 200, 20, _battle.Enemy);

        // Action menu
        if (_battle.State == BattleTurnState.PlayerChoosing)
            DrawActionMenu(sb);

        // Battle log
        DrawBattleLog(sb);
    }

    private void DrawActionMenu(SpriteBatch sb)
    {
        int x = 40, y = Game1.ScreenH - 140;
        DrawHelpers.DrawRect(sb, x - 10, y - 10, 200, 130, Color.Black * 0.8f);

        for (int i = 0; i < Actions.Length; i++)
        {
            bool selected = i == _menu.Index;
            string prefix = selected ? "> " : "  ";
            Color c = selected ? Color.Yellow : Color.White;
            sb.DrawString(Game1.Resources.Font, prefix + Actions[i],
                new Vector2(x, y + i * 28), c);
        }
    }

    private void DrawBattleLog(SpriteBatch sb)
    {
        int x = Game1.ScreenW - 400, y = Game1.ScreenH - 140;
        DrawHelpers.DrawRect(sb, x - 10, y - 10, 390, 130, Color.Black * 0.8f);

        var log = _battle.Log;
        int start = Math.Max(0, log.Count - 4);
        for (int i = start; i < log.Count; i++)
        {
            sb.DrawString(Game1.Resources.Font, log[i],
                new Vector2(x, y + (i - start) * 28), Color.LightGray);
        }
    }
}