# Asset-Flip Workflow

Use the setup wizard to create the product-owned starter:

```text
Tools > Deucarian > Templates > Idle Auto Defense > Create Playable Game
```

The wizard copies the scene, bootstrap, prefabs, visuals, audio, and docs into the chosen `Assets` folder. It copies authored gameplay data into `Assets/GameContent/IdleAutoDefense` by default so the content appears in Game Content Authoring. It does not copy Deucarian package source.

## First Pass

1. Press Play and verify enemies spawn outside the view and move toward the central core.
2. Verify the direct and projectile mounts fire.
3. Buy or draft upgrades during the run.
4. Save a snapshot and reset it from the HUD.
5. Replace starter visuals under `Prefabs` and `Visuals`.
6. Tune generated enemies, attacks, towers, waves, upgrades, and progression assets under `Assets/GameContent`.
7. Rename template IDs into product IDs.

Keep reusable framework behavior in Deucarian packages. Keep product theme, scene composition, and save names in the generated product folder; keep authored gameplay balance under `Assets/GameContent`.
