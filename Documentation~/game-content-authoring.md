# Game Content Authoring

## Named Pack

Running the existing Idle Auto Defense setup wizard generates project-owned authored graphs under `Assets/GameContent/IdleAutoDefense`. In `Tools > Deucarian > Game Content Authoring`, two named pack identities are available:

- Display name: `Basic Idle Auto Defense`
- Pack ID: `contentpack.idle-auto-defense.playable`
- Owner: `com.deucarian.template.game.idle-auto-defense`
- Persistence: staged project-owned ScriptableObject field editing

- Display name: `Scrap Frontier`
- Pack ID: `contentpack.idle-auto-defense.scrap-frontier`
- Owner: `com.deucarian.template.game.idle-auto-defense`
- Persistence: staged project-owned ScriptableObject field editing

The generated `GameContentPackAsset`, its default `GameContentSetAsset`, and the set's project-owned ScriptableObjects form the source-of-truth graph. The authoring integration does not create a GCA manifest, JSON mirror, content copy, or second database. The provider ignores private `TemplateSource~` assets and discovers only generated project content with the expected serialized pack ID. Persisted `AttackDefinitionAsset` roots are discovered from the selected pack's exact content root, independently of current weapon references, so an unreferenced former target remains a canonical authored Attack record.

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
| Player Experience | 1 | Root references for the complete player-facing presentation graph |
| Theme | 2 | Bastion Command default and Neon Bastion alternate color/style tokens |
| Audio Palette | 1 | Event-to-clip/category/volume/throttle owner |
| Audio Event | 26 | UI, gameplay, warning, reward, victory/defeat, summary, and offline claim events |
| Tutorial Definition | 1 | First-run tutorial owner |
| Tutorial Step | 10 | Stable IDs, player copy, and optional focus targets |
| UI Settings | 1 | Title/ability copy, module tokens, safe-area policy, touch size, and breakpoints |

Each generated pack shows 124 canonical records: 82 gameplay/core records plus 42 presentation records. Canonical keys include the selected pack ID, so Basic and Scrap records remain independent even when both are generated. Nested records share their owning ScriptableObject source but retain distinct stable IDs and canonical record keys. Gameplay links and player-experience-to-presentation links resolve inside the selected pack.

Raw cooldown and schedule ticks remain visible as ticks. The authored Run Profile declares fixed-rate semantics and 20 ticks per second, so its dashboard shows both 5,600 ticks and 280 seconds. No raw tick value is presented as seconds without that profile conversion.

## Project Content Ownership

The Idle provider claims the generated pack, content set, six authored-core owners, presentation owners, root records, and companion assets by Unity asset GUID. Claimed records are omitted from synthetic Project Content, so they do not appear twice or remain writable through an unrelated backend.

If setup has not run, each named pack remains visible in a missing/generated-content state and offers the existing setup wizard. If multiple generated packs use the same expected stable ID, discovery reports ambiguity and does not select one. Cross-pack validation rejects concrete dependencies on another generated named-pack content root.

## Dashboard Actions

- **Validate** runs `GameContentPackValidator` and existing content-set/domain validation for the selected generated graph.
- **Reveal Source** selects and pings the generated `GameContentPackAsset`.
- **Open Playable Scene** resolves the generated scene that references the selected pack and opens it.
- **Open Setup Wizard** appears when generated content is missing and calls the existing setup command.

Browsing remains non-mutating. For a supported record, **Edit Existing** opens the shared GCA transaction workbench without dirtying the source asset.

## Safe Field Editing

Editing is deliberately limited to one standalone section asset per transaction:

| Record | Writable fields | Serialized owner |
|---|---|---|
| Attack | cooldown ticks, range, damage | `AttackMechanicsDefinitionAsset` |
| Enemy | maximum health, move speed, reward value, contact damage, collision radius | `EnemyStatsDefinitionAsset` |
| Weapon / Tower | cooldown ticks, range, burst count, volley count, spread degrees, build cost, Attack reference | `WeaponStatsDefinitionAsset` |
| Run Upgrade | rarity, draft weight, maximum rank | `RunUpgradeEconomyDefinitionAsset` |
| Run Profile | ordered Waves collection | `IdleAutoDefenseRunProfileAsset` |

