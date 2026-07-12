# Game Content Authoring

## Named Pack

Running the existing Idle Auto Defense setup wizard generates the project-owned authored graph under `Assets/GameContent/IdleAutoDefense`. In `Tools > Deucarian > Game Content Authoring`, that graph appears as one named pack:

- Display name: `Basic Idle Auto Defense`
- Pack ID: `contentpack.idle-auto-defense.playable`
- Owner: `com.deucarian.template.game.idle-auto-defense`
- Persistence: read-only ScriptableObject graph

The generated `GameContentPackAsset`, its default `GameContentSetAsset`, and the set's referenced ScriptableObjects form the source-of-truth graph. The authoring integration does not create a GCA manifest, JSON mirror, content copy, or second database. The provider ignores private `TemplateSource~` assets and discovers only generated project content with the expected serialized pack ID.

## Lenses And Records

The pack exposes exactly the current authored gameplay graph:

| Lens | Records | Mapping |
|---|---:|---|
| Attack | 4 | First-class `AttackDefinitionAsset` records |
| Enemy | 6 | `EnemyDefinitionAsset` records; elite/boss capabilities come from authored role/tags |
| Wave / Encounter | 7 | `WaveDefinitionAsset` schedules and enemy composition |
| Weapon / Tower | 4 | One mounted `WeaponDefinitionAsset` identity shown through both capabilities |
| Upgrade | 6 | `RunUpgradeDefinitionAsset`; Weapon Upgrade only when a weapon target is authored |
| Reward Choice | 37 | 3 unlocks, 12 normal, 12 Epic, 4 Legendary, and 6 base choices from the reward catalog |
| Reward Table | 1 | Draft cadence, XP curve, rank gates, and rarity weights |
| Economy | 8 | One economy owner, two currency records, and five purchase cost curves |
| Run Profile | 1 | Fixed 20 Hz semantics, 5,600 ticks / 280 seconds, wave links, scaling, and outcome rules |
| Persistent Progression | 6 | One catalog, one account track, and four research nodes |
| Offline Progression | 1 | Production rates, cycle reward, cap, multiplier, rounding, and save key |
| Game Rules | 1 | Objective, module roles, elite/boss references, combat, repair, projectile, and Overdrive rules |

All Content shows 82 canonical records: the original 27 plus 55 authored-core projections. Nested records share their owning ScriptableObject source but retain distinct stable IDs and canonical record keys. Weapon-to-attack, wave-to-enemy, reward-to-weapon, cost-to-currency, run-profile-to-wave, progression prerequisite/target, and game-rule links resolve through pack references.

Raw cooldown and schedule ticks remain visible as ticks. The authored Run Profile declares fixed-rate semantics and 20 ticks per second, so its dashboard shows both 5,600 ticks and 280 seconds. No raw tick value is presented as seconds without that profile conversion.

## Project Content Ownership

The Idle provider claims the generated pack, content set, six authored-core owners, root records, and their authored companion section assets by Unity asset GUID. Claimed gameplay records are omitted from synthetic Project Content, so they do not appear twice or remain writable through an unrelated backend. Unclaimed ScriptableObjects keep the existing Project Content create/edit workflow.

If setup has not run, the named pack remains visible in a missing/generated-content state and offers the existing setup wizard. If multiple generated packs use the expected stable ID, discovery reports ambiguity and does not select one.

## Dashboard Actions

- **Validate** runs `GameContentPackValidator` and existing content-set/domain validation for the selected generated graph.
- **Reveal Source** selects and pings the generated `GameContentPackAsset`.
- **Open Playable Scene** resolves the generated scene that references the selected pack and opens it.
- **Open Setup Wizard** appears when generated content is missing and calls the existing setup command.

Browsing is read-only and must not dirty assets, prefabs, metadata, or scenes. Transactional editing is a later milestone.

## Current Limits

The named pack remains intentionally read-only; this milestone does not add transactional GCA editing or generic economy/progression lenses. UI theme, tutorial, audio palette, player-facing offline claim flow, menus, and final mobile layout remain later product-UX work. The gameplay core is authored and strict, but the template is not yet claimed as fully product-complete.
