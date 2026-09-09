using System;
using System.Collections.Generic;
using Deucarian.Common;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    internal sealed class IdleAutoDefenseWorldResources
    {
        private readonly HashSet<GameObject> _ownedObjects = new HashSet<GameObject>();
        private readonly HashSet<AudioClip> _ownedClips = new HashSet<AudioClip>();
        private GameObject _root;
        private bool _fallbackClipOwned;
        internal GameObject Root { get => _root; set { _root = value; if (value != null) _ownedObjects.Add(value); } }
        internal GameObject EnemyPrefab;
        internal GameObject ProjectilePrefab;
        internal readonly Dictionary<string, GameObject> EnemyPrefabs = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
        internal readonly Dictionary<string, GameObject> ProjectilePrefabs = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
        internal AudioSource AudioSource;
        internal AudioClip FallbackClip;
        internal bool FallbackClipOwned { get => _fallbackClipOwned; set { _fallbackClipOwned = value; if (value && FallbackClip != null) _ownedClips.Add(FallbackClip); } }
        internal IdleAutoDefenseCameraShake CameraShake;
        internal IdleAutoDefenseCameraShake Shake => CameraShake ??= new IdleAutoDefenseCameraShake(
            () => Camera.main != null ? Camera.main : UnityEngine.Object.FindFirstObjectByType<Camera>());
        internal readonly Func<bool> ShowDebugAimLines;
        internal readonly Func<float> SurvivalSeconds;

        internal IdleAutoDefenseWorldResources(Func<bool> showDebugAimLines, Func<float> survivalSeconds)
        {
            ShowDebugAimLines = showDebugAimLines;
            SurvivalSeconds = survivalSeconds;
        }

        internal GameObject OwnPrefab(GameObject prefab)
        {
            if (prefab != null) _ownedObjects.Add(prefab);
            return prefab;
        }

        internal void Dispose(bool destroySceneObjects)
        {
            foreach (GameObject instance in _ownedObjects)
            {
                // Inactive prefab instances may never receive MonoBehaviour.OnDestroy.
                IdleAutoDefenseMaterialLifetime.Release(instance);
                if (destroySceneObjects) UnityObjectUtility.DestroySafely(instance);
            }
            foreach (AudioClip clip in _ownedClips) UnityObjectUtility.DestroySafely(clip);
            _ownedObjects.Clear();
            _ownedClips.Clear();
            EnemyPrefabs.Clear();
            ProjectilePrefabs.Clear();
            EnemyPrefab = null;
            ProjectilePrefab = null;
            Root = null;
            AudioSource = null;
            FallbackClip = null;
            FallbackClipOwned = false;
            CameraShake?.Dispose();
            CameraShake = null;
        }
    }
}
