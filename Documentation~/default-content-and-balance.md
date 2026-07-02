# Default Content And Balance

The private template source carries one neutral authored starter pack under:

```text
TemplateSource~/BasicIdleAutoDefenseGame/Content
```

The setup wizard copies this content into `Assets/GameContent/IdleAutoDefense` by default and remaps GUIDs. Scene files, scripts, docs, visuals, and audio still go into the chosen generated game root. Product teams should tune the generated authored assets, not the package source.

| Folder | Purpose |
| --- | --- |
| `Enemies` | Starter enemy definitions for the generated run. |
| `Attacks` | Direct and projectile attack recipes plus starter variations. |
| `Weapons` | Tower definitions wired to the attack recipes. |
| `Waves` | Spawn profiles used by the starter encounter. |
| `Upgrades` | Starter run upgrade choices. |
| `ContentSets` | Playable run recipe assigned by the generated scene. |
| `ContentPacks` | Wrapper assigned by the generated scene. |

The starter pack contains four generic attacks, four enemies, four tower weapons, five spawn profiles, six upgrades, one content set, and one content pack. Starter tuning is intentionally readable, not commercial. Use it to verify spawning, targeting, attacks, upgrades, rewards, and save smoke before building a real product loop.
