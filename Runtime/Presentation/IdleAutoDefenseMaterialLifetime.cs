using System;
using System.Collections.Generic;
using Deucarian.Common;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    // A clone borrows prefab materials. Only materials created for this exact object are released here.
    [ExecuteAlways]
    internal sealed class IdleAutoDefenseMaterialLifetime : MonoBehaviour
    {
        [NonSerialized] private readonly List<Material> _owned = new List<Material>();

        internal static Material Own(GameObject instance, Material material)
        {
            if (instance == null || material == null) return material;
            IdleAutoDefenseMaterialLifetime lifetime = instance.GetComponent<IdleAutoDefenseMaterialLifetime>();
            if (lifetime == null) lifetime = instance.AddComponent<IdleAutoDefenseMaterialLifetime>();
            lifetime.hideFlags = HideFlags.HideInInspector;
            if (!lifetime._owned.Contains(material)) lifetime._owned.Add(material);
            return material;
        }

        private void OnDestroy()
        {
            ReleaseOwned();
        }

        internal static void Release(GameObject instance)
        {
            if (instance == null) return;
            foreach (IdleAutoDefenseMaterialLifetime lifetime in instance.GetComponentsInChildren<IdleAutoDefenseMaterialLifetime>(true))
                lifetime.ReleaseOwned();
        }

        private void ReleaseOwned()
        {
            for (int i = _owned.Count - 1; i >= 0; i--)
                UnityObjectUtility.DestroySafely(_owned[i]);
            _owned.Clear();
        }
    }
}
