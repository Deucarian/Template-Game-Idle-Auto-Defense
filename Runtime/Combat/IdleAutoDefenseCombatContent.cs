using System;
using System.Collections.Generic;
using Deucarian.Attacks;
using Deucarian.Attacks.Authoring;
using Deucarian.AutoDefense;
using Deucarian.Combat;
using Deucarian.Projectiles;
using Deucarian.WeaponSystems;
using Deucarian.WorldNavigation;
using Deucarian.WorldSpawning;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    internal sealed class IdleAutoDefenseCombatContent
    {
        private readonly Func<AttackDefinitionAsset[]> _attacks;
        private readonly Func<EnemyDefinitionAsset[]> _enemies;
        private readonly Func<ProjectileDefinition[]> _projectiles;
        private AttackDefinitionAsset[] _resolvedAttackRecipes => _attacks();
        private EnemyDefinitionAsset[] _resolvedEnemyDefinitions => _enemies();
        private ProjectileDefinition[] _resolvedProjectileDefinitions => _projectiles();

        internal IdleAutoDefenseCombatContent(Func<AttackDefinitionAsset[]> attacks,
            Func<EnemyDefinitionAsset[]> enemies, Func<ProjectileDefinition[]> projectiles)
        {
            _attacks = attacks ?? throw new ArgumentNullException(nameof(attacks));
            _enemies = enemies ?? throw new ArgumentNullException(nameof(enemies));
            _projectiles = projectiles ?? throw new ArgumentNullException(nameof(projectiles));
        }

        internal AttackDefinitionAsset FindAttackRecipeForPresentation(string attackId)
        {
            if (string.IsNullOrWhiteSpace(attackId)) return null;
            for (int i = 0; i < _resolvedAttackRecipes.Length; i++)
            {
                AttackDefinitionAsset attack = _resolvedAttackRecipes[i];
                if (attack != null && string.Equals(attack.Id, attackId, StringComparison.OrdinalIgnoreCase))
                    return attack;
            }

            return null;
        }

        internal AttackDefinitionAsset FindAttackRecipeForProjectile(ProjectileDefinition definition)
        {
            if (definition == null) return null;
            for (int i = 0; i < _resolvedAttackRecipes.Length; i++)
            {
                AttackDefinitionAsset attack = _resolvedAttackRecipes[i];
                if (attack == null || attack.Delivery == null) continue;
                if (string.Equals(attack.Delivery.ProjectileDefinitionId, definition.Id.Value, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(attack.Delivery.ProjectileSpawnableId, definition.SpawnableId.Value, StringComparison.OrdinalIgnoreCase))
                {
                    return attack;
                }
            }

            return null;
        }

        internal EnemyDefinitionAsset FindEnemyDefinitionForPresentation(WorldSpawnableId spawnableId)
        {
            if (spawnableId.IsEmpty) return null;
            for (int i = 0; i < _resolvedEnemyDefinitions.Length; i++)
            {
                EnemyDefinitionAsset enemy = _resolvedEnemyDefinitions[i];
                if (enemy != null && string.Equals(enemy.Id, spawnableId.Value, StringComparison.OrdinalIgnoreCase))
                    return enemy;
            }

            return null;
        }

        internal ProjectileDefinition FindProjectileDefinition(ProjectileDefinitionId id)
        {
            for (int i = 0; i < _resolvedProjectileDefinitions.Length; i++)
            {
                ProjectileDefinition definition = _resolvedProjectileDefinitions[i];
                if (definition != null && definition.Id.Equals(id))
                    return definition;
            }

            return null;
        }
    }
}
