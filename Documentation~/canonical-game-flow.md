# Canonical Idle Auto Defense Game Flow

The template owns a starter idle auto-defense flow. Product games should begin by replacing content and balance before forking orchestration.

```text
Boot
-> strictly validate and bind assigned content pack/set plus six authored-core owners
-> apply offline reward smoke
-> start run
-> spawn profiles
-> auto weapons fire
-> apply upgrade drafts or buy runtime upgrades
-> win/fail
-> apply rewards
-> save smoke
-> restart
```

The generated scene is intentionally compact: enemies spawn from the perimeter, the player tower sits in the middle, visible direct/projectile mounts fire, and a small HUD shows run state, rewards, upgrade progress, and save controls. Its existing economy is authored under `Assets/GameContent`; later theme and product-specific UI flow belong in the generated game root. Invalid required gameplay content blocks startup instead of silently selecting fallback balance.
