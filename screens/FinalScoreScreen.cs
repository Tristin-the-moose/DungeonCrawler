// ============================================================
// FILE: screens/FinalScoreScreen.cs — Cash out / end screen
// ============================================================
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DungeonCrawler.utils;

namespace DungeonCrawler.screens;

public class FinalScoreScreen : IGameScreen
{
    private readonly GameContext _ctx;
    private readonly Action<IGameScreen> _setScreen;
    private readonly MenuSelector _menu = new(1);

    public FinalScoreScreen(GameContext ctx, Action<IGameScreen> setScreen)
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
        DrawHelpers.CenterText(sb, "DUNGEON COMPLETE", 140, Color.Gold);
        DrawHelpers.CenterText(sb, $"Final Depth: {_ctx.Depth.CurrentDepth}", 220, Color.White);
        DrawHelpers.CenterText(sb, $"Enemies Slain: {_ctx.Depth.TotalKills}", 260, Color.White);
        DrawHelpers.CenterText(sb, $"Final Score: {_ctx.Depth.Score}", 300, Color.Yellow);
        DrawHelpers.CenterText(sb, "Press ENTER to play again", 400, Color.Gray);
    }
}