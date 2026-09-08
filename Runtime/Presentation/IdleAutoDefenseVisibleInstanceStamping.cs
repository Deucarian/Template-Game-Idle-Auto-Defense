using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Deucarian.Attacks;
using Deucarian.Attacks.Authoring;
using Deucarian.AutoDefense;
using Deucarian.Combat;
using Deucarian.Common;
using Deucarian.DefenseGames;
using Deucarian.Encounters;
using Deucarian.IdleProgression;
using Deucarian.Monetization;
using Deucarian.Persistence;
using Deucarian.Progression;
using Deucarian.Projectiles;
using Deucarian.RunUpgrades;
using Deucarian.RunUpgrades.Authoring;
using Deucarian.WeaponSystems;
using Deucarian.WeaponSystems.Authoring;
using Deucarian.WorldNavigation;
using Deucarian.WorldSpawning;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    internal sealed class IdleAutoDefenseVisibleInstanceStamping
    {
        private readonly IdleAutoDefensePresentationCounters _counters;

        internal IdleAutoDefenseVisibleInstanceStamping(IdleAutoDefensePresentationCounters counters)
        {
            _counters = counters;
        }

        internal void StampAuthoredVisibleInstance(
            GameObject instance,
            string definitionType,
            string contentId,
            string prefabName,
            string ownerWeaponId,
            string ownerAttackId,
            string effectRole,
            UnityEngine.Object sourceAsset = null,
            string originSocketId = "",
            string targetSocketId = "",
            bool fallbackUsed = false,
            bool allowed = true)
        {
            if (AuthoredContentInstance.Stamp(
                    instance,
                    definitionType,
                    contentId,
                    ResolveAssetGuid(sourceAsset),
                    ResolveAssetPath(sourceAsset),
                    prefabName,
                    ownerWeaponId,
                    ownerAttackId,
                    effectRole,
                    definitionType,
                    ownerWeaponId,
                    ownerAttackId,
                    effectRole,
                    originSocketId,
                    targetSocketId,
                    "IdleAutoDefenseTemplateController",
                    fallbackUsed,
                    allowed) != null)
            {
                _counters.AuthoredVisibleInstanceStampCount++;
            }
        }

        internal static string ResolveAssetPath(UnityEngine.Object asset)
        {
#if UNITY_EDITOR
            return asset == null ? string.Empty : UnityEditor.AssetDatabase.GetAssetPath(asset);
#else
            return string.Empty;
#endif
        }

        internal static string ResolveAssetGuid(UnityEngine.Object asset)
        {
#if UNITY_EDITOR
            string path = ResolveAssetPath(asset);
            return string.IsNullOrWhiteSpace(path) ? string.Empty : UnityEditor.AssetDatabase.AssetPathToGUID(path);
#else
            return string.Empty;
#endif
        }

        internal static string ResolveMuzzleSocketId(AttackDefinitionAsset attack)
        {
            return attack == null || string.IsNullOrWhiteSpace(attack.Id) ? string.Empty : attack.Id + ":muzzle.primary";
        }

        internal static string ResolveTargetSocketId(AttackPresentationEventRecipe recipe)
        {
            if (recipe == null) return string.Empty;
            if (recipe.SpawnPointRole == AttackPresentationSpawnPointRole.Target) return "target.center";
            if (recipe.SpawnPointRole == AttackPresentationSpawnPointRole.ImpactPoint) return "target.impact";
            return string.Empty;
        }
    }
}
