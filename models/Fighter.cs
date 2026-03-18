using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonCrawler.models;

public class Fighter
{
    public Stats Stats { get; set; }
    public Texture2D Sprite { get; set; }
    public Vector2 Position { get; set; }
    public float Scale { get; set; } = 1.0f;
    public bool IsPlayer { get; set; }
    public SpriteEffects FlipEffect { get; set; } = SpriteEffects.None;

    // Simple hit flash
    public float FlashTimer { get; set; } = 0f;
    public bool IsFlashing => FlashTimer > 0f;

    public Fighter(Stats stats, bool isPlayer)
    {
        Stats = stats;
        IsPlayer = isPlayer;
    }

    public void Update(float dt)
    {
        if (FlashTimer > 0f)
            FlashTimer -= dt;
    }

    public void Draw(SpriteBatch sb)
    {
        Color tint = IsFlashing ? Color.Red : Color.White;
        sb.Draw(Sprite, Position, null, tint, 0f,
            Vector2.Zero, Scale, FlipEffect, 0f);
    }
}