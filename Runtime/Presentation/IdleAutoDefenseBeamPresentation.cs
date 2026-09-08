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
    internal sealed class IdleAutoDefenseBeamPresentation
    {
        private readonly IdleAutoDefenseWorldResources _resources;

        private readonly IdleAutoDefensePresentationCounters _counters;

        private readonly IdleAutoDefenseVisibleInstanceStamping _stamps;

        private readonly IdleAutoDefenseTargetPresentation _targets;

        private readonly IdleAutoDefensePresentationQueries _queries;

        internal IdleAutoDefenseBeamPresentation(IdleAutoDefenseWorldResources resources, IdleAutoDefensePresentationCounters counters, IdleAutoDefenseVisibleInstanceStamping stamps, IdleAutoDefenseTargetPresentation targets, IdleAutoDefensePresentationQueries queries)
        {
            _resources = resources;
            _counters = counters;
            _stamps = stamps;
            _targets = targets;
            _queries = queries;
        }

        internal IdleAutoDefenseBeamVisuals Active;
        private IdleAutoDefenseBeamVisuals BeamVisuals => Active ??= new IdleAutoDefenseBeamVisuals(ResolveBeamTargetPosition, _targets.ResolveTowerMuzzlePosition, AlignBeamInstance);

        internal bool TryEmitBeamVfx(AttackDefinitionAsset attack, Vector3 impactPosition, long targetEnemyId)
        {
            if (!TryGetBeamVfxPrefab(attack, out GameObject prefab)) return false;
            Vector3 origin = _targets.ResolveTowerMuzzlePosition(attack);
            if (!IsFiniteVector(origin) || !IsFiniteVector(impactPosition))
            {
                _counters.BeamVisualInvalidEndpointCount++;
                return false;
            }

            Vector3 delta = impactPosition - origin;
            float distance = delta.magnitude;
            if (distance <= 0.05f)
            {
                _counters.BeamVisualInvalidEndpointCount++;
                return false;
            }

            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            instance.name = prefab.name + " Runtime Beam";
            if (_resources.Root != null) instance.transform.SetParent(_resources.Root.transform, true);
            _stamps.StampAuthoredVisibleInstance(
                instance,
                "BeamVfx",
                attack == null ? string.Empty : attack.Id,
                prefab.name,
                _targets.ResolveWeaponIdForAttack(attack),
                attack == null ? string.Empty : attack.Id,
                "Beam",
                prefab,
                IdleAutoDefenseVisibleInstanceStamping.ResolveMuzzleSocketId(attack),
                "target.center");
            ConfigureBeamLineRenderer(instance, prefab, attack);
            HideBeamMeshRenderers(instance);
            AlignBeamInstance(instance, prefab, origin, impactPosition);
            instance.SetActive(true);
            PrepareBeamRenderers(instance, _targets.ResolveAttackColor(attack));
            IdleAutoDefenseVisualAssets.DisableColliders(instance);

            ParticleSystem[] particles = instance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].gameObject.SetActive(true);
                particles[i].Play(true);
            }

            _counters.AttackVfxSpawnCount++;
            _counters.BeamVisualSpawnCount++;
            BeamVisuals.Add(instance, prefab, attack, targetEnemyId, impactPosition, ResolveBeamDurationSeconds(attack));
            return true;
        }

        internal static bool TryGetBeamVfxPrefab(AttackDefinitionAsset attack, out GameObject prefab)
        {
            prefab = null;
            if (attack == null || attack.Delivery == null) return false;
            if (attack.Delivery.Mode != AttackRecipeDeliveryMode.Hitscan) return false;
            prefab = attack.Delivery.BeamVfxPrefab;
            return prefab != null;
        }

        internal static bool IsBeamPresentationPrefab(AttackDefinitionAsset attack, GameObject prefab)
        {
            if (prefab == null) return false;
            return TryGetBeamVfxPrefab(attack, out GameObject beamPrefab) && prefab == beamPrefab;
        }

        internal static Vector3 ResolveBeamWorldScale(GameObject prefab, float distance)
        {
            Vector3 sourceScale = prefab == null ? Vector3.one : prefab.transform.localScale;
            float width = Mathf.Clamp(Mathf.Max(Mathf.Abs(sourceScale.x), Mathf.Abs(sourceScale.y), 0.08f), 0.08f, 0.38f);
            return new Vector3(width, width, Mathf.Max(0.05f, distance));
        }

        internal static float ResolveBeamDurationSeconds(AttackDefinitionAsset attack)
        {
            float authoredTick = attack != null && attack.Delivery != null ? attack.Delivery.TickIntervalSeconds : 0.5f;
            return Mathf.Clamp(authoredTick * 0.44f, 0.14f, 0.3f);
        }

        internal static bool AlignBeamInstance(GameObject instance, GameObject prefab, Vector3 origin, Vector3 impactPosition)
        {
            if (instance == null || !IsFiniteVector(origin) || !IsFiniteVector(impactPosition)) return false;
            Vector3 delta = impactPosition - origin;
            float distance = delta.magnitude;
            if (distance <= 0.05f) return false;
            LineRenderer lineRenderer = instance.GetComponentInChildren<LineRenderer>(true);
            if (lineRenderer != null)
            {
                instance.transform.position = origin;
                instance.transform.rotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
                lineRenderer.useWorldSpace = true;
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, origin);
                lineRenderer.SetPosition(1, impactPosition);
                return true;
            }

            instance.transform.position = origin + delta * 0.5f;
            instance.transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
            instance.transform.localScale = ResolveBeamWorldScale(prefab, distance);
            return true;
        }

        internal void ConfigureBeamLineRenderer(GameObject instance, GameObject prefab, AttackDefinitionAsset attack)
        {
            if (instance == null) return;
            LineRenderer lineRenderer = instance.GetComponentInChildren<LineRenderer>(true);
            if (lineRenderer == null)
                lineRenderer = instance.AddComponent<LineRenderer>();

            float width = ResolveBeamLineWidth(prefab);
            lineRenderer.enabled = true;
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 2;
            lineRenderer.widthMultiplier = width;
            lineRenderer.widthCurve = new AnimationCurve(
                new Keyframe(0f, 0.35f),
                new Keyframe(0.08f, 1f),
                new Keyframe(0.82f, 0.72f),
                new Keyframe(1f, 0.22f));
            lineRenderer.numCapVertices = 8;
            lineRenderer.numCornerVertices = 2;
            lineRenderer.alignment = LineAlignment.View;
            lineRenderer.textureMode = LineTextureMode.Stretch;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
            lineRenderer.generateLightingData = false;
            lineRenderer.sortingOrder = 18;
            lineRenderer.sharedMaterial = IdleAutoDefenseMaterialLifetime.Own(instance, CreateBeamLineMaterial(prefab));
            ApplyBeamLineColors(lineRenderer, _targets.ResolveAttackColor(attack));
        }

        internal static float ResolveBeamLineWidth(GameObject prefab)
        {
            Vector3 sourceScale = prefab == null ? Vector3.one : prefab.transform.localScale;
            float width = Mathf.Max(Mathf.Abs(sourceScale.x), Mathf.Abs(sourceScale.y), 0.08f);
            return Mathf.Clamp(width * 1.8f, 0.16f, 0.42f);
        }

        internal static Material ResolveBeamSourceMaterial(GameObject prefab)
        {
            if (prefab != null)
            {
                Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null && renderers[i].sharedMaterial != null)
                        return renderers[i].sharedMaterial;
                }
            }

            return null;
        }

        internal static Material CreateBeamLineMaterial(GameObject prefab)
        {
            Material source = ResolveBeamSourceMaterial(prefab);
            bool ownsSource = source == null;
            if (ownsSource)
            {
                Shader fallbackShader = Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
                source = fallbackShader != null ? new Material(fallbackShader) : null;
            }
            try
            {
                Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Standard");
                Material material = shader != null ? new Material(shader) : (source != null ? new Material(source) : null);
                if (material == null) return null;
                material.name = (source != null ? source.name : "PulseBeamEnergy") + " Runtime Line";
                Color sourceColor = Color.white;
                if (source != null)
                {
                    if (source.HasProperty("_EmissionColor"))
                        sourceColor = source.GetColor("_EmissionColor");
                    else if (source.HasProperty("_Color"))
                        sourceColor = source.color;
                }

                sourceColor.a = 1f;
                if (material.HasProperty("_Color"))
                    material.color = sourceColor;
                if (material.HasProperty("_EmissionColor"))
                    material.SetColor("_EmissionColor", sourceColor * 1.35f);
                return material;
            }
            finally
            {
                if (ownsSource) UnityObjectUtility.DestroySafely(source);
            }
        }

        internal static void HideBeamMeshRenderers(GameObject instance)
        {
            if (instance == null) return;
            MeshRenderer[] meshRenderers = instance.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < meshRenderers.Length; i++)
                meshRenderers[i].enabled = false;
        }

        internal static void ApplyBeamLineColors(LineRenderer lineRenderer, Color color)
        {
            if (lineRenderer == null) return;
            Color start = Color.Lerp(Color.white, color, 0.18f);
            start.a = 1f;
            Color end = Color.Lerp(Color.white, color, 0.52f);
            end.a = 1f;
            lineRenderer.startColor = start;
            lineRenderer.endColor = end;
        }

        internal static void PrepareBeamRenderers(GameObject instance, Color color)
        {
            if (instance == null) return;
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                Material source = renderer.sharedMaterial;
                Material material = source != null ? new Material(source) : (shader != null ? new Material(shader) : null);
                if (material != null)
                {
                    if (material.HasProperty("_Color"))
                        material.color = Color.Lerp(Color.white, color, 0.5f);
                    if (material.HasProperty("_EmissionColor"))
                        material.SetColor("_EmissionColor", color * 1.25f);
                    renderer.sharedMaterial = IdleAutoDefenseMaterialLifetime.Own(instance, material);
                }

                if (renderer is LineRenderer lineRenderer)
                    ApplyBeamLineColors(lineRenderer, color);
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        internal Vector3? ResolveBeamTargetPosition(long targetEnemyId)
        {
            return _queries.TryFindEnemy(targetEnemyId, out AutoDefenseEnemySnapshot enemy)
                ? IdleAutoDefensePresentationGeometry.EnemyAimPosition(enemy.Position) : (Vector3?)null;
        }

        internal void UpdateActiveBeamVisuals(float deltaSeconds)
        {
            if (Active != null) _counters.BeamVisualInvalidEndpointCount += Active.Update(deltaSeconds);
        }

        internal static bool IsFiniteVector(Vector3 value)
        {
            return !(float.IsNaN(value.x) || float.IsNaN(value.y) || float.IsNaN(value.z) ||
                float.IsInfinity(value.x) || float.IsInfinity(value.y) || float.IsInfinity(value.z));
        }

        internal void ClearActiveBeamVisuals() => Active?.Clear();
    }
}
