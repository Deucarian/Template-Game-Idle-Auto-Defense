# Scrap Frontier Template Source

This folder is private package template source. Unity Package Manager should not present it as an importable sample.

The setup wizard copies this source into a product-owned game root and a product-owned authored content root:

```text
Tools > Deucarian > Templates > Idle Auto Defense > Create Playable Game
```

Select Scrap Frontier Only or Both. The generated scene references the generated content pack, content set, and player-experience root after GUID remapping. Open `Assets/OPEN_THIS_TO_TEST_ScrapFrontier_PlayableGame/OPEN_THIS_TO_TEST_ScrapFrontier_PlayableGame.unity`. Edit generated code/docs/visuals/audio under the chosen game root, and edit authored gameplay/presentation definitions under the selected content root.

The generated sample is meant to be played. It opens on a main menu and demonstrates a complete 4:40 defense with onboarding, responsive HUD, mounted-module purchases, Redline, authored reward cards, pause/build/settings, offline claim, elite/boss bars and markers, run summary, pack-scoped persistence, and hidden `F1` debug diagnostics.

## Folder Map

```text
ScrapFrontierGame
|-- Audio
|-- Content
|   |-- Attacks
|   |-- Economy
|   |-- Enemies
|   |-- GameRules
|   |-- OfflineProgression
|   |   `-- offline-progression.idle-auto-defense.scrap-frontier.asset
|   |-- Presentation
|   |-- Progression
|   |-- Rewards
|   |-- RunProfiles
|   |-- Weapons
|   |-- Waves
|   |-- Upgrades
|   |-- ContentSets
|   `-- ContentPacks
|-- Prefabs
|-- Scenes
`-- Visuals
```
