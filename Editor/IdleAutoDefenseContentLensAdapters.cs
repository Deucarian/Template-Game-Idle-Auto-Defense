using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Deucarian.Attacks.Authoring;
using Deucarian.Attacks.Editor;
using Deucarian.GameContentAuthoring.Editor;
using Deucarian.RunUpgrades.Authoring;
using Deucarian.RunUpgrades.Editor;
using Deucarian.WeaponSystems.Authoring;
using Deucarian.WeaponSystems.Editor;
using UnityEditor;

namespace Deucarian.TemplateGameIdleAutoDefense.Editor
{
    [InitializeOnLoad]
    public static class IdleAutoDefenseContentLensAdapters
    {
        public const string AttackAdapterId = "com.deucarian.template.game.idle-auto-defense.attack-projection";
        public const string EnemyAdapterId = "com.deucarian.template.game.idle-auto-defense.enemy-projection";
        public const string EncounterAdapterId = "com.deucarian.template.game.idle-auto-defense.encounter-projection";
        public const string WeaponAdapterId = "com.deucarian.template.game.idle-auto-defense.weapon-projection";
        public const string UpgradeAdapterId = "com.deucarian.template.game.idle-auto-defense.upgrade-projection";

        // Existing template tests and projectile travel conversion use a nominal 0.05-second simulation step.
        private const float NominalSecondsPerSimulationTick = 0.05f;

        static IdleAutoDefenseContentLensAdapters()
        {
            EnsureRegistered();
        }

        public static void EnsureRegistered()
        {
            GameContentRecordProjectionRegistry<AttackContentRecordProjection>.Register(new IdleAttackAdapter());
            GameContentRecordProjectionRegistry<EnemyContentRecordProjection>.Register(new IdleEnemyAdapter());
            GameContentRecordProjectionRegistry<EncounterContentRecordProjection>.Register(new IdleEncounterAdapter());
            GameContentRecordProjectionRegistry<WeaponContentRecordProjection>.Register(new IdleWeaponAdapter());
            GameContentRecordProjectionRegistry<UpgradeContentRecordProjection>.Register(new IdleUpgradeAdapter());
        }

        private static bool IsIdle(GameContentRecordDescriptor record, GameContentRecordCapability capability)
        {
            return record != null &&
                   string.Equals(record.CanonicalKey.OwningPackageId, IdleAutoDefenseContentPackIndex.OwningPackageId, StringComparison.OrdinalIgnoreCase) &&
                   IdleAutoDefenseNamedPackDefinition.All.Any(definition =>
                       string.Equals(record.CanonicalKey.PackId, definition.PackId, StringComparison.OrdinalIgnoreCase)) &&
                   record.HasCapability(capability);
        }

        private sealed class IdleAttackAdapter : IGameContentRecordProjectionAdapter<AttackContentRecordProjection>
        {
            public string AdapterId => AttackAdapterId;
            public int SortOrder => 110;

            public bool TryProject(GameContentRecordDescriptor record, out AttackContentRecordProjection projection)
            {
                if (!IsIdle(record, GameContentRecordCapabilities.Attack) || !(record.SourceAsset is AttackDefinitionAsset attack))
                {
                    projection = null;
                    return false;
                }

                AttackMechanicsDefinitionAsset mechanics = attack.Mechanics;
                AttackDeliveryDefinitionAsset delivery = attack.Delivery;
                string statuses = attack.StatusEffects == null
                    ? "None"
                    : string.Join(", ", attack.StatusEffects.StatusEffects
                        .Where(value => value != null && !string.IsNullOrWhiteSpace(value.StatusId))
                        .Select(value => value.StatusId));
                int cooldownTicks = mechanics == null ? 0 : mechanics.CooldownTicks;
                string cadence = "Authored cadence " + cooldownTicks.ToString(CultureInfo.InvariantCulture) +
                                 " simulation ticks (nominal " + ToSeconds(cooldownTicks).ToString("0.###", CultureInfo.InvariantCulture) + "s at 20 Hz).";
                projection = new AttackContentRecordProjection(
                    record,
                    mechanics == null ? 0f : mechanics.DamageAmount,
                    ToSeconds(cooldownTicks),
                    mechanics == null ? 0f : mechanics.Range,
                    attack.Targeting == null ? "Not authored" : attack.Targeting.Mode.ToString(),
                    delivery == null ? "Not authored" : delivery.Mode.ToString(),
                    delivery == null ? string.Empty : delivery.ProjectileDefinitionId,
                    delivery == null || delivery.Mode != AttackRecipeDeliveryMode.Projectile ? 0 : 1,
                    delivery == null ? 0f : delivery.Radius,
                    delivery == null ? 0f : ToSeconds(delivery.ProjectileLifetimeTicks),
                    cadence + " Status: " + (string.IsNullOrWhiteSpace(statuses) ? "None" : statuses),
                    AttackPresentation(attack),
                    RelatedUpgradeKeys(record));
                return true;
            }
        }

