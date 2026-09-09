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
    internal sealed class IdleAutoDefenseVisualAssets
    {
        private readonly IdleAutoDefenseWorldResources _resources;

        private readonly IdleAutoDefensePresentationCounters _counters;

        private readonly IdleAutoDefenseVisibleInstanceStamping _stamps;

        internal IdleAutoDefenseVisualAssets(IdleAutoDefenseWorldResources resources, IdleAutoDefensePresentationCounters counters, IdleAutoDefenseVisibleInstanceStamping stamps)
        {
            _resources = resources;
            _counters = counters;
            _stamps = stamps;
        }

        internal const string KenneyResourceRoot = "Kenney/IdleAutoDefense/";
        internal const string Kenney3DResourceRoot = KenneyResourceRoot + "Models/TowerDefenseKit/FBX/";

        internal static void ApplyColor(GameObject instance, Color color)
        {
            Renderer renderer = instance.GetComponent<Renderer>();
            Shader shader = Shader.Find("Standard");
            if (renderer != null && shader != null)
                renderer.sharedMaterial = IdleAutoDefenseMaterialLifetime.Own(instance, new Material(shader) { color = color });
        }

        internal static void DisableColliders(GameObject instance)
        {
            if (instance == null) return;
            Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;
        }

        internal static void HideMeshRenderers(GameObject instance)
        {
            if (instance == null) return;
            MeshRenderer[] renderers = instance.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].enabled = false;
        }

        internal GameObject CreateEnemyModelPrefab(string name, string enemyId, Color tint)
        {
            GameObject prefab = new GameObject(name);
            GameObject model = InstantiateKenneyModel(ResolveEnemyModelName(enemyId), prefab.transform, Vector3.zero, Quaternion.identity, ResolveEnemyModelScale(enemyId), tint);
            if (model == null)
            {
                GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                fallback.name = "Hidden Enemy Fallback Mesh";
                fallback.transform.SetParent(prefab.transform, false);
                fallback.transform.localScale = ResolveEnemyModelScale(enemyId);
                IdleAutoDefenseVisualAssets.ApplyColor(fallback, tint);
            }

            var presentation = prefab.AddComponent<IdleAutoDefenseEnemyModelPresentation>();
            presentation.Configure(tint, Vector3.one);
            DisableColliders(prefab);
            prefab.SetActive(false);
            return _resources.OwnPrefab(prefab);
        }

        internal static IdleAutoDefenseEnemyModelPresentation EnsureEnemyModelPresentation(GameObject prefab, Color tint)
        {
            if (prefab == null) return null;
            IdleAutoDefenseEnemyModelPresentation presentation = prefab.GetComponent<IdleAutoDefenseEnemyModelPresentation>();
            if (presentation == null)
                presentation = prefab.AddComponent<IdleAutoDefenseEnemyModelPresentation>();
            presentation.Configure(tint, Vector3.one);
            return presentation;
        }

        internal GameObject CreateProjectileModelPrefab(string name, string modelName, Color tint)
        {
            GameObject prefab = new GameObject(name);
            GameObject model = InstantiateKenneyModel(modelName, prefab.transform, Vector3.zero, Quaternion.Euler(0f, 90f, 0f), Vector3.one * 0.34f, tint);
            if (model == null)
            {
                GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                fallback.name = "Hidden Projectile Fallback Mesh";
                fallback.transform.SetParent(prefab.transform, false);
                fallback.transform.localScale = Vector3.one * 0.2f;
                IdleAutoDefenseVisualAssets.ApplyColor(fallback, tint);
            }

            AddProjectileTrail(prefab, tint);
            DisableColliders(prefab);
            prefab.SetActive(false);
            return _resources.OwnPrefab(prefab);
        }

        internal GameObject InstantiateKenneyModel(string modelName, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Color? tint = null)
        {
            if (string.IsNullOrWhiteSpace(modelName)) return null;
            GameObject source = Resources.Load<GameObject>(IdleAutoDefenseVisualAssets.Kenney3DResourceRoot + modelName);
            if (source == null) return null;

            GameObject instance = UnityEngine.Object.Instantiate(source, parent, false);
            instance.name = "Kenney 3D " + modelName;
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;
            instance.transform.localScale = localScale;
            DisableColliders(instance);
            if (tint.HasValue)
                TintRenderers(instance, tint.Value);
            _counters.Kenney3DModelSpawnCount++;
            return instance;
        }

        internal void TintRenderers(GameObject instance, Color tint)
        {
            if (instance == null) return;
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                Material source = renderer.sharedMaterial;
                Material material = source != null ? new Material(source) : (shader != null ? new Material(shader) : null);
                if (material == null) continue;
                if (material.HasProperty("_Color"))
                    material.color = Color.Lerp(material.color, tint, 0.22f);
                renderer.sharedMaterial = IdleAutoDefenseMaterialLifetime.Own(instance, material);
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }

        internal GameObject CreateRuntimeVisualPrefab(
            string name,
            GameObject sourcePrefab,
            PrimitiveType fallbackPrimitive,
            Color color,
            string kenneyArtPath,
            Vector3 spriteLocalPosition,
            Vector3 spriteScale,
            bool projectile,
            int sortingOrder,
            string definitionType,
            string contentId,
            string ownerWeaponId,
            string ownerAttackId,
            string effectRole)
        {
            GameObject prefab = sourcePrefab != null
                ? UnityEngine.Object.Instantiate(sourcePrefab)
                : GameObject.CreatePrimitive(fallbackPrimitive);
            prefab.name = name;
            prefab.transform.SetParent(_resources.Root != null ? _resources.Root.transform : null, false);
            prefab.transform.localPosition = Vector3.zero;
            prefab.transform.localRotation = Quaternion.identity;
            prefab.transform.localScale = Vector3.one;
            DisableColliders(prefab);
            if (sourcePrefab != null)
            {
                IdleAutoDefenseKenneyModelPrefab[] authoredModels = prefab.GetComponentsInChildren<IdleAutoDefenseKenneyModelPrefab>(true);
                for (int i = 0; i < authoredModels.Length; i++)
                    authoredModels[i].EnsureModel();
                TintRenderers(prefab, color);
                _stamps.StampAuthoredVisibleInstance(prefab, definitionType, contentId, sourcePrefab.name, ownerWeaponId, ownerAttackId, effectRole, sourcePrefab);
            }
            else
            {
                _counters.FallbackVisibleGameplaySpawnCount++;
                IdleAutoDefenseVisualAssets.ApplyColor(prefab, color);
                bool attachedSprite = prefab.GetComponentInChildren<SpriteRenderer>(true) != null ||
                    AttachKenneySprite(prefab, kenneyArtPath, false, spriteLocalPosition, spriteScale, sortingOrder, color);
                if (attachedSprite)
                {
                    TintSpriteRenderers(prefab, color);
                    HideMeshRenderers(prefab);
                }
            }

            if (projectile)
                AddProjectileTrail(prefab, color);
            prefab.SetActive(false);
            return _resources.OwnPrefab(prefab);
        }

        internal static bool AttachKenneySprite(GameObject instance, string artPath, bool groundSprite, Vector3 localPosition, Vector3 localScale, int sortingOrder = 20, Color? tint = null)
        {
            if (instance == null || string.IsNullOrWhiteSpace(artPath)) return false;
            Sprite sprite = Resources.Load<Sprite>(IdleAutoDefenseVisualAssets.KenneyResourceRoot + artPath);
            if (sprite == null) return false;

            var spriteObject = new GameObject("Kenney Visual");
            Transform spriteParent = groundSprite && instance.transform.parent != null
                ? instance.transform.parent
                : instance.transform;
            spriteObject.transform.SetParent(spriteParent, false);
            spriteObject.transform.localPosition = groundSprite
                ? instance.transform.localPosition + localPosition
                : localPosition;
            spriteObject.transform.localRotation = groundSprite ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.identity;
            spriteObject.transform.localScale = localScale;
            SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = tint ?? Color.white;
            renderer.sortingOrder = sortingOrder;
            if (!groundSprite)
            {
                var billboard = spriteObject.AddComponent<KenneyBillboardVisual>();
                billboard.Configure(true);
            }

            return true;
        }

        internal void AddProjectileTrail(GameObject instance, Color color)
        {
            if (instance == null) return;
            TrailRenderer trail = instance.GetComponentInChildren<TrailRenderer>(true);
            if (trail == null)
                trail = instance.AddComponent<TrailRenderer>();
            trail.time = 0.32f;
            trail.startWidth = 0.22f;
            trail.endWidth = 0.02f;
            trail.minVertexDistance = 0.03f;
            trail.autodestruct = false;
            trail.emitting = true;
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            if (shader != null)
                trail.sharedMaterial = IdleAutoDefenseMaterialLifetime.Own(instance, new Material(shader) { color = color });
            trail.startColor = new Color(color.r, color.g, color.b, 0.88f);
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
        }

        internal static string ResolveEnemyKenneyArtPath(string enemyId)
        {
            if (string.IsNullOrWhiteSpace(enemyId)) return "Art/enemy_basic_green";
            if (enemyId.IndexOf("runner", StringComparison.OrdinalIgnoreCase) >= 0) return "Art/enemy_fast_gray";
            if (enemyId.IndexOf("boss", StringComparison.OrdinalIgnoreCase) >= 0 ||
                enemyId.IndexOf("elite", StringComparison.OrdinalIgnoreCase) >= 0 ||
                enemyId.IndexOf("shield", StringComparison.OrdinalIgnoreCase) >= 0 ||
                enemyId.IndexOf("tank", StringComparison.OrdinalIgnoreCase) >= 0) return "Art/enemy_tank_brown";
            return "Art/enemy_basic_green";
        }

        internal static Color ResolveEnemyFallbackColor(string enemyId)
        {
            if (string.IsNullOrWhiteSpace(enemyId)) return Color.red;
            if (enemyId.IndexOf("runner", StringComparison.OrdinalIgnoreCase) >= 0) return new Color(0.82f, 0.82f, 0.9f);
            if (enemyId.IndexOf("boss", StringComparison.OrdinalIgnoreCase) >= 0) return new Color(1f, 0.25f, 0.18f);
            if (enemyId.IndexOf("elite", StringComparison.OrdinalIgnoreCase) >= 0) return new Color(1f, 0.72f, 0.22f);
            if (enemyId.IndexOf("shield", StringComparison.OrdinalIgnoreCase) >= 0) return new Color(0.32f, 0.58f, 1f);
            if (enemyId.IndexOf("tank", StringComparison.OrdinalIgnoreCase) >= 0) return new Color(0.78f, 0.48f, 0.22f);
            return new Color(0.46f, 1f, 0.5f);
        }

        internal static Vector3 ResolveEnemySpriteScale(string enemyId)
        {
            if (string.IsNullOrWhiteSpace(enemyId)) return new Vector3(1.1f, 1.1f, 1f);
            if (enemyId.IndexOf("boss", StringComparison.OrdinalIgnoreCase) >= 0) return new Vector3(2.7f, 2.7f, 1f);
            if (enemyId.IndexOf("elite", StringComparison.OrdinalIgnoreCase) >= 0) return new Vector3(1.9f, 1.9f, 1f);
            if (enemyId.IndexOf("tank", StringComparison.OrdinalIgnoreCase) >= 0) return new Vector3(1.45f, 1.45f, 1f);
            if (enemyId.IndexOf("shield", StringComparison.OrdinalIgnoreCase) >= 0) return new Vector3(1.32f, 1.32f, 1f);
            if (enemyId.IndexOf("runner", StringComparison.OrdinalIgnoreCase) >= 0) return new Vector3(1.0f, 1.0f, 1f);
            return new Vector3(1.15f, 1.15f, 1f);
        }

        internal static string ResolveEnemyModelName(string enemyId)
        {
            if (string.IsNullOrWhiteSpace(enemyId)) return "enemy-ufo-a";
            if (enemyId.IndexOf("runner", StringComparison.OrdinalIgnoreCase) >= 0) return "enemy-ufo-b";
            if (enemyId.IndexOf("boss", StringComparison.OrdinalIgnoreCase) >= 0) return "enemy-ufo-d";
            if (enemyId.IndexOf("elite", StringComparison.OrdinalIgnoreCase) >= 0) return "enemy-ufo-d-weapon";
            if (enemyId.IndexOf("shield", StringComparison.OrdinalIgnoreCase) >= 0) return "enemy-ufo-c-weapon";
            if (enemyId.IndexOf("tank", StringComparison.OrdinalIgnoreCase) >= 0) return "enemy-ufo-c";
            return "enemy-ufo-a";
        }

        internal static Vector3 ResolveEnemyModelScale(string enemyId)
        {
            if (string.IsNullOrWhiteSpace(enemyId)) return Vector3.one * 0.72f;
            if (enemyId.IndexOf("boss", StringComparison.OrdinalIgnoreCase) >= 0) return Vector3.one * 1.48f;
            if (enemyId.IndexOf("elite", StringComparison.OrdinalIgnoreCase) >= 0) return Vector3.one * 1.16f;
            if (enemyId.IndexOf("tank", StringComparison.OrdinalIgnoreCase) >= 0) return Vector3.one * 1.02f;
            if (enemyId.IndexOf("shield", StringComparison.OrdinalIgnoreCase) >= 0) return Vector3.one * 0.95f;
            if (enemyId.IndexOf("runner", StringComparison.OrdinalIgnoreCase) >= 0) return Vector3.one * 0.66f;
            return Vector3.one * 0.72f;
        }

        internal static string ResolveProjectileModelName(AttackDefinitionAsset attack)
        {
            string attackId = attack == null ? string.Empty : attack.Id;
            if (attackId.IndexOf("homing", StringComparison.OrdinalIgnoreCase) >= 0) return "weapon-ammo-cannonball";
            if (attackId.IndexOf("arc", StringComparison.OrdinalIgnoreCase) >= 0) return "weapon-ammo-boulder";
            if (attackId.IndexOf("pulse", StringComparison.OrdinalIgnoreCase) >= 0) return "weapon-ammo-bullet";
            return "weapon-ammo-arrow";
        }

        internal static void TintSpriteRenderers(GameObject instance, Color tint)
        {
            if (instance == null) return;
            SpriteRenderer[] renderers = instance.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].color = tint;
        }
    }
}
