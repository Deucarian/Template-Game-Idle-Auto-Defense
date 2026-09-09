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
    internal sealed class IdleAutoDefenseCombatPresentationAdapter : IIdleAutoDefenseCombatPresentation
    {
        private readonly Func<AttackDefinitionAsset, Vector3> _muzzle;
        private readonly Func<AttackDefinitionAsset, Color> _color;
        private readonly Action<AttackDefinitionAsset, Vector3> _fire;
        private readonly Action<AttackDefinitionAsset, AttackPresentationEventKind, Vector3, long> _attack;
        private readonly Action<Vector3, Vector3, Color> _tracer;
        private readonly Action<Vector3, double, Color, string> _number;
        private readonly Action<AutoDefenseEnemySnapshot, EnemyPresentationEventKind> _enemy;

        internal IdleAutoDefenseCombatPresentationAdapter(Func<AttackDefinitionAsset, Vector3> muzzle,
            Func<AttackDefinitionAsset, Color> color, Action<AttackDefinitionAsset, Vector3> fire,
            Action<AttackDefinitionAsset, AttackPresentationEventKind, Vector3, long> attack,
            Action<Vector3, Vector3, Color> tracer, Action<Vector3, double, Color, string> number,
            Action<AutoDefenseEnemySnapshot, EnemyPresentationEventKind> enemy)
        {
            _muzzle = muzzle ?? throw new ArgumentNullException(nameof(muzzle));
            _color = color ?? throw new ArgumentNullException(nameof(color));
            _fire = fire ?? throw new ArgumentNullException(nameof(fire));
            _attack = attack ?? throw new ArgumentNullException(nameof(attack));
            _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
            _number = number ?? throw new ArgumentNullException(nameof(number));
            _enemy = enemy ?? throw new ArgumentNullException(nameof(enemy));
        }

        public Vector3 ResolveTowerMuzzlePosition(AttackDefinitionAsset attack) => _muzzle(attack);
        public Color ResolveAttackColor(AttackDefinitionAsset attack) => _color(attack);
        public void PlayWeaponFirePresentation(AttackDefinitionAsset attack, Vector3 target) => _fire(attack, target);
        public void EmitAttackEvent(AttackDefinitionAsset attack, AttackPresentationEventKind kind, Vector3 position, long targetId = 0) => _attack(attack, kind, position, targetId);
        public void EmitAttackTracer(Vector3 origin, Vector3 destination, Color color) => _tracer(origin, destination, color);
        public void EmitDamageNumber(Vector3 position, double amount, Color color, string prefix) => _number(position, amount, color, prefix);
        public void EmitEnemyPresentationEvent(AutoDefenseEnemySnapshot enemy, EnemyPresentationEventKind kind) => _enemy(enemy, kind);

    }
}
