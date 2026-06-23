using System;
using System.IO;
using Deucarian.TemplateGameIdleAutoDefense;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense.Samples
{
    public sealed class BasicIdleAutoDefenseGameBootstrap : IdleAutoDefenseTemplateController
    {
        private string _lastSaveMessage = "Sample save has not been written yet.";

        private void Start()
        {
            WriteSampleSnapshot("play-started");
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12f, 12f, 380f, 214f), GUI.skin.box);
            GUILayout.Label("Idle Auto Defense Starter");
            GUILayout.Label(StatusSummary);
            GUILayout.Label("Credits: " + (OfflineRewardCredits + EncounterRewardCredits) + "  Parts: " + (OfflineRewardParts + EncounterRewardParts));
            GUILayout.Label("Save: " + _lastSaveMessage);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Snapshot"))
                WriteSampleSnapshot("manual");
            if (GUILayout.Button("Reset Save"))
                ResetSampleSave();
            GUILayout.EndHorizontal();

            GUILayout.Label("Edit first: Content/starter-content.json and Scripts/BasicIdleAutoDefenseGameBootstrap.cs");
            GUILayout.EndArea();
        }

        private void WriteSampleSnapshot(string reason)
        {
            BasicIdleAutoDefenseSampleSave.WriteSnapshot(reason, this);
            _lastSaveMessage = "written to " + BasicIdleAutoDefenseSampleSave.SaveFilePath;
        }

        private void ResetSampleSave()
        {
            bool deleted = BasicIdleAutoDefenseSampleSave.Reset();
            _lastSaveMessage = deleted ? "reset complete" : "nothing to reset";
            Debug.Log("[Idle Auto Defense Template] " + _lastSaveMessage + ".");
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
