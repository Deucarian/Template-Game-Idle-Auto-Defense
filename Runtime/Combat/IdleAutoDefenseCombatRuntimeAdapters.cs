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
    internal sealed class IdleAutoDefenseCombatRuntimeAdapters : IIdleAutoDefenseCombatEnemies, IIdleAutoDefenseCombatProjectiles
    {
        private readonly Func<AutoDefenseRuntime> _enemies;
        private readonly Func<ProjectileRuntime> _projectiles;
        private readonly Func<WorldNavigationService> _navigation;

        internal IdleAutoDefenseCombatRuntimeAdapters(Func<AutoDefenseRuntime> enemies,
            Func<ProjectileRuntime> projectiles, Func<WorldNavigationService> navigation)
        {
            _enemies = enemies ?? throw new ArgumentNullException(nameof(enemies));
            _projectiles = projectiles ?? throw new ArgumentNullException(nameof(projectiles));
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        }

        bool IIdleAutoDefenseCombatEnemies.IsAvailable => _enemies() != null;
        bool IIdleAutoDefenseCombatProjectiles.IsAvailable => _projectiles() != null;
        public bool IsNavigationAvailable => _navigation() != null;
        public AutoDefenseRuntimeSnapshot CreateSnapshot() => _enemies()?.CreateSnapshot();
        public bool TryKillEnemy(long id) => _enemies() != null && _enemies().TryKillEnemy(id);
        public ProjectileLaunchResult Launch(ProjectileLaunchRequest request) => _projectiles().Launch(request);
        public ProjectileImpactResult ReportImpact(ProjectileImpactRequest request) => _projectiles().ReportImpact(request);
        public void Cleanup(ProjectileInstanceId id, ProjectileExpiryReason reason) => _projectiles().Cleanup(id, reason);
        public MovementSnapshot CreateMovementSnapshot() => _navigation()?.CreateSnapshot();

    }
}
