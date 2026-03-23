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

        // Heal percentage decays with depth — early floors are forgiving, later floors are punishing
        // healPct = baseHeal - (depth * decay), clamped to minimum
        float healPct = cfg.HealPercentBetweenFloors - (CurrentDepth * cfg.HealDecayPerFloor);
        healPct = MathF.Max(healPct, cfg.MinHealPercent);

        int heal = (int)(playerStats.MaxHp * healPct);
        playerStats.Heal(heal);

        // Small flat bonuses (reduced from original — gear is the main power source now)
        playerStats.MaxHp += cfg.MaxHpBoostPerFloor;
        playerStats.Attack += cfg.AttackBoostPerFloor;
    }
}