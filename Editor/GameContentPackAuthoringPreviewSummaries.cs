using System;
using System.Collections.Generic;
using System.Globalization;
using Deucarian.GameContentAuthoring.Editor;

namespace Deucarian.TemplateGameIdleAutoDefense.Editor
{
    public static class GameContentPackAuthoringPreviewSummaries
    {
        public static string PreviewStatus(GameContentPackAuthoringState state, GameContentPackValidationReport report)
        {
            if (state == null) return "Content pack preview unavailable: authoring state is missing.";
            if (report == null || !report.IsValid)
                return "Content pack preview found blocking issues; fix validation before installing this recipe.";
            return "Ready to install: " + state.DisplayName + " uses " + GetAssetName(state.DefaultContentSet, "no default content set") + ".";
        }

        public static IReadOnlyList<GameContentAuthoringPreviewRow> BuildReadinessRows(GameContentPackAuthoringState state, GameContentPackValidationReport report)
        {
            if (state == null) return Array.Empty<GameContentAuthoringPreviewRow>();
            string status = report != null && report.IsValid ? "Ready to install" : "Needs fixes";
            string warnings = report == null ? "0" : report.WarningCount.ToString(CultureInfo.InvariantCulture);
            string blockers = report == null ? "0" : report.ErrorCount.ToString(CultureInfo.InvariantCulture);
            return new[]
            {
                Row("Status", status),
                Row("Blockers", blockers),
                Row("Warnings", warnings),
                Row("Default Set", GetAssetName(state.DefaultContentSet, "Not assigned")),
                Row("Output", GameContentPackAssetCreator.GetContentPackFolder(state))
            };
        }

        public static IReadOnlyList<GameContentAuthoringPreviewRow> BuildSummaryRows(GameContentPackAuthoringState state, GameContentPackDependencySummary dependencies)
        {
            if (state == null) return Array.Empty<GameContentAuthoringPreviewRow>();
            dependencies ??= new GameContentPackDependencySummary(0, 0, 0, 0, 0, 0);
            return new[]
            {
                Row("Pack ID", state.PackId),
                Row("Version", string.IsNullOrWhiteSpace(state.Version) ? "Not set" : state.Version),
                Row("Author", string.IsNullOrWhiteSpace(state.Author) ? "Not set" : state.Author),
                Row("Content Sets", dependencies.ContentSetCount.ToString(CultureInfo.InvariantCulture)),
                Row("Weapons", dependencies.WeaponCount.ToString(CultureInfo.InvariantCulture)),
                Row("Attacks", dependencies.AttackCount.ToString(CultureInfo.InvariantCulture)),
                Row("Enemies", dependencies.EnemyCount.ToString(CultureInfo.InvariantCulture)),
                Row("Waves", dependencies.WaveCount.ToString(CultureInfo.InvariantCulture)),
                Row("Upgrades", dependencies.UpgradeCount.ToString(CultureInfo.InvariantCulture)),
                Row("Required Packages", GameContentAuthoringEditorAssets.SplitCsv(state.RequiredPackagesCsv).Length.ToString(CultureInfo.InvariantCulture))
            };
        }

        public static IReadOnlyList<GameContentAuthoringPreviewRow> BuildDependencyRows(GameContentPackAuthoringState state)
        {
            if (state == null) return Array.Empty<GameContentAuthoringPreviewRow>();
            var rows = new List<GameContentAuthoringPreviewRow>
            {
                Row("Content Pack", state.DisplayName)
            };

            for (int i = 0; i < state.ContentSets.Count; i++)
            {
                GameContentSetAsset contentSet = state.ContentSets[i];
                rows.Add(Row("Set " + (i + 1).ToString(CultureInfo.InvariantCulture), GetAssetName(contentSet, "Missing content set")));
                if (contentSet == null) continue;
                rows.Add(Row("  Starting", GetAssetName(contentSet.StartingWeapon, "Missing starting weapon")));
                rows.Add(Row("  Weapons", contentSet.AvailableWeapons.Count.ToString(CultureInfo.InvariantCulture)));
                rows.Add(Row("  Enemies", contentSet.EnemyPool.Count.ToString(CultureInfo.InvariantCulture)));
                rows.Add(Row("  Waves", contentSet.WaveSet.Count.ToString(CultureInfo.InvariantCulture)));
                rows.Add(Row("  Upgrades", contentSet.UpgradePool.Count.ToString(CultureInfo.InvariantCulture)));
            }

            return rows;
        }

        public static IReadOnlyList<GameContentAuthoringPreviewRow> BuildSceneSetupRows(GameContentPackSceneSetupState setup)
        {
            if (setup == null) return Array.Empty<GameContentAuthoringPreviewRow>();
            return new[]
            {
                Row("Controller", setup.Controller == null ? "Not selected" : setup.Controller.name),
                Row("Pack", GetAssetName(setup.ContentPack, "Not selected")),
                Row("Selected Set", GetAssetName(setup.SelectedContentSet, "Pack default"))
            };
        }

        public static IReadOnlyList<string> BuildWarnings(GameContentPackValidationReport report)
        {
            if (report == null || report.Issues.Count == 0) return Array.Empty<string>();
            var warnings = new List<string>();
            for (int i = 0; i < report.Issues.Count; i++)
            {
                GameContentPackValidationIssue issue = report.Issues[i];
                string prefix = issue.IsError ? "Blocker" : "Warning";
                warnings.Add(prefix + " - " + issue.Path + ": " + issue.Message);
            }

            return warnings;
        }

        private static GameContentAuthoringPreviewRow Row(string label, string value)
        {
            return new GameContentAuthoringPreviewRow(label, value);
        }

        private static string GetAssetName(UnityEngine.Object asset, string empty)
        {
            return asset == null ? empty : asset.name;
        }
    }
}
