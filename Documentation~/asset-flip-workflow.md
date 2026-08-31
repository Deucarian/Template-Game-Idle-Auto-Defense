# Asset-Flip Workflow

Use the setup wizard to create the product-owned starter:

```text
Deucarian Control Center > Authoring > Idle Auto Defense > Create Playable Game
```

The wizard can create Basic, Scrap Frontier, or both. Basic opens at `Assets/OPEN_THIS_TO_TEST_IdleAutoDefense_PlayableGame/OPEN_THIS_TO_TEST_IdleAutoDefense_PlayableGame.unity`; Scrap opens at `Assets/OPEN_THIS_TO_TEST_ScrapFrontier_PlayableGame/OPEN_THIS_TO_TEST_ScrapFrontier_PlayableGame.unity`. Both scenes use one generated bootstrap and the same package runtime while their authored graphs and presentation remain independent.

Scrap Frontier is the checked-in reference proof for this workflow. Compare its source under `TemplateSource~/ScrapFrontierGame` with Basic, and see [scrap-frontier-asset-flip.md](scrap-frontier-asset-flip.md) before creating a product variant.

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

## Staged Tuning In GCA

For a small tuning change, select the generated Basic or Scrap named pack in `Tools > Deucarian > Authoring > Game Content...`, open an Attack, Enemy, Weapon/Tower, Upgrade, or Run Profile record, and choose **Edit Existing**. Apply values and use the workbench Undo/Redo freely; the generated ScriptableObject is untouched until Commit. A Weapon/Tower may be repointed to a canonical compatible Attack, and a Run Profile's ordered Waves may be added, removed, moved, or replaced using canonical Waves from that same selected pack. Review the cloned-pack validation result, confirm warnings deliberately, then Commit. Cancel is byte-neutral.

After Commit, Unity Undo/Redo is available and GCA reindexes automatically. The workbench Rollback action restores the exact pre-edit source only if no later source change occurred. If the setup wizard repairs/regenerates content or another tool edits the asset, the session becomes stale; cancel it and reopen the current record instead of forcing an overwrite.

This workflow edits only the selected pack's claimed project-owned source asset. Basic and Scrap stay isolated. The Weapon/Tower Attack link and Run Profile Waves sequence are selected from canonical same-pack records and revalidated for source ownership and health at Preview and Commit. Waves requires at least one item, forbids nulls/duplicates, and uses order as the runtime encounter sequence; Commit requires a run-profile rebind/restart. Removing a reference does not delete its Wave asset, and adding one does not create an asset. IDs, all other references/collections, wave schedules and entries, nested catalogs, presentation structures, and JSON are outside this workflow. Do not run setup repair while an edit is staged. ScriptableObjects remain authoritative and no mirror, replacement asset, or generic CRUD path is created.
