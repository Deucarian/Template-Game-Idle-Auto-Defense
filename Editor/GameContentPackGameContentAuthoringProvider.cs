using System;
using System.Collections.Generic;
using System.Globalization;
using Deucarian.GameContentAuthoring.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense.Editor
{
    [InitializeOnLoad]
    internal static class GameContentPackAuthoringProviderRegistration
    {
        static GameContentPackAuthoringProviderRegistration()
        {
            GameContentAuthoringProviderRegistry.Register(new GameContentPackAuthoringProvider());
        }
    }

    internal sealed class GameContentPackAuthoringProvider : IGameContentAuthoringProvider
    {
        private readonly GameContentPackAuthoringState _state = new GameContentPackAuthoringState();
        private readonly GameContentPackSceneSetupState _setup = new GameContentPackSceneSetupState();
        private readonly GameContentPackPreviewController _preview = new GameContentPackPreviewController();

        public string ProviderId => "com.deucarian.template.idle-auto-defense.content-pack";
        public string DisplayName => "Content Pack";
        public string Description => "Package playable Game / Run Content Sets and apply one to an idle auto-defense scene.";
        public int SortOrder => 175;
        public bool Enabled => true;
        public void OnSelected() { }
        public void DrawPreview(GameContentAuthoringPreviewContext context) { _preview.Draw(context, _state, _setup); }
        public void StopPreview() { _preview.Stop(); }

        public void Draw(GameContentAuthoringContext context)
        {
            GameContentPackAsset preview = GameContentPackAssetCreator.BuildTransient(_state);
            GameContentAuthoringValidationResult report;
            try
            {
                report = GameContentPackAssetCreator.ValidateForCreation(_state, preview);
            }
            finally
            {
                GameContentPackAssetCreator.DestroyTransient(preview);
            }

            context.DrawSection("Pack Identity", () =>
            {
                _state.PackId = EditorGUILayout.TextField("Stable ID", _state.PackId);
                _state.DisplayName = EditorGUILayout.TextField("Display Name", _state.DisplayName);
                _state.Description = EditorGUILayout.TextField("Description", _state.Description);
                _state.Version = EditorGUILayout.TextField("Version", _state.Version);
                _state.Author = EditorGUILayout.TextField("Author", _state.Author);
                _state.Icon = (Sprite)EditorGUILayout.ObjectField("Icon", _state.Icon, typeof(Sprite), false);
                _state.Banner = (Texture2D)EditorGUILayout.ObjectField("Banner", _state.Banner, typeof(Texture2D), false);
                _state.TagsCsv = EditorGUILayout.TextField("Tags", _state.TagsCsv);
                _state.OutputRoot = context.DrawOutputRootField(_state.OutputRoot);
            });

            context.DrawSection("Included Game / Run Content Sets", () =>
            {
                _state.DefaultContentSet = (GameContentSetAsset)EditorGUILayout.ObjectField("Default Content Set", _state.DefaultContentSet, typeof(GameContentSetAsset), false);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _state.SelectedContentSet = (GameContentSetAsset)EditorGUILayout.ObjectField("Create From Selection", _state.SelectedContentSet, typeof(GameContentSetAsset), false);
                    if (context.DrawSecondaryButton("Use Set", _state.SelectedContentSet != null, GUILayout.Width(72f), GUILayout.Height(22f)))
                        GameContentPackAssetCreator.UseSelectedContentSet(_state);
                }

                DrawContentSetList(context, _state);
            });

            context.DrawSection("Compatibility", () =>
            {
                _state.RequiredPackagesCsv = EditorGUILayout.TextField("Required Packages", _state.RequiredPackagesCsv);
                _state.MinimumVersionsCsv = EditorGUILayout.TextField("Minimum Versions", _state.MinimumVersionsCsv);
                _state.CompatibilityNotes = EditorGUILayout.TextField("Compatibility Notes", _state.CompatibilityNotes);
                EditorGUILayout.LabelField("Minimum versions may be left empty when the pack is tied to the installed template package set.", context.MutedStyle);
            });

            context.DrawSection("Preview", () =>
            {
                foreach (string line in GameContentPackAssetCreator.GetPreviewLines(_state))
                    EditorGUILayout.LabelField(line, context.MutedStyle);
                GUILayout.Space(6f);
                context.DrawValidation(report, "Ready to create one Content Pack root asset.");
                GUILayout.Space(8f);
                if (context.DrawCreateButton("Create Content Pack", report.IsValid))
                {
                    GameContentCreationResult result = GameContentPackAssetCreator.CreateAssets(_state);
                    if (result.Succeeded && result.CreatedRoot is GameContentPackAsset createdPack)
                    {
                        _setup.ContentPack = createdPack;
                        _setup.SelectedContentSet = createdPack.DefaultContentSet;
                    }

                    context.SetCreationResult(result);
                }

                context.DrawCreationResult();
            });

            context.DrawSection("One-Click Scene Setup", () =>
            {
                DrawSceneSetup(context);
            });
        }

        private void DrawSceneSetup(GameContentAuthoringContext context)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _setup.Controller = (IdleAutoDefenseTemplateController)EditorGUILayout.ObjectField("Template Controller", _setup.Controller, typeof(IdleAutoDefenseTemplateController), true);
                if (context.DrawSecondaryButton("Find", true, GUILayout.Width(56f), GUILayout.Height(22f)))
                    _setup.Controller = GameContentPackSceneSetupUtility.FindControllerInOpenScenes();
            }

            _setup.ContentPack = (GameContentPackAsset)EditorGUILayout.ObjectField("Content Pack", _setup.ContentPack, typeof(GameContentPackAsset), false);
            _setup.SelectedContentSet = (GameContentSetAsset)EditorGUILayout.ObjectField("Selected Content Set", _setup.SelectedContentSet, typeof(GameContentSetAsset), false);
            GameContentAuthoringValidationResult validation = GameContentPackSceneSetupUtility.Validate(_setup.Controller, _setup.ContentPack, _setup.SelectedContentSet);
            context.DrawValidation(validation, "Ready to apply this content pack to the selected scene controller.");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (context.DrawSecondaryButton("Preview Setup", validation.IsValid, GUILayout.Width(112f)))
                    _setup.LastMessage = GameContentPackSceneSetupUtility.CreatePreviewSummary(_setup.ContentPack, _setup.SelectedContentSet);
                if (context.DrawSecondaryButton("Apply To Scene", validation.IsValid, GUILayout.Width(112f)))
                {
                    GameContentCreationResult result = GameContentPackSceneSetupUtility.Apply(_setup.Controller, _setup.ContentPack, _setup.SelectedContentSet);
                    _setup.LastMessage = result.Message;
                    context.SetCreationResult(result);
                }
            }

            if (!string.IsNullOrWhiteSpace(_setup.LastMessage))
                EditorGUILayout.LabelField(_setup.LastMessage, context.MutedStyle);
        }

        private static void DrawContentSetList(GameContentAuthoringContext context, GameContentPackAuthoringState state)
        {
            context.DrawInlineCard(() =>
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Included Sets", context.SectionTitleStyle);
                    GUILayout.FlexibleSpace();
                    if (context.DrawSecondaryButton("Add Set", true, GUILayout.Width(82f), GUILayout.Height(22f)))
                        state.ContentSets.Add(null);
                }

                if (state.ContentSets.Count == 0)
                    EditorGUILayout.LabelField("None assigned.", context.MutedStyle);

                for (int i = 0; i < state.ContentSets.Count; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        state.ContentSets[i] = (GameContentSetAsset)EditorGUILayout.ObjectField("Set " + (i + 1).ToString(CultureInfo.InvariantCulture), state.ContentSets[i], typeof(GameContentSetAsset), false);
                        if (context.DrawSecondaryButton("Default", state.ContentSets[i] != null, GUILayout.Width(70f), GUILayout.Height(22f)))
                            state.DefaultContentSet = state.ContentSets[i];
                        if (context.DrawSecondaryButton("Remove", true, GUILayout.Width(70f), GUILayout.Height(22f)))
                        {
                            GameContentSetAsset removed = state.ContentSets[i];
                            state.ContentSets.RemoveAt(i);
                            if (state.DefaultContentSet == removed)
                                state.DefaultContentSet = state.ContentSets.Count > 0 ? state.ContentSets[0] : null;
                            i--;
                        }
                    }
                }
            });
        }
    }

    public sealed class GameContentPackAuthoringState
    {
        public string PackId = "contentpack.example.basic-idle-auto-defense";
        public string DisplayName = "Basic Idle Auto Defense Pack";
        public string Description = "Packages playable authored idle auto-defense content for one-click scene setup.";
        public string Version = "0.1.0";
        public string Author = "Deucarian";
        public Sprite Icon;
        public Texture2D Banner;
        public GameContentSetAsset SelectedContentSet;
        public readonly List<GameContentSetAsset> ContentSets = new List<GameContentSetAsset>();
        public GameContentSetAsset DefaultContentSet;
        public string RequiredPackagesCsv = "com.deucarian.template.game.idle-auto-defense, com.deucarian.attacks, com.deucarian.weapon-systems, com.deucarian.run-upgrades, com.deucarian.game-content-authoring";
        public string MinimumVersionsCsv = string.Empty;
        public string CompatibilityNotes = "Validated with the Idle Auto Defense template package set.";
        public string TagsCsv = "template, content-pack, idle-auto-defense";
        public string OutputRoot = "Assets/GameContent/ContentPacks";
    }

    public sealed class GameContentPackSceneSetupState
    {
        public IdleAutoDefenseTemplateController Controller;
        public GameContentPackAsset ContentPack;
        public GameContentSetAsset SelectedContentSet;
        public string LastMessage = string.Empty;
    }

    internal sealed class GameContentPackPreviewController
    {
        private string _status = "Preview idle";

        public void Draw(GameContentAuthoringPreviewContext context, GameContentPackAuthoringState state, GameContentPackSceneSetupState setup)
        {
            if (context == null) return;
            context.SetStatus(_status);

            GameContentPackAsset preview = GameContentPackAssetCreator.BuildTransient(state);
            GameContentPackValidationReport report;
            GameContentPackDependencySummary dependencies;
            try
            {
                report = GameContentPackValidator.Validate(preview);
                dependencies = GameContentPackValidator.CollectDependencies(preview);
            }
            finally
            {
                GameContentPackAssetCreator.DestroyTransient(preview);
            }

            context.DrawCard("Content Pack Preview", () =>
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (context.DrawPrimaryButton("Preview Pack", true, GUILayout.Height(26f)))
                        SetStatus(context, GameContentPackAuthoringPreviewSummaries.PreviewStatus(state, report));
                    if (context.DrawSecondaryButton("Stop Preview", true, GUILayout.Width(104f), GUILayout.Height(26f)))
                        Stop(context);
                }

                context.DrawStatus(_status);
            });

            context.DrawCard("Ready To Install", () =>
            {
                context.DrawSummaryRows(GameContentPackAuthoringPreviewSummaries.BuildReadinessRows(state, report));
            });

            context.DrawCard("Pack Summary", () =>
            {
                context.DrawSummaryRows(GameContentPackAuthoringPreviewSummaries.BuildSummaryRows(state, dependencies));
            });

            context.DrawCard("Dependency Graph", () =>
            {
                context.DrawSummaryRows(GameContentPackAuthoringPreviewSummaries.BuildDependencyRows(state));
            });

            context.DrawCard("Scene Setup", () =>
            {
                context.DrawSummaryRows(GameContentPackAuthoringPreviewSummaries.BuildSceneSetupRows(setup));
            });

            context.DrawObjectPreview(state == null ? null : state.Banner, "Banner", "Optional banner/reference image is not assigned.");
            context.DrawWarnings(GameContentPackAuthoringPreviewSummaries.BuildWarnings(report));
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

    internal static class GameContentPackAssetCreator
    {
        private const string DefaultRoot = "Assets/GameContent/ContentPacks";

        public static GameContentPackAsset BuildTransient(GameContentPackAuthoringState state)
        {
            GameContentPackAsset asset = ScriptableObject.CreateInstance<GameContentPackAsset>();
            asset.hideFlags = HideFlags.HideAndDontSave;
            asset.Configure(
                state.PackId,
                state.DisplayName,
                state.Description,
                state.Version,
                state.Author,
                state.Icon,
                state.Banner,
                state.ContentSets,
                state.DefaultContentSet,
                GameContentAuthoringEditorAssets.SplitCsv(state.RequiredPackagesCsv),
                GameContentAuthoringEditorAssets.SplitCsv(state.MinimumVersionsCsv),
                state.CompatibilityNotes,
                GameContentAuthoringEditorAssets.SplitCsv(state.TagsCsv));
            return asset;
        }

        public static void UseSelectedContentSet(GameContentPackAuthoringState state)
        {
            if (state == null || state.SelectedContentSet == null) return;
            if (!state.ContentSets.Contains(state.SelectedContentSet))
                state.ContentSets.Add(state.SelectedContentSet);
            state.DefaultContentSet ??= state.SelectedContentSet;
            if (!string.IsNullOrWhiteSpace(state.SelectedContentSet.DisplayName))
                state.DisplayName = state.SelectedContentSet.DisplayName.Replace("Content Set", "Pack");
            if (!string.IsNullOrWhiteSpace(state.SelectedContentSet.Id))
                state.PackId = "contentpack." + state.SelectedContentSet.Id.Replace("contentset.", string.Empty);
        }

        public static GameContentAuthoringValidationResult ValidateForCreation(GameContentPackAuthoringState state, GameContentPackAsset asset)
        {
            var issues = ToSharedIssues(GameContentPackValidator.Validate(asset));
            string folder = GetContentPackFolder(state);
            string rootPath = GetRootPath(state);
            GameContentAuthoringEditorAssets.AddPathIssues(issues, state.OutputRoot, DefaultRoot, folder, rootPath, "Content Pack", "OutputRoot");
            if (GameContentAuthoringEditorAssets.HasDuplicateId<GameContentPackAsset>(state.PackId, item => item.Id))
                issues.Add(GameContentAuthoringValidationIssue.Error("ContentPack.Id", "Content pack IDs must be unique. Rename this pack or edit the existing asset instead of creating another."));
            return new GameContentAuthoringValidationResult(issues);
        }

        public static IReadOnlyList<string> GetPreviewLines(GameContentPackAuthoringState state)
        {
            return new[]
            {
                "Folder: " + GetContentPackFolder(state),
                "Root asset: " + GetFileStem(state) + "_ContentPack.asset",
                "References: one or more Game / Run Content Sets, plus compatibility metadata.",
                "Setup: Apply writes the pack and selected content set onto a template scene controller.",
                "Preview: validation and dependency scans do not dirty the active scene."
            };
        }

        public static GameContentCreationResult CreateAssets(GameContentPackAuthoringState state)
        {
            GameContentPackAsset preview = BuildTransient(state);
            GameContentAuthoringValidationResult report;
            try
            {
                report = ValidateForCreation(state, preview);
                if (!report.IsValid)
                    return new GameContentCreationResult(false, "Fix validation errors before creating the content pack.", null);
            }
            finally
            {
                DestroyTransient(preview);
            }

            string folder = GetContentPackFolder(state);
            string rootPath = GetRootPath(state);
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(rootPath) != null)
                return new GameContentCreationResult(false, "Asset already exists: " + rootPath, null);
            if (AssetDatabase.IsValidFolder(folder) && GameContentAuthoringEditorPaths.FolderContainsAssets(folder))
            {
                bool confirmed = GameContentAuthoringEditorAssets.ConfirmExistingFolder(folder, "Content Pack");
                if (!confirmed)
                    return new GameContentCreationResult(false, "Creation canceled before writing into existing folder.", null);
            }

            folder = GameContentAuthoringEditorPaths.EnsureFolder(folder, DefaultRoot);
            rootPath = folder + "/" + GetFileStem(state) + "_ContentPack.asset";
            GameContentPackAsset root = BuildTransient(state);
            root.hideFlags = HideFlags.None;
            AssetDatabase.CreateAsset(root, rootPath);
            EditorUtility.SetDirty(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return new GameContentCreationResult(true, "Created content pack at " + rootPath, AssetDatabase.LoadAssetAtPath<GameContentPackAsset>(rootPath));
        }

        public static void DestroyTransient(GameContentPackAsset asset)
        {
            if (asset == null || asset.hideFlags != HideFlags.HideAndDontSave) return;
            GameContentAuthoringEditorAssets.DestroyTransientObject(asset);
        }

        public static string GetContentPackFolder(GameContentPackAuthoringState state)
        {
            string root = GameContentAuthoringEditorPaths.NormalizeAssetFolderPath(state.OutputRoot, DefaultRoot);
            return root.TrimEnd('/') + "/" + GetFileStem(state);
        }

        private static string GetRootPath(GameContentPackAuthoringState state)
        {
            return GetContentPackFolder(state) + "/" + GetFileStem(state) + "_ContentPack.asset";
        }

        private static string GetFileStem(GameContentPackAuthoringState state)
        {
            return GameContentAuthoringEditorPaths.SanitizePathSegment(state.PackId, "NewContentPack");
        }

        private static List<GameContentAuthoringValidationIssue> ToSharedIssues(GameContentPackValidationReport report)
        {
            var issues = new List<GameContentAuthoringValidationIssue>();
            if (report == null) return issues;
            for (int i = 0; i < report.Issues.Count; i++)
            {
                GameContentPackValidationIssue issue = report.Issues[i];
                GameContentAuthoringValidationSeverity severity = issue.Severity == GameContentPackValidationSeverity.Error
                    ? GameContentAuthoringValidationSeverity.Error
                    : issue.Severity == GameContentPackValidationSeverity.Warning
                        ? GameContentAuthoringValidationSeverity.Warning
                        : GameContentAuthoringValidationSeverity.Info;
                issues.Add(new GameContentAuthoringValidationIssue(severity, issue.Path, issue.Message));
            }

            return issues;
        }
    }

    public static class GameContentPackSceneSetupUtility
    {
        public static IdleAutoDefenseTemplateController FindControllerInOpenScenes()
        {
            IdleAutoDefenseTemplateController[] controllers = Resources.FindObjectsOfTypeAll<IdleAutoDefenseTemplateController>();
            for (int i = 0; i < controllers.Length; i++)
            {
                IdleAutoDefenseTemplateController controller = controllers[i];
                if (controller == null || EditorUtility.IsPersistent(controller)) continue;
                if (controller.gameObject == null || !controller.gameObject.scene.IsValid() || !controller.gameObject.scene.isLoaded) continue;
                return controller;
            }

            return null;
        }

        public static GameContentAuthoringValidationResult Validate(IdleAutoDefenseTemplateController controller, GameContentPackAsset contentPack, GameContentSetAsset selectedContentSet)
        {
            var issues = new List<GameContentAuthoringValidationIssue>();
            if (controller == null)
                issues.Add(GameContentAuthoringValidationIssue.Error("Scene.Controller", "Choose an IdleAutoDefenseTemplateController in the open scene."));
            if (contentPack == null)
            {
                issues.Add(GameContentAuthoringValidationIssue.Error("ContentPack", "Choose a Content Pack asset before applying scene setup."));
                return new GameContentAuthoringValidationResult(issues);
            }

            GameContentPackResolution resolution = GameContentPackValidator.Resolve(contentPack, selectedContentSet);
            for (int i = 0; i < resolution.PackReport.Issues.Count; i++)
            {
                GameContentPackValidationIssue issue = resolution.PackReport.Issues[i];
                issues.Add(new GameContentAuthoringValidationIssue(ToSharedSeverity(issue.Severity), issue.Path, issue.Message));
            }

            if (resolution.ContentSetResolution != null)
            {
                for (int i = 0; i < resolution.ContentSetResolution.Report.Issues.Count; i++)
                {
                    GameContentSetValidationIssue issue = resolution.ContentSetResolution.Report.Issues[i];
                    GameContentAuthoringValidationSeverity severity = issue.IsError ? GameContentAuthoringValidationSeverity.Error : GameContentAuthoringValidationSeverity.Warning;
                    issues.Add(new GameContentAuthoringValidationIssue(severity, "ContentSet." + issue.Path, issue.Message));
                }
            }

            return new GameContentAuthoringValidationResult(issues);
        }

        public static string CreatePreviewSummary(GameContentPackAsset contentPack, GameContentSetAsset selectedContentSet)
        {
            if (contentPack == null) return "No content pack selected.";
            GameContentSetAsset contentSet = selectedContentSet != null ? selectedContentSet : contentPack.DefaultContentSet;
            return "Preview setup: " + contentPack.DisplayName + " will assign " + (contentSet == null ? "the pack default content set" : contentSet.DisplayName) + ". Scene is unchanged.";
        }

        public static GameContentCreationResult Apply(IdleAutoDefenseTemplateController controller, GameContentPackAsset contentPack, GameContentSetAsset selectedContentSet)
        {
            GameContentAuthoringValidationResult validation = Validate(controller, contentPack, selectedContentSet);
            if (!validation.IsValid)
                return new GameContentCreationResult(false, "Fix scene setup validation before applying the content pack.", controller);

            GameContentSetAsset contentSet = selectedContentSet != null ? selectedContentSet : contentPack.DefaultContentSet;
            Undo.RecordObject(controller, "Apply Deucarian Content Pack");
            SerializedObject serialized = new SerializedObject(controller);
            SerializedProperty packProperty = serialized.FindProperty("_contentPack");
            SerializedProperty setProperty = serialized.FindProperty("_contentSet");
            if (packProperty == null || setProperty == null)
                return new GameContentCreationResult(false, "Template controller fields are unavailable; package scripts may be out of date.", controller);

            packProperty.objectReferenceValue = contentPack;
            setProperty.objectReferenceValue = contentSet;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);
            if (controller.gameObject.scene.IsValid() && controller.gameObject.scene.isLoaded)
                EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            return new GameContentCreationResult(true, "Applied content pack to scene controller: " + contentPack.DisplayName, controller);
        }

        private static GameContentAuthoringValidationSeverity ToSharedSeverity(GameContentPackValidationSeverity severity)
        {
            if (severity == GameContentPackValidationSeverity.Error) return GameContentAuthoringValidationSeverity.Error;
            if (severity == GameContentPackValidationSeverity.Info) return GameContentAuthoringValidationSeverity.Info;
            return GameContentAuthoringValidationSeverity.Warning;
        }
    }
}
