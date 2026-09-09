# Architecture

The package is a template, not a reusable gameplay framework. It composes the Auto Defense Suite through starter application glue:

- `BasicIdleAutoDefenseGame` creates deterministic transient fallback definitions only for explicit unbound tests and debug hosts.
- `IdleAutoDefenseTemplateController` wires spawning, navigation, combat, weapons, projectiles, upgrades, idle rewards, progression, and simple scene visuals.
- `IdleAutoDefensePlayerExperienceController` is the existing serialized/source compatibility facade. It creates the composed player application and forwards player commands; it contains no player-flow, profile, HUD or audio implementation.
- `IdleAutoDefensePlayerExperience` coordinates menu-first flow through an explicit run-session port, one profile session, player-flow state, and separate HUD/menu/modal/audio presenters.
- `IdleAutoDefensePlayerExperienceAsset` references the generated UI settings, tutorial, audio palette, and two theme assets.
- `IdleAutoDefensePlayerProfileStore` uses `Deucarian.Persistence` envelopes, backups, validation, and recovery for settings, tutorial/theme state, offline timestamps, totals, and a template-owned progression snapshot DTO.
- `BasicIdleAutoDefenseGameBootstrap` is private template-source code copied and renamed into generated product folders.
- `IdleAutoDefenseTemplateSetupService` creates a product-owned game root from `TemplateSource~/BasicIdleAutoDefenseGame` and a discoverable authored content root under `Assets/GameContent`.

The generated scene requires its assigned `GameContentPackAsset`, `GameContentSetAsset`, and `IdleAutoDefensePlayerExperienceAsset`. Invalid strict gameplay blocks Start Run. Invalid or missing theme selection falls back visibly to the authored default without changing gameplay content.

Unbound package smoke fixtures may use deterministic transient content. This intentional path reports `FallbackModeActive == true` and is not an editable source of truth. See [template-contract.md](template-contract.md).

The legacy reward draft members on `IdleAutoDefenseContentSetRuntimeSettings` are non-serialized compatibility drafts used only to seed transient editor/test content. Persisted gameplay always reads the first-class `GameContentSetAsset.RewardCatalog` asset.

Template source assets store root and section data as sibling `.asset` files. During setup, copied `.meta` files receive fresh GUIDs and copied YAML references are rewritten across the generated game root and `Assets/GameContent` so generated scenes and assets point at product-owned authored content.

Reusable systems belong in lower Deucarian packages. Keep product-specific scene composition, UI composition, prefabs, content IDs, authored data, and save DTOs in the product project. The current generic Progression API exposes snapshots but no restore constructor; this template replays its small authored research graph through public reward/research APIs instead of modifying the dependency or using reflection.

## Player composition and compatibility

The generated bootstrap still subclasses `IdleAutoDefensePlayerExperienceController`, and existing scenes retain the same component GUID and serialized fields. Its inherited `IdleAutoDefenseTemplateController` relationship is retained only at that migration boundary so existing public/protected consumers remain source-compatible. The player application and its presenters are ordinary composed C# objects; none inherit simulation behavior.

`IdleAutoDefenseRunSession` adapts the existing runtime through explicit commands and captures `IdleAutoDefenseRunSnapshot` for observers. Scalar state, reward ranks, research ranks and balances are captured together; authored assets remain the existing read-only presentation references. The runtime remains authoritative. Player commands never recalculate the lower packages' combat, rewards, upgrades, or progression logic.

`AdvanceFrame(deltaSeconds)` is the explicit simulation entry point. Standalone controllers still call it from `Update`. The player facade asks the player-flow owner whether ticking is permitted and calls it through the run port. Main menu, pause, tutorial, summary and portrait blocking therefore do not invoke inherited simulation lifecycle methods. Pre-input and post-tick presentation snapshots deliberately observe legal legacy `Step` and public command calls, while public player facade commands refresh before making decisions. A legacy module purchase immediately followed by a player module action remains valid in the same frame.

