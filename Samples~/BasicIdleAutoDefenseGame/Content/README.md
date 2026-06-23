# Content

`starter-content.json` mirrors the constants used by the deterministic starter code. Replace these IDs and values when converting the template into a project-specific data pipeline.

Edit this file first when experimenting with:

- objective health, lives, and contact radius
- spawn ring radius and channels
- direct and projectile weapon IDs
- upgrade IDs
- offline reward caps and rates

The starter runtime does not load this JSON directly. It is a readable map for the values in `Runtime/IdleAutoDefenseTemplate.cs` so a new project can copy the template and wire the values into its own data pipeline.
