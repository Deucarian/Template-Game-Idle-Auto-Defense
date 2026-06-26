using System;
using System.Collections.Generic;
using System.Globalization;
using Deucarian.Attacks.Authoring;
using Deucarian.GameContentAuthoring.Editor;
using Deucarian.RunUpgrades.Authoring;
using Deucarian.WeaponSystems.Authoring;
using UnityEditor;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense.Editor
{
    [InitializeOnLoad]
    internal static class GameContentSetAuthoringProviderRegistration
    {
        static GameContentSetAuthoringProviderRegistration()
        {
            GameContentAuthoringProviderRegistry.Register(new GameContentSetAuthoringProvider());
        }
    }

    internal sealed class GameContentSetAuthoringProvider : IGameContentAuthoringProvider
    {
        private readonly GameContentSetAuthoringState _state = new GameContentSetAuthoringState();
        private readonly GameContentSetPreviewController _preview = new GameContentSetPreviewController();

        public string ProviderId => "com.deucarian.template.idle-auto-defense.game-content-set";
        public string DisplayName => "Game / Run Content Set";
        public string Description => "Create a playable idle auto-defense run recipe from authored attacks, enemies, waves, weapons, and upgrades.";
        public int SortOrder => 150;
        public bool Enabled => true;
        public void OnSelected() { }
        public void DrawPreview(GameContentAuthoringPreviewContext context) { _preview.Draw(context, _state); }
        public void StopPreview() { _preview.Stop(); }

        public void Draw(GameContentAuthoringContext context)
        {
            GameContentSetAsset preview = GameContentSetAssetCreator.BuildTransient(_state);
            GameContentAuthoringValidationResult report;
            try
            {
                report = GameContentSetAssetCreator.ValidateForCreation(_state, preview);
            }
            finally
            {
                GameContentSetAssetCreator.DestroyTransient(preview);
            }

            context.DrawSection("Content Set Identity", () =>
            {
                _state.ContentSetId = context.DrawTextField("Stable ID", _state.ContentSetId);
                _state.DisplayName = context.DrawTextField("Display Name", _state.DisplayName);
                _state.Description = context.DrawTextArea("Description", _state.Description);
                _state.Icon = context.DrawObjectField("Icon", _state.Icon);
                _state.Banner = context.DrawObjectField("Banner", _state.Banner);
                _state.TagsCsv = context.DrawTextField("Tags", _state.TagsCsv);
                _state.OutputRoot = context.DrawOutputRootField(_state.OutputRoot);
            });

            context.DrawSection("Loadout", () =>
            {
                _state.StartingWeapon = context.DrawObjectField("Starting Weapon", _state.StartingWeapon);
                DrawAssetList(context, "Available Weapons / Towers", _state.AvailableWeapons, "Add Weapon");
            });

            context.DrawSection("Enemies And Waves", () =>
            {
                DrawAssetList(context, "Enemy Pool", _state.EnemyPool, "Add Enemy");
                GUILayout.Space(6f);
                DrawAssetList(context, "Wave / Spawn Set", _state.WaveSet, "Add Wave");
            });

            context.DrawSection("Upgrade Pool", () =>
            {
                DrawAssetList(context, "Upgrades", _state.UpgradePool, "Add Upgrade");
                EditorGUILayout.LabelField("An empty upgrade pool is allowed; the playable run simply has no upgrade draft choices.", context.MutedStyle);
            });

            context.DrawSection("Run Settings", () =>
            {
                _state.StartingCredits = context.DrawIntField("Starting Credits", _state.StartingCredits);
                _state.StartingParts = context.DrawIntField("Starting Parts", _state.StartingParts);
                _state.RewardMultiplier = context.DrawFloatField("Reward Multiplier", _state.RewardMultiplier);
                _state.DifficultyMultiplier = context.DrawFloatField("Difficulty Multiplier", _state.DifficultyMultiplier);
                _state.SessionLengthTicks = context.DrawIntField("Session Length Ticks", _state.SessionLengthTicks);
                _state.Endless = context.DrawToggle("Endless", _state.Endless);
            });

            context.DrawSection("Preview", () =>
            {
                foreach (string line in GameContentSetAssetCreator.GetPreviewLines(_state))
                    EditorGUILayout.LabelField(line, context.MutedStyle);
                GUILayout.Space(6f);
                context.DrawValidation(report, "Ready to create one root GameContentSet asset that references existing authored content.");
                GUILayout.Space(8f);
                if (context.DrawCreateButton("Create Content Set", report.IsValid))
                    context.SetCreationResult(GameContentSetAssetCreator.CreateAssets(_state));
                context.DrawCreationResult();
            });
        }

        private static void DrawAssetList<TAsset>(GameContentAuthoringContext context, string title, List<TAsset> assets, string addLabel) where TAsset : UnityEngine.Object
        {
            context.DrawInlineCard(() =>
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(title, context.SectionTitleStyle);
                    GUILayout.FlexibleSpace();
                    if (context.DrawSecondaryButton(addLabel, true, GUILayout.Width(104f), GUILayout.Height(22f)))
                        assets.Add(null);
                }

                if (assets.Count == 0)
                    EditorGUILayout.LabelField("None assigned.", context.MutedStyle);

                for (int i = 0; i < assets.Count; i++)
                {
                    bool remove = false;
                    context.DrawInlineCard(() =>
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("Item " + (i + 1).ToString(CultureInfo.InvariantCulture), context.SectionTitleStyle);
                            GUILayout.FlexibleSpace();
                            if (context.DrawSecondaryButton("Remove", true, GUILayout.Width(70f), GUILayout.Height(22f)))
                                remove = true;
                        }

                        if (remove) return;
                        assets[i] = context.DrawObjectField("Item " + (i + 1).ToString(CultureInfo.InvariantCulture), assets[i]);
                    });

                    if (remove)
                    {
                        assets.RemoveAt(i);
                        i--;
                    }
                }
            });
        }
    }

    public sealed class GameContentSetAuthoringState
    {
        public string ContentSetId = "contentset.example.basic-idle-auto-defense";
        public string DisplayName = "Basic Idle Auto Defense Content Set";
        public string Description = "Assembles a playable idle auto-defense run from authored Deucarian content.";
        public Sprite Icon;
        public Texture2D Banner;
        public WeaponDefinitionAsset StartingWeapon;
        public readonly List<WeaponDefinitionAsset> AvailableWeapons = new List<WeaponDefinitionAsset>();
        public readonly List<EnemyDefinitionAsset> EnemyPool = new List<EnemyDefinitionAsset>();
        public readonly List<WaveDefinitionAsset> WaveSet = new List<WaveDefinitionAsset>();
        public readonly List<RunUpgradeDefinitionAsset> UpgradePool = new List<RunUpgradeDefinitionAsset>();
        public int StartingCredits = 60;
        public int StartingParts;
        public float RewardMultiplier = 1f;
        public float DifficultyMultiplier = 1f;
        public int SessionLengthTicks = 180;
        public bool Endless;
        public string TagsCsv = "template, run";
        public string OutputRoot = "Assets/GameContent/ContentSets";
    }

    internal sealed class GameContentSetPreviewController
    {
        private string _status = "Preview idle";

        public void Draw(GameContentAuthoringPreviewContext context, GameContentSetAuthoringState state)
        {
            if (context == null) return;
            context.SetStatus(_status);

            GameContentSetAsset preview = GameContentSetAssetCreator.BuildTransient(state);
            GameContentSetValidationReport report;
            try
            {
                report = GameContentSetValidator.Validate(preview);
            }
            finally
            {
                GameContentSetAssetCreator.DestroyTransient(preview);
            }

            context.DrawCard("Playable Run Preview", () =>
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (context.DrawPrimaryButton("Preview Content Set", true, GUILayout.Height(26f)))
                        SetStatus(context, GameContentSetAuthoringPreviewSummaries.PreviewStatus(state, report));
                    if (context.DrawSecondaryButton("Stop Preview", true, GUILayout.Width(104f), GUILayout.Height(26f)))
                        Stop(context);
                }

                context.DrawStatus(_status);
            });

            context.DrawCard("Ready To Play", () =>
            {
                context.DrawSummaryRows(GameContentSetAuthoringPreviewSummaries.BuildReadinessRows(state, report));
            });

            context.DrawCard("Game Recipe Summary", () =>
            {
                context.DrawSummaryRows(GameContentSetAuthoringPreviewSummaries.BuildSummaryRows(state));
            });

            context.DrawCard("Dependency Graph", () =>
            {
                context.DrawSummaryRows(GameContentSetAuthoringPreviewSummaries.BuildDependencyRows(state));
            });

            context.DrawObjectPreview(state == null ? null : state.Banner, "Banner", "Optional banner/reference image is not assigned.");
            context.DrawWarnings(GameContentSetAuthoringPreviewSummaries.BuildWarnings(report));
        }

        public void Stop()
        {
            _status = "Preview stopped";
        }

        private void Stop(GameContentAuthoringPreviewContext context)
        {
            Stop();
            context.SetStatus(_status);
        }

        private void SetStatus(GameContentAuthoringPreviewContext context, string status)
        {
            _status = string.IsNullOrWhiteSpace(status) ? "Preview idle" : status;
            context.SetStatus(_status);
        }
    }

    public static class GameContentSetAuthoringPreviewSummaries
    {
        public static string PreviewStatus(GameContentSetAuthoringState state, GameContentSetValidationReport report)
        {
            if (state == null) return "Content set preview unavailable: authoring state is missing.";
            if (report == null || !report.IsValid)
                return "Content set preview found blocking issues; fix validation before using this run recipe.";
            return "Ready to play: " + state.DisplayName + " starts with " + GetAssetName(state.StartingWeapon, "no starting weapon") + ".";
        }

        public static IReadOnlyList<GameContentAuthoringPreviewRow> BuildReadinessRows(GameContentSetAuthoringState state, GameContentSetValidationReport report)
        {
            if (state == null) return Array.Empty<GameContentAuthoringPreviewRow>();
            string status = report != null && report.IsValid ? "Ready to play" : "Needs fixes";
            string warnings = report == null ? "0" : report.WarningCount.ToString(CultureInfo.InvariantCulture);
            string blockers = report == null ? "0" : report.ErrorCount.ToString(CultureInfo.InvariantCulture);
            return new[]
            {
                Row("Status", status),
                Row("Blockers", blockers),
                Row("Warnings", warnings),
                Row("Output", GameContentSetAssetCreator.GetContentSetFolder(state))
            };
        }

        public static IReadOnlyList<GameContentAuthoringPreviewRow> BuildSummaryRows(GameContentSetAuthoringState state)
        {
            if (state == null) return Array.Empty<GameContentAuthoringPreviewRow>();
            return new[]
            {
                Row("Starting", GetAssetName(state.StartingWeapon, "Not assigned")),
                Row("Weapons", CountAssigned(state.AvailableWeapons).ToString(CultureInfo.InvariantCulture)),
                Row("Enemies", CountAssigned(state.EnemyPool).ToString(CultureInfo.InvariantCulture)),
                Row("Waves", CountAssigned(state.WaveSet).ToString(CultureInfo.InvariantCulture)),
                Row("Enemy Count", CountWaveEnemies(state.WaveSet).ToString(CultureInfo.InvariantCulture)),
                Row("Duration", ApproximateDuration(state.WaveSet).ToString(CultureInfo.InvariantCulture) + " tick(s)"),
                Row("Upgrades", CountAssigned(state.UpgradePool).ToString(CultureInfo.InvariantCulture)),
                Row("Resources", state.StartingCredits.ToString(CultureInfo.InvariantCulture) + " credits, " + state.StartingParts.ToString(CultureInfo.InvariantCulture) + " parts"),
                Row("Economy", "Rewards x" + FormatFloat(state.RewardMultiplier) + ", difficulty x" + FormatFloat(state.DifficultyMultiplier)),
                Row("Mode", state.Endless ? "Endless" : "Session length " + state.SessionLengthTicks.ToString(CultureInfo.InvariantCulture) + " ticks")
            };
        }

        public static IReadOnlyList<GameContentAuthoringPreviewRow> BuildDependencyRows(GameContentSetAuthoringState state)
        {
            if (state == null) return Array.Empty<GameContentAuthoringPreviewRow>();
            return new[]
            {
                Row("Weapons -> Attacks", CountWeaponAttackReferences(state.AvailableWeapons).ToString(CultureInfo.InvariantCulture) + " referenced attack(s)"),
                Row("Waves -> Enemies", CountWaveEnemyReferences(state.WaveSet).ToString(CultureInfo.InvariantCulture) + " enemy reference(s)"),
                Row("Upgrades -> Targets", CountUpgradeTargetReferences(state.UpgradePool).ToString(CultureInfo.InvariantCulture) + " target reference(s)")
            };
        }

        public static IReadOnlyList<string> BuildWarnings(GameContentSetValidationReport report)
        {
            if (report == null || report.Issues.Count == 0) return Array.Empty<string>();
            var warnings = new List<string>();
            for (int i = 0; i < report.Issues.Count; i++)
            {
                GameContentSetValidationIssue issue = report.Issues[i];
                string prefix = issue.IsError ? "Blocker" : "Warning";
                warnings.Add(prefix + " - " + issue.Path + ": " + issue.Message);
            }

            return warnings;
        }

        private static int CountAssigned<TAsset>(IReadOnlyList<TAsset> assets) where TAsset : UnityEngine.Object
        {
            if (assets == null) return 0;
            int count = 0;
            for (int i = 0; i < assets.Count; i++)
                if (assets[i] != null)
                    count++;
            return count;
        }

        private static int CountWaveEnemies(IReadOnlyList<WaveDefinitionAsset> waves)
        {
            if (waves == null) return 0;
            int count = 0;
            for (int i = 0; i < waves.Count; i++)
            {
                WaveDefinitionAsset wave = waves[i];
                if (wave == null || wave.Entries == null) continue;
                IReadOnlyList<WaveEntryRecipe> entries = wave.Entries.Entries;
                for (int j = 0; j < entries.Count; j++)
                    if (entries[j] != null)
                        count += Math.Max(0, entries[j].Count);
            }

            return count;
        }

        private static int ApproximateDuration(IReadOnlyList<WaveDefinitionAsset> waves)
        {
            if (waves == null) return 0;
            int maxTick = 0;
            for (int i = 0; i < waves.Count; i++)
            {
                WaveDefinitionAsset wave = waves[i];
                if (wave == null || wave.Schedule == null || wave.Entries == null) continue;
                IReadOnlyList<WaveEntryRecipe> entries = wave.Entries.Entries;
                for (int j = 0; j < entries.Count; j++)
                {
                    WaveEntryRecipe entry = entries[j];
                    if (entry == null) continue;
                    int batches = Math.Max(1, (int)Math.Ceiling(entry.Count / (double)Math.Max(1, entry.BatchSize)));
                    int endTick = wave.Schedule.StartTick + entry.InitialDelayTicks + Math.Max(0, batches - 1) * entry.IntervalTicks;
                    maxTick = Math.Max(maxTick, endTick);
                }
            }

            return maxTick;
        }

        private static int CountWeaponAttackReferences(IReadOnlyList<WeaponDefinitionAsset> weapons)
        {
            if (weapons == null) return 0;
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < weapons.Count; i++)
            {
                WeaponDefinitionAsset weapon = weapons[i];
                if (weapon == null || weapon.Stats == null || weapon.Stats.Attack == null || string.IsNullOrWhiteSpace(weapon.Stats.Attack.Id)) continue;
                ids.Add(weapon.Stats.Attack.Id.Trim());
            }

            return ids.Count;
        }

        private static int CountWaveEnemyReferences(IReadOnlyList<WaveDefinitionAsset> waves)
        {
            if (waves == null) return 0;
            int count = 0;
            for (int i = 0; i < waves.Count; i++)
            {
                WaveDefinitionAsset wave = waves[i];
                if (wave == null || wave.Entries == null) continue;
                IReadOnlyList<WaveEntryRecipe> entries = wave.Entries.Entries;
                for (int j = 0; j < entries.Count; j++)
                    if (entries[j] != null && entries[j].Enemy != null)
                        count++;
            }

            return count;
        }

        private static int CountUpgradeTargetReferences(IReadOnlyList<RunUpgradeDefinitionAsset> upgrades)
        {
            if (upgrades == null) return 0;
            int count = 0;
            for (int i = 0; i < upgrades.Count; i++)
            {
                RunUpgradeDefinitionAsset upgrade = upgrades[i];
                if (upgrade == null || upgrade.Effects == null) continue;
                IReadOnlyList<RunUpgradeEffectRecipe> effects = upgrade.Effects.Effects;
                for (int j = 0; j < effects.Count; j++)
                    if (effects[j] != null && !string.IsNullOrWhiteSpace(effects[j].GetTargetId()))
                        count++;
            }

            return count;
        }

        private static GameContentAuthoringPreviewRow Row(string label, string value)
        {
            return new GameContentAuthoringPreviewRow(label, value);
        }

        private static string GetAssetName(UnityEngine.Object asset, string empty)
        {
            return asset == null ? empty : asset.name;
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }

    internal static class GameContentSetAssetCreator
    {
        private const string DefaultRoot = "Assets/GameContent/ContentSets";

        public static GameContentSetAsset BuildTransient(GameContentSetAuthoringState state)
        {
            GameContentSetAsset asset = ScriptableObject.CreateInstance<GameContentSetAsset>();
            asset.hideFlags = HideFlags.HideAndDontSave;
            asset.Configure(
                state.ContentSetId,
                state.DisplayName,
                state.Description,
                state.Icon,
                state.Banner,
                state.StartingWeapon,
                state.AvailableWeapons,
                state.EnemyPool,
                state.WaveSet,
                state.UpgradePool,
                state.StartingCredits,
                state.StartingParts,
                state.RewardMultiplier,
                state.DifficultyMultiplier,
                state.SessionLengthTicks,
                state.Endless,
                GameContentAuthoringEditorAssets.SplitCsv(state.TagsCsv));
            return asset;
        }

        public static GameContentAuthoringValidationResult ValidateForCreation(GameContentSetAuthoringState state, GameContentSetAsset asset)
        {
            var issues = ToSharedIssues(GameContentSetValidator.Validate(asset));
            string folder = GetContentSetFolder(state);
            string rootPath = GetRootPath(state);
            GameContentAuthoringEditorAssets.AddPathIssues(issues, state.OutputRoot, DefaultRoot, folder, rootPath, "Content Set", "OutputRoot");
            if (GameContentAuthoringEditorAssets.HasDuplicateId<GameContentSetAsset>(state.ContentSetId, item => item.Id))
                issues.Add(GameContentAuthoringValidationIssue.Error("ContentSet.Id", "Content set IDs must be unique. Rename this set or edit the existing asset instead of creating another."));
            return new GameContentAuthoringValidationResult(issues);
        }

        public static IReadOnlyList<string> GetPreviewLines(GameContentSetAuthoringState state)
        {
            return new[]
            {
                "Folder: " + GetContentSetFolder(state),
                "Root asset: " + GetFileStem(state) + "_GameContentSet.asset",
                "References: starting weapon, available weapons, enemy pool, waves, and upgrades.",
                "Runtime: template consumes the assigned set when validation is complete; missing or invalid sets fall back safely.",
                "Optional media: icon and banner are metadata only and can be left unassigned."
            };
        }

        public static GameContentCreationResult CreateAssets(GameContentSetAuthoringState state)
        {
            GameContentSetAsset preview = BuildTransient(state);
            GameContentAuthoringValidationResult report;
            try
            {
                report = ValidateForCreation(state, preview);
                if (!report.IsValid)
                    return new GameContentCreationResult(false, "Fix validation errors before creating the content set.", null);
            }
            finally
            {
                DestroyTransient(preview);
            }

            string folder = GetContentSetFolder(state);
            string rootPath = GetRootPath(state);
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(rootPath) != null)
                return new GameContentCreationResult(false, "Asset already exists: " + rootPath, null);
            if (AssetDatabase.IsValidFolder(folder) && GameContentAuthoringEditorPaths.FolderContainsAssets(folder))
            {
                bool confirmed = GameContentAuthoringEditorAssets.ConfirmExistingFolder(folder, "Content Set");
                if (!confirmed)
                    return new GameContentCreationResult(false, "Creation canceled before writing into existing folder.", null);
            }

            folder = GameContentAuthoringEditorPaths.EnsureFolder(folder, DefaultRoot);
            GameContentSetAsset root = BuildTransient(state);
            root.hideFlags = HideFlags.None;
            AssetDatabase.CreateAsset(root, rootPath);
            EditorUtility.SetDirty(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return new GameContentCreationResult(true, "Created content set at " + rootPath, AssetDatabase.LoadAssetAtPath<GameContentSetAsset>(rootPath));
        }

        public static void DestroyTransient(GameContentSetAsset asset)
        {
            if (asset == null || asset.hideFlags != HideFlags.HideAndDontSave) return;
            GameContentAuthoringEditorAssets.DestroyTransientObject(asset);
        }

        public static string GetContentSetFolder(GameContentSetAuthoringState state)
        {
            string root = GameContentAuthoringEditorPaths.NormalizeAssetFolderPath(state.OutputRoot, DefaultRoot);
            return root.TrimEnd('/') + "/" + GetFileStem(state);
        }

        private static string GetRootPath(GameContentSetAuthoringState state)
        {
            return GetContentSetFolder(state) + "/" + GetFileStem(state) + "_GameContentSet.asset";
        }

        private static string GetFileStem(GameContentSetAuthoringState state)
        {
            return GameContentAuthoringEditorPaths.SanitizePathSegment(state.ContentSetId, "NewContentSet");
        }

        private static List<GameContentAuthoringValidationIssue> ToSharedIssues(GameContentSetValidationReport report)
        {
            var issues = new List<GameContentAuthoringValidationIssue>();
            if (report == null) return issues;
            for (int i = 0; i < report.Issues.Count; i++)
            {
                GameContentSetValidationIssue issue = report.Issues[i];
                GameContentAuthoringValidationSeverity severity = issue.Severity == GameContentSetValidationSeverity.Error
                    ? GameContentAuthoringValidationSeverity.Error
                    : issue.Severity == GameContentSetValidationSeverity.Warning
                        ? GameContentAuthoringValidationSeverity.Warning
                        : GameContentAuthoringValidationSeverity.Info;
                issues.Add(new GameContentAuthoringValidationIssue(severity, issue.Path, issue.Message));
            }

            return issues;
        }
    }
}
