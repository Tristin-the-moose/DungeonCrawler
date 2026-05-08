// ============================================================
// FILE: models/DungeonMap.cs — Grid map + player position logic
// ============================================================
using System;

namespace DungeonCrawler.models;

public class DungeonMap
{
    private readonly Room[,] _rooms;

    public int Width    { get; }
    public int Height   { get; }
    public int PlayerX  { get; private set; }
    public int PlayerY  { get; private set; }

    public Room GetRoom(int x, int y) => _rooms[x, y];

    public bool InBounds(int x, int y) =>
        x >= 0 && x < Width && y >= 0 && y < Height;

    /// <summary>True when (tx,ty) is orthogonally adjacent to the player.</summary>
    public bool IsAdjacentToPlayer(int tx, int ty) =>
        InBounds(tx, ty) &&
        Math.Abs(tx - PlayerX) + Math.Abs(ty - PlayerY) == 1;

    /// <summary>
    /// Player can step into a room if it is directly adjacent and already
    /// revealed (Accessible or Visited — not Hidden).
    /// The Current room itself is also included so we can skip re-entering it.
    /// </summary>
    public bool CanEnter(int tx, int ty)
    {
        if (!IsAdjacentToPlayer(tx, ty)) return false;
        var s = _rooms[tx, ty].State;
        return s == RoomState.Accessible || s == RoomState.Visited;
    }

    public DungeonMap(Room[,] rooms, int startX, int startY)
    {
        _rooms  = rooms;
        Width   = rooms.GetLength(0);
        Height  = rooms.GetLength(1);
        PlayerX = startX;
        PlayerY = startY;

        _rooms[startX, startY].State = RoomState.Current;
        RevealAdjacent();
    }

    /// <summary>
    /// Reconstruct a DungeonMap from saved data.
    /// Room states are taken as-is from the save — no initialization is run.
    /// </summary>
    public static DungeonMap FromSave(Room[,] rooms, int playerX, int playerY)
    {
        return new DungeonMap(rooms, playerX, playerY, restored: true);
    }

    // Private constructor used by FromSave — skips state initialization.
    private DungeonMap(Room[,] rooms, int playerX, int playerY, bool restored)
    {
        _rooms  = rooms;
        Width   = rooms.GetLength(0);
        Height  = rooms.GetLength(1);
        PlayerX = playerX;
        PlayerY = playerY;
        // Room states already loaded from save — do not override them.
    }

    /// <summary>
    /// Move the player to (tx, ty).
    /// Marks the old room Visited, the new room Current, and reveals
    /// any newly adjacent hidden rooms.
    /// Returns the room now occupied.
    /// </summary>
    public Room MovePlayer(int tx, int ty)
    {
        if (_rooms[PlayerX, PlayerY].State == RoomState.Current)
            _rooms[PlayerX, PlayerY].State = RoomState.Visited;

        PlayerX = tx;
        PlayerY = ty;
        _rooms[tx, ty].State = RoomState.Current;
        RevealAdjacent();
        return _rooms[tx, ty];
    }

    // ── Orthogonal direction offsets ────────────────────────
    private static readonly int[] Dx = { 0, 0,  1, -1 };
    private static readonly int[] Dy = { 1, -1, 0,  0 };

    private void RevealAdjacent()
    {
        for (int d = 0; d < 4; d++)
        {
            int nx = PlayerX + Dx[d];
            int ny = PlayerY + Dy[d];
            if (InBounds(nx, ny) && _rooms[nx, ny].State == RoomState.Hidden)
                _rooms[nx, ny].State = RoomState.Accessible;
        }
    }
}
