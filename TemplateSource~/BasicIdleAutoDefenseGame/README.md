# Basic Idle Auto Defense Template Source

This folder is private package template source. Unity Package Manager should not present it as an importable sample.

The setup wizard copies this source into a product-owned game root and a product-owned authored content root:

```text
Tools > Deucarian > Templates > Idle Auto Defense > Create Playable Game
```

The generated scene references the generated content pack and content set after GUID remapping. Open the generated scene at `Assets/OPEN_THIS_TO_TEST_IdleAutoDefense_PlayableGame/OPEN_THIS_TO_TEST_IdleAutoDefense_PlayableGame.unity`. Edit generated code/docs/visuals/resources under the chosen game root, and edit gameplay definitions under `Assets/GameContent/IdleAutoDefense` or the setup wizard's chosen content root.

The generated sample is meant to be played, not inspected as a static showcase. It demonstrates short-range starter defense, offscreen-style enemy approach, Kenney 3D towers/enemies/projectiles, turret aiming, muzzle-based firing, recoil, muzzle flash, visible projectile/VFX/audio feedback, damage numbers, early reward cards, module unlocks, and a live Overdrive button. Tune the first two minutes through generated authored assets first, then swap Kenney visuals under the generated runtime resources.

## Folder Map

```text
BasicIdleAutoDefenseGame
|-- Audio
|-- Content
|   |-- Attacks
|   |-- Enemies
|   |-- Weapons
|   |-- Waves
|   |-- Upgrades
|   |-- ContentSets
|   `-- ContentPacks
|-- Prefabs
|-- Resources
|   `-- Kenney
|       `-- IdleAutoDefense
|           `-- Models
|-- Scenes
|-- Scripts
`-- Visuals
```
