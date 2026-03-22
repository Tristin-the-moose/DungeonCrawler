// ============================================================
// FILE: Game1.cs — Main MonoGame entry point (Refactored)
// ============================================================
using System;
using System.IO;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DungeonCrawler.models;
using DungeonCrawler.logic;
using DungeonCrawler.screens;
using DungeonCrawler.utils;

namespace DungeonCrawler;

public class Game1 : Game
{
    // ── Core ──
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _sb;

    // ── Shared Resources ──
    public static GameResources Resources { get; private set; }

    // ── Screen Management ──
    private IGameScreen _currentScreen;
    private GameContext _context;

    // ── Screen Constants ──
    public const int ScreenW = 960;
    public const int ScreenH = 540;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = ScreenW,
            PreferredBackBufferHeight = ScreenH
        };
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void LoadContent()
    {
        _sb = new SpriteBatch(GraphicsDevice);

        // Load all shared resources once
        Resources = new GameResources(GraphicsDevice, Content);

        // Create shared game context
        _context = new GameContext
        {
            Player = FighterFactory.CreatePlayer(Resources.PlayerSprite),
            Depth = new DepthManager(),
            Rng = new Random()
        };

        // Start on title screen
        SetScreen(new TitleScreen(_context, SetScreen));
    }

    public void SetScreen(IGameScreen screen)
    {
        _currentScreen = screen;
    }

    protected override void Update(GameTime gt)
    {
        float dt = (float)gt.ElapsedGameTime.TotalSeconds;
        _currentScreen.Update(dt);
        base.Update(gt);
    }

    protected override void Draw(GameTime gt)
    {
        GraphicsDevice.Clear(Color.Black);
        _sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                  SamplerState.PointClamp);

        _currentScreen.Draw(_sb);

        _sb.End();
        base.Draw(gt);
    }

    protected override void UnloadContent()
    {
        Resources?.Dispose();
        base.UnloadContent();
    }
}