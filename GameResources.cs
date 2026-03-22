// ============================================================
// FILE: GameResources.cs — Centralized texture/font loading
// ============================================================
using System;
using System.IO;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonCrawler;

/// <summary>
/// Loads and holds all shared game resources (textures, fonts).
/// Created once in Game1.LoadContent(), disposed in UnloadContent().
/// </summary>
public class GameResources : IDisposable
{
    public Texture2D Pixel { get; }
    public Texture2D PlayerSprite { get; }
    public Texture2D BattleBackground { get; }
    public Texture2D[] EnemySprites { get; }
    public SpriteFontBase Font { get; }
    public SpriteFontBase FontLarge { get; }

    private readonly FontSystem _fontSystem;

    public GameResources(GraphicsDevice gd, ContentManager content)
    {
        // 1x1 white pixel for drawing rectangles
        Pixel = new Texture2D(gd, 1, 1);
        Pixel.SetData(new[] { Color.White });

        // Fonts
        _fontSystem = new FontSystem();
        _fontSystem.AddFont(File.ReadAllBytes("styles/fonts/arial.ttf"));
        Font = _fontSystem.GetFont(18);
        FontLarge = _fontSystem.GetFont(32);

        // Placeholder sprites — replace with Content.Load<Texture2D>() when you have art
        PlayerSprite = MakePlaceholder(gd, 64, 96, Color.CornflowerBlue);
        var cfg = GameConfig.Instance;
        BattleBackground = MakePlaceholder(gd, cfg.ScreenWidth, cfg.ScreenHeight, new Color(30, 20, 40));
        EnemySprites = new[]
        {
            MakePlaceholder(gd, 56, 72, Color.LimeGreen),
            MakePlaceholder(gd, 60, 80, Color.OrangeRed),
            MakePlaceholder(gd, 72, 88, Color.MediumPurple),
            MakePlaceholder(gd, 80, 96, Color.DarkRed),
            MakePlaceholder(gd, 96, 112, Color.Gold),
        };
    }

    private static Texture2D MakePlaceholder(GraphicsDevice gd, int w, int h, Color color)
    {
        var tex = new Texture2D(gd, w, h);
        var data = new Color[w * h];
        Array.Fill(data, color);

        // 1px border for visibility
        for (int x = 0; x < w; x++)
        {
            data[x] = Color.White;
            data[(h - 1) * w + x] = Color.White;
        }
        for (int y = 0; y < h; y++)
        {
            data[y * w] = Color.White;
            data[y * w + w - 1] = Color.White;
        }

        tex.SetData(data);
        return tex;
    }

    public void Dispose()
    {
        Pixel?.Dispose();
        PlayerSprite?.Dispose();
        BattleBackground?.Dispose();
        if (EnemySprites != null)
            foreach (var t in EnemySprites)
                t?.Dispose();
        _fontSystem?.Dispose();
    }
}