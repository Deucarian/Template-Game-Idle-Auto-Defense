using System;
using System.Collections.Generic;
using System.Globalization;
using Deucarian.RunUpgrades;
using Deucarian.RunUpgrades.Authoring;
using Deucarian.WeaponSystems.Authoring;
using static Deucarian.TemplateGameIdleAutoDefense.BasicIdleAutoDefenseGame;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    // Owns starter upgrade choices and the distinct fallback and strict conversion policies.
    internal static class IdleAutoDefenseUpgradeContent
    {
        internal static RunUpgradeCatalog CreateRunUpgradeCatalog(IReadOnlyList<RunUpgradeDefinitionAsset> upgradeDefinitions = null)
        {
            if (upgradeDefinitions == null || upgradeDefinitions.Count == 0)
                return CreateDefaultRunUpgradeCatalog();
            return new RunUpgradeCatalog(CreateRunUpgradeDefinitions(upgradeDefinitions));
        }

        internal static RunUpgradeCatalog CreateRunUpgradeCatalogOrEmpty(IReadOnlyList<RunUpgradeDefinitionAsset> upgradeDefinitions)
        {
            if (upgradeDefinitions == null || upgradeDefinitions.Count == 0)
                return new RunUpgradeCatalog(Array.Empty<RunUpgradeDefinition>());
            return new RunUpgradeCatalog(CreateRunUpgradeDefinitions(upgradeDefinitions));
        }

        internal static RunUpgradeDefinitionAsset[] CreateRunUpgradeDefinitionAssets(IReadOnlyList<WeaponDefinitionAsset> weaponDefinitions = null, IdleAutoDefenseGeneratedContent generated = null)
        {
            weaponDefinitions ??= IdleAutoDefenseWeaponContent.CreateWeaponDefinitionAssets(null, generated);
            WeaponDefinitionAsset shard = IdleAutoDefenseWeaponContent.FindWeaponDefinition(weaponDefinitions, ShardLauncherWeaponId.Value);
            var definitions = new[]
            {
                UpgradeAsset("upgrade.idle-auto-defense.damage-up", "Damage Boost", RunUpgradeAuthoringTargetKind.AttackDamage, RunUpgradeModifierType.Additive, 1.5, shard, "10,20,35", RunUpgradeRarity.Common, 6, 3),
                UpgradeAsset("upgrade.idle-auto-defense.fire-rate-up", "Fire Rate Boost", RunUpgradeAuthoringTargetKind.AttackRate, RunUpgradeModifierType.Additive, 1, shard, "8,16,28", RunUpgradeRarity.Common, 5, 3),
                UpgradeAsset("upgrade.idle-auto-defense.range-up", "Range Boost", RunUpgradeAuthoringTargetKind.Range, RunUpgradeModifierType.Additive, 1.25, shard, "12,24,36", RunUpgradeRarity.Common, 4, 3),
                UpgradeAsset("upgrade.idle-auto-defense.projectile-speed-up", "Projectile Speed", RunUpgradeAuthoringTargetKind.ProjectileSpeed, RunUpgradeModifierType.Multiplicative, 0.35, shard, "10,20,40", RunUpgradeRarity.Common, 5, 3),
                UpgradeAsset("upgrade.idle-auto-defense.objective-max-health-up", "Core Reinforcement", RunUpgradeAuthoringTargetKind.WeaponStat, RunUpgradeModifierType.Additive, 8, null, "14,28,42", RunUpgradeRarity.Uncommon, 3, 3, "objective.idle-auto-defense.core", "idle-auto-defense.objective.max_health"),
                UpgradeAsset("upgrade.idle-auto-defense.enemy-reward-up", "Credit Reward", RunUpgradeAuthoringTargetKind.EnemyReward, RunUpgradeModifierType.Multiplicative, 0.15, null, "16,32,48", RunUpgradeRarity.Uncommon, 3, 3, "reward.idle-auto-defense.run")
            };
            return generated == null ? definitions : generated.OwnGenerated<RunUpgradeDefinitionAsset>(null, definitions);
        }

        internal static RunUpgradeDefinitionAsset[] ResolveUpgradeDefinitionsForTemplate(IReadOnlyList<RunUpgradeDefinitionAsset> assignedDefinitions, out int rejectedDefinitionCount, IdleAutoDefenseGeneratedContent generated = null)
        {
            rejectedDefinitionCount = 0;
            if (assignedDefinitions == null || assignedDefinitions.Count == 0)
                return CreateRunUpgradeDefinitionAssets(null, generated);

            var definitions = new List<RunUpgradeDefinitionAsset>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < assignedDefinitions.Count; i++)
            {
                RunUpgradeDefinitionAsset definition = assignedDefinitions[i];
                if (definition == null)
                {
                    rejectedDefinitionCount++;
                    continue;
                }

                RunUpgradeDefinitionValidationReport report = RunUpgradeDefinitionValidator.Validate(definition);
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
                return CreateRunUpgradeDefinitionAssets(null, generated);
            if (rejectedDefinitionCount > 0)
                return CreateRunUpgradeDefinitionAssets(null, generated);

            return definitions.ToArray();
        }

        internal static RunUpgradeDefinition[] CreateRunUpgradeDefinitions(IReadOnlyList<RunUpgradeDefinitionAsset> upgradeDefinitions)
        {
            if (upgradeDefinitions == null || upgradeDefinitions.Count == 0) throw new ArgumentException("At least one upgrade definition is required.", nameof(upgradeDefinitions));
            var definitions = new RunUpgradeDefinition[upgradeDefinitions.Count];
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < upgradeDefinitions.Count; i++)
            {
                if (upgradeDefinitions[i] == null) throw new ArgumentException("Upgrade definition cannot be null.", nameof(upgradeDefinitions));
                RunUpgradeDefinitionValidationReport report = RunUpgradeDefinitionValidator.Validate(upgradeDefinitions[i]);
                if (!report.IsValid) throw new ArgumentException("Upgrade definition is invalid.", nameof(upgradeDefinitions));
                if (!seen.Add(upgradeDefinitions[i].Id.Trim())) throw new ArgumentException("Duplicate upgrade definition ID: " + upgradeDefinitions[i].Id, nameof(upgradeDefinitions));
                definitions[i] = upgradeDefinitions[i].ToRuntimeDefinition();
            }

            return definitions;
        }

        private static RunUpgradeCatalog CreateDefaultRunUpgradeCatalog()
        {
            return new RunUpgradeCatalog(new[]
            {
                Upgrade("upgrade.idle-auto-defense.damage-up", "idle-auto-defense.direct.damage_bonus", PulseCannonWeaponId.Value, 1.5, RunUpgradeRarity.Common, 6, 5),
                Upgrade("upgrade.idle-auto-defense.fire-rate-up", "idle-auto-defense.weapon.fire_rate_intent", PulseCannonWeaponId.Value, 1, RunUpgradeRarity.Common, 5, 3),
                Upgrade("upgrade.idle-auto-defense.projectile-count-up", "idle-auto-defense.projectile.volley_intent", ShardLauncherWeaponId.Value, 1, RunUpgradeRarity.Uncommon, 3, 2),
                Upgrade("upgrade.idle-auto-defense.projectile-speed-up", "idle-auto-defense.projectile.speed_multiplier", ShardProjectileId.Value, 0.35, RunUpgradeRarity.Common, 5, 4),
                Upgrade("upgrade.idle-auto-defense.objective-max-health-up", "idle-auto-defense.objective.max_health", "objective.idle-auto-defense.core", 6, RunUpgradeRarity.Uncommon, 3, 3),
                Upgrade("upgrade.idle-auto-defense.objective-repair", "idle-auto-defense.objective.heal", "objective.idle-auto-defense.core", 5, RunUpgradeRarity.Common, 5, 4),
                Upgrade("upgrade.idle-auto-defense.shield-restore-intent", "idle-auto-defense.objective.shield_restore_intent", "objective.idle-auto-defense.core", 4, RunUpgradeRarity.Uncommon, 2, 2),
                Upgrade("upgrade.idle-auto-defense.enemy-reward-up", "idle-auto-defense.reward.credits_multiplier", "reward.idle-auto-defense.run", 0.15, RunUpgradeRarity.Uncommon, 3, 3),
                Upgrade("upgrade.idle-auto-defense.offline-gain-up", "idle-auto-defense.offline.credits_multiplier", "offline.idle-auto-defense.credits", 0.10, RunUpgradeRarity.Common, 4, 3),
                Upgrade("upgrade.idle-auto-defense.reroll-bonus", "idle-auto-defense.reroll.bonus_intent", "monetization.idle-auto-defense.reroll", 1, RunUpgradeRarity.Rare, 2, 1),
                Upgrade("upgrade.idle-auto-defense.crit-chance-intent", "idle-auto-defense.attack.crit_chance_intent", PulseCannonWeaponId.Value, 0.05, RunUpgradeRarity.Rare, 2, 2),
                Upgrade("upgrade.idle-auto-defense.crit-damage-intent", "idle-auto-defense.attack.crit_damage_intent", PulseCannonWeaponId.Value, 0.20, RunUpgradeRarity.Rare, 2, 2),
                Upgrade("upgrade.idle-auto-defense.direct-specialization", "idle-auto-defense.direct.damage_bonus", PulseCannonWeaponId.Value, 3, RunUpgradeRarity.Epic, 1, 1, new[] { new RunUpgradeId("upgrade.idle-auto-defense.damage-up") }),
                Upgrade("upgrade.idle-auto-defense.projectile-specialization", "idle-auto-defense.projectile.speed_multiplier", ShardProjectileId.Value, 0.75, RunUpgradeRarity.Epic, 1, 1, new[] { new RunUpgradeId("upgrade.idle-auto-defense.projectile-speed-up") })
            });
        }

        private static RunUpgradeDefinition Upgrade(
            string id,
            string effect,
            string target,
            double amount,
            RunUpgradeRarity rarity,
            int weight,
            int maxRank,
            IReadOnlyList<RunUpgradeId> prerequisites = null)
        {
            return new RunUpgradeDefinition(
                new RunUpgradeId(id),
                rarity,
                weight,
                maxRank,
                new[] { new RunUpgradeEffectDescriptor(new RunUpgradeEffectId(effect), new RunUpgradeTargetId(target), amount) },
                prerequisites);
        }

        private static RunUpgradeDefinitionAsset UpgradeAsset(
            string id,
            string displayName,
            RunUpgradeAuthoringTargetKind targetKind,
            RunUpgradeModifierType modifierType,
            double amount,
            WeaponDefinitionAsset weapon,
            string costsCsv,
            RunUpgradeRarity rarity,
            int weight,
            int maxRank,
            string targetIdOverride = "",
            string effectIdOverride = "")
        {
            string targetId = string.IsNullOrWhiteSpace(targetIdOverride)
                ? weapon == null ? string.Empty : weapon.Id
                : targetIdOverride;
            return RunUpgradeDefinitionAsset.CreateTransient(
                id,
                displayName,
                rarity,
                weight,
                maxRank,
                new[]
                {
                    new RunUpgradeEffectRecipe(targetKind, modifierType, amount, weapon: weapon, targetIdOverride: targetId, effectIdOverride: effectIdOverride)
                },
                ParseIntCsv(costsCsv),
                displayName + " authored upgrade.",
                new[] { "idle-auto-defense", "upgrade" });
        }

        private static int[] ParseIntCsv(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) return Array.Empty<int>();
            string[] parts = csv.Split(',');
            var values = new List<int>();
            for (int i = 0; i < parts.Length; i++)
                if (int.TryParse(parts[i].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                    values.Add(value);
            return values.ToArray();
        }
    }
}
