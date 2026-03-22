using Microsoft.Xna.Framework.Graphics;

namespace DungeonCrawler.screens;

public interface IGameScreen
{
    void Update(float dt);
    void Draw(SpriteBatch sb);
}