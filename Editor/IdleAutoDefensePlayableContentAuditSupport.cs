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
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense.Editor
{
    public static partial class IdleAutoDefensePlayableContentAuditMenu
    {
        internal static bool HasMainContentSet()
        {
            return TryFindMainContentSet(out _, out _);
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
                contentRoot + "/ContentPacks/contentpack.idle-auto-defense.playable/contentpack.idle-auto-defense.playable_ContentPack.asset");
            GameContentSetAsset contentSet = AssetDatabase.LoadAssetAtPath<GameContentSetAsset>(
                contentRoot + "/ContentSets/contentset.idle-auto-defense.playable/contentset.idle-auto-defense.playable_GameContentSet.asset");
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
