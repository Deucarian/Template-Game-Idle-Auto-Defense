# Default Content And Balance

The private template source carries one neutral authored starter pack under:

```text
TemplateSource~/BasicIdleAutoDefenseGame/Content
```

The setup wizard copies this content into `Assets/GameContent/IdleAutoDefense` by default and remaps GUIDs. Scripts, docs, visuals, audio, and runtime resources go into the chosen generated game root, while the visible playable scene is generated at `Assets/OPEN_THIS_TO_TEST_IdleAutoDefense_PlayableGame/OPEN_THIS_TO_TEST_IdleAutoDefense_PlayableGame.unity`. Product teams should tune the generated authored assets, not the package source.

| Folder | Purpose |
| --- | --- |
| `Enemies` | Starter enemy definitions for the generated run. |
| `Attacks` | Direct and projectile attack recipes plus starter variations. |
| `Weapons` | Tower definitions wired to the attack recipes. |
| `Waves` | Spawn profiles used by the starter encounter. |
| `Upgrades` | Starter authored upgrade definitions used by content validation and product tuning. |
| `ContentSets` | Playable run recipe assigned by the generated scene, including reward draft settings/catalog, debug defaults, and Kenney turret presentation bindings. |
| `ContentPacks` | Wrapper assigned by the generated scene. |

The starter pack contains four generic attacks, six enemies, four tower weapons, seven spawn profiles, six authored upgrades, one content set, and one content pack. Starter tuning is a tight 3-5 minute vertical slice: weak Shard Launcher start, visible pressure, a first reward around 30-60 seconds, near-leaks and tower damage around the middle, then elite/boss spikes with stronger reward drafts. Use it to verify spawning, targeting, attacks, upgrades, elite/boss rewards, and save smoke before building a real product loop.

The playable sample also builds a runtime three-choice reward draft from the resolved tower weapons. Kills grant commander experience, wave completion contributes experience, elite kills always queue a reward, boss kills queue a stronger reward, and the controller guarantees an early reward prompt around the opening pressure beat if XP has not already produced one. Early level-up drafts prefer one module unlock, then fill the rest from owned weapon/base upgrades. The generated `GameContentSetAsset` owns the sample XP values, rarity weights, unlock weighting, progression thresholds, projectile retarget tolerance, actual unlock cards, normal weapon tracks, Epic tracks, Legendary tracks, and base reward entries. The controller consumes that assigned content set; fallback defaults exist only so isolated smoke tests fail gracefully instead of blocking the editor. Each owned weapon has three normal investments, three Epic investments unlocked after the normal track, and one Legendary investment unlocked after the Epic track by default.

The first-view runtime layout shows the core, the starter Shard Launcher, and three colored Kenney build pads for the unlockable Pulse Beam, Arc Burst, and Homing Pulse modules. Kenney 3D model names, mount offsets, muzzle positions, turn speeds, and debug visibility defaults are authored on the content set. Runtime code creates the presenters and consumes those bindings; it should not become a second tower visual catalog.

The authored content set deliberately starts with only 10 credits, no parts, and a four-minute session hint. Do not raise the starting economy or compress the wave schedule unless a live playtest still shows enemies surviving multiple hits, reaching near the core, and damaging the tower in a normal run.

The first-run feel depends on authored data and small sample runtime glue:

- Tune enemy HP, speed, reward value, and contact damage in `Enemies`.
- Tune tower range, cooldown, and starting weapon identity in `Weapons`.
- Tune attack damage, delivery mode, projectile speed, VFX, and audio in `Attacks`.
- Tune pressure timing, offscreen approach lanes, elite moments, and boss timing in `Waves`.
- Tune live starter upgrade costs/effects in `Upgrades` and reward draft tracks/rarity weights in the content set.
- Replace tower, enemy, projectile, impact, UI, and ground Kenney assets in the generated game root under `Resources/Kenney/IdleAutoDefense`.

The sample HUD includes explicit Damage, Fire Rate, Range, Repair, module unlock, and Overdrive buttons. Overdrive is a short credit spend for pressure moments; it is intentionally player-facing and replaces the older visible test reward buttons. Product games can keep this sample progression as glue or replace it after their authored `Assets/GameContent` pack has real balance.
