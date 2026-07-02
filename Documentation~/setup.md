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

Choose a target folder under `Assets`, a content folder under `Assets/GameContent`, a C# namespace, and a game prefix. The wizard copies scene files, scripts, docs, visuals, and audio into the target folder, copies authored gameplay data into the content folder, remaps copied asset GUIDs across both roots, renames the bootstrap script, opens the created scene if requested, and writes `Docs/setup-report.md`.

Troubleshooting:

- `Create Playable Game cannot find template source`: reinstall or relink the template package and confirm `TemplateSource~/BasicIdleAutoDefenseGame` exists inside the package.
- `Generated scene has invalid content set`: open `Tools > Deucarian > Game Content Authoring`, validate the generated content pack/set under `Assets/GameContent`, and fix missing weapon, enemy, wave, or upgrade references.
- Existing files block setup by default. Enable overwrite only after reviewing the target folder.
