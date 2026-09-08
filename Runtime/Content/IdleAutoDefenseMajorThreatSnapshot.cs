using System;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    public readonly struct IdleAutoDefenseMajorThreatSnapshot
    {
        public IdleAutoDefenseMajorThreatSnapshot(
            long instanceId,
            string displayName,
            bool boss,
            double health,
            double maximumHealth,
            Vector3 worldPosition)
        {
            InstanceId = instanceId;
            DisplayName = displayName ?? string.Empty;
            Boss = boss;
            Health = Math.Max(0d, health);
            MaximumHealth = Math.Max(1d, maximumHealth);
            WorldPosition = worldPosition;
        }

        public long InstanceId { get; }
        public string DisplayName { get; }
        public bool Boss { get; }
        public double Health { get; }
        public double MaximumHealth { get; }
        public float HealthNormalized => Mathf.Clamp01((float)(Health / MaximumHealth));
        public Vector3 WorldPosition { get; }
    }
}
