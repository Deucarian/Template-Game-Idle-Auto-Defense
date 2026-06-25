using System.Collections.Generic;
using Deucarian.GameContentAuthoring.Editor;
using UnityEditor;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense.Editor
{
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
}
