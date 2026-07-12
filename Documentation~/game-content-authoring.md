# Game Content Authoring

## Named Pack

Running the existing Idle Auto Defense setup wizard generates the project-owned authored graph under `Assets/GameContent/IdleAutoDefense`. In `Tools > Deucarian > Game Content Authoring`, that graph appears as one named pack:

- Display name: `Basic Idle Auto Defense`
- Pack ID: `contentpack.idle-auto-defense.playable`
- Owner: `com.deucarian.template.game.idle-auto-defense`
- Persistence: read-only ScriptableObject graph

The generated `GameContentPackAsset` and its default `GameContentSetAsset` remain the source of truth. The authoring integration does not create a GCA manifest, JSON mirror, content copy, or second database. The provider ignores private `TemplateSource~` assets and discovers only generated project content with the expected serialized pack ID.

## Lenses And Records

The pack exposes exactly the current authored gameplay graph:

| Lens | Records | Mapping |
|---|---:|---|
| Attack | 4 | First-class `AttackDefinitionAsset` records |
| Enemy | 6 | `EnemyDefinitionAsset` records; elite/boss capabilities come from authored role/tags |
| Wave / Encounter | 7 | `WaveDefinitionAsset` schedules and enemy composition |
| Weapon / Tower | 4 | One mounted `WeaponDefinitionAsset` identity shown through both capabilities |
| Upgrade | 6 | `RunUpgradeDefinitionAsset`; Weapon Upgrade only when a weapon target is authored |

All Content shows the same 27 canonical records. A record key combines the owning package, pack, Unity asset GUID source identity, and authored stable ID. Weapon-to-attack, wave-to-enemy, and upgrade-to-target links use canonical pack references. Projectile IDs remain authored delivery metadata because this milestone does not create separate projectile records.

Raw cooldown and schedule ticks remain visible as ticks. The common Attack and Weapon previews use the template's existing nominal 20 Hz presentation conversion while retaining the raw authored value. Defense waves are not presented as Survivors run profiles, and `GameContentSetAsset.SessionLengthTicks` is not advertised as an authoritative run duration.

## Project Content Ownership

The Idle provider claims the generated pack, content set, root records, and their authored companion section assets by Unity asset GUID. Claimed gameplay records are omitted from synthetic Project Content, so they do not appear twice or remain writable through an unrelated backend. Unclaimed ScriptableObjects keep the existing Project Content create/edit workflow.

If setup has not run, the named pack remains visible in a missing/generated-content state and offers the existing setup wizard. If multiple generated packs use the expected stable ID, discovery reports ambiguity and does not select one.

## Dashboard Actions

- **Validate** runs `GameContentPackValidator` and existing content-set/domain validation for the selected generated graph.
- **Reveal Source** selects and pings the generated `GameContentPackAsset`.
- **Open Playable Scene** resolves the generated scene that references the selected pack and opens it.
- **Open Setup Wizard** appears when generated content is missing and calls the existing setup command.

Browsing is read-only and must not dirty assets, prefabs, metadata, or scenes. Transactional editing is a later milestone.

## Current Limits

The live reward-card catalog, persistent progression, offline accumulation, parts of the economy, run timing, UI theme, tutorial, and mobile layout are not projected as authored records because they remain hardcoded or disconnected from authoritative runtime inputs. Invalid generated sample-critical content may still activate runtime fallback. Strict startup hardening and converting those systems into authored sources belong to the next content-hardening milestone; this integration does not claim that the template is fully asset-flippable yet.
