# Scrap Frontier Asset-Flip Proof

Scrap Frontier proves that the Idle Auto Defense vertical slice can become a visibly different game through authored assets while retaining one gameplay runtime, controller stack, UI flow, persistence implementation, and setup architecture.

## Identity And Launch

- Named pack: `Scrap Frontier`
- Pack ID: `contentpack.idle-auto-defense.scrap-frontier`
- Content set ID: `contentset.idle-auto-defense.scrap-frontier.playable`
- Source root: `TemplateSource~/ScrapFrontierGame`
- Generated scene: `Assets/OPEN_THIS_TO_TEST_ScrapFrontier_PlayableGame/OPEN_THIS_TO_TEST_ScrapFrontier_PlayableGame.unity`
- Default generated content root for Scrap-only setup: `Assets/GameContent/IdleAutoDefense`
- Generated content root when Both is selected: `Assets/GameContent/IdleAutoDefense/ScrapFrontier`

From Deucarian Control Center > Authoring, run `Create Playable Game`, select `Scrap Frontier Only` or `Both`, create the game, open the Scrap Frontier scene, and press Play. The scene starts at the same menu-first player flow as Basic and binds Scrap Frontier before runtime startup.

## Shared Runtime

Scrap Frontier does not contain a gameplay controller, runtime assembly, or copied gameplay implementation. Its scene references the same generated thin bootstrap type as Basic, and that bootstrap delegates to:

- `IdleAutoDefenseTemplateController`
- `IdleAutoDefensePlayerExperienceController`
- the package-owned authored-core validators
- the package-owned persistence and UI flows

The only intentional reference from the Scrap source tree to the Basic source tree is the bootstrap script GUID used by the source scene. Setup remaps that GUID to one generated product bootstrap. All gameplay and player-facing assets remain pack-owned.

## Independent Authored Graph

Scrap Frontier independently owns:

- four attacks: Rivet Barrage, Arc Furnace, Sawblade Relay, and Salvage Mortar
- four mounted modules with matching Scrap-specific weapon IDs
- six enemies: Scrap Rusher, Wirejack, Plateback, Slag Caster, Foreman, and Foundry Titan
- seven waves: Yard Breach, Wirejack Rush, Salvage Surge, Plateback Break, Foreman Arrival, Scrapyard Storm, and Titan March
- six run-upgrade assets
- 37 live reward choices across unlock, Normal, Epic, Legendary, and base paths
- reward catalog, economy, currencies, run profile, progression, offline progression, and game rules
- content set, pack root, runtime presentation settings, and scene bindings

The initial numeric values intentionally match Basic. This keeps the proof about ownership and binding rather than a second balance pass. EditMode tests compare attacks, modules, enemies, waves, rewards, economy, run timing, progression shape, offline behavior, and objective pressure, then mutate Scrap assets to prove Basic is unaffected.

## Presentation

The pack uses a rust, charcoal, yellow, and teal foundry identity. It owns a Scrap Recycler objective, distinct module and projectile prefabs, six enemy prefabs, elite/boss scale and tint, foundry arena colors, impact/death presentation assignments, warning copy, icon tokens, and summary accents.

The art remains intentionally production-replaceable. Kenney CC0 geometry and clips are used as generic foundations where noted by `ThirdPartyNotices.md`; the concrete prefab compositions, materials, assignments, colors, IDs, and copy are independently authored. Replace those assets through the existing presentation references rather than adding a rendering system.

## Themes, Audio, Tutorial, And UI

Scrap Frontier owns two themes:

- `theme.idle-auto-defense.scrap-frontier.default` (`Scrap Frontier`)
- `theme.idle-auto-defense.scrap-frontier.molten-foundry` (`Molten Foundry`)

It also owns a 26-event audio palette with independent clip assignments, throttling values, a ten-step tutorial with unique stable step IDs, and UI settings for Scrap-specific title, Recycler terminology, Crew Rank, Salvage Ledger, victory/defeat copy, module tokens, and `Redline` in place of Overdrive.

Audio event keys remain shared semantic runtime events so the same controller can dispatch them. The palette asset and mappings are pack-owned and do not reference the Basic palette.

## Economy And Persistence

Scrap Frontier owns `scrap` and `cogs` currencies, five semantic purchase-cost IDs, starting balances, passive income, completion rewards, research tracks, save-document IDs, run rules, and offline accumulation values. Values begin at Basic parity.

Player state is pack-isolated. `IdleAutoDefensePlayerProfileStore` derives a document name from the active pack ID. Basic retains the legacy document name for compatibility; Scrap uses `idle-auto-defense-player-profile__contentpack-idle-auto-defense-scrap-frontier`. Theme selection, tutorial seen/skipped state, progression, settings, and offline claim timestamps cannot overwrite the other pack.

## Setup Behavior

The existing setup wizard supports:

- `Basic Only`: preserves the established paths and workflow
- `Scrap Frontier Only`: generates the complete Scrap graph and scene
- `Both`: generates sibling Basic/Scrap roots, both scenes, and one shared bootstrap
- `Repair Missing Content`: reuses existing destination GUIDs, skips byte-identical files, restores missing files, and blocks conflicting content unless overwrite is explicitly enabled

All copied asset GUIDs are remapped. The two source trees have no duplicate GUIDs, and generated scene/content references point to their selected pack. No manual reference reconstruction is required.

## Game Content Authoring

Game Content Authoring displays Basic and Scrap Frontier as separate named packs. Each projects 124 canonical records with its own pack ID and source identities. Validate, Reveal Source, and Open Playable Scene route to the selected pack. Safe editing is limited to the documented scalar fields, Weapon/Tower Attack reference, and Run Profile Waves sequence; each selector accepts only canonical compatible records from the same pack. Source claims prevent the same generated assets from appearing under Project Content.

Cross-pack validation compares selected-pack dependencies against other generated named-pack content roots and reports concrete leakage. The source-isolation test also rejects Basic source GUIDs in Scrap, except for the declared shared bootstrap script.

## Strict Binding

The Scrap scene assigns its own pack, content set, and player-experience root. Valid content reports authored core active and fallback false. Missing attack, enemy, wave, reward catalog, economy, run profile, progression, UI, or theme ownership fails validation. The runtime never substitutes Basic content for an invalid Scrap graph.

## Template Contract

This proof follows: "Extract only reusable infrastructure, never the playable vertical slice."

Both Basic Idle Auto Defense and Scrap Frontier remain complete playable games in the template. Runtime and authoring infrastructure are shared; concrete gameplay records, balance assets, presentation, copy, and persistence scope remain inside each playable pack. The proof strengthens future package-extraction decisions without requiring a generic clone UI or moving either vertical slice into infrastructure.

## Known Limits

- Presentation is polished placeholder art, not production art.
- Both scenes coexist, but pack switching is scene-based rather than an in-game pack selector.
- When Both is generated and setup opens a scene automatically, it opens Basic first; Scrap remains one click away at its visible scene path.
- Game Content Authoring supports the narrow documented field-editing surface; all other references and complex structures remain read-only.
- Generic Kenney resources and semantic audio event keys are shared foundations by design; concrete pack assignments are independent.
