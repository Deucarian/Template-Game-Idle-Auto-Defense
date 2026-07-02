# Validation

Expected validation coverage:

- Package import through the shared validation project.
- No public Unity Package Manager sample entry.
- Setup service creates product-owned `Scripts`, `Scenes`, `Prefabs`, `Visuals`, `Audio`, and `Docs` in the generated game root plus authored gameplay data under `Assets/GameContent`.
- Generated scene references generated product-owned content assets, not template-source GUIDs.
- Generated content pack and content set validate with zero errors.
- Generated controller uses assigned content in Play Mode.
- Menu checks expose only `Create Playable Game` and `Open Template Docs` under `Tools > Deucarian > Templates > Idle Auto Defense`.
- EditMode tests cover definitions, upgrade draft, save/load, corrupted save recovery, offline reward, progression reward, setup copying, and GUID remapping.
- PlayMode smoke covers spawning, direct/projectile weapons, enemy kills, objective contact/failure handling, terminal encounter state, offline reward, and encounter reward.

Use Deucarian Test Automation when available:

```text
Deucarian.TestAutomation.BatchTestRunner.RunEditMode
Deucarian.TestAutomation.BatchTestRunner.RunPlayMode
```
