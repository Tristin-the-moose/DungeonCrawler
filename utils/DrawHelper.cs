// ============================================================
// FILE: utils/DrawHelpers.cs — Shared drawing utilities
// ============================================================
using System.Collections.Generic;
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

    /// <summary>
    /// Truncate <paramref name="text"/> with an ellipsis so it fits inside
    /// <paramref name="maxWidth"/> when rendered with <paramref name="font"/>.
    /// Returns the original string unchanged if it already fits.
    /// </summary>
    public static string TruncateToWidth(string text, SpriteFontBase font, float maxWidth)
    {
        if (string.IsNullOrEmpty(text)) return text;
        if (font.MeasureString(text).X <= maxWidth) return text;

        // Drop characters from the end until "<text>…" fits.
        const string Ellipsis = "…";
        int len = text.Length;
        while (len > 0 && font.MeasureString(text.Substring(0, len) + Ellipsis).X > maxWidth)
            len--;
        return len <= 0 ? Ellipsis : text.Substring(0, len) + Ellipsis;
    }

    /// <summary>
    /// Greedy-wrap a comma-separated list (e.g. "+5 HP, +3 ATK, +2 DEF") onto
    /// multiple lines so each rendered line fits within <paramref name="maxWidth"/>.
    /// Useful for the loot screen's stat summaries which can blow past a card's width.
    /// </summary>
    public static List<string> WrapCommaList(string text, SpriteFontBase font, float maxWidth)
    {
        var lines = new List<string>();
        if (string.IsNullOrEmpty(text)) return lines;

        var parts = text.Split(", ");
        var current = "";
        foreach (var p in parts)
        {
            var candidate = current.Length == 0 ? p : current + ", " + p;
            if (font.MeasureString(candidate).X <= maxWidth)
            {
                current = candidate;
            }
            else
            {
                if (current.Length > 0) lines.Add(current);
                // If the single token itself exceeds maxWidth, hard-truncate it
                // so we don't loop forever or push a known-overflowing line.
                current = font.MeasureString(p).X <= maxWidth
                    ? p
                    : TruncateToWidth(p, font, maxWidth);
            }
        }
        if (current.Length > 0) lines.Add(current);
        return lines;
    }
}
