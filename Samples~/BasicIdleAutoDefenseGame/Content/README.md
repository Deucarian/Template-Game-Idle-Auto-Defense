# Content

`starter-content.json` mirrors the constants used by the deterministic starter code. Replace these IDs and values when converting the template into a project-specific data pipeline.

The default pack is split by override area:

- `DefaultBalance` owns the central objective and loop-level values.
- `DefaultStages` owns First Orbit, Pressure Ring, Boss Pulse, and Endless placeholder stage routing.
- `DefaultEnemies` owns Swarm, Runner, Tank, Shielded, Elite, and Boss archetypes.
- `DefaultWeapons` owns supported Pulse Cannon and Shard Launcher modules plus future Arc Emitter and Orbital Shot intent.
- `DefaultWaves` owns staged encounter and wave groups.
- `DefaultUpgrades` owns draft cadence and the 14-upgrade starter catalog.
- `DefaultProgression` owns currencies, rewards, account XP, unlocks, research-like defaults, offline rewards, and save DTO setup.
- `DefaultMonetization` owns mock rewarded, interstitial, and IAP placeholder placement IDs.
- `ContentSets` owns the authored playable run recipe consumed by the template controller.
- `ContentPacks` owns the one-click package that points at the default content set and can be applied from Game Content Authoring.
- `Enemies` and `Waves` are legacy authoring samples kept for browsing older enemy/wave recipes. Their IDs intentionally stay distinct from `RuntimeEnemies` and `RuntimeWaves` so Content Library duplicate checks stay clean.

Edit this file first when experimenting with:

- objective health, lives, and contact radius
- stage names, encounter IDs, unlock order, and reward references
- spawn ring radius and channels
- enemy archetype IDs and values
- Pulse Cannon and Shard Launcher IDs
- upgrade IDs
- account XP, unlocks, and research node IDs
- offline reward caps and rates
- rewarded/interstitial placement IDs, cooldowns, session caps, and no-ads placeholders

The starter runtime does not load this JSON directly. It is a readable map for the values in `Runtime/IdleAutoDefenseTemplate.cs` so a new project can copy the template and wire the values into its own data pipeline.

Product games should copy these files into product-owned content folders, rename IDs away from `template.*`, and edit content/balance before forking the canonical flow.
Use `Tools > Deucarian > Game Content Authoring` to inspect the pack, validate dependencies, and apply a selected content set to an open scene controller.
