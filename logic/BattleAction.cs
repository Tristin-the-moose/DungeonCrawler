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

    /// <summary>Resolve this action and return a log message.</summary>
    public string Execute(Random rng)
    {
        switch (Type)
        {
            case BattleActionType.Attack:
            {
                int dmg = Source.Stats.Attack + rng.Next(-2, 4);
                Target.Stats.TakeDamage(dmg);
                Target.FlashTimer = 0.3f;
                int dealt = Math.Max(0, dmg - Target.Stats.Defense / 2);
                return $"{Source.Stats.Name} attacks for {dealt} damage!";
            }

            case BattleActionType.Magic:
            {
                int dmg = Source.Stats.Magic * 2 + rng.Next(0, 5);
                Target.Stats.TakeDamage(dmg);
                Target.FlashTimer = 0.4f;
                int dealt = Math.Max(0, dmg - Target.Stats.Defense / 2);
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