using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Deucarian.Attacks;
using Deucarian.Attacks.Authoring;
using Deucarian.AutoDefense;
using Deucarian.Combat;
using Deucarian.Projectiles;
using Deucarian.WorldNavigation;
using Deucarian.WorldSpawning;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense.Tests
{
    internal sealed class IdleCombatTestHost : IIdleAutoDefenseCombatEnemies, IIdleAutoDefenseCombatProjectiles,
        IIdleAutoDefenseCombatPresentation, IIdleAutoDefenseBuildEffects, IDisposable
    {
        internal readonly List<AutoDefenseEnemySnapshot> Enemies = new List<AutoDefenseEnemySnapshot>();
        internal readonly List<string> Events = new List<string>();
        internal readonly List<double> Numbers = new List<double>();
        internal readonly List<string> FiredAttackIds = new List<string>();
        internal readonly List<AttackDefinitionAsset> Attacks = new List<AttackDefinitionAsset>();
        internal readonly List<ProjectileDefinition> ProjectileDefinitions = new List<ProjectileDefinition>();
        internal readonly IdleAutoDefenseRewardDraftSettings Draft = IdleAutoDefenseRewardDraftSettings.CreateDefault();
        internal readonly IdleAutoDefenseRunWallet Wallet = new IdleAutoDefenseRunWallet();
        internal readonly IdleAutoDefenseRunBuild Build;
        internal readonly IdleAutoDefenseCombatRuntime Combat;
        internal Vector3 Muzzle = Vector3.up * 0.75f;
        internal bool AcceptImpacts = true;
        internal Action BeforeImpactReport;
        private readonly List<UnityEngine.Object> _assets = new List<UnityEngine.Object>();
        private long _sequence;

        internal IdleCombatTestHost(bool bindEconomy = false)
        {
            IdleAutoDefenseEconomyAsset economy = bindEconomy ? IdleAutoDefenseEconomyAsset.CreateTransient() : null;
            if (economy != null) _assets.Add(economy);
            Build = new IdleAutoDefenseRunBuild(() => null, () => economy, Wallet, this);
            Combat = new IdleAutoDefenseCombatRuntime(this, this, this, Build,
                () => Attacks.ToArray(), () => Array.Empty<EnemyDefinitionAsset>(), () => ProjectileDefinitions.ToArray(),
                () => null, () => null, () => Draft, () => "objective.test", enemy => Events.Add("reward:" + enemy.Id));
        }

        internal AutoDefenseEnemySnapshot AddEnemy(long id, double health, Vector3 position, float progress)
        {
            var enemy = new AutoDefenseEnemySnapshot(id, new WorldSpawnableId("enemy.test"),
                new CombatantId("enemy." + id.ToString(CultureInfo.InvariantCulture)), position, health,
                AutoDefenseEnemyLifecycle.Active, progress);
            Enemies.Add(enemy);
            return enemy;
        }

        internal AttackDefinitionAsset CreateAttack(bool projectile = false, AttackRecipeTargetingMode mode = AttackRecipeTargetingMode.Nearest, string id = "attack.test")
        {
            var attack = AttackDefinitionAsset.CreateTransient(id, "Test", projectile ? AttackRecipeDeliveryMode.Projectile : AttackRecipeDeliveryMode.Hitscan,
                "damage.test", 3f, 34, 8f, mode, projectileDefinitionId: "projectile.test", projectileSpawnableId: "spawnable.projectile.test",
                projectileSpeed: 4.2f, projectileLifetimeTicks: 150);
            _assets.AddRange(new UnityEngine.Object[] { attack, attack.Mechanics, attack.Targeting, attack.Delivery, attack.StatusEffects, attack.Presentation });
            Attacks.Add(attack);
            if (projectile) ProjectileDefinitions.AddRange(BasicIdleAutoDefenseGame.CreateProjectileDefinitions(new[] { attack }));
            return attack;
        }

        public bool IsAvailable => true;
        public bool IsNavigationAvailable => false;
        public bool EncounterRunning => true;
        public bool HasObjective => true;
        public AutoDefenseRuntimeSnapshot CreateSnapshot() => new AutoDefenseRuntimeSnapshot(AutoDefenseRuntimeState.Running, 100d, 10, Enemies);

        public bool TryKillEnemy(long id)
        {
            Events.Add("kill:" + id);
            return Enemies.RemoveAll(enemy => enemy.Id == id) > 0;
        }

        public ProjectileLaunchResult Launch(ProjectileLaunchRequest request)
        {
            Events.Add("launch");
            return new ProjectileLaunchResult(true, ProjectileLaunchFailureReason.None, new ProjectileInstanceId(++_sequence));
        }

        public ProjectileImpactResult ReportImpact(ProjectileImpactRequest request)
        {
            BeforeImpactReport?.Invoke();
            long id = Enemies.First(enemy => enemy.CombatantId.Equals(request.TargetId)).Id;
            Events.Add("report:" + id);
            return new ProjectileImpactResult(AcceptImpacts, AcceptImpacts ? ProjectileImpactFailureReason.None : ProjectileImpactFailureReason.Expired, false, null);
        }

        public void Cleanup(ProjectileInstanceId id, ProjectileExpiryReason reason) => Events.Add("cleanup:" + id.Value + ":" + reason);
        public MovementSnapshot CreateMovementSnapshot() => new MovementSnapshot(Array.Empty<MovementAgentSnapshot>());
        public Vector3 ResolveTowerMuzzlePosition(AttackDefinitionAsset attack) => Muzzle;
        public Color ResolveAttackColor(AttackDefinitionAsset attack) => Color.white;
        public void PlayWeaponFirePresentation(AttackDefinitionAsset attack, Vector3 target) { Events.Add("fire"); FiredAttackIds.Add(attack == null ? string.Empty : attack.Id); }
        public void EmitAttackEvent(AttackDefinitionAsset attack, AttackPresentationEventKind kind, Vector3 position, long targetId = 0) => Events.Add("attack:" + kind + ":" + targetId);
        public void EmitAttackTracer(Vector3 origin, Vector3 destination, Color color) => Events.Add("tracer");
        public void EmitDamageNumber(Vector3 position, double amount, Color color, string prefix) { Events.Add("number"); Numbers.Add(amount); }
        public void EmitEnemyPresentationEvent(AutoDefenseEnemySnapshot enemy, EnemyPresentationEventKind kind) => Events.Add("enemy:" + kind + ":" + enemy.Id);
        public void ChangeObjectiveMaximum(double amount, MaximumChangePolicy policy) { }
        public void HealObjective(double amount) { }
        public void CreateWeapon(string weaponId, string attackId, bool enabled) { }
        public void Feedback(string text, Color color, float scale) { }

        public void Dispose()
        {
            Combat.Release();
            foreach (UnityEngine.Object asset in _assets) if (asset != null) UnityEngine.Object.DestroyImmediate(asset);
            _assets.Clear();
        }
    }
}