        private sealed class IdleEnemyAdapter : IGameContentRecordProjectionAdapter<EnemyContentRecordProjection>
        {
            public string AdapterId => EnemyAdapterId;
            public int SortOrder => 110;

            public bool TryProject(GameContentRecordDescriptor record, out EnemyContentRecordProjection projection)
            {
                if (!IsIdle(record, GameContentRecordCapabilities.Enemy) || !(record.SourceAsset is EnemyDefinitionAsset enemy))
                {
                    projection = null;
                    return false;
                }

                EnemyStatsDefinitionAsset stats = enemy.Stats;
                bool major = record.HasCapability(GameContentRecordCapabilities.MajorThreat);
                projection = new EnemyContentRecordProjection(
                    record,
                    enemy.Role.ToString(),
                    stats == null ? 0f : stats.MaximumHealth,
                    stats == null ? 0f : stats.MoveSpeed,
                    stats == null ? 0f : stats.CollisionRadius,
                    stats == null ? 0f : stats.ContactDamage,
                    0f,
                    stats == null ? 0 : stats.RewardValue,
                    "Perimeter spawn and objective approach are runtime-owned",
                    major,
                    "Not authored by the current enemy graph",
                    "Not authored by the current enemy graph",
                    enemy.Presentation == null || enemy.Presentation.Prefab == null
                        ? "No prefab assigned"
                        : enemy.Presentation.Prefab.name,
                    "Contact cadence is runtime-owned, not authored. Tags: " + string.Join(", ", enemy.Tags));
                return true;
            }
        }

        private sealed class IdleEncounterAdapter : IGameContentRecordProjectionAdapter<EncounterContentRecordProjection>
        {
            public string AdapterId => EncounterAdapterId;
            public int SortOrder => 110;

            public bool TryProject(GameContentRecordDescriptor record, out EncounterContentRecordProjection projection)
            {
                if (!IsIdle(record, GameContentRecordCapabilities.Encounter) || !(record.SourceAsset is WaveDefinitionAsset wave))
                {
                    projection = null;
                    return false;
                }

                int startTick = wave.Schedule == null ? 0 : wave.Schedule.StartTick;
                IReadOnlyList<WaveEntryRecipe> entries = wave.Entries == null
                    ? Array.Empty<WaveEntryRecipe>()
                    : wave.Entries.Entries;
                GameContentRecordKey[] enemies = record.OutboundReferences
                    .Where(reference => string.Equals(reference.TargetCategoryId, "enemies", StringComparison.OrdinalIgnoreCase))
                    .Select(reference => reference.TargetRecordKey)
                    .Where(value => value != null)
                    .Distinct()
                    .ToArray();
                float eventSeconds = ToSeconds(startTick);
                projection = new EncounterContentRecordProjection(
                    record,
                    "Defense Wave (simulation-tick schedule)",
                    0f,
                    0f,
                    record.HasCapability(GameContentRecordCapabilities.EliteEvent) ? eventSeconds : -1f,
                    -1f,
                    -1f,
                    record.HasCapability(GameContentRecordCapabilities.BossEvent) ? eventSeconds : -1f,
                    false,
                    "Starts at authored tick " + startTick.ToString(CultureInfo.InvariantCulture) +
                    "; " + entries.Count.ToString(CultureInfo.InvariantCulture) + " spawn group(s); channels " +
                    string.Join(", ", entries.Where(value => value != null).Select(value => value.SpawnChannelId).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct()) + ".",
                    enemies,
                    Array.Empty<GameContentRecordKey>());
                return true;
            }
        }

        private sealed class IdleWeaponAdapter : IGameContentRecordProjectionAdapter<WeaponContentRecordProjection>
        {
            public string AdapterId => WeaponAdapterId;
            public int SortOrder => 110;

