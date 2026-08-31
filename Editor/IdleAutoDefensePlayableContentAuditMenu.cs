using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Deucarian.Attacks;
using Deucarian.Attacks.Authoring;
using Deucarian.RunUpgrades.Authoring;
using Deucarian.WeaponSystems.Authoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense.Editor
{
    public static partial class IdleAutoDefensePlayableContentAuditMenu
    {
        private const string RuntimeAuditFileName = "idle-auto-defense-runtime-content-audit.md";
        private const string ParityReportFileName = "idle-auto-defense-authoring-runtime-parity.md";
        private const string RuntimeAuditJsonPath = "Temp/IdleAutoDefenseRuntimeContentAudit.json";

        public static void ValidatePlayableContent()
        {
            string report = BuildPlayableContentValidationReport();
            EditorUtility.DisplayDialog("Idle Auto Defense Playable Content", report, "OK");
        }

        public static void GenerateRuntimeContentAudit()
        {
            string path = WriteRuntimeContentAudit();
            EditorUtility.DisplayDialog("Idle Auto Defense Runtime Audit", "Runtime audit written to:\n" + path, "OK");
        }

        public static void GenerateAuthoringRuntimeParityReport()
        {
            string path = WriteAuthoringRuntimeParityReport();
            EditorUtility.DisplayDialog("Idle Auto Defense Parity Report", "Parity report written to:\n" + path, "OK");
        }

        public static void OpenMainContentSet()
        {
            if (!TryFindMainContentSet(out GameContentSetAsset contentSet, out string path))
            {
                EditorUtility.DisplayDialog("Idle Auto Defense", "No Idle Auto Defense GameContentSetAsset was found.", "OK");
                return;
            }

            Selection.activeObject = contentSet;
            EditorGUIUtility.PingObject(contentSet);
        }

        public static void FindUnauthoredVisibleAssets()
        {
            List<string> unauthored = FindVisibleObjectsWithoutAuthoredStamp();
            string message = unauthored.Count == 0
                ? "No visible runtime objects without AuthoredContentInstance stamps were found in the open scene."
                : string.Join("\n", unauthored);
            EditorUtility.DisplayDialog("Idle Auto Defense Unauthored Visible Assets", message, "OK");
        }

        public static string WriteRuntimeContentAudit()
        {
            EnsureReportsDirectory(out string reportsDirectory);
            string markdown = BuildRuntimeContentAuditMarkdown();
            string markdownPath = Path.Combine(reportsDirectory, RuntimeAuditFileName);
            File.WriteAllText(markdownPath, markdown, Encoding.UTF8);

            Directory.CreateDirectory("Temp");
            File.WriteAllText(RuntimeAuditJsonPath, BuildRuntimeContentAuditJson(), Encoding.UTF8);
            AssetDatabase.Refresh();
            return markdownPath;
        }

        public static string WriteAuthoringRuntimeParityReport()
        {
            EnsureReportsDirectory(out string reportsDirectory);
            string markdown = BuildAuthoringRuntimeParityMarkdown();
            string markdownPath = Path.Combine(reportsDirectory, ParityReportFileName);
            File.WriteAllText(markdownPath, markdown, Encoding.UTF8);
            AssetDatabase.Refresh();
            return markdownPath;
        }

        public static void GenerateReportsForBatch()
        {
            WriteRuntimeContentAudit();
            WriteAuthoringRuntimeParityReport();
        }

        public static void GenerateFreshSampleReportsForBatch()
        {
            const string contentRoot = "Assets/GameContent/IdleAutoDefense";
            IdleAutoDefenseTemplateSetupResult setup = IdleAutoDefenseTemplateSetupService.CreateGameFromTemplate(
                new IdleAutoDefenseTemplateSetupRequest
                {
                    TargetRootAssetPath = "Assets/IdleAutoDefenseAudit",
                    ContentRootAssetPath = contentRoot,
                    GameNamespace = "IdleAutoDefenseAudit",
                    GamePrefix = "Audit",
                    AllowOverwrite = true,
                    OpenCreatedScene = false,
                    RefreshAssetDatabase = true
                });
            if (setup == null || !setup.Succeeded)
                throw new InvalidOperationException(setup == null ? "Fresh sample setup failed." : setup.CreateSummary());

            CreateRuntimeAuditProbe(contentRoot);
            WriteRuntimeContentAudit();
            WriteAuthoringRuntimeParityReport();
        }

        public static string BuildPlayableContentValidationReport()
        {
            var builder = new StringBuilder();
            int errorCount = 0;
            int warningCount = 0;
            if (!TryFindMainContentSet(out GameContentSetAsset contentSet, out string contentSetPath))
            {
                builder.AppendLine("FAIL: No Idle Auto Defense main content set found.");
                errorCount++;
            }
            else
            {
                GameContentSetValidationReport report = GameContentSetValidator.Validate(contentSet);
                errorCount += report.ErrorCount;
                warningCount += report.WarningCount;
                builder.AppendLine((report.IsValid ? "PASS" : "FAIL") + ": " + contentSet.Id + " (" + contentSetPath + ")");
                AppendValidationIssues(builder, report);
                errorCount += AppendPlaceholderProductionAssetIssues(builder, contentSet);
                errorCount += AppendPulseBeamMisuseIssues(builder, contentSet);
                warningCount += AppendUnauthoredOpenSceneWarnings(builder);
            }

            builder.Insert(
                0,
                "Idle Auto Defense playable content validation: "
                + (errorCount == 0 ? "PASS" : "FAIL")
                + " | errors=" + errorCount.ToString(CultureInfo.InvariantCulture)
                + " | warnings=" + warningCount.ToString(CultureInfo.InvariantCulture)
                + Environment.NewLine + Environment.NewLine);
            return builder.ToString();
        }

        public static string BuildRuntimeContentAuditMarkdown()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Idle Auto Defense Runtime Content Audit");
            builder.AppendLine();
            builder.AppendLine("Generated: " + DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture));
            builder.AppendLine("Scene: `" + EditorSceneManager.GetActiveScene().path + "`");
            builder.AppendLine();

            AuthoredContentInstance[] stamps = UnityEngine.Object.FindObjectsByType<AuthoredContentInstance>(FindObjectsSortMode.None);
            List<string> unauthored = FindVisibleObjectsWithoutAuthoredStamp();
            builder.AppendLine("## Summary");
            builder.AppendLine();
            builder.AppendLine("- Stamped visible runtime objects: " + stamps.Length.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("- Visible objects without authored stamp: " + unauthored.Count.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine();

            builder.AppendLine("## Runtime Objects");
            builder.AppendLine();
            builder.AppendLine("| Runtime Object | Type | Content ID | Source Asset | Prefab | Owner | Attack | VFX/Role | Origin Socket | Target Socket | Authoring Source | Fallback | Allowed |");
            builder.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
            for (int i = 0; i < stamps.Length; i++)
            {
                AuthoredContentInstance stamp = stamps[i];
                if (stamp == null) continue;
                builder.Append("| ")
                    .Append(EscapeTable(stamp.gameObject.name)).Append(" | ")
                    .Append(EscapeTable(stamp.ContentType)).Append(" | ")
                    .Append(EscapeTable(stamp.ContentId)).Append(" | ")
                    .Append(EscapeTable(FormatAssetRef(stamp.SourceAssetPath, stamp.SourceAssetGuid))).Append(" | ")
                    .Append(EscapeTable(stamp.PrefabName)).Append(" | ")
                    .Append(EscapeTable(stamp.OwnerContentId)).Append(" | ")
                    .Append(EscapeTable(stamp.AttackContentId)).Append(" | ")
                    .Append(EscapeTable(string.IsNullOrWhiteSpace(stamp.VfxContentId) ? stamp.EffectRole : stamp.VfxContentId)).Append(" | ")
                    .Append(EscapeTable(stamp.OriginSocketId)).Append(" | ")
                    .Append(EscapeTable(stamp.TargetSocketId)).Append(" | ")
                    .Append(stamp.CameFromGameContentAuthoring ? "Yes" : "No").Append(" | ")
                    .Append(stamp.FallbackUsed ? "Yes" : "No").Append(" | ")
                    .Append(stamp.Allowed ? "Yes" : "No").AppendLine(" |");
            }

            builder.AppendLine();
            builder.AppendLine("## Unauthored Visible Objects");
            builder.AppendLine();
            if (unauthored.Count == 0)
            {
                builder.AppendLine("None found in the currently open scene.");
            }
            else
            {
                for (int i = 0; i < unauthored.Count; i++)
                    builder.AppendLine("- " + unauthored[i]);
            }

            return builder.ToString();
        }

        public static string BuildRuntimeContentAuditJson()
        {
            AuthoredContentInstance[] stamps = UnityEngine.Object.FindObjectsByType<AuthoredContentInstance>(FindObjectsSortMode.None);
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.Append("  \"scene\": ").Append(Json(EditorSceneManager.GetActiveScene().path)).AppendLine(",");
            builder.AppendLine("  \"objects\": [");
            bool appendedObject = false;
            for (int i = 0; i < stamps.Length; i++)
            {
                AuthoredContentInstance stamp = stamps[i];
                if (stamp == null) continue;
                if (appendedObject)
                    builder.AppendLine(",");
                builder.AppendLine("    {");
                builder.Append("      \"name\": ").Append(Json(stamp.gameObject.name)).AppendLine(",");
                builder.Append("      \"type\": ").Append(Json(stamp.ContentType)).AppendLine(",");
                builder.Append("      \"contentId\": ").Append(Json(stamp.ContentId)).AppendLine(",");
                builder.Append("      \"sourceAssetPath\": ").Append(Json(stamp.SourceAssetPath)).AppendLine(",");
                builder.Append("      \"sourceAssetGuid\": ").Append(Json(stamp.SourceAssetGuid)).AppendLine(",");
                builder.Append("      \"prefabName\": ").Append(Json(stamp.PrefabName)).AppendLine(",");
                builder.Append("      \"ownerContentId\": ").Append(Json(stamp.OwnerContentId)).AppendLine(",");
                builder.Append("      \"attackContentId\": ").Append(Json(stamp.AttackContentId)).AppendLine(",");
                builder.Append("      \"vfxContentId\": ").Append(Json(stamp.VfxContentId)).AppendLine(",");
                builder.Append("      \"originSocketId\": ").Append(Json(stamp.OriginSocketId)).AppendLine(",");
                builder.Append("      \"targetSocketId\": ").Append(Json(stamp.TargetSocketId)).AppendLine(",");
                builder.Append("      \"cameFromGameContentAuthoring\": ").Append(stamp.CameFromGameContentAuthoring ? "true" : "false").AppendLine(",");
                builder.Append("      \"fallbackUsed\": ").Append(stamp.FallbackUsed ? "true" : "false").AppendLine(",");
                builder.Append("      \"allowed\": ").Append(stamp.Allowed ? "true" : "false").AppendLine();
                builder.Append("    }");
                appendedObject = true;
            }
            if (appendedObject)
                builder.AppendLine();
            builder.AppendLine("  ]");
            builder.AppendLine("}");
            return builder.ToString();
        }

        public static string BuildAuthoringRuntimeParityMarkdown()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Idle Auto Defense Authoring Runtime Parity");
            builder.AppendLine();
            builder.AppendLine("Generated: " + DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture));
            builder.AppendLine();
            if (!TryFindMainContentSet(out GameContentSetAsset contentSet, out string contentSetPath))
            {
                builder.AppendLine("No main Idle Auto Defense content set was found.");
                return builder.ToString();
            }

            builder.AppendLine("Main content set: `" + contentSetPath + "`");
            builder.AppendLine();
            AppendTowerTable(builder, contentSet);
            AppendEnemyTable(builder, contentSet);
            AppendAttackTable(builder, contentSet);
            AppendVfxTable(builder, contentSet);
            AppendUpgradeTable(builder, contentSet);
            return builder.ToString();
        }

        private static void AppendTowerTable(StringBuilder builder, GameContentSetAsset contentSet)
        {
            builder.AppendLine("## Towers");
            builder.AppendLine();
            builder.AppendLine("| Content ID | Authoring Prefab | Runtime Prefab | Match | Notes |");
            builder.AppendLine("| ---------- | ---------------- | -------------- | ----- | ----- |");
            for (int i = 0; i < contentSet.AvailableWeapons.Count; i++)
            {
                WeaponDefinitionAsset weapon = contentSet.AvailableWeapons[i];
                if (weapon == null) continue;
                string prefab = FormatObjectPath(weapon.Presentation == null ? null : weapon.Presentation.Prefab);
                string runtime = FindRuntimePrefabName(weapon.Id, "WeaponPrefab");
                bool match = string.IsNullOrWhiteSpace(runtime) || runtime.Contains(ObjectName(weapon.Presentation == null ? null : weapon.Presentation.Prefab));
                builder.Append("| ").Append(EscapeTable(weapon.Id)).Append(" | ")
                    .Append(EscapeTable(prefab)).Append(" | ")
                    .Append(EscapeTable(runtime)).Append(" | ")
                    .Append(match ? "Yes" : "No").Append(" | ")
                    .Append(IsPlaceholderProductionName(prefab) ? "Placeholder/template-named production prefab." : string.Empty)
                    .AppendLine(" |");
            }
            builder.AppendLine();
        }

        private static void AppendEnemyTable(StringBuilder builder, GameContentSetAsset contentSet)
        {
            builder.AppendLine("## Enemies");
            builder.AppendLine();
            builder.AppendLine("| Content ID | Authoring Prefab | Runtime Prefab | Match | Notes |");
            builder.AppendLine("| ---------- | ---------------- | -------------- | ----- | ----- |");
            for (int i = 0; i < contentSet.EnemyPool.Count; i++)
            {
                EnemyDefinitionAsset enemy = contentSet.EnemyPool[i];
                if (enemy == null) continue;
                string prefab = FormatObjectPath(enemy.Presentation == null ? null : enemy.Presentation.Prefab);
                string runtime = FindRuntimePrefabName(enemy.Id, "EnemyVisual");
                bool match = string.IsNullOrWhiteSpace(runtime) || runtime.Contains(ObjectName(enemy.Presentation == null ? null : enemy.Presentation.Prefab));
                builder.Append("| ").Append(EscapeTable(enemy.Id)).Append(" | ")
                    .Append(EscapeTable(prefab)).Append(" | ")
                    .Append(EscapeTable(runtime)).Append(" | ")
                    .Append(match ? "Yes" : "No").Append(" | ")
                    .Append(IsPlaceholderProductionName(prefab) ? "Placeholder/template-named production prefab." : string.Empty)
                    .AppendLine(" |");
            }
            builder.AppendLine();
        }

        private static void AppendAttackTable(StringBuilder builder, GameContentSetAsset contentSet)
        {
            builder.AppendLine("## Attacks");
            builder.AppendLine();
            builder.AppendLine("| Attack ID | Mode | Authoring Projectile/Beam | Runtime Projectile/Beam | Match | Notes |");
            builder.AppendLine("| --------- | ---- | ------------------------- | ----------------------- | ----- | ----- |");
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < contentSet.AvailableWeapons.Count; i++)
            {
                AttackDefinitionAsset attack = contentSet.AvailableWeapons[i] == null || contentSet.AvailableWeapons[i].Stats == null
                    ? null
                    : contentSet.AvailableWeapons[i].Stats.Attack;
                if (attack == null || !seen.Add(attack.Id)) continue;
                GameObject authored = attack.Delivery == null
                    ? null
                    : attack.Delivery.Mode == AttackRecipeDeliveryMode.Hitscan
                        ? attack.Delivery.BeamVfxPrefab
                        : attack.Delivery.ProjectilePrefab;
                string runtime = FindRuntimePrefabName(attack.Id, attack.Delivery != null && attack.Delivery.Mode == AttackRecipeDeliveryMode.Hitscan ? "BeamVfx" : "ProjectileVisual");
                string authoredPath = FormatObjectPath(authored);
                bool match = string.IsNullOrWhiteSpace(runtime) || runtime.Contains(ObjectName(authored));
                builder.Append("| ").Append(EscapeTable(attack.Id)).Append(" | ")
                    .Append(EscapeTable(attack.Delivery == null ? string.Empty : attack.Delivery.Mode.ToString())).Append(" | ")
                    .Append(EscapeTable(authoredPath)).Append(" | ")
                    .Append(EscapeTable(runtime)).Append(" | ")
                    .Append(match ? "Yes" : "No").Append(" | ")
                    .Append(IsPlaceholderProductionName(authoredPath) ? "Placeholder/template-named production visual." : string.Empty)
                    .AppendLine(" |");
            }
            builder.AppendLine();
        }

        private static void AppendVfxTable(StringBuilder builder, GameContentSetAsset contentSet)
        {
            builder.AppendLine("## VFX");
            builder.AppendLine();
            builder.AppendLine("| VFX ID | Authoring Prefab/Material | Runtime Object | Match | Notes |");
            builder.AppendLine("| ------ | ------------------------- | -------------- | ----- | ----- |");
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < contentSet.AvailableWeapons.Count; i++)
                AppendAttackVfxRows(builder, contentSet.AvailableWeapons[i], seen);
            for (int i = 0; i < contentSet.EnemyPool.Count; i++)
                AppendEnemyVfxRows(builder, contentSet.EnemyPool[i], seen);
            builder.AppendLine();
        }

        private static void AppendUpgradeTable(StringBuilder builder, GameContentSetAsset contentSet)
        {
            builder.AppendLine("## Upgrades");
            builder.AppendLine();
            builder.AppendLine("| Upgrade ID | Authoring Effect | Runtime Effect | Visible? | Match |");
            builder.AppendLine("| ---------- | ---------------- | -------------- | -------- | ----- |");
            for (int i = 0; i < contentSet.UpgradePool.Count; i++)
            {
                RunUpgradeDefinitionAsset upgrade = contentSet.UpgradePool[i];
                if (upgrade == null) continue;
                string effects = FormatUpgradeEffects(upgrade);
                builder.Append("| ").Append(EscapeTable(upgrade.Id)).Append(" | ")
                    .Append(EscapeTable(effects)).Append(" | ")
                    .Append(EscapeTable(effects)).Append(" | ")
                    .Append("Reward feedback authored through content-set reward catalog").Append(" | ")
                    .Append("Partial").AppendLine(" |");
            }
            builder.AppendLine();
        }

        private static void AppendAttackVfxRows(StringBuilder builder, WeaponDefinitionAsset weapon, HashSet<string> seen)
        {
            AttackDefinitionAsset attack = weapon == null || weapon.Stats == null ? null : weapon.Stats.Attack;
            if (attack == null) return;
            if (attack.Delivery != null)
            {
                AppendVfxRow(builder, seen, attack.Id + ".delivery.impact", attack.Delivery.ImpactVfxPrefab);
                AppendVfxRow(builder, seen, attack.Id + ".delivery.beam", attack.Delivery.BeamVfxPrefab);
                AppendVfxRow(builder, seen, attack.Id + ".delivery.projectile", attack.Delivery.ProjectilePrefab);
            }

            if (attack.Presentation == null) return;
            IReadOnlyList<AttackPresentationEventRecipe> events = attack.Presentation.Events;
            for (int i = 0; i < events.Count; i++)
            {
                AttackPresentationEventRecipe recipe = events[i];
                if (recipe == null) continue;
                AppendVfxRow(builder, seen, attack.Id + ".presentation." + recipe.EventKind, recipe.VfxPrefab);
            }
        }

        private static void AppendEnemyVfxRows(StringBuilder builder, EnemyDefinitionAsset enemy, HashSet<string> seen)
        {
            if (enemy == null || enemy.Presentation == null) return;
            IReadOnlyList<EnemyPresentationEventRecipe> events = enemy.Presentation.Events;
            for (int i = 0; i < events.Count; i++)
            {
                EnemyPresentationEventRecipe recipe = events[i];
                if (recipe == null) continue;
                AppendVfxRow(builder, seen, enemy.Id + ".presentation." + recipe.EventKind, recipe.VfxPrefab);
            }
        }

        private static void AppendVfxRow(StringBuilder builder, HashSet<string> seen, string id, GameObject prefab)
        {
            if (prefab == null) return;
            string path = FormatObjectPath(prefab);
            string key = id + "|" + path;
            if (!seen.Add(key)) return;
            string runtime = FindRuntimePrefabName(string.Empty, ObjectName(prefab));
            builder.Append("| ").Append(EscapeTable(id)).Append(" | ")
                .Append(EscapeTable(path)).Append(" | ")
                .Append(EscapeTable(runtime)).Append(" | ")
                .Append(string.IsNullOrWhiteSpace(runtime) || runtime.Contains(ObjectName(prefab)) ? "Yes" : "No").Append(" | ")
                .Append(IsPlaceholderProductionName(path) ? "Placeholder/template-named production VFX." : string.Empty)
                .AppendLine(" |");
        }

    }
}
