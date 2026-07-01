# Canonical Idle Auto Defense Game Flow

The template owns a starter idle auto-defense flow. Product games should begin by replacing content and balance before forking orchestration.

```text
Boot
-> resolve assigned content pack/set
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

The generated scene is intentionally compact: enemies spawn from the perimeter, the player tower sits in the middle, visible direct/projectile mounts fire, and a small HUD shows run state, rewards, upgrade progress, and save controls. Advanced theme, economy, and product-specific flow should be added in the generated product folder once this starter loop is solid.
