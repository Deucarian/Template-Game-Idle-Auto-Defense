# Asset-Flip Workflow

Use the setup wizard to create the product-owned starter:

```text
Tools > Deucarian > Templates > Idle Auto Defense > Create Playable Game
```

The wizard creates the playable scene at `Assets/OPEN_THIS_TO_TEST_IdleAutoDefense_PlayableGame/OPEN_THIS_TO_TEST_IdleAutoDefense_PlayableGame.unity`, copies bootstrap code, prefabs, visuals, audio, resources, and docs into the chosen `Assets` folder, and copies authored gameplay data into `Assets/GameContent/IdleAutoDefense` by default so the content appears in Game Content Authoring. It does not copy Deucarian package source.

## First Pass

1. Press Play and verify enemies spawn outside the view and move toward the central core.
2. Verify the direct and projectile mounts fire.
3. Choose reward draft cards during the run and verify 1/2/3 hotkeys select them.
4. Save a snapshot and reset it from the HUD.
5. Replace starter visuals under `Prefabs` and `Visuals`.
6. Tune generated enemies, attacks, towers, waves, reward choices, economy, run profile, upgrades, progression, offline settings, and game rules under `Assets/GameContent`.
7. Rename template IDs into product IDs.

Keep reusable framework behavior in Deucarian packages. Keep product theme, scene composition, and save names in the generated product folder; keep authored gameplay balance under `Assets/GameContent`.

The generated `GameContentSetAsset` is the graph root, not a duplicate balance container. Follow its references to `Rewards`, `Economy`, `RunProfiles`, `Progression`, `OfflineProgression`, and `GameRules`; tune weapon, attack, enemy, wave, and run-upgrade assets in their existing folders. The controller reads these assets at runtime. Do not maintain a separate reward catalog, JSON mirror, economy table, or tower map on the scene object.

After an asset flip, run content validation and press Play. A valid generated scene reports `UsingAuthoredCore == true` and `FallbackModeActive == false`. If required content is missing, strict startup intentionally blocks; fix the reported asset/reference instead of enabling fallback.
