# Default Content And Balance

The private template source carries one neutral authored starter pack under:

```text
TemplateSource~/BasicIdleAutoDefenseGame/Content
```

The setup wizard copies this content into the generated product folder and remaps GUIDs. Product teams should tune the generated assets, not the package source.

| Folder | Purpose |
| --- | --- |
| `Enemies` | Starter enemy definitions for the generated run. |
| `Attacks` | Direct and projectile attack recipes plus starter variations. |
| `Weapons` | Tower definitions wired to the attack recipes. |
| `Waves` | Spawn profiles used by the starter encounter. |
| `Upgrades` | Starter run upgrade choices. |
| `ContentSets` | Playable run recipe assigned by the generated scene. |
| `ContentPacks` | Wrapper assigned by the generated scene. |

Starter tuning is intentionally readable, not commercial. Use it to verify spawning, targeting, attacks, upgrades, rewards, and save smoke before building a real product loop.
