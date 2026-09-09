using System.Collections.Generic;
using Deucarian.Attacks.Authoring;
using Deucarian.AutoDefense;
using Deucarian.DefenseGames;
using Deucarian.WeaponSystems.Authoring;
using UnityEngine;
using static Deucarian.TemplateGameIdleAutoDefense.BasicIdleAutoDefenseGame;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    // Composes the template objective, spawn ring, enemies and weapon modules without owning a live run.
    internal static class IdleAutoDefenseRuntimeFactory
    {
        internal static AutoDefenseDefinition CreateDefinition(
            IReadOnlyList<EnemyDefinitionAsset> enemyDefinitions = null,
            IReadOnlyList<WeaponDefinitionAsset> weaponDefinitions = null,
            IdleAutoDefenseGameRulesAsset gameRules = null,
            float difficultyMultiplier = 1f)
        {
            WeaponDefinitionAsset[] weapons = weaponDefinitions == null || weaponDefinitions.Count == 0
                ? IdleAutoDefenseWeaponContent.CreateWeaponDefinitionAssets(IdleAutoDefenseAttackContent.CreateAttackRecipes())
                : IdleAutoDefenseWeaponContent.CopyWeaponDefinitions(weaponDefinitions);
            AutoDefenseEnemyDefinition[] enemies = enemyDefinitions == null
                ? IdleAutoDefenseEnemyContent.CreateDefaultAutoDefenseEnemyDefinitions()
                : IdleAutoDefenseEnemyContent.CreateAutoDefenseEnemyDefinitions(enemyDefinitions, difficultyMultiplier);
            AutoDefenseMountDefinition[] mounts = IdleAutoDefenseWeaponContent.CreateAutoDefenseMountDefinitions(weapons);
            return new AutoDefenseDefinition(
                gameRules == null
                    ? new AutoDefenseObjectiveDefinition(new DefenseObjectiveId("objective.idle-auto-defense.core"), Vector3.zero, 240, DamageType, 0.45f, 60, 2)
                    : gameRules.CreateObjectiveDefinition(),
                gameRules == null ? IdleAutoDefenseEncounterContent.CreateSampleSpawnRing() : gameRules.CreateSpawnRingDefinition(),
                enemies,
                mounts,
                IdleAutoDefenseWeaponContent.CreateAutoDefenseWeaponModuleDefinitions(weapons, mounts));
        }
    }
}
