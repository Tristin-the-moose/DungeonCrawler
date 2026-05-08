// ============================================================
// FILE: screens/TitleScreen.cs — Title / main menu screen
// ============================================================
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DungeonCrawler.logic;
using DungeonCrawler.models;
using DungeonCrawler.utils;

namespace DungeonCrawler.screens;

public class TitleScreen : IGameScreen
{
    private readonly GameContext _ctx;
    private readonly Action<IGameScreen> _setScreen;
    private readonly MenuSelector _menu;
    private readonly List<(string Label, Color Color, Action OnSelect)> _options = new();

    public TitleScreen(GameContext ctx, Action<IGameScreen> setScreen)
    {
        _ctx = ctx;
        _setScreen = setScreen;

        // Build menu options dynamically based on save state
        if (SaveSystem.HasSave())
        {
            _options.Add(("Continue",  Color.LimeGreen,    ContinueGame));
            _options.Add(("New Game",  Color.White,        StartNewGame));
            _options.Add(("Settings",  Color.MediumPurple, OpenSettings));
            _options.Add(("Exit",      Color.Gray,         Game1.ExitRequest));
        }
        else
        {
            _options.Add(("Start Game", Color.White,        StartNewGame));
            _options.Add(("Settings",   Color.MediumPurple, OpenSettings));
            _options.Add(("Exit",       Color.Gray,         Game1.ExitRequest));
        }

        _menu = new MenuSelector(_options.Count);
    }

    public void Update(float dt)
    {
        _menu.Update();

        if (_menu.Confirmed)
            _options[_menu.Index].OnSelect();
    }

    public void Draw(SpriteBatch sb)
    {
        DrawHelpers.CenterTextLarge(sb, "DUNGEON CRAWLER", 160, Color.Gold);

        for (int i = 0; i < _options.Count; i++)
        {
            var (label, color, _) = _options[i];
            bool selected = _menu.Index == i;
            string prefix = selected ? "> " : "  ";
            Color drawColor = selected ? Color.Yellow : color;
            DrawHelpers.CenterText(sb, prefix + label, 280 + i * 40, drawColor);
        }
    }

    private void StartNewGame()
    {
        SaveSystem.Delete();
        _ctx.Reset();          // also calls GenerateNewMap()
        _setScreen(new MapScreen(_ctx, _setScreen));
    }

    private void ContinueGame()
    {
        var save = SaveSystem.Load();
        if (save != null)
        {
            _ctx.Player = FighterFactory.FromSave(save, Game1.Resources.PlayerSprite);
            _ctx.Depth  = new DepthManager
            {
                CurrentDepth = save.CurrentDepth,
                TotalKills   = save.TotalKills,
                Score        = save.Score,
                Gold         = save.Gold
            };

            // Restore the saved map if present, otherwise generate a fresh one
            if (save.MapRooms != null && save.MapRooms.Length > 0)
            {
                var rooms = new Room[save.MapWidth, save.MapHeight];
                foreach (var r in save.MapRooms)
                    rooms[r.X, r.Y] = new Room(r.X, r.Y, r.Type) { State = r.State };
                _ctx.CurrentMap = DungeonMap.FromSave(rooms, save.MapPlayerX, save.MapPlayerY);
            }
            else
            {
                _ctx.GenerateNewMap();
            }
        }
        else
        {
            _ctx.GenerateNewMap();
        }

        _setScreen(new MapScreen(_ctx, _setScreen));
    }

    private void OpenSettings()
    {
        _setScreen(new ConfigScreen(_setScreen, new TitleScreen(_ctx, _setScreen)));
    }
}