# Content

`starter-content.json` mirrors the constants used by the deterministic starter code. Replace these IDs and values when converting the template into a project-specific data pipeline.

The default pack is split by override area:

- `DefaultBalance` owns the central objective and loop-level values.
- `DefaultEnemies` owns starter enemy definitions.
- `DefaultWeapons` owns direct/projectile weapons, attacks, and projectile settings.
- `DefaultWaves` owns the starter encounter and wave groups.
- `DefaultUpgrades` owns draft cadence and common run upgrades.
- `DefaultProgression` owns currencies, rewards, offline rewards, and save DTO setup.
- `DefaultMonetization` owns mock rewarded, interstitial, and IAP placeholder placement IDs.

Edit this file first when experimenting with:

- objective health, lives, and contact radius
- spawn ring radius and channels
- direct and projectile weapon IDs
- upgrade IDs
- offline reward caps and rates
- rewarded/interstitial placement IDs, cooldowns, session caps, and no-ads placeholders

The starter runtime does not load this JSON directly. It is a readable map for the values in `Runtime/IdleAutoDefenseTemplate.cs` so a new project can copy the template and wire the values into its own data pipeline.

Product games should copy these files into product-owned content folders, rename IDs away from `template.*`, and edit content/balance before forking the canonical flow.
