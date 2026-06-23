# Setup

Install `com.deucarian.template.game.idle-auto-defense` with Package Manager. The package declares one dependency, `com.deucarian.auto-defense-suite`, which pulls the gameplay foundation, persistence, progression, combat, encounter, spawning, navigation, defense, attack, projectile, weapon, auto-defense, upgrade, and idle progression packages.

For local workspace validation before registry publication, add this package as a local file reference:

```json
"com.deucarian.template.game.idle-auto-defense": "file:C:/Repositories/Deucarian/Template-Game-Idle-Auto-Defense"
```

If the suite dependency is not resolvable from the registry yet, keep explicit local file references for the lower packages in the validation project. The template package itself should still declare only the suite dependency.

Import the sample named `Basic Idle Auto Defense Game`, then open `Scenes/BasicIdleAutoDefenseGame.unity`.
