# Setup

Install `com.deucarian.template.game.idle-auto-defense` with Package Manager or Deucarian Package Installer. The package directly requires the Auto Defense Suite, Editor, Game Content Authoring, Gameplay Foundation, and Monetization packages.

For local workspace validation before registry publication, add this package as a local file reference:

```json
"com.deucarian.template.game.idle-auto-defense": "file:C:/Repositories/Template-Game-Idle-Auto-Defense"
```

Create the product-owned game folder from the template:

```text
Tools > Deucarian > Templates > Idle Auto Defense > Create Playable Game
```

Choose a target folder under `Assets`, a content folder under `Assets/GameContent`, a C# namespace, and a game prefix. The wizard copies scripts, docs, visuals, audio, and runtime resources into the target folder; copies the complete authored graph into the content folder; creates the playable scene; remaps copied GUIDs; renames the bootstrap; and writes `Docs/setup-report.md`. The generated graph includes attacks, enemies, weapons, waves, run upgrades, reward catalog, economy, run profile, progression, offline progression, game rules, content set, and content pack.

Troubleshooting:

- `Create Playable Game cannot find template source`: reinstall or relink the template package and confirm `TemplateSource~/BasicIdleAutoDefenseGame` exists inside the package.
- `Strict authored startup blocked`: open `Tools > Deucarian > Game Content Authoring`, validate the generated named pack, and fix the reported missing/invalid source or reference. Do not enable fallback for the normal sample.
- Existing files block setup by default. Enable overwrite only after reviewing the target folder.
