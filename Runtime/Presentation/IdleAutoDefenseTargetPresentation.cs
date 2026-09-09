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
    internal sealed class IdleAutoDefenseTargetPresentation
    {
        private readonly IdleAutoDefenseContentBinding _content;

        private readonly IdleAutoDefensePresentationCounters _counters;

        private readonly IdleAutoDefensePresentationQueries _queries;

        internal IdleAutoDefenseTargetPresentation(IdleAutoDefenseContentBinding content, IdleAutoDefensePresentationCounters counters, IdleAutoDefensePresentationQueries queries)
        {
            _content = content;
            _counters = counters;
            _queries = queries;
        }

        internal readonly Dictionary<string, IdleAutoDefenseWeaponVisualBinding> WeaponBindings = new Dictionary<string, IdleAutoDefenseWeaponVisualBinding>(StringComparer.OrdinalIgnoreCase);
        internal readonly Dictionary<long, IdleAutoDefenseEnemyModelPresentation> EnemyBindings = new Dictionary<long, IdleAutoDefenseEnemyModelPresentation>();

        internal void UpdateEnemyModelPresentations(AutoDefenseRuntimeSnapshot snapshot, float deltaSeconds)
        {
            if (snapshot == null) return;
            for (int i = 0; i < snapshot.Enemies.Count; i++)
            {
                AutoDefenseEnemySnapshot enemy = snapshot.Enemies[i];
                if (enemy.Lifecycle != AutoDefenseEnemyLifecycle.Active) continue;
                IdleAutoDefenseEnemyModelPresentation presentation = BindEnemyModelPresentation(enemy);
                if (presentation == null) continue;
                if (presentation.FaceMovement(enemy.Position, deltaSeconds))
                    _counters.EnemyFacingUpdateCount++;
            }
        }

        internal IdleAutoDefenseEnemyModelPresentation BindEnemyModelPresentation(AutoDefenseEnemySnapshot enemy)
        {
            if (enemy.Id <= 0) return null;
            if (EnemyBindings.TryGetValue(enemy.Id, out IdleAutoDefenseEnemyModelPresentation cached) && cached != null)
                return cached;

            IdleAutoDefenseEnemyModelPresentation[] presentations = UnityEngine.Object.FindObjectsByType<IdleAutoDefenseEnemyModelPresentation>(FindObjectsSortMode.None);
            IdleAutoDefenseEnemyModelPresentation best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < presentations.Length; i++)
            {
                IdleAutoDefenseEnemyModelPresentation candidate = presentations[i];
                if (candidate == null || candidate.IsBound) continue;
                float distance = Vector3.Distance(candidate.transform.position, enemy.Position);
                if (distance >= bestDistance) continue;
                best = candidate;
                bestDistance = distance;
            }

            if (best == null) return null;
            best.Bind(enemy.Id, enemy.Position, IdleAutoDefenseVisualAssets.ResolveEnemyFallbackColor(enemy.SpawnableId.Value));
            EnemyBindings[enemy.Id] = best;
            return best;
        }

        internal Vector3 ResolveTowerMuzzlePosition(AttackDefinitionAsset attack)
        {
            IdleAutoDefenseWeaponVisualBinding binding = FindWeaponVisualBinding(attack);
            if (binding != null && binding.Muzzle != null)
                return binding.Muzzle.position;
            return IdleAutoDefensePresentationGeometry.TowerMuzzlePosition(Vector3.zero);
        }

        internal IdleAutoDefenseWeaponVisualBinding FindWeaponVisualBinding(AttackDefinitionAsset attack)
        {
            if (attack == null || string.IsNullOrWhiteSpace(attack.Id)) return null;
            return WeaponBindings.TryGetValue(attack.Id, out IdleAutoDefenseWeaponVisualBinding binding) ? binding : null;
        }

        internal string ResolveWeaponIdForAttack(AttackDefinitionAsset attack)
        {
            if (attack == null || string.IsNullOrWhiteSpace(attack.Id) || _content.Weapons == null)
                return string.Empty;

            for (int i = 0; i < _content.Weapons.Length; i++)
            {
                WeaponDefinitionAsset weapon = _content.Weapons[i];
                AttackDefinitionAsset weaponAttack = weapon != null && weapon.Stats != null ? weapon.Stats.Attack : null;
                if (weaponAttack != null && string.Equals(weaponAttack.Id, attack.Id, StringComparison.OrdinalIgnoreCase))
                    return weapon.Id;
            }

            return string.Empty;
        }

        internal void PlayWeaponFirePresentation(AttackDefinitionAsset attack, Vector3 targetPosition)
        {
            IdleAutoDefenseWeaponVisualBinding binding = FindWeaponVisualBinding(attack);
            if (binding == null) return;
            if (binding.AimAt(targetPosition, true, Time.deltaTime <= 0f ? 1f / 60f : Time.deltaTime))
                _counters.TurretAimUpdateCount++;
            if (binding.TriggerRecoil())
                _counters.RecoilEventCount++;
        }

        internal void UpdateWeaponPresentationTargets(AutoDefenseRuntimeSnapshot snapshot, float deltaSeconds)
        {
            if (snapshot == null || WeaponBindings.Count == 0) return;
            foreach (KeyValuePair<string, IdleAutoDefenseWeaponVisualBinding> pair in WeaponBindings)
            {
                AttackDefinitionAsset attack = _queries.FindAttack(pair.Key);
                if (attack == null) continue;
                double range = _queries.ResolveRange(attack);
                if (_queries.TrySelectEnemy(snapshot, range, out AutoDefenseEnemySnapshot enemy))
                {
                    if (pair.Value.AimAt(IdleAutoDefensePresentationGeometry.EnemyAimPosition(enemy.Position), false, deltaSeconds))
                        _counters.TurretAimUpdateCount++;
                }
                else if (pair.Value.Rest(deltaSeconds))
                {
                    _counters.TurretAimUpdateCount++;
                }
            }
        }

        internal Vector3 ResolveAttackEventPosition(AttackDefinitionAsset attack, AttackPresentationEventRecipe recipe, Vector3 requestedPosition)
        {
            if (recipe == null) return requestedPosition;
            if (recipe.SpawnPointRole == AttackPresentationSpawnPointRole.Caster ||
                recipe.SpawnPointRole == AttackPresentationSpawnPointRole.Muzzle)
            {
                return ResolveTowerMuzzlePosition(attack);
            }

            return requestedPosition;
        }

        internal Color ResolveAttackColor(AttackDefinitionAsset attack)
        {
            if (attack == null) return Color.white;
            IdleAutoDefenseModuleRule module = _content.GameRules == null ? null : _content.GameRules.GetModuleByAttackId(attack.Id);
            if (module != null)
            {
                if (module.Role == IdleAutoDefenseModuleRole.StartingProjectile) return new Color(1f, 0.45f, 0.1f);
                if (module.Role == IdleAutoDefenseModuleRole.PrecisionBeam) return new Color(0.15f, 0.8f, 1f);
                if (module.Role == IdleAutoDefenseModuleRole.AreaBurst) return new Color(1f, 0.65f, 0.12f);
                if (module.Role == IdleAutoDefenseModuleRole.HomingProjectile) return new Color(0.68f, 0.38f, 1f);
            }
            if (_content.GameRules == null)
            {
                if (string.Equals(attack.Id, BasicIdleAutoDefenseGame.ShardAttackId.Value, StringComparison.OrdinalIgnoreCase)) return new Color(1f, 0.45f, 0.1f);
                if (string.Equals(attack.Id, BasicIdleAutoDefenseGame.PulseAttackId.Value, StringComparison.OrdinalIgnoreCase)) return new Color(0.15f, 0.8f, 1f);
                if (string.Equals(attack.Id, BasicIdleAutoDefenseGame.ArcBurstAttackId.Value, StringComparison.OrdinalIgnoreCase)) return new Color(1f, 0.65f, 0.12f);
                if (string.Equals(attack.Id, BasicIdleAutoDefenseGame.HomingPulseAttackId.Value, StringComparison.OrdinalIgnoreCase)) return new Color(0.68f, 0.38f, 1f);
            }
            return Color.white;
        }
    }
}
