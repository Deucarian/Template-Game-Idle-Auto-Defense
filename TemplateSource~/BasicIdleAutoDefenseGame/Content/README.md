# Content

This folder is the private template source copied by `Create Playable Game`.
It contains the authored assets consumed by the generated scene:

- `ContentPacks` owns the starter pack.
- `ContentSets` owns the playable run recipe.
- `Enemies` contains four generic enemy definitions: Swarm, Runner, Tank, and Shielded.
- `Attacks` contains four generic attack recipes: Pulse Beam, Shard Projectile, Arc Burst, and Homing Pulse.
- `Weapons` contains four tower weapon definitions paired with those attacks.
- `Waves` contains five spawn profiles: Opening Wave, Runner Pressure, Mixed Pressure, Tank Break, and Final Surge.
- `Upgrades` contains six run upgrades: Damage Boost, Fire Rate Boost, Range Boost, Projectile Speed, Core Reinforcement, and Credit Reward.
- `starter-content.json` mirrors those IDs for quick inspection.

The starter runtime does not load the JSON directly. The actual playable loop
uses the authored `GameContentPackAsset` and `GameContentSetAsset` references
assigned in the scene.

The setup wizard copies these files into product-owned folders under
`Assets/GameContent`, creates fresh GUIDs, and rewrites the generated scene to
reference the copied assets.
Use `Tools > Deucarian > Game Content Authoring` to inspect the pack, validate
dependencies, and apply a selected content set to an open scene controller.
