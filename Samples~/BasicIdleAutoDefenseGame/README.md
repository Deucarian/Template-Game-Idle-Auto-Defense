# Basic Idle Auto Defense Game

Open `Scenes/BasicIdleAutoDefenseGame.unity` after importing the sample from Package Manager, or use:

```text
Tools > Deucarian > Templates > Idle Auto Defense > Open Starter Scene
```

The scene contains a bootstrap object that creates:

- a central core objective
- four visible perimeter spawn markers
- one visible enemy placeholder preview
- one direct weapon mount
- one projectile weapon mount
- deterministic run upgrade drafts
- save/load, offline reward, mock monetization offers, progression reward, sample save reset, and corrupted save recovery smoke paths
- a small on-screen status panel with save/reset buttons

All visible gameplay objects are primitive placeholders. Replace them with real content in the `Prefabs` and `Content` folders when turning the template into a production game.

## Folder Map

```text
Basic Idle Auto Defense Game
├── Content
│   ├── DefaultBalance
│   ├── DefaultEnemies
│   ├── DefaultWeapons
│   ├── DefaultWaves
│   ├── DefaultUpgrades
│   ├── DefaultProgression
│   ├── DefaultMonetization
│   └── starter-content.json
├── Prefabs
│   └── README.md
├── Scenes
│   └── BasicIdleAutoDefenseGame.unity
├── Scripts
│   └── BasicIdleAutoDefenseGameBootstrap.cs
└── Tests
    └── BasicIdleAutoDefenseGameSampleTests.cs
```

## First Edits

1. Duplicate this sample folder outside `Assets/Samples`.
2. Rename the scene and assembly definition.
3. Replace IDs and tuning values in `Content/starter-content.json`.
4. Replace the generated placeholder primitives with prefabs under `Prefabs`.
5. Move copied starter code into your own namespace before deleting the template package.
