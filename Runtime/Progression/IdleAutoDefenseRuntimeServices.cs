using System;
using Deucarian.Attacks;
using Deucarian.AutoDefense;
using Deucarian.Combat;
using Deucarian.Encounters;
using Deucarian.Projectiles;
using Deucarian.WeaponSystems;
using Deucarian.WeaponSystems.Authoring;
using Deucarian.WorldNavigation;
using Deucarian.WorldSpawning;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    /// <summary>Resolved managed runtime construction, before world objects are created.</summary>
    internal sealed class IdleAutoDefenseRuntimePlan
    {
        internal readonly AutoDefenseDefinition Definition;
        internal readonly CombatCatalog Catalog;
        internal readonly WeaponRuntime Weapons;

        internal IdleAutoDefenseRuntimePlan(IdleAutoDefenseContentBinding content, WeaponDefinitionAsset[] activeWeapons)
        {
            Definition = BasicIdleAutoDefenseGame.CreateDefinition(content.Enemies, activeWeapons, content.GameRules,
                content.RunProfile == null ? 1f : content.RunProfile.DifficultyMultiplier);
            Catalog = BasicIdleAutoDefenseGame.CreateCombatCatalog(content.Attacks, content.Enemies, content.GameRules);
            AttackRuntime attacks = BasicIdleAutoDefenseGame.CreateAttackRuntime(Catalog, Definition, content.Attacks);
            Weapons = BasicIdleAutoDefenseGame.CreateWeaponRuntime(Definition, attacks);
        }
    }

    /// <summary>Owns one run's lower gameplay services and their spawn-service lifetime.</summary>
    internal sealed class IdleAutoDefenseRuntimeServices
    {
        internal AutoDefenseRuntime Runtime { get; private set; }
        internal EncounterRuntime Encounter { get; private set; }
        internal ProjectileRuntime Projectiles { get; private set; }
        internal WorldSpawnService EnemySpawning { get; private set; }
        internal WorldSpawnService ProjectileSpawning { get; private set; }
        internal WorldNavigationService Navigation { get; private set; }
        internal WorldNavigationService ProjectileNavigation { get; private set; }
        internal ProjectileDefinition[] ProjectileDefinitions { get; private set; } = Array.Empty<ProjectileDefinition>();
        private bool _released;

        internal IdleAutoDefenseRuntimeServices(IdleAutoDefenseRuntimePlan plan, IdleAutoDefenseContentBinding content,
            IdleAutoDefenseSpawnablePresentation spawnables, ISpawnPoseResolver projectilePoses, EncounterDefinition encounterOverride)
        {
            try
            {
                var poses = new TemplateJitteredPerimeterPoseResolver(plan.Definition.Objective, plan.Definition.SpawnRing);
                EnemySpawning = new WorldSpawnService(new SpawnableCatalog(spawnables.CreateEnemySpawnables(content.Enemies)),
                    poses, rootName: "TemplateIdleEnemies");
                Navigation = new WorldNavigationService();
                Encounter = new EncounterRuntime(encounterOverride ?? BasicIdleAutoDefenseGame.CreateEncounterDefinition(
                    content.Waves, content.RunProfile == null ? 20260623 : content.RunProfile.EncounterSeed));
                Runtime = new AutoDefenseRuntime(plan.Definition, EnemySpawning, Navigation, plan.Weapons, plan.Catalog,
                    Encounter, poses: poses, candidateCapacity: 64);
                ProjectileDefinitions = BasicIdleAutoDefenseGame.CreateProjectileDefinitions(content.Attacks);
                ProjectileSpawning = new WorldSpawnService(new SpawnableCatalog(spawnables.CreateProjectileSpawnables(ProjectileDefinitions)),
                    projectilePoses, rootName: "TemplateIdleProjectiles");
                ProjectileNavigation = new WorldNavigationService();
                Projectiles = new ProjectileRuntime(plan.Catalog, ProjectileDefinitions,
                    new WorldSpawnProjectileSpawner(ProjectileSpawning, new WorldSpawnChannelId("projectile-origin")),
                    new WorldNavigationProjectileNavigator(ProjectileNavigation));
            }
            catch
            {
                Dispose(true);
                throw;
            }
        }

        internal void ClearSpawnedObjects()
        {
            EnemySpawning?.Clear(false);
            ProjectileSpawning?.Clear(false);
        }

        internal void Dispose(bool destroySceneObjects)
        {
            if (_released) return;
            _released = true;
            if (destroySceneObjects)
            {
                EnemySpawning?.Dispose();
                ProjectileSpawning?.Dispose();
            }
            else ClearSpawnedObjects();
            EnemySpawning = null;
            ProjectileSpawning = null;
            ProjectileDefinitions = Array.Empty<ProjectileDefinition>();
        }
    }
}
