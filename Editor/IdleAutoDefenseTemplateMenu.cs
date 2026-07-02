using System;
using System.IO;
using Deucarian.TemplateGameIdleAutoDefense;
using UnityEditor;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense.Editor
{
    public static class IdleAutoDefenseTemplateMenu
    {
        private const string MenuRoot = "Tools/Deucarian/Templates/Idle Auto Defense/";

        [MenuItem(MenuRoot + "Create Playable Game", priority = 5)]
        public static void CreateGameFromTemplate()
        {
            IdleAutoDefenseTemplateSetupWizardWindow.Open();
        }

        [MenuItem(MenuRoot + "Open Template Docs", priority = 20)]
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
