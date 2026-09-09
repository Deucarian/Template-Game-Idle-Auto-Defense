using System;
using Deucarian.Attacks.Authoring;
using Deucarian.AutoDefense;
using Deucarian.Projectiles;
using Deucarian.WorldSpawning;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    internal delegate bool IdleAutoDefenseFindPresentationEnemy(long id, out AutoDefenseEnemySnapshot enemy);
    internal delegate bool IdleAutoDefenseSelectPresentationEnemy(AutoDefenseRuntimeSnapshot snapshot, double range, out AutoDefenseEnemySnapshot enemy);

    // Read ports keep the presentation owners independent from the run controller and damage commands.
    internal sealed class IdleAutoDefensePresentationQueries
    {
        internal readonly Func<WorldSpawnableId, EnemyDefinitionAsset> FindEnemy;
        internal readonly Func<ProjectileDefinition, AttackDefinitionAsset> FindProjectileAttack;
        internal readonly Func<string, AttackDefinitionAsset> FindAttack;
        internal readonly Func<AttackDefinitionAsset, double> ResolveRange;
        internal readonly IdleAutoDefenseSelectPresentationEnemy TrySelectEnemy;
        internal readonly IdleAutoDefenseFindPresentationEnemy TryFindEnemy;
        internal readonly Func<bool> ShowDebugSpawnRing;

        internal IdleAutoDefensePresentationQueries(
            Func<WorldSpawnableId, EnemyDefinitionAsset> findEnemy,
            Func<ProjectileDefinition, AttackDefinitionAsset> findProjectileAttack,
            Func<string, AttackDefinitionAsset> findAttack,
            Func<AttackDefinitionAsset, double> resolveRange,
            IdleAutoDefenseSelectPresentationEnemy trySelectEnemy,
            IdleAutoDefenseFindPresentationEnemy tryFindEnemy,
            Func<bool> showDebugSpawnRing)
        {
            FindEnemy = findEnemy;
            FindProjectileAttack = findProjectileAttack;
            FindAttack = findAttack;
            ResolveRange = resolveRange;
            TrySelectEnemy = trySelectEnemy;
            TryFindEnemy = tryFindEnemy;
            ShowDebugSpawnRing = showDebugSpawnRing;
        }
    }
}
