# Idle Auto Defense Content Source Audit

This sample treats Game Content Authoring assets as the canonical gameplay source. Runtime objects may be instantiated, pooled, aimed, moved, and destroyed, but player-visible combat art is expected to trace back to an authored weapon, attack, enemy, content set, or prefab reference.

Canonical flow:

`Game Content Authoring definition -> authored prefab/VFX/material/socket references -> content set/catalog -> runtime instance -> AuthoredContentInstance source stamp`

Runtime owns gameplay execution. It does not own the canonical weapon-to-VFX mapping, reward definitions, wave definitions, or final balance tables for the playable sample.

| Weapon | Attack Mode | Expected VFX | Actual VFX Before | Actual VFX After | Source | Fixed? |
| ------ | ----------- | ------------ | ----------------- | ---------------- | ------ | ------ |
| Shard Launcher (`weapon.template.shard-launcher`) | Projectile (`attack.template.shard-launcher`) | Authored shard projectile, authored non-beam cast/fire burst, authored impact burst | `PulseBeamVfx` was referenced by `OnCast` and `OnFire` in `attack.template.fire-orb_Presentation.asset` | `template-placement-vfx` on cast, `template-impact-vfx` on fire/impact, `template-projectile` delivery prefab | Authored attack presentation and delivery under `TemplateSource~/BasicIdleAutoDefenseGame/Content/Attacks/attack.template.fire-orb` | Yes |
| Pulse Beam (`weapon.template.pulse-cannon`) | Beam / hitscan (`attack.template.pulse-cannon`) | Authored `PulseBeamVfx` from muzzle to target plus authored impact feedback | Correctly used `PulseBeamVfx` in delivery and presentation | Still uses `PulseBeamVfx`, restricted to this authored beam attack | Authored attack presentation and delivery under `TemplateSource~/BasicIdleAutoDefenseGame/Content/Attacks/attack.template.hitscan-beam` | Yes |
| Arc Burst / Arc Cannon (`weapon.template.arc-burst-tower`) | Area / splash (`attack.template.arc-burst`) | Authored non-beam cast/fire burst and authored area/impact burst | `PulseBeamVfx` was referenced by `OnCast` and `OnFire` in `attack.template.arc-burst_Presentation.asset` | `template-placement-vfx` on cast, `template-impact-vfx` on fire/impact, no beam prefab in delivery | Authored attack presentation and delivery under `TemplateSource~/BasicIdleAutoDefenseGame/Content/Attacks/attack.template.arc-burst` | Yes |
| Homing Pulse / Homing Spire (`weapon.template.homing-spire`) | Homing projectile (`attack.template.homing-pulse`) | Authored seeker projectile and authored non-beam cast/fire/impact feedback | `PulseBeamVfx` was referenced by `OnCast` and `OnFire` in `attack.template.homing-pulse_Presentation.asset` | `template-placement-vfx` on cast, `template-impact-vfx` on fire/impact, `template-seeker-projectile` delivery prefab | Authored attack presentation and delivery under `TemplateSource~/BasicIdleAutoDefenseGame/Content/Attacks/attack.template.homing-pulse` | Yes |

Current authored weapon equivalents:

- Cannon/artillery equivalent: Arc Burst / Arc Cannon.
- Drone/support/resource equivalent: Homing Pulse / Homing Spire as the seeker/support cleanup module.
- Frost/control and tesla/chain are not separate authored weapons in this vertical slice yet. Validation now fails if a non-beam attack uses `PulseBeamVfx`; future frost/tesla/drone assets must enter through authored attack, weapon, presentation, and content set references before appearing in the playable scene.

Validation guardrails added:

- Projectile attacks must reference an authored projectile prefab, an authored impact prefab, and no beam prefab.
- Beam attacks must reference an authored beam prefab and must not also carry a projectile prefab.
- Area/aura/direct attacks must not use beam VFX unless authored as beam delivery.
- PulseBeamVfx is restricted to authored beam attacks.
- Any non-beam attack presentation event using `PulseBeamVfx` is a content validation error.
- Attack `OnFire` and `OnImpact` events must have authored VFX so the playable sample does not fall back to runtime-generated combat art.
- Runtime-visible spawned weapons, projectiles, enemies, beam VFX, and attack/enemy VFX are stamped with `AuthoredContentInstance` when they come from authored content.