            public bool TryProject(GameContentRecordDescriptor record, out WeaponContentRecordProjection projection)
            {
                if (!IsIdle(record, GameContentRecordCapabilities.Weapon) || !(record.SourceAsset is WeaponDefinitionAsset weapon))
                {
                    projection = null;
                    return false;
                }

                WeaponStatsDefinitionAsset stats = weapon.Stats;
                AttackDefinitionAsset attack = stats == null ? null : stats.Attack;
                AttackMechanicsDefinitionAsset mechanics = attack == null ? null : attack.Mechanics;
                AttackDeliveryDefinitionAsset delivery = attack == null ? null : attack.Delivery;
                int cooldownTicks = stats == null ? 0 : stats.CooldownTicks;
                GameContentRecordKey[] upgrades = RelatedUpgradeKeys(record).ToArray();
                projection = new WeaponContentRecordProjection(
                    record,
                    true,
                    stats == null ? "Not authored" : stats.FireMode.ToString(),
                    mechanics == null ? 0f : mechanics.DamageAmount,
                    ToSeconds(cooldownTicks),
                    stats == null ? 0f : stats.Range,
                    stats == null ? "Not authored" : stats.TargetingRoleId,
                    delivery == null ? string.Empty : delivery.ProjectileDefinitionId,
                    delivery == null ? 0f : delivery.Radius,
                    "Authored cadence " + cooldownTicks.ToString(CultureInfo.InvariantCulture) + " simulation ticks; " + upgrades.Length.ToString(CultureInfo.InvariantCulture) + " linked upgrade(s)",
                    "None authored",
                    "None authored",
                    weapon.Presentation == null || weapon.Presentation.Prefab == null
                        ? "No mounted-module prefab assigned"
                        : weapon.Presentation.Prefab.name);
                return true;
            }
        }

        private sealed class IdleUpgradeAdapter : IGameContentRecordProjectionAdapter<UpgradeContentRecordProjection>
        {
            public string AdapterId => UpgradeAdapterId;
            public int SortOrder => 110;

            public bool TryProject(GameContentRecordDescriptor record, out UpgradeContentRecordProjection projection)
            {
                if (!IsIdle(record, GameContentRecordCapabilities.Upgrade) || !(record.SourceAsset is RunUpgradeDefinitionAsset upgrade))
                {
                    projection = null;
                    return false;
                }

                RunUpgradeEffectRecipe[] effects = upgrade.Effects == null
                    ? Array.Empty<RunUpgradeEffectRecipe>()
                    : upgrade.Effects.Effects.Where(value => value != null).ToArray();
                RunUpgradeEffectRecipe primary = effects.FirstOrDefault();
                string references = string.Join(", ", record.OutboundReferences.Select(reference =>
                    reference.RelationshipLabel + " " + (reference.TargetRecordKey?.SourceRecordId ?? reference.TargetRecordId)));
                projection = new UpgradeContentRecordProjection(
                    record,
                    upgrade.Description,
                    record.HasCapability(GameContentRecordCapabilities.WeaponUpgrade) ? "Weapon Upgrade" : "Run Upgrade",
                    upgrade.Economy == null ? string.Empty : upgrade.Economy.Rarity.ToString(),
                    upgrade.Economy == null ? 0d : upgrade.Economy.Weight,
                    upgrade.Economy == null ? 0 : upgrade.Economy.MaxRank,
                    primary == null ? string.Empty : primary.TargetKind + " / " + primary.ModifierType,
                    primary == null ? 0d : primary.Amount,
                    primary == null ? string.Empty : primary.GetTargetId(),
                    upgrade.Effects == null ? string.Empty : string.Join(", ", upgrade.Effects.Prerequisites),
                    string.Empty,
                    references,
                    primary == null
                        ? "No authored comparison amount"
                        : primary.Amount.ToString("0.###", CultureInfo.InvariantCulture) + " per authored rank effect");
                return true;
            }
        }

        private static float ToSeconds(int ticks)
        {
            return Math.Max(0, ticks) * NominalSecondsPerSimulationTick;
        }

        private static IReadOnlyList<GameContentRecordKey> RelatedUpgradeKeys(GameContentRecordDescriptor record)
        {
            return record.InboundReferences
                .Where(reference => reference.RelationshipLabel.StartsWith("targets ", StringComparison.OrdinalIgnoreCase))
                .Select(reference => reference.TargetRecordKey)
                .Where(value => value != null)
                .Distinct()
                .ToArray();
        }

        private static string AttackPresentation(AttackDefinitionAsset attack)
        {
            int eventCount = attack.Presentation == null ? 0 : attack.Presentation.Events.Count;
            string projectile = attack.Delivery == null || attack.Delivery.ProjectilePrefab == null
                ? string.Empty
                : attack.Delivery.ProjectilePrefab.name;
            return eventCount.ToString(CultureInfo.InvariantCulture) + " presentation event(s)" +
                   (string.IsNullOrWhiteSpace(projectile) ? string.Empty : "; projectile prefab " + projectile);
        }
    }
}
