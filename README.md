# Dungeon Crawler

A turn-based, procedurally generated dungeon crawler built in C# on top of MonoGame. Descend through randomly assembled floors, fight scaling enemies, equip rarity-tiered loot, spend gold at shops between rooms, and try to push as deep as you can before you die.

## Status

Active solo project. The codebase compiles and runs against .NET 9 / MonoGame 3.8 and is structured to make balance, content, and mechanic tweaks cheap (most knobs live in `config.json` or `GameConfig.cs`).

## Features

- **Procedural map generation** — each floor produces a fresh node graph of battle, treasure, shop, and rest rooms via `MapGenerator` and `DungeonMap`.
- **Turn-based combat** — attack / defend / heal / magic loop with crits, defense vs. protection split (physical vs. magic mitigation), speed-based crit bonuses, and a counter mechanic on successful blocks.
- **Loot system** — equipment with rarity tiers, randomized stat rolls, name tables, and a configurable cursed-loot chance. See `LootFactory`, `LootRarity`, `LootStatPools`, `LootNameTables`.
- **Economy & shops** — guaranteed shop room per floor with stat upgrades and re-rolls funded by gold drops from battles and treasure (`ShopOfferFactory`, `ShopScreen`, `DepthManager.Gold`). The full design is documented in [`SHOP_PLAN.md`](SHOP_PLAN.md).
- **Persistence** — single-slot save/load through `SaveSystem`, including player stats, depth, gold, and the current map layout so a Continue resumes mid-floor.
- **Centralized configuration** — every balance value (combat, scaling, loot, economy, display) is exposed via `GameConfig` and overridable from `config.json` without recompiling.
- **Crash reporting** — `Program.cs` writes a timestamped `logs/CRASH_*.txt` on unhandled exceptions and `GameLogger` keeps a rolling log of run events.

## Tech stack

- **Language:** C# 12 with `Nullable=annotations`
- **Runtime:** .NET 9 (`RollForward=Major`)
- **Engine:** [MonoGame.Framework.DesktopGL 3.8.\*](https://www.monogame.net/)
- **Fonts:** [FontStashSharp.MonoGame 1.5.4](https://github.com/FontStashSharp/FontStashSharp)
- **Content pipeline:** MonoGame Content Builder Task

## Project layout

```
GameDesign/
├── GameDesign.sln              # top-level solution
├── SHOP_PLAN.md                # design doc for the shop & gold system
├── GameBuilds/                 # output of publish.bat
└── DungeonCrawler/
    ├── DungeonCrawler.csproj
    ├── Program.cs              # entry point + crash handler
    ├── Game1.cs                # MonoGame Game subclass, screen dispatcher
    ├── GameContext.cs          # shared state across screens
    ├── GameConfig.cs           # all tunables (loaded from config.json)
    ├── GameResources.cs        # font + sprite loading
    ├── config.json             # user-editable balance overrides
    ├── publish.bat             # one-shot win-x64 publish script
    ├── Content/                # MonoGame content pipeline
    ├── models/                 # Fighter, Stats, Equipment, Room, DungeonMap
    ├── logic/                  # BattleSystem, MapGenerator, factories, loot tables
    ├── screens/                # Title, Map, Battle, Loot, Shop, Rest, Stats, Victory, GameOver, FinalScore, Config
    └── utils/                  # DrawHelper, MenuSelector, KeyboardWatcher, GameLogger, SaveSystem
```

The architecture follows a simple screen-stack pattern. `Game1` owns the active `IGameScreen`; each screen receives a shared `GameContext` (player, depth manager, RNG, current map) and a `setScreen` callback so transitions stay one-way and explicit.

## Getting started

### Prerequisites

- .NET 9 SDK
- A desktop OS supported by MonoGame DesktopGL (Windows, macOS, or Linux)
- `dotnet` on your PATH

### Build & run

From the repo root:

```
dotnet build GameDesign.sln
dotnet run --project DungeonCrawler/DungeonCrawler.csproj
```

The first build pulls MonoGame and FontStashSharp from NuGet and triggers the content pipeline.

### Publish a standalone build (Windows)

```
cd DungeonCrawler
publish.bat
```

The script outputs a self-contained win-x64 build into `../GameBuilds/`.

## Configuration

`DungeonCrawler/config.json` is loaded at startup; any field present overrides the default in `GameConfig.cs`, and missing fields fall back to the defaults — so you can keep the JSON minimal and only override what you want to tweak.

A few of the most useful knobs:

| Setting | Effect |
| --- | --- |
| `ScreenWidth` / `ScreenHeight` / `Fullscreen` / `VSync` | Display |
| `StartingMaxHp`, `StartingAttack`, `StartingDefense`, `StartingSpeed`, `StartingMagic` | Player base stats |
| `CritChance`, `CritMultiplier`, `DamageVariance` | Combat feel |
| `EnemyScaleExponent`, `EnemyScaleMultiplier` | Difficulty curve per depth |
| `LootChoiceCount`, `LootMaxTier`, `CursedLootChance` | Loot generation |
| `BattleGoldBase`, `ShopUpgradeBasePrice`, `ShopUpgradePriceScale` | Economy (see `SHOP_PLAN.md`) |

In-game, the **Settings** option on the title screen exposes a subset of these through `ConfigScreen`.

## Saves & logs

- **Saves** are written to disk by `SaveSystem` and surfaced as a **Continue** option on the title screen when present. Starting a new game deletes the existing save.
- **Logs** land in `logs/` next to the executable. Crashes additionally produce `logs/CRASH_<timestamp>.txt` with the stack trace.

## Controls

Keyboard-driven menus throughout. `MenuSelector` and `KeyboardWatcher` handle navigation: arrow keys to move the cursor, Enter/Space to confirm, Escape to back out where supported.

## Roadmap / known unfinished work

## License

No license has been declared yet. Treat the contents as "all rights reserved" until a `LICENSE` file is added.

Code Repo: https://github.com/Tristin-the-moose/DungeonCrawler
