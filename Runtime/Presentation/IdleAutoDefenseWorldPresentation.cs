using System;
using System.Collections.Generic;
using Deucarian.Attacks.Authoring;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    // Composes world-only resource and presentation owners around explicit run read ports.
    internal sealed class IdleAutoDefenseWorldPresentation
    {
        internal readonly IdleAutoDefenseWorldResources Resources;
        internal readonly IdleAutoDefensePresentationCounters Counters;
        internal readonly IdleAutoDefenseVisualAssets Assets;
        internal readonly IdleAutoDefenseVisibleInstanceStamping Stamps;
        internal readonly IdleAutoDefenseSpawnablePresentation Spawnables;
        internal readonly IdleAutoDefenseArenaPresentation Arena;
        internal readonly IdleAutoDefenseTargetPresentation Targets;
        internal readonly IdleAutoDefenseBeamPresentation Beams;
        internal readonly IdleAutoDefenseEventPresentation Events;

        internal IdleAutoDefenseWorldPresentation(IdleAutoDefenseContentBinding content,
            IdleAutoDefensePresentationQueries queries, Func<bool> showDebugAimLines, Func<float> survivalSeconds,
            IdleAutoDefensePresentationCounters counters = null)
        {
            Resources = new IdleAutoDefenseWorldResources(showDebugAimLines, survivalSeconds);
            Counters = counters ?? new IdleAutoDefensePresentationCounters();
            Stamps = new IdleAutoDefenseVisibleInstanceStamping(Counters);
            Assets = new IdleAutoDefenseVisualAssets(Resources, Counters, Stamps);
            Targets = new IdleAutoDefenseTargetPresentation(content, Counters, queries);
            Beams = new IdleAutoDefenseBeamPresentation(Resources, Counters, Stamps, Targets, queries);
            Events = new IdleAutoDefenseEventPresentation(Resources, Counters, Stamps, Assets, Targets, Beams, queries);
            Spawnables = new IdleAutoDefenseSpawnablePresentation(Resources, Assets, Targets, queries);
            Arena = new IdleAutoDefenseArenaPresentation(Resources, Assets, Targets, Stamps, content, Counters, queries);
        }

        internal void Dispose(bool destroySceneObjects)
        {
            Beams.ClearActiveBeamVisuals();
            Targets.WeaponBindings.Clear();
            Targets.EnemyBindings.Clear();
            Resources.Dispose(destroySceneObjects);
        }

        internal void ResetTransientFeedback()
        {
            Resources.CameraShake?.ResetEnvelope();
            Beams.ClearActiveBeamVisuals();
        }

        internal void Initialize(Vector3 objectivePosition, IReadOnlyList<EnemyDefinitionAsset> enemies, Action initializeUi)
        {
            if (initializeUi == null) throw new ArgumentNullException(nameof(initializeUi));
            Resources.Root = new GameObject("Basic Idle Auto Defense Runtime");
            Resources.AudioSource = Resources.Root.AddComponent<AudioSource>();
            Resources.AudioSource.playOnAwake = false;
            Resources.AudioSource.spatialBlend = 0f;
            Resources.AudioSource.volume = 0.75f;
            if (UnityEngine.Object.FindFirstObjectByType<AudioListener>() == null)
                Resources.Root.AddComponent<AudioListener>();
            initializeUi();
            Arena.ConfigureGameplayCamera(objectivePosition);
            Arena.ConfigureGameplayLighting();
            Arena.CreateArenaBackdrop();
            Arena.CreateCorePresentation(objectivePosition);
            Arena.CreatePlayAreaMarkers();

            string firstEnemyId = enemies.Count == 0 || enemies[0] == null
                ? BasicIdleAutoDefenseGame.SwarmEnemySpawnableId.Value
                : enemies[0].Id;
            Resources.EnemyPrefab = Assets.CreateEnemyModelPrefab("Template Idle Enemy Runtime Prefab", firstEnemyId, IdleAutoDefenseVisualAssets.ResolveEnemyFallbackColor(firstEnemyId));
            Resources.ProjectilePrefab = Assets.CreateProjectileModelPrefab("Template Idle Projectile Runtime Prefab", "weapon-ammo-arrow", new Color(1f, 0.45f, 0.1f));
        }
    }
}
