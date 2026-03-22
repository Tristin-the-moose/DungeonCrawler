using System;
using DungeonCrawler.models;
using DungeonCrawler.logic;

namespace DungeonCrawler;

/// <summary>
/// Holds game-wide state that persists across screen transitions.
/// Passed by reference to each screen so they can read/modify shared data.
/// </summary>
public class GameContext
{
    public Fighter Player { get; set; }
    public DepthManager Depth { get; set; }
    public Random Rng { get; set; }

    /// <summary>
    /// Resets the context for a new game.
    /// </summary>
    public void Reset()
    {
        Depth = new DepthManager();
        Player = FighterFactory.CreatePlayer(Game1.Resources.PlayerSprite);
    }
}