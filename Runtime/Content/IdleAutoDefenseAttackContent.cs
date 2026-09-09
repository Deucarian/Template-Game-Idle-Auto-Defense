using System;
using System.Collections.Generic;
using Deucarian.Attacks;
using Deucarian.Attacks.Authoring;
using Deucarian.AutoDefense;
using Deucarian.Combat;
using Deucarian.DefenseGames;
using Deucarian.Projectiles;
using Deucarian.WorldSpawning;
using UnityEngine;
using static Deucarian.TemplateGameIdleAutoDefense.BasicIdleAutoDefenseGame;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    // Owns starter attack recipes and their combat/projectile runtime conversion.
    internal static class IdleAutoDefenseAttackContent
    {
        private static readonly string[] RequiredTemplateAttackIds =
        {
            PulseAttackId.Value,
            ShardAttackId.Value,
            ArcBurstAttackId.Value,
            HomingPulseAttackId.Value
        };

        internal static CombatCatalog CreateCombatCatalog(
            IReadOnlyList<AttackDefinitionAsset> attackRecipes = null,
            IReadOnlyList<EnemyDefinitionAsset> enemyDefinitions = null,
            IdleAutoDefenseGameRulesAsset gameRules = null)
        {
            attackRecipes ??= CreateAttackRecipes();
            enemyDefinitions ??= IdleAutoDefenseEnemyContent.CreateEnemyDefinitions();
            var damageTypes = new List<DamageTypeDefinition>();
            var damageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddDamageType(damageTypes, damageIds, DamageType);
            if (gameRules != null && !string.IsNullOrWhiteSpace(gameRules.DamageTypeId))
                AddDamageType(damageTypes, damageIds, new DamageTypeId(gameRules.DamageTypeId));
            for (int i = 0; i < attackRecipes.Count; i++)
            {
                AttackDefinitionAsset recipe = attackRecipes[i];
                if (recipe == null || recipe.Mechanics == null || string.IsNullOrWhiteSpace(recipe.Mechanics.DamageTypeId)) continue;
                AddDamageType(damageTypes, damageIds, new DamageTypeId(recipe.Mechanics.DamageTypeId));
            }

            for (int i = 0; i < enemyDefinitions.Count; i++)
            {
                EnemyDefinitionAsset enemy = enemyDefinitions[i];
                if (enemy == null || enemy.Stats == null || string.IsNullOrWhiteSpace(enemy.Stats.DamageTypeId)) continue;
                AddDamageType(damageTypes, damageIds, new DamageTypeId(enemy.Stats.DamageTypeId));
            }

            var statuses = new List<StatusEffectDefinition>();
            var statusIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < attackRecipes.Count; i++)
            {
                AttackDefinitionAsset recipe = attackRecipes[i];
                if (recipe == null) continue;
                StatusEffectDefinition[] definitions = recipe.CreateStatusDefinitions();
                for (int j = 0; j < definitions.Length; j++)
                    if (definitions[j] != null && statusIds.Add(definitions[j].Id.Value))
                        statuses.Add(definitions[j]);
            }

            return new CombatCatalog(damageTypes, statuses);
        }

        internal static AttackRuntime CreateAttackRuntime(CombatCatalog catalog, AutoDefenseDefinition definition, IReadOnlyList<AttackDefinitionAsset> attackRecipes = null)
        {
            var runtime = new AttackRuntime(catalog, CreateAttackDefinitions(attackRecipes ?? CreateAttackRecipes()));
            for (int i = 0; i < definition.WeaponModules.Count; i++)
                runtime.RegisterSource(definition.WeaponModules[i].Source);
            return runtime;
        }

        internal static ProjectileDefinition CreateProjectileDefinition()
        {
            return CreateProjectileDefinitions()[0];
        }

        internal static ProjectileDefinition[] CreateProjectileDefinitions(IReadOnlyList<AttackDefinitionAsset> attackRecipes = null)
        {
            attackRecipes ??= CreateAttackRecipes();
            var definitions = new List<ProjectileDefinition>();
            for (int i = 0; i < attackRecipes.Count; i++)
            {
                AttackDefinitionAsset recipe = attackRecipes[i];
                if (recipe == null || recipe.Delivery == null || recipe.Mechanics == null) continue;
                if (recipe.Delivery.Mode != AttackRecipeDeliveryMode.Projectile) continue;
                definitions.Add(new ProjectileDefinition(
                    new ProjectileDefinitionId(recipe.Delivery.ProjectileDefinitionId),
                    new WorldSpawnableId(recipe.Delivery.ProjectileSpawnableId),
                    new DamageTypeId(recipe.Mechanics.DamageTypeId),
                    recipe.Mechanics.DamageAmount,
                    recipe.Delivery.ProjectileLifetimeTicks,
                    recipe.Delivery.ProjectileSpeed,
                    recipe.Delivery.MaxImpacts));
            }

            return definitions.ToArray();
        }

        internal static AttackDefinitionAsset[] CreateAttackRecipes()
        {
            return new[]
            {
                AttackDefinitionAsset.CreateTransient(
                    PulseAttackId.Value,
                    "Pulse Beam",
                    AttackRecipeDeliveryMode.Hitscan,
                    DamageType.Value,
                    5.0f,
                    72,
                    5.0f,
                    AttackRecipeTargetingMode.Nearest),
                AttackDefinitionAsset.CreateTransient(
                    ShardAttackId.Value,
                    "Shard Projectile",
                    AttackRecipeDeliveryMode.Projectile,
                    DamageType.Value,
                    3.0f,
                    0,
                    4.7f,
                    AttackRecipeTargetingMode.Strongest,
                    projectileDefinitionId: ShardProjectileId.Value,
                    projectileSpawnableId: ProjectileSpawnableId.Value,
                    projectileSpeed: 4.2f,
                    projectileLifetimeTicks: 150,
                    pierceCount: 0),
                AttackDefinitionAsset.CreateTransient(
                    ArcBurstAttackId.Value,
                    "Arc Burst",
                    AttackRecipeDeliveryMode.Area,
                    DamageType.Value,
                    8.0f,
                    108,
                    4.1f,
                    AttackRecipeTargetingMode.Strongest),
                AttackDefinitionAsset.CreateTransient(
                    HomingPulseAttackId.Value,
                    "Homing Pulse",
                    AttackRecipeDeliveryMode.Projectile,
                    DamageType.Value,
                    8.0f,
                    92,
                    6.3f,
                    AttackRecipeTargetingMode.LowestHealth,
                    projectileDefinitionId: HomingPulseProjectileId.Value,
                    projectileSpawnableId: HomingPulseProjectileId.Value,
                    projectileSpeed: 4.4f,
                    projectileLifetimeTicks: 150,
                    homing: true,
                    pierceCount: 1)
            };
        }

        internal static AttackDefinitionAsset[] ResolveAttackRecipesForTemplate(IReadOnlyList<AttackDefinitionAsset> assignedRecipes, out int rejectedRecipeCount)
        {
            rejectedRecipeCount = 0;
            if (assignedRecipes == null || assignedRecipes.Count == 0)
                return CreateAttackRecipes();

            var recipes = new List<AttackDefinitionAsset>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < assignedRecipes.Count; i++)
            {
                AttackDefinitionAsset recipe = assignedRecipes[i];
                if (recipe == null)
                {
                    rejectedRecipeCount++;
                    continue;
                }

                AttackRecipeValidationReport report = AttackRecipeValidator.Validate(recipe, AttackRecipeValidationOptions.RuntimeFriendly);
                if (!report.IsValid)
                {
                    rejectedRecipeCount++;
                    continue;
                }

                string id = recipe.Id.Trim();
                if (!seen.Add(id))
                {
                    rejectedRecipeCount++;
                    continue;
                }

                recipes.Add(recipe);
            }

            if (recipes.Count == 0)
                return CreateAttackRecipes();

            int missingRequired = CountMissingRequiredTemplateAttackIds(recipes);
            if (missingRequired > 0)
            {
                rejectedRecipeCount += missingRequired;
                return CreateAttackRecipes();
            }

            return recipes.ToArray();
        }

        internal static AttackDefinition[] CreateAttackDefinitions(IReadOnlyList<AttackDefinitionAsset> attackRecipes)
        {
            if (attackRecipes == null || attackRecipes.Count == 0) throw new ArgumentException("At least one attack recipe is required.", nameof(attackRecipes));
            var definitions = new AttackDefinition[attackRecipes.Count];
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < attackRecipes.Count; i++)
            {
                if (attackRecipes[i] == null) throw new ArgumentException("Attack recipe cannot be null.", nameof(attackRecipes));
                AttackRecipeValidationReport report = AttackRecipeValidator.Validate(attackRecipes[i], AttackRecipeValidationOptions.RuntimeFriendly);
                if (!report.IsValid) throw new ArgumentException("Attack recipe is invalid: " + IdleAutoDefenseContentValidation.GetFirstValidationError(report), nameof(attackRecipes));
                if (!seen.Add(attackRecipes[i].Id.Trim())) throw new ArgumentException("Duplicate attack recipe ID: " + attackRecipes[i].Id, nameof(attackRecipes));
                definitions[i] = attackRecipes[i].ToRuntimeDefinition();
            }

            return definitions;
        }

        internal static AttackDefinitionAsset FindAttackRecipe(IReadOnlyList<AttackDefinitionAsset> recipes, string id)
        {
            if (recipes == null || string.IsNullOrWhiteSpace(id)) return null;
            for (int i = 0; i < recipes.Count; i++)
            {
                AttackDefinitionAsset recipe = recipes[i];
                if (recipe != null && string.Equals(recipe.Id, id, StringComparison.OrdinalIgnoreCase))
                    return recipe;
            }

            return null;
        }

        private static void AddDamageType(List<DamageTypeDefinition> damageTypes, HashSet<string> seen, DamageTypeId id)
        {
            if (!id.IsEmpty && seen.Add(id.Value)) damageTypes.Add(new DamageTypeDefinition(id));
        }

        private static int CountMissingRequiredTemplateAttackIds(IReadOnlyList<AttackDefinitionAsset> recipes)
        {
            int missing = 0;
            for (int i = 0; i < RequiredTemplateAttackIds.Length; i++)
                if (!ContainsRecipeId(recipes, RequiredTemplateAttackIds[i]))
                    missing++;
            return missing;
        }

        private static bool ContainsRecipeId(IReadOnlyList<AttackDefinitionAsset> recipes, string id)
        {
            for (int i = 0; i < recipes.Count; i++)
            {
                AttackDefinitionAsset recipe = recipes[i];
                if (recipe != null && string.Equals(recipe.Id, id, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
