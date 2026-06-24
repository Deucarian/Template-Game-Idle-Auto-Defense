# Canonical Idle Auto Defense Game Flow

The Idle Auto Defense template owns the default starter game flow. Deucarian packages provide reusable systems for persistence, progression, encounters, spawning, navigation, combat, projectiles, weapons, upgrades, idle rewards, and auto-defense runtime behavior. Product games should start by overriding content and balance before forking the overall flow.

## Flow

```text
Boot
-> load profile/save
-> resolve monetization availability
-> apply offline reward
-> optionally offer rewarded 2x offline reward
-> start run
-> spawn waves
-> auto weapons fire
-> upgrade draft moments
-> optional rewarded reroll
-> win/fail
-> optional rewarded revive on failure
-> apply rewards
-> optional rewarded double reward
-> optional interstitial at transition if pacing allows
-> save
-> restart/return
```

## Runtime Ownership

`IdleAutoDefenseTemplateController` is the canonical starter orchestration. It builds a central objective, perimeter spawns, enemy and projectile spawning, direct and projectile weapon modules, run upgrades, encounter rewards, offline rewards, mock/no-op monetization placements, and sample save/reset behavior.

The template uses `com.deucarian.monetization` for placement IDs, rewarded and interstitial abstractions, mock/no-op providers, pacing gates, and no-ads entitlement checks. No real ad SDK, IAP billing SDK, analytics SDK, store config, or privacy policy generation is included.

The reusable packages stay generic:

- Auto Defense Suite supplies the systems.
- The template wires those systems into a playable starter loop.
- Product games copy or reference the template flow intentionally, then replace content and balance first.

## State Flow

| Step | Template Responsibility | Product Override Point |
| --- | --- | --- |
| Boot | Build the default runtime graph. | Replace scene bootstrap only when a product needs a different entry point. |
| Load sample profile/save | Demonstrate profile, run, and settings DTOs. | Rename documents and expand DTOs for product saves. |
| Resolve monetization availability | Create mock/no-op rewarded and interstitial sessions from template placement policies. | Swap providers through a future product integration package; keep placement IDs product-owned when shipping. |
| Apply offline reward | Use the default idle reward definition. | Tune reward rates, caps, and currencies. |
| Optional rewarded 2x offline reward | Offer `template.rewarded.double-offline-reward` after the offline result is known. | Change the offer copy, reward multiplier, claim identity, and product placement ID. |
| Start run | Start a deterministic starter encounter. | Swap encounter IDs, waves, and enemy groups. |
| Spawn waves | Emit four perimeter groups. | Add channels, patterns, enemy types, and wave timing. |
| Auto weapons fire | Use one direct weapon and one projectile weapon. | Tune attacks, projectile behavior, mounts, and visual prefabs. |
| Run upgrade draft moments | Draft common upgrades every fixed tick interval. | Replace upgrade IDs, rarity, effects, and draft cadence. |
| Optional rewarded reroll | Offer `template.rewarded.reroll-upgrade-draft` at draft moments. | Tune reroll limits, draft sources, and placement ID. |
| Win/fail | Finish when the encounter/runtime reaches terminal state. | Add product-specific fail rules only when needed. |
| Optional rewarded revive | Offer `template.rewarded.revive-after-failure` only after failure. | Add product revive rules and visual state restoration. |
| Apply rewards | Grant credits and parts. | Change currencies, amounts, XP, unlocks, and operations. |
| Optional rewarded double reward | Offer `template.rewarded.double-run-reward` after rewards are known. | Tune multiplier and claim identity. |
| Optional interstitial at transition | Show completion/failure interstitials only outside combat, after the first terminal run, under cooldown/session caps, and blocked by no-ads entitlement. | Replace with product pacing and real provider adapters later. |
| Save | Persist sample snapshot and composition smoke DTOs. | Store real product profile/run/settings documents. |
| Restart/return | Keep the scene immediately replayable. | Route into menus, maps, or meta progression later. |

## Guardrail

Moss and other product games should not copy reusable Deucarian package source into the game project. Start by editing content packs, prefabs, scene composition, product DTOs, and balance values. Fork the loop only after a product requirement cannot be expressed by content/balance or a narrow product bootstrap.
