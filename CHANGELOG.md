# Changelog

## Unreleased

- Updated the Basic Idle Auto Defense sample to consume three Attack Authoring recipes: hitscan beam, fire projectile, and homing/status projectile.
- Added runtime conversion from `AttackDefinitionAsset` recipes into Combat, Attacks, Projectiles, and Weapon Systems definitions.
- Added smoke coverage for recipe-generated projectile definitions, status hooks, and presentation event invocation with missing optional audio/VFX.

## 0.1.0 - 2026-06-23

- Added the Idle Auto Defense template package.
- Added a Basic Idle Auto Defense Game sample with scene, bootstrap script, content notes, placeholder prefab notes, and sample tests.
- Added package EditMode and PlayMode smoke tests for definition setup, upgrade drafts, offline rewards, save/load, corrupted save recovery, progression rewards, and deterministic runtime behavior.
