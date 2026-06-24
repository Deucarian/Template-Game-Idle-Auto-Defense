# Basic Idle Auto Defense Game

Open `Scenes/BasicIdleAutoDefenseGame.unity` after importing the sample from Package Manager.

The scene contains a bootstrap object that creates:

- a central core objective
- four perimeter spawn channels
- one enemy archetype
- one direct hitscan-style weapon mount
- one projectile weapon mount
- one homing/status projectile weapon mount
- deterministic run upgrade drafts
- save/load, offline reward, progression reward, and corrupted save recovery smoke paths
- three attack recipes converted into runtime attack, projectile, status, and presentation hooks

All visible gameplay objects are primitive placeholders. The direct mount, fire orb projectile, and homing pulse projectile use different colors and shapes so the attack paths are easy to distinguish in Play Mode. Replace them with real content in the `Prefabs` and `Content` folders when turning the template into a production game.
