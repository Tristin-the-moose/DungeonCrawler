// ============================================================
// FILE: logic/BattleSystem.cs — Turn-based battle engine
// ============================================================
using System;
using System.Collections.Generic;
using DungeonCrawler;
using DungeonCrawler.models;

namespace DungeonCrawler.logic;

public enum BattleTurnState
{
    PlayerChoosing,
    Animating,
    EnemyTurn,
    BattleWon,
    BattleLost
}

public class BattleSystem
{
    public Fighter Player { get; }
    public Fighter Enemy { get; }
    public BattleTurnState State { get; set; }
    public List<string> Log { get; } = new();
    public int Depth { get; }

    private readonly Random _rng;
    private float _animTimer;
    private readonly Queue<BattleAction> _pendingActions = new();

    // Cached so BattleAction.Execute(_rng, _logLine) doesn't allocate a fresh
    // delegate per action.
    private readonly Action<string> _logLine;

    // % chance the enemy holds back instead of swinging into a defending player.
    private const int EnemyDefendSkipChance = 50;

    public BattleSystem(Fighter player, Fighter enemy, int depth, Random rng)
    {
        Player = player;
        Enemy  = enemy;
        Depth  = depth;
        _rng   = rng;
        State  = BattleTurnState.PlayerChoosing;
        _logLine = Log.Add;

        // Fresh battle — clear lingering combat state on both fighters
        // (heal cooldowns, defend buffs, etc.).
        Player.ResetCombatState();
        Enemy.ResetCombatState();

        Log.Add($"A wild {enemy.Stats.Name} appears! (Depth {depth})");
    }

    public void SubmitPlayerAction(BattleActionType type)
    {
        if (State != BattleTurnState.PlayerChoosing) return;

        var cfg = GameConfig.Instance;

        // Reset temporary defend buffs and tick cooldowns. Only the player
        // has cooldowns today (Heal); enemies don't heal, so we don't tick them.
        Player.ResetBuffs();
        Enemy.ResetBuffs();
        Player.TickCooldowns();

        var playerTarget = type is BattleActionType.Defend or BattleActionType.Heal
            ? Player : Enemy;

        var playerAction = new BattleAction
        {
            Type = type, Source = Player, Target = playerTarget
        };

        // Enemy always attacks
        var enemyAction = new BattleAction
        {
            Type = BattleActionType.Attack, Source = Enemy, Target = Player
        };

        // Defend applies immediately — it's a stance, not a timed action.
        // This ensures it works regardless of speed/turn order.
        if (type == BattleActionType.Defend)
        {
            playerAction.Execute(_rng, _logLine);

            // 50/50: enemy hesitates and waits, or commits to its attack into
            // the defender (which lets the player's counter trigger).
            if (_rng.Next(100) < EnemyDefendSkipChance)
            {
                Log.Add($"{Enemy.Stats.Name} hesitates and waits.");
                // Queue nothing — Update will fall through to PlayerChoosing.
            }
            else
            {
                _pendingActions.Enqueue(enemyAction);
            }
        }
        else
        {
            // Normal turn order based on speed
            if (Enemy.EffectiveSpeed > Player.EffectiveSpeed)
            {
                _pendingActions.Enqueue(enemyAction);
                _pendingActions.Enqueue(playerAction);
            }
            else
            {
                _pendingActions.Enqueue(playerAction);
                _pendingActions.Enqueue(enemyAction);
            }
        }

        State = BattleTurnState.Animating;
        _animTimer = cfg.PreActionDelay;
    }

    public void Update(float dt)
    {
        Player.Update(dt);
        Enemy.Update(dt);

        if (State != BattleTurnState.Animating) return;

        _animTimer -= dt;
        if (_animTimer > 0f) return;

        if (_pendingActions.Count > 0)
        {
            var action = _pendingActions.Dequeue();

            if (action.Source.Stats.IsAlive)
                action.Execute(_rng, _logLine);

            _animTimer = GameConfig.Instance.BetweenActionDelay;
        }

        if (_pendingActions.Count == 0 && _animTimer <= 0f)
        {
            if (!Enemy.Stats.IsAlive)
                State = BattleTurnState.BattleWon;
            else if (!Player.Stats.IsAlive)
                State = BattleTurnState.BattleLost;
            else
                State = BattleTurnState.PlayerChoosing;
        }
    }
}