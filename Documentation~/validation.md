# Validation

Expected validation coverage:

- Package import through the shared validation project.
- No public Unity Package Manager sample entry.
- Setup service creates product-owned `Scripts`, `Prefabs`, `Visuals`, `Audio`, `Resources`, and `Docs` in the generated game root, creates the visible playable scene under `Assets/OPEN_THIS_TO_TEST_IdleAutoDefense_PlayableGame`, and creates authored gameplay data under `Assets/GameContent`.
- Generated scene references generated product-owned content assets, not template-source GUIDs.
- Generated content pack, content set, reward catalog, economy, run profile, progression, offline progression, and game rules validate with zero errors.
- Generated controller uses all six authored-core owners in Play Mode with strict startup enabled and fallback false.
- Menu checks expose only `Create Playable Game` and `Open Template Docs` under `Tools > Deucarian > Templates > Idle Auto Defense`.
- EditMode tests cover definitions, parity values, strict startup blockers, reward/economy/run/progression/offline mutations, save/load, corrupted save recovery, setup copying, GUID remapping, GCA counts, claims, and canonical references.
- PlayMode smoke covers strict authored binding, spawning, direct/projectile weapons, enemy kills, reward drafts, objective contact/failure handling, terminal encounter state, offline reward, and encounter reward.

Use Deucarian Test Automation when available:

```text
Deucarian.TestAutomation.BatchTestRunner.RunEditMode
Deucarian.TestAutomation.BatchTestRunner.RunPlayMode
```
