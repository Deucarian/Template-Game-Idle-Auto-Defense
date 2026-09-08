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
    internal sealed class IdleAutoDefenseEventPresentation
    {
        private readonly IdleAutoDefenseWorldResources _resources;

        private readonly IdleAutoDefensePresentationCounters _counters;

        private readonly IdleAutoDefenseVisibleInstanceStamping _stamps;

        private readonly IdleAutoDefenseVisualAssets _assets;

        private readonly IdleAutoDefenseTargetPresentation _targets;

        private readonly IdleAutoDefenseBeamPresentation _beams;

        private readonly IdleAutoDefensePresentationQueries _queries;

        internal IdleAutoDefenseEventPresentation(IdleAutoDefenseWorldResources resources, IdleAutoDefensePresentationCounters counters, IdleAutoDefenseVisibleInstanceStamping stamps, IdleAutoDefenseVisualAssets assets, IdleAutoDefenseTargetPresentation targets, IdleAutoDefenseBeamPresentation beams, IdleAutoDefensePresentationQueries queries)
        {
            _resources = resources;
            _counters = counters;
            _stamps = stamps;
            _assets = assets;
            _targets = targets;
            _beams = beams;
            _queries = queries;
        }

        internal void EmitEnemyPresentationEvent(AutoDefenseEnemySnapshot enemy, EnemyPresentationEventKind eventKind)
        {
            _counters.EnemyPresentationEventCount++;
            EnemyDefinitionAsset definition = _queries.FindEnemy(enemy.SpawnableId);
            bool emittedVfx = false;
            bool emittedAudio = false;
            Vector3 position = IdleAutoDefensePresentationGeometry.EnemyAimPosition(enemy.Position);
            IdleAutoDefenseEnemyModelPresentation presentation = _targets.BindEnemyModelPresentation(enemy);
            if (presentation != null)
            {
                if (eventKind == EnemyPresentationEventKind.OnHit && presentation.PlayHitFeedback())
                    _counters.EnemyHitFlashCount++;
                if (eventKind == EnemyPresentationEventKind.OnDeath && presentation.PlayDeathFeedback())
                    _counters.EnemyDeathPopCount++;
            }

            if (definition != null &&
                definition.Presentation != null &&
                definition.Presentation.TryGetEvent(eventKind, out EnemyPresentationEventRecipe recipe))
            {
                emittedVfx = EmitPresentationVfx(
                    recipe.VfxPrefab,
                    position,
                    "EnemyPresentation",
                    definition.Id,
                    string.Empty,
                    string.Empty,
                    eventKind.ToString(),
                    recipe.VfxPrefab,
                    string.Empty,
                    eventKind == EnemyPresentationEventKind.OnSpawn ? string.Empty : "enemy.center");
                emittedAudio = PlayPresentationAudio(recipe.AudioClip);
            }

            if (!emittedVfx)
                EmitFallbackPresentationVfx(position, ResolveEnemyEventColor(eventKind), 0.34f);
            if (!emittedAudio)
                PlayPresentationAudio(null);
        }

        internal void EmitAttackEvent(AttackDefinitionAsset attack, AttackPresentationEventKind eventKind, Vector3 eventPosition, long targetEnemyId = 0)
        {
            bool emittedVfx = false;
            bool emittedAudio = false;
            if (eventKind == AttackPresentationEventKind.OnImpact)
                emittedVfx = _beams.TryEmitBeamVfx(attack, eventPosition, targetEnemyId);
            if (attack != null &&
                attack.Presentation != null &&
                attack.Presentation.TryGetEvent(eventKind, out AttackPresentationEventRecipe recipe))
            {
                Vector3 position = _targets.ResolveAttackEventPosition(attack, recipe, eventPosition);
                bool recipeUsesBeamPrefab = IdleAutoDefenseBeamPresentation.IsBeamPresentationPrefab(attack, recipe.VfxPrefab);
                if (recipeUsesBeamPrefab)
                {
                    if (eventKind == AttackPresentationEventKind.OnImpact && !emittedVfx)
                        emittedVfx = _beams.TryEmitBeamVfx(attack, eventPosition, targetEnemyId);
                    else
                        emittedVfx = true;
                }
                else
                {
                    emittedVfx = EmitPresentationVfx(
                        recipe.VfxPrefab,
                        position,
                        "AttackPresentation",
                        attack.Id,
                        _targets.ResolveWeaponIdForAttack(attack),
                        attack.Id,
                        eventKind.ToString(),
                        recipe.VfxPrefab,
                        IdleAutoDefenseVisibleInstanceStamping.ResolveMuzzleSocketId(attack),
                        IdleAutoDefenseVisibleInstanceStamping.ResolveTargetSocketId(recipe)) || emittedVfx;
                }
                emittedAudio = PlayPresentationAudio(recipe.AudioClip);
            }

            if (!emittedVfx)
            {
                EmitFallbackPresentationVfx(eventPosition, _targets.ResolveAttackColor(attack), ResolveAttackEventScale(eventKind));
                if (eventKind == AttackPresentationEventKind.OnFire)
                    _counters.MuzzleFlashSpawnCount++;
            }
            if (!emittedAudio && eventKind != AttackPresentationEventKind.OnTick)
                PlayPresentationAudio(null);
        }

        internal bool EmitPresentationVfx(
            GameObject prefab,
            Vector3 position,
            string definitionType,
            string contentId,
            string ownerWeaponId,
            string ownerAttackId,
            string effectRole,
            UnityEngine.Object sourceAsset = null,
            string originSocketId = "",
            string targetSocketId = "")
        {
            if (prefab == null) return false;
            GameObject instance = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
            instance.name = prefab.name + " Runtime";
            if (_resources.Root != null) instance.transform.SetParent(_resources.Root.transform, true);
            _stamps.StampAuthoredVisibleInstance(instance, definitionType, contentId, prefab.name, ownerWeaponId, ownerAttackId, effectRole, sourceAsset ?? prefab, originSocketId, targetSocketId);
            instance.SetActive(true);
            IdleAutoDefenseVisualAssets.DisableColliders(instance);
            ParticleSystem[] particles = instance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].gameObject.SetActive(true);
                particles[i].Play(true);
            }

            _counters.AttackVfxSpawnCount++;
            if (string.Equals(effectRole, AttackPresentationEventKind.OnFire.ToString(), StringComparison.OrdinalIgnoreCase))
                _counters.MuzzleFlashSpawnCount++;
            UnityObjectUtility.DestroySafely(instance, 2f);
            return true;
        }

        internal void EmitFallbackPresentationVfx(Vector3 position, Color color, float scale)
        {
            if (_resources.Root == null) return;
            _counters.FallbackVisibleGameplaySpawnCount++;
            if (EmitKenneySpriteBurst("Kenney Presentation Burst", "Art/impact_flame", position, color, Mathf.Max(0.45f, scale * 1.8f), 0.42f, 0.18f, 50))
            {
                _counters.AttackVfxSpawnCount++;
                return;
            }

            GameObject instance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            instance.name = "Template Presentation VFX";
            instance.transform.SetParent(_resources.Root.transform, false);
            instance.transform.position = position;
            instance.transform.localScale = Vector3.one * Mathf.Max(0.08f, scale);
            IdleAutoDefenseVisualAssets.ApplyColor(instance, color);
            IdleAutoDefenseVisualAssets.DisableColliders(instance);
            _counters.AttackVfxSpawnCount++;
            UnityObjectUtility.DestroySafely(instance, 0.45f);
        }

        internal void EmitAttackTracer(Vector3 origin, Vector3 destination, Color color)
        {
            if (!_resources.ShowDebugAimLines()) return;
            if (_resources.Root == null) return;
            GameObject tracer = new GameObject("Template Attack Tracer");
            tracer.transform.SetParent(_resources.Root.transform, false);
            LineRenderer line = tracer.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPosition(0, origin);
            line.SetPosition(1, destination);
            line.startWidth = 0.12f;
            line.endWidth = 0.035f;
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            if (shader != null)
                line.sharedMaterial = IdleAutoDefenseMaterialLifetime.Own(tracer, new Material(shader) { color = color });
            line.startColor = color;
            line.endColor = new Color(color.r, color.g, color.b, 0.2f);
            _counters.AttackVfxSpawnCount++;
            _counters.DebugAimTracerSpawnCount++;
            UnityObjectUtility.DestroySafely(tracer, 0.32f);
        }

        internal void EmitKenneyEnemyEventBurst(Vector3 position, EnemyPresentationEventKind eventKind)
        {
            if (eventKind == EnemyPresentationEventKind.OnSpawn)
            {
                EmitKenneySpriteBurst("Enemy Spawn Ring", "Art/build_pad_target", position + Vector3.down * 0.25f, new Color(0.35f, 1f, 0.55f, 0.9f), 0.72f, 0.36f, 0.05f, 18);
                return;
            }

            if (eventKind == EnemyPresentationEventKind.OnHit)
            {
                EmitKenneySpriteBurst("Enemy Hit Spark", "Art/impact_flame", position, new Color(1f, 0.92f, 0.22f), 0.52f, 0.34f, 0.18f, 60);
                TriggerCameraShake(0.045f, 0.025f);
                return;
            }

            if (eventKind == EnemyPresentationEventKind.OnDeath)
            {
                EmitKenneySpriteBurst("Enemy Death Pop", "Art/impact_flame", position, new Color(1f, 0.24f, 0.08f), 1.08f, 0.58f, 0.42f, 62);
                TriggerCameraShake(0.12f, 0.065f);
            }
        }

        internal void EmitKenneyAttackEventBurst(AttackDefinitionAsset attack, AttackPresentationEventKind eventKind, Vector3 eventPosition)
        {
            Color color = _targets.ResolveAttackColor(attack);
            if (eventKind == AttackPresentationEventKind.OnFire)
            {
                EmitKenneySpriteBurst("Muzzle Flash", "Art/impact_flame", _targets.ResolveTowerMuzzlePosition(attack), color, 0.45f, 0.26f, 0.1f, 58);
                return;
            }

            if (eventKind == AttackPresentationEventKind.OnImpact)
            {
                EmitKenneySpriteBurst("Attack Impact Pop", "Art/impact_flame", eventPosition, color, 0.72f, 0.42f, 0.2f, 61);
                TriggerCameraShake(0.06f, 0.035f);
            }
        }

        internal bool EmitKenneySpriteBurst(string name, string artPath, Vector3 position, Color color, float scale, float duration, float rise, int sortingOrder)
        {
            if (_resources.Root == null || string.IsNullOrWhiteSpace(artPath)) return false;
            Sprite sprite = Resources.Load<Sprite>(IdleAutoDefenseVisualAssets.KenneyResourceRoot + artPath);
            if (sprite == null) return false;

            GameObject instance = new GameObject(name);
            instance.transform.SetParent(_resources.Root.transform, false);
            instance.transform.position = position;
            instance.transform.localScale = Vector3.one * Mathf.Max(0.05f, scale);
            SpriteRenderer renderer = instance.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            var billboard = instance.AddComponent<KenneyBillboardVisual>();
            billboard.Configure(true);
            var burst = instance.AddComponent<KenneySpriteBurstVisual>();
            burst.Configure(renderer, Mathf.Max(0.1f, duration), Mathf.Max(0f, rise), instance.transform.localScale);
            UnityObjectUtility.DestroySafely(instance, Mathf.Max(0.12f, duration) + 0.08f);
            return true;
        }

        internal bool PlayPresentationAudio(AudioClip clip)
        {
            AudioClip playableClip = clip != null ? clip : GetFallbackPresentationClip();
            if (playableClip == null) return false;
            _counters.AttackAudioPlayCount++;
            if (_resources.AudioSource != null && Application.isPlaying)
                _resources.AudioSource.PlayOneShot(playableClip);
            return true;
        }

        internal void TriggerCameraShake(float durationSeconds, float magnitude) => _resources.Shake.Trigger(durationSeconds, magnitude);

        internal void UpdateCameraShake(float deltaSeconds) => _resources.CameraShake?.Update(deltaSeconds, _resources.SurvivalSeconds());

        internal AudioClip GetFallbackPresentationClip()
        {
            if (_resources.FallbackClip != null) return _resources.FallbackClip;
            AudioClip kenneyClip = Resources.Load<AudioClip>(IdleAutoDefenseVisualAssets.KenneyResourceRoot + "Audio/laserSmall_000");
            if (kenneyClip != null)
            {
                _resources.FallbackClip = kenneyClip;
                _resources.FallbackClipOwned = false;
                return _resources.FallbackClip;
            }

            const int sampleRate = 22050;
            const int sampleCount = 2205;
            float[] samples = new float[sampleCount];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = 1f - (float)i / samples.Length;
                samples[i] = Mathf.Sin(2f * Mathf.PI * 660f * t) * 0.18f * envelope;
            }

            _resources.FallbackClip = AudioClip.Create("Template Presentation Click", sampleCount, 1, sampleRate, false);
            _resources.FallbackClip.hideFlags = HideFlags.HideAndDontSave;
            _resources.FallbackClip.SetData(samples, 0);
            _resources.FallbackClipOwned = true;
            return _resources.FallbackClip;
        }

        internal static Color ResolveEnemyEventColor(EnemyPresentationEventKind eventKind)
        {
            if (eventKind == EnemyPresentationEventKind.OnDeath) return new Color(1f, 0.25f, 0.18f);
            if (eventKind == EnemyPresentationEventKind.OnHit) return new Color(1f, 0.95f, 0.25f);
            return new Color(0.35f, 1f, 0.55f);
        }

        internal static float ResolveAttackEventScale(AttackPresentationEventKind eventKind)
        {
            if (eventKind == AttackPresentationEventKind.OnImpact) return 0.42f;
            if (eventKind == AttackPresentationEventKind.OnExpire) return 0.28f;
            return 0.3f;
        }
    }
}
