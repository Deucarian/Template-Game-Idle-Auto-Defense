using System;
using System.Collections.Generic;
using Deucarian.Attacks.Authoring;
using Deucarian.Common;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    /// <summary>Owns active beam instances, target-follow updates and terminal cleanup.</summary>
    internal sealed class IdleAutoDefenseBeamVisuals
    {
        private readonly Func<long, Vector3?> _resolveTarget;
        private readonly Func<AttackDefinitionAsset, Vector3> _resolveMuzzle;
        private readonly Func<GameObject, GameObject, Vector3, Vector3, bool> _align;
        private readonly List<Beam> _beams = new List<Beam>();

        internal IdleAutoDefenseBeamVisuals(
            Func<long, Vector3?> resolveTarget,
            Func<AttackDefinitionAsset, Vector3> resolveMuzzle,
            Func<GameObject, GameObject, Vector3, Vector3, bool> align)
        {
            _resolveTarget = resolveTarget ?? throw new ArgumentNullException(nameof(resolveTarget));
            _resolveMuzzle = resolveMuzzle ?? throw new ArgumentNullException(nameof(resolveMuzzle));
            _align = align ?? throw new ArgumentNullException(nameof(align));
        }

        internal void Add(GameObject instance, GameObject prefab, AttackDefinitionAsset attack,
            long targetEnemyId, Vector3 impactPosition, float durationSeconds)
        {
            _beams.Add(new Beam
            {
                Instance = instance,
                Prefab = prefab,
                Attack = attack,
                TargetEnemyId = targetEnemyId,
                LastImpactPosition = impactPosition,
                DurationSeconds = Mathf.Max(0.05f, durationSeconds)
            });
        }

        /// <returns>The number of invalid endpoints for the run's presentation diagnostics.</returns>
        internal int Update(float deltaSeconds)
        {
            int invalidEndpoints = 0;
            float safeDelta = Mathf.Max(0.016f, deltaSeconds);
            for (int i = _beams.Count - 1; i >= 0; i--)
            {
                Beam visual = _beams[i];
                visual.ElapsedSeconds += safeDelta;
                if (visual.Instance == null || visual.ElapsedSeconds >= visual.DurationSeconds)
                {
                    RemoveAt(i, visual.Instance);
                    continue;
                }

                Vector3 impactPosition = visual.LastImpactPosition;
                Vector3? target = visual.TargetEnemyId > 0 ? _resolveTarget(visual.TargetEnemyId) : null;
                if (target.HasValue) impactPosition = target.Value;
                if (!_align(visual.Instance, visual.Prefab, _resolveMuzzle(visual.Attack), impactPosition))
                {
                    invalidEndpoints++;
                    RemoveAt(i, visual.Instance);
                    continue;
                }

                visual.LastImpactPosition = impactPosition;
                _beams[i] = visual;
            }
            return invalidEndpoints;
        }

        internal void Clear()
        {
            for (int i = _beams.Count - 1; i >= 0; i--)
                UnityObjectUtility.DestroySafely(_beams[i].Instance);
            _beams.Clear();
        }

        private void RemoveAt(int index, GameObject instance)
        {
            UnityObjectUtility.DestroySafely(instance);
            _beams.RemoveAt(index);
        }

        private struct Beam
        {
            internal GameObject Instance;
            internal GameObject Prefab;
            internal AttackDefinitionAsset Attack;
            internal long TargetEnemyId;
            internal Vector3 LastImpactPosition;
            internal float DurationSeconds;
            internal float ElapsedSeconds;
        }
    }
}
