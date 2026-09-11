using System;
using System.IO;
using Deucarian.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Controls = Deucarian.Editor.DeucarianEditorWorkspaceControls;

namespace Deucarian.TemplateGameIdleAutoDefense.Editor
{
    public sealed class IdleAutoDefenseTemplateSetupWizardWindow : EditorWindow
    {
        private readonly IdleAutoDefenseTemplateSetupRequest request = new IdleAutoDefenseTemplateSetupRequest { OpenCreatedScene = true };
        private DeucarianEditorPageSession navigation;
        private DeucarianEditorWorkspace workspace;
        private DeucarianEditorWorkspaceForm form;
        private DeucarianEditorSteps steps;
        private Label result;

        public static void Open() => DeucarianEditorWindowPages.ShowStandalone<IdleAutoDefenseTemplateSetupWizardWindow>(
            "Idle Auto Defense Setup", new Vector2(560, 500));
        public static IDeucarianEditorPage CreatePage() =>
            DeucarianEditorWindowPages.Create<IdleAutoDefenseTemplateSetupWizardWindow>((window, root) => window.Build(root));
        private void CreateGUI()
        {
            navigation?.Dispose();
            navigation = new DeucarianEditorPageSession(this, "idle-setup-home", _ => { });
            navigation.Navigate("deucarian.template.idle-auto-defense");
        }
        private void OnDisable() { navigation?.Dispose(); navigation = null; workspace?.Dispose(); workspace = null; }
        private void Build(VisualElement root)
        {
            workspace = new DeucarianEditorWorkspace(root, Application.productName);
            workspace.Title.text = "Idle Auto Defense Setup";
            workspace.Subtitle.text = "Start from a working game setup.";
            DeucarianEditorWorkspaceNavigation.Populate(workspace, "deucarian.template.idle-auto-defense");
            var scroll = Controls.Scroll("idle-setup"); workspace.Content.Add(scroll);
            steps = new DeucarianEditorSteps("Choose content", "Create scene", "Validate"); scroll.Add(steps.Root);
            var feature = new DeucarianEditorFeatureSection("idle-setup-form", "Game setup",
                "Configure the content and scene for your game.", DeucarianEditorIconIds.Package); scroll.Add(feature.Root);
            form = new DeucarianEditorWorkspaceForm(feature.Details);
            form.Choice("idle-pack", "Content pack", new[] { "Basic Idle Auto Defense", "Scrap Frontier", "Both packs" },
                () => (int)request.PackSelection, value => request.PackSelection = (IdleAutoDefenseTemplatePackSelection)value);
            form.Text("idle-prefix", "Game prefix", () => request.GamePrefix, value => request.GamePrefix = value);
            var folder = form.Text("idle-folder", "Save folder", () => request.TargetRootAssetPath, value => request.TargetRootAssetPath = value);
            folder.parent.Add(Controls.IconButton("Choose folder", DeucarianEditorIconIds.Folder, ChooseFolder));
            feature.Details.Add(Controls.Divider());
            feature.Actions.Add(Controls.Button("Create game", CreateGame, true));
            feature.Actions.Add(Controls.Button("Validate existing", () =>
            {
                result.text = IdleAutoDefenseAuthoredContentValidationMenu.BuildReport();
                Controls.Show(result, true); steps.SetCurrent(2);
            }));
            feature.UseFormLayout();
            var advanced = form.Section("Advanced settings", true);
            advanced.Text("idle-content-root", "Content root", () => request.ContentRootAssetPath, value => request.ContentRootAssetPath = value);
            advanced.Text("idle-namespace", "Namespace", () => request.GameNamespace, value => request.GameNamespace = value);
            advanced.Toggle("idle-open-scene", "Open created scene", () => request.OpenCreatedScene, value => request.OpenCreatedScene = value);
            advanced.Toggle("idle-repair", "Repair missing content", () => request.RepairMissingContent, value => request.RepairMissingContent = value);
            advanced.Toggle("idle-overwrite", "Allow overwrite", () => request.AllowOverwrite, value => request.AllowOverwrite = value);
            advanced.Note(() => "Each pack includes its authored scene, gameplay, presentation and audio. Scene names come from the template.");
            result = Controls.Label(string.Empty, "dw-note"); scroll.Add(result); Controls.Show(result, false);
        }
        private void ChooseFolder()
        {
            string selected = EditorUtility.OpenFolderPanel("Target folder under Assets", Application.dataPath, string.Empty);
            if (string.IsNullOrEmpty(selected)) return;
            string assets = Path.GetFullPath(Application.dataPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string full = Path.GetFullPath(selected);
            if (!full.StartsWith(assets + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("Idle Auto Defense Setup", "Choose a product folder inside Assets.", "OK"); return;
            }
            request.TargetRootAssetPath = "Assets/" + full.Substring(assets.Length + 1).Replace('\\', '/');
            form.Refresh();
        }
        private void CreateGame()
        {
            if (request.AllowOverwrite && !EditorUtility.DisplayDialog("Overwrite template files?",
                "Existing files in the selected target and content folders may be replaced. Continue?", "Create and overwrite", "Cancel")) return;
            if (request.OpenCreatedScene && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            steps.SetCurrent(1);
            var created = IdleAutoDefenseTemplateSetupService.CreateGameFromTemplate(request);
            result.text = created.CreateSummary(); Controls.Show(result, true);
            steps.SetCurrent(created.Succeeded ? 2 : 0);
        }
    }
}
