# Deucarian Template Game - Idle Auto Defense

Package ID: `com.deucarian.template.game.idle-auto-defense`

Version: `0.1.4`

This package creates a product-owned idle auto-defense game folder. It owns template glue, setup helpers, starter authored content, and smoke coverage. Reusable gameplay systems stay in lower Deucarian packages.

No Unity Package Manager sample import is required. The private template sources live under `TemplateSource~/BasicIdleAutoDefenseGame` and `TemplateSource~/ScrapFrontierGame` so the setup wizard can create product-owned files. Scrap Frontier is a complete asset-flip proof, not runtime infrastructure.

## Quick Start

1. Install the template package.
2. Open Deucarian Control Center > Authoring and run `Create Playable Game`.
3. Choose Basic only, Scrap Frontier only, or Both, plus a target folder under `Assets`, a content folder under `Assets/GameContent`, a namespace, and a game prefix.
4. Open `Assets/OPEN_THIS_TO_TEST_IdleAutoDefense_PlayableGame/OPEN_THIS_TO_TEST_IdleAutoDefense_PlayableGame.unity`.
5. Press Play.

Scrap Frontier launches from `Assets/OPEN_THIS_TO_TEST_ScrapFrontier_PlayableGame/OPEN_THIS_TO_TEST_ScrapFrontier_PlayableGame.unity`. When Both is selected, Basic and Scrap content are generated into sibling `Basic` and `ScrapFrontier` folders and both scenes share the same generated bootstrap type and package runtime.

The generated scene opens on a player-facing main menu. Start Run launches the assigned 4:40 authored profile; the run includes a responsive HUD, four mounted-module controls, Overdrive, three-card reward drafts, pause/build/settings, elite and boss bars, offscreen threat markers, victory/defeat summaries, persistent research, and one-time authored offline claims.

Controls:

- `Esc`: pause/resume
- `Tab` or `B`: open Current Build
- `Space`: activate Overdrive when available
- `1`, `2`, `3`: choose reward cards
- `F1`: toggle the hidden debug panel

## Generated Game

The created folder includes:

- `Scripts`: a thin renamed bootstrap that binds strict gameplay and player-experience assets.
- `Prefabs`, `Visuals`, `Audio`, and `Resources`: starter Kenney CC0 sample assets for the playable look, plus product-owned locations for future art swaps.
- `Docs`: setup report and asset-flip checklist.

The playable scene is generated in a fixed top-level folder so it is easy to find:

- `Assets/OPEN_THIS_TO_TEST_IdleAutoDefense_PlayableGame/OPEN_THIS_TO_TEST_IdleAutoDefense_PlayableGame.unity`

The authored gameplay data is created separately under `Assets/GameContent/IdleAutoDefense` by default:

- `Attacks`, `Enemies`, `Weapons`, `Waves`, and `Upgrades`: editable starter definitions.
- `Rewards`, `Economy`, `RunProfiles`, `Progression`, `OfflineProgression`, and `GameRules`: the live gameplay-core catalogs and tuning consumed by runtime.
- `Presentation`: player-experience root, mobile UI settings, ten-step tutorial, two themes, and the audio event palette.
- `ContentSets`: the playable run recipe assigned by the generated scene.
- `ContentPacks`: the package-style wrapper assigned by the generated scene.

The generated scene references the generated content pack and content set. Its bootstrap enables strict authored startup. A valid run reports `UsingAssignedContentSet == true`, `UsingAuthoredCore == true`, and `FallbackModeActive == false`; incomplete required content blocks startup instead of substituting hidden balance.

The generated graphs also appear in `Tools > Deucarian > Authoring > Game Content...` as the named packs `Basic Idle Auto Defense` and `Scrap Frontier`. Each exposes 124 records including gameplay, Player Experience, Themes, Audio Events, Tutorial Steps, and UI Settings. A narrow set of direct attack, enemy, mounted-weapon, and run-upgrade scalars can be staged and committed on claimed project assets; all other records and structural fields remain read-only. See [Game Content Authoring](Documentation~/game-content-authoring.md) and the [Scrap Frontier asset-flip proof](Documentation~/scrap-frontier-asset-flip.md).

## Template Source

The package-owned source lives at:

