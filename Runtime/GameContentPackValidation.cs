using System;
using System.Collections.Generic;
using System.Globalization;
using Deucarian.Attacks.Authoring;
using Deucarian.RunUpgrades.Authoring;
using Deucarian.WeaponSystems.Authoring;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    public enum GameContentPackValidationSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }

    public readonly struct GameContentPackValidationIssue
    {
        public GameContentPackValidationIssue(GameContentPackValidationSeverity severity, string path, string message)
        {
            Severity = severity;
            Path = path ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public GameContentPackValidationSeverity Severity { get; }
        public string Path { get; }
        public string Message { get; }
        public bool IsError => Severity == GameContentPackValidationSeverity.Error;

        public static GameContentPackValidationIssue Error(string path, string message)
        {
            return new GameContentPackValidationIssue(GameContentPackValidationSeverity.Error, path, message);
        }

        public static GameContentPackValidationIssue Warning(string path, string message)
        {
            return new GameContentPackValidationIssue(GameContentPackValidationSeverity.Warning, path, message);
        }

        public static GameContentPackValidationIssue Info(string path, string message)
        {
            return new GameContentPackValidationIssue(GameContentPackValidationSeverity.Info, path, message);
        }
    }

    public sealed class GameContentPackValidationReport
    {
        private readonly GameContentPackValidationIssue[] _issues;

        public GameContentPackValidationReport(IReadOnlyList<GameContentPackValidationIssue> issues)
        {
            if (issues == null || issues.Count == 0)
            {
                _issues = Array.Empty<GameContentPackValidationIssue>();
                return;
            }

            _issues = new GameContentPackValidationIssue[issues.Count];
            for (int i = 0; i < issues.Count; i++) _issues[i] = issues[i];
        }

        public IReadOnlyList<GameContentPackValidationIssue> Issues => _issues;

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

        public int ErrorCount => Count(GameContentPackValidationSeverity.Error);
        public int WarningCount => Count(GameContentPackValidationSeverity.Warning);
        public int InfoCount => Count(GameContentPackValidationSeverity.Info);

        private int Count(GameContentPackValidationSeverity severity)
        {
            int count = 0;
            for (int i = 0; i < _issues.Length; i++)
                if (_issues[i].Severity == severity)
                    count++;
            return count;
        }
    }

    public sealed class GameContentPackDependencySummary
    {
        public GameContentPackDependencySummary(
            int contentSetCount,
            int attackCount,
            int enemyCount,
            int waveCount,
            int weaponCount,
            int upgradeCount)
        {
            ContentSetCount = contentSetCount;
            AttackCount = attackCount;
            EnemyCount = enemyCount;
            WaveCount = waveCount;
            WeaponCount = weaponCount;
            UpgradeCount = upgradeCount;
        }

        public int ContentSetCount { get; }
        public int AttackCount { get; }
        public int EnemyCount { get; }
        public int WaveCount { get; }
        public int WeaponCount { get; }
        public int UpgradeCount { get; }
    }

    public sealed class GameContentPackResolution
    {
        public GameContentPackResolution(
            GameContentPackAsset contentPack,
            GameContentSetAsset selectedContentSet,
            GameContentPackValidationReport packReport,
            GameContentSetResolution contentSetResolution)
        {
            ContentPack = contentPack;
            SelectedContentSet = selectedContentSet;
            PackReport = packReport ?? new GameContentPackValidationReport(Array.Empty<GameContentPackValidationIssue>());
            ContentSetResolution = contentSetResolution;
        }

        public GameContentPackAsset ContentPack { get; }
        public GameContentSetAsset SelectedContentSet { get; }
        public GameContentPackValidationReport PackReport { get; }
        public GameContentSetResolution ContentSetResolution { get; }
        public bool IsValid => ContentPack != null && SelectedContentSet != null && PackReport.IsValid && ContentSetResolution != null && ContentSetResolution.IsValid;
    }

    public static class GameContentPackValidator
    {
        public static GameContentPackValidationReport Validate(GameContentPackAsset contentPack)
        {
            var issues = new List<GameContentPackValidationIssue>();
            if (contentPack == null)
            {
                issues.Add(GameContentPackValidationIssue.Error("ContentPack", "Content pack is missing."));
                return new GameContentPackValidationReport(issues);
            }

            if (string.IsNullOrWhiteSpace(contentPack.Id))
                issues.Add(GameContentPackValidationIssue.Error("ContentPack.Id", "Content pack ID is required."));
            if (string.IsNullOrWhiteSpace(contentPack.DisplayName))
                issues.Add(GameContentPackValidationIssue.Warning("ContentPack.DisplayName", "Display name is empty."));
            if (string.IsNullOrWhiteSpace(contentPack.Version))
                issues.Add(GameContentPackValidationIssue.Warning("ContentPack.Version", "Version is empty; package updates will be harder to reason about."));
            if (contentPack.DefaultContentSet == null)
                issues.Add(GameContentPackValidationIssue.Error("DefaultContentSet", "Choose a default Game / Run Content Set."));
            if (contentPack.ContentSets.Count == 0)
                issues.Add(GameContentPackValidationIssue.Error("ContentSets", "Include at least one Game / Run Content Set."));

            ValidateContentSets(contentPack, issues);
            ValidatePackageMetadata(contentPack, issues);
            return new GameContentPackValidationReport(issues);
        }

        public static GameContentPackResolution Resolve(GameContentPackAsset contentPack, GameContentSetAsset selectedContentSet = null)
        {
            GameContentPackValidationReport packReport = Validate(contentPack);
            if (contentPack == null)
                return new GameContentPackResolution(null, null, packReport, null);

            GameContentSetAsset contentSet = selectedContentSet != null ? selectedContentSet : contentPack.DefaultContentSet;
            var issues = new List<GameContentPackValidationIssue>(packReport.Issues);
            if (contentSet != null && !ContainsContentSet(contentPack, contentSet))
            {
                issues.Add(GameContentPackValidationIssue.Error(
                    "SelectedContentSet",
                    "Selected content set is not included in this content pack: " + contentSet.DisplayName));
            }

            packReport = new GameContentPackValidationReport(issues);
            if (!packReport.IsValid || contentSet == null)
                return new GameContentPackResolution(contentPack, contentSet, packReport, null);

            GameContentSetResolution contentSetResolution = GameContentSetValidator.Resolve(contentSet);
            return new GameContentPackResolution(contentPack, contentSet, packReport, contentSetResolution);
        }

        public static GameContentPackDependencySummary CollectDependencies(GameContentPackAsset contentPack)
        {
            var contentSets = new HashSet<GameContentSetAsset>();
            var attacks = new HashSet<AttackDefinitionAsset>();
            var enemies = new HashSet<EnemyDefinitionAsset>();
            var waves = new HashSet<WaveDefinitionAsset>();
            var weapons = new HashSet<WeaponDefinitionAsset>();
            var upgrades = new HashSet<RunUpgradeDefinitionAsset>();

            if (contentPack != null)
            {
                for (int i = 0; i < contentPack.ContentSets.Count; i++)
                    AddContentSet(contentPack.ContentSets[i], contentSets, attacks, enemies, waves, weapons, upgrades);
            }

            return new GameContentPackDependencySummary(
                contentSets.Count,
                attacks.Count,
                enemies.Count,
                waves.Count,
                weapons.Count,
                upgrades.Count);
        }

        private static void ValidateContentSets(GameContentPackAsset contentPack, List<GameContentPackValidationIssue> issues)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool defaultIncluded = false;
            for (int i = 0; i < contentPack.ContentSets.Count; i++)
            {
                GameContentSetAsset contentSet = contentPack.ContentSets[i];
                string path = "ContentSets[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                if (contentSet == null)
                {
                    issues.Add(GameContentPackValidationIssue.Error(path, "Included content set entry is empty."));
                    continue;
                }

                if (contentSet == contentPack.DefaultContentSet) defaultIncluded = true;
                if (!string.IsNullOrWhiteSpace(contentSet.Id) && !ids.Add(contentSet.Id.Trim()))
                    issues.Add(GameContentPackValidationIssue.Error(path + ".Id", "Duplicate content set ID in pack: " + contentSet.Id));

                GameContentSetValidationReport report = GameContentSetValidator.Validate(contentSet);
                for (int j = 0; j < report.Issues.Count; j++)
                {
                    GameContentSetValidationIssue issue = report.Issues[j];
                    GameContentPackValidationSeverity severity = issue.IsError ? GameContentPackValidationSeverity.Error : GameContentPackValidationSeverity.Warning;
                    issues.Add(new GameContentPackValidationIssue(severity, path + "." + issue.Path, issue.Message));
                }
            }

            if (contentPack.DefaultContentSet != null && !defaultIncluded)
                issues.Add(GameContentPackValidationIssue.Error("DefaultContentSet", "Default content set must also be included in the pack."));
        }

        private static void ValidatePackageMetadata(GameContentPackAsset contentPack, List<GameContentPackValidationIssue> issues)
        {
            var required = contentPack.RequiredPackages;
            var versions = contentPack.MinimumPackageVersions;
            for (int i = 0; i < required.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(required[i]))
                    issues.Add(GameContentPackValidationIssue.Warning("RequiredPackages[" + i.ToString(CultureInfo.InvariantCulture) + "]", "Required package ID is empty."));
            }

            if (versions.Count > 0 && versions.Count != required.Count)
                issues.Add(GameContentPackValidationIssue.Warning("MinimumPackageVersions", "Minimum package version list should align with required packages."));
        }

        private static bool ContainsContentSet(GameContentPackAsset contentPack, GameContentSetAsset contentSet)
        {
            if (contentPack == null || contentSet == null) return false;
            for (int i = 0; i < contentPack.ContentSets.Count; i++)
                if (contentPack.ContentSets[i] == contentSet)
                    return true;
            return false;
        }

        private static void AddContentSet(
            GameContentSetAsset contentSet,
            HashSet<GameContentSetAsset> contentSets,
            HashSet<AttackDefinitionAsset> attacks,
            HashSet<EnemyDefinitionAsset> enemies,
            HashSet<WaveDefinitionAsset> waves,
            HashSet<WeaponDefinitionAsset> weapons,
            HashSet<RunUpgradeDefinitionAsset> upgrades)
        {
            if (contentSet == null || !contentSets.Add(contentSet)) return;
            for (int i = 0; i < contentSet.AvailableWeapons.Count; i++)
            {
                WeaponDefinitionAsset weapon = contentSet.AvailableWeapons[i];
                if (weapon == null || !weapons.Add(weapon)) continue;
                if (weapon.Stats != null && weapon.Stats.Attack != null)
                    attacks.Add(weapon.Stats.Attack);
            }

            for (int i = 0; i < contentSet.EnemyPool.Count; i++)
                if (contentSet.EnemyPool[i] != null)
                    enemies.Add(contentSet.EnemyPool[i]);
            for (int i = 0; i < contentSet.WaveSet.Count; i++)
                if (contentSet.WaveSet[i] != null)
                    waves.Add(contentSet.WaveSet[i]);
            for (int i = 0; i < contentSet.UpgradePool.Count; i++)
                if (contentSet.UpgradePool[i] != null)
                    upgrades.Add(contentSet.UpgradePool[i]);
        }
    }
}
