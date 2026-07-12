# Content

This folder is the private template source copied by `Create Playable Game`.
It contains the authored assets consumed by the generated scene:

- `ContentPacks` owns the starter pack.
- `ContentSets` owns the playable run recipe.
- `Enemies` contains six generic enemy definitions: Swarm, Runner, Tank, Shielded, Elite, and Boss.
- `Attacks` contains four generic attack recipes: Pulse Beam, Shard Projectile, Arc Burst, and Homing Pulse.
- `Weapons` contains four tower weapon definitions paired with those attacks.
- `Waves` contains seven spawn profiles: Opening Wave, Runner Pressure, Mixed Pressure, Tank Break, Elite Pressure, Final Surge, and Boss Push.
- `Upgrades` contains six run upgrades: Damage Boost, Fire Rate Boost, Range Boost, Projectile Speed, Core Reinforcement, and Credit Reward.
- `Rewards` owns the live draft table and all 37 stable unlock, normal, Epic, Legendary, and base choices.
- `Economy` owns currencies, starting resources, income, costs, and encounter/run rewards.
- `RunProfiles` owns fixed-rate timing, the seven-wave sequence, scaling, outcomes, and endless settings.
- `Progression` and `OfflineProgression` own persistent research and deterministic catch-up values.
- `GameRules` owns the objective, module roles, elite/boss references, combat, repair, projectile, and Overdrive values.
- The content set references those required owners and retains presentation/debug bindings.

The current authored tuning is a 3-5 minute vertical slice: low starting
credits, visible offscreen approaches, a first reward around 30-60 seconds,
mid-run near-leaks that can damage the core, and elite/boss pressure before
the final reward moments.

ScriptableObjects are the only gameplay source of truth. The obsolete, non-consumed
`starter-content.json` mirror was removed so asset flippers cannot edit a file that
runtime ignores.

The setup wizard copies these files into product-owned folders under
`Assets/GameContent`, creates fresh GUIDs, and rewrites the generated scene to
reference the copied assets.
Use `Tools > Deucarian > Game Content Authoring` to inspect the pack, validate
dependencies, and apply a selected content set to an open scene controller.
The generated bootstrap uses strict authored startup: invalid required content blocks
play with an actionable error, while transient fallback remains limited to explicit
unbound tests and debug hosts.
