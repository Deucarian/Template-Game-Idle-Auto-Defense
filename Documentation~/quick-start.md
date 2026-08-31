# Idle Auto Defense Template Quick Start

1. Install `com.deucarian.template.game.idle-auto-defense`.
2. Open Deucarian Control Center > Authoring and run `Create Playable Game`.
3. Choose a target folder under `Assets`, a content folder under `Assets/GameContent`, a namespace, and a game prefix.
4. Open the generated scene.
5. Press Play.

In Play Mode, verify these starter pieces:

- central core/tower
- four perimeter spawn lane markers
- visible direct and projectile weapon mounts
- runtime-spawned enemies
- direct and projectile attacks
- HUD state, credits, parts, enemies, kills, projectiles, upgrades, objective hits, and save status
- Save Snapshot and Reset Save buttons
- `UsingAuthoredCore == true`, `FallbackModeActive == false`, and no strict-startup error

Tune the generated authored content under `Assets/GameContent/IdleAutoDefense` through `Tools > Deucarian > Authoring > Game Content...`. Rewards, economy, run timing, persistent progression, offline progression, and game rules are first-class ScriptableObjects referenced by the content set. Do not import a package sample; this template's official onboarding path is generated product-owned content.
