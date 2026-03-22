// ============================================================
// FILE: screens/GameOverScreen.cs — Death screen
// ============================================================
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DungeonCrawler.utils;

namespace DungeonCrawler.screens;

public class GameOverScreen : IGameScreen
{
    private readonly GameContext _ctx;
    private readonly Action<IGameScreen> _setScreen;
    private readonly MenuSelector _menu = new(1); // just listening for Enter

    public GameOverScreen(GameContext ctx, Action<IGameScreen> setScreen)
    {
        _ctx = ctx;
        _setScreen = setScreen;
    }

    public void Update(float dt)
    {
        _menu.Update();
        if (_menu.Confirmed)
        {
            _ctx.Reset();
            _setScreen(new TitleScreen(_ctx, _setScreen));
        }
    }

    public void Draw(SpriteBatch sb)
    {
        DrawHelpers.CenterText(sb, "YOU DIED", 180, Color.Red);
        DrawHelpers.CenterText(sb,
            $"Reached depth {_ctx.Depth.CurrentDepth}  |  Score: {_ctx.Depth.Score}",
            240, Color.Gray);
        DrawHelpers.CenterText(sb, "Press ENTER to restart", 340, Color.White);
    }
}