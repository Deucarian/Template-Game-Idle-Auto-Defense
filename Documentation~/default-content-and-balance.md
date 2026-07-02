# Default Content And Balance

The private template source carries one neutral authored starter pack under:

```text
TemplateSource~/BasicIdleAutoDefenseGame/Content
```

The setup wizard copies this content into `Assets/GameContent/IdleAutoDefense` by default and remaps GUIDs. Scripts, docs, visuals, audio, and runtime resources go into the chosen generated game root, while the visible playable scene is generated at `Assets/OPEN_THIS_TO_TEST_IdleAutoDefense_PlayableGame/OPEN_THIS_TO_TEST_IdleAutoDefense_PlayableGame.unity`. Product teams should tune the generated authored assets, not the package source.

| Folder | Purpose |
| --- | --- |
| `Enemies` | Starter enemy definitions for the generated run. |
| `Attacks` | Direct and projectile attack recipes plus starter variations. |
| `Weapons` | Tower definitions wired to the attack recipes. |
| `Waves` | Spawn profiles used by the starter encounter. |
| `Upgrades` | Starter authored upgrade definitions used by content validation and product tuning. |
| `ContentSets` | Playable run recipe assigned by the generated scene. |
| `ContentPacks` | Wrapper assigned by the generated scene. |

The starter pack contains four generic attacks, six enemies, four tower weapons, seven spawn profiles, six authored upgrades, one content set, and one content pack. Starter tuning is intentionally readable, not commercial. Use it to verify spawning, targeting, attacks, upgrades, elite/boss rewards, and save smoke before building a real product loop.

The playable sample also builds a runtime three-choice reward draft from the resolved tower weapons. Kills grant commander experience, wave completion contributes experience, elite kills always queue a reward, and boss kills queue a stronger reward. `IdleAutoDefenseRewardDraftSettings` exposes the sample XP values, rarity weights, unlock weighting, progression thresholds, and projectile retarget tolerance. The generated controller's reward draft catalog holds the actual unlock, normal, Epic, Legendary, and base reward entries so an asset flip can change names, effects, and tracks without digging through controller branches. Each owned weapon has three normal investments, three Epic investments unlocked after the normal track, and one Legendary investment unlocked after the Epic track by default. Product games can keep this sample progression as glue or replace it after their authored `Assets/GameContent` pack has real balance.
