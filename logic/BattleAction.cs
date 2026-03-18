using System;

using DungeonCrawler.models;

namespace DungeonCrawler.logic;

public enum BattleActionType
{
    Attack,
    Magic,
    Defend,
    Heal
}

public class BattleAction
{
    public BattleActionType Type { get; set; }
    public Fighter Source { get; set; }
    public Fighter Target { get; set; }

    public string Execute(Random rng)
    {
        switch (Type)
        {
            case BattleActionType.Attack:
            {
                int atk = Source.IsPlayer ? Source.EffectiveAttack : Source.Stats.Attack ;
                int def = Target.IsPlayer ? Target.EffectiveDefense : Target.Stats.Defense;

                int dealt = (atk - def) < 0 ? 1 : (atk - def);

                Target.Stats.TakeDamage(dealt);
                Target.FlashTimer = 0.3f;
                return $"{Source.Stats.Name} attacks for {dealt} damage!";
            }

            case BattleActionType.Magic:
            {
                int atk = Source.IsPlayer ? Source.EffectiveMagic : Source.Stats.Magic ;
                int def = Target.IsPlayer ? Target.EffectiveProtection : Target.Stats.Defense;

                int dealt = (atk - def) < 0 ? 1 : (atk - def);

                Target.Stats.TakeDamage(dealt);
                Target.FlashTimer = 0.3f;
                return $"{Source.Stats.Name} casts a spell for {dealt} damage!";
            }

            case BattleActionType.Defend:
            {
                Source.Stats.Defense += 3; // temporary boost
                return $"{Source.Stats.Name} braces for impact! (+3 DEF)";
            }

            case BattleActionType.Heal:
            {
                int heal = 10 + Source.Stats.Magic;
                Source.Stats.Hp = Math.Min(Source.Stats.MaxHp,
                                           Source.Stats.Hp + heal);
                return $"{Source.Stats.Name} heals for {heal} HP!";
            }

            default:
                return "";
        }
    }
}