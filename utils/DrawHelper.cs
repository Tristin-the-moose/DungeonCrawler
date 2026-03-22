// ============================================================
// FILE: utils/DrawHelpers.cs — Shared drawing utilities
// ============================================================
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FontStashSharp;

namespace DungeonCrawler.utils;

/// <summary>
/// Static helpers for common draw operations (rectangles, centered text, HP bars).
/// Eliminates duplicate drawing code across screens.
/// </summary>
public static class DrawHelpers
{
    public static void DrawRect(SpriteBatch sb, int x, int y, int w, int h, Color c)
    {
        sb.Draw(Game1.Resources.Pixel, new Rectangle(x, y, w, h), c);
    }

    public static void CenterText(SpriteBatch sb, string text, int y, Color color)
    {
        var font = Game1.Resources.Font;
        var size = font.MeasureString(text);
        sb.DrawString(font, text,
            new Vector2((Game1.ScreenW - size.X) / 2, y), color);
    }

    public static void CenterTextLarge(SpriteBatch sb, string text, int y, Color color)
    {
        var font = Game1.Resources.FontLarge;
        var size = font.MeasureString(text);
        sb.DrawString(font, text,
            new Vector2((Game1.ScreenW - size.X) / 2, y), color);
    }

    public static void DrawHpBar(SpriteBatch sb, int x, int y, int w, int h, models.Fighter f)
    {
        DrawRect(sb, x, y, w, h, Color.DarkGray);

        int fill = (int)(w * f.Stats.HpPercent);
        Color barColor = f.Stats.HpPercent > 0.5f  ? Color.LimeGreen
                       : f.Stats.HpPercent > 0.25f ? Color.Yellow
                       : Color.Red;

        DrawRect(sb, x, y, fill, h, barColor);

        string label = $"{f.Stats.Name} {f.Stats.Hp}/{f.Stats.MaxHp}";
        sb.DrawString(Game1.Resources.Font, label,
            new Vector2(x + 4, y + 1), Color.Black);
    }

    /// <summary>
    /// Returns a color based on equipment rarity tier.
    /// Centralizes the rarity → color mapping (was duplicated in original).
    /// </summary>
    public static Color GetRarityColor(int rarity) => rarity switch
    {
        0 => Color.Gray,
        1 => Color.White,
        2 => Color.LimeGreen,
        3 => Color.CornflowerBlue,
        4 => Color.Gold,
        _ => Color.Gray
    };
}