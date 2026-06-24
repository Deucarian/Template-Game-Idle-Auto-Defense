using System;
using System.IO;
using Deucarian.TemplateGameIdleAutoDefense;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense.Editor
{
    public static class IdleAutoDefenseTemplateMenu
    {
        private const string MenuRoot = "Tools/Deucarian/Templates/Idle Auto Defense/";
        private const string ImportedSampleRoot = "Assets/Samples/Deucarian Template Game - Idle Auto Defense";
        private const string ImportedSampleSceneSuffix = "/Basic Idle Auto Defense Game/Scenes/BasicIdleAutoDefenseGame.unity";
        private const string SampleSaveFolderName = "IdleAutoDefenseTemplateSample";

        [MenuItem(MenuRoot + "Create Game From Template", priority = 5)]
        public static void CreateGameFromTemplate()
        {
            IdleAutoDefenseTemplateSetupWizardWindow.Open();
        }

        [MenuItem(MenuRoot + "Open Starter Scene", priority = 10)]
        public static void OpenStarterScene()
        {
            if (!TryFindImportedStarterScene(out string scenePath))
            {
                EditorUtility.DisplayDialog(
                    "Idle Auto Defense Template",
                    "Import the Basic Idle Auto Defense Game sample first, then run this command again.",
                    "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(scenePath);
        }

        [MenuItem(MenuRoot + "Reset Sample Save", priority = 20)]
        public static void ResetSampleSave()
        {
            int deletedFiles = ResetSampleSaveFiles();
            EditorUtility.DisplayDialog(
                "Idle Auto Defense Template",
                deletedFiles > 0
                    ? "Deleted " + deletedFiles + " sample save file(s)."
                    : "No Idle Auto Defense sample save files were found.",
                "OK");
        }

        [MenuItem(MenuRoot + "Open Template Docs", priority = 30)]
        public static void OpenTemplateDocs()
        {
            if (!TryFindTemplateDocs(out string docsPath))
            {
                EditorUtility.DisplayDialog(
                    "Idle Auto Defense Template",
                    "Could not find the template documentation in the installed package.",
                    "OK");
                return;
            }

            Application.OpenURL(new Uri(docsPath).AbsoluteUri);
        }

        internal static bool TryFindImportedStarterScene(out string scenePath)
        {
            scenePath = string.Empty;
            if (!AssetDatabase.IsValidFolder("Assets/Samples")) return false;

            string[] sceneGuids = AssetDatabase.FindAssets("BasicIdleAutoDefenseGame t:Scene", new[] { "Assets/Samples" });
            for (int i = 0; i < sceneGuids.Length; i++)
            {
                string candidate = AssetDatabase.GUIDToAssetPath(sceneGuids[i]).Replace('\\', '/');
                if (candidate.StartsWith(ImportedSampleRoot + "/", StringComparison.OrdinalIgnoreCase) &&
                    candidate.EndsWith(ImportedSampleSceneSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    scenePath = candidate;
                    return true;
                }
            }

            return false;
        }

        internal static int ResetSampleSaveFiles()
        {
            string directory = Path.Combine(Application.persistentDataPath, "Deucarian", SampleSaveFolderName);
            if (!Directory.Exists(directory)) return 0;

            int fileCount = Directory.GetFiles(directory, "*", SearchOption.AllDirectories).Length;
            Directory.Delete(directory, true);
            return Math.Max(fileCount, 1);
        }

        internal static bool TryFindTemplateDocs(out string docsPath)
        {
            docsPath = string.Empty;
            UnityEditor.PackageManager.PackageInfo packageInfo =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(IdleAutoDefenseTemplateController).Assembly);
            if (packageInfo == null || string.IsNullOrWhiteSpace(packageInfo.resolvedPath)) return false;

            string quickStart = Path.Combine(packageInfo.resolvedPath, "Documentation~", "quick-start.md");
            if (File.Exists(quickStart))
            {
                docsPath = quickStart;
                return true;
            }

            string readme = Path.Combine(packageInfo.resolvedPath, "README.md");
            if (!File.Exists(readme)) return false;
            docsPath = readme;
            return true;
        }
    }
}
