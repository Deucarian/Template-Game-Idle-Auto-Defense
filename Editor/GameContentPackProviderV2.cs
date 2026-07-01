using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Deucarian.Attacks.Authoring;
using Deucarian.Editor;
using Deucarian.GameContentAuthoring.Editor;
using Deucarian.RunUpgrades.Authoring;
using Deucarian.WeaponSystems.Authoring;
using UnityEditor;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense.Editor
{
    internal sealed class GameContentPackProviderV2State
    {
        public string SearchText = string.Empty;
        public bool Creating;
        public int DetailPage;
        public int WizardStep;
        public Vector2 ListScroll;
        public Vector2 DetailScroll;
        public Vector2 PreviewScroll;
        public bool PreviewLoop = true;
        public bool PreviewPlaying = true;
        public float PreviewSpeed = 1f;
        public GameContentAuthoringActionPreviewRenderMode PreviewRenderMode = GameContentAuthoringActionPreviewRenderMode.Game;
        public double PreviewStartTime;
        public float PausedNormalizedTime = 0.5f;
        public string ActivePreviewKey = string.Empty;
        public string PreviewStatus = "Preview idle";
        public GameContentPackAuthoringState EditingState;
        public GameContentAuthoringObjectEditorContext EditingContext;
        public GameContentCreationResult LastEditResult;
        public readonly GameContentPackSceneSetupState SceneSetup = new GameContentPackSceneSetupState();

        public void StopPreview()
        {
            PreviewPlaying = false;
            PreviewStartTime = 0d;
            PausedNormalizedTime = 0.5f;
            PreviewStatus = "Preview stopped";
        }

        public void BeginCreate()
        {
            Creating = true;
            WizardStep = 0;
            DetailPage = 0;
            DetailScroll = Vector2.zero;
            ClearEditingState();
            SceneSetup.ContentPack = null;
            SceneSetup.SelectedContentSet = null;
            SceneSetup.LastMessage = string.Empty;
            PreviewStatus = "Previewing draft content pack";
        }

        public void ResetProviderSession()
        {
            Creating = false;
            DetailPage = 0;
            WizardStep = 0;
            ListScroll = Vector2.zero;
            DetailScroll = Vector2.zero;
            PreviewScroll = Vector2.zero;
            ActivePreviewKey = string.Empty;
            PreviewStatus = "Preview idle";
            SceneSetup.ContentPack = null;
            SceneSetup.SelectedContentSet = null;
            SceneSetup.LastMessage = string.Empty;
            ClearEditingState();
        }

        public void SetPreviewSource(string key)
        {
            key = key ?? string.Empty;
            if (string.Equals(ActivePreviewKey, key, StringComparison.Ordinal))
                return;

            ActivePreviewKey = key;
            PreviewPlaying = true;
            PreviewStartTime = EditorApplication.timeSinceStartup;
            PausedNormalizedTime = 0f;
            PreviewStatus = "Previewing";
        }

        public void ClearEditingState()
        {
            EditingState = null;
            EditingContext = null;
            LastEditResult = null;
        }
    }

    internal sealed class GameContentPackProviderV2View
    {
        private static readonly string[] DetailPages =
        {
            "Overview",
            "Included Sets",
            "Default Setup",
            "Compatibility",
            "One-Click Setup",
            "Preview / Readiness",
            "References",
            "Advanced"
        };

        private static readonly string[] WizardSteps =
        {
            "Identity",
            "Included Sets",
            "Default Setup",
            "Compatibility",
            "One-Click Setup",
            "Review"
        };

        public void Draw(
            GameContentAuthoringSurfaceContext context,
            GameContentPackAuthoringState draft,
            GameContentPackProviderV2State state)
        {
            if (context == null || draft == null || state == null)
                return;

            IReadOnlyList<GameContentPackProviderV2ListItem> items = GameContentPackProviderV2ListItem.Build(context.AuthoredItems);
            EnsureDefaultMode(context, state, items);
            EnsureEditingState(context, state);
            TrackPreviewSource(context, state);

            GameContentAuthoringWorkbench.Draw(
                context,
                () => DrawContentPackList(context, state, items),
                () => DrawDetailOrWizard(context, draft, state),
                () => DrawPreviewLab(context, draft, state));
        }

        private static void EnsureDefaultMode(GameContentAuthoringSurfaceContext context, GameContentPackProviderV2State state, IReadOnlyList<GameContentPackProviderV2ListItem> items)
        {
            if (items.Count == 0)
            {
                state.Creating = true;
                state.ClearEditingState();
                return;
            }

            if (!state.Creating && context.SelectedItem == null)
            {
                context.SelectItem(items[0].Source);
                context.RequestRepaint();
            }
        }

        private static void EnsureEditingState(GameContentAuthoringSurfaceContext context, GameContentPackProviderV2State state)
        {
            if (state.Creating || context.SelectedItem == null)
            {
                state.ClearEditingState();
                return;
            }

            GameContentPackAsset selected = context.SelectedItem.Asset as GameContentPackAsset;
            if (selected == null)
            {
                state.ClearEditingState();
                return;
            }

            if (state.EditingContext != null && string.Equals(state.EditingContext.Key, context.SelectedItem.Key, StringComparison.Ordinal) && state.EditingState != null)
                return;

            state.EditingState = FromContentPackAsset(selected);
            string fingerprint = BuildStateFingerprint(state.EditingState);
            state.EditingContext = new GameContentAuthoringObjectEditorContext(context.SelectedItem, fingerprint);
            state.SceneSetup.ContentPack = selected;
            state.SceneSetup.SelectedContentSet = selected.DefaultContentSet;
            state.SceneSetup.LastMessage = string.Empty;
            state.LastEditResult = null;
        }

        private static void TrackPreviewSource(GameContentAuthoringSurfaceContext context, GameContentPackProviderV2State state)
        {
            string key = state.Creating
                ? "__draft_content_pack__"
                : context.SelectedItem == null
                    ? string.Empty
                    : context.SelectedItem.Key;
            state.SetPreviewSource(key);
        }

        private static void DrawContentPackList(GameContentAuthoringSurfaceContext context, GameContentPackProviderV2State state, IReadOnlyList<GameContentPackProviderV2ListItem> items)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Content Packs", DeucarianEditorStyles.SectionTitle);
                GUILayout.FlexibleSpace();
                if (DeucarianEditorMiniToolbar.Button("Refresh", true, GUILayout.Width(62f), GUILayout.Height(22f)))
                    context.RefreshLibrary();
            }

            state.SearchText = DeucarianEditorSearchField.Draw(state.SearchText, "Search content packs", GUILayout.ExpandWidth(true));
            if (DeucarianEditorButtons.Secondary("Create New", true, GUILayout.Height(24f)))
            {
                state.BeginCreate();
                context.ClearSelection();
                context.RequestRepaint();
            }

            GUILayout.Space(DeucarianEditorSpacing.Small);
            state.ListScroll = EditorGUILayout.BeginScrollView(state.ListScroll);
            int shown = 0;
            for (int i = 0; i < items.Count; i++)
            {
                GameContentPackProviderV2ListItem item = items[i];
                if (!item.Matches(state.SearchText))
                    continue;

                shown++;
                DrawContentPackCard(context, state, item);
            }

            if (shown == 0)
                EditorGUILayout.LabelField(items.Count == 0 ? "No authored content packs found." : "No content packs match the current search.", DeucarianEditorStyles.MutedLabel);
            EditorGUILayout.EndScrollView();
        }

        private static void DrawContentPackCard(GameContentAuthoringSurfaceContext context, GameContentPackProviderV2State state, GameContentPackProviderV2ListItem item)
        {
            bool selected = !state.Creating && context.IsSelected(item.Source);
            var chips = new[]
            {
                new DeucarianEditorStatusChip(item.ReadinessLabel, item.ReadinessStatus),
                new DeucarianEditorStatusChip(item.DefaultContentSetLabel, item.HasDefaultContentSet ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Error),
                new DeucarianEditorStatusChip(item.ContentSetCount.ToString(CultureInfo.InvariantCulture) + " set(s)", item.ContentSetCount > 0 ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Error),
                new DeucarianEditorStatusChip(item.DependencyCount.ToString(CultureInfo.InvariantCulture) + " assets", item.DependencyCount > 0 ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Warning),
                new DeucarianEditorStatusChip(item.CompatibilityLabel, item.CompatibilityStatus)
            };

            bool clicked = DeucarianEditorCompactObjectCard.Draw(
                item.DisplayName,
                item.StableId,
                selected,
                chips,
                () =>
                {
                    if (DeucarianEditorMiniToolbar.PingButton(item.Source.Asset))
                        GUI.FocusControl(null);
                },
                null,
                GUILayout.ExpandWidth(true));

            if (clicked && item.Source != null)
            {
                state.Creating = false;
                state.DetailScroll = Vector2.zero;
                context.SelectItem(item.Source);
                if (Event.current != null)
                    Event.current.Use();
            }
        }

        private static void DrawDetailOrWizard(GameContentAuthoringSurfaceContext context, GameContentPackAuthoringState draft, GameContentPackProviderV2State state)
        {
            state.DetailScroll = EditorGUILayout.BeginScrollView(state.DetailScroll);
            if (state.Creating)
                DrawCreateWizard(context, draft, state);
            else
                DrawSelectedContentPack(context, state);
            EditorGUILayout.EndScrollView();
        }

        private static void DrawSelectedContentPack(GameContentAuthoringSurfaceContext context, GameContentPackProviderV2State state)
        {
            GameContentPackAsset asset = context.SelectedItem == null ? null : context.SelectedItem.Asset as GameContentPackAsset;
            if (asset == null || state.EditingState == null || state.EditingContext == null)
            {
                EditorGUILayout.LabelField("Select a content pack to edit.", DeucarianEditorStyles.MutedLabel);
                return;
            }

            GameContentPackAuthoringState edit = state.EditingState;
            GameContentAuthoringValidationResult validation = GameContentPackAssetCreator.ValidateForUpdate(edit, asset);
            string fingerprint = BuildStateFingerprint(edit);
            state.EditingContext.Capture(fingerprint, validation);
            context.Authoring.SetValidation(validation);

            DrawHeader(edit.DisplayName, edit.PackId, BuildContentPackChips(edit, validation, context.SelectedItem));
            GameContentAuthoringCommand command = GameContentAuthoringCommandBar.Draw(
                GameContentAuthoringWorkbenchMode.Edit,
                validation.IsValid,
                state.EditingContext.IsDirty,
                "Save",
                state.LastEditResult == null ? state.EditingContext.StatusMessage : state.LastEditResult.Message);
            HandleEditCommand(context, state, asset, command);

            state.DetailPage = DeucarianEditorSegmentedControl.DrawPageChips(state.DetailPage, DetailPages);
            GUILayout.Space(DeucarianEditorSpacing.Small);
            switch (Mathf.Clamp(state.DetailPage, 0, DetailPages.Length - 1))
            {
                case 1:
                    DrawIncludedSets(edit);
                    break;
                case 2:
                    DrawDefaultSetup(edit);
                    break;
                case 3:
                    DrawCompatibility(context, edit);
                    break;
                case 4:
                    DrawOneClickSetup(context, edit, state, asset);
                    break;
                case 5:
                    DrawReadiness(edit, validation);
                    break;
                case 6:
                    DrawReferences(context.SelectedItem, edit);
                    break;
                case 7:
                    DrawAdvanced(context.SelectedItem, edit, asset);
                    break;
                default:
                    DrawOverview(context, edit, context.SelectedItem, false, validation);
                    break;
            }

            DrawValidationIssues(validation);
        }

        private static void HandleEditCommand(GameContentAuthoringSurfaceContext context, GameContentPackProviderV2State state, GameContentPackAsset asset, GameContentAuthoringCommand command)
        {
            if (command == GameContentAuthoringCommand.Revert)
            {
                state.EditingState = FromContentPackAsset(asset);
                string fingerprint = BuildStateFingerprint(state.EditingState);
                state.EditingContext.Accept(fingerprint, "Reverted");
                state.SceneSetup.ContentPack = asset;
                state.SceneSetup.SelectedContentSet = asset.DefaultContentSet;
                state.SceneSetup.LastMessage = string.Empty;
                state.LastEditResult = null;
                GUI.FocusControl(null);
                context.RequestRepaint();
                return;
            }

            if (command != GameContentAuthoringCommand.Save)
                return;

            state.LastEditResult = GameContentPackAssetCreator.UpdateExistingAsset(asset, state.EditingState);
            if (state.LastEditResult != null && state.LastEditResult.Succeeded)
            {
                state.EditingState = FromContentPackAsset(asset);
                string fingerprint = BuildStateFingerprint(state.EditingState);
                state.EditingContext.Accept(fingerprint, "Saved");
                state.SceneSetup.ContentPack = asset;
                state.SceneSetup.SelectedContentSet = asset.DefaultContentSet;
                context.RefreshLibrary();
            }
            else if (state.LastEditResult != null)
            {
                state.EditingContext.SetStatus(state.LastEditResult.Message);
            }

            GUI.FocusControl(null);
            context.RequestRepaint();
        }

        private static void DrawCreateWizard(GameContentAuthoringSurfaceContext context, GameContentPackAuthoringState draft, GameContentPackProviderV2State state)
        {
            GameContentAuthoringValidationResult validation = ValidateDraft(draft);
            DrawHeader("New Content Pack", draft.PackId, BuildContentPackChips(draft, validation, null));
            GameContentAuthoringCommand command = GameContentAuthoringCommandBar.Draw(GameContentAuthoringWorkbenchMode.Create, validation.IsValid, true, "Create");
            if (command == GameContentAuthoringCommand.Create)
            {
                GameContentCreationResult result = GameContentPackAssetCreator.CreateAssets(draft);
                context.Authoring.SetCreationResult(result);
                if (result != null && result.Succeeded)
                {
                    state.Creating = false;
                    if (result.CreatedRoot is GameContentPackAsset pack)
                    {
                        state.SceneSetup.ContentPack = pack;
                        state.SceneSetup.SelectedContentSet = pack.DefaultContentSet;
                    }

                    context.RefreshLibrary();
                }
            }

            state.WizardStep = DeucarianEditorWizardHeader.Draw(state.WizardStep, WizardSteps);
            GUILayout.Space(DeucarianEditorSpacing.Small);
            switch (Mathf.Clamp(state.WizardStep, 0, WizardSteps.Length - 1))
            {
                case 1:
                    DrawIncludedSets(draft);
                    break;
                case 2:
                    DrawDefaultSetup(draft);
                    break;
                case 3:
                    DrawCompatibility(context, draft);
                    break;
                case 4:
                    DrawOneClickSetup(context, draft, state, null);
                    break;
                case 5:
                    DrawReview(draft, validation);
                    break;
                default:
                    DrawOverview(context, draft, null, true, validation);
                    break;
            }

            DrawValidationIssues(validation);
        }

        private static void DrawHeader(string title, string subtitle, IReadOnlyList<DeucarianEditorStatusChip> chips)
        {
            EditorGUILayout.LabelField(title, HeaderStyle);
            if (!string.IsNullOrWhiteSpace(subtitle))
                EditorGUILayout.LabelField(subtitle, DeucarianEditorStyles.MutedLabel);
            DeucarianEditorStatusChipRow.Draw(chips);
        }

        private static void DrawOverview(GameContentAuthoringSurfaceContext context, GameContentPackAuthoringState state, GameContentLibraryItem item, bool creating, GameContentAuthoringValidationResult validation)
        {
            state.PackId = context.Authoring.DrawTextField("Stable ID", state.PackId);
            state.DisplayName = context.Authoring.DrawTextField("Display Name", state.DisplayName);
            state.Description = context.Authoring.DrawTextArea("Summary", state.Description);
            state.Version = context.Authoring.DrawTextField("Version", state.Version);
            state.Author = context.Authoring.DrawTextField("Author", state.Author);
            state.Icon = context.Authoring.DrawObjectField("Icon", state.Icon);
            state.Banner = context.Authoring.DrawObjectField("Banner", state.Banner);
            state.TagsCsv = context.Authoring.DrawTextField("Tags", state.TagsCsv);

            DrawSummaryRows(
                Row("Readiness", BuildValidationSummary(validation)),
                Row("Default Set", GetAssetName(state.DefaultContentSet, "Missing")),
                Row("Included Sets", CountAssigned(state.ContentSets).ToString(CultureInfo.InvariantCulture)),
                Row("Dependencies", BuildDependencyTotalSummary(state)),
                Row("Compatibility", BuildCompatibilitySummary(state)));

            if (creating)
                EditorGUILayout.LabelField("Output details are available on Review and Advanced.", DeucarianEditorStyles.MutedLabel);
            else if (item != null)
                EditorGUILayout.LabelField("Referenced by " + item.ReverseReferences.Count.ToString(CultureInfo.InvariantCulture) + " authored item(s).", DeucarianEditorStyles.MutedLabel);
        }

        private static void DrawIncludedSets(GameContentPackAuthoringState state)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Included Sets", DeucarianEditorStyles.SectionTitle);
                GUILayout.FlexibleSpace();
                if (DeucarianEditorButtons.Secondary("Add Set", true, GUILayout.Height(24f), GUILayout.Width(88f)))
                    state.ContentSets.Add(null);
            }

            if (state.ContentSets.Count == 0)
            {
                EditorGUILayout.LabelField("None assigned.", DeucarianEditorStyles.MutedLabel);
                return;
            }

            for (int i = 0; i < state.ContentSets.Count; i++)
            {
                int index = i;
                DeucarianEditorCards.DrawInlineCard(() =>
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(GetAssetName(state.ContentSets[index], "Missing content set"), DeucarianEditorStyles.SectionTitle);
                        GUILayout.FlexibleSpace();
                        if (DeucarianEditorMiniToolbar.Button("Default", state.ContentSets[index] != null, GUILayout.Width(64f), GUILayout.Height(22f)))
                            state.DefaultContentSet = state.ContentSets[index];
                        if (DeucarianEditorMiniToolbar.Button("Up", index > 0, GUILayout.Width(38f), GUILayout.Height(22f)))
                            Move(state.ContentSets, index, index - 1);
                        if (DeucarianEditorMiniToolbar.Button("Down", index < state.ContentSets.Count - 1, GUILayout.Width(54f), GUILayout.Height(22f)))
                            Move(state.ContentSets, index, index + 1);
                        if (DeucarianEditorMiniToolbar.Button("Remove", true, GUILayout.Width(68f), GUILayout.Height(22f)))
                        {
                            GameContentSetAsset removed = state.ContentSets[index];
                            state.ContentSets.RemoveAt(index);
                            if (state.DefaultContentSet == removed)
                                state.DefaultContentSet = state.ContentSets.Count > 0 ? state.ContentSets[0] : null;
                            return;
                        }
                    }

                    state.ContentSets[index] = DrawContentSetField("Content Set", state.ContentSets[index]);
                    DeucarianEditorStatusChipRow.Draw(BuildContentSetChips(state.ContentSets[index], state));
                });
            }
        }

        private static void DrawDefaultSetup(GameContentPackAuthoringState state)
        {
            state.DefaultContentSet = DrawContentSetField("Default Content Set", state.DefaultContentSet);
            GameContentPackValidationReport report = BuildValidationReport(state);
            GameContentSetAsset defaultSet = state.DefaultContentSet;
            DrawSummaryRows(
                Row("Ready To Play", report.IsValid ? "Ready" : report.ErrorCount.ToString(CultureInfo.InvariantCulture) + " blocker(s)"),
                Row("Included", defaultSet != null && Contains(state.ContentSets, defaultSet) ? "Yes" : "No"),
                Row("Starting Weapon", defaultSet == null ? "Missing" : GetAssetName(defaultSet.StartingWeapon, "Missing")),
                Row("Content Sets", CountAssigned(state.ContentSets).ToString(CultureInfo.InvariantCulture)),
                Row("Dependencies", BuildDependencyTotalSummary(state)));
        }

        private static void DrawCompatibility(GameContentAuthoringSurfaceContext context, GameContentPackAuthoringState state)
        {
            state.RequiredPackagesCsv = context.Authoring.DrawTextField("Required Packages", state.RequiredPackagesCsv);
            state.MinimumVersionsCsv = context.Authoring.DrawTextField("Minimum Versions", state.MinimumVersionsCsv);
            state.CompatibilityNotes = context.Authoring.DrawTextArea("Notes", state.CompatibilityNotes);
            DrawSummaryRows(
                Row("Required Packages", SplitCsv(state.RequiredPackagesCsv).Length.ToString(CultureInfo.InvariantCulture)),
                Row("Minimum Versions", SplitCsv(state.MinimumVersionsCsv).Length.ToString(CultureInfo.InvariantCulture)),
                Row("Status", BuildCompatibilitySummary(state)));
        }

        private static void DrawOneClickSetup(GameContentAuthoringSurfaceContext context, GameContentPackAuthoringState source, GameContentPackProviderV2State state, GameContentPackAsset savedAsset)
        {
            GameContentPackSceneSetupState setup = state.SceneSetup;
            if (savedAsset != null)
                setup.ContentPack = savedAsset;
            if (setup.SelectedContentSet == null)
                setup.SelectedContentSet = source.DefaultContentSet;

            setup.Controller = context.Authoring.DrawObjectField("Template Controller", setup.Controller, true);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (DeucarianEditorButtons.Secondary("Find Controller", true, GUILayout.Width(112f), GUILayout.Height(24f)))
                    setup.Controller = GameContentPackSceneSetupUtility.FindControllerInOpenScenes();
                GUILayout.FlexibleSpace();
            }

            if (savedAsset == null)
            {
                EditorGUILayout.LabelField("Create or select a saved content pack before applying to a scene.", DeucarianEditorStyles.MutedLabel);
                DrawSummaryRows(Row("Setup State", "Draft preview only"));
                return;
            }

            setup.SelectedContentSet = DrawContentSetField("Default Content Set", setup.SelectedContentSet);
            GameContentAuthoringValidationResult validation = GameContentPackSceneSetupUtility.Validate(setup.Controller, setup.ContentPack, setup.SelectedContentSet);
            DrawSummaryRows(
                Row("Setup State", validation.IsValid ? "Ready to Apply" : validation.ErrorCount.ToString(CultureInfo.InvariantCulture) + " blocker(s)"),
                Row("Pack", setup.ContentPack == null ? "Missing" : setup.ContentPack.DisplayName),
                Row("Content Set", GetAssetName(setup.SelectedContentSet, "Pack default")));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (DeucarianEditorButtons.Secondary("Preview Setup", validation.IsValid, GUILayout.Width(112f), GUILayout.Height(24f)))
                    setup.LastMessage = GameContentPackSceneSetupUtility.CreatePreviewSummary(setup.ContentPack, setup.SelectedContentSet);
                if (DeucarianEditorButtons.Primary("Apply", validation.IsValid, GUILayout.Width(72f), GUILayout.Height(24f)))
                {
                    GameContentCreationResult result = GameContentPackSceneSetupUtility.Apply(setup.Controller, setup.ContentPack, setup.SelectedContentSet);
                    setup.LastMessage = result.Message + (result.Succeeded ? " Scene intentionally marked dirty." : string.Empty);
                    context.Authoring.SetCreationResult(result);
                }
            }

            if (!string.IsNullOrWhiteSpace(setup.LastMessage))
                EditorGUILayout.LabelField(setup.LastMessage, DeucarianEditorStyles.MutedLabel);
        }

        private static void DrawReadiness(GameContentPackAuthoringState state, GameContentAuthoringValidationResult validation)
        {
            GameContentPackDependencySummary dependencies = BuildDependencySummary(state);
            DrawSummaryRows(
                Row("Readiness", BuildValidationSummary(validation)),
                Row("Content Sets", dependencies.ContentSetCount.ToString(CultureInfo.InvariantCulture)),
                Row("Attacks", dependencies.AttackCount.ToString(CultureInfo.InvariantCulture)),
                Row("Enemies", dependencies.EnemyCount.ToString(CultureInfo.InvariantCulture)),
                Row("Waves", dependencies.WaveCount.ToString(CultureInfo.InvariantCulture)),
                Row("Weapons", dependencies.WeaponCount.ToString(CultureInfo.InvariantCulture)),
                Row("Upgrades", dependencies.UpgradeCount.ToString(CultureInfo.InvariantCulture)));
        }

        private static void DrawReferences(GameContentLibraryItem item, GameContentPackAuthoringState state)
        {
            EditorGUILayout.LabelField("Linked Content Sets", DeucarianEditorStyles.SectionTitle);
            for (int i = 0; i < state.ContentSets.Count; i++)
                EditorGUILayout.LabelField(GetAssetName(state.ContentSets[i], "Missing content set"), DeucarianEditorStyles.MutedLabel);

            if (item == null) return;
            EditorGUILayout.LabelField("Referenced By", DeucarianEditorStyles.SectionTitle);
            if (item.ReverseReferences.Count == 0)
                EditorGUILayout.LabelField("No authored reverse references found.", DeucarianEditorStyles.MutedLabel);
            for (int i = 0; i < item.ReverseReferences.Count; i++)
            {
                GameContentLibraryReference reference = item.ReverseReferences[i];
                if (reference == null || reference.Target == null) continue;
                EditorGUILayout.LabelField(reference.Target.DisplayName + " - " + reference.Target.Category, DeucarianEditorStyles.MutedLabel);
            }
        }

        private static void DrawAdvanced(GameContentLibraryItem item, GameContentPackAuthoringState state, GameContentPackAsset asset)
        {
            state.OutputRoot = EditorGUILayout.TextField("Output Root", state.OutputRoot);
            DrawSummaryRows(
                Row("Path", item == null ? "(draft)" : item.Path),
                Row("Raw Pack ID", state.PackId),
                Row("Raw Content Set IDs", JoinIds(state.ContentSets)),
                Row("Required Package IDs", state.RequiredPackagesCsv),
                Row("Minimum Versions", state.MinimumVersionsCsv));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (DeucarianEditorButtons.Secondary("Copy Report", true, GUILayout.Width(110f), GUILayout.Height(24f)))
                    EditorGUIUtility.systemCopyBuffer = BuildAdvancedReport(item, state);
                if (asset != null)
                    DeucarianEditorMiniToolbar.PingButton(asset);
            }
        }

        private static void DrawReview(GameContentPackAuthoringState state, GameContentAuthoringValidationResult validation)
        {
            IReadOnlyList<string> lines = GameContentPackAssetCreator.GetPreviewLines(state);
            for (int i = 0; i < lines.Count; i++)
                EditorGUILayout.LabelField(lines[i], DeucarianEditorStyles.MutedLabel);

            DrawSummaryRows(
                Row("Readiness", BuildValidationSummary(validation)),
                Row("Default Set", GetAssetName(state.DefaultContentSet, "Missing")),
                Row("Included Sets", CountAssigned(state.ContentSets).ToString(CultureInfo.InvariantCulture)),
                Row("Dependencies", BuildDependencyTotalSummary(state)));
        }

        private static void DrawPreviewLab(GameContentAuthoringSurfaceContext context, GameContentPackAuthoringState draft, GameContentPackProviderV2State state)
        {
            GameContentPackAuthoringState source = state.Creating ? draft : state.EditingState;
            if (source == null)
            {
                EditorGUILayout.LabelField("Select a content pack to preview.", DeucarianEditorStyles.MutedLabel);
                return;
            }

            bool dirty = state.EditingContext != null && state.EditingContext.IsDirty;
            GameContentPackValidationReport report = BuildValidationReport(source);
            GameContentPackDependencySummary dependencies = BuildDependencySummary(source);
            GameContentPreviewLabModel model = new GameContentPreviewLabModel
            {
                Title = "Content Pack Preview Lab",
                PreviewTitle = string.IsNullOrWhiteSpace(source.DisplayName) ? "Content Pack Preview" : source.DisplayName,
                ScopeLabel = GameContentPackProviderV2PreviewModel.GetScopeLabel(state.Creating, dirty),
                PrimaryAsset = GetPrimaryPreviewAsset(source),
                EmptyText = "No icon, banner, or default content set assigned.",
                PreviewOptions = BuildPreviewOptions(source, state, dependencies),
                Chips = GameContentPackProviderV2PreviewModel.BuildChips(source, state, report, dependencies),
                DrawControls = () => DrawPreviewControls(state),
                DrawContext = () => DrawPreviewContext(source, report, dependencies, state.SceneSetup),
                DrawBody = () => DrawPreviewBody(source, state, report, dependencies)
            };

            state.PreviewScroll = EditorGUILayout.BeginScrollView(state.PreviewScroll);
            context.Preview.SetStatus(state.PreviewStatus);
            GameContentPreviewLabRenderer.Draw(context.Preview, model);
            EditorGUILayout.EndScrollView();
        }

        private static void DrawPreviewControls(GameContentPackProviderV2State state)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                string playLabel = state.PreviewPlaying ? "Pause" : "Preview";
                if (DeucarianEditorMiniToolbar.Button(playLabel, true, GUILayout.Width(64f), GUILayout.Height(22f)))
                {
                    state.PreviewPlaying = !state.PreviewPlaying;
                    if (state.PreviewPlaying)
                        state.PreviewStartTime = EditorApplication.timeSinceStartup;
                    else
                        state.PausedNormalizedTime = 0.5f;
                }

                if (DeucarianEditorMiniToolbar.Button("Stop", true, GUILayout.Width(48f), GUILayout.Height(22f)))
                    state.StopPreview();
                if (DeucarianEditorMiniToolbar.Button(state.PreviewLoop ? "Loop" : "Once", true, GUILayout.Width(48f), GUILayout.Height(22f)))
                    state.PreviewLoop = !state.PreviewLoop;
                if (DeucarianEditorMiniToolbar.Button("0.5x", true, GUILayout.Width(48f), GUILayout.Height(22f)))
                    state.PreviewSpeed = 0.5f;
                if (DeucarianEditorMiniToolbar.Button("1x", true, GUILayout.Width(38f), GUILayout.Height(22f)))
                    state.PreviewSpeed = 1f;
                if (DeucarianEditorMiniToolbar.Button("2x", true, GUILayout.Width(38f), GUILayout.Height(22f)))
                    state.PreviewSpeed = 2f;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (DeucarianEditorMiniToolbar.Button(state.PreviewRenderMode == GameContentAuthoringActionPreviewRenderMode.Game ? "Game" : "Debug", true, GUILayout.Width(58f), GUILayout.Height(22f)))
                    state.PreviewRenderMode = state.PreviewRenderMode == GameContentAuthoringActionPreviewRenderMode.Game
                        ? GameContentAuthoringActionPreviewRenderMode.Debug
                        : GameContentAuthoringActionPreviewRenderMode.Game;
            }
        }

        private static void DrawPreviewContext(GameContentPackAuthoringState source, GameContentPackValidationReport report, GameContentPackDependencySummary dependencies, GameContentPackSceneSetupState setup)
        {
            DrawSummaryRows(
                Row("Ready", report != null && report.IsValid ? "Playable" : report == null ? "Pending" : report.ErrorCount.ToString(CultureInfo.InvariantCulture) + " blocker(s)"),
                Row("Default", GetAssetName(source.DefaultContentSet, "Missing")),
                Row("Packaged", dependencies.ContentSetCount.ToString(CultureInfo.InvariantCulture) + " set(s), " + dependencies.WeaponCount.ToString(CultureInfo.InvariantCulture) + " weapon(s)"),
                Row("Setup", setup != null && setup.ContentPack != null ? "Saved pack" : "Draft / selection"));
        }

        private static void DrawPreviewBody(GameContentPackAuthoringState source, GameContentPackProviderV2State state, GameContentPackValidationReport report, GameContentPackDependencySummary dependencies)
        {
            DrawPackSummaryStrip(source, report, dependencies);
            DrawContentSetCards(source);
            DrawCompatibilityChips(source);
            if (state.PreviewRenderMode == GameContentAuthoringActionPreviewRenderMode.Debug)
            {
                DrawSummaryRows(BuildDebugRows(source, dependencies));
                DrawWarnings(GameContentPackAuthoringPreviewSummaries.BuildWarnings(report));
            }
        }

        private static void DrawPackSummaryStrip(GameContentPackAuthoringState source, GameContentPackValidationReport report, GameContentPackDependencySummary dependencies)
        {
            DrawSummaryRows(
                Row("Ready To Install", report != null && report.IsValid ? "Ready" : report == null ? "Pending" : report.ErrorCount.ToString(CultureInfo.InvariantCulture) + " blocker(s)"),
                Row("Default Set", GetAssetName(source.DefaultContentSet, "Missing")),
                Row("Included Sets", dependencies.ContentSetCount.ToString(CultureInfo.InvariantCulture)),
                Row("Dependency Totals", BuildDependencyTotalSummary(dependencies)),
                Row("Compatibility", BuildCompatibilitySummary(source)));
        }

        private static void DrawContentSetCards(GameContentPackAuthoringState source)
        {
            EditorGUILayout.LabelField("Packaged Content Sets", DeucarianEditorStyles.SectionTitle);
            if (source.ContentSets.Count == 0)
            {
                EditorGUILayout.LabelField("No content sets assigned.", DeucarianEditorStyles.MutedLabel);
                return;
            }

            for (int i = 0; i < source.ContentSets.Count; i++)
            {
                GameContentSetAsset contentSet = source.ContentSets[i];
                DeucarianEditorCards.DrawInlineCard(() =>
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(GetAssetName(contentSet, "Missing content set"), DeucarianEditorStyles.SectionTitle);
                        GUILayout.FlexibleSpace();
                        DeucarianEditorStatusChipRow.Draw(BuildContentSetChips(contentSet, source));
                    }

                    EditorGUILayout.LabelField(BuildContentSetSummary(contentSet), DeucarianEditorStyles.MutedLabel);
                });
            }
        }

        private static void DrawCompatibilityChips(GameContentPackAuthoringState source)
        {
            EditorGUILayout.LabelField("Compatibility", DeucarianEditorStyles.SectionTitle);
            DeucarianEditorStatusChipRow.Draw(new[]
            {
                new DeucarianEditorStatusChip(SplitCsv(source.RequiredPackagesCsv).Length.ToString(CultureInfo.InvariantCulture) + " package(s)", SplitCsv(source.RequiredPackagesCsv).Length > 0 ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Warning),
                new DeucarianEditorStatusChip(string.IsNullOrWhiteSpace(source.MinimumVersionsCsv) ? "No min versions" : "Min versions", DeucarianEditorStatus.Info),
                new DeucarianEditorStatusChip(string.IsNullOrWhiteSpace(source.CompatibilityNotes) ? "No notes" : "Notes", DeucarianEditorStatus.Info)
            });
        }

        private static GameContentAuthoringObjectPreviewOptions BuildPreviewOptions(GameContentPackAuthoringState source, GameContentPackProviderV2State state, GameContentPackDependencySummary dependencies)
        {
            var preview = new GameContentAuthoringActionPreview
            {
                PrimaryAsset = GetPrimaryPreviewAsset(source),
                Mode = GameContentAuthoringActionPreviewMode.Static,
                RenderMode = state.PreviewRenderMode,
                Playing = state.PreviewPlaying,
                Loop = state.PreviewLoop,
                Speed = state.PreviewSpeed,
                StartTime = state.PreviewStartTime,
                StaticNormalizedTime = state.PausedNormalizedTime,
                Label = string.IsNullOrWhiteSpace(source.DisplayName) ? source.PackId : source.DisplayName,
                DeliveryTypeLabel = "Playable Pack",
                SourceContextLabel = GetAssetName(source.DefaultContentSet, "Missing default"),
                TargetContextLabel = dependencies.ContentSetCount.ToString(CultureInfo.InvariantCulture) + " content sets"
            };

            preview.Roles.Add(new GameContentAuthoringActionPreviewRole("Default", GetAssetName(source.DefaultContentSet, "Missing default set"), source.DefaultContentSet));
            int roleCount = Math.Min(3, source.ContentSets.Count);
            for (int i = 0; i < roleCount; i++)
            {
                GameContentSetAsset contentSet = source.ContentSets[i];
                preview.Roles.Add(new GameContentAuthoringActionPreviewRole("Set " + (i + 1).ToString(CultureInfo.InvariantCulture), GetAssetName(contentSet, "Missing set"), contentSet));
            }

            return new GameContentAuthoringObjectPreviewOptions
            {
                MinimumHeight = 184f,
                ActionPreview = preview
            };
        }

        private static GameContentSetAsset DrawContentSetField(string label, GameContentSetAsset value)
        {
            return (GameContentSetAsset)EditorGUILayout.ObjectField(label, value, typeof(GameContentSetAsset), false);
        }

        private static void Move<TAsset>(List<TAsset> assets, int from, int to)
        {
            if (assets == null || from < 0 || from >= assets.Count || to < 0 || to >= assets.Count || from == to)
                return;

            TAsset asset = assets[from];
            assets.RemoveAt(from);
            assets.Insert(to, asset);
        }

        public static GameContentPackAuthoringState FromContentPackAsset(GameContentPackAsset asset)
        {
            var state = new GameContentPackAuthoringState();
            if (asset == null)
                return state;

            state.PackId = asset.Id;
            state.DisplayName = asset.DisplayName;
            state.Description = asset.Description;
            state.Version = asset.Version;
            state.Author = asset.Author;
            state.Icon = asset.Icon;
            state.Banner = asset.Banner;
            state.ContentSets.Clear();
            state.ContentSets.AddRange(asset.ContentSets);
            state.DefaultContentSet = asset.DefaultContentSet;
            state.RequiredPackagesCsv = string.Join(", ", asset.RequiredPackages);
            state.MinimumVersionsCsv = string.Join(", ", asset.MinimumPackageVersions);
            state.CompatibilityNotes = asset.TemplateCompatibilityNotes;
            state.TagsCsv = string.Join(", ", asset.Tags);
            state.OutputRoot = string.IsNullOrWhiteSpace(AssetDatabase.GetAssetPath(asset))
                ? state.OutputRoot
                : System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(asset)).Replace("\\", "/");
            return state;
        }

        public static string BuildStateFingerprint(GameContentPackAuthoringState state)
        {
            if (state == null)
                return string.Empty;

            var builder = new StringBuilder()
                .Append(state.PackId).Append('|')
                .Append(state.DisplayName).Append('|')
                .Append(state.Description).Append('|')
                .Append(state.Version).Append('|')
                .Append(state.Author).Append('|')
                .Append(GetAssetId(state.Icon)).Append('|')
                .Append(GetAssetId(state.Banner)).Append('|')
                .Append(GetAssetId(state.DefaultContentSet)).Append('|')
                .Append(state.RequiredPackagesCsv).Append('|')
                .Append(state.MinimumVersionsCsv).Append('|')
                .Append(state.CompatibilityNotes).Append('|')
                .Append(state.TagsCsv);
            AppendAssetIds(builder, state.ContentSets);
            return builder.ToString();
        }

        public static int CountAssigned<TAsset>(IReadOnlyList<TAsset> assets) where TAsset : UnityEngine.Object
        {
            if (assets == null) return 0;
            int count = 0;
            for (int i = 0; i < assets.Count; i++)
                if (assets[i] != null)
                    count++;
            return count;
        }

        public static GameContentPackDependencySummary BuildDependencySummary(GameContentPackAuthoringState state)
        {
            GameContentPackAsset preview = GameContentPackAssetCreator.BuildTransient(state);
            try
            {
                return GameContentPackValidator.CollectDependencies(preview);
            }
            finally
            {
                GameContentPackAssetCreator.DestroyTransient(preview);
            }
        }

        public static string BuildDependencyTotalSummary(GameContentPackAuthoringState state)
        {
            return BuildDependencyTotalSummary(BuildDependencySummary(state));
        }

        public static string BuildDependencyTotalSummary(GameContentPackDependencySummary dependencies)
        {
            if (dependencies == null) return "0 assets";
            int total = dependencies.AttackCount + dependencies.EnemyCount + dependencies.WaveCount + dependencies.WeaponCount + dependencies.UpgradeCount;
            return total.ToString(CultureInfo.InvariantCulture) + " authored asset(s)";
        }

        public static string BuildCompatibilitySummary(GameContentPackAuthoringState state)
        {
            if (state == null) return "Pending";
            int packages = SplitCsv(state.RequiredPackagesCsv).Length;
            int versions = SplitCsv(state.MinimumVersionsCsv).Length;
            if (packages == 0) return "No package requirements";
            if (versions > 0 && versions != packages) return "Version count mismatch";
            return packages.ToString(CultureInfo.InvariantCulture) + " required package(s)";
        }

        private static GameContentPackValidationReport BuildValidationReport(GameContentPackAuthoringState state)
        {
            GameContentPackAsset preview = GameContentPackAssetCreator.BuildTransient(state);
            try
            {
                return GameContentPackValidator.Validate(preview);
            }
            finally
            {
                GameContentPackAssetCreator.DestroyTransient(preview);
            }
        }

        private static GameContentAuthoringValidationResult ValidateDraft(GameContentPackAuthoringState draft)
        {
            GameContentPackAsset preview = GameContentPackAssetCreator.BuildTransient(draft);
            try
            {
                return GameContentPackAssetCreator.ValidateForCreation(draft, preview);
            }
            finally
            {
                GameContentPackAssetCreator.DestroyTransient(preview);
            }
        }

        private static IReadOnlyList<DeucarianEditorStatusChip> BuildContentPackChips(GameContentPackAuthoringState state, GameContentAuthoringValidationResult validation, GameContentLibraryItem item)
        {
            GameContentPackDependencySummary dependencies = BuildDependencySummary(state);
            return new[]
            {
                new DeucarianEditorStatusChip(BuildValidationSummary(validation), validation != null && validation.ErrorCount > 0 ? DeucarianEditorStatus.Error : validation != null && validation.WarningCount > 0 ? DeucarianEditorStatus.Warning : DeucarianEditorStatus.Success),
                new DeucarianEditorStatusChip(state.DefaultContentSet == null ? "NoDefault" : "Default", state.DefaultContentSet == null ? DeucarianEditorStatus.Error : DeucarianEditorStatus.Success),
                new DeucarianEditorStatusChip(CountAssigned(state.ContentSets).ToString(CultureInfo.InvariantCulture) + " set(s)", CountAssigned(state.ContentSets) > 0 ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Error),
                new DeucarianEditorStatusChip(BuildDependencyTotalSummary(dependencies), dependencies.ContentSetCount > 0 ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Warning),
                new DeucarianEditorStatusChip(item == null ? "Draft" : BuildUsageSummary(item), item == null || item.ReverseReferences.Count == 0 ? DeucarianEditorStatus.Disabled : DeucarianEditorStatus.Success)
            };
        }

        private static IReadOnlyList<DeucarianEditorStatusChip> BuildContentSetChips(GameContentSetAsset contentSet, GameContentPackAuthoringState pack)
        {
            if (contentSet == null)
            {
                return new[]
                {
                    new DeucarianEditorStatusChip("Missing", DeucarianEditorStatus.Error),
                    new DeucarianEditorStatusChip("No default", DeucarianEditorStatus.Error)
                };
            }

            bool isDefault = contentSet == pack.DefaultContentSet;
            return new[]
            {
                new DeucarianEditorStatusChip("Ready", GameContentSetValidator.Validate(contentSet).IsValid ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Error),
                new DeucarianEditorStatusChip(isDefault ? "Default" : "Included", isDefault ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Info),
                new DeucarianEditorStatusChip(contentSet.AvailableWeapons.Count.ToString(CultureInfo.InvariantCulture) + " weapons", DeucarianEditorStatus.Info),
                new DeucarianEditorStatusChip(contentSet.WaveSet.Count.ToString(CultureInfo.InvariantCulture) + " waves", DeucarianEditorStatus.Info),
                new DeucarianEditorStatusChip(contentSet.UpgradePool.Count.ToString(CultureInfo.InvariantCulture) + " upgrades", DeucarianEditorStatus.Info)
            };
        }

        private static void DrawValidationIssues(GameContentAuthoringValidationResult validation)
        {
            if (validation == null || validation.Issues.Count == 0)
                return;

            var messages = new List<string>();
            for (int i = 0; i < validation.Issues.Count; i++)
            {
                GameContentAuthoringValidationIssue issue = validation.Issues[i];
                string prefix = string.IsNullOrWhiteSpace(issue.Path) ? string.Empty : issue.Path + ": ";
                messages.Add(prefix + issue.Message);
            }

            DeucarianEditorStatus status = validation.ErrorCount > 0
                ? DeucarianEditorStatus.Error
                : validation.WarningCount > 0
                    ? DeucarianEditorStatus.Warning
                    : DeucarianEditorStatus.Info;
            DeucarianEditorStatusPanel.DrawValidationCard(BuildValidationSummary(validation), messages, status);
        }

        private static void DrawWarnings(IReadOnlyList<string> warnings)
        {
            if (warnings == null || warnings.Count == 0) return;
            DeucarianEditorStatusPanel.DrawValidationCard("Diagnostics", warnings, DeucarianEditorStatus.Warning);
        }

        private static IReadOnlyList<GameContentAuthoringPreviewRow> BuildDebugRows(GameContentPackAuthoringState state, GameContentPackDependencySummary dependencies)
        {
            return new[]
            {
                Row("Raw Content Pack ID", state.PackId),
                Row("Output Root", state.OutputRoot),
                Row("Tags", state.TagsCsv),
                Row("Content Set IDs", JoinIds(state.ContentSets)),
                Row("Required Packages", state.RequiredPackagesCsv),
                Row("Minimum Versions", state.MinimumVersionsCsv),
                Row("Content Sets", dependencies.ContentSetCount.ToString(CultureInfo.InvariantCulture)),
                Row("Attacks", dependencies.AttackCount.ToString(CultureInfo.InvariantCulture)),
                Row("Enemies", dependencies.EnemyCount.ToString(CultureInfo.InvariantCulture)),
                Row("Waves", dependencies.WaveCount.ToString(CultureInfo.InvariantCulture)),
                Row("Weapons", dependencies.WeaponCount.ToString(CultureInfo.InvariantCulture)),
                Row("Upgrades", dependencies.UpgradeCount.ToString(CultureInfo.InvariantCulture))
            };
        }

        private static void DrawSummaryRows(params GameContentAuthoringPreviewRow[] rows)
        {
            DrawSummaryRows((IReadOnlyList<GameContentAuthoringPreviewRow>)rows);
        }

        private static void DrawSummaryRows(IReadOnlyList<GameContentAuthoringPreviewRow> rows)
        {
            if (rows == null)
                return;

            for (int i = 0; i < rows.Count; i++)
            {
                GameContentAuthoringPreviewRow row = rows[i];
                DeucarianEditorFieldRow.Draw(row.Label, () => EditorGUILayout.LabelField(row.Value, EditorStyles.label));
            }
        }

        private static GameContentAuthoringPreviewRow Row(string label, string value)
        {
            return new GameContentAuthoringPreviewRow(label, value);
        }

        private static string BuildValidationSummary(GameContentAuthoringValidationResult validation)
        {
            if (validation == null)
                return "Pending";
            if (validation.ErrorCount > 0)
                return validation.ErrorCount.ToString(CultureInfo.InvariantCulture) + " blocker(s)";
            if (validation.WarningCount > 0)
                return validation.WarningCount.ToString(CultureInfo.InvariantCulture) + " warning(s)";
            return "Ready";
        }

        private static string BuildUsageSummary(GameContentLibraryItem item)
        {
            if (item == null) return "Draft";
            return item.ReverseReferences.Count.ToString(CultureInfo.InvariantCulture) + " use(s)";
        }

        private static string BuildContentSetSummary(GameContentSetAsset contentSet)
        {
            if (contentSet == null) return "Missing content set reference.";
            return contentSet.AvailableWeapons.Count.ToString(CultureInfo.InvariantCulture) + " weapons, "
                + contentSet.WaveSet.Count.ToString(CultureInfo.InvariantCulture) + " waves, "
                + contentSet.UpgradePool.Count.ToString(CultureInfo.InvariantCulture) + " upgrades";
        }

        private static string BuildAdvancedReport(GameContentLibraryItem item, GameContentPackAuthoringState state)
        {
            return "Content Pack: " + state.DisplayName + Environment.NewLine
                + "ID: " + state.PackId + Environment.NewLine
                + "Path: " + (item == null ? "(draft)" : item.Path) + Environment.NewLine
                + "Default Content Set: " + GetAssetName(state.DefaultContentSet, "Missing") + Environment.NewLine
                + "Content Sets: " + JoinIds(state.ContentSets) + Environment.NewLine
                + "Required Packages: " + state.RequiredPackagesCsv + Environment.NewLine
                + "Minimum Versions: " + state.MinimumVersionsCsv + Environment.NewLine
                + "Compatibility: " + state.CompatibilityNotes;
        }

        private static UnityEngine.Object GetPrimaryPreviewAsset(GameContentPackAuthoringState state)
        {
            if (state == null) return null;
            if (state.Banner != null) return state.Banner;
            if (state.Icon != null) return state.Icon;
            if (state.DefaultContentSet != null) return state.DefaultContentSet;
            if (state.ContentSets.Count > 0 && state.ContentSets[0] != null) return state.ContentSets[0];
            return null;
        }

        private static bool Contains(IReadOnlyList<GameContentSetAsset> contentSets, GameContentSetAsset target)
        {
            if (contentSets == null || target == null) return false;
            for (int i = 0; i < contentSets.Count; i++)
                if (contentSets[i] == target)
                    return true;
            return false;
        }

        private static string JoinIds(IReadOnlyList<GameContentSetAsset> assets)
        {
            if (assets == null || assets.Count == 0) return "None";
            var ids = new List<string>();
            for (int i = 0; i < assets.Count; i++)
            {
                string id = GetContentId(assets[i]);
                if (!string.IsNullOrWhiteSpace(id))
                    ids.Add(id);
            }

            return ids.Count == 0 ? "None" : string.Join(", ", ids.ToArray());
        }

        private static void AppendAssetIds(StringBuilder builder, IReadOnlyList<GameContentSetAsset> assets)
        {
            builder.Append('|');
            if (assets == null) return;
            for (int i = 0; i < assets.Count; i++)
                builder.Append(GetAssetId(assets[i])).Append(',');
        }

        private static string GetAssetId(UnityEngine.Object asset)
        {
            if (asset == null) return string.Empty;
            string contentId = GetContentId(asset);
            if (!string.IsNullOrWhiteSpace(contentId)) return contentId;
            return AssetDatabase.GetAssetPath(asset);
        }

        private static string GetContentId(UnityEngine.Object asset)
        {
            if (asset is GameContentSetAsset set) return set.Id;
            if (asset is WeaponDefinitionAsset weapon) return weapon.Id;
            if (asset is AttackDefinitionAsset attack) return attack.Id;
            if (asset is EnemyDefinitionAsset enemy) return enemy.Id;
            if (asset is WaveDefinitionAsset wave) return wave.Id;
            if (asset is RunUpgradeDefinitionAsset upgrade) return upgrade.Id;
            return asset == null ? string.Empty : asset.name;
        }

        private static string GetAssetName(UnityEngine.Object asset, string empty)
        {
            if (asset == null) return empty;
            if (asset is GameContentPackAsset pack) return string.IsNullOrWhiteSpace(pack.DisplayName) ? pack.Id : pack.DisplayName;
            if (asset is GameContentSetAsset set) return string.IsNullOrWhiteSpace(set.DisplayName) ? set.Id : set.DisplayName;
            if (asset is WeaponDefinitionAsset weapon) return string.IsNullOrWhiteSpace(weapon.DisplayName) ? weapon.Id : weapon.DisplayName;
            if (asset is EnemyDefinitionAsset enemy) return string.IsNullOrWhiteSpace(enemy.DisplayName) ? enemy.Id : enemy.DisplayName;
            if (asset is WaveDefinitionAsset wave) return string.IsNullOrWhiteSpace(wave.DisplayName) ? wave.Id : wave.DisplayName;
            if (asset is RunUpgradeDefinitionAsset upgrade) return string.IsNullOrWhiteSpace(upgrade.DisplayName) ? upgrade.Id : upgrade.DisplayName;
            return asset.name;
        }

        private static string[] SplitCsv(string value)
        {
            return GameContentAuthoringEditorAssets.SplitCsv(value);
        }

        private static GUIStyle headerStyle;

        private static GUIStyle HeaderStyle
        {
            get
            {
                if (headerStyle == null)
                {
                    headerStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 14
                    };
                    headerStyle.normal.textColor = DeucarianEditorTheme.Text;
                }

                return headerStyle;
            }
        }
    }

    internal static class GameContentPackProviderV2PreviewModel
    {
        public const bool ExposesRedundantSelectButton = false;

        public static string GetScopeLabel(bool creating, bool unsaved)
        {
            if (creating)
                return "Draft";
            return unsaved ? "Unsaved" : "Selected";
        }

        public static IReadOnlyList<DeucarianEditorStatusChip> BuildChips(GameContentPackAuthoringState state, GameContentPackProviderV2State previewState, GameContentPackValidationReport report, GameContentPackDependencySummary dependencies)
        {
            if (state == null)
                return Array.Empty<DeucarianEditorStatusChip>();

            bool debug = previewState != null && previewState.PreviewRenderMode == GameContentAuthoringActionPreviewRenderMode.Debug;
            float speed = previewState == null ? 1f : previewState.PreviewSpeed;
            DeucarianEditorStatus readiness = report != null && report.ErrorCount > 0
                ? DeucarianEditorStatus.Error
                : report != null && report.WarningCount > 0
                    ? DeucarianEditorStatus.Warning
                    : DeucarianEditorStatus.Success;
            string readinessLabel = report == null
                ? "Pending"
                : report.ErrorCount > 0
                    ? report.ErrorCount.ToString(CultureInfo.InvariantCulture) + " blocker(s)"
                    : report.WarningCount > 0
                        ? report.WarningCount.ToString(CultureInfo.InvariantCulture) + " warning(s)"
                        : "Ready";
            dependencies ??= new GameContentPackDependencySummary(0, 0, 0, 0, 0, 0);
            return new[]
            {
                new DeucarianEditorStatusChip(debug ? "Debug" : "Game", debug ? DeucarianEditorStatus.Warning : DeucarianEditorStatus.Info),
                new DeucarianEditorStatusChip(readinessLabel, readiness),
                new DeucarianEditorStatusChip(dependencies.ContentSetCount.ToString(CultureInfo.InvariantCulture) + " sets", dependencies.ContentSetCount > 0 ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Error),
                new DeucarianEditorStatusChip(dependencies.WeaponCount.ToString(CultureInfo.InvariantCulture) + " weapons", dependencies.WeaponCount > 0 ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Warning),
                new DeucarianEditorStatusChip(dependencies.WaveCount.ToString(CultureInfo.InvariantCulture) + " waves", dependencies.WaveCount > 0 ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Warning),
                new DeucarianEditorStatusChip(speed.ToString("0.#", CultureInfo.InvariantCulture) + "x", DeucarianEditorStatus.Info)
            };
        }
    }

    internal sealed class GameContentPackProviderV2ListItem
    {
        private GameContentPackProviderV2ListItem(GameContentLibraryItem source, GameContentPackAsset asset)
        {
            Source = source;
            Asset = asset;
            StableId = asset == null ? source == null ? string.Empty : source.Id : asset.Id;
            DisplayName = asset == null ? source == null ? "Content Pack" : source.DisplayName : asset.DisplayName;
            Tags = asset == null ? string.Empty : string.Join(", ", asset.Tags);
            DefaultContentSetLabel = asset == null || asset.DefaultContentSet == null ? "NoDefault" : "Default";
            HasDefaultContentSet = asset != null && asset.DefaultContentSet != null;
            ContentSetCount = asset == null ? 0 : GameContentPackProviderV2View.CountAssigned(asset.ContentSets);
            GameContentPackDependencySummary dependencies = asset == null ? new GameContentPackDependencySummary(0, 0, 0, 0, 0, 0) : GameContentPackValidator.CollectDependencies(asset);
            DependencyCount = dependencies.AttackCount + dependencies.EnemyCount + dependencies.WaveCount + dependencies.WeaponCount + dependencies.UpgradeCount;
            CompatibilityLabel = asset == null ? "Compat" : asset.RequiredPackages.Count.ToString(CultureInfo.InvariantCulture) + " pkg";
            CompatibilityStatus = asset != null && asset.RequiredPackages.Count > 0 ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Warning;
            ReadinessLabel = source == null ? "Ready" : source.ValidationLabel;
            ReadinessStatus = source != null && source.ErrorCount > 0
                ? DeucarianEditorStatus.Error
                : source != null && source.WarningCount > 0
                    ? DeucarianEditorStatus.Warning
                    : DeucarianEditorStatus.Success;
            SearchText = string.Join(" ", new[]
            {
                DisplayName,
                StableId,
                Tags,
                ReadinessLabel,
                DefaultContentSetLabel,
                ContentSetCount.ToString(CultureInfo.InvariantCulture),
                DependencyCount.ToString(CultureInfo.InvariantCulture),
                CompatibilityLabel,
                asset == null || asset.DefaultContentSet == null ? string.Empty : asset.DefaultContentSet.DisplayName
            });
        }

        public GameContentLibraryItem Source { get; }
        public GameContentPackAsset Asset { get; }
        public string StableId { get; }
        public string DisplayName { get; }
        public string Tags { get; }
        public bool HasDefaultContentSet { get; }
        public string DefaultContentSetLabel { get; }
        public int ContentSetCount { get; }
        public int DependencyCount { get; }
        public string CompatibilityLabel { get; }
        public DeucarianEditorStatus CompatibilityStatus { get; }
        public string ReadinessLabel { get; }
        public DeucarianEditorStatus ReadinessStatus { get; }
        private string SearchText { get; }

        public static IReadOnlyList<GameContentPackProviderV2ListItem> Build(IReadOnlyList<GameContentLibraryItem> items)
        {
            if (items == null || items.Count == 0)
                return Array.Empty<GameContentPackProviderV2ListItem>();

            var result = new List<GameContentPackProviderV2ListItem>();
            for (int i = 0; i < items.Count; i++)
            {
                GameContentPackProviderV2ListItem item = FromItem(items[i]);
                if (item != null)
                    result.Add(item);
            }

            result.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase));
            return result;
        }

        public static GameContentPackProviderV2ListItem FromItem(GameContentLibraryItem item)
        {
            if (item == null || item.Kind != GameContentLibraryKind.ContentPack)
                return null;

            return new GameContentPackProviderV2ListItem(item, item.Asset as GameContentPackAsset);
        }

        public static GameContentPackProviderV2ListItem FromAssetForTests(GameContentPackAsset asset)
        {
            return new GameContentPackProviderV2ListItem(null, asset);
        }

        public bool Matches(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return true;

            return SearchText != null && SearchText.IndexOf(query.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
