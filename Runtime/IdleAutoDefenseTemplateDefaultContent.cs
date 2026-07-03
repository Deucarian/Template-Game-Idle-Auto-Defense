using System;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    public sealed class IdleAutoDefenseTemplateStageContent
    {
        public IdleAutoDefenseTemplateStageContent(
            string id,
            string displayName,
            string encounterId,
            string[] enemyIds,
            string[] weaponIds,
            string[] upgradeIds,
            bool endlessPlaceholder = false)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            EncounterId = encounterId ?? string.Empty;
            EnemyIds = Copy(enemyIds);
            WeaponIds = Copy(weaponIds);
            UpgradeIds = Copy(upgradeIds);
            EndlessPlaceholder = endlessPlaceholder;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string EncounterId { get; }
        public string[] EnemyIds { get; }
        public string[] WeaponIds { get; }
        public string[] UpgradeIds { get; }
        public bool EndlessPlaceholder { get; }

        private static string[] Copy(string[] source)
        {
            if (source == null) return Array.Empty<string>();
            var copy = new string[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }
    }

    public sealed class IdleAutoDefenseTemplateModuleContent
    {
        public IdleAutoDefenseTemplateModuleContent(string id, string displayName, string mode, bool supportedInRuntime, string notes)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Mode = mode ?? string.Empty;
            SupportedInRuntime = supportedInRuntime;
            Notes = notes ?? string.Empty;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Mode { get; }
        public bool SupportedInRuntime { get; }
        public string Notes { get; }
    }

    public static class IdleAutoDefenseTemplateDefaultContent
    {
        public static IdleAutoDefenseTemplateStageContent[] CreateStages()
        {
            return new[]
            {
                new IdleAutoDefenseTemplateStageContent(
                    "stage.idle-auto-defense.first-orbit",
                    "First Orbit",
                    "encounter.idle-auto-defense.first-orbit",
                    new[] { "enemy.idle-auto-defense.swarm", "enemy.idle-auto-defense.runner", "enemy.idle-auto-defense.tank" },
                    new[] { "weapon.idle-auto-defense.pulse-beam", "weapon.idle-auto-defense.shard-launcher" },
                    new[] { "upgrade.idle-auto-defense.damage-up", "upgrade.idle-auto-defense.projectile-speed-up", "upgrade.idle-auto-defense.objective-repair", "upgrade.idle-auto-defense.offline-gain-up" }),
                new IdleAutoDefenseTemplateStageContent(
                    "stage.idle-auto-defense.pressure-ring",
                    "Pressure Ring",
                    "encounter.idle-auto-defense.pressure-ring",
                    new[] { "enemy.idle-auto-defense.runner", "enemy.idle-auto-defense.tank", "enemy.idle-auto-defense.shielded", "enemy.idle-auto-defense.elite", "enemy.idle-auto-defense.swarm" },
                    new[] { "weapon.idle-auto-defense.pulse-beam", "weapon.idle-auto-defense.shard-launcher" },
                    new[] { "upgrade.idle-auto-defense.fire-rate-up", "upgrade.idle-auto-defense.projectile-count-up", "upgrade.idle-auto-defense.objective-max-health-up", "upgrade.idle-auto-defense.enemy-reward-up" }),
                new IdleAutoDefenseTemplateStageContent(
                    "stage.idle-auto-defense.boss-pulse",
                    "Boss Pulse",
                    "encounter.idle-auto-defense.boss-pulse",
                    new[] { "enemy.idle-auto-defense.runner", "enemy.idle-auto-defense.shielded", "enemy.idle-auto-defense.tank", "enemy.idle-auto-defense.elite", "enemy.idle-auto-defense.boss" },
                    new[] { "weapon.idle-auto-defense.pulse-beam", "weapon.idle-auto-defense.shard-launcher" },
                    new[] { "upgrade.idle-auto-defense.direct-specialization", "upgrade.idle-auto-defense.projectile-specialization", "upgrade.idle-auto-defense.crit-chance-intent", "upgrade.idle-auto-defense.crit-damage-intent" }),
                new IdleAutoDefenseTemplateStageContent(
                    "stage.idle-auto-defense.endless-placeholder",
                    "Endless Mode Placeholder",
                    "encounter.idle-auto-defense.endless-placeholder",
                    new[] { "enemy.idle-auto-defense.swarm", "enemy.idle-auto-defense.runner" },
                    new[] { "weapon.idle-auto-defense.pulse-beam", "weapon.idle-auto-defense.shard-launcher" },
                    new[] { "upgrade.idle-auto-defense.reroll-bonus", "upgrade.idle-auto-defense.enemy-reward-up", "upgrade.idle-auto-defense.offline-gain-up" },
                    endlessPlaceholder: true)
            };
        }

        public static IdleAutoDefenseTemplateModuleContent[] CreateModules()
        {
            return new[]
            {
                new IdleAutoDefenseTemplateModuleContent("weapon.idle-auto-defense.pulse-beam", "Pulse Beam", "direct-single-target", true, "Supported by WeaponFireMode.DirectAttack."),
                new IdleAutoDefenseTemplateModuleContent("weapon.idle-auto-defense.shard-launcher", "Shard Launcher", "projectile", true, "Supported by WeaponFireMode.Projectile."),
                new IdleAutoDefenseTemplateModuleContent("weapon.idle-auto-defense.arc-burst", "Arc Burst Module", "area", true, "Sample controller unlocks this as live module damage."),
                new IdleAutoDefenseTemplateModuleContent("weapon.idle-auto-defense.homing-pulse", "Homing Pulse Module", "homing-projectile", true, "Sample controller unlocks this as live module damage.")
            };
        }
    }
}
