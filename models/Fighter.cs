// ============================================================
// FILE: models/Fighter.cs — A combatant (player or enemy)
// ============================================================
using DungeonCrawler;
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
    public EquipmentSet Equipment { get; set; }

    // ── Temporary buffs (reset each turn by BattleSystem) ──
    public int DefendBuff { get; set; }
    public bool IsDefending { get; set; }

    // ── Heal cooldown ──
    public int HealCooldown { get; set; }
    public bool CanHeal => HealCooldown <= 0;

    // ── Effective stats (base + gear + temp buffs) ──
    public int EffectiveAttack    => Stats.Attack  + (Equipment?.TotalBonusAttack ?? 0);
    public int EffectiveMagic     => Stats.Magic   + (Equipment?.TotalBonusMagic ?? 0);
    public int EffectiveDefense   => Stats.Defense + (Equipment?.TotalBonusDefense ?? 0) + DefendBuff;
    public int EffectiveSpeed     => Stats.Speed   + (Equipment?.TotalBonusSpeed ?? 0);
    public int EffectiveMaxHealth => Stats.MaxHp   + (Equipment?.TotalBonusHealth ?? 0);
    public int EffectiveProtection => Stats.Protection + (Equipment?.TotalBonusProtection ?? 0) + DefendBuff;

    public void ResetBuffs()
    {
        DefendBuff = 0;
        IsDefending = false;
    }

    /// <summary>
    /// Wipe transient combat state at the start of a new battle: heal cooldown,
    /// defend buffs, hit-flash. Equipment and Stats are untouched.
    /// </summary>
    public void ResetCombatState()
    {
        HealCooldown = 0;
        DefendBuff   = 0;
        IsDefending  = false;
        FlashTimer   = 0f;
    }

    /// <summary>
    /// Restore HP, clamped to the fighter's effective max (base + gear bonuses).
    /// All heal callers should use this — Stats has no Heal of its own because
    /// it has no knowledge of equipment HP bonuses.
    /// </summary>
    public void Heal(int amount)
    {
        Stats.Hp = System.Math.Min(EffectiveMaxHealth, Stats.Hp + amount);
    }

    /// <summary>Tick cooldowns at the start of each turn.</summary>
    public void TickCooldowns()
    {
        if (HealCooldown > 0)
            HealCooldown--;
    }

    /// <summary>Whether this fighter's weapon deals magic damage.</summary>
    public bool UsesMagicAttack => Equipment?.Weapon?.IsMagicWeapon ?? false;

    // ── Hit flash ──
    public float FlashTimer { get; set; }
    public bool IsFlashing => FlashTimer > 0f;

    public void TriggerFlash() => FlashTimer = GameConfig.Instance.FlashDuration;

    public Fighter(Stats stats, bool isPlayer)
    {
        Stats = stats;
        IsPlayer = isPlayer;
        if (isPlayer) Equipment = new EquipmentSet();
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