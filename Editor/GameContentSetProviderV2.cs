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
    internal sealed class GameContentSetProviderV2State
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
        public GameContentSetAuthoringState EditingState;
        public GameContentAuthoringObjectEditorContext EditingContext;
        public GameContentCreationResult LastEditResult;

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
            PreviewStatus = "Previewing draft content set";
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

    internal sealed class GameContentSetProviderV2View
    {
        private static readonly string[] DetailPages =
        {
            "Overview",
            "Starting Setup",
            "Weapons",
            "Enemies",
            "Waves",
            "Upgrades",
            "Economy / Difficulty",
            "References",
            "Advanced"
        };

        private static readonly string[] WizardSteps =
        {
            "Identity",
            "Starting Setup",
            "Weapons",
            "Enemies",
            "Waves",
            "Upgrades",
            "Economy / Difficulty",
            "Review"
        };

        public void Draw(
            GameContentAuthoringSurfaceContext context,
            GameContentSetAuthoringState draft,
            GameContentSetProviderV2State state)
        {
            if (context == null || draft == null || state == null)
                return;

            IReadOnlyList<GameContentSetProviderV2ListItem> items = GameContentSetProviderV2ListItem.Build(context.AuthoredItems);
            EnsureDefaultMode(context, state, items);
            EnsureEditingState(context, state);
            TrackPreviewSource(context, state);

            GameContentAuthoringWorkbench.Draw(
                context,
                () => DrawContentSetList(context, state, items),
                () => DrawDetailOrWizard(context, draft, state),
                () => DrawPreviewLab(context, draft, state));
        }

        private static void EnsureDefaultMode(GameContentAuthoringSurfaceContext context, GameContentSetProviderV2State state, IReadOnlyList<GameContentSetProviderV2ListItem> items)
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

        private static void EnsureEditingState(GameContentAuthoringSurfaceContext context, GameContentSetProviderV2State state)
        {
            if (state.Creating || context.SelectedItem == null)
            {
                state.ClearEditingState();
                return;
            }

            GameContentSetAsset selected = context.SelectedItem.Asset as GameContentSetAsset;
            if (selected == null)
            {
                state.ClearEditingState();
                return;
            }

            if (state.EditingContext != null && string.Equals(state.EditingContext.Key, context.SelectedItem.Key, StringComparison.Ordinal) && state.EditingState != null)
                return;

            state.EditingState = FromContentSetAsset(selected);
            string fingerprint = BuildStateFingerprint(state.EditingState);
            state.EditingContext = new GameContentAuthoringObjectEditorContext(context.SelectedItem, fingerprint);
            state.LastEditResult = null;
        }

        private static void TrackPreviewSource(GameContentAuthoringSurfaceContext context, GameContentSetProviderV2State state)
        {
            string key = state.Creating
                ? "__draft_content_set__"
                : context.SelectedItem == null
                    ? string.Empty
                    : context.SelectedItem.Key;
            state.SetPreviewSource(key);
        }

        private static void DrawContentSetList(GameContentAuthoringSurfaceContext context, GameContentSetProviderV2State state, IReadOnlyList<GameContentSetProviderV2ListItem> items)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Content Sets", DeucarianEditorStyles.SectionTitle);
                GUILayout.FlexibleSpace();
                if (DeucarianEditorMiniToolbar.Button("Refresh", true, GUILayout.Width(62f), GUILayout.Height(22f)))
                    context.RefreshLibrary();
            }

            state.SearchText = DeucarianEditorSearchField.Draw(state.SearchText, "Search content sets", GUILayout.ExpandWidth(true));
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
                GameContentSetProviderV2ListItem item = items[i];
                if (!item.Matches(state.SearchText))
                    continue;

                shown++;
                DrawContentSetCard(context, state, item);
            }

            if (shown == 0)
                EditorGUILayout.LabelField(items.Count == 0 ? "No authored content sets found." : "No content sets match the current search.", DeucarianEditorStyles.MutedLabel);
            EditorGUILayout.EndScrollView();
        }

        private static void DrawContentSetCard(GameContentAuthoringSurfaceContext context, GameContentSetProviderV2State state, GameContentSetProviderV2ListItem item)
        {
            bool selected = !state.Creating && context.IsSelected(item.Source);
            var chips = new[]
            {
                new DeucarianEditorStatusChip(item.ReadinessLabel, item.ReadinessStatus),
                new DeucarianEditorStatusChip(item.StartingWeaponLabel, item.HasStartingWeapon ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Error),
                new DeucarianEditorStatusChip(item.WeaponCount.ToString(CultureInfo.InvariantCulture) + " weapon(s)", item.WeaponCount > 0 ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Error),
                new DeucarianEditorStatusChip(item.WaveCount.ToString(CultureInfo.InvariantCulture) + " wave(s)", item.WaveCount > 0 ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Error),
                new DeucarianEditorStatusChip(item.EnemyCount.ToString(CultureInfo.InvariantCulture) + " enemies", item.EnemyCount > 0 ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Error),
                new DeucarianEditorStatusChip(item.UpgradeCount.ToString(CultureInfo.InvariantCulture) + " upgrade(s)", item.UpgradeCount > 0 ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Warning),
                new DeucarianEditorStatusChip(item.DurationTicks.ToString(CultureInfo.InvariantCulture) + " ticks", DeucarianEditorStatus.Info)
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

        private static void DrawDetailOrWizard(GameContentAuthoringSurfaceContext context, GameContentSetAuthoringState draft, GameContentSetProviderV2State state)
        {
            state.DetailScroll = EditorGUILayout.BeginScrollView(state.DetailScroll);
            if (state.Creating)
                DrawCreateWizard(context, draft, state);
            else
                DrawSelectedContentSet(context, state);
            EditorGUILayout.EndScrollView();
        }

        private static void DrawSelectedContentSet(GameContentAuthoringSurfaceContext context, GameContentSetProviderV2State state)
        {
            GameContentSetAsset asset = context.SelectedItem == null ? null : context.SelectedItem.Asset as GameContentSetAsset;
            if (asset == null || state.EditingState == null || state.EditingContext == null)
            {
                EditorGUILayout.LabelField("Select a content set to edit.", DeucarianEditorStyles.MutedLabel);
                return;
            }

            GameContentSetAuthoringState edit = state.EditingState;
            GameContentAuthoringValidationResult validation = GameContentSetAssetCreator.ValidateForUpdate(edit, asset);
            string fingerprint = BuildStateFingerprint(edit);
            state.EditingContext.Capture(fingerprint, validation);
            context.Authoring.SetValidation(validation);

            DrawHeader(edit.DisplayName, edit.ContentSetId, BuildContentSetChips(edit, validation, context.SelectedItem));
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
                    DrawStartingSetup(context, edit, validation);
                    break;
                case 2:
                    DrawWeapons(edit);
                    break;
                case 3:
                    DrawEnemies(edit);
                    break;
                case 4:
                    DrawWaves(edit);
                    break;
                case 5:
                    DrawUpgrades(edit);
                    break;
                case 6:
                    DrawEconomy(context, edit);
                    break;
                case 7:
                    DrawReferences(context.SelectedItem);
                    break;
                case 8:
                    DrawAdvanced(context.SelectedItem, edit, asset);
                    break;
                default:
                    DrawOverview(context, edit, context.SelectedItem, false, validation);
                    break;
            }

            DrawValidationIssues(validation);
        }

        private static void HandleEditCommand(GameContentAuthoringSurfaceContext context, GameContentSetProviderV2State state, GameContentSetAsset asset, GameContentAuthoringCommand command)
        {
            if (command == GameContentAuthoringCommand.Revert)
            {
                state.EditingState = FromContentSetAsset(asset);
                string fingerprint = BuildStateFingerprint(state.EditingState);
                state.EditingContext.Accept(fingerprint, "Reverted");
                state.LastEditResult = null;
                GUI.FocusControl(null);
                context.RequestRepaint();
                return;
            }

            if (command != GameContentAuthoringCommand.Save)
                return;

            state.LastEditResult = GameContentSetAssetCreator.UpdateExistingAsset(asset, state.EditingState);
            if (state.LastEditResult != null && state.LastEditResult.Succeeded)
            {
                state.EditingState = FromContentSetAsset(asset);
                string fingerprint = BuildStateFingerprint(state.EditingState);
                state.EditingContext.Accept(fingerprint, "Saved");
                context.RefreshLibrary();
            }
            else if (state.LastEditResult != null)
            {
                state.EditingContext.SetStatus(state.LastEditResult.Message);
            }

            GUI.FocusControl(null);
            context.RequestRepaint();
        }

        private static void DrawCreateWizard(GameContentAuthoringSurfaceContext context, GameContentSetAuthoringState draft, GameContentSetProviderV2State state)
        {
            GameContentAuthoringValidationResult validation = ValidateDraft(draft);
            DrawHeader("New Content Set", draft.ContentSetId, BuildContentSetChips(draft, validation, null));
            GameContentAuthoringCommand command = GameContentAuthoringCommandBar.Draw(GameContentAuthoringWorkbenchMode.Create, validation.IsValid, true, "Create");
            if (command == GameContentAuthoringCommand.Create)
            {
                GameContentCreationResult result = GameContentSetAssetCreator.CreateAssets(draft);
                context.Authoring.SetCreationResult(result);
                if (result != null && result.Succeeded)
                {
                    state.Creating = false;
                    context.RefreshLibrary();
                }
            }

            state.WizardStep = DeucarianEditorWizardHeader.Draw(state.WizardStep, WizardSteps);
            GUILayout.Space(DeucarianEditorSpacing.Small);
            switch (Mathf.Clamp(state.WizardStep, 0, WizardSteps.Length - 1))
            {
                case 1:
                    DrawStartingSetup(context, draft, validation);
                    break;
                case 2:
                    DrawWeapons(draft);
                    break;
                case 3:
                    DrawEnemies(draft);
                    break;
                case 4:
                    DrawWaves(draft);
                    break;
                case 5:
                    DrawUpgrades(draft);
                    break;
                case 6:
                    DrawEconomy(context, draft);
                    break;
                case 7:
                    DrawReview(draft, validation);
                    break;
                default:
                    DrawOverview(context, draft, null, true, validation);
                    break;
            }

            DrawValidationIssues(validation);
            context.Authoring.DrawCreationResult();
        }

        private static void DrawHeader(string title, string subtitle, IReadOnlyList<DeucarianEditorStatusChip> chips)
        {
            EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(title) ? "Content Set" : title, HeaderStyle);
            if (!string.IsNullOrWhiteSpace(subtitle))
                EditorGUILayout.LabelField(subtitle, DeucarianEditorStyles.MutedLabel);
            DeucarianEditorStatusChipRow.Draw(chips);
        }

        private static void DrawOverview(GameContentAuthoringSurfaceContext context, GameContentSetAuthoringState state, GameContentLibraryItem item, bool creating, GameContentAuthoringValidationResult validation)
        {
            state.ContentSetId = context.Authoring.DrawTextField("Stable ID", state.ContentSetId);
            state.DisplayName = context.Authoring.DrawTextField("Display Name", state.DisplayName);
            state.Description = context.Authoring.DrawTextArea("Description", state.Description);
            state.Icon = DrawObjectField("Icon", state.Icon);
            state.Banner = DrawObjectField("Banner", state.Banner);
            state.TagsCsv = context.Authoring.DrawTextField("Tags", state.TagsCsv);
            if (creating)
                state.OutputRoot = context.Authoring.DrawOutputRootField(state.OutputRoot);

            DrawSummaryRows(
                Row("Readiness", BuildValidationSummary(validation)),
                Row("Playable Run", BuildPlayableRunSummary(state)),
                Row("Starting Weapon", GetAssetName(state.StartingWeapon, "Missing")),
                Row("Weapons", CountAssigned(state.AvailableWeapons).ToString(CultureInfo.InvariantCulture)),
                Row("Waves", CountAssigned(state.WaveSet).ToString(CultureInfo.InvariantCulture)),
                Row("Total Enemies", CountWaveEnemies(state.WaveSet).ToString(CultureInfo.InvariantCulture)),
                Row("Upgrades", CountAssigned(state.UpgradePool).ToString(CultureInfo.InvariantCulture)),
                Row("Approx Duration", ApproximateDuration(state.WaveSet).ToString(CultureInfo.InvariantCulture) + " tick(s)"),
                Row("Used By", item == null ? "New draft" : BuildUsageSummary(item)));
        }

        private static void DrawStartingSetup(GameContentAuthoringSurfaceContext context, GameContentSetAuthoringState state, GameContentAuthoringValidationResult validation)
        {
            state.StartingWeapon = DrawObjectField("Starting Weapon", state.StartingWeapon);
            state.StartingCredits = context.Authoring.DrawIntField("Starting Credits", state.StartingCredits);
            state.StartingParts = context.Authoring.DrawIntField("Starting Parts", state.StartingParts);
            state.SessionLengthTicks = context.Authoring.DrawIntField("Session Length Ticks", state.SessionLengthTicks);
            state.Endless = context.Authoring.DrawToggle("Endless", state.Endless);

            DrawSummaryRows(
                Row("Start", GetAssetName(state.StartingWeapon, "Missing starting weapon")),
                Row("Resources", state.StartingCredits.ToString(CultureInfo.InvariantCulture) + " credits, " + state.StartingParts.ToString(CultureInfo.InvariantCulture) + " parts"),
                Row("Mode", state.Endless ? "Endless" : state.SessionLengthTicks.ToString(CultureInfo.InvariantCulture) + " tick session"));
        }

        private static void DrawWeapons(GameContentSetAuthoringState state)
        {
            DrawAssetList(
                "Weapons / Towers",
                state.AvailableWeapons,
                "Add Weapon",
                weapon => BuildWeaponChips(weapon),
                weapon => DrawSummaryRows(
                    Row("Assigned Attack", BuildWeaponAttackSummary(weapon)),
                    Row("Build Cost", weapon != null && weapon.Stats != null ? weapon.Stats.BuildCost.ToString(CultureInfo.InvariantCulture) : "Missing"),
                    Row("Fire", weapon != null && weapon.Stats != null ? weapon.Stats.FireMode.ToString() : "Missing")));
        }

        private static void DrawEnemies(GameContentSetAuthoringState state)
        {
            DrawAssetList(
                "Enemy Pool",
                state.EnemyPool,
                "Add Enemy",
                enemy => BuildEnemyChips(enemy, state),
                enemy => DrawSummaryRows(
                    Row("Role", enemy == null ? "Missing" : enemy.Role.ToString()),
                    Row("Used In Waves", CountEnemyWaveUses(enemy, state.WaveSet).ToString(CultureInfo.InvariantCulture)),
                    Row("Reward", enemy != null && enemy.Stats != null ? enemy.Stats.RewardValue.ToString(CultureInfo.InvariantCulture) : "Missing")));
        }

        private static void DrawWaves(GameContentSetAuthoringState state)
        {
            DrawAssetList(
                "Wave Progression",
                state.WaveSet,
                "Add Wave",
                BuildWaveChips,
                wave => DrawSummaryRows(
                    Row("Total Enemies", CountWaveEnemies(wave).ToString(CultureInfo.InvariantCulture)),
                    Row("Duration", ApproximateDuration(wave).ToString(CultureInfo.InvariantCulture) + " tick(s)"),
                    Row("Enemy Mix", BuildEnemyMixSummary(wave))));
        }

        private static void DrawUpgrades(GameContentSetAuthoringState state)
        {
            DrawAssetList(
                "Upgrade Pool",
                state.UpgradePool,
                "Add Upgrade",
                upgrade => BuildUpgradeChips(upgrade, state),
                upgrade => DrawSummaryRows(
                    Row("Target", BuildUpgradeTargetSummary(upgrade)),
                    Row("Modifier", BuildUpgradeModifierSummary(upgrade)),
                    Row("Rank", upgrade != null && upgrade.Economy != null ? upgrade.Economy.MaxRank.ToString(CultureInfo.InvariantCulture) : "Missing")));
        }

        private static void DrawEconomy(GameContentAuthoringSurfaceContext context, GameContentSetAuthoringState state)
        {
            state.StartingCredits = context.Authoring.DrawIntField("Starting Credits", state.StartingCredits);
            state.StartingParts = context.Authoring.DrawIntField("Starting Parts", state.StartingParts);
            state.RewardMultiplier = context.Authoring.DrawFloatField("Reward Multiplier", state.RewardMultiplier);
            state.DifficultyMultiplier = context.Authoring.DrawFloatField("Difficulty Multiplier", state.DifficultyMultiplier);
            state.SessionLengthTicks = context.Authoring.DrawIntField("Session Length Ticks", state.SessionLengthTicks);
            state.Endless = context.Authoring.DrawToggle("Endless", state.Endless);

            DrawSummaryRows(
                Row("Starting Resources", state.StartingCredits.ToString(CultureInfo.InvariantCulture) + " credits, " + state.StartingParts.ToString(CultureInfo.InvariantCulture) + " parts"),
                Row("Economy", "Rewards x" + FormatFloat(state.RewardMultiplier)),
                Row("Difficulty", "Difficulty x" + FormatFloat(state.DifficultyMultiplier)),
                Row("Session", state.Endless ? "Endless" : state.SessionLengthTicks.ToString(CultureInfo.InvariantCulture) + " ticks"));
        }

        private static void DrawReferences(GameContentLibraryItem item)
        {
            if (item == null)
            {
                EditorGUILayout.LabelField("Content packs and scene assignments appear after the content set is created.", DeucarianEditorStyles.MutedLabel);
                return;
            }

            int packCount = 0;
            for (int i = 0; i < item.ReverseReferences.Count; i++)
            {
                GameContentLibraryItem target = item.ReverseReferences[i].Target;
                if (target != null && target.Kind == GameContentLibraryKind.ContentPack)
                    packCount++;
            }

            DrawSummaryRows(
                Row("Content Packs", packCount.ToString(CultureInfo.InvariantCulture)),
                Row("Referenced By", item.ReverseReferences.Count.ToString(CultureInfo.InvariantCulture) + " authored reference(s)"));

            for (int i = 0; i < item.ReverseReferences.Count; i++)
            {
                GameContentLibraryReference reference = item.ReverseReferences[i];
                GameContentLibraryItem target = reference.Target;
                if (target == null) continue;
                DeucarianEditorCards.DrawInlineCard(() =>
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(target.DisplayName, DeucarianEditorStyles.SectionTitle);
                        GUILayout.FlexibleSpace();
                        DeucarianEditorStatusBadge.Draw(target.Kind.ToString(), DeucarianEditorStatus.Info, GUILayout.Width(104f));
                        DeucarianEditorMiniToolbar.PingButton(target.Asset);
                    }

                    EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(reference.PropertyPath) ? target.Id : reference.PropertyPath, DeucarianEditorStyles.MutedLabel);
                });
            }
        }

        private static void DrawAdvanced(GameContentLibraryItem item, GameContentSetAuthoringState state, GameContentSetAsset asset)
        {
            DrawSummaryRows(
                Row("Asset Path", item == null ? "(not created)" : item.Path),
                Row("Output Root", state.OutputRoot),
                Row("Raw References", item == null ? "New draft" : item.DirectReferences.Count.ToString(CultureInfo.InvariantCulture) + " direct, " + item.ReverseReferences.Count.ToString(CultureInfo.InvariantCulture) + " reverse"),
                Row("Icon", asset != null && asset.Icon != null ? AssetDatabase.GetAssetPath(asset.Icon) : "None"),
                Row("Banner", asset != null && asset.Banner != null ? AssetDatabase.GetAssetPath(asset.Banner) : "None"),
                Row("Starting Weapon", state.StartingWeapon == null ? "Missing" : AssetDatabase.GetAssetPath(state.StartingWeapon)));

            if (DeucarianEditorButtons.Secondary("Copy Report", true, GUILayout.Width(110f), GUILayout.Height(24f)))
                EditorGUIUtility.systemCopyBuffer = BuildAdvancedReport(item, state);
        }

        private static void DrawReview(GameContentSetAuthoringState state, GameContentAuthoringValidationResult validation)
        {
            IReadOnlyList<string> lines = GameContentSetAssetCreator.GetPreviewLines(state);
            for (int i = 0; i < lines.Count; i++)
                EditorGUILayout.LabelField(lines[i], DeucarianEditorStyles.MutedLabel);

            DrawSummaryRows(
                Row("Readiness", BuildValidationSummary(validation)),
                Row("Starting Weapon", GetAssetName(state.StartingWeapon, "Missing")),
                Row("Weapons", CountAssigned(state.AvailableWeapons).ToString(CultureInfo.InvariantCulture)),
                Row("Enemies", CountAssigned(state.EnemyPool).ToString(CultureInfo.InvariantCulture)),
                Row("Waves", CountAssigned(state.WaveSet).ToString(CultureInfo.InvariantCulture)),
                Row("Upgrades", CountAssigned(state.UpgradePool).ToString(CultureInfo.InvariantCulture)),
                Row("Duration", ApproximateDuration(state.WaveSet).ToString(CultureInfo.InvariantCulture) + " tick(s)"));
        }

        private static void DrawPreviewLab(GameContentAuthoringSurfaceContext context, GameContentSetAuthoringState draft, GameContentSetProviderV2State state)
        {
            GameContentSetAuthoringState source = state.Creating ? draft : state.EditingState;
            if (source == null)
            {
                EditorGUILayout.LabelField("Select a content set to preview.", DeucarianEditorStyles.MutedLabel);
                return;
            }

            bool dirty = !state.Creating && state.EditingContext != null && state.EditingContext.IsDirty;
            GameContentSetValidationReport report = BuildValidationReport(source);
            GameContentPreviewLabModel model = new GameContentPreviewLabModel
            {
                Title = "Content Set Preview Lab",
                PreviewTitle = string.IsNullOrWhiteSpace(source.DisplayName) ? "Content Set Preview" : source.DisplayName,
                ScopeLabel = GameContentSetProviderV2PreviewModel.GetScopeLabel(state.Creating, dirty),
                PrimaryAsset = GetPrimaryPreviewAsset(source),
                EmptyText = "No content set visual asset assigned.",
                PreviewOptions = BuildPreviewOptions(source, state),
                Chips = GameContentSetProviderV2PreviewModel.BuildChips(source, state, report),
                DrawControls = () => DrawPreviewControls(state),
                DrawContext = () => DrawPreviewContext(context, source, report),
                DrawBody = () => DrawPreviewBody(context, source, state, report)
            };

            state.PreviewScroll = EditorGUILayout.BeginScrollView(state.PreviewScroll);
            context.Preview.SetStatus(state.PreviewStatus);
            GameContentPreviewLabRenderer.Draw(context.Preview, model);
            EditorGUILayout.EndScrollView();
        }

        private static void DrawPreviewControls(GameContentSetProviderV2State state)
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

        private static void DrawPreviewContext(GameContentAuthoringSurfaceContext context, GameContentSetAuthoringState source, GameContentSetValidationReport report)
        {
            context.Preview.DrawSummaryRow("Ready", report != null && report.IsValid ? "Playable" : report == null ? "Pending" : report.ErrorCount.ToString(CultureInfo.InvariantCulture) + " blocker(s)");
            context.Preview.DrawSummaryRow("Starting", GetAssetName(source.StartingWeapon, "Missing"));
            context.Preview.DrawSummaryRow("Roster", CountAssigned(source.AvailableWeapons).ToString(CultureInfo.InvariantCulture) + " weapon(s), " + CountAssigned(source.WaveSet).ToString(CultureInfo.InvariantCulture) + " wave(s)");
            context.Preview.DrawSummaryRow("Duration", ApproximateDuration(source.WaveSet).ToString(CultureInfo.InvariantCulture) + " tick(s)");
        }

        private static void DrawPreviewBody(GameContentAuthoringSurfaceContext context, GameContentSetAuthoringState source, GameContentSetProviderV2State state, GameContentSetValidationReport report)
        {
            DrawRunSummaryStrip(source, report);
            DrawWaveTimeline(source);
            DrawWeaponAttackChains(source);
            DrawUpgradeTargets(source);

            if (state.PreviewRenderMode == GameContentAuthoringActionPreviewRenderMode.Debug)
            {
                context.Preview.DrawSummaryRows(BuildDebugRows(source));
                context.Preview.DrawWarnings(GameContentSetAuthoringPreviewSummaries.BuildWarnings(report));
            }
        }

        private static void DrawRunSummaryStrip(GameContentSetAuthoringState source, GameContentSetValidationReport report)
        {
            DrawSummaryRows(
                Row("Ready To Play", report != null && report.IsValid ? "Ready" : report == null ? "Pending" : report.ErrorCount.ToString(CultureInfo.InvariantCulture) + " blocker(s)"),
                Row("Starting Weapon", GetAssetName(source.StartingWeapon, "Missing")),
                Row("Enemy Mix", BuildEnemyMixSummary(source.WaveSet)),
                Row("Upgrade Pool", BuildUpgradePoolSummary(source)),
                Row("Economy", source.StartingCredits.ToString(CultureInfo.InvariantCulture) + " credits, rewards x" + FormatFloat(source.RewardMultiplier)));
        }

        private static void DrawWaveTimeline(GameContentSetAuthoringState source)
        {
            EditorGUILayout.LabelField("Wave Timeline", DeucarianEditorStyles.SectionTitle);
            if (source.WaveSet.Count == 0)
            {
                EditorGUILayout.LabelField("No waves assigned.", DeucarianEditorStyles.MutedLabel);
                return;
            }

            for (int i = 0; i < source.WaveSet.Count; i++)
            {
                WaveDefinitionAsset wave = source.WaveSet[i];
                DeucarianEditorCards.DrawInlineCard(() =>
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(GetAssetName(wave, "Missing wave"), DeucarianEditorStyles.SectionTitle);
                        GUILayout.FlexibleSpace();
                        DeucarianEditorStatusChipRow.Draw(BuildWaveChips(wave));
                    }

                    EditorGUILayout.LabelField(wave == null ? "Missing wave reference." : BuildEnemyMixSummary(wave), DeucarianEditorStyles.MutedLabel);
                });
            }
        }

        private static void DrawWeaponAttackChains(GameContentSetAuthoringState source)
        {
            EditorGUILayout.LabelField("Weapon / Attack Chains", DeucarianEditorStyles.SectionTitle);
            for (int i = 0; i < source.AvailableWeapons.Count; i++)
            {
                WeaponDefinitionAsset weapon = source.AvailableWeapons[i];
                DeucarianEditorCards.DrawInlineCard(() =>
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(GetAssetName(weapon, "Missing weapon"), DeucarianEditorStyles.SectionTitle);
                        GUILayout.FlexibleSpace();
                        DeucarianEditorStatusChipRow.Draw(BuildWeaponChips(weapon));
                    }

                    EditorGUILayout.LabelField(BuildWeaponAttackSummary(weapon), DeucarianEditorStyles.MutedLabel);
                });
            }
        }

        private static void DrawUpgradeTargets(GameContentSetAuthoringState source)
        {
            EditorGUILayout.LabelField("Upgrade Targets", DeucarianEditorStyles.SectionTitle);
            if (source.UpgradePool.Count == 0)
            {
                EditorGUILayout.LabelField("No upgrades assigned.", DeucarianEditorStyles.MutedLabel);
                return;
            }

            for (int i = 0; i < source.UpgradePool.Count; i++)
            {
                RunUpgradeDefinitionAsset upgrade = source.UpgradePool[i];
                DeucarianEditorCards.DrawInlineCard(() =>
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(GetAssetName(upgrade, "Missing upgrade"), DeucarianEditorStyles.SectionTitle);
                        GUILayout.FlexibleSpace();
                        DeucarianEditorStatusChipRow.Draw(BuildUpgradeChips(upgrade, source));
                    }

                    EditorGUILayout.LabelField(BuildUpgradeTargetSummary(upgrade), DeucarianEditorStyles.MutedLabel);
                });
            }
        }

        private static GameContentAuthoringObjectPreviewOptions BuildPreviewOptions(GameContentSetAuthoringState source, GameContentSetProviderV2State state)
        {
            var preview = new GameContentAuthoringActionPreview
            {
                Mode = GameContentAuthoringActionPreviewMode.Area,
                RenderMode = state.PreviewRenderMode,
                Playing = state.PreviewPlaying,
                Loop = state.PreviewLoop,
                Speed = state.PreviewSpeed,
                StartTime = state.PreviewStartTime,
                StaticNormalizedTime = state.PausedNormalizedTime,
                Label = string.IsNullOrWhiteSpace(source.DisplayName) ? source.ContentSetId : source.DisplayName,
                DeliveryTypeLabel = "Playable Run",
                SourceContextLabel = GetAssetName(source.StartingWeapon, "Missing starting weapon"),
                TargetContextLabel = CountWaveEnemies(source.WaveSet).ToString(CultureInfo.InvariantCulture) + " enemies"
            };

            preview.Roles.Add(new GameContentAuthoringActionPreviewRole("Start", GetAssetName(source.StartingWeapon, "Missing weapon"), source.StartingWeapon));
            int roleCount = Math.Min(3, source.AvailableWeapons.Count);
            for (int i = 0; i < roleCount; i++)
            {
                WeaponDefinitionAsset weapon = source.AvailableWeapons[i];
                preview.Roles.Add(new GameContentAuthoringActionPreviewRole("Weapon " + (i + 1).ToString(CultureInfo.InvariantCulture), GetAssetName(weapon, "Missing weapon"), weapon));
            }

            return new GameContentAuthoringObjectPreviewOptions
            {
                MinimumHeight = 150f,
                ActionPreview = preview
            };
        }

        private static void DrawAssetList<TAsset>(
            string title,
            List<TAsset> assets,
            string addLabel,
            Func<TAsset, IReadOnlyList<DeucarianEditorStatusChip>> buildChips,
            Action<TAsset> drawDetails) where TAsset : UnityEngine.Object
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(title, DeucarianEditorStyles.SectionTitle);
                GUILayout.FlexibleSpace();
                if (DeucarianEditorButtons.Secondary(addLabel, true, GUILayout.Height(24f), GUILayout.Width(104f)))
                    assets.Add(null);
            }

            if (assets.Count == 0)
                EditorGUILayout.LabelField("None assigned.", DeucarianEditorStyles.MutedLabel);

            for (int i = 0; i < assets.Count; i++)
            {
                int index = i;
                bool remove = false;
                bool duplicate = false;
                bool moveUp = false;
                bool moveDown = false;
                DeucarianEditorCards.DrawInlineCard(() =>
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Item " + (index + 1).ToString(CultureInfo.InvariantCulture), DeucarianEditorStyles.SectionTitle);
                        GUILayout.FlexibleSpace();
                        if (DeucarianEditorMiniToolbar.Button("Up", index > 0, GUILayout.Width(38f), GUILayout.Height(22f)))
                            moveUp = true;
                        if (DeucarianEditorMiniToolbar.Button("Down", index < assets.Count - 1, GUILayout.Width(54f), GUILayout.Height(22f)))
                            moveDown = true;
                        if (DeucarianEditorMiniToolbar.Button("Copy", assets[index] != null, GUILayout.Width(50f), GUILayout.Height(22f)))
                            duplicate = true;
                        if (DeucarianEditorMiniToolbar.Button("Remove", true, GUILayout.Width(68f), GUILayout.Height(22f)))
                            remove = true;
                    }

                    if (remove)
                        return;

                    assets[index] = DrawObjectField("Asset", assets[index]);
                    DeucarianEditorStatusChipRow.Draw(buildChips == null ? Array.Empty<DeucarianEditorStatusChip>() : buildChips(assets[index]));
                    drawDetails?.Invoke(assets[index]);
                });

                if (remove)
                {
                    assets.RemoveAt(index);
                    i--;
                }
                else if (duplicate)
                {
                    assets.Insert(index + 1, assets[index]);
                    i++;
                }
                else if (moveUp)
                {
                    MoveAsset(assets, index, index - 1);
                }
                else if (moveDown)
                {
                    MoveAsset(assets, index, index + 1);
                }
            }
        }

        private static TAsset DrawObjectField<TAsset>(string label, TAsset value) where TAsset : UnityEngine.Object
        {
            TAsset result = value;
            DeucarianEditorFieldRow.Draw(label, () =>
            {
                result = EditorGUILayout.ObjectField(result, typeof(TAsset), false) as TAsset;
                DeucarianEditorMiniToolbar.PingButton(result);
            });
            return result;
        }

        private static void MoveAsset<TAsset>(List<TAsset> assets, int from, int to)
        {
            if (assets == null || from < 0 || to < 0 || from >= assets.Count || to >= assets.Count)
                return;

            TAsset asset = assets[from];
            assets.RemoveAt(from);
            assets.Insert(to, asset);
        }

        public static GameContentSetAuthoringState FromContentSetAsset(GameContentSetAsset asset)
        {
            var state = new GameContentSetAuthoringState();
            if (asset == null)
                return state;

            state.ContentSetId = asset.Id;
            state.DisplayName = asset.DisplayName;
            state.Description = asset.Description;
            state.Icon = asset.Icon;
            state.Banner = asset.Banner;
            state.StartingWeapon = asset.StartingWeapon;
            state.AvailableWeapons.Clear();
            state.AvailableWeapons.AddRange(asset.AvailableWeapons);
            state.EnemyPool.Clear();
            state.EnemyPool.AddRange(asset.EnemyPool);
            state.WaveSet.Clear();
            state.WaveSet.AddRange(asset.WaveSet);
            state.UpgradePool.Clear();
            state.UpgradePool.AddRange(asset.UpgradePool);
            state.StartingCredits = asset.StartingCredits;
            state.StartingParts = asset.StartingParts;
            state.RewardMultiplier = asset.RewardMultiplier;
            state.DifficultyMultiplier = asset.DifficultyMultiplier;
            state.SessionLengthTicks = asset.SessionLengthTicks;
            state.Endless = asset.Endless;
            state.TagsCsv = string.Join(", ", asset.Tags);
            state.OutputRoot = string.IsNullOrWhiteSpace(AssetDatabase.GetAssetPath(asset))
                ? state.OutputRoot
                : System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(asset)).Replace("\\", "/");
            return state;
        }

        public static string BuildStateFingerprint(GameContentSetAuthoringState state)
        {
            if (state == null)
                return string.Empty;

            var builder = new StringBuilder()
                .Append(state.ContentSetId).Append('|')
                .Append(state.DisplayName).Append('|')
                .Append(state.Description).Append('|')
                .Append(GetAssetId(state.Icon)).Append('|')
                .Append(GetAssetId(state.Banner)).Append('|')
                .Append(GetAssetId(state.StartingWeapon)).Append('|')
                .Append(state.StartingCredits.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(state.StartingParts.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(state.RewardMultiplier.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(state.DifficultyMultiplier.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(state.SessionLengthTicks.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(state.Endless ? "1" : "0").Append('|')
                .Append(state.TagsCsv);
            AppendAssetIds(builder, state.AvailableWeapons);
            AppendAssetIds(builder, state.EnemyPool);
            AppendAssetIds(builder, state.WaveSet);
            AppendAssetIds(builder, state.UpgradePool);
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

        public static int CountWaveEnemies(IReadOnlyList<WaveDefinitionAsset> waves)
        {
            if (waves == null) return 0;
            int count = 0;
            for (int i = 0; i < waves.Count; i++)
                count += CountWaveEnemies(waves[i]);
            return count;
        }

        public static int CountWaveEnemies(WaveDefinitionAsset wave)
        {
            if (wave == null || wave.Entries == null) return 0;
            int count = 0;
            IReadOnlyList<WaveEntryRecipe> entries = wave.Entries.Entries;
            for (int i = 0; i < entries.Count; i++)
                if (entries[i] != null)
                    count += Math.Max(0, entries[i].Count);
            return count;
        }

        public static int ApproximateDuration(IReadOnlyList<WaveDefinitionAsset> waves)
        {
            if (waves == null) return 0;
            int maxTick = 0;
            for (int i = 0; i < waves.Count; i++)
                maxTick = Math.Max(maxTick, ApproximateDuration(waves[i]));
            return maxTick;
        }

        public static int ApproximateDuration(WaveDefinitionAsset wave)
        {
            if (wave == null || wave.Schedule == null || wave.Entries == null) return 0;
            int maxTick = Math.Max(0, wave.Schedule.StartTick);
            IReadOnlyList<WaveEntryRecipe> entries = wave.Entries.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                WaveEntryRecipe entry = entries[i];
                if (entry == null) continue;
                int batches = Math.Max(1, (int)Math.Ceiling(entry.Count / (double)Math.Max(1, entry.BatchSize)));
                int endTick = wave.Schedule.StartTick + Math.Max(0, entry.InitialDelayTicks) + Math.Max(0, batches - 1) * Math.Max(0, entry.IntervalTicks);
                maxTick = Math.Max(maxTick, endTick);
            }

            return maxTick;
        }

        public static string BuildEnemyMixSummary(IReadOnlyList<WaveDefinitionAsset> waves)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (waves != null)
            {
                for (int i = 0; i < waves.Count; i++)
                    AddWaveEnemies(counts, waves[i]);
            }

            return FormatCounts(counts, "None");
        }

        public static string BuildEnemyMixSummary(WaveDefinitionAsset wave)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            AddWaveEnemies(counts, wave);
            return FormatCounts(counts, "None");
        }

        public static string BuildWeaponAttackSummary(WeaponDefinitionAsset weapon)
        {
            if (weapon == null) return "Missing weapon";
            if (weapon.Stats == null) return "Missing stats";
            if (weapon.Stats.Attack == null) return "Missing attack";
            return GetAssetName(weapon.Stats.Attack, weapon.Stats.Attack.Id);
        }

        public static string BuildUpgradeTargetSummary(RunUpgradeDefinitionAsset upgrade)
        {
            if (upgrade == null) return "Missing upgrade";
            if (upgrade.Effects == null || upgrade.Effects.Effects.Count == 0) return "No target effects";
            var targets = new List<string>();
            IReadOnlyList<RunUpgradeEffectRecipe> effects = upgrade.Effects.Effects;
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i] == null) continue;
                string target = effects[i].GetTargetId();
                if (!string.IsNullOrWhiteSpace(target) && !targets.Contains(target))
                    targets.Add(target);
            }

            return targets.Count == 0 ? "No target" : string.Join(", ", targets.ToArray());
        }

        private static string BuildUpgradeModifierSummary(RunUpgradeDefinitionAsset upgrade)
        {
            if (upgrade == null || upgrade.Effects == null || upgrade.Effects.Effects.Count == 0) return "No modifier";
            RunUpgradeEffectRecipe effect = upgrade.Effects.Effects[0];
            if (effect == null) return "No modifier";
            return effect.ModifierType + " " + effect.Amount.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static string BuildUpgradePoolSummary(GameContentSetAuthoringState state)
        {
            if (state == null || state.UpgradePool.Count == 0) return "No upgrades";
            return CountAssigned(state.UpgradePool).ToString(CultureInfo.InvariantCulture) + " upgrade(s) targeting " + CountUpgradeTargets(state.UpgradePool).ToString(CultureInfo.InvariantCulture) + " asset(s)";
        }

        private static int CountUpgradeTargets(IReadOnlyList<RunUpgradeDefinitionAsset> upgrades)
        {
            var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (upgrades == null) return 0;
            for (int i = 0; i < upgrades.Count; i++)
            {
                RunUpgradeDefinitionAsset upgrade = upgrades[i];
                if (upgrade == null || upgrade.Effects == null) continue;
                IReadOnlyList<RunUpgradeEffectRecipe> effects = upgrade.Effects.Effects;
                for (int j = 0; j < effects.Count; j++)
                {
                    string target = effects[j] == null ? string.Empty : effects[j].GetTargetId();
                    if (!string.IsNullOrWhiteSpace(target))
                        targets.Add(target.Trim());
                }
            }

            return targets.Count;
        }

        private static int CountEnemyWaveUses(EnemyDefinitionAsset enemy, IReadOnlyList<WaveDefinitionAsset> waves)
        {
            if (enemy == null || waves == null) return 0;
            int count = 0;
            for (int i = 0; i < waves.Count; i++)
            {
                WaveDefinitionAsset wave = waves[i];
                if (wave == null || wave.Entries == null) continue;
                IReadOnlyList<WaveEntryRecipe> entries = wave.Entries.Entries;
                for (int j = 0; j < entries.Count; j++)
                {
                    WaveEntryRecipe entry = entries[j];
                    if (entry != null && entry.Enemy == enemy)
                        count++;
                }
            }

            return count;
        }

        private static IReadOnlyList<DeucarianEditorStatusChip> BuildContentSetChips(GameContentSetAuthoringState state, GameContentAuthoringValidationResult validation, GameContentLibraryItem item)
        {
            return new[]
            {
                new DeucarianEditorStatusChip(BuildValidationSummary(validation), validation != null && validation.ErrorCount > 0 ? DeucarianEditorStatus.Error : validation != null && validation.WarningCount > 0 ? DeucarianEditorStatus.Warning : DeucarianEditorStatus.Success),
                new DeucarianEditorStatusChip(state.StartingWeapon == null ? "NoStart" : "Start", state.StartingWeapon == null ? DeucarianEditorStatus.Error : DeucarianEditorStatus.Success),
                new DeucarianEditorStatusChip(CountAssigned(state.AvailableWeapons).ToString(CultureInfo.InvariantCulture) + " weapon(s)", CountAssigned(state.AvailableWeapons) > 0 ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Error),
                new DeucarianEditorStatusChip(CountAssigned(state.WaveSet).ToString(CultureInfo.InvariantCulture) + " wave(s)", CountAssigned(state.WaveSet) > 0 ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Error),
                new DeucarianEditorStatusChip(CountWaveEnemies(state.WaveSet).ToString(CultureInfo.InvariantCulture) + " enemies", CountWaveEnemies(state.WaveSet) > 0 ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Error),
                new DeucarianEditorStatusChip(item == null ? "Draft" : BuildUsageSummary(item), item == null || item.ReverseReferences.Count == 0 ? DeucarianEditorStatus.Disabled : DeucarianEditorStatus.Success)
            };
        }

        private static IReadOnlyList<DeucarianEditorStatusChip> BuildWeaponChips(WeaponDefinitionAsset weapon)
        {
            bool hasAttack = weapon != null && weapon.Stats != null && weapon.Stats.Attack != null;
            return new[]
            {
                new DeucarianEditorStatusChip(weapon == null ? "Missing" : "Weapon", weapon == null ? DeucarianEditorStatus.Error : DeucarianEditorStatus.Success),
                new DeucarianEditorStatusChip(hasAttack ? "Attack" : "NoAttack", hasAttack ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Error),
                new DeucarianEditorStatusChip(weapon != null && weapon.Stats != null ? weapon.Stats.FireMode.ToString() : "NoStats", weapon != null && weapon.Stats != null ? DeucarianEditorStatus.Info : DeucarianEditorStatus.Error)
            };
        }

        private static IReadOnlyList<DeucarianEditorStatusChip> BuildEnemyChips(EnemyDefinitionAsset enemy, GameContentSetAuthoringState state)
        {
            return new[]
            {
                new DeucarianEditorStatusChip(enemy == null ? "Missing" : enemy.Role.ToString(), enemy == null ? DeucarianEditorStatus.Error : DeucarianEditorStatus.Info),
                new DeucarianEditorStatusChip(CountEnemyWaveUses(enemy, state.WaveSet).ToString(CultureInfo.InvariantCulture) + " use", enemy == null ? DeucarianEditorStatus.Disabled : DeucarianEditorStatus.Success)
            };
        }

        private static IReadOnlyList<DeucarianEditorStatusChip> BuildWaveChips(WaveDefinitionAsset wave)
        {
            bool ready = wave != null && wave.Entries != null && wave.Entries.Entries.Count > 0;
            return new[]
            {
                new DeucarianEditorStatusChip(ready ? "Ready" : "Blocked", ready ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Error),
                new DeucarianEditorStatusChip(CountWaveEnemies(wave).ToString(CultureInfo.InvariantCulture) + " enemies", CountWaveEnemies(wave) > 0 ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Error),
                new DeucarianEditorStatusChip(ApproximateDuration(wave).ToString(CultureInfo.InvariantCulture) + " ticks", DeucarianEditorStatus.Info)
            };
        }

        private static IReadOnlyList<DeucarianEditorStatusChip> BuildUpgradeChips(RunUpgradeDefinitionAsset upgrade, GameContentSetAuthoringState state)
        {
            bool targetInside = UpgradeTargetsInsideContentSet(upgrade, state);
            return new[]
            {
                new DeucarianEditorStatusChip(upgrade == null ? "Missing" : "Upgrade", upgrade == null ? DeucarianEditorStatus.Error : DeucarianEditorStatus.Success),
                new DeucarianEditorStatusChip(BuildUpgradeTargetSummary(upgrade) == "No target" ? "NoTarget" : "Target", BuildUpgradeTargetSummary(upgrade) == "No target" ? DeucarianEditorStatus.Warning : DeucarianEditorStatus.Success),
                new DeucarianEditorStatusChip(targetInside ? "InSet" : "External", targetInside ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Warning)
            };
        }

        private static bool UpgradeTargetsInsideContentSet(RunUpgradeDefinitionAsset upgrade, GameContentSetAuthoringState state)
        {
            if (upgrade == null || upgrade.Effects == null || state == null)
                return true;

            HashSet<string> known = BuildKnownTargetIds(state);
            IReadOnlyList<RunUpgradeEffectRecipe> effects = upgrade.Effects.Effects;
            for (int i = 0; i < effects.Count; i++)
            {
                string target = effects[i] == null ? string.Empty : effects[i].GetTargetId();
                if (!string.IsNullOrWhiteSpace(target) && !known.Contains(target.Trim()))
                    return false;
            }

            return true;
        }

        private static HashSet<string> BuildKnownTargetIds(GameContentSetAuthoringState state)
        {
            var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddIds(known, state.AvailableWeapons);
            AddIds(known, state.EnemyPool);
            for (int i = 0; i < state.AvailableWeapons.Count; i++)
            {
                WeaponDefinitionAsset weapon = state.AvailableWeapons[i];
                if (weapon == null || weapon.Stats == null) continue;
                if (weapon.Stats.Attack != null && !string.IsNullOrWhiteSpace(weapon.Stats.Attack.Id))
                    known.Add(weapon.Stats.Attack.Id.Trim());
                string projectile = weapon.Stats.ResolveProjectileDefinitionId();
                if (!string.IsNullOrWhiteSpace(projectile))
                    known.Add(projectile.Trim());
            }

            return known;
        }

        private static void AddIds<TAsset>(HashSet<string> ids, IReadOnlyList<TAsset> assets) where TAsset : UnityEngine.Object
        {
            if (assets == null) return;
            for (int i = 0; i < assets.Count; i++)
            {
                string id = GetContentId(assets[i]);
                if (!string.IsNullOrWhiteSpace(id))
                    ids.Add(id.Trim());
            }
        }

        private static GameContentAuthoringValidationResult ValidateDraft(GameContentSetAuthoringState draft)
        {
            GameContentSetAsset preview = GameContentSetAssetCreator.BuildTransient(draft);
            try
            {
                return GameContentSetAssetCreator.ValidateForCreation(draft, preview);
            }
            finally
            {
                GameContentSetAssetCreator.DestroyTransient(preview);
            }
        }

        private static GameContentSetValidationReport BuildValidationReport(GameContentSetAuthoringState state)
        {
            GameContentSetAsset preview = GameContentSetAssetCreator.BuildTransient(state);
            try
            {
                return GameContentSetValidator.Validate(preview);
            }
            finally
            {
                GameContentSetAssetCreator.DestroyTransient(preview);
            }
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

        private static IReadOnlyList<GameContentAuthoringPreviewRow> BuildDebugRows(GameContentSetAuthoringState state)
        {
            return new[]
            {
                Row("Raw Content Set ID", state.ContentSetId),
                Row("Output Root", state.OutputRoot),
                Row("Tags", state.TagsCsv),
                Row("Weapon IDs", JoinIds(state.AvailableWeapons)),
                Row("Enemy IDs", JoinIds(state.EnemyPool)),
                Row("Wave IDs", JoinIds(state.WaveSet)),
                Row("Upgrade IDs", JoinIds(state.UpgradePool))
            };
        }

        private static void DrawSummaryRows(params GameContentAuthoringPreviewRow[] rows)
        {
            for (int i = 0; i < rows.Length; i++)
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
            int packCount = 0;
            for (int i = 0; i < item.ReverseReferences.Count; i++)
            {
                GameContentLibraryItem target = item.ReverseReferences[i].Target;
                if (target != null && target.Kind == GameContentLibraryKind.ContentPack)
                    packCount++;
            }

            return packCount.ToString(CultureInfo.InvariantCulture) + " pack(s)";
        }

        private static string BuildPlayableRunSummary(GameContentSetAuthoringState state)
        {
            return CountAssigned(state.AvailableWeapons).ToString(CultureInfo.InvariantCulture) + " weapons, "
                + CountAssigned(state.WaveSet).ToString(CultureInfo.InvariantCulture) + " waves, "
                + CountWaveEnemies(state.WaveSet).ToString(CultureInfo.InvariantCulture) + " enemies";
        }

        private static string BuildAdvancedReport(GameContentLibraryItem item, GameContentSetAuthoringState state)
        {
            return "Content Set: " + state.DisplayName + Environment.NewLine
                + "ID: " + state.ContentSetId + Environment.NewLine
                + "Path: " + (item == null ? "(draft)" : item.Path) + Environment.NewLine
                + "Starting Weapon: " + GetAssetName(state.StartingWeapon, "Missing") + Environment.NewLine
                + "Weapons: " + JoinIds(state.AvailableWeapons) + Environment.NewLine
                + "Enemies: " + JoinIds(state.EnemyPool) + Environment.NewLine
                + "Waves: " + JoinIds(state.WaveSet) + Environment.NewLine
                + "Upgrades: " + JoinIds(state.UpgradePool);
        }

        private static UnityEngine.Object GetPrimaryPreviewAsset(GameContentSetAuthoringState state)
        {
            if (state == null) return null;
            if (state.Banner != null) return state.Banner;
            if (state.Icon != null) return state.Icon;
            if (state.StartingWeapon != null) return state.StartingWeapon;
            if (state.AvailableWeapons.Count > 0 && state.AvailableWeapons[0] != null) return state.AvailableWeapons[0];
            if (state.WaveSet.Count > 0 && state.WaveSet[0] != null) return state.WaveSet[0];
            if (state.EnemyPool.Count > 0 && state.EnemyPool[0] != null) return state.EnemyPool[0];
            if (state.UpgradePool.Count > 0 && state.UpgradePool[0] != null) return state.UpgradePool[0];
            return null;
        }

        private static void AddWaveEnemies(Dictionary<string, int> counts, WaveDefinitionAsset wave)
        {
            if (wave == null || wave.Entries == null) return;
            IReadOnlyList<WaveEntryRecipe> entries = wave.Entries.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                WaveEntryRecipe entry = entries[i];
                if (entry == null) continue;
                string label = entry.Enemy == null ? "Missing enemy" : GetAssetName(entry.Enemy, entry.Enemy.Id);
                int count = Math.Max(0, entry.Count);
                counts[label] = counts.ContainsKey(label) ? counts[label] + count : count;
            }
        }

        private static string FormatCounts(Dictionary<string, int> counts, string empty)
        {
            if (counts == null || counts.Count == 0) return empty;
            var parts = new List<string>();
            foreach (KeyValuePair<string, int> pair in counts)
                parts.Add(pair.Key + " x" + pair.Value.ToString(CultureInfo.InvariantCulture));
            return string.Join(", ", parts.ToArray());
        }

        private static string JoinIds<TAsset>(IReadOnlyList<TAsset> assets) where TAsset : UnityEngine.Object
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

        private static void AppendAssetIds<TAsset>(StringBuilder builder, IReadOnlyList<TAsset> assets) where TAsset : UnityEngine.Object
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
            if (asset is WeaponDefinitionAsset weapon) return weapon.Id;
            if (asset is EnemyDefinitionAsset enemy) return enemy.Id;
            if (asset is WaveDefinitionAsset wave) return wave.Id;
            if (asset is RunUpgradeDefinitionAsset upgrade) return upgrade.Id;
            if (asset is GameContentSetAsset set) return set.Id;
            return asset == null ? string.Empty : asset.name;
        }

        private static string GetAssetName(UnityEngine.Object asset, string empty)
        {
            if (asset == null) return empty;
            if (asset is WeaponDefinitionAsset weapon) return string.IsNullOrWhiteSpace(weapon.DisplayName) ? weapon.Id : weapon.DisplayName;
            if (asset is EnemyDefinitionAsset enemy) return string.IsNullOrWhiteSpace(enemy.DisplayName) ? enemy.Id : enemy.DisplayName;
            if (asset is WaveDefinitionAsset wave) return string.IsNullOrWhiteSpace(wave.DisplayName) ? wave.Id : wave.DisplayName;
            if (asset is RunUpgradeDefinitionAsset upgrade) return string.IsNullOrWhiteSpace(upgrade.DisplayName) ? upgrade.Id : upgrade.DisplayName;
            if (asset is GameContentSetAsset set) return string.IsNullOrWhiteSpace(set.DisplayName) ? set.Id : set.DisplayName;
            return asset.name;
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
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

    internal static class GameContentSetProviderV2PreviewModel
    {
        public const bool ExposesRedundantSelectButton = false;

        public static string GetScopeLabel(bool creating, bool unsaved)
        {
            if (creating)
                return "Draft";
            return unsaved ? "Unsaved" : "Selected";
        }

        public static IReadOnlyList<DeucarianEditorStatusChip> BuildChips(GameContentSetAuthoringState state, GameContentSetProviderV2State previewState, GameContentSetValidationReport report)
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
            return new[]
            {
                new DeucarianEditorStatusChip(debug ? "Debug" : "Game", debug ? DeucarianEditorStatus.Warning : DeucarianEditorStatus.Info),
                new DeucarianEditorStatusChip(readinessLabel, readiness),
                new DeucarianEditorStatusChip(GameContentSetProviderV2View.CountAssigned(state.AvailableWeapons).ToString(CultureInfo.InvariantCulture) + " weapons", GameContentSetProviderV2View.CountAssigned(state.AvailableWeapons) > 0 ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Error),
                new DeucarianEditorStatusChip(GameContentSetProviderV2View.CountAssigned(state.WaveSet).ToString(CultureInfo.InvariantCulture) + " waves", GameContentSetProviderV2View.CountAssigned(state.WaveSet) > 0 ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Error),
                new DeucarianEditorStatusChip(GameContentSetProviderV2View.CountWaveEnemies(state.WaveSet).ToString(CultureInfo.InvariantCulture) + " enemies", GameContentSetProviderV2View.CountWaveEnemies(state.WaveSet) > 0 ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Error),
                new DeucarianEditorStatusChip(speed.ToString("0.#", CultureInfo.InvariantCulture) + "x", DeucarianEditorStatus.Info)
            };
        }
    }

    internal sealed class GameContentSetProviderV2ListItem
    {
        private GameContentSetProviderV2ListItem(GameContentLibraryItem source, GameContentSetAsset asset)
        {
            Source = source;
            Asset = asset;
            StableId = asset == null ? source == null ? string.Empty : source.Id : asset.Id;
            DisplayName = asset == null ? source == null ? "Content Set" : source.DisplayName : asset.DisplayName;
            Tags = asset == null ? string.Empty : string.Join(", ", asset.Tags);
            GameContentSetAuthoringState state = asset == null ? new GameContentSetAuthoringState() : GameContentSetProviderV2View.FromContentSetAsset(asset);
            HasStartingWeapon = state.StartingWeapon != null;
            StartingWeaponLabel = HasStartingWeapon ? "Start" : "NoStart";
            WeaponCount = GameContentSetProviderV2View.CountAssigned(state.AvailableWeapons);
            WaveCount = GameContentSetProviderV2View.CountAssigned(state.WaveSet);
            EnemyCount = GameContentSetProviderV2View.CountWaveEnemies(state.WaveSet);
            UpgradeCount = GameContentSetProviderV2View.CountAssigned(state.UpgradePool);
            DurationTicks = GameContentSetProviderV2View.ApproximateDuration(state.WaveSet);
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
                WeaponCount.ToString(CultureInfo.InvariantCulture),
                WaveCount.ToString(CultureInfo.InvariantCulture),
                EnemyCount.ToString(CultureInfo.InvariantCulture),
                UpgradeCount.ToString(CultureInfo.InvariantCulture),
                DurationTicks.ToString(CultureInfo.InvariantCulture)
            });
        }

        public GameContentLibraryItem Source { get; }
        public GameContentSetAsset Asset { get; }
        public string StableId { get; }
        public string DisplayName { get; }
        public string Tags { get; }
        public bool HasStartingWeapon { get; }
        public string StartingWeaponLabel { get; }
        public int WeaponCount { get; }
        public int WaveCount { get; }
        public int EnemyCount { get; }
        public int UpgradeCount { get; }
        public int DurationTicks { get; }
        public string ReadinessLabel { get; }
        public DeucarianEditorStatus ReadinessStatus { get; }
        private string SearchText { get; }

        public static IReadOnlyList<GameContentSetProviderV2ListItem> Build(IReadOnlyList<GameContentLibraryItem> items)
        {
            if (items == null || items.Count == 0)
                return Array.Empty<GameContentSetProviderV2ListItem>();

            var result = new List<GameContentSetProviderV2ListItem>();
            for (int i = 0; i < items.Count; i++)
            {
                GameContentSetProviderV2ListItem item = FromItem(items[i]);
                if (item != null)
                    result.Add(item);
            }

            result.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase));
            return result;
        }

        public static GameContentSetProviderV2ListItem FromItem(GameContentLibraryItem item)
        {
            if (item == null || item.Kind != GameContentLibraryKind.ContentSet)
                return null;

            return new GameContentSetProviderV2ListItem(item, item.Asset as GameContentSetAsset);
        }

        public static GameContentSetProviderV2ListItem FromAssetForTests(GameContentSetAsset asset)
        {
            return new GameContentSetProviderV2ListItem(null, asset);
        }

        public bool Matches(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return true;

            return SearchText != null && SearchText.IndexOf(query.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
