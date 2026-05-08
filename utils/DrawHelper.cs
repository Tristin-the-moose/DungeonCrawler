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

        // Use effective max so the bar agrees with the map-screen HP readout
        // and reflects equipment HealthBonus.
        int maxHp = f.EffectiveMaxHealth;
        float pct = maxHp > 0 ? (float)f.Stats.Hp / maxHp : 0f;
        int fill = (int)(w * pct);
        Color barColor = pct > 0.5f  ? Color.LimeGreen
                       : pct > 0.25f ? Color.Yellow
                       : Color.Red;

        DrawRect(sb, x, y, fill, h, barColor);

        string label = $"{f.Stats.Name} {f.Stats.Hp}/{maxHp}";
        sb.DrawString(Game1.Resources.Font, label,
            new Vector2(x + 4, y + 1), Color.Black);
    }

    /// <summary>
    /// Returns a color based on equipment rarity tier.
    /// Rarity also controls how many stat bonuses an item rolls in LootFactory:
    ///   0 white  – 0 bonuses (default / starter gear)
    ///   1 green  – 1 bonus
    ///   2 blue   – 2 bonuses
    ///   3 purple – 3 bonuses
    ///   4 yellow – 4 bonuses
    ///   5 red    - cursed item
    /// </summary>
    public static Color GetRarityColor(int rarity) => rarity switch
    {
        0 => Color.White,
        1 => Color.LimeGreen,
        2 => Color.CornflowerBlue,
        3 => Color.MediumPurple,
        4 => Color.Gold,
        5 => Color.DarkRed,
        _ => Color.White
    };
}