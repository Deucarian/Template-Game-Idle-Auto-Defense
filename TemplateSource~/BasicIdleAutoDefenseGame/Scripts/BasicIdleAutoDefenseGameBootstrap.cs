using System;
using System.Globalization;
using System.IO;
using Deucarian.TemplateGameIdleAutoDefense;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.TemplateGameIdleAutoDefense.Samples
{
    public sealed class BasicIdleAutoDefenseGameBootstrap : IdleAutoDefenseTemplateController
    {
        private const float HudWidth = 390f;
        private const float HudMinimumHeight = 500f;
        [SerializeField] private GameContentPackAsset _templateContentPack;
        [SerializeField] private GameContentSetAsset _templateContentSet;
        private string _saveStatus = "No snapshot saved";
        private VisualElement _hudRoot;
        private Label _stateLabel;
        private Label _healthLabel;
        private Label _currencyLabel;
        private Label _rewardLabel;
        private Label _timeLabel;
        private Label _waveLabel;
        private Label _enemyLabel;
        private Label _killLabel;
        private Label _purchaseLabel;
        private Label _buildLabel;
        private Label _draftLabel;
        private Label _choiceFeedbackLabel;
        private VisualElement _draftOverlay;
        private VisualElement _draftOverlayPanel;
        private Label _draftOverlayTitle;
        private Label _draftOverlaySubtitle;
        private VisualElement _draftPanel;
        private readonly Button[] _draftButtons = new Button[3];
        private Label _saveLabel;
        private Label _resultLabel;
        private Button _damageButton;
        private Button _attackSpeedButton;
        private Button _rangeButton;
        private Button _repairButton;
        private Button _pulseButton;
        private Button _arcButton;
        private Button _homingButton;
        private Button _overdriveButton;
        private Button _saveButton;
        private Button _resetButton;
        private Button _restartButton;
        private string _choiceFeedback = string.Empty;
        private float _choiceFeedbackUntil;

        public bool UiToolkitHudReady { get; private set; }
        public bool UiToolkitHudVisible => _hudRoot != null &&
            _hudRoot.resolvedStyle.display != DisplayStyle.None &&
            _hudRoot.resolvedStyle.visibility == Visibility.Visible &&
            ResolveHudWidth() > 100f &&
            ResolveHudHeight() > 100f;
        public bool UiToolkitHudPaintReady => UiToolkitHudVisible &&
            UiToolkitHudLabelCount >= 9 &&
            UiToolkitHudButtonCount >= 9 &&
            _hudRoot.resolvedStyle.backgroundColor.a > 0.5f;
        public int UiToolkitHudLabelCount => CountHudElements<Label>();
        public int UiToolkitHudButtonCount => CountHudElements<Button>();
        public float UiToolkitHudResolvedWidth => ResolveHudWidth();
        public float UiToolkitHudResolvedHeight => ResolveHudHeight();

        protected override void Awake()
        {
            if (_templateContentPack != null || _templateContentSet != null)
                ConfigureContentPack(_templateContentPack, _templateContentSet);
            base.Awake();
            BuildUiToolkitHud();
            RefreshUiToolkitHud();
        }

        private void LateUpdate()
        {
            if (UiToolkitHudReady)
                RefreshUiToolkitHud();
        }

        protected override void Update()
        {
            base.Update();
            if (!RewardDraftActive) return;
            if (Input.GetKeyDown(KeyCode.Alpha1)) TryChooseRewardAndRefresh(0);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) TryChooseRewardAndRefresh(1);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) TryChooseRewardAndRefresh(2);
        }

        private void SaveSnapshot(string reason)
        {
            BasicIdleAutoDefenseSampleSave.WriteSnapshot(reason, this);
            _saveStatus = "Saved " + DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
            RefreshUiToolkitHud();
        }

        private void ResetSave()
        {
            bool existed = BasicIdleAutoDefenseSampleSave.Reset();
            _saveStatus = existed ? "Reset saved snapshot" : "No snapshot to reset";
            RefreshUiToolkitHud();
        }

        private void BuildUiToolkitHud()
        {
            VisualElement root = RuntimeUiRoot;
            _hudRoot?.RemoveFromHierarchy();
            _hudRoot = new VisualElement { name = "idle-auto-defense-hud" };
            _hudRoot.pickingMode = PickingMode.Position;
            _hudRoot.style.display = DisplayStyle.Flex;
            _hudRoot.style.position = Position.Absolute;
            _hudRoot.style.left = 12;
            _hudRoot.style.top = 12;
            _hudRoot.style.width = HudWidth;
            _hudRoot.style.minWidth = HudWidth;
            _hudRoot.style.maxWidth = HudWidth;
            _hudRoot.style.minHeight = HudMinimumHeight;
            _hudRoot.style.maxHeight = Length.Percent(96);
            _hudRoot.style.flexDirection = FlexDirection.Column;
            _hudRoot.style.flexShrink = 0;
            _hudRoot.style.opacity = 1f;
            _hudRoot.style.visibility = Visibility.Visible;
            _hudRoot.style.overflow = Overflow.Visible;
            _hudRoot.style.paddingLeft = 10;
            _hudRoot.style.paddingRight = 10;
            _hudRoot.style.paddingTop = 8;
            _hudRoot.style.paddingBottom = 8;
            _hudRoot.style.backgroundColor = new Color(0.035f, 0.045f, 0.055f, 0.94f);
            _hudRoot.style.borderTopColor = new Color(0.25f, 0.88f, 1f, 0.95f);
            _hudRoot.style.borderBottomColor = new Color(0.18f, 0.28f, 0.34f, 0.95f);
            _hudRoot.style.borderLeftColor = new Color(0.18f, 0.28f, 0.34f, 0.95f);
            _hudRoot.style.borderRightColor = new Color(0.18f, 0.28f, 0.34f, 0.95f);
            _hudRoot.style.borderTopWidth = 3;
            _hudRoot.style.borderBottomWidth = 1;
            _hudRoot.style.borderLeftWidth = 1;
            _hudRoot.style.borderRightWidth = 1;
            _hudRoot.style.borderTopLeftRadius = 8;
            _hudRoot.style.borderTopRightRadius = 8;
            _hudRoot.style.borderBottomLeftRadius = 8;
            _hudRoot.style.borderBottomRightRadius = 8;
            root.Insert(0, _hudRoot);

            Label title = AddLabel(_hudRoot, "Idle Auto Defense", 20, FontStyle.Bold);
            title.style.marginBottom = 4;
            title.style.color = new Color(0.72f, 0.96f, 1f, 1f);
            _stateLabel = AddLabel(_hudRoot);
            _healthLabel = AddLabel(_hudRoot);
            _currencyLabel = AddLabel(_hudRoot);
            _rewardLabel = AddLabel(_hudRoot);
            _timeLabel = AddLabel(_hudRoot);
            _waveLabel = AddLabel(_hudRoot);
            _enemyLabel = AddLabel(_hudRoot);
            _killLabel = AddLabel(_hudRoot);
            _purchaseLabel = AddLabel(_hudRoot);
            _buildLabel = AddLabel(_hudRoot);

            AddSectionTitle(_hudRoot, "Rewards");
            _draftLabel = AddLabel(_hudRoot);
            _choiceFeedbackLabel = AddLabel(_hudRoot, string.Empty, 14, FontStyle.Bold);

            AddSectionTitle(_hudRoot, "Upgrades");
            _damageButton = AddButton(_hudRoot, () => TryPurchaseAndRefresh(TryPurchaseDamageUpgrade));
            _attackSpeedButton = AddButton(_hudRoot, () => TryPurchaseAndRefresh(TryPurchaseAttackSpeedUpgrade));
            _rangeButton = AddButton(_hudRoot, () => TryPurchaseAndRefresh(TryPurchaseRangeUpgrade));
            _repairButton = AddButton(_hudRoot, () => TryPurchaseAndRefresh(TryPurchaseRepairUpgrade));

            AddSectionTitle(_hudRoot, "Modules");
            _pulseButton = AddButton(_hudRoot, () => TryPurchaseAndRefresh(TryPurchasePulseBeamModule));
            _arcButton = AddButton(_hudRoot, () => TryPurchaseAndRefresh(TryPurchaseArcBurstModule));
            _homingButton = AddButton(_hudRoot, () => TryPurchaseAndRefresh(TryPurchaseHomingPulseModule));

            AddSectionTitle(_hudRoot, "Active");
            _overdriveButton = AddButton(_hudRoot, () => TryPurchaseAndRefresh(TryPurchaseOverdrive));

            AddSectionTitle(_hudRoot, "Save");
            _saveLabel = AddLabel(_hudRoot);
            VisualElement saveRow = AddRow(_hudRoot);
            _saveButton = AddButton(saveRow, () => SaveSnapshot("manual"), "Save Snapshot");
            _resetButton = AddButton(saveRow, ResetSave, "Reset Save");

            _resultLabel = AddLabel(_hudRoot, string.Empty, 16, FontStyle.Bold);
            _resultLabel.style.marginTop = 8;
            _restartButton = AddButton(_hudRoot, () =>
            {
                RestartRun();
                RefreshUiToolkitHud();
            }, "Restart Run");

            UiToolkitHudReady = true;
            BuildRewardDraftOverlay(root);
        }

        private void BuildRewardDraftOverlay(VisualElement root)
        {
            _draftOverlay?.RemoveFromHierarchy();
            _draftOverlay = new VisualElement { name = "reward-draft-overlay" };
            _draftOverlay.pickingMode = PickingMode.Position;
            _draftOverlay.style.display = DisplayStyle.None;
            _draftOverlay.style.position = Position.Absolute;
            _draftOverlay.style.left = 0;
            _draftOverlay.style.top = 0;
            _draftOverlay.style.right = 0;
            _draftOverlay.style.bottom = 0;
            _draftOverlay.style.flexDirection = FlexDirection.Column;
            _draftOverlay.style.justifyContent = Justify.Center;
            _draftOverlay.style.alignItems = Align.Center;
            _draftOverlay.style.backgroundColor = new Color(0.005f, 0.01f, 0.015f, 0.74f);
            _draftOverlay.style.paddingLeft = 32;
            _draftOverlay.style.paddingRight = 32;
            _draftOverlay.style.paddingTop = 26;
            _draftOverlay.style.paddingBottom = 26;
            root.Add(_draftOverlay);

            _draftOverlayPanel = new VisualElement { name = "reward-draft-cards" };
            _draftOverlayPanel.style.width = Length.Percent(92);
            _draftOverlayPanel.style.maxWidth = 1060;
            _draftOverlayPanel.style.minHeight = 390;
            _draftOverlayPanel.style.flexDirection = FlexDirection.Column;
            _draftOverlayPanel.style.paddingLeft = 22;
            _draftOverlayPanel.style.paddingRight = 22;
            _draftOverlayPanel.style.paddingTop = 18;
            _draftOverlayPanel.style.paddingBottom = 22;
            _draftOverlayPanel.style.backgroundColor = new Color(0.035f, 0.045f, 0.06f, 0.98f);
            _draftOverlayPanel.style.borderTopWidth = 4;
            _draftOverlayPanel.style.borderBottomWidth = 2;
            _draftOverlayPanel.style.borderLeftWidth = 2;
            _draftOverlayPanel.style.borderRightWidth = 2;
            _draftOverlayPanel.style.borderTopColor = new Color(0.45f, 0.95f, 1f, 1f);
            _draftOverlayPanel.style.borderBottomColor = new Color(0.12f, 0.24f, 0.30f, 1f);
            _draftOverlayPanel.style.borderLeftColor = new Color(0.20f, 0.42f, 0.50f, 1f);
            _draftOverlayPanel.style.borderRightColor = new Color(0.20f, 0.42f, 0.50f, 1f);
            _draftOverlayPanel.style.borderTopLeftRadius = 10;
            _draftOverlayPanel.style.borderTopRightRadius = 10;
            _draftOverlayPanel.style.borderBottomLeftRadius = 10;
            _draftOverlayPanel.style.borderBottomRightRadius = 10;
            _draftOverlay.Add(_draftOverlayPanel);

            _draftOverlayTitle = AddLabel(_draftOverlayPanel, "Choose an Upgrade", 30, FontStyle.Bold);
            _draftOverlayTitle.style.unityTextAlign = TextAnchor.MiddleCenter;
            _draftOverlayTitle.style.color = new Color(0.78f, 0.96f, 1f, 1f);
            _draftOverlaySubtitle = AddLabel(_draftOverlayPanel, string.Empty, 16, FontStyle.Bold);
            _draftOverlaySubtitle.style.unityTextAlign = TextAnchor.MiddleCenter;
            _draftOverlaySubtitle.style.color = new Color(0.92f, 0.96f, 1f, 1f);

            _draftPanel = new VisualElement { name = "reward-draft-card-row" };
            _draftPanel.style.flexDirection = FlexDirection.Row;
            _draftPanel.style.width = Length.Percent(100);
            _draftPanel.style.marginTop = 18;
            _draftPanel.style.alignItems = Align.Stretch;
            _draftOverlayPanel.Add(_draftPanel);

            for (int i = 0; i < _draftButtons.Length; i++)
            {
                int index = i;
                _draftButtons[i] = AddButton(_draftPanel, () => TryChooseRewardAndRefresh(index));
                _draftButtons[i].name = "reward-card-" + (i + 1).ToString(CultureInfo.InvariantCulture);
                _draftButtons[i].style.height = 232;
                _draftButtons[i].style.minHeight = 232;
                _draftButtons[i].style.flexBasis = 0;
                _draftButtons[i].style.flexGrow = 1;
                _draftButtons[i].style.marginLeft = 7;
                _draftButtons[i].style.marginRight = 7;
                _draftButtons[i].style.fontSize = 17;
                _draftButtons[i].style.whiteSpace = WhiteSpace.Normal;
                _draftButtons[i].style.unityTextAlign = TextAnchor.MiddleLeft;
                _draftButtons[i].style.paddingLeft = 18;
                _draftButtons[i].style.paddingRight = 18;
                _draftButtons[i].style.borderTopWidth = 5;
                _draftButtons[i].style.borderBottomWidth = 3;
                _draftButtons[i].style.borderLeftWidth = 3;
                _draftButtons[i].style.borderRightWidth = 3;
            }
        }

        private void RefreshUiToolkitHud()
        {
            if (_hudRoot == null) return;

            _stateLabel.text = "State: " + RuntimeStateName + "  Time: " + SurvivalSeconds.ToString("0", CultureInfo.InvariantCulture) + "s";
            _healthLabel.text = "Tower HP: " + ObjectiveHealthText + "  Lives: " + ObjectiveLivesRemaining +
                "  Credits: " + RuntimeCurrency.ToString(CultureInfo.InvariantCulture);
            _currencyLabel.text = string.Empty;
            _currencyLabel.style.display = DisplayStyle.None;
            _rewardLabel.text = "Banked: " + EncounterRewardCredits.ToString(CultureInfo.InvariantCulture) + " credits / " + EncounterRewardParts.ToString(CultureInfo.InvariantCulture) + " parts";
            _timeLabel.text = string.Empty;
            _timeLabel.style.display = DisplayStyle.None;
            _waveLabel.text = "Wave: " + CurrentSpawnProfileName;
            _enemyLabel.text = "Enemies: " + ActiveEnemyCount + " active / " + SpawnedCount + " spawned";
            _killLabel.text = "Kills: " + (DirectOrCombatKillCount + ProjectileAdapterKillCount).ToString(CultureInfo.InvariantCulture) +
                "  Projectiles: " + ProjectileLaunchCount.ToString(CultureInfo.InvariantCulture);
            _purchaseLabel.text = "Purchases: " + SelectedUpgradeCount.ToString(CultureInfo.InvariantCulture) +
                "  Modules: " + UnlockedModuleCount.ToString(CultureInfo.InvariantCulture) + "/4  Tower Hits: " + ObjectiveDamageEvents.ToString(CultureInfo.InvariantCulture) +
                (OverdriveActive ? "  OVERDRIVE " + OverdriveSecondsRemaining.ToString("0", CultureInfo.InvariantCulture) + "s" : string.Empty);
            _buildLabel.text = "Build: Shard Launcher" +
                (PulseBeamUnlocked ? " + Pulse Beam" : string.Empty) +
                (ArcBurstUnlocked ? " + Arc Burst" : string.Empty) +
                (HomingPulseUnlocked ? " + Homing Pulse" : string.Empty) +
                "  Level " + CommanderLevel.ToString(CultureInfo.InvariantCulture) +
                "  Drafts " + RewardDraftSelectionCount.ToString(CultureInfo.InvariantCulture);
            bool showFeedback = !string.IsNullOrEmpty(_choiceFeedback) && Time.realtimeSinceStartup < _choiceFeedbackUntil;
            _choiceFeedbackLabel.text = showFeedback ? _choiceFeedback : string.Empty;
            _choiceFeedbackLabel.style.display = showFeedback ? DisplayStyle.Flex : DisplayStyle.None;
            _choiceFeedbackLabel.style.color = new Color(1f, 0.88f, 0.35f, 1f);
            RefreshRewardDraftPanel();

            SetUpgradeButton(_damageButton, "Damage", DamageUpgradeRank, DamageUpgradeCost, CanPurchaseDamageUpgrade);
            SetUpgradeButton(_attackSpeedButton, "Fire Rate", AttackSpeedUpgradeRank, AttackSpeedUpgradeCost, CanPurchaseAttackSpeedUpgrade);
            SetUpgradeButton(_rangeButton, "Range", RangeUpgradeRank, RangeUpgradeCost, CanPurchaseRangeUpgrade);
            SetUpgradeButton(_repairButton, "Repair / Max HP", RepairUpgradeRank, RepairUpgradeCost, CanPurchaseRepairUpgrade);

            SetModuleButton(_pulseButton, "Pulse Beam", PulseBeamUnlocked, PulseBeamUnlockCost, CanPurchasePulseBeamModule);
            SetModuleButton(_arcButton, "Arc Burst", ArcBurstUnlocked, ArcBurstUnlockCost, CanPurchaseArcBurstModule);
            SetModuleButton(_homingButton, "Homing Pulse", HomingPulseUnlocked, HomingPulseUnlockCost, CanPurchaseHomingPulseModule);
            SetOverdriveButton(_overdriveButton, OverdriveActive, OverdriveSecondsRemaining, OverdriveCooldownSecondsRemaining, OverdriveCost, CanPurchaseOverdrive);

            _saveLabel.text = "Save: " + _saveStatus + (BasicIdleAutoDefenseSampleSave.HasSave ? " (file present)" : string.Empty);
            _saveButton.SetEnabled(true);
            _resetButton.SetEnabled(true);

            bool terminal = EncounterCompleted || EncounterFailed;
            _resultLabel.text = EncounterCompleted ? "Run complete" : EncounterFailed ? "Tower destroyed" : string.Empty;
            _resultLabel.style.display = terminal ? DisplayStyle.Flex : DisplayStyle.None;
            _restartButton.style.display = terminal ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void TryPurchaseAndRefresh(Func<bool> purchase)
        {
            purchase?.Invoke();
            RefreshUiToolkitHud();
        }

        private void TryChooseRewardAndRefresh(int choiceIndex)
        {
            string selectedName = RewardDraftActive && choiceIndex >= 0 && choiceIndex < RewardDraftChoices.Count
                ? RewardDraftChoices[choiceIndex].DisplayName
                : string.Empty;
            if (TryChooseRewardDraftChoice(choiceIndex))
            {
                _choiceFeedback = "Selected: " + selectedName;
                _choiceFeedbackUntil = Time.realtimeSinceStartup + 3.5f;
            }

            RefreshUiToolkitHud();
        }

        private void RefreshRewardDraftPanel()
        {
            if (_draftLabel == null || _draftOverlay == null || _draftPanel == null) return;
            bool active = RewardDraftActive;
            _draftLabel.text = active
                ? "REWARD READY: choose 1 / 2 / 3 or click a card"
                : "Level " + CommanderLevel.ToString(CultureInfo.InvariantCulture) +
                  "  XP " + CommanderExperience.ToString(CultureInfo.InvariantCulture) + "/" + ExperienceToNextLevel.ToString(CultureInfo.InvariantCulture);
            _draftOverlay.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
            if (_draftOverlayTitle != null)
                _draftOverlayTitle.text = active ? ActiveRewardDraftKindName + " Reward" : "Choose an Upgrade";
            if (_draftOverlaySubtitle != null)
                _draftOverlaySubtitle.text = "Level " + CommanderLevel.ToString(CultureInfo.InvariantCulture) +
                    "  XP " + CommanderExperience.ToString(CultureInfo.InvariantCulture) + "/" + ExperienceToNextLevel.ToString(CultureInfo.InvariantCulture) +
                    (active ? "  -  Click a card or press 1 / 2 / 3" : string.Empty);
            for (int i = 0; i < _draftButtons.Length; i++)
            {
                Button button = _draftButtons[i];
                if (button == null) continue;
                if (!active || i >= RewardDraftChoices.Count)
                {
                    button.style.display = DisplayStyle.None;
                    button.SetEnabled(false);
                    continue;
                }

                IdleAutoDefenseRewardDraftChoice choice = RewardDraftChoices[i];
                button.style.display = DisplayStyle.Flex;
                button.text = choice.HotkeyLabel + "\n" +
                    choice.RarityName.ToUpperInvariant() + "  -  " + choice.TypeName + "\n\n" +
                    choice.DisplayName + "\n" +
                    choice.TargetName + "\n\n" +
                    choice.EffectDescription;
                button.SetEnabled(true);
                Color rarityColor = ResolveRarityColor(choice.Rarity);
                button.style.backgroundColor = new Color(rarityColor.r * 0.18f, rarityColor.g * 0.18f, rarityColor.b * 0.18f, 0.98f);
                button.style.borderTopColor = rarityColor;
                button.style.borderBottomColor = rarityColor;
                button.style.borderLeftColor = rarityColor;
                button.style.borderRightColor = rarityColor;
                button.style.color = Color.white;
                button.style.opacity = 1f;
            }
        }

        private static VisualElement AddRow(VisualElement parent)
        {
            var row = new VisualElement();
            row.style.width = Length.Percent(100);
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginTop = 1;
            row.style.marginBottom = 1;
            parent.Add(row);
            return row;
        }

        private static Label AddLabel(VisualElement parent, string text = "", int fontSize = 13, FontStyle fontStyle = FontStyle.Normal)
        {
            var label = new Label(text);
            ApplyRuntimeUiFont(label);
            label.style.color = new Color(0.92f, 0.96f, 1f);
            label.style.fontSize = fontSize;
            label.style.unityFontStyleAndWeight = fontStyle;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.minHeight = Mathf.Max(16, fontSize + 4);
            label.style.marginTop = 0;
            label.style.marginBottom = 1;
            parent.Add(label);
            return label;
        }

        private static void AddSectionTitle(VisualElement parent, string text)
        {
            Label label = AddLabel(parent, text, 14, FontStyle.Bold);
            label.style.marginTop = 6;
            label.style.marginBottom = 2;
            label.style.color = new Color(0.65f, 0.9f, 1f);
        }

        private static Button AddButton(VisualElement parent, Action clicked, string text = "")
        {
            var button = new Button(clicked) { text = text };
            ApplyRuntimeUiFont(button);
            button.style.height = 27;
            button.style.minHeight = 27;
            button.style.minWidth = 112;
            button.style.marginTop = 1;
            button.style.marginBottom = 1;
            button.style.marginLeft = 1;
            button.style.marginRight = 1;
            button.style.flexGrow = 1;
            button.style.fontSize = 11;
            button.style.color = Color.white;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.unityTextAlign = TextAnchor.MiddleCenter;
            button.style.backgroundColor = new Color(0.12f, 0.23f, 0.30f, 0.98f);
            button.style.borderTopColor = new Color(0.38f, 0.78f, 0.9f, 1f);
            button.style.borderBottomColor = new Color(0.05f, 0.12f, 0.16f, 1f);
            button.style.borderLeftColor = new Color(0.25f, 0.48f, 0.56f, 1f);
            button.style.borderRightColor = new Color(0.25f, 0.48f, 0.56f, 1f);
            button.style.borderTopWidth = 1;
            button.style.borderBottomWidth = 1;
            button.style.borderLeftWidth = 1;
            button.style.borderRightWidth = 1;
            button.style.borderTopLeftRadius = 5;
            button.style.borderTopRightRadius = 5;
            button.style.borderBottomLeftRadius = 5;
            button.style.borderBottomRightRadius = 5;
            button.style.paddingLeft = 8;
            button.style.paddingRight = 8;
            parent.Add(button);
            return button;
        }

        private float ResolveHudWidth()
        {
            if (_hudRoot == null) return 0f;
            float resolved = _hudRoot.resolvedStyle.width;
            if (!float.IsNaN(resolved) && resolved > 1f) return resolved;
            return HudWidth;
        }

        private float ResolveHudHeight()
        {
            if (_hudRoot == null) return 0f;
            float resolved = _hudRoot.resolvedStyle.height;
            if (!float.IsNaN(resolved) && resolved > 1f) return resolved;
            return HudMinimumHeight;
        }

        private static void SetUpgradeButton(Button button, string label, int rank, int cost, bool enabled)
        {
            button.text = label + "  Lv " + rank.ToString(CultureInfo.InvariantCulture) + "  " + cost.ToString(CultureInfo.InvariantCulture);
            button.SetEnabled(enabled);
            ApplyButtonState(button, enabled, false);
        }

        private static void SetModuleButton(Button button, string label, bool unlocked, int cost, bool enabled)
        {
            button.text = label + "  " + (unlocked ? "Unlocked" : cost.ToString(CultureInfo.InvariantCulture));
            button.SetEnabled(enabled);
            ApplyButtonState(button, enabled, unlocked);
        }

        private static void SetOverdriveButton(Button button, bool active, float activeSeconds, float cooldownSeconds, int cost, bool enabled)
        {
            if (button == null) return;
            if (active)
                button.text = "Overdrive  " + activeSeconds.ToString("0", CultureInfo.InvariantCulture) + "s";
            else if (cooldownSeconds > 0.1f)
                button.text = "Overdrive  " + cooldownSeconds.ToString("0", CultureInfo.InvariantCulture) + "s";
            else
                button.text = "Overdrive  " + cost.ToString(CultureInfo.InvariantCulture);
            button.SetEnabled(enabled);
            ApplyButtonState(button, enabled, active);
            if (active)
            {
                button.style.backgroundColor = new Color(0.34f, 0.22f, 0.05f, 0.98f);
                button.style.borderTopColor = new Color(1f, 0.84f, 0.22f, 1f);
            }
        }

        private static void ApplyButtonState(Button button, bool enabled, bool complete)
        {
            if (button == null) return;
            button.style.opacity = enabled || complete ? 1f : 0.68f;
            button.style.color = enabled || complete ? Color.white : new Color(0.76f, 0.82f, 0.86f, 1f);
            button.style.backgroundColor = complete
                ? new Color(0.12f, 0.34f, 0.22f, 0.98f)
                : enabled
                    ? new Color(0.12f, 0.23f, 0.30f, 0.98f)
                    : new Color(0.08f, 0.11f, 0.13f, 0.95f);
            button.style.borderTopColor = complete
                ? new Color(0.45f, 1f, 0.72f, 1f)
                : new Color(0.38f, 0.78f, 0.9f, 1f);
        }

        private static Color ResolveRarityColor(IdleAutoDefenseRewardRarity rarity)
        {
            if (rarity == IdleAutoDefenseRewardRarity.Legendary) return new Color(1f, 0.78f, 0.16f, 1f);
            if (rarity == IdleAutoDefenseRewardRarity.Epic) return new Color(0.78f, 0.42f, 1f, 1f);
            if (rarity == IdleAutoDefenseRewardRarity.Rare) return new Color(0.25f, 0.65f, 1f, 1f);
            if (rarity == IdleAutoDefenseRewardRarity.Uncommon) return new Color(0.35f, 1f, 0.55f, 1f);
            return new Color(0.92f, 0.96f, 1f, 1f);
        }

        private int CountHudElements<T>() where T : VisualElement
        {
            if (_hudRoot == null) return 0;
            int count = 0;
            _hudRoot.Query<T>().ForEach(_ => count++);
            return count;
        }
    }

    public static class BasicIdleAutoDefenseSampleSave
    {
        private const string SampleFolderName = "IdleAutoDefenseTemplateSample";
        private const string SampleFileName = "sample-state.json";

        public static string SaveDirectoryPath => Path.Combine(Application.persistentDataPath, "Deucarian", SampleFolderName);
        public static string SaveFilePath => Path.Combine(SaveDirectoryPath, SampleFileName);
        public static bool HasSave => File.Exists(SaveFilePath);

        public static void WriteSnapshot(string reason, IdleAutoDefenseTemplateController controller)
        {
            if (controller == null) throw new ArgumentNullException(nameof(controller));
            Directory.CreateDirectory(SaveDirectoryPath);
            File.WriteAllText(SaveFilePath, CreateSnapshotJson(reason, controller));
        }

        public static bool Reset()
        {
            bool existed = Directory.Exists(SaveDirectoryPath);
            if (existed)
                Directory.Delete(SaveDirectoryPath, true);
            return existed;
        }

        private static string CreateSnapshotJson(string reason, IdleAutoDefenseTemplateController controller)
        {
            return "{\n" +
                   "  \"reason\": \"" + Escape(reason) + "\",\n" +
                   "  \"savedUtc\": \"" + DateTimeOffset.UtcNow.ToString("O") + "\",\n" +
                   "  \"runtimeState\": \"" + controller.RuntimeStateName + "\",\n" +
                   "  \"spawned\": " + controller.SpawnedCount + ",\n" +
                   "  \"kills\": " + (controller.DirectOrCombatKillCount + controller.ProjectileAdapterKillCount) + ",\n" +
                   "  \"projectileLaunches\": " + controller.ProjectileLaunchCount + ",\n" +
                   "  \"selectedUpgrades\": " + controller.SelectedUpgradeCount + ",\n" +
                   "  \"objectiveHits\": " + controller.ObjectiveDamageEvents + "\n" +
                   "}\n";
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
