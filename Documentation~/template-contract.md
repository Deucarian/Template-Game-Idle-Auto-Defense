# Idle Auto Defense Template Contract

This template must always generate and open as a complete playable vertical slice. It must not become a hollow systems framework.

> Extract only reusable infrastructure, never the playable vertical slice.

## Product Boundary

- The generated Basic Idle Auto Defense game must play without manual reconstruction.
- The scene, concrete towers, attacks, enemies, waves, bosses, upgrades, economy, reward content, progression content, tuning, presentation bindings, and complete playable loop remain local to this template.
- Package extraction may move only reusable infrastructure through the Deucarian governance process.
- Any future extraction must prove that a newly generated playable scene still binds its authored graph and completes the gameplay smoke tests.

## Authored Source Of Truth

- Asset-flip-critical gameplay values come from the generated ScriptableObject graph under `Assets/GameContent`.
- The normal generated scene must not silently fall back to hidden code-owned balance.
- Missing or invalid sample-critical content blocks strict startup with an actionable validation error.
- The setup wizard must generate every required asset and reference so pressing Play works immediately.
- A duplicate JSON gameplay mirror is not permitted. ScriptableObjects are the single editable source of truth.

## Intentional Fallback

Transient defaults remain only for explicitly unbound debug hosts, focused unit tests, and package-level recovery fixtures. These paths must expose `FallbackModeActive == true`; they are not the generated sample path and are not an alternative product balance owner.

The product UX layer still has later work, including final mobile layout, tutorial, theme, audio palette, menus, and offline-claim presentation. That work may improve the vertical slice, but it must preserve this contract.