```text
TemplateSource~/BasicIdleAutoDefenseGame
|-- Content
|   |-- Attacks
|   |-- ContentPacks
|   |-- ContentSets
|   |-- Economy
|   |-- Enemies
|   |-- GameRules
|   |-- OfflineProgression
|   |-- Presentation
|   |-- Progression
|   |-- Rewards
|   |-- RunProfiles
|   |-- Upgrades
|   |-- Waves
|   `-- Weapons
|-- Prefabs
|-- Resources
|   `-- Kenney
|-- Scenes
|   `-- OPEN_THIS_TO_TEST_IdleAutoDefense_PlayableGame.unity
|-- Scripts
|   `-- BasicIdleAutoDefenseGameBootstrap.cs
|-- Visuals
`-- Audio
```

This source is not a public package sample. It is copied by the setup wizard with product-owned namespaces, assembly names, scene references, and remapped GUIDs.

`TemplateSource~/ScrapFrontierGame` has the same authored ownership shape with independent IDs, content assets, presentation prefabs, themes, audio palette, tutorial, UI copy, economy, progression, and scene binding. It deliberately reuses the Basic thin bootstrap source and the package runtime instead of copying gameplay implementation.

## Editing Content

Open `Tools > Deucarian > Authoring > Game Content...` to browse, validate, reveal, navigate, and safely stage approved edits on generated assets under `Assets/GameContent/IdleAutoDefense` or the content root chosen in the setup wizard. Select `Basic Idle Auto Defense` or `Scrap Frontier` directly, then open an Attack, Enemy, Weapon/Tower, Upgrade, or Run Profile record. The workbench keeps Apply and its in-session Undo/Redo outside the live asset until Commit, validates a cloned proposed pack, and commits only the physical source asset in one Unity Undo group.

Cancel changes no source bytes. After Commit, normal Unity Undo/Redo reindexes the named pack, and the workbench's explicit Rollback restores the captured original source only while the committed revision is still current. Setup repair, regeneration, manual Inspector edits, or any other source/dependency change makes the session stale and blocks Commit or Rollback; cancel and reopen the record after that operation.

The approved fields are attack cooldown/range/damage; enemy health/speed/reward/contact damage/collision radius; mounted-weapon cooldown/range/burst/volley/spread/build cost and canonical Attack reference; run-upgrade rarity/weight/max rank; and the Run Profile's ordered Waves collection. Waves supports Add, Remove, Move, Replace, and Restore Original Order with at least one item, no nulls or duplicates, and canonical targets from the selected pack only. Order determines the runtime encounter sequence, and a committed change requires a run-profile rebind/run restart. Removing a reference never deletes a Wave asset, and adding one never creates a Wave asset.

Stable IDs, display metadata on record roots, other object references, tags, wave schedules and entries, content-set arrays, rewards, economy collections, progression, themes, audio, tutorial, and UI structures remain read-only. Editing requires a writable project-owned asset claimed only by the selected named pack. `TemplateSource~`, packages, Project Content through this backend, JSON, and arbitrary paths are never write targets.

Generated ScriptableObjects remain the only source of truth. Editing creates no JSON mirror, replacement asset, or duplicate content database; unclaimed standalone ScriptableObjects retain the existing Project Content behavior.

During play, the controller turns kills, wave progress, elite kills, boss kills, and a guaranteed early run moment into a visible three-choice reward draft. Choices, prerequisites, source eligibility, XP cadence, and rarity tables come from the reward-catalog asset. Economy, run/session rules, persistent progression, offline accumulation, objective/module rules, themes, tutorial copy, UI tokens, and audio event mappings are authored under `Assets/GameContent`; the generated bootstrap owns no duplicate catalog.

The starter balance is tuned as a 3-5 minute vertical slice. The Shard Launcher begins with short range, low starting credits, and a slower cadence so enemies survive multiple hits and can pressure the core. The first reward appears around 30-60 seconds, mid-run enemies should sometimes reach the base, and Pulse Beam, Arc Burst, Homing Pulse, and Overdrive create visible relief after pressure spikes. To make the first two minutes easier or harder, tune authored enemy health/speed under `Assets/GameContent/IdleAutoDefense/Enemies`, wave timings under `Waves`, weapon cooldown/range under `Weapons`, and attack damage/range/projectile speed under `Attacks`.

The starter content intentionally stays generic and reusable:

- 6 enemies, including elite and boss enemies
- 4 attacks
- 4 tower weapons
- 7 spawn profiles, including authored elite and boss waves
- 6 authored starter upgrades plus runtime three-choice reward drafts

The template doctrine and extraction boundary are defined in [the template contract](Documentation~/template-contract.md). Numerical ownership and parity are recorded in [the authored-core parity inventory](Documentation~/idle-auto-defense-authored-core-parity.md).

Use [playtesting.md](Documentation~/playtesting.md) for the end-to-end run checklist and [mobile-testing.md](Documentation~/mobile-testing.md) for safe-area and target-resolution checks.

## Package Boundary

This template depends on:

- `com.deucarian.auto-defense-suite` for the reusable auto-defense gameplay stack.
- `com.deucarian.editor` for shared editor shell/resources used by template setup tools.
- `com.deucarian.game-content-authoring` for content authoring provider integration.
- `com.deucarian.gameplay-foundation` for shared IDs, validation, and gameplay primitives used by template glue.
- `com.deucarian.monetization` for SDK-free placement and mock/no-op monetization abstractions.

Keep product-specific starter glue, setup reporting, template scene composition, starter sample content, and asset-flip helpers local to this template. Move reusable behavior down only through explicit governance.

## Tests

Package tests live under `Tests/EditMode` and `Tests/PlayMode`. Template source files under `TemplateSource~` are not user-importable samples.

## Validation

Before committing package changes, run:

```powershell
python C:/Repositories/Package-Registry/Tools/deucarian_package_validator.py --registry-root C:/Repositories/Package-Registry --repository-root . --config deucarian-package.json
git diff --check
```

Run existing Unity EditMode and PlayMode tests when changing code, asmdefs, package dependencies, template source content, setup wizard behavior, or starter gameplay behavior.

Durable batch entry points are:

```text
Deucarian.TestAutomation.BatchTestRunner.RunEditMode
Deucarian.TestAutomation.BatchTestRunner.RunPlayMode
```

## License

MIT. See `LICENSE.md`.
