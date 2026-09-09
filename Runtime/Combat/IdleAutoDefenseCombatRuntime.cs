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
    internal sealed class IdleAutoDefenseCombatRuntime
    {
        internal readonly IdleAutoDefenseCombatStatistics Statistics;
        internal readonly IdleAutoDefenseCombatContent Content;
        internal readonly IdleAutoDefenseCombatRules Rules;
        internal readonly IdleAutoDefenseCombatTargets Targets;
        internal readonly IdleAutoDefenseCombatFeedback Feedback;
        internal readonly IdleAutoDefenseVisibleDamage Damage;
        internal readonly IdleAutoDefenseCombatProjectiles Projectiles;
        internal readonly IdleAutoDefenseCombatCadence Cadence;
        internal readonly IIdleAutoDefenseProjectileMuzzleQueries MuzzleQueries;

        internal IdleAutoDefenseCombatRuntime(IIdleAutoDefenseCombatEnemies enemies, IIdleAutoDefenseCombatProjectiles projectiles,
            IIdleAutoDefenseCombatPresentation presentation, IdleAutoDefenseRunBuild build,
            Func<AttackDefinitionAsset[]> attacks, Func<EnemyDefinitionAsset[]> enemyDefinitions,
            Func<ProjectileDefinition[]> projectileDefinitions, Func<IdleAutoDefenseGameRulesAsset> gameRules,
            Func<IdleAutoDefenseRunProfileAsset> runProfile, Func<IdleAutoDefenseRewardDraftSettings> draftSettings,
            Func<string> objectiveId, Action<AutoDefenseEnemySnapshot> reward)
        {
            if (enemies == null) throw new ArgumentNullException(nameof(enemies));
            if (projectiles == null) throw new ArgumentNullException(nameof(projectiles));
            if (presentation == null) throw new ArgumentNullException(nameof(presentation));
            if (reward == null) throw new ArgumentNullException(nameof(reward));
            Statistics = new IdleAutoDefenseCombatStatistics();
            Content = new IdleAutoDefenseCombatContent(attacks, enemyDefinitions, projectileDefinitions);
            Rules = new IdleAutoDefenseCombatRules(build, gameRules, runProfile, draftSettings);
            Targets = new IdleAutoDefenseCombatTargets(enemies, Statistics, Content, Rules);
            Feedback = new IdleAutoDefenseCombatFeedback(enemies, presentation, Statistics, Content, Rules, reward);
            Damage = new IdleAutoDefenseVisibleDamage(enemies, presentation, Statistics, Targets, Feedback, reward);
            Projectiles = new IdleAutoDefenseCombatProjectiles(enemies, projectiles, presentation, Statistics,
                Targets, Content, Rules, Damage, Feedback, objectiveId);
            Cadence = new IdleAutoDefenseCombatCadence(enemies, Statistics, build, Content, Rules, Targets, Damage, Projectiles);
            MuzzleQueries = new IdleAutoDefenseProjectileMuzzleQueries(Content.FindAttackRecipeForPresentation,
                presentation.ResolveTowerMuzzlePosition, Targets.TrySelectPresentationEnemyWithinAnyRange);
        }

        internal void Reset()
        {
            Statistics.Reset();
            Cadence.Reset();
            Release();
        }

        // Release only transient tracking; diagnostic totals remain observable,
        // as they were after disposal of the original controller's scene objects.
        internal void Release()
        {
            Projectiles.Clear();
            Damage.Clear();
            Feedback.Clear();
        }

    }
}
