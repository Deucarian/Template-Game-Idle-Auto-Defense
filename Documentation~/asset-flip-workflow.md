# Asset-Flip Workflow

Use the setup wizard to create the product-owned starter:

```text
Tools > Deucarian > Templates > Idle Auto Defense > Create Playable Game
```

The wizard creates the playable scene at `Assets/OPEN_THIS_TO_TEST_IdleAutoDefense_PlayableGame/OPEN_THIS_TO_TEST_IdleAutoDefense_PlayableGame.unity`, copies bootstrap code, prefabs, visuals, audio, resources, and docs into the chosen `Assets` folder, and copies authored gameplay data into `Assets/GameContent/IdleAutoDefense` by default so the content appears in Game Content Authoring. It does not copy Deucarian package source.

## First Pass

1. Press Play and verify enemies spawn outside the view and move toward the central core.
2. Verify the direct and projectile mounts fire.
3. Unlock or improve modules, activate Overdrive, and choose reward cards by touch/click and `1`/`2`/`3`.
4. Verify pause/build/settings, victory/defeat summary, restart, return to menu, and a one-time offline claim.
5. Replace starter visuals under `Prefabs` and `Visuals`.
6. Replace themes, UI copy/tokens, tutorial copy, and audio clips under `Assets/GameContent/IdleAutoDefense/Presentation`.
7. Tune gameplay only through the existing authored gameplay folders.
8. Rename template IDs into product IDs as references are coordinated.

Keep reusable framework behavior in Deucarian packages. Keep product player flow and scene composition in the generated product folder; keep authored gameplay and presentation records under `Assets/GameContent`.

The generated `GameContentSetAsset` is the graph root, not a duplicate balance container. Follow its references to `Rewards`, `Economy`, `RunProfiles`, `Progression`, `OfflineProgression`, and `GameRules`; tune weapon, attack, enemy, wave, and run-upgrade assets in their existing folders. The controller reads these assets at runtime. Do not maintain a separate reward catalog, JSON mirror, economy table, or tower map on the scene object.

After an asset flip, run content validation and press Play. A valid generated scene reports `UsingAuthoredCore == true` and `FallbackModeActive == false`. If required content is missing, strict startup intentionally blocks; fix the reported asset/reference instead of enabling fallback.