The provider uses explicit field IDs and `SerializedProperty` paths; it never exposes an arbitrary serialized property tree or raw Unity object picker. A missing path, mismatched property type, invalid value type, non-finite number, or out-of-range value disables or rejects that edit safely.

`Weapon / Tower -> Attack` is an editable one-to-one record reference. Its selector is populated from canonical Attack records in the directly selected named pack. A target must be a persistent exact-type `AttackDefinitionAsset` under that pack's exact content root, have one matching canonical record and one provider-owned source claim, pass Attack validation, and match the weapon's delivery domain: projectile weapons require Projectile attacks, while direct weapons require non-Projectile attacks. Null, transient, scene, foreign-root, cross-pack, ambiguous, broken, or incompatible targets are rejected rather than serialized.

`Run Profile -> Waves` is the only editable ordered collection. It contains canonical persistent `WaveDefinitionAsset` references from the directly selected Basic or Scrap pack, requires at least one Wave, disallows nulls and duplicates, and treats order as the runtime encounter sequence. The generic workbench provides Add, Remove, Move Up/Down, Replace, Open Wave, Restore Original Order, staged Undo/Redo, Preview, Commit, Cancel, and post-Commit Rollback. Removing a reference does not delete the independent Wave record; adding one does not create a Wave record. A committed sequence requires a run-profile rebind and run restart.

Persistent Waves are indexed from the selected pack's exact generated content root, not only from the current run-profile sequence. All seven authored Wave records therefore remain canonical and selectable when one is temporarily unreferenced. Candidate selection and Preview/Commit revalidate exact type, canonical identity, selected-pack ownership, unique source claim, Wave capability, Wave content, enemy links, and complete strict pack integrity.

The workbench captures the asset GUID, `GlobalObjectId`, normalized path, exact file SHA-256, dependency hash, mapped values, selected pack, canonical record, and backend schema. Apply, in-session Undo/Redo, Preview, and Cancel only change staged memory. Preview resolves and revalidates staged reference targets, substitutes in-memory clones into the complete selected pack, and runs the existing strict validators. Errors block Commit and warnings require explicit confirmation.

Commit rechecks the revision, source policy, canonical target, source claim, validation state, and delivery compatibility immediately before writing only the whitelisted fields to the same object in one Unity Undo group. It validates the actual graph, saves only that source asset, imports it, and reindexes GCA. Unity Undo/Redo keeps the GUID and references and triggers another reindex. Explicit Rollback uses a new named Undo group and restores the exact captured source bytes only when the current revision still equals the committed revision; a later Inspector edit, setup repair, regeneration, import, target removal, or dependency change makes the session stale and prevents overwrite.

Only writable generated assets under `Assets` that are claimed exclusively by the directly selected named pack qualify. Template sources, installed packages, `PackageCache`, `Library`, `Temp`, traversal/reparse paths, missing/read-only sources, All Packs, and Project Content through this backend remain read-only. Basic and Scrap have distinct GUID-backed sources and locks, so editing one cannot mutate the other.

## Current Limits

Stable IDs, every record reference except `WeaponStatsDefinitionAsset._attack` and `IdleAutoDefenseRunProfileAsset._waves`, other Unity object fields, tags, all other arrays/lists/maps, wave schedules and entry rows, reward choices, economy collections, progression nodes, offline resource links, presentation, themes, audio events, tutorials, UI settings, creation, duplication, deletion, bulk edits, and pack cloning remain read-only. Setup refresh or repair must finish before a new edit session begins; if it or another source/dependency edit occurs during a session, the source becomes stale and must be discarded/reopened rather than merged.

ScriptableObjects remain the source of truth. No JSON mirror, duplicate ScriptableObject, temporary asset, second content database, generic CRUD, or gameplay runtime copy is created. JSON and complex nested editing are separate deferred milestones.
