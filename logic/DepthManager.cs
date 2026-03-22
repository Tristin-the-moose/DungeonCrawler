// ============================================================
// FILE: logic/DepthManager.cs — Tracks dungeon progression
// ============================================================
using System;
using DungeonCrawler;
using DungeonCrawler.models;

namespace DungeonCrawler.logic;

public class DepthManager
{
    public int CurrentDepth { get; set; } = 1;
    public int TotalKills { get; set; }
    public int Score { get; set; }

    public void OnVictory()
    {
        TotalKills++;
        Score += CurrentDepth * GameConfig.Instance.ScorePerDepth;
    }

    public void GoDeeper() => CurrentDepth++;

    public void RestBetweenFloors(Stats playerStats)
    {
        var cfg = GameConfig.Instance;
        int heal = (int)(playerStats.MaxHp * cfg.HealPercentBetweenFloors);
        playerStats.Heal(heal);

        playerStats.MaxHp += cfg.MaxHpBoostPerFloor;
        playerStats.Attack += cfg.AttackBoostPerFloor;
    }
}