using System;
using Deucarian.Attacks;
using Deucarian.Attacks.Authoring;
using Deucarian.AutoDefense;
using Deucarian.WorldSpawning;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    /// <summary>Projectile spawn adapter that resolves authored attacks to the current turret muzzle transform.</summary>
    internal sealed class TemplateProjectileMuzzlePoseResolver : ISpawnPoseResolver
    {
        private readonly IIdleAutoDefenseProjectileMuzzleQueries _queries;
        private readonly WorldSpawnChannelId _channelId;

        public TemplateProjectileMuzzlePoseResolver(IIdleAutoDefenseProjectileMuzzleQueries queries, WorldSpawnChannelId channelId)
        {
            _queries = queries ?? throw new ArgumentNullException(nameof(queries));
            _channelId = channelId;
        }

        public SpawnPoseResult TryResolvePose(WorldSpawnRequest request)
        {
            if (!request.ChannelId.Equals(_channelId))
                return SpawnPoseResult.Failure("Unknown projectile spawn channel: " + request.ChannelId);

            AttackDefinitionAsset attack = _queries.FindAttack(request.Context.WaveId);
            Vector3 position = _queries.ResolveMuzzle(attack);
            Vector3 forward = Vector3.forward;
            if (_queries.TrySelectTarget(out AutoDefenseEnemySnapshot target))
            {
                forward = IdleAutoDefenseCombatTargets.CreateEnemyAimPosition(target.Position) - position;
                forward.y = 0f;
            }

            if (forward.sqrMagnitude <= 0.0001f)
                forward = Vector3.forward;
            return SpawnPoseResult.Success(new SpawnPose(position, Quaternion.LookRotation(forward.normalized, Vector3.up)));
        }
    }
}