`IdleAutoDefenseProfileSession` owns one loaded profile and one storage lifetime, including restore, capture/save, reset and offline preview. UI controls observe that profile; persistence is not triggered by rendering. `IdleAutoDefenseAudioPresenter` observes run snapshots, detects feedback edges and applies authored throttle/category volumes through a replaceable output. Only its Unity output owns an AudioSource. The player view owns the visual tree and composes HUD, menus, modals and shared style construction; disposal unregisters geometry callbacks and detaches its tree.

Failure and teardown paths release owned resources even when initialization or profile persistence fails. Quit followed by destroy does not save or dispose the profile twice. The original facade invokes base runtime cleanup in a `finally` block.

The run recreates its Unity UI document during restart. The player view receives a narrow current-root provider and reattaches its existing tree after player restart commands and at frame boundaries. This preserves live HUD/modal attachment and callback state across both player-flow transitions and direct legacy restarts without rebuilding the view or giving it a gameplay mutation port.

## Run composition and ownership

`IdleAutoDefenseTemplateController` retains its original component, serialized configuration and public commands. Its partial companions contain configuration bindings and forwarding observations; gameplay state and policy live in ordinary composed owners. `BasicIdleAutoDefenseGame` preserves its public factory methods while delegating authored fallback construction to content-specific factories.

| Responsibility | Authoritative owner |
| --- | --- |
| Assigned graph, strict-startup diagnostics, generated fallback lifetime | `IdleAutoDefenseContentBinding` |
| Lower-package construction and spawn-service teardown | `IdleAutoDefenseRuntimePlan`, `IdleAutoDefenseRuntimeServices` |
| Elapsed ticks, frame residual, run sequence, victory and deferred restart | `IdleAutoDefenseRunTimeline` |
| Ordered simulation phases | `IdleAutoDefenseRunLoop` |
| Wallet accounting and passive-income residual | `IdleAutoDefenseRunWallet` |
| Run ranks, unlocks, effect intents and Overdrive | `IdleAutoDefenseRunBuild` |
| Commander XP, reward queues, selected ranks and kill/wave deduplication | `IdleAutoDefenseRewardProgression` |
| Deterministic eligible-choice sampling | `IdleAutoDefenseRewardSelection` |
| Persistent research, offline rewards and terminal reward operations | `IdleAutoDefensePersistentProgression` |
| Existing monetization offers and legacy draft API | `IdleAutoDefenseRewardOffers`, `IdleAutoDefenseLegacyDraft` |
| Target policy, cadence, accumulated damage, delayed impacts and feedback | Composed `IdleAutoDefenseCombatRuntime` owners |
| World objects, visual bindings, events, beams and generated materials | Composed `IdleAutoDefenseWorldPresentation` owners |
| UI document, panel settings and floating feedback | `IdleAutoDefenseRuntimeUi` |

The run loop preserves the established phase order. It advances session ticks and Overdrive before checking a paused reward draft; survival time and combat remain paused. For an active frame it consumes encounter requests, updates targets, ticks combat, observes feedback, launches projectiles, ticks navigation, resolves expiry and delayed impacts, fires manual/module cadence, awards currency and progression, updates presentation, and finally evaluates authored session victory. Launching a projectile does not immediately apply its visible impact damage.

Combat receives explicit enemy/projectile backends and presentation/reward ports. World presentation receives authored content and narrow target/lookup queries. These owners do not retain the MonoBehaviour or scan assemblies. The player's `IdleAutoDefenseRunSession` remains an adapter; it is distinct from the runtime's `IdleAutoDefenseRunTimeline`.

Generated fallback assets and material copies have explicit lifetime owners. Assigned content, prefab source materials and themes are borrowed and survive teardown. Runtime construction rolls back world, UI and service resources on failure so a subsequent Build can retry cleanly. Restart resets run state while retaining persistent progression and the existing monetization session. Scope remains local to this concrete template; no new generic framework or shared-package API is introduced.
