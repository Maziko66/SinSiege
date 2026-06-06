# SinSiege

A 2D tower-defense / wave-survival hybrid built in **Unity 6**, themed around the seven deadly
sins (the first playable region is *Lust*). You control a hero **and** build towers to defend a
central **Base** against waves of enemies that travel authored routes — while a parallel **horde**
mechanic spawns enemies that chase the player. Killing enemies drops **Souls** (currency) used for
towers and upgrades. Characters, towers, upgrades and all UI text are data-driven and localized
into 12 languages.

> **📄 Full handover documentation:** [`Documentation/SinSiege_Developer_Handover.pdf`](Documentation/SinSiege_Developer_Handover.pdf)
> — architecture, systems, the custom tools, and step-by-step workflows. Start there.
> It is generated from [`Documentation/build_handover_pdf.py`](Documentation/build_handover_pdf.py)
> (`python build_handover_pdf.py`, needs `reportlab`).

---

## Requirements

| | |
|---|---|
| **Engine** | Unity **6000.3.8f1** (open with this exact version via Unity Hub) |
| **Rendering** | Universal Render Pipeline 17.3 (2D Renderer) |
| **Audio** | FMOD Studio integration (`Assets/Plugins/FMOD`, `Assets/Fmod`) |
| **Other** | Cinemachine 3.1, Input System 1.18, TextMeshPro |

Packages restore automatically from `Packages/manifest.json`. The first import is slow (it
regenerates `Library/`).

## Running the game

1. Open the project root in Unity 6000.3.8f1.
2. Press **Play** from `Assets/Scenes/MainMenu.unity` for the full flow, or open
   `Assets/Scenes/Lust1.unity` to jump straight into gameplay.
3. Use **Scenes ▸ Open Scene List** (`Ctrl+G`) to switch scenes quickly.

Scenes: **MainMenu**, **Lust1** (main gameplay), **Gokhan Testing** (scratch — not shipped).

## Custom editor tools

All under the **Tools** / **Scenes** menus. Full usage is in the PDF (Section 4).

| Tool | Menu | What it does |
|---|---|---|
| **Sheet Fetcher** | `Tools ▸ Sheet Fetcher` | Syncs Upgrades / Characters / Towers balance data from Google Sheets into ScriptableObjects & tower prefabs. ⚠ Upgrades/Characters sync **deletes** assets not in the sheet. |
| **Localization Sync** | `Tools ▸ Localization` | Bakes UI text from a Google Sheet into `Resources/LocalizationData.asset`. |
| **Enemy Creator** | `Tools ▸ Enemy Creator` | Creates a new enemy as a prefab **variant** of a base, with stats + a generated looping animation (or an existing controller). |
| **Wave Editor** | `Tools ▸ Wave Editor` | Authors a level's **waves, path segments and routes**, editing the level prefab live in Prefab Mode. |
| **Scene Quick Access** | `Scenes ▸ Open Scene List` (`Ctrl+G`) | Jump between scenes in `Assets/Scenes`. |

## Project structure

```
Assets/
  Scripts/            Runtime gameplay code
    System/           Bootstrap & cross-cutting (LevelInitializer, GameState, MasterDictionary, ReferencesSO/Refs)
    Managers/         Per-scene services (GameManager, WaveManager, BuildManager, ...)
    Wave Related/     WaveSO + WaveSpawnData, WaveGroup + WaveSlot
    Data/             LevelData (routes/segments/waves), CharacterData
    Enemies/ Towers/ Mechanics/ UI/   Gameplay
    Sheets Related/   Localization runtime
    Editor/           Editor code shipped next to gameplay (Sheet Fetcher, Localization Sync, WaveSO inspector)
  Editor/             Standalone tools (Enemy Creator, Wave Editor, Scene Quick Access)
  Prefabs/            Enemies (by sin), Towers, Levels, Bullets, Coins, Player, UI, Persistent
  Resources/          Loaded by name at runtime: ReferencesSO, LocalizationData, Upgrades/, Characters/
  Scriptable Objects/ Authoring data: Waves/ (per-level WaveSO), Routes/
  Scenes/             MainMenu, Lust1, Gokhan Testing
Documentation/        This handover PDF + its generator
```

## Architecture in one paragraph

A `DontDestroyOnLoad` **PersistentManager** holds GameState / Localization / Save / Scene
managers across scenes. Each gameplay scene has a **LevelInitializer** that finds every scene
manager and calls their `Init()` in a deliberate order (the project uses an explicit `Init()`
pattern rather than relying on Awake/Start ordering), reads the chosen level index from GameState,
and hands the matching **LevelData** to the **WaveManager**. `LevelData` (on the level prefab root)
holds the routes, segment pool and wave groups the Wave Editor authors; the WaveManager caches
each route's path, runs the wave timer, spawns from `WaveSO` data (with route blocking and an
optional horde), and ends the level when the last wave group clears.

## Gotchas (the short list — see the PDF for the rest)

- **Sheet Fetcher is destructive** for Upgrades/Characters: any asset in the target folder not in
  the sheet is deleted. Keep those folders for synced data only.
- **Wave Editor edits the level prefab live** — save with `Ctrl+S` (or Auto Save). Watch the
  Saved/Unsaved badge.
- A **route's index is its position** in `LevelData.mapRoutes`; reordering routes rebinds wave slots.
- A new level's `LevelData` must be added to **`LevelInitializer.levelDatas`** to be playable.
- After making an enemy, run **Auto-Find All Enemies** on the `EnemyDatabase` asset.
- Known open item: enemies reaching the Base are currently destroyed **without dealing damage**
  (`// TODO` in `Enemy.cs`).

## Source control

Standard Unity `.gitignore` (`Library/`, `obj/`, `Logs/`, `Builds/`, generated `*.csproj`/`*.sln`
are ignored). **Git LFS is not configured** — large binaries are committed directly. Always commit
`.meta` files alongside their assets.
