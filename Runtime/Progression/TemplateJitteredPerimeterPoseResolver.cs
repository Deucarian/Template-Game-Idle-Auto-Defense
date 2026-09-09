using System;
using System.Collections.Generic;
using Deucarian.AutoDefense;
using Deucarian.WorldSpawning;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    internal sealed class TemplateJitteredPerimeterPoseResolver : IAutoDefensePoseResolver, ISpawnPoseResolver
    {
        private const float AngleJitterDegrees = 17.5f;
        private const float RadiusJitter = 2.5f;
        private readonly AutoDefenseObjectiveDefinition _objective;
        private readonly Dictionary<WorldSpawnChannelId, AutoDefenseSpawnChannelDefinition> _channels = new Dictionary<WorldSpawnChannelId, AutoDefenseSpawnChannelDefinition>();
        private readonly float _radius;

        public TemplateJitteredPerimeterPoseResolver(AutoDefenseObjectiveDefinition objective, AutoDefenseSpawnRingDefinition ring)
        {
            _objective = objective ?? throw new ArgumentNullException(nameof(objective));
            if (ring == null) throw new ArgumentNullException(nameof(ring));
            _radius = ring.Radius;
            for (int i = 0; i < ring.Channels.Count; i++)
                _channels.Add(ring.Channels[i].Id, ring.Channels[i]);
        }

        public bool TryResolvePose(WorldSpawnChannelId channelId, out SpawnPose pose)
        {
            return TryResolvePose(channelId, 0L, 0, string.Empty, out pose);
        }

        public SpawnPoseResult TryResolvePose(WorldSpawnRequest request)
        {
            return TryResolvePose(request.ChannelId, request.Sequence, request.Context.Tick, request.Context.GroupId, out SpawnPose pose)
                ? SpawnPoseResult.Success(pose)
                : SpawnPoseResult.Failure("Unknown auto-defense channel: " + request.ChannelId);
        }

        private bool TryResolvePose(WorldSpawnChannelId channelId, long sequence, int tick, string groupId, out SpawnPose pose)
        {
            if (!_channels.TryGetValue(channelId, out AutoDefenseSpawnChannelDefinition channel))
            {
                pose = default;
                return false;
            }

            int hash = StableHash(channelId.Value);
            hash = CombineHash(hash, StableHash(groupId));
            hash = CombineHash(hash, sequence.GetHashCode());
            hash = CombineHash(hash, tick);
            float angleOffset = (Hash01(hash) * 2f - 1f) * AngleJitterDegrees;
            float distanceOffset = Hash01(CombineHash(hash, 7919)) * RadiusJitter;
            float radians = (channel.AngleDegrees + angleOffset) * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians));
            float distance = _radius + distanceOffset;
            pose = new SpawnPose(_objective.Position + direction * distance, Quaternion.LookRotation(-direction, Vector3.up));
            return true;
        }

        private static int CombineHash(int current, int value)
        {
            unchecked { return (current * 397) ^ value; }
        }

        private static float Hash01(int hash)
        {
            unchecked
            {
                uint value = (uint)hash;
                value ^= value >> 16;
                value *= 2246822519u;
                value ^= value >> 13;
                value *= 3266489917u;
                value ^= value >> 16;
                return (value & 0x00FFFFFFu) / 16777215f;
            }
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                int hash = 5381;
                if (!string.IsNullOrEmpty(value))
                {
                    for (int i = 0; i < value.Length; i++)
                        hash = ((hash << 5) + hash) ^ value[i];
                }

                return hash;
            }
        }
    }
}
