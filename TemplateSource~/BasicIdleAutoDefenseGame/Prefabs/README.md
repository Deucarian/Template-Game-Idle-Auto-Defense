# Prefabs

The starter scene now builds visible gameplay from the curated Kenney Tower Defense Kit 3D models under `Resources/Kenney/IdleAutoDefense/Models/TowerDefenseKit`. Use this folder for project-specific core, enemy, weapon, projectile, muzzle flash, and impact prefabs when the product game is ready for authored visuals.

Suggested first folders:

```text
Prefabs
- Core
- Enemies
- Projectiles
- Weapons
```

Keep invisible colliders/helpers separate from visible art. Visible towers should expose a yaw pivot and muzzle transform so the local presentation binding can aim, recoil, flash, and launch from the muzzle.

The starter README files under `Enemies`, `Weapons`, and `Projectiles` give copied product folders obvious asset drop zones.
