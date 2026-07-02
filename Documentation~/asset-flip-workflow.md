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
6. Tune generated enemies, attacks, towers, waves, upgrades, and progression assets under `Assets/GameContent`.
7. Rename template IDs into product IDs.

Keep reusable framework behavior in Deucarian packages. Keep product theme, scene composition, and save names in the generated product folder; keep authored gameplay balance under `Assets/GameContent`.

For first-balance passes, tune the authored weapon/attack/enemy/wave values first, then tune `IdleAutoDefenseRewardDraftSettings` on the generated controller if the product run still needs different XP pacing, rarity weights, unlock weighting, or projectile retarget tolerance. Edit the same controller's reward draft catalog when changing unlock cards, normal weapon tracks, Epic tracks, Legendary tracks, or global/base cards. Epic choices are intentionally gated behind three normal investments in the affected weapon, and Legendary choices are gated behind three Epic investments by default.
