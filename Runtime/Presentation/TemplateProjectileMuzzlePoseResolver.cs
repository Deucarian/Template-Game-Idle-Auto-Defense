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
        private readonly IdleAutoDefenseTemplateController _controller;
        private readonly WorldSpawnChannelId _channelId;

        public TemplateProjectileMuzzlePoseResolver(IdleAutoDefenseTemplateController controller, WorldSpawnChannelId channelId)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _channelId = channelId;
        }

        public SpawnPoseResult TryResolvePose(WorldSpawnRequest request)
        {
            if (!request.ChannelId.Equals(_channelId))
                return SpawnPoseResult.Failure("Unknown projectile spawn channel: " + request.ChannelId);

            AttackDefinitionAsset attack = _controller.FindAttackRecipeForPresentation(request.Context.WaveId);
            Vector3 position = _controller.ResolveTowerMuzzlePosition(attack);
            Vector3 forward = Vector3.forward;
            if (_controller.TrySelectPresentationEnemyWithinAnyRange(out AutoDefenseEnemySnapshot target))
            {
                forward = IdleAutoDefenseTemplateController.CreateEnemyAimPosition(target.Position) - position;
                forward.y = 0f;
            }

            if (forward.sqrMagnitude <= 0.0001f)
                forward = Vector3.forward;
            return SpawnPoseResult.Success(new SpawnPose(position, Quaternion.LookRotation(forward.normalized, Vector3.up)));
        }
    }
}
