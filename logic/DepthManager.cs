using  System;

using DungeonCrawler.models;

namespace DungeonCrawler.logic;

public class DepthManager
{
    public int CurrentDepth { get; set; } = 1;
    public int TotalKills { get; set; } = 0;
    public int Score { get; set; } = 0;

    public void OnVictory()
    {
        TotalKills++;
        Score += CurrentDepth * 100;
    }

    public void GoDeeper()
    {
        CurrentDepth++;
    }

    /// <summary>Heal player partially between floors.</summary>
    public void RestBetweenFloors(Stats playerStats)
    {
        int heal = playerStats.MaxHp / 4;
        playerStats.Hp = Math.Min(playerStats.MaxHp, playerStats.Hp + heal);

        // Small permanent stat boost for going deeper
        playerStats.MaxHp += 2;
        playerStats.Attack += 1;
    }
}