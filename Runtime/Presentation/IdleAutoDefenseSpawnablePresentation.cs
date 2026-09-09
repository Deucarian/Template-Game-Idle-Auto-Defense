using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Deucarian.Attacks;
using Deucarian.Attacks.Authoring;
using Deucarian.AutoDefense;
using Deucarian.Combat;
using Deucarian.Common;
using Deucarian.DefenseGames;
using Deucarian.Encounters;
using Deucarian.IdleProgression;
using Deucarian.Monetization;
using Deucarian.Persistence;
using Deucarian.Progression;
using Deucarian.Projectiles;
using Deucarian.RunUpgrades;
using Deucarian.RunUpgrades.Authoring;
using Deucarian.WeaponSystems;
using Deucarian.WeaponSystems.Authoring;
using Deucarian.WorldNavigation;
using Deucarian.WorldSpawning;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    internal sealed class IdleAutoDefenseSpawnablePresentation
    {
        private readonly IdleAutoDefenseWorldResources _resources;

        private readonly IdleAutoDefenseVisualAssets _assets;

        private readonly IdleAutoDefenseTargetPresentation _targets;

        private readonly IdleAutoDefensePresentationQueries _queries;

        internal IdleAutoDefenseSpawnablePresentation(IdleAutoDefenseWorldResources resources, IdleAutoDefenseVisualAssets assets, IdleAutoDefenseTargetPresentation targets, IdleAutoDefensePresentationQueries queries)
        {
            _resources = resources;
            _assets = assets;
            _targets = targets;
            _queries = queries;
        }

        internal GameObject GetProjectilePrefab(ProjectileDefinition definition)
        {
            AttackDefinitionAsset attack = _queries.FindProjectileAttack(definition);
            string key = definition == null || definition.Id.IsEmpty ? "projectile.default" : definition.Id.Value;
            if (_resources.ProjectilePrefabs.TryGetValue(key, out GameObject cached) && cached != null)
                return cached;

            GameObject authoredPrefab = attack != null && attack.Delivery != null ? attack.Delivery.ProjectilePrefab : null;
            Color color = _targets.ResolveAttackColor(attack);
            GameObject prefab = authoredPrefab != null
                ? _assets.CreateRuntimeVisualPrefab(
                    "Kenney Projectile Runtime Prefab " + key,
                    authoredPrefab,
                    PrimitiveType.Sphere,
                    color,
                    "Art/projectile_rocket",
                    new Vector3(0f, 0f, -0.02f),
                    new Vector3(0.72f, 0.72f, 1f),
                    true,
                    35,
                    "ProjectileVisual",
                    attack != null && attack.Delivery != null ? attack.Delivery.ProjectileDefinitionId : key,
                    _targets.ResolveWeaponIdForAttack(attack),
                    attack == null ? string.Empty : attack.Id,
                    "ProjectilePrefab")
                : _assets.CreateProjectileModelPrefab("Kenney Projectile Runtime Prefab " + key, IdleAutoDefenseVisualAssets.ResolveProjectileModelName(attack), color);
            _resources.ProjectilePrefabs[key] = prefab;
            return prefab;
        }

        internal SpawnableDefinition[] CreateProjectileSpawnables(IReadOnlyList<ProjectileDefinition> projectileDefinitions)
        {
            var spawnables = new List<SpawnableDefinition>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < projectileDefinitions.Count; i++)
            {
                ProjectileDefinition definition = projectileDefinitions[i];
                if (definition == null || !seen.Add(definition.SpawnableId.Value)) continue;
                spawnables.Add(new SpawnableDefinition(definition.SpawnableId, new GameObjectPrefabProvider(GetProjectilePrefab(definition)), 4, 32));
            }

            return spawnables.ToArray();
        }

        internal SpawnableDefinition[] CreateEnemySpawnables(IReadOnlyList<EnemyDefinitionAsset> enemyDefinitions)
        {
            var spawnables = new List<SpawnableDefinition>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < enemyDefinitions.Count; i++)
            {
                EnemyDefinitionAsset definition = enemyDefinitions[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.Id) || !seen.Add(definition.Id)) continue;
                spawnables.Add(new SpawnableDefinition(new WorldSpawnableId(definition.Id), new GameObjectPrefabProvider(GetEnemyPrefab(definition)), 4, 64));
            }

            return spawnables.ToArray();
        }

        internal static SpawnableDefinition[] CreateEnemySpawnables(AutoDefenseDefinition definition, GameObject prefab)
        {
            var spawnables = new SpawnableDefinition[definition.Enemies.Count];
            for (int i = 0; i < definition.Enemies.Count; i++)
                spawnables[i] = new SpawnableDefinition(definition.Enemies[i].SpawnableId, new GameObjectPrefabProvider(prefab), 4, 64);
            return spawnables;
        }

        internal GameObject GetEnemyPrefab(EnemyDefinitionAsset enemy)
        {
            string id = enemy == null ? string.Empty : enemy.Id;
            if (string.IsNullOrWhiteSpace(id)) return _resources.EnemyPrefab;
            if (_resources.EnemyPrefabs.TryGetValue(id, out GameObject cached) && cached != null)
                return cached;

            Color color = IdleAutoDefenseVisualAssets.ResolveEnemyFallbackColor(id);
            GameObject authoredPrefab = enemy != null && enemy.Presentation != null ? enemy.Presentation.Prefab : null;
            GameObject prefab = authoredPrefab != null
                ? _assets.CreateRuntimeVisualPrefab(
                    "Kenney Enemy Runtime Prefab " + id,
                    authoredPrefab,
                    PrimitiveType.Capsule,
                    color,
                    IdleAutoDefenseVisualAssets.ResolveEnemyKenneyArtPath(id),
                    new Vector3(0f, 0.62f, -0.08f),
                    IdleAutoDefenseVisualAssets.ResolveEnemySpriteScale(id),
                    false,
                    24,
                    "EnemyVisual",
                    id,
                    string.Empty,
                    string.Empty,
                    "EnemyPrefab")
                : _assets.CreateEnemyModelPrefab("Kenney Enemy Runtime Prefab " + id, id, color);
            IdleAutoDefenseVisualAssets.EnsureEnemyModelPresentation(prefab, color);
            _resources.EnemyPrefabs[id] = prefab;
            return prefab;
        }
    }
}
