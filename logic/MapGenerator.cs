// ============================================================
// FILE: logic/MapGenerator.cs — Procedural dungeon map generation
// ============================================================
using System;
using System.Collections.Generic;
using DungeonCrawler.models;

namespace DungeonCrawler.logic;

public static class MapGenerator
{
    /// <summary>
    /// Generate a Width×Height grid of rooms for the given depth.
    ///
    /// Layout:
    ///   • Entrance — random cell on the grid (player starts here)
    ///   • Boss     — anywhere except the entrance and its four orthogonal
    ///                neighbours; defeating it converts the room into the
    ///                floor's Exit
    ///   • Remaining rooms filled with Battle / Elite / Treasure / Rest
    ///     in proportions that scale with depth.
    /// </summary>
    public static DungeonMap Generate(int width, int height, int depth, Random rng)
    {
        var rooms = new Room[width, height];

        // Pre-fill everything as Battle
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                rooms[x, y] = new Room(x, y, RoomType.Battle);

        // ── Fixed rooms ──────────────────────────────────────
        // Entrance can sit anywhere on the grid — the player simply starts there.
        int entranceX = rng.Next(width);
        int entranceY = rng.Next(height);
        rooms[entranceX, entranceY].Type = RoomType.Entrance;

        // Boss guards the floor — beat it to turn this room into the Exit.
        // Allowed anywhere on the grid except the entrance itself and the
        // four rooms immediately around it (Manhattan distance <= 1), so
        // the player has to actually traverse a couple of rooms first.
        const int MinDistFromEntrance = 2;

        var bossCandidates = new List<(int x, int y)>();
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                int dist = Math.Abs(x - entranceX) + Math.Abs(y - entranceY);
                if (dist >= MinDistFromEntrance)
                    bossCandidates.Add((x, y));
            }

        // Fallback for tiny grids where every cell is within the safe zone.
        // Pick whichever non-entrance cell is farthest from the entrance, so
        // the boss never overwrites the player's starting room.
        if (bossCandidates.Count == 0)
        {
            int bestX = entranceX, bestY = entranceY, bestDist = -1;
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    if (x == entranceX && y == entranceY) continue;
                    int d = Math.Abs(x - entranceX) + Math.Abs(y - entranceY);
                    if (d > bestDist) { bestX = x; bestY = y; bestDist = d; }
                }
            if (bestDist >= 0)
                bossCandidates.Add((bestX, bestY));
        }

        // Skip boss placement entirely on a 1×1 grid (no other cell exists).
        if (bossCandidates.Count > 0)
        {
            var (bossX, bossY) = bossCandidates[rng.Next(bossCandidates.Count)];
            rooms[bossX, bossY].Type = RoomType.Boss;
        }

        // ── Build shuffled list of assignable positions ───────
        // Clamp at 0 — on a 1×1 grid the boss isn't placed, so width*height-2
        // would underflow and crash List<>'s capacity ctor.
        var pool = new List<(int x, int y)>(Math.Max(0, width * height - 2));
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (rooms[x, y].Type == RoomType.Battle)
                    pool.Add((x, y));

        Shuffle(pool, rng);

        // ── Calculate room counts based on pool size + depth ──
        int slots = pool.Count;

        // Rest and Treasure: roughly 1 per 5 slots each, minimum 1
        int restCount     = Math.Max(1, slots / 5);
        int treasureCount = Math.Max(1, slots / 5);

        // Elite: base 1 per 6 slots, +1 every 3 depths (capped at 3 bonus)
        int eliteBase  = Math.Max(1, slots / 6);
        int eliteBonus = Math.Min(depth / 3, 3);
        int eliteCount = eliteBase + eliteBonus;

        // Shop: exactly one per floor (skipped automatically if pool is empty)
        int shopCount = 1;

        // Assign in priority order (rarest/hardest first so they get random spots)
        int cursor = 0;
        Assign(rooms, pool, ref cursor, RoomType.Elite,    eliteCount);
        Assign(rooms, pool, ref cursor, RoomType.Rest,     restCount);
        Assign(rooms, pool, ref cursor, RoomType.Shop,     shopCount);
        Assign(rooms, pool, ref cursor, RoomType.Treasure, treasureCount);
        // Everything else stays as Battle

        return new DungeonMap(rooms, entranceX, entranceY);
    }

    // ── Helpers ──────────────────────────────────────────────

    private static void Assign(
        Room[,] rooms,
        List<(int x, int y)> pool,
        ref int cursor,
        RoomType type,
        int count)
    {
        for (int i = 0; i < count && cursor < pool.Count; i++, cursor++)
        {
            var (x, y) = pool[cursor];
            rooms[x, y].Type = type;
        }
    }

    private static void Shuffle<T>(List<T> list, Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
