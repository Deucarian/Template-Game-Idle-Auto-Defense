# Basic Idle Auto Defense Game

Open `Scenes/BasicIdleAutoDefenseGame.unity` after importing the sample from Package Manager.

The scene contains a bootstrap object that creates:

- a central core objective
- four perimeter spawn channels
- three authored enemy archetypes
- two authored wave definitions
- one direct hitscan-style weapon mount
- one projectile weapon mount
- one homing/status projectile weapon mount
- deterministic run upgrade drafts
- save/load, offline reward, progression reward, and corrupted save recovery smoke paths
- three attack recipes converted into runtime attack, projectile, status, and presentation hooks

The bootstrap object has serialized references to the authored attacks, enemies, and waves in `Content`. The controller validates those assets, converts them to runtime definitions, and falls back to transient built-in content if an assigned set is missing or invalid.

All visible gameplay objects are primitive placeholders. The direct mount, fire orb projectile, homing pulse projectile, basic enemy, fast enemy, and tank enemy use different colors and shapes so the paths are easy to distinguish in Play Mode. Replace them with real content in the `Prefabs` and `Content` folders when turning the template into a production game.
