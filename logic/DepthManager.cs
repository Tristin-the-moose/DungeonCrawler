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

    /// <summary>
    /// Award score for a cleared fight. Depth-based base score plus a flat
    /// per-room-type bonus (Battle &lt; Elite &lt; Boss). Non-fighting rooms
    /// (Treasure / Rest) don't go through this — they reward the player in other ways.
    /// </summary>
    public void OnVictory(RoomType roomType)
    {
        var cfg = GameConfig.Instance;
        TotalKills++;

        int score = CurrentDepth * cfg.ScorePerDepth;
        score += roomType switch
        {
            RoomType.Battle => cfg.BattleClearBonus,
            RoomType.Elite  => cfg.EliteClearBonus,
            RoomType.Boss   => cfg.BossClearBonus,
            _               => 0
        };
        Score += score;
    }

    public void GoDeeper() => CurrentDepth++;

    /// <summary>
    /// Heal the player at the start of a new floor — same behaviour as a Rest
    /// room (a flat % of effective max HP). Plus the small per-floor stat boost.
    /// </summary>
    public void RestBetweenFloors(Fighter player)
    {
        var cfg = GameConfig.Instance;

        int heal = (int)(player.EffectiveMaxHealth * cfg.RestHealPercent);
        player.Heal(heal);

        // Small flat bonuses (gear is the main power source now)
        player.Stats.MaxHp  += cfg.MaxHpBoostPerFloor;
        player.Stats.Attack += cfg.AttackBoostPerFloor;
    }
}