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
    internal delegate bool IdleAutoDefenseSelectMuzzleTarget(out AutoDefenseEnemySnapshot target);

    internal interface IIdleAutoDefenseProjectileMuzzleQueries
    {
        AttackDefinitionAsset FindAttack(string id);
        Vector3 ResolveMuzzle(AttackDefinitionAsset attack);
        bool TrySelectTarget(out AutoDefenseEnemySnapshot target);
    }

    internal sealed class IdleAutoDefenseProjectileMuzzleQueries : IIdleAutoDefenseProjectileMuzzleQueries
    {
        private readonly Func<string, AttackDefinitionAsset> _attack;
        private readonly Func<AttackDefinitionAsset, Vector3> _muzzle;
        private readonly IdleAutoDefenseSelectMuzzleTarget _target;

        internal IdleAutoDefenseProjectileMuzzleQueries(Func<string, AttackDefinitionAsset> attack,
            Func<AttackDefinitionAsset, Vector3> muzzle, IdleAutoDefenseSelectMuzzleTarget target)
        {
            _attack = attack ?? throw new ArgumentNullException(nameof(attack));
            _muzzle = muzzle ?? throw new ArgumentNullException(nameof(muzzle));
            _target = target ?? throw new ArgumentNullException(nameof(target));
        }

        public AttackDefinitionAsset FindAttack(string id) => _attack(id);
        public Vector3 ResolveMuzzle(AttackDefinitionAsset attack) => _muzzle(attack);
        public bool TrySelectTarget(out AutoDefenseEnemySnapshot target) => _target(out target);
    }
}
