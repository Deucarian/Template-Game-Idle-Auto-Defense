# Deucarian Template Game - Idle Auto Defense Agent Notes

Package ID: `com.deucarian.template.game.idle-auto-defense`
Repository: `Deucarian/Template-Game-Idle-Auto-Defense`

Follow the canonical Deucarian governance docs in [Package Registry](https://github.com/Deucarian/Package-Registry/blob/develop/ARCHITECTURE.md), especially capability ownership and dependency rules.

## Ownership

This package owns:

- Idle Auto Defense starter-game glue, sample content, setup wizard, template-local editor utilities, sample monetization hooks, and developer-facing setup/reporting helpers.

Registered capabilities:
- None.

This package must not own:

- Reusable gameplay frameworks, lower-level gameplay systems, monetization SDKs, package installer behavior, or generic authoring frameworks.

## Dependencies

Allowed dependency shape:

- Template package that composes the Auto Defense Suite, Game Content Authoring, Gameplay Foundation, Editor, and Monetization packages.

Required dependencies and why:

- `com.deucarian.auto-defense-suite`: reusable auto-defense gameplay stack.
- `com.deucarian.editor`: shared editor shell/resources used by template setup tools.
- `com.deucarian.game-content-authoring`: content authoring provider integration for starter content.
- `com.deucarian.gameplay-foundation`: shared IDs, validation, and gameplay primitives used by template glue.
- `com.deucarian.monetization`: SDK-free placement and mock/no-op monetization abstractions.

Optional/version-defined dependencies:

- None.

Architecture exceptions:

- Direct Unity Debug calls are allowed only in the files listed in `deucarian-package.json` for template/sample developer visibility.

## Policies

- Template code: Keep product-specific starter glue local to the template; move reusable behavior down only through explicit governance.
- Samples: Keep imported sample content under `Samples~`.
- Editor UI: Use shared Editor and Game Content Authoring surfaces rather than local copies.
- Monetization: Keep real SDK/billing adapters out of this package.
- Testing: Keep setup wizard, content validation, save/reset smoke, sample, EditMode, and PlayMode coverage focused on the starter-game flow.

## Validation

Run the shared validator before committing:

```powershell
python C:/Repositories/Package-Registry/Tools/deucarian_package_validator.py --registry-root C:/Repositories/Package-Registry --repository-root . --config deucarian-package.json
```

Also run existing repository tests when changing code or asmdefs. Documentation-only updates should still run `git diff --check`.

## Codex Guidance

- Inspect current files before changing anything.
- Work on `develop`; do not edit or merge `main` unless the task is promotion-only.
- Do not edit `Library/PackageCache`.
- Do not guess package versions or dependency versions.
- Do not add package dependencies casually; update asmdefs, `package.json`, `deucarian-package.json`, Package Registry, and fallback catalogs together when a dependency is truly required.
- Do not create local copies of shared helpers.
- Keep commits focused and report exactly what changed and what was validated.

