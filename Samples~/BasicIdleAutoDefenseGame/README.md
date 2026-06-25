# Basic Idle Auto Defense Game

Open `Scenes/BasicIdleAutoDefenseGame.unity` after importing the sample from Package Manager, or use:

```text
Tools > Deucarian > Templates > Idle Auto Defense > Open Starter Scene
```

The scene contains a bootstrap object that creates:

- a central core objective
- four visible perimeter spawn markers
- one visible enemy placeholder preview
- one Pulse Cannon direct weapon mount
- one Shard Launcher projectile weapon mount
- First Orbit, Pressure Ring, Boss Pulse, and Endless placeholder content definitions
- a `Basic Idle Auto Defense Content Set` assigned to the scene bootstrap
- deterministic run upgrade drafts
- save/load, offline reward, mock monetization offers, progression reward, sample save reset, and corrupted save recovery smoke paths
- a small on-screen status panel with save/reset buttons

All visible gameplay objects are primitive placeholders. Replace them with real content in the `Prefabs` and `Content` folders when turning the template into a production game.

## Folder Map

```text
Basic Idle Auto Defense Game
├── Content
│   ├── DefaultBalance
│   ├── DefaultStages
│   ├── DefaultEnemies
│   ├── DefaultWeapons
│   ├── DefaultWaves
│   ├── DefaultUpgrades
│   ├── DefaultProgression
│   ├── DefaultMonetization
│   ├── ContentSets
│   ├── ContentPacks
│   ├── RuntimeEnemies
│   ├── RuntimeWaves
│   └── starter-content.json
├── Prefabs
│   ├── Enemies
│   ├── Projectiles
│   ├── Weapons
│   └── README.md
├── Scenes
│   └── BasicIdleAutoDefenseGame.unity
├── Scripts
│   └── BasicIdleAutoDefenseGameBootstrap.cs
└── Tests
    └── BasicIdleAutoDefenseGameSampleTests.cs
```

## First Edits

1. Prefer `Tools > Deucarian > Templates > Idle Auto Defense > Create Game From Template` for a product-owned folder.
2. Use the generated `Docs/asset-flip-checklist.md` and `Docs/setup-report.md`.
3. Replace IDs and tuning values in `Content/starter-content.json` plus the focused `Default*` files.
4. Replace the generated placeholder primitives with prefabs under `Prefabs`.
5. Keep Deucarian package source in packages; only the generated product folder should contain product code and content.

The assigned content set lives under `Content/ContentSets`, and the one-click setup pack lives under `Content/ContentPacks`. It references the authored sample attacks, runtime enemies, runtime waves, towers/weapons, and upgrades, and proves the template can run from a single game recipe instead of only per-type fallback arrays. `Content/Enemies` and `Content/Waves` are legacy authoring samples with distinct IDs so the Content Library remains clean after import. Sample audio is intentionally optional; missing audio or VFX references are expected to preview as unassigned rather than as errors.
