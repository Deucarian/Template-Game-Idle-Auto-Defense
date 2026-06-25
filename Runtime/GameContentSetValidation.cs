using System;
using System.Collections.Generic;
using System.Globalization;
using Deucarian.Attacks.Authoring;
using Deucarian.RunUpgrades.Authoring;
using Deucarian.WeaponSystems.Authoring;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    public enum GameContentSetValidationSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }

    public readonly struct GameContentSetValidationIssue
    {
        public GameContentSetValidationIssue(GameContentSetValidationSeverity severity, string path, string message)
        {
            Severity = severity;
            Path = path ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public GameContentSetValidationSeverity Severity { get; }
        public string Path { get; }
        public string Message { get; }
        public bool IsError => Severity == GameContentSetValidationSeverity.Error;

        public static GameContentSetValidationIssue Error(string path, string message)
        {
            return new GameContentSetValidationIssue(GameContentSetValidationSeverity.Error, path, message);
        }

        public static GameContentSetValidationIssue Warning(string path, string message)
        {
            return new GameContentSetValidationIssue(GameContentSetValidationSeverity.Warning, path, message);
        }
    }

    public sealed class GameContentSetValidationReport
    {
        private readonly GameContentSetValidationIssue[] _issues;

        public GameContentSetValidationReport(IReadOnlyList<GameContentSetValidationIssue> issues)
        {
            if (issues == null || issues.Count == 0)
            {
                _issues = Array.Empty<GameContentSetValidationIssue>();
                return;
            }

            _issues = new GameContentSetValidationIssue[issues.Count];
            for (int i = 0; i < issues.Count; i++) _issues[i] = issues[i];
        }

        public IReadOnlyList<GameContentSetValidationIssue> Issues => _issues;

        public bool IsValid
        {
            get
            {
                for (int i = 0; i < _issues.Length; i++)
                    if (_issues[i].IsError)
                        return false;
                return true;
            }
        }

        public int ErrorCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _issues.Length; i++)
                    if (_issues[i].IsError)
                        count++;
                return count;
            }
        }

        public int WarningCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _issues.Length; i++)
                    if (_issues[i].Severity == GameContentSetValidationSeverity.Warning)
                        count++;
                return count;
            }
        }
    }

    public sealed class GameContentSetResolution
    {
        public GameContentSetResolution(
            GameContentSetAsset contentSet,
            GameContentSetValidationReport report,
            AttackDefinitionAsset[] attackRecipes,
            EnemyDefinitionAsset[] enemies,
            WaveDefinitionAsset[] waves,
            WeaponDefinitionAsset[] weapons,
            RunUpgradeDefinitionAsset[] upgrades)
        {
            ContentSet = contentSet;
            Report = report ?? new GameContentSetValidationReport(Array.Empty<GameContentSetValidationIssue>());
            AttackRecipes = attackRecipes ?? Array.Empty<AttackDefinitionAsset>();
            Enemies = enemies ?? Array.Empty<EnemyDefinitionAsset>();
            Waves = waves ?? Array.Empty<WaveDefinitionAsset>();
            Weapons = weapons ?? Array.Empty<WeaponDefinitionAsset>();
            Upgrades = upgrades ?? Array.Empty<RunUpgradeDefinitionAsset>();
        }

        public GameContentSetAsset ContentSet { get; }
        public GameContentSetValidationReport Report { get; }
        public IReadOnlyList<AttackDefinitionAsset> AttackRecipes { get; }
        public IReadOnlyList<EnemyDefinitionAsset> Enemies { get; }
        public IReadOnlyList<WaveDefinitionAsset> Waves { get; }
        public IReadOnlyList<WeaponDefinitionAsset> Weapons { get; }
        public IReadOnlyList<RunUpgradeDefinitionAsset> Upgrades { get; }
        public bool IsValid => ContentSet != null && Report.IsValid;
    }

    public static class GameContentSetValidator
    {
        public static GameContentSetValidationReport Validate(GameContentSetAsset contentSet)
        {
            var issues = new List<GameContentSetValidationIssue>();
            if (contentSet == null)
            {
                issues.Add(GameContentSetValidationIssue.Error("ContentSet", "Game content set is missing."));
                return new GameContentSetValidationReport(issues);
            }

            if (string.IsNullOrWhiteSpace(contentSet.Id)) issues.Add(GameContentSetValidationIssue.Error("ContentSet.Id", "Content set ID is required."));
            if (string.IsNullOrWhiteSpace(contentSet.DisplayName)) issues.Add(GameContentSetValidationIssue.Warning("ContentSet.DisplayName", "Display name is empty."));
            if (contentSet.StartingWeapon == null) issues.Add(GameContentSetValidationIssue.Error("StartingWeapon", "Choose a starting tower or weapon."));
            if (contentSet.AvailableWeapons.Count == 0) issues.Add(GameContentSetValidationIssue.Error("AvailableWeapons", "Add at least one available tower or weapon."));
            if (contentSet.EnemyPool.Count == 0) issues.Add(GameContentSetValidationIssue.Error("EnemyPool", "Add at least one enemy definition."));
            if (contentSet.WaveSet.Count == 0) issues.Add(GameContentSetValidationIssue.Error("WaveSet", "Add at least one authored wave."));
            if (contentSet.UpgradePool.Count == 0) issues.Add(GameContentSetValidationIssue.Warning("UpgradePool", "No upgrades are assigned. The run remains playable, but upgrade drafts will be empty."));

            ValidateEconomy(contentSet, issues);
            HashSet<string> weaponIds = ValidateWeapons(contentSet, issues);
            HashSet<string> enemyIds = ValidateEnemies(contentSet, issues);
            ValidateWaves(contentSet, enemyIds, issues);
            ValidateUpgrades(contentSet, weaponIds, enemyIds, CollectAttackAndProjectileTargets(contentSet), issues);
            return new GameContentSetValidationReport(issues);
        }

        public static GameContentSetResolution Resolve(GameContentSetAsset contentSet)
        {
            GameContentSetValidationReport report = Validate(contentSet);
            if (contentSet == null || !report.IsValid)
                return new GameContentSetResolution(contentSet, report, null, null, null, null, null);

            return new GameContentSetResolution(
                contentSet,
                report,
                CollectAttackRecipes(contentSet),
                Copy(contentSet.EnemyPool),
                Copy(contentSet.WaveSet),
                OrderWeapons(contentSet),
                Copy(contentSet.UpgradePool));
        }

        private static void ValidateEconomy(GameContentSetAsset contentSet, List<GameContentSetValidationIssue> issues)
        {
            if (contentSet.StartingCredits < 0) issues.Add(GameContentSetValidationIssue.Error("Economy.StartingCredits", "Starting credits cannot be negative."));
            if (contentSet.StartingParts < 0) issues.Add(GameContentSetValidationIssue.Error("Economy.StartingParts", "Starting parts cannot be negative."));
            if (contentSet.RewardMultiplier <= 0f || float.IsNaN(contentSet.RewardMultiplier) || float.IsInfinity(contentSet.RewardMultiplier))
                issues.Add(GameContentSetValidationIssue.Error("Economy.RewardMultiplier", "Reward multiplier must be a finite value greater than zero."));
            if (contentSet.DifficultyMultiplier <= 0f || float.IsNaN(contentSet.DifficultyMultiplier) || float.IsInfinity(contentSet.DifficultyMultiplier))
                issues.Add(GameContentSetValidationIssue.Error("Difficulty.Multiplier", "Difficulty multiplier must be a finite value greater than zero."));
            if (!contentSet.Endless && contentSet.SessionLengthTicks <= 0)
                issues.Add(GameContentSetValidationIssue.Error("Run.SessionLengthTicks", "Session length must be greater than zero unless endless mode is enabled."));
            if (contentSet.Endless && contentSet.SessionLengthTicks <= 0)
                issues.Add(GameContentSetValidationIssue.Warning("Run.SessionLengthTicks", "Endless mode is enabled; session length is only used as an authoring preview hint."));
        }

        private static HashSet<string> ValidateWeapons(GameContentSetAsset contentSet, List<GameContentSetValidationIssue> issues)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool startingWeaponIncluded = false;
            for (int i = 0; i < contentSet.AvailableWeapons.Count; i++)
            {
                WeaponDefinitionAsset weapon = contentSet.AvailableWeapons[i];
                string path = "AvailableWeapons[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                if (weapon == null)
                {
                    issues.Add(GameContentSetValidationIssue.Error(path, "Weapon entry is empty."));
                    continue;
                }

                WeaponDefinitionValidationReport report = WeaponDefinitionValidator.Validate(weapon, WeaponDefinitionValidationOptions.RuntimeFriendly);
                AddWeaponIssues(path, report, issues);
                if (!string.IsNullOrWhiteSpace(weapon.Id) && !ids.Add(weapon.Id.Trim()))
                    issues.Add(GameContentSetValidationIssue.Error(path + ".Id", "Duplicate weapon ID: " + weapon.Id));
                if (weapon == contentSet.StartingWeapon) startingWeaponIncluded = true;
            }

            if (contentSet.StartingWeapon != null && !startingWeaponIncluded)
                issues.Add(GameContentSetValidationIssue.Error("StartingWeapon", "Starting weapon must also appear in Available Weapons so the runtime can mount it."));
            return ids;
        }

        private static HashSet<string> ValidateEnemies(GameContentSetAsset contentSet, List<GameContentSetValidationIssue> issues)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < contentSet.EnemyPool.Count; i++)
            {
                EnemyDefinitionAsset enemy = contentSet.EnemyPool[i];
                string path = "EnemyPool[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                if (enemy == null)
                {
                    issues.Add(GameContentSetValidationIssue.Error(path, "Enemy entry is empty."));
                    continue;
                }

                ContentAuthoringValidationReport report = EnemyDefinitionValidator.Validate(enemy, EnemyDefinitionValidationOptions.RuntimeFriendly);
                AddContentIssues(path, report, issues);
                if (!string.IsNullOrWhiteSpace(enemy.Id) && !ids.Add(enemy.Id.Trim()))
                    issues.Add(GameContentSetValidationIssue.Error(path + ".Id", "Duplicate enemy ID: " + enemy.Id));
            }

            return ids;
        }

        private static void ValidateWaves(GameContentSetAsset contentSet, HashSet<string> enemyIds, List<GameContentSetValidationIssue> issues)
        {
            var waveIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < contentSet.WaveSet.Count; i++)
            {
                WaveDefinitionAsset wave = contentSet.WaveSet[i];
                string path = "WaveSet[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                if (wave == null)
                {
                    issues.Add(GameContentSetValidationIssue.Error(path, "Wave entry is empty."));
                    continue;
                }

                ContentAuthoringValidationReport report = WaveDefinitionValidator.Validate(wave, WaveDefinitionValidationOptions.RuntimeFriendly);
                AddContentIssues(path, report, issues);
                if (!string.IsNullOrWhiteSpace(wave.Id) && !waveIds.Add(wave.Id.Trim()))
                    issues.Add(GameContentSetValidationIssue.Error(path + ".Id", "Duplicate wave ID: " + wave.Id));
                if (wave.Entries == null) continue;
                IReadOnlyList<WaveEntryRecipe> entries = wave.Entries.Entries;
                for (int j = 0; j < entries.Count; j++)
                {
                    WaveEntryRecipe entry = entries[j];
                    if (entry == null || entry.Enemy == null || string.IsNullOrWhiteSpace(entry.Enemy.Id)) continue;
                    if (!enemyIds.Contains(entry.Enemy.Id.Trim()))
                        issues.Add(GameContentSetValidationIssue.Error(path + ".Entries[" + j.ToString(CultureInfo.InvariantCulture) + "].Enemy", "Wave references an enemy that is not in this content set's enemy pool: " + entry.Enemy.Id));
                }
            }
        }

        private static void ValidateUpgrades(
            GameContentSetAsset contentSet,
            HashSet<string> weaponIds,
            HashSet<string> enemyIds,
            HashSet<string> attackAndProjectileIds,
            List<GameContentSetValidationIssue> issues)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var knownTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddAll(knownTargets, weaponIds);
            AddAll(knownTargets, enemyIds);
            AddAll(knownTargets, attackAndProjectileIds);

            for (int i = 0; i < contentSet.UpgradePool.Count; i++)
            {
                RunUpgradeDefinitionAsset upgrade = contentSet.UpgradePool[i];
                string path = "UpgradePool[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                if (upgrade == null)
                {
                    issues.Add(GameContentSetValidationIssue.Error(path, "Upgrade entry is empty."));
                    continue;
                }

                RunUpgradeDefinitionValidationReport report = RunUpgradeDefinitionValidator.Validate(upgrade);
                AddUpgradeIssues(path, report, issues);
                if (!string.IsNullOrWhiteSpace(upgrade.Id) && !ids.Add(upgrade.Id.Trim()))
                    issues.Add(GameContentSetValidationIssue.Error(path + ".Id", "Duplicate upgrade ID: " + upgrade.Id));
                if (upgrade.Effects == null) continue;
                IReadOnlyList<RunUpgradeEffectRecipe> effects = upgrade.Effects.Effects;
                for (int j = 0; j < effects.Count; j++)
                {
                    RunUpgradeEffectRecipe effect = effects[j];
                    if (effect == null) continue;
                    string targetId = effect.GetTargetId();
                    if (!string.IsNullOrWhiteSpace(targetId) && !knownTargets.Contains(targetId.Trim()))
                        issues.Add(GameContentSetValidationIssue.Warning(path + ".Effects[" + j.ToString(CultureInfo.InvariantCulture) + "].Target", "Upgrade target is outside this content set: " + targetId));
                }
            }
        }

        private static HashSet<string> CollectAttackAndProjectileTargets(GameContentSetAsset contentSet)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < contentSet.AvailableWeapons.Count; i++)
            {
                WeaponDefinitionAsset weapon = contentSet.AvailableWeapons[i];
                if (weapon == null || weapon.Stats == null) continue;
                AttackDefinitionAsset attack = weapon.Stats.Attack;
                if (attack != null && !string.IsNullOrWhiteSpace(attack.Id)) ids.Add(attack.Id.Trim());
                string projectileId = weapon.Stats.ResolveProjectileDefinitionId();
                if (!string.IsNullOrWhiteSpace(projectileId)) ids.Add(projectileId.Trim());
            }

            return ids;
        }

        private static AttackDefinitionAsset[] CollectAttackRecipes(GameContentSetAsset contentSet)
        {
            var recipes = new List<AttackDefinitionAsset>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < contentSet.AvailableWeapons.Count; i++)
            {
                WeaponDefinitionAsset weapon = contentSet.AvailableWeapons[i];
                AttackDefinitionAsset attack = weapon == null || weapon.Stats == null ? null : weapon.Stats.Attack;
                if (attack == null || string.IsNullOrWhiteSpace(attack.Id) || !seen.Add(attack.Id.Trim())) continue;
                recipes.Add(attack);
            }

            return recipes.ToArray();
        }

        private static WeaponDefinitionAsset[] OrderWeapons(GameContentSetAsset contentSet)
        {
            var ordered = new List<WeaponDefinitionAsset>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddWeapon(contentSet.StartingWeapon, ordered, seen);
            for (int i = 0; i < contentSet.AvailableWeapons.Count; i++)
                AddWeapon(contentSet.AvailableWeapons[i], ordered, seen);
            return ordered.ToArray();
        }

        private static void AddWeapon(WeaponDefinitionAsset weapon, List<WeaponDefinitionAsset> ordered, HashSet<string> seen)
        {
            if (weapon == null || string.IsNullOrWhiteSpace(weapon.Id) || !seen.Add(weapon.Id.Trim())) return;
            ordered.Add(weapon);
        }

        private static TAsset[] Copy<TAsset>(IReadOnlyList<TAsset> source)
        {
            if (source == null || source.Count == 0) return Array.Empty<TAsset>();
            var copy = new TAsset[source.Count];
            for (int i = 0; i < source.Count; i++) copy[i] = source[i];
            return copy;
        }

        private static void AddAll(HashSet<string> target, IEnumerable<string> source)
        {
            if (source == null) return;
            foreach (string id in source)
                if (!string.IsNullOrWhiteSpace(id))
                    target.Add(id.Trim());
        }

        private static void AddWeaponIssues(string prefix, WeaponDefinitionValidationReport report, List<GameContentSetValidationIssue> issues)
        {
            if (report == null) return;
            for (int i = 0; i < report.Issues.Count; i++)
            {
                WeaponDefinitionValidationIssue issue = report.Issues[i];
                GameContentSetValidationSeverity severity = issue.IsError ? GameContentSetValidationSeverity.Error : GameContentSetValidationSeverity.Warning;
                issues.Add(new GameContentSetValidationIssue(severity, prefix + "." + issue.Path, issue.Message));
            }
        }

        private static void AddContentIssues(string prefix, ContentAuthoringValidationReport report, List<GameContentSetValidationIssue> issues)
        {
            if (report == null) return;
            for (int i = 0; i < report.Issues.Count; i++)
            {
                ContentAuthoringValidationIssue issue = report.Issues[i];
                GameContentSetValidationSeverity severity = issue.IsError ? GameContentSetValidationSeverity.Error : GameContentSetValidationSeverity.Warning;
                issues.Add(new GameContentSetValidationIssue(severity, prefix + "." + issue.Path, issue.Message));
            }
        }

        private static void AddUpgradeIssues(string prefix, RunUpgradeDefinitionValidationReport report, List<GameContentSetValidationIssue> issues)
        {
            if (report == null) return;
            for (int i = 0; i < report.Issues.Count; i++)
            {
                RunUpgradeDefinitionValidationIssue issue = report.Issues[i];
                GameContentSetValidationSeverity severity = issue.IsError ? GameContentSetValidationSeverity.Error : GameContentSetValidationSeverity.Warning;
                issues.Add(new GameContentSetValidationIssue(severity, prefix + "." + issue.Path, issue.Message));
            }
        }
    }
}
