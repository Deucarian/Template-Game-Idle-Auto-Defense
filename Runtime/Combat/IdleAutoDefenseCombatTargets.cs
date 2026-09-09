using System;
using System.Collections.Generic;
using Deucarian.Attacks;
using Deucarian.Attacks.Authoring;
using Deucarian.AutoDefense;
using Deucarian.Combat;
using Deucarian.Projectiles;
using Deucarian.WeaponSystems;
using Deucarian.WorldNavigation;
using Deucarian.WorldSpawning;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    internal sealed class IdleAutoDefenseCombatTargets
    {
        private readonly IIdleAutoDefenseCombatEnemies _enemies;
        private readonly IdleAutoDefenseCombatStatistics _stats;
        private readonly IdleAutoDefenseCombatContent _content;
        private readonly IdleAutoDefenseCombatRules _rules;

        internal IdleAutoDefenseCombatTargets(IIdleAutoDefenseCombatEnemies enemies, IdleAutoDefenseCombatStatistics stats,
            IdleAutoDefenseCombatContent content, IdleAutoDefenseCombatRules rules)
        {
            _enemies = enemies;
            _stats = stats;
            _content = content;
            _rules = rules;
        }

        internal bool TrySelectPriorityEnemy(AutoDefenseRuntimeSnapshot snapshot, out AutoDefenseEnemySnapshot selected)
        {
            selected = default;
            if (snapshot == null || snapshot.Enemies.Count == 0) return false;
            bool hasSelected = false;
            for (int i = 0; i < snapshot.Enemies.Count; i++)
            {
                AutoDefenseEnemySnapshot enemy = snapshot.Enemies[i];
                if (enemy.Lifecycle != AutoDefenseEnemyLifecycle.Active) continue;
                if (hasSelected && enemy.ObjectiveProgress <= selected.ObjectiveProgress) continue;
                selected = enemy;
                hasSelected = true;
            }

            return hasSelected;
        }

        internal bool TrySelectPriorityEnemyWithinRange(AutoDefenseRuntimeSnapshot snapshot, double range, out AutoDefenseEnemySnapshot selected)
        {
            selected = default;
            if (snapshot == null || snapshot.Enemies.Count == 0) return false;
            bool hasSelected = false;
            bool sawActiveEnemy = false;
            float maxRange = (float)Math.Max(0.1d, range);
            for (int i = 0; i < snapshot.Enemies.Count; i++)
            {
                AutoDefenseEnemySnapshot enemy = snapshot.Enemies[i];
                if (enemy.Lifecycle != AutoDefenseEnemyLifecycle.Active) continue;
                sawActiveEnemy = true;
                if (Vector3.Distance(enemy.Position, Vector3.zero) > maxRange) continue;
                if (hasSelected && enemy.ObjectiveProgress <= selected.ObjectiveProgress) continue;
                selected = enemy;
                hasSelected = true;
            }

            if (!hasSelected && sawActiveEnemy)
                _stats.RangeRejectedTargetCount++;
            return hasSelected;
        }

        internal bool TrySelectAttackTarget(AttackDefinitionAsset attack, AutoDefenseRuntimeSnapshot snapshot, out AutoDefenseEnemySnapshot selected)
        {
            selected = default;
            if (snapshot == null || snapshot.Enemies.Count == 0) return false;
            AttackRecipeTargetingMode mode = attack != null && attack.Targeting != null
                ? attack.Targeting.Mode
                : AttackRecipeTargetingMode.Nearest;
            bool hasSelected = false;
            for (int i = 0; i < snapshot.Enemies.Count; i++)
            {
                AutoDefenseEnemySnapshot enemy = snapshot.Enemies[i];
                if (enemy.Lifecycle != AutoDefenseEnemyLifecycle.Active) continue;
                if (!hasSelected || IsBetterTarget(enemy, selected, mode))
                {
                    selected = enemy;
                    hasSelected = true;
                }
            }

            return hasSelected;
        }

        internal static bool IsBetterTarget(AutoDefenseEnemySnapshot candidate, AutoDefenseEnemySnapshot current, AttackRecipeTargetingMode mode)
        {
            if (mode == AttackRecipeTargetingMode.LowestHealth)
                return candidate.Health < current.Health || (Math.Abs(candidate.Health - current.Health) < 0.001d && candidate.ObjectiveProgress > current.ObjectiveProgress);
            if (mode == AttackRecipeTargetingMode.Strongest)
                return candidate.Health > current.Health || (Math.Abs(candidate.Health - current.Health) < 0.001d && candidate.ObjectiveProgress > current.ObjectiveProgress);
            if (mode == AttackRecipeTargetingMode.Random)
                return candidate.Id < current.Id;
            return candidate.ObjectiveProgress > current.ObjectiveProgress;
        }

        internal bool TryFindActiveEnemy(long enemyId, out AutoDefenseEnemySnapshot enemy)
        {
            enemy = default;
            if (!_enemies.IsAvailable || enemyId <= 0) return false;
            return TryFindEnemy(_enemies.CreateSnapshot(), enemyId, out enemy) &&
                enemy.Lifecycle == AutoDefenseEnemyLifecycle.Active;
        }

        internal static bool TryFindEnemy(AutoDefenseRuntimeSnapshot snapshot, long enemyId, out AutoDefenseEnemySnapshot enemy)
        {
            enemy = default;
            if (snapshot == null || enemyId <= 0) return false;
            for (int i = 0; i < snapshot.Enemies.Count; i++)
            {
                if (snapshot.Enemies[i].Id != enemyId) continue;
                enemy = snapshot.Enemies[i];
                return true;
            }

            return false;
        }

        internal static bool TryFindEnemyByCombatant(AutoDefenseRuntimeSnapshot snapshot, CombatantId combatantId, out AutoDefenseEnemySnapshot enemy)
        {
            enemy = default;
            if (snapshot == null || combatantId.IsEmpty) return false;
            for (int i = 0; i < snapshot.Enemies.Count; i++)
            {
                if (!snapshot.Enemies[i].CombatantId.Equals(combatantId)) continue;
                enemy = snapshot.Enemies[i];
                return true;
            }

            return false;
        }

        internal static bool TrySelectPresentationEnemyWithinRange(AutoDefenseRuntimeSnapshot snapshot, double range, out AutoDefenseEnemySnapshot selected)
        {
            selected = default;
            if (snapshot == null || snapshot.Enemies.Count == 0) return false;
            bool hasSelected = false;
            float maxRange = (float)Math.Max(0.1d, range);
            for (int i = 0; i < snapshot.Enemies.Count; i++)
            {
                AutoDefenseEnemySnapshot enemy = snapshot.Enemies[i];
                if (enemy.Lifecycle != AutoDefenseEnemyLifecycle.Active) continue;
                if (Vector3.Distance(enemy.Position, Vector3.zero) > maxRange) continue;
                if (hasSelected && enemy.ObjectiveProgress <= selected.ObjectiveProgress) continue;
                selected = enemy;
                hasSelected = true;
            }

            return hasSelected;
        }

        internal bool TrySelectPresentationEnemyWithinAnyRange(out AutoDefenseEnemySnapshot selected)
        {
            selected = default;
            if (!_enemies.IsAvailable) return false;
            return TrySelectPriorityEnemy(_enemies.CreateSnapshot(), out selected);
        }

        internal static Vector3 CreateTowerMuzzlePosition(Vector3 objectivePosition)
        {
            return objectivePosition + Vector3.up * 0.75f;
        }

        internal static Vector3 CreateEnemyAimPosition(Vector3 enemyPosition)
        {
            return enemyPosition + Vector3.up * 0.35f;
        }

        internal bool TryGetPrimaryMajorThreat(out IdleAutoDefenseMajorThreatSnapshot threat)
        {
            threat = default;
            if (!_enemies.IsAvailable) return false;
            AutoDefenseRuntimeSnapshot snapshot = _enemies.CreateSnapshot();
            AutoDefenseEnemySnapshot selected = default;
            bool found = false;
            bool selectedBoss = false;
            for (int i = 0; i < snapshot.Enemies.Count; i++)
            {
                AutoDefenseEnemySnapshot candidate = snapshot.Enemies[i];
                if (candidate.Lifecycle != AutoDefenseEnemyLifecycle.Active) continue;
                bool boss = _rules.IsBossEnemy(candidate);
                if (!boss && !_rules.IsEliteEnemy(candidate)) continue;
                if (!found || boss && !selectedBoss || boss == selectedBoss && candidate.ObjectiveProgress > selected.ObjectiveProgress)
                {
                    selected = candidate;
                    selectedBoss = boss;
                    found = true;
                }
            }

            if (!found) return false;
            EnemyDefinitionAsset definition = _content.FindEnemyDefinitionForPresentation(selected.SpawnableId);
            string displayName = definition == null || string.IsNullOrWhiteSpace(definition.DisplayName)
                ? (selectedBoss ? "Boss" : "Elite")
                : definition.DisplayName;
            double maximumHealth = definition == null || definition.Stats == null
                ? Math.Max(1d, selected.Health)
                : Math.Max(1d, definition.Stats.MaximumHealth);
            threat = new IdleAutoDefenseMajorThreatSnapshot(
                selected.Id,
                displayName,
                selectedBoss,
                selected.Health,
                maximumHealth,
                selected.Position);
            return true;
        }
    }
}
