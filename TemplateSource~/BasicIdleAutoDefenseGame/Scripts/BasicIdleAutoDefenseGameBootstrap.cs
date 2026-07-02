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
        private Label _saveLabel;
        private Label _resultLabel;
        private Button _damageButton;
        private Button _attackSpeedButton;
        private Button _rangeButton;
        private Button _repairButton;
        private Button _pulseButton;
        private Button _arcButton;
        private Button _homingButton;
        private Button _saveButton;
        private Button _resetButton;
        private Button _restartButton;

        public bool UiToolkitHudReady { get; private set; }
        public bool UiToolkitHudVisible => _hudRoot != null &&
            _hudRoot.resolvedStyle.display != DisplayStyle.None &&
            _hudRoot.resolvedStyle.width > 1f &&
            _hudRoot.resolvedStyle.height > 1f;
        public int UiToolkitHudLabelCount => CountHudElements<Label>();
        public int UiToolkitHudButtonCount => CountHudElements<Button>();
        public float UiToolkitHudResolvedWidth => _hudRoot == null ? 0f : _hudRoot.resolvedStyle.width;
        public float UiToolkitHudResolvedHeight => _hudRoot == null ? 0f : _hudRoot.resolvedStyle.height;

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
            _hudRoot.style.width = 390;
            _hudRoot.style.minWidth = 340;
            _hudRoot.style.maxWidth = 430;
            _hudRoot.style.maxHeight = Length.Percent(96);
            _hudRoot.style.flexDirection = FlexDirection.Column;
            _hudRoot.style.overflow = Overflow.Visible;
            _hudRoot.style.paddingLeft = 12;
            _hudRoot.style.paddingRight = 12;
            _hudRoot.style.paddingTop = 10;
            _hudRoot.style.paddingBottom = 10;
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

            Label title = AddLabel(_hudRoot, "Idle Auto Defense", 22, FontStyle.Bold);
            title.style.marginBottom = 6;
            _stateLabel = AddLabel(_hudRoot);
            _healthLabel = AddLabel(_hudRoot);
            _currencyLabel = AddLabel(_hudRoot);
            _rewardLabel = AddLabel(_hudRoot);
            _timeLabel = AddLabel(_hudRoot);
            _waveLabel = AddLabel(_hudRoot);
            _enemyLabel = AddLabel(_hudRoot);
            _killLabel = AddLabel(_hudRoot);
            _purchaseLabel = AddLabel(_hudRoot);

            AddSectionTitle(_hudRoot, "Upgrades");
            _damageButton = AddButton(_hudRoot, () => TryPurchaseAndRefresh(TryPurchaseDamageUpgrade));
            _attackSpeedButton = AddButton(_hudRoot, () => TryPurchaseAndRefresh(TryPurchaseAttackSpeedUpgrade));
            _rangeButton = AddButton(_hudRoot, () => TryPurchaseAndRefresh(TryPurchaseRangeUpgrade));
            _repairButton = AddButton(_hudRoot, () => TryPurchaseAndRefresh(TryPurchaseRepairUpgrade));

            AddSectionTitle(_hudRoot, "Modules");
            _pulseButton = AddButton(_hudRoot, () => TryPurchaseAndRefresh(TryPurchasePulseBeamModule));
            _arcButton = AddButton(_hudRoot, () => TryPurchaseAndRefresh(TryPurchaseArcBurstModule));
            _homingButton = AddButton(_hudRoot, () => TryPurchaseAndRefresh(TryPurchaseHomingPulseModule));

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
        }

        private void RefreshUiToolkitHud()
        {
            if (_hudRoot == null) return;

            _stateLabel.text = "State: " + RuntimeStateName;
            _healthLabel.text = "Tower HP: " + ObjectiveHealthText + "  Lives: " + ObjectiveLivesRemaining;
            _currencyLabel.text = "Credits: " + RuntimeCurrency.ToString(CultureInfo.InvariantCulture);
            _rewardLabel.text = "Banked: " + EncounterRewardCredits.ToString(CultureInfo.InvariantCulture) + " credits / " + EncounterRewardParts.ToString(CultureInfo.InvariantCulture) + " parts";
            _timeLabel.text = "Time: " + SurvivalSeconds.ToString("0", CultureInfo.InvariantCulture) + "s";
            _waveLabel.text = "Wave: " + CurrentSpawnProfileName;
            _enemyLabel.text = "Enemies: " + ActiveEnemyCount + " active / " + SpawnedCount + " spawned";
            _killLabel.text = "Kills: " + (DirectOrCombatKillCount + ProjectileAdapterKillCount).ToString(CultureInfo.InvariantCulture) +
                "  Projectiles: " + ProjectileLaunchCount.ToString(CultureInfo.InvariantCulture);
            _purchaseLabel.text = "Purchases: " + SelectedUpgradeCount.ToString(CultureInfo.InvariantCulture) +
                "  Modules: " + UnlockedModuleCount.ToString(CultureInfo.InvariantCulture) + "/4  Tower Hits: " + ObjectiveDamageEvents.ToString(CultureInfo.InvariantCulture);

            SetUpgradeButton(_damageButton, "Damage", DamageUpgradeRank, DamageUpgradeCost, CanPurchaseDamageUpgrade);
            SetUpgradeButton(_attackSpeedButton, "Fire Rate", AttackSpeedUpgradeRank, AttackSpeedUpgradeCost, CanPurchaseAttackSpeedUpgrade);
            SetUpgradeButton(_rangeButton, "Range", RangeUpgradeRank, RangeUpgradeCost, CanPurchaseRangeUpgrade);
            SetUpgradeButton(_repairButton, "Repair / Max HP", RepairUpgradeRank, RepairUpgradeCost, CanPurchaseRepairUpgrade);

            SetModuleButton(_pulseButton, "Pulse Beam", PulseBeamUnlocked, PulseBeamUnlockCost, CanPurchasePulseBeamModule);
            SetModuleButton(_arcButton, "Arc Burst", ArcBurstUnlocked, ArcBurstUnlockCost, CanPurchaseArcBurstModule);
            SetModuleButton(_homingButton, "Homing Pulse", HomingPulseUnlocked, HomingPulseUnlockCost, CanPurchaseHomingPulseModule);

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

        private static VisualElement AddRow(VisualElement parent)
        {
            var row = new VisualElement();
            row.style.width = Length.Percent(100);
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginTop = 2;
            row.style.marginBottom = 2;
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
            label.style.minHeight = Mathf.Max(18, fontSize + 5);
            label.style.marginTop = 1;
            label.style.marginBottom = 1;
            parent.Add(label);
            return label;
        }

        private static void AddSectionTitle(VisualElement parent, string text)
        {
            Label label = AddLabel(parent, text, 14, FontStyle.Bold);
            label.style.marginTop = 8;
            label.style.marginBottom = 3;
            label.style.color = new Color(0.65f, 0.9f, 1f);
        }

        private static Button AddButton(VisualElement parent, Action clicked, string text = "")
        {
            var button = new Button(clicked) { text = text };
            ApplyRuntimeUiFont(button);
            button.style.height = 30;
            button.style.minHeight = 30;
            button.style.marginTop = 2;
            button.style.marginBottom = 2;
            button.style.marginLeft = 1;
            button.style.marginRight = 1;
            button.style.flexGrow = 1;
            button.style.fontSize = 12;
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
