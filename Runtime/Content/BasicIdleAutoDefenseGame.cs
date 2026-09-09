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
    // Stable public entry points and IDs for the template-local content owners.
    public static class BasicIdleAutoDefenseGame
    {
        public static readonly DamageTypeId DamageType = new DamageTypeId("damage.idle-auto-defense.basic");
        public static readonly AttackDefinitionId PulseAttackId = new AttackDefinitionId("attack.idle-auto-defense.pulse-beam");
        public static readonly AttackDefinitionId ShardAttackId = new AttackDefinitionId("attack.idle-auto-defense.shard-projectile");
        public static readonly AttackDefinitionId ArcBurstAttackId = new AttackDefinitionId("attack.idle-auto-defense.arc-burst");
        public static readonly AttackDefinitionId HomingPulseAttackId = new AttackDefinitionId("attack.idle-auto-defense.homing-pulse");
        public static readonly AttackDefinitionId AttackId = PulseAttackId;
        public static readonly ProjectileDefinitionId ShardProjectileId = new ProjectileDefinitionId("projectile.idle-auto-defense.shard");
        public static readonly ProjectileDefinitionId HomingPulseProjectileId = new ProjectileDefinitionId("projectile.idle-auto-defense.homing-pulse");
        public static readonly ProjectileDefinitionId ProjectileId = ShardProjectileId;
        public static readonly WorldSpawnableId SwarmEnemySpawnableId = new WorldSpawnableId("enemy.idle-auto-defense.swarm");
        public static readonly WorldSpawnableId RunnerEnemySpawnableId = new WorldSpawnableId("enemy.idle-auto-defense.runner");
        public static readonly WorldSpawnableId TankEnemySpawnableId = new WorldSpawnableId("enemy.idle-auto-defense.tank");
        public static readonly WorldSpawnableId ShieldedEnemySpawnableId = new WorldSpawnableId("enemy.idle-auto-defense.shielded");
        public static readonly WorldSpawnableId EliteEnemySpawnableId = new WorldSpawnableId("enemy.idle-auto-defense.elite");
        public static readonly WorldSpawnableId BossEnemySpawnableId = new WorldSpawnableId("enemy.idle-auto-defense.boss");
        public static readonly WorldSpawnableId EnemySpawnableId = SwarmEnemySpawnableId;
        public static readonly WorldSpawnableId ProjectileSpawnableId = new WorldSpawnableId("projectile.idle-auto-defense.shard");
        public static readonly WeaponDefinitionId PulseCannonWeaponId = new WeaponDefinitionId("weapon.idle-auto-defense.pulse-beam");
        public static readonly WeaponDefinitionId ShardLauncherWeaponId = new WeaponDefinitionId("weapon.idle-auto-defense.shard-launcher");
        public static readonly WeaponDefinitionId ArcBurstTowerWeaponId = new WeaponDefinitionId("weapon.idle-auto-defense.arc-burst");
        public static readonly WeaponDefinitionId HomingSpireWeaponId = new WeaponDefinitionId("weapon.idle-auto-defense.homing-pulse");
        public static readonly WeaponDefinitionId ArcEmitterWeaponId = ArcBurstTowerWeaponId;
        public static readonly WeaponDefinitionId OrbitalShotWeaponId = HomingSpireWeaponId;
        public static readonly CurrencyId Credits = new CurrencyId("currency.idle-auto-defense.credits");
        public static readonly CurrencyId Parts = new CurrencyId("currency.idle-auto-defense.parts");
        public static readonly TrackId AccountXp = new TrackId("track.idle-auto-defense.account");
        public static readonly UnlockId StarterUnlock = new UnlockId("unlock.idle-auto-defense.starter");
        public static readonly UnlockId Stage2Unlock = new UnlockId("unlock.idle-auto-defense.stage.pressure-ring");
        public static readonly UnlockId Stage3Unlock = new UnlockId("unlock.idle-auto-defense.stage.boss-pulse");
        public static readonly UnlockId PulseCannonUnlock = new UnlockId("unlock.idle-auto-defense.module.pulse-cannon");
        public static readonly UnlockId ShardLauncherUnlock = new UnlockId("unlock.idle-auto-defense.module.shard-launcher");
        public static readonly ResearchNodeId CorePlatingResearch = new ResearchNodeId("research.idle-auto-defense.core-plating");
        public static readonly ResearchNodeId PulseCapacitorResearch = new ResearchNodeId("research.idle-auto-defense.pulse-capacitor");
        public static readonly ResearchNodeId ShardLoaderResearch = new ResearchNodeId("research.idle-auto-defense.shard-loader");
        public static readonly ResearchNodeId OfflineRoutingResearch = new ResearchNodeId("research.idle-auto-defense.offline-routing");

        public static AutoDefenseDefinition CreateDefinition(
            IReadOnlyList<EnemyDefinitionAsset> enemyDefinitions = null,
            IReadOnlyList<WeaponDefinitionAsset> weaponDefinitions = null,
            IdleAutoDefenseGameRulesAsset gameRules = null,
            float difficultyMultiplier = 1f)
        {
            return IdleAutoDefenseRuntimeFactory.CreateDefinition(enemyDefinitions, weaponDefinitions, gameRules, difficultyMultiplier);
        }

        public static AutoDefenseSpawnRingDefinition CreateSampleSpawnRing()
        {
            return IdleAutoDefenseEncounterContent.CreateSampleSpawnRing();
        }

        public static EncounterDefinition CreateEncounterDefinition(IReadOnlyList<WaveDefinitionAsset> waveDefinitions = null, int seed = 20260623)
        {
            return IdleAutoDefenseEncounterContent.CreateEncounterDefinition(waveDefinitions, seed);
        }

        public static GameContentSetResolution ResolveGameContentSetForTemplate(GameContentSetAsset contentSet)
        {
            return GameContentSetValidator.Resolve(contentSet);
        }

        public static StageDefinition[] CreateStageDefinitions()
        {
            return IdleAutoDefenseEncounterContent.CreateStageDefinitions();
        }

        public static EncounterDefinition[] CreateEncounterDefinitions()
        {
            return IdleAutoDefenseEncounterContent.CreateEncounterDefinitions();
        }

        public static EncounterDefinition CreateFirstOrbitEncounterDefinition()
        {
            return IdleAutoDefenseEncounterContent.CreateFirstOrbitEncounterDefinition();
        }

        public static EncounterDefinition CreatePressureRingEncounterDefinition()
        {
            return IdleAutoDefenseEncounterContent.CreatePressureRingEncounterDefinition();
        }

        public static EncounterDefinition CreateBossPulseEncounterDefinition()
        {
            return IdleAutoDefenseEncounterContent.CreateBossPulseEncounterDefinition();
        }

        public static EncounterDefinition CreateEndlessPlaceholderEncounterDefinition()
        {
            return IdleAutoDefenseEncounterContent.CreateEndlessPlaceholderEncounterDefinition();
        }

        public static CombatCatalog CreateCombatCatalog(
            IReadOnlyList<AttackDefinitionAsset> attackRecipes = null,
            IReadOnlyList<EnemyDefinitionAsset> enemyDefinitions = null,
            IdleAutoDefenseGameRulesAsset gameRules = null)
        {
            return IdleAutoDefenseAttackContent.CreateCombatCatalog(attackRecipes, enemyDefinitions, gameRules);
        }

        public static AttackRuntime CreateAttackRuntime(CombatCatalog catalog, AutoDefenseDefinition definition, IReadOnlyList<AttackDefinitionAsset> attackRecipes = null)
        {
            return IdleAutoDefenseAttackContent.CreateAttackRuntime(catalog, definition, attackRecipes);
        }

        public static WeaponRuntime CreateWeaponRuntime(AutoDefenseDefinition definition, AttackRuntime attacks)
        {
            return IdleAutoDefenseWeaponContent.CreateWeaponRuntime(definition, attacks);
        }

        public static WeaponDefinitionAsset[] CreateWeaponDefinitionAssets(IReadOnlyList<AttackDefinitionAsset> attackRecipes = null)
        {
            return IdleAutoDefenseWeaponContent.CreateWeaponDefinitionAssets(attackRecipes);
        }

        public static WeaponDefinitionAsset[] ResolveWeaponDefinitionsForTemplate(IReadOnlyList<WeaponDefinitionAsset> assignedDefinitions, IReadOnlyList<AttackDefinitionAsset> attackRecipes, out int rejectedDefinitionCount)
        {
            return IdleAutoDefenseWeaponContent.ResolveWeaponDefinitionsForTemplate(assignedDefinitions, attackRecipes, out rejectedDefinitionCount);
        }

        public static WeaponDefinition[] CreateWeaponDefinitions(IReadOnlyList<WeaponDefinitionAsset> weaponDefinitions)
        {
            return IdleAutoDefenseWeaponContent.CreateWeaponDefinitions(weaponDefinitions);
        }

        public static ProjectileDefinition CreateProjectileDefinition()
        {
            return IdleAutoDefenseAttackContent.CreateProjectileDefinition();
        }

        public static ProjectileDefinition[] CreateProjectileDefinitions(IReadOnlyList<AttackDefinitionAsset> attackRecipes = null)
        {
            return IdleAutoDefenseAttackContent.CreateProjectileDefinitions(attackRecipes);
        }

        public static EnemyDefinitionAsset[] CreateEnemyDefinitions()
        {
            return IdleAutoDefenseEnemyContent.CreateEnemyDefinitions();
        }

        public static EnemyDefinitionAsset[] ResolveEnemyDefinitionsForTemplate(IReadOnlyList<EnemyDefinitionAsset> assignedDefinitions, out int rejectedDefinitionCount)
        {
            return IdleAutoDefenseEnemyContent.ResolveEnemyDefinitionsForTemplate(assignedDefinitions, out rejectedDefinitionCount);
        }

        public static AutoDefenseEnemyDefinition[] CreateAutoDefenseEnemyDefinitions(IReadOnlyList<EnemyDefinitionAsset> enemyDefinitions, float difficultyMultiplier = 1f)
        {
            return IdleAutoDefenseEnemyContent.CreateAutoDefenseEnemyDefinitions(enemyDefinitions, difficultyMultiplier);
        }

        public static WaveDefinitionAsset[] CreateWaveDefinitions()
        {
            return IdleAutoDefenseEncounterContent.CreateWaveDefinitions();
        }

        public static WaveDefinitionAsset[] ResolveWaveDefinitionsForTemplate(IReadOnlyList<WaveDefinitionAsset> assignedDefinitions, IReadOnlyList<EnemyDefinitionAsset> enemyDefinitions, out int rejectedDefinitionCount)
        {
            return IdleAutoDefenseEncounterContent.ResolveWaveDefinitionsForTemplate(assignedDefinitions, enemyDefinitions, out rejectedDefinitionCount);
        }

        public static WaveDefinition[] CreateEncounterWaves(IReadOnlyList<WaveDefinitionAsset> waveDefinitions)
        {
            return IdleAutoDefenseEncounterContent.CreateEncounterWaves(waveDefinitions);
        }

        public static AttackDefinitionAsset[] CreateAttackRecipes()
        {
            return IdleAutoDefenseAttackContent.CreateAttackRecipes();
        }

        public static AttackDefinitionAsset[] ResolveAttackRecipesForTemplate(IReadOnlyList<AttackDefinitionAsset> assignedRecipes, out int rejectedRecipeCount)
        {
            return IdleAutoDefenseAttackContent.ResolveAttackRecipesForTemplate(assignedRecipes, out rejectedRecipeCount);
        }

        public static AttackDefinition[] CreateAttackDefinitions(IReadOnlyList<AttackDefinitionAsset> attackRecipes)
        {
            return IdleAutoDefenseAttackContent.CreateAttackDefinitions(attackRecipes);
        }

        public static RunUpgradeCatalog CreateRunUpgradeCatalog(IReadOnlyList<RunUpgradeDefinitionAsset> upgradeDefinitions = null)
        {
            return IdleAutoDefenseUpgradeContent.CreateRunUpgradeCatalog(upgradeDefinitions);
        }

        public static RunUpgradeCatalog CreateRunUpgradeCatalogOrEmpty(IReadOnlyList<RunUpgradeDefinitionAsset> upgradeDefinitions)
        {
            return IdleAutoDefenseUpgradeContent.CreateRunUpgradeCatalogOrEmpty(upgradeDefinitions);
        }

        public static RunUpgradeDefinitionAsset[] CreateRunUpgradeDefinitionAssets(IReadOnlyList<WeaponDefinitionAsset> weaponDefinitions = null)
        {
            return IdleAutoDefenseUpgradeContent.CreateRunUpgradeDefinitionAssets(weaponDefinitions);
        }

        public static RunUpgradeDefinitionAsset[] ResolveUpgradeDefinitionsForTemplate(IReadOnlyList<RunUpgradeDefinitionAsset> assignedDefinitions, out int rejectedDefinitionCount)
        {
            return IdleAutoDefenseUpgradeContent.ResolveUpgradeDefinitionsForTemplate(assignedDefinitions, out rejectedDefinitionCount);
        }

        public static RunUpgradeDefinition[] CreateRunUpgradeDefinitions(IReadOnlyList<RunUpgradeDefinitionAsset> upgradeDefinitions)
        {
            return IdleAutoDefenseUpgradeContent.CreateRunUpgradeDefinitions(upgradeDefinitions);
        }

        public static IdleProgressionDefinition CreateOfflineProgressionDefinition()
        {
            return IdleAutoDefenseProgressionContent.CreateOfflineProgressionDefinition();
        }

        public static ProgressionCatalog CreateProgressionCatalog()
        {
            return IdleAutoDefenseProgressionContent.CreateProgressionCatalog();
        }

        public static RewardBundle CreateEncounterCompletionReward()
        {
            return IdleAutoDefenseProgressionContent.CreateEncounterCompletionReward();
        }

        public static string SanitizeContentSetOperationSegment(string value)
        {
            return IdleAutoDefenseContentIdentity.SanitizeContentSetOperationSegment(value);
        }
    }
}
