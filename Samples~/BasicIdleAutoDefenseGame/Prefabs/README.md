# Prefabs

The starter scene creates primitive placeholders at runtime for the core, mounts, and projectiles. This folder also contains authored enemy prefabs used by the sample enemy definition assets:

- `TemplateBasicEnemy.prefab`: capsule placeholder.
- `TemplateFastEnemy.prefab`: smaller sphere placeholder.
- `TemplateTankEnemy.prefab`: larger cube placeholder.

Use this folder for project-specific core, enemy, weapon, projectile, hitscan tracer, impact VFX, and audio-ready prefabs after replacing the starter placeholders.

Attack and enemy presentation events tolerate missing optional audio and VFX references, so the sample remains playable before production assets exist. Sample audio is intentionally empty.
