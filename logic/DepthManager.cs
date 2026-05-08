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
    public int Gold { get; set; } = GameConfig.Instance.StartingGold;

    /// <summary>
    /// Most recent gold delta from a battle/treasure drop. Surfaced to UI so
    /// the battle log / loot screen can show "+N gold" without DepthManager
    /// owning any rendering concerns.
    /// </summary>
    public int LastGoldAwarded { get; private set; }

    /// <summary>
    /// Award score for a cleared fight. Depth-based base score plus a flat
    /// per-room-type bonus (Battle &lt; Elite &lt; Boss). Non-fighting rooms
    /// (Treasure / Rest) don't go through this — they reward the player in other ways.
    /// Also drops gold on top of the score reward.
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

        AwardBattleGold(roomType);
    }

    /// <summary>
    /// Roll a gold drop for a battle victory and add it to the run total.
    /// Stored on <see cref="LastGoldAwarded"/> for the UI to display.
    /// </summary>
    private void AwardBattleGold(RoomType roomType)
    {
        var cfg = GameConfig.Instance;
        float depthMult = MathF.Pow(cfg.BattleGoldDepthScale, Math.Max(0, CurrentDepth - 1));
        float typeMult  = roomType switch
        {
            RoomType.Elite => cfg.EliteGoldMultiplier,
            RoomType.Boss  => cfg.BossGoldMultiplier,
            _              => 1f,
        };
        int amount = (int)MathF.Round(cfg.BattleGoldBase * depthMult * typeMult);
        amount = ApplyVariance(amount, cfg.GoldDropVariance);

        LastGoldAwarded = Math.Max(0, amount);
        Gold += LastGoldAwarded;
    }

    /// <summary>
    /// Award a gold drop for a treasure room. Larger base + steeper depth scale
    /// than battle drops so chests stay rewarding through the late game.
    /// </summary>
    public int AwardTreasureGold(Random rng)
    {
        var cfg = GameConfig.Instance;
        float depthMult = MathF.Pow(cfg.TreasureGoldDepthScale, Math.Max(0, CurrentDepth - 1));
        int amount = (int)MathF.Round(cfg.TreasureGoldBase * depthMult);
        amount = ApplyVariance(amount, cfg.GoldDropVariance, rng);

        LastGoldAwarded = Math.Max(0, amount);
        Gold += LastGoldAwarded;
        return LastGoldAwarded;
    }

    /// <summary>
    /// Try to deduct <paramref name="cost"/> gold. Returns true if successful,
    /// false if the player can't afford it (Gold left untouched in that case).
    /// </summary>
    public bool TrySpend(int cost)
    {
        if (cost < 0) return false;
        if (Gold < cost) return false;
        Gold -= cost;
        return true;
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

    // ── Helpers ──────────────────────────────────────────────

    /// <summary>
    /// Apply ±variance jitter to <paramref name="amount"/>. Uses a shared Random
    /// so calls without a Rng (battle drops) still get spread; pass an Rng to
    /// stay deterministic when the run's RNG is available.
    /// </summary>
    private static int ApplyVariance(int amount, float variance, Random rng = null)
    {
        if (variance <= 0f) return amount;
        rng ??= _sharedRng;
        // Range: [1-v, 1+v]
        double mult = 1.0 + (rng.NextDouble() * 2.0 - 1.0) * variance;
        return Math.Max(0, (int)Math.Round(amount * mult));
    }

    private static readonly Random _sharedRng = new();
}
