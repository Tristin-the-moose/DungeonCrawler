// ============================================================
// FILE: logic/DepthManager.cs — Tracks dungeon progression
// ============================================================
using System;
using DungeonCrawler.models;

namespace DungeonCrawler.logic;

public class DepthManager
{
    private const int ScorePerDepth = 100;
    private const int HpBoostPerFloor = 2;
    private const int AtkBoostPerFloor = 1;
    private const float HealPercent = 0.25f;

    public int CurrentDepth { get; set; } = 1;
    public int TotalKills { get; set; }
    public int Score { get; set; }

    public void OnVictory()
    {
        TotalKills++;
        Score += CurrentDepth * ScorePerDepth;
    }

    public void GoDeeper() => CurrentDepth++;

    /// <summary>Heal player partially between floors + small permanent stat boost.</summary>
    public void RestBetweenFloors(Stats playerStats)
    {
        int heal = (int)(playerStats.MaxHp * HealPercent);
        playerStats.Heal(heal);  // Uses the new Heal() method on Stats

        playerStats.MaxHp += HpBoostPerFloor;
        playerStats.Attack += AtkBoostPerFloor;
    }
}