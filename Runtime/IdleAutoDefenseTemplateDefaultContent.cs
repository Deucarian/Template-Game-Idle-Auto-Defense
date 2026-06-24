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
                    "stage.template.first-orbit",
                    "First Orbit",
                    "encounter.template.first-orbit",
                    new[] { "enemy.template.swarm", "enemy.template.runner", "enemy.template.tank" },
                    new[] { "weapon.template.pulse-cannon", "weapon.template.shard-launcher" },
                    new[] { "upgrade.template.damage-up", "upgrade.template.projectile-speed-up", "upgrade.template.objective-repair", "upgrade.template.offline-gain-up" }),
                new IdleAutoDefenseTemplateStageContent(
                    "stage.template.pressure-ring",
                    "Pressure Ring",
                    "encounter.template.pressure-ring",
                    new[] { "enemy.template.runner", "enemy.template.tank", "enemy.template.shielded", "enemy.template.elite", "enemy.template.swarm" },
                    new[] { "weapon.template.pulse-cannon", "weapon.template.shard-launcher" },
                    new[] { "upgrade.template.fire-rate-up", "upgrade.template.projectile-count-up", "upgrade.template.objective-max-health-up", "upgrade.template.enemy-reward-up" }),
                new IdleAutoDefenseTemplateStageContent(
                    "stage.template.boss-pulse",
                    "Boss Pulse",
                    "encounter.template.boss-pulse",
                    new[] { "enemy.template.runner", "enemy.template.shielded", "enemy.template.tank", "enemy.template.elite", "enemy.template.boss" },
                    new[] { "weapon.template.pulse-cannon", "weapon.template.shard-launcher" },
                    new[] { "upgrade.template.direct-specialization", "upgrade.template.projectile-specialization", "upgrade.template.crit-chance-intent", "upgrade.template.crit-damage-intent" }),
                new IdleAutoDefenseTemplateStageContent(
                    "stage.template.endless-placeholder",
                    "Endless Mode Placeholder",
                    "encounter.template.endless-placeholder",
                    new[] { "enemy.template.swarm", "enemy.template.runner" },
                    new[] { "weapon.template.pulse-cannon", "weapon.template.shard-launcher" },
                    new[] { "upgrade.template.reroll-bonus", "upgrade.template.enemy-reward-up", "upgrade.template.offline-gain-up" },
                    endlessPlaceholder: true)
            };
        }

        public static IdleAutoDefenseTemplateModuleContent[] CreateModules()
        {
            return new[]
            {
                new IdleAutoDefenseTemplateModuleContent("weapon.template.pulse-cannon", "Pulse Cannon", "direct-single-target", true, "Supported by WeaponFireMode.DirectAttack."),
                new IdleAutoDefenseTemplateModuleContent("weapon.template.shard-launcher", "Shard Launcher", "projectile", true, "Supported by WeaponFireMode.Projectile."),
                new IdleAutoDefenseTemplateModuleContent("weapon.template.arc-emitter", "Arc Emitter", "chain-beam-placeholder", false, "Future package work: current weapon runtime has no chain or beam target resolver."),
                new IdleAutoDefenseTemplateModuleContent("weapon.template.orbital-shot", "Orbital Shot", "delayed-area-placeholder", false, "Future package work: current weapon runtime has no mine, orbital, or delayed area behavior.")
            };
        }
    }
}
