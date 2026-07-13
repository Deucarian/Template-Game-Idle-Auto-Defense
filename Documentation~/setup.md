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

Choose `Basic Only`, `Scrap Frontier Only`, or `Both`, then select a target folder under `Assets`, a content folder under `Assets/GameContent`, a C# namespace, and a game prefix. The wizard copies scripts, docs, visuals, audio, and runtime resources into the target folder; copies each selected complete authored graph into the content folder; creates the corresponding visible playable scene; remaps copied GUIDs; renames one shared bootstrap; and writes `Docs/setup-report.md`. With Both, generated roots use sibling `Basic` and `ScrapFrontier` folders. No manual scene reconstruction is required.

`Repair Missing Content` reuses generated destination GUIDs, skips byte-identical files, and restores missing files. A differing existing file is reported as a conflict unless overwrite is explicitly enabled. Use repair for damaged generated output; use overwrite only for an intentional full refresh.

Troubleshooting:

- `Create Playable Game cannot find template source`: reinstall or relink the template package and confirm `TemplateSource~/BasicIdleAutoDefenseGame` and `TemplateSource~/ScrapFrontierGame` exist inside the package.
- `Strict authored startup blocked`: open `Tools > Deucarian > Game Content Authoring`, validate the generated named pack, and fix the reported missing/invalid source or reference. Do not enable fallback for the normal sample.
- `Player experience content is missing`: regenerate or restore the generated `Presentation` folder and scene reference.
- Existing files block setup by default. Select repair for identical/partially missing output; enable overwrite only after reviewing reported conflicts.
