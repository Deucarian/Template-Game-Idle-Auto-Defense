# Setup

Install `com.deucarian.template.game.idle-auto-defense` with Package Manager or Deucarian Package Installer. The package declares `com.deucarian.auto-defense-suite` and `com.deucarian.monetization`. The suite pulls the gameplay foundation, persistence, progression, combat, encounter, spawning, navigation, defense, attack, projectile, weapon, auto-defense, upgrade, and idle progression packages.

For local workspace validation before registry publication, add this package as a local file reference:

```json
"com.deucarian.template.game.idle-auto-defense": "file:C:/Repositories/Deucarian/Template-Game-Idle-Auto-Defense"
```

If the suite dependency is not resolvable from the registry yet, keep explicit local file references for the lower packages in the validation project. The template package itself should still declare only the suite dependency.

Import the sample named `Basic Idle Auto Defense Game`, then run:

```text
Tools > Deucarian > Templates > Idle Auto Defense > Open Starter Scene
```

Press Play. The starter scene writes a small sample save snapshot and exposes reset actions in the Game view and under:

```text
Tools > Deucarian > Templates > Idle Auto Defense > Reset Sample Save
```

To create a project-owned game folder, run:

```text
Tools > Deucarian > Templates > Idle Auto Defense > Create Game From Template
```

Choose a target folder under `Assets`, a C# namespace, and a game prefix. The wizard copies starter scenes, prefabs, content, docs, and a renamed bootstrap script without copying Deucarian package source. It blocks existing files unless overwrite is explicitly enabled, opens the created scene if requested, and writes `Docs/setup-report.md`.
