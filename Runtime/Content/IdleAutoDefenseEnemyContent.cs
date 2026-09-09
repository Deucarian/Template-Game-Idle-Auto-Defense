using System;
using System.Collections.Generic;
using Deucarian.Attacks.Authoring;
using Deucarian.AutoDefense;
using Deucarian.Combat;
using Deucarian.DefenseGames;
using Deucarian.WorldSpawning;
using static Deucarian.TemplateGameIdleAutoDefense.BasicIdleAutoDefenseGame;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    // Owns starter enemy balance, authored admission and difficulty-scaled runtime conversion.
    internal static class IdleAutoDefenseEnemyContent
    {
        private static readonly string[] RequiredTemplateEnemyIds =
        {
            SwarmEnemySpawnableId.Value,
            RunnerEnemySpawnableId.Value,
            TankEnemySpawnableId.Value,
            ShieldedEnemySpawnableId.Value,
            EliteEnemySpawnableId.Value,
            BossEnemySpawnableId.Value
        };

        internal static EnemyDefinitionAsset[] CreateEnemyDefinitions()
        {
            return new[]
            {
                EnemyDefinitionAsset.CreateTransient(SwarmEnemySpawnableId.Value, "Swarm", EnemyRole.Swarm, 20f, 0.72f, 5, 4f, DamageType.Value, 0.28f, tags: new[] { "idle-auto-defense", "swarm" }),
                EnemyDefinitionAsset.CreateTransient(RunnerEnemySpawnableId.Value, "Runner", EnemyRole.Fast, 26f, 1.1f, 5, 5f, DamageType.Value, 0.27f, tags: new[] { "idle-auto-defense", "runner" }),
                EnemyDefinitionAsset.CreateTransient(TankEnemySpawnableId.Value, "Tank", EnemyRole.Tank, 74f, 0.48f, 5, 10f, DamageType.Value, 0.48f, tags: new[] { "idle-auto-defense", "tank" }),
                EnemyDefinitionAsset.CreateTransient(ShieldedEnemySpawnableId.Value, "Shielded", EnemyRole.Basic, 46f, 0.64f, 5, 7f, DamageType.Value, 0.38f, tags: new[] { "idle-auto-defense", "shielded" }),
                EnemyDefinitionAsset.CreateTransient(EliteEnemySpawnableId.Value, "Elite", EnemyRole.Boss, 170f, 0.54f, 5, 26f, DamageType.Value, 0.54f, tags: new[] { "idle-auto-defense", "elite" }),
                EnemyDefinitionAsset.CreateTransient(BossEnemySpawnableId.Value, "Boss", EnemyRole.Boss, 390f, 0.36f, 5, 60f, DamageType.Value, 0.82f, tags: new[] { "idle-auto-defense", "boss" })
            };
        }

        internal static EnemyDefinitionAsset[] ResolveEnemyDefinitionsForTemplate(IReadOnlyList<EnemyDefinitionAsset> assignedDefinitions, out int rejectedDefinitionCount)
        {
            rejectedDefinitionCount = 0;
            if (assignedDefinitions == null || assignedDefinitions.Count == 0)
                return CreateEnemyDefinitions();

            var definitions = new List<EnemyDefinitionAsset>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < assignedDefinitions.Count; i++)
            {
                EnemyDefinitionAsset definition = assignedDefinitions[i];
                if (definition == null)
                {
                    rejectedDefinitionCount++;
                    continue;
                }

                ContentAuthoringValidationReport report = EnemyDefinitionValidator.Validate(definition, EnemyDefinitionValidationOptions.AssetCreation);
                if (!report.IsValid)
                {
                    rejectedDefinitionCount++;
                    continue;
                }

                string id = definition.Id.Trim();
                if (!seen.Add(id))
                {
                    rejectedDefinitionCount++;
                    continue;
                }

                definitions.Add(definition);
            }

            if (definitions.Count == 0)
                return CreateEnemyDefinitions();

            int missingRequired = CountMissingRequiredTemplateEnemyIds(definitions);
            if (missingRequired > 0)
            {
                rejectedDefinitionCount += missingRequired;
                return CreateEnemyDefinitions();
            }

            return definitions.ToArray();
        }

        internal static AutoDefenseEnemyDefinition[] CreateAutoDefenseEnemyDefinitions(IReadOnlyList<EnemyDefinitionAsset> enemyDefinitions, float difficultyMultiplier = 1f)
        {
            if (enemyDefinitions == null || enemyDefinitions.Count == 0) throw new ArgumentException("At least one enemy definition is required.", nameof(enemyDefinitions));
            var definitions = new AutoDefenseEnemyDefinition[enemyDefinitions.Count];
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < enemyDefinitions.Count; i++)
            {
                EnemyDefinitionAsset enemy = enemyDefinitions[i];
                if (enemy == null) throw new ArgumentException("Enemy definition cannot be null.", nameof(enemyDefinitions));
                ContentAuthoringValidationReport report = EnemyDefinitionValidator.Validate(enemy, EnemyDefinitionValidationOptions.RuntimeFriendly);
                if (!report.IsValid) throw new ArgumentException("Enemy definition is invalid: " + IdleAutoDefenseContentValidation.GetFirstValidationError(report), nameof(enemyDefinitions));
                if (!seen.Add(enemy.Id.Trim())) throw new ArgumentException("Duplicate enemy definition ID: " + enemy.Id, nameof(enemyDefinitions));
                double difficulty = float.IsNaN(difficultyMultiplier) || float.IsInfinity(difficultyMultiplier)
                    ? 1d
                    : Math.Max(0.01d, difficultyMultiplier);
                definitions[i] = new AutoDefenseEnemyDefinition(
                    new WorldSpawnableId(enemy.Id),
                    enemy.Stats.MaximumHealth * difficulty,
                    enemy.Stats.MoveSpeed,
                    enemy.Stats.ContactDamage * difficulty,
                    new DamageTypeId(enemy.Stats.DamageTypeId),
                    enemy.Stats.CollisionRadius);
            }

            return definitions;
        }

        private static AutoDefenseEnemyDefinition Enemy(WorldSpawnableId id, double health, float speed, double contactDamage, float radius)
        {
            return new AutoDefenseEnemyDefinition(id, health, speed, contactDamage, DamageType, radius);
        }

        internal static AutoDefenseEnemyDefinition[] CreateDefaultAutoDefenseEnemyDefinitions()
        {
            return new[]
            {
                Enemy(SwarmEnemySpawnableId, 20, 0.72f, 1, 0.28f),
                Enemy(RunnerEnemySpawnableId, 26, 1.1f, 2, 0.27f),
                Enemy(TankEnemySpawnableId, 74, 0.48f, 5, 0.48f),
                Enemy(ShieldedEnemySpawnableId, 46, 0.64f, 4, 0.38f),
                Enemy(EliteEnemySpawnableId, 170, 0.54f, 10, 0.54f),
                Enemy(BossEnemySpawnableId, 390, 0.36f, 22, 0.82f)
            };
        }

        private static int CountMissingRequiredTemplateEnemyIds(IReadOnlyList<EnemyDefinitionAsset> recipes)
        {
            int missing = 0;
            for (int i = 0; i < RequiredTemplateEnemyIds.Length; i++)
                if (!ContainsEnemyRecipeId(recipes, RequiredTemplateEnemyIds[i]))
                    missing++;
            return missing;
        }

        private static bool ContainsEnemyRecipeId(IReadOnlyList<EnemyDefinitionAsset> recipes, string id)
        {
            for (int i = 0; i < recipes.Count; i++)
            {
                EnemyDefinitionAsset recipe = recipes[i];
                if (recipe != null && string.Equals(recipe.Id, id, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
