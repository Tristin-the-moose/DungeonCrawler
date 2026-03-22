// ============================================================
// FILE: screens/VictoryScreen.cs — Post-battle continue/quit
// ============================================================
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DungeonCrawler.logic;
using DungeonCrawler.utils;

namespace DungeonCrawler.screens;

public class VictoryScreen : IGameScreen
{
    private readonly GameContext _ctx;
    private readonly Action<IGameScreen> _setScreen;
    private readonly MenuSelector _menu = new(3);

    private static readonly (string Label, Color Color)[] Options =
    {
        ("Go Deeper", Color.White),
        ("Save & Quit", Color.CornflowerBlue),
        ("Cash Out", Color.Gray)
    };

    public VictoryScreen(GameContext ctx, Action<IGameScreen> setScreen)
    {
        _ctx = ctx;
        _setScreen = setScreen;
    }

    public void Update(float dt)
    {
        _menu.Update();

        if (!_menu.Confirmed) return;

        switch (_menu.Index)
        {
            case 0: // Go Deeper
                AdvanceAndSave();
                _setScreen(new BattleScreen(_ctx, _setScreen));
                break;

            case 1: // Save & Quit
                AdvanceAndSave();
                _setScreen(new TitleScreen(_ctx, _setScreen));
                break;

            case 2: // Cash Out
                SaveSystem.Delete();
                _setScreen(new FinalScoreScreen(_ctx, _setScreen));
                break;
        }
    }

    public void Draw(SpriteBatch sb)
    {
        string msg = $"Victory!  Depth: {_ctx.Depth.CurrentDepth}  Score: {_ctx.Depth.Score}";
        DrawHelpers.CenterText(sb, msg, 140, Color.Gold);

        string hp = $"HP: {_ctx.Player.Stats.Hp}/{_ctx.Player.Stats.MaxHp}";
        DrawHelpers.CenterText(sb, hp, 200, Color.LightGreen);

        for (int i = 0; i < Options.Length; i++)
        {
            bool selected = _menu.Index == i;
            string prefix = selected ? "> " : "  ";
            Color c = selected ? Color.Yellow : Options[i].Color;
            DrawHelpers.CenterText(sb, prefix + Options[i].Label, 280 + i * 40, c);
        }
    }

    private void AdvanceAndSave()
    {
        _ctx.Depth.GoDeeper();
        _ctx.Depth.RestBetweenFloors(_ctx.Player.Stats);
        SaveSystem.Save(_ctx.Player, _ctx.Depth);
    }
}