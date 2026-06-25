using System.Globalization;
using Deucarian.GameContentAuthoring.Editor;
using UnityEditor;
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
}
