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
    public static class IdleAutoDefensePlayableContentAuditMenu
    {
        public const string ValidateMenuPath = "Deucarian/Idle Auto Defense/Validate Playable Content";
        public const string RuntimeAuditMenuPath = "Deucarian/Idle Auto Defense/Generate Runtime Content Audit";
        public const string ParityReportMenuPath = "Deucarian/Idle Auto Defense/Generate Authoring Runtime Parity Report";
        public const string OpenContentSetMenuPath = "Deucarian/Idle Auto Defense/Open Main Content Set";
        public const string FindUnauthoredMenuPath = "Deucarian/Idle Auto Defense/Find Unauthored Visible Assets";

        private const string RuntimeAuditFileName = "idle-auto-defense-runtime-content-audit.md";
        private const string ParityReportFileName = "idle-auto-defense-authoring-runtime-parity.md";
        private const string RuntimeAuditJsonPath = "Temp/IdleAutoDefenseRuntimeContentAudit.json";

        [MenuItem(ValidateMenuPath, priority = 10)]
        public static void ValidatePlayableContent()
        {
            string report = BuildPlayableContentValidationReport();
            EditorUtility.DisplayDialog("Idle Auto Defense Playable Content", report, "OK");
        }

        [MenuItem(RuntimeAuditMenuPath, priority = 11)]
        public static void GenerateRuntimeContentAudit()
        {
            string path = WriteRuntimeContentAudit();
            EditorUtility.DisplayDialog("Idle Auto Defense Runtime Audit", "Runtime audit written to:\n" + path, "OK");
        }

        [MenuItem(ParityReportMenuPath, priority = 12)]
        public static void GenerateAuthoringRuntimeParityReport()
        {
            string path = WriteAuthoringRuntimeParityReport();
            EditorUtility.DisplayDialog("Idle Auto Defense Parity Report", "Parity report written to:\n" + path, "OK");
        }

        [MenuItem(OpenContentSetMenuPath, priority = 13)]
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

        [MenuItem(FindUnauthoredMenuPath, priority = 14)]
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

        private static int AppendPlaceholderProductionAssetIssues(StringBuilder builder, GameContentSetAsset contentSet)
        {
            int errors = 0;
            List<string> assets = CollectProductionAssetRefs(contentSet);
            for (int i = 0; i < assets.Count; i++)
            {
                if (!IsPlaceholderProductionName(assets[i])) continue;
                builder.AppendLine("ERROR: Main playable content references placeholder/template-named asset: " + assets[i]);
                errors++;
            }

            return errors;
        }

        private static int AppendPulseBeamMisuseIssues(StringBuilder builder, GameContentSetAsset contentSet)
        {
            int errors = 0;
            for (int i = 0; i < contentSet.AvailableWeapons.Count; i++)
            {
                WeaponDefinitionAsset weapon = contentSet.AvailableWeapons[i];
                AttackDefinitionAsset attack = weapon == null || weapon.Stats == null ? null : weapon.Stats.Attack;
                if (attack == null || attack.Delivery == null) continue;
                GameObject beam = attack.Delivery.BeamVfxPrefab;
                if (beam == null) continue;
                bool isPulse = attack.Id.IndexOf("pulse", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (weapon.Id.IndexOf("pulse", StringComparison.OrdinalIgnoreCase) >= 0);
                if (!isPulse && beam.name.IndexOf("PulseBeam", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    builder.AppendLine("ERROR: Non-pulse attack uses Pulse Beam VFX: " + attack.Id);
                    errors++;
                }
            }

            return errors;
        }

        private static int AppendUnauthoredOpenSceneWarnings(StringBuilder builder)
        {
            List<string> unauthored = FindVisibleObjectsWithoutAuthoredStamp();
            if (unauthored.Count == 0) return 0;
            builder.AppendLine("WARNING: Open scene has visible objects without authored stamps:");
            for (int i = 0; i < unauthored.Count; i++)
                builder.AppendLine("  - " + unauthored[i]);
            return unauthored.Count;
        }

        private static List<string> CollectProductionAssetRefs(GameContentSetAsset contentSet)
        {
            var paths = new List<string>();
            for (int i = 0; i < contentSet.AvailableWeapons.Count; i++)
            {
                WeaponDefinitionAsset weapon = contentSet.AvailableWeapons[i];
                if (weapon == null) continue;
                AddObjectPath(paths, weapon.Presentation == null ? null : weapon.Presentation.Prefab);
                AddObjectPath(paths, weapon.Presentation == null ? null : weapon.Presentation.PlacementVfxPrefab);
                AttackDefinitionAsset attack = weapon.Stats == null ? null : weapon.Stats.Attack;
                if (attack == null) continue;
                if (attack.Delivery != null)
                {
                    AddObjectPath(paths, attack.Delivery.ProjectilePrefab);
                    AddObjectPath(paths, attack.Delivery.BeamVfxPrefab);
                    AddObjectPath(paths, attack.Delivery.ImpactVfxPrefab);
                }

                if (attack.Presentation != null)
                {
                    IReadOnlyList<AttackPresentationEventRecipe> events = attack.Presentation.Events;
                    for (int eventIndex = 0; eventIndex < events.Count; eventIndex++)
                    {
                        AttackPresentationEventRecipe recipe = events[eventIndex];
                        if (recipe != null)
                            AddObjectPath(paths, recipe.VfxPrefab);
                    }
                }
            }

            for (int i = 0; i < contentSet.EnemyPool.Count; i++)
            {
                EnemyDefinitionAsset enemy = contentSet.EnemyPool[i];
                if (enemy == null || enemy.Presentation == null) continue;
                AddObjectPath(paths, enemy.Presentation.Prefab);
                IReadOnlyList<EnemyPresentationEventRecipe> events = enemy.Presentation.Events;
                for (int eventIndex = 0; eventIndex < events.Count; eventIndex++)
                {
                    EnemyPresentationEventRecipe recipe = events[eventIndex];
                    if (recipe != null)
                        AddObjectPath(paths, recipe.VfxPrefab);
                }
            }

            return paths;
        }

        private static void AddObjectPath(List<string> paths, UnityEngine.Object asset)
        {
            if (asset == null) return;
            string path = AssetDatabase.GetAssetPath(asset);
            if (!string.IsNullOrWhiteSpace(path))
                paths.Add(path + " (" + asset.name + ")");
        }

        private static void AppendValidationIssues(StringBuilder builder, GameContentSetValidationReport report)
        {
            if (report == null) return;
            for (int i = 0; i < report.Issues.Count; i++)
            {
                GameContentSetValidationIssue issue = report.Issues[i];
                builder.AppendLine(issue.Severity + ": " + issue.Path + " - " + issue.Message);
            }
        }

        private static List<string> FindVisibleObjectsWithoutAuthoredStamp()
        {
            var result = new List<string>();
            Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            var seenRoots = new HashSet<int>();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                AuthoredContentInstance stamp = renderer.GetComponentInParent<AuthoredContentInstance>();
                if (stamp != null) continue;
                int id = renderer.transform.root.GetInstanceID();
                if (!seenRoots.Add(id)) continue;
                result.Add(renderer.transform.root.name + " / " + renderer.name);
            }

            return result;
        }

        private static bool TryFindMainContentSet(out GameContentSetAsset contentSet, out string path)
        {
            string[] guids = AssetDatabase.FindAssets("t:GameContentSetAsset");
            int bestScore = int.MinValue;
            GameContentSetAsset bestContentSet = null;
            string bestPath = string.Empty;
            for (int i = 0; i < guids.Length; i++)
            {
                path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.IndexOf("IdleAutoDefense", StringComparison.OrdinalIgnoreCase) < 0 &&
                    path.IndexOf("BasicIdleAutoDefenseGame", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                contentSet = AssetDatabase.LoadAssetAtPath<GameContentSetAsset>(path);
                if (contentSet == null)
                    continue;

                int score = ScoreContentSetPath(path);
                if (score <= bestScore)
                    continue;
                bestScore = score;
                bestContentSet = contentSet;
                bestPath = path;
            }

            contentSet = bestContentSet;
            path = bestPath;
            return contentSet != null;
        }

        private static int ScoreContentSetPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return int.MinValue;
            int score = 0;
            if (path.IndexOf("Assets/GameContent/IdleAutoDefense/", StringComparison.OrdinalIgnoreCase) >= 0) score += 1000;
            if (path.IndexOf("TemplateSource~/BasicIdleAutoDefenseGame/", StringComparison.OrdinalIgnoreCase) >= 0) score += 900;
            if (path.IndexOf("Assets/GameContent/OPEN_THIS", StringComparison.OrdinalIgnoreCase) >= 0) score -= 500;
            if (path.IndexOf("/ContentSets/", StringComparison.OrdinalIgnoreCase) >= 0) score += 10;
            return score;
        }

        private static GameObject CreateRuntimeAuditProbe(string contentRoot)
        {
            GameContentPackAsset contentPack = AssetDatabase.LoadAssetAtPath<GameContentPackAsset>(
                contentRoot + "/ContentPacks/contentpack.template.basic-idle-auto-defense/contentpack.template.basic-idle-auto-defense_ContentPack.asset");
            GameContentSetAsset contentSet = AssetDatabase.LoadAssetAtPath<GameContentSetAsset>(
                contentRoot + "/ContentSets/contentset.template.basic-idle-auto-defense/contentset.template.basic-idle-auto-defense_GameContentSet.asset");
            if (contentPack == null || contentSet == null)
                throw new InvalidOperationException("Fresh generated content pack/set could not be loaded from " + contentRoot + ".");

            var host = new GameObject("Idle Auto Defense Runtime Audit Probe");
            host.hideFlags = HideFlags.HideAndDontSave;
            var controller = host.AddComponent<IdleAutoDefenseTemplateController>();
            controller.enabled = false;
            var serialized = new SerializedObject(controller);
            AssignObjectReference(serialized, "_contentPack", contentPack);
            AssignObjectReference(serialized, "_contentSet", contentSet);
            AssignObjectReference(serialized, "_templateContentPack", contentPack);
            AssignObjectReference(serialized, "_templateContentSet", contentSet);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            controller.Build();

            for (int i = 0; i < 1600; i++)
            {
                BuyAvailableLivePurchases(controller);
                controller.Step(1, 0.05f);
                if (controller.AuthoredVisibleInstanceStampCount > 20 &&
                    controller.ProjectileVisualSpawnCount > 0 &&
                    controller.AttackVfxSpawnCount > 0 &&
                    controller.EnemyPresentationEventCount > 0)
                {
                    break;
                }
            }

            if (controller.AuthoredVisibleInstanceStampCount <= 0)
                throw new InvalidOperationException("Runtime audit probe did not create authored visible instances. " + controller.StatusSummary);

            return host;
        }

        private static void AssignObjectReference(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
                property.objectReferenceValue = value;
        }

        private static void BuyAvailableLivePurchases(IdleAutoDefenseTemplateController controller)
        {
            if (controller == null) return;
            if (controller.RewardDraftActive)
                controller.TryChooseRewardDraftChoice(0);
            if (controller.ObjectiveHealth < controller.ObjectiveMaximumHealth * 0.7d && controller.CanPurchaseRepairUpgrade)
                controller.TryPurchaseRepairUpgrade();
            if (controller.CanPurchasePulseBeamModule) controller.TryPurchasePulseBeamModule();
            if (controller.PulseBeamUnlocked && controller.CanPurchaseDamageUpgrade) controller.TryPurchaseDamageUpgrade();
            if (controller.PulseBeamUnlocked && controller.CanPurchaseAttackSpeedUpgrade) controller.TryPurchaseAttackSpeedUpgrade();
            if (controller.CanPurchaseArcBurstModule) controller.TryPurchaseArcBurstModule();
            if (controller.ArcBurstUnlocked && controller.CanPurchaseRangeUpgrade) controller.TryPurchaseRangeUpgrade();
            if (controller.CanPurchaseHomingPulseModule) controller.TryPurchaseHomingPulseModule();
            if (controller.CanPurchaseOverdrive) controller.TryPurchaseOverdrive();
        }

        private static void EnsureReportsDirectory(out string reportsDirectory)
        {
            UnityEditor.PackageManager.PackageInfo packageInfo =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(IdleAutoDefenseTemplateController).Assembly);
            string packageRoot = packageInfo == null || string.IsNullOrWhiteSpace(packageInfo.resolvedPath)
                ? Directory.GetCurrentDirectory()
                : packageInfo.resolvedPath;
            reportsDirectory = Path.Combine(packageRoot, "Documentation~");
            Directory.CreateDirectory(reportsDirectory);
        }

        private static string FindRuntimePrefabName(string contentId, string typeOrName)
        {
            AuthoredContentInstance[] stamps = UnityEngine.Object.FindObjectsByType<AuthoredContentInstance>(FindObjectsSortMode.None);
            for (int i = 0; i < stamps.Length; i++)
            {
                AuthoredContentInstance stamp = stamps[i];
                if (stamp == null) continue;
                bool idMatches = string.IsNullOrWhiteSpace(contentId) || string.Equals(stamp.ContentId, contentId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(stamp.OwnerContentId, contentId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(stamp.AttackContentId, contentId, StringComparison.OrdinalIgnoreCase);
                bool typeMatches = string.IsNullOrWhiteSpace(typeOrName) || stamp.ContentType.IndexOf(typeOrName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    stamp.PrefabName.IndexOf(typeOrName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    stamp.EffectRole.IndexOf(typeOrName, StringComparison.OrdinalIgnoreCase) >= 0;
                if (idMatches && typeMatches)
                    return stamp.PrefabName + " / " + stamp.gameObject.name;
            }

            return string.Empty;
        }

        private static string FormatUpgradeEffects(RunUpgradeDefinitionAsset upgrade)
        {
            if (upgrade == null || upgrade.Effects == null) return string.Empty;
            IReadOnlyList<RunUpgradeEffectRecipe> effects = upgrade.Effects.Effects;
            if (effects.Count == 0) return "No effects";
            var builder = new StringBuilder();
            for (int i = 0; i < effects.Count; i++)
            {
                if (i > 0) builder.Append("; ");
                builder.Append(effects[i].TargetKind).Append(" ")
                    .Append(effects[i].ModifierType).Append(" ")
                    .Append(effects[i].Amount.ToString("0.###", CultureInfo.InvariantCulture))
                    .Append(" -> ").Append(effects[i].GetTargetId());
            }

            return builder.ToString();
        }

        private static string FormatObjectPath(UnityEngine.Object asset)
        {
            if (asset == null) return string.Empty;
            string path = AssetDatabase.GetAssetPath(asset);
            return string.IsNullOrWhiteSpace(path) ? asset.name : path + " (" + asset.name + ")";
        }

        private static string ObjectName(UnityEngine.Object asset)
        {
            return asset == null ? string.Empty : asset.name;
        }

        private static string FormatAssetRef(string path, string guid)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.IsNullOrWhiteSpace(guid) ? string.Empty : guid;
            return string.IsNullOrWhiteSpace(guid) ? path : path + " [" + guid + "]";
        }

        private static bool IsPlaceholderProductionName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            string normalized = value.Replace("\\", "/");
            int slash = normalized.LastIndexOf('/');
            string leaf = slash >= 0 ? normalized.Substring(slash + 1) : normalized;
            int objectNameStart = leaf.IndexOf("(", StringComparison.Ordinal);
            string objectName = objectNameStart >= 0 ? leaf.Substring(objectNameStart + 1).TrimEnd(')', ' ') : leaf;
            return leaf.IndexOf("template-", StringComparison.OrdinalIgnoreCase) >= 0 ||
                objectName.IndexOf("template-", StringComparison.OrdinalIgnoreCase) >= 0 ||
                leaf.StartsWith("template", StringComparison.OrdinalIgnoreCase) ||
                objectName.StartsWith("template", StringComparison.OrdinalIgnoreCase);
        }

        private static string EscapeTable(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
        }

        private static string Json(string value)
        {
            return "\"" + (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\"";
        }
    }
}
