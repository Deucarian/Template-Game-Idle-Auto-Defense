# Validation

Expected validation coverage:

- Package import through the shared validation project.
- Sample metadata discovery and import.
- Sample path checks for `Scenes`, `Prefabs`, `Scripts`, `Content`, and `Tests`.
- Sample scene load.
- Template menu helper checks for starter scene discovery, documentation discovery, and sample save reset.
- EditMode tests for definitions, upgrade draft, save/load, corrupted save recovery, offline reward, and progression reward.
- PlayMode smoke for direct and projectile weapons, enemy kills, objective contact/failure handling, terminal encounter state, offline reward, and encounter reward.
- Imported sample smoke for bootstrap inheritance, save reset, and save/progression composition.

Use Deucarian Test Automation for durable batch results:

```text
Deucarian.TestAutomation.BatchTestRunner.RunEditMode
Deucarian.TestAutomation.BatchTestRunner.RunPlayMode
```
