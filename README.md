# Deucarian Template Game - Idle Auto Defense

Package ID: `com.deucarian.template.game.idle-auto-defense`

Namespace: `Deucarian.TemplateGameIdleAutoDefense`

Version: `0.1.0`

This package provides a starter idle auto-defense game template built on `com.deucarian.auto-defense-suite`. It owns starter-game application glue and sample content only; reusable gameplay systems stay in the lower Deucarian packages delivered through the suite.

## What It Includes

- Central objective with health, lives, damage, failure, and terminal state handling.
- Four-way perimeter spawning.
- Enemy movement toward the objective.
- Recipe-backed direct hitscan, projectile, and homing/status projectile attack examples.
- Enemy death, objective contact damage, encounter completion, and encounter failure paths.
- Deterministic upgrade draft with at least three choices.
- Offline reward calculation.
- Progression currency reward application.
- Save/load smoke coverage for profile, run, and settings data.
- Corrupted primary save recovery and migration smoke coverage.
- Primitive placeholder visuals for the core, mounts, enemies, and projectiles.
- Attack recipe consumption through `AttackDefinitionAsset` conversion into Combat, Attacks, Projectiles, and Weapon Systems runtime definitions.

## Import Workflow

1. Add the package from the Package Manager using a local path or Git URL.
2. Ensure `com.deucarian.auto-defense-suite` resolves. For local validation projects, keep explicit local file references to lower Deucarian packages when the suite registry entries are not yet published.
3. Import the `Basic Idle Auto Defense Game` sample.
4. Open `Assets/Samples/Deucarian Template Game - Idle Auto Defense/0.1.0/Basic Idle Auto Defense Game/Scenes/BasicIdleAutoDefenseGame.unity`.
5. Enter Play Mode.

## Attack Recipe Sample

The template creates three attack recipes when no serialized recipes are assigned to `IdleAutoDefenseTemplateController`:

- `attack.template.hitscan-beam`: direct hitscan-style beam attack.
- `attack.template.fire-orb`: projectile attack using `projectile.template.fire-orb`.
- `attack.template.homing-pulse`: homing-style projectile metadata with `status.template.slow`.

The controller converts these recipes into `AttackDefinition`, `ProjectileDefinition`, and `CombatCatalog` entries at runtime. It also invokes OnFire/OnImpact presentation events and runs a small status hook smoke path so missing optional audio/VFX never blocks gameplay.

To replace the transient recipes with persistent assets, create them through `Deucarian/Game Content Authoring` and assign them to `IdleAutoDefenseTemplateController`. The assigned set must include the three template attack IDs above because the starter weapon modules reference those IDs. Empty, duplicate, invalid, or incomplete assigned sets are ignored with a warning and the controller falls back to the built-in transient recipes.

## Dependency Graph

```text
com.deucarian.template.game.idle-auto-defense
└── com.deucarian.auto-defense-suite
    ├── com.deucarian.gameplay-foundation
    ├── com.deucarian.persistence
    ├── com.deucarian.progression
    ├── com.deucarian.combat
    ├── com.deucarian.encounters
    ├── com.deucarian.world-spawning
    ├── com.deucarian.world-navigation
    ├── com.deucarian.defense-games
    ├── com.deucarian.attacks
    ├── com.deucarian.projectiles
    ├── com.deucarian.weapon-systems
    ├── com.deucarian.auto-defense
    ├── com.deucarian.run-upgrades
    └── com.deucarian.idle-progression
```

The template package declares only the suite dependency. Local validation projects may include explicit file references to lower packages so Unity can resolve unpublished suite dependencies from the workspace.

## Tests

Package tests are under `Tests/EditMode` and `Tests/PlayMode`. The sample also includes smoke tests under `Samples~/BasicIdleAutoDefenseGame/Tests` for projects that import the sample.
