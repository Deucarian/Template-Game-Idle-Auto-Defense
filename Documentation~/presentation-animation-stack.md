# Idle Auto Defense Presentation Stack

Idle Auto Defense now contains the first local implementation of a Deucarian presentation/animation layer. It is intentionally concrete and sample-owned: the goal is to prove turret aiming, muzzle-origin firing, recoil, muzzle flash, enemy facing, hit feedback, and death feedback in the playable template before extracting reusable packages.

The curated Kenney Tower Defense Kit subset is intentionally present in two places:

- `TemplateSource~/BasicIdleAutoDefenseGame/Resources/Kenney/IdleAutoDefense/Models/TowerDefenseKit` is copied into generated product games.
- `Runtime/Resources/Kenney/IdleAutoDefense/Models/TowerDefenseKit` is importable by Unity while the local package sample/runtime controller is validated directly from the package.

## Ownership Boundaries

- UI animation belongs with UI systems and runtime UI Toolkit code.
- Sprite presentation should stay separate from 3D model presentation.
- Combat remains rules-focused: damage, health, targeting results, and kill outcomes stay there.
- Weapon Systems can provide fire and target intent; 3D yaw/pitch, recoil, muzzle flash, and model-specific animation are presentation-side.
- Projectiles own launch/move/impact rules; projectile origins should use presentation muzzle transforms when a weapon binding exists.
- Auto Defense composes mounted weapons, objective defense, enemy selection, and spawn pressure for this genre.
- Idle Auto Defense currently owns the local 3D bindings and presenters.

Future extraction candidates are `Weapon-Presentation` or `Presentation-Animation`, but only after this pattern proves useful in this template and at least one more playable template/sample.

## New Tower Model Setup

1. Create a tower root GameObject at the mount position.
2. Add or assign a yaw pivot transform.
3. Add a pitch/barrel pivot if the model needs one; the current slice uses yaw-only weapons.
4. Add a muzzle transform at the projectile/fire origin.
5. Assign a projectile model and muzzle flash behavior.
6. Tune turn speed, recoil distance, and flash color on the visual binding.
7. Connect the binding to the weapon or attack ID used by the runtime.
8. Open `Assets/OPEN_THIS_TO_TEST_IdleAutoDefense_PlayableGame/OPEN_THIS_TO_TEST_IdleAutoDefense_PlayableGame.unity`, press Play, and verify the turret tracks, recoils, flashes, and fires from the muzzle.

The starter mappings use Kenney Tower Defense Kit models:

- Shard Launcher: round base plus ballista, arrow projectile.
- Pulse Beam: square base plus turret, bullet-style muzzle feedback.
- Arc Burst: round base plus catapult, boulder-style impact.
- Homing Pulse: square base plus cannon, cannonball projectile.
- Enemies: UFO variants, with larger elite and boss variants.
- Arena: Kenney 3D tiles, dirt approach lanes, spawn markers, rocks, trees, and crystals.
