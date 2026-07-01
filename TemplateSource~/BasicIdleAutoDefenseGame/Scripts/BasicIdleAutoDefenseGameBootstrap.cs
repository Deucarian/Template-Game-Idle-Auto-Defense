using System;
using System.Globalization;
using System.IO;
using Deucarian.TemplateGameIdleAutoDefense;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense.Samples
{
    public sealed class BasicIdleAutoDefenseGameBootstrap : IdleAutoDefenseTemplateController
    {
        private const float HudWidth = 360f;
        [SerializeField] private GameContentPackAsset _templateContentPack;
        [SerializeField] private GameContentSetAsset _templateContentSet;
        private string _saveStatus = "No snapshot saved";

        protected override void Awake()
        {
            ConfigureContentPack(_templateContentPack, _templateContentSet);
            base.Awake();
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12f, 12f, HudWidth, 382f), GUI.skin.box);
            GUILayout.Label("Idle Auto Defense");
            GUILayout.Label("State: " + RuntimeStateName);
            GUILayout.Label("Tower HP: " + ObjectiveHealthText + "  Lives: " + ObjectiveLivesRemaining);
            GUILayout.Label("Runtime Credits: " + RuntimeCurrency);
            GUILayout.Label("Progression Credits: " + EncounterRewardCredits + "  Parts: " + EncounterRewardParts);
            GUILayout.Label("Time: " + SurvivalSeconds.ToString("0", CultureInfo.InvariantCulture) + "s");
            GUILayout.Label("Spawn Profile: " + CurrentSpawnProfileName);
            GUILayout.Label("Enemies: " + ActiveEnemyCount + " active / " + SpawnedCount + " spawned");
            GUILayout.Label("Kills: " + (DirectOrCombatKillCount + ProjectileAdapterKillCount) + "  Projectiles: " + ProjectileLaunchCount);
            GUILayout.Label("Selected Upgrades: " + SelectedUpgradeCount + "  Objective Hits: " + ObjectiveDamageEvents);
            GUILayout.Space(4f);
            GUILayout.Label("Upgrades");
            DrawUpgradeButton("Damage", DamageUpgradeRank, DamageUpgradeCost, CanPurchaseDamageUpgrade, TryPurchaseDamageUpgrade);
            DrawUpgradeButton("Attack Speed", AttackSpeedUpgradeRank, AttackSpeedUpgradeCost, CanPurchaseAttackSpeedUpgrade, TryPurchaseAttackSpeedUpgrade);
            DrawUpgradeButton("Range", RangeUpgradeRank, RangeUpgradeCost, CanPurchaseRangeUpgrade, TryPurchaseRangeUpgrade);
            DrawUpgradeButton("Repair / Max HP", RepairUpgradeRank, RepairUpgradeCost, CanPurchaseRepairUpgrade, TryPurchaseRepairUpgrade);
            GUILayout.Space(4f);
            GUILayout.Label("Save: " + _saveStatus + (BasicIdleAutoDefenseSampleSave.HasSave ? " (file present)" : string.Empty));
            if (GUILayout.Button("Save Snapshot")) SaveSnapshot("manual");
            if (GUILayout.Button("Reset Save")) ResetSave();
            GUILayout.Space(4f);
            if (EncounterCompleted) GUILayout.Label("Run complete");
            else if (EncounterFailed) GUILayout.Label("Tower destroyed");
            if ((EncounterCompleted || EncounterFailed) && GUILayout.Button("Restart Run")) RestartRun();
            GUILayout.EndArea();
        }

        private void SaveSnapshot(string reason)
        {
            BasicIdleAutoDefenseSampleSave.WriteSnapshot(reason, this);
            _saveStatus = "Saved " + DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        }

        private void ResetSave()
        {
            bool existed = BasicIdleAutoDefenseSampleSave.Reset();
            _saveStatus = existed ? "Reset saved snapshot" : "No snapshot to reset";
        }

        private static void DrawUpgradeButton(string label, int rank, int cost, bool enabled, Func<bool> purchase)
        {
            bool wasEnabled = GUI.enabled;
            GUI.enabled = enabled;
            if (GUILayout.Button(label + "  Lv " + rank.ToString(CultureInfo.InvariantCulture) + "  " + cost.ToString(CultureInfo.InvariantCulture)))
                purchase?.Invoke();
            GUI.enabled = wasEnabled;
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
