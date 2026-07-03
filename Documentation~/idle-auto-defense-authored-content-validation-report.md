# Idle Auto Defense Authored Content Validation Report

Status: passing by design after the authored Pulse Beam mapping fix.

Menu command: `Deucarian > Idle Auto Defense > Validate Authored Content`

Primary playable content set:

- `contentset.idle-auto-defense.playable`
- Source path: `TemplateSource~/BasicIdleAutoDefenseGame/Content/ContentSets/contentset.idle-auto-defense.playable/contentset.idle-auto-defense.playable_GameContentSet.asset`
- Consumer import root: `Assets/GameContent/IdleAutoDefense`
- Playable scene after generation/import: `Assets/OPEN_THIS_TO_TEST_IdleAutoDefense_PlayableGame/OPEN_THIS_TO_TEST_IdleAutoDefense_PlayableGame.unity`

Weapon / Attack Visual Matrix:

| Weapon | Attack | Mode | Projectile Prefab | Beam Prefab | Cast/Fire VFX | Impact VFX | Result |
| ------ | ------ | ---- | ----------------- | ----------- | ------------- | ---------- | ------ |
| Shard Launcher | `attack.idle-auto-defense.shard-projectile` | Projectile | `KenneyShardProjectile` | None | `KenneyPlacementRingVfx`, `KenneyImpactBurstVfx` | `KenneyImpactBurstVfx` | Pass |
| Pulse Beam | `attack.idle-auto-defense.pulse-beam` | Hitscan | None | `PulseBeamVfx` | `PulseBeamVfx` | `KenneyImpactBurstVfx` | Pass |
| Arc Burst | `attack.idle-auto-defense.arc-burst` | Area | None | None | `KenneyPlacementRingVfx`, `KenneyImpactBurstVfx` | `KenneyImpactBurstVfx` | Pass |
| Homing Pulse | `attack.idle-auto-defense.homing-pulse` | Projectile | `KenneyHomingProjectile` | None | `KenneyPlacementRingVfx`, `KenneyImpactBurstVfx` | `KenneyImpactBurstVfx` | Pass |

Authored runtime presentation:

- Objective/core presentation: `objective.idle-auto-defense.core`, `Kenney 3D Core Base`, three authored Kenney model bindings.
- Module slot pads: four authored slot bindings, one per weapon/module, using `tile-spawn` model references and authored positions/tints.
- Runtime counters now distinguish authored objective/module-slot bindings from fallback bindings; the playable sample is expected to run with zero fallback counts.

Reward authoring matrix:

- Each of the four authored weapons has 3 normal rewards, 3 Epic rewards, and 1 Legendary reward.
- Epic rewards create visible behavior or readable spikes through authored reward effect kinds such as extra projectiles, extra beam targets, extra area hits, faster projectile travel, stronger damage numbers, or faster module cycles.
- Legendary rewards are `Crystal Tempest`, `Orbital Lance`, `Siege Barrage`, and `Carrier Hive`.

Future validation should remain strict: missing authored VFX is a content authoring blocker, not a reason for the playable scene to borrow Pulse Beam or primitive runtime art.
