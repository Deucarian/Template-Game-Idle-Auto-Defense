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
    internal static class IdleAutoDefenseCombatDefaults
    {
        internal const int ManualTowerBaseCooldownTicks = 34;
        internal const int ManualTowerMinimumCooldownTicks = 18;
        internal const double ManualTowerBaseDamage = 3.2d;
        internal const double ManualTowerDamageRankBonus = 1.6d;
        internal const double ManualTowerBaseRange = 8.4d;
        internal const double ManualTowerRangeRankBonus = 0.4d;
        internal const double ManualTowerMaximumRange = 10.6d;
        internal const double PulseBeamModuleBaseRange = 7.4d;
        internal const double ArcBurstModuleBaseRange = 6.2d;
        internal const double HomingPulseModuleBaseRange = 8.8d;
        internal const double ModuleRangeRankBonus = 0.35d;
        internal const double SampleProjectileFinishThreshold = 3d;
        internal const int PulseBeamModuleCooldownTicks = 72;
        internal const int ArcBurstModuleCooldownTicks = 108;
        internal const int HomingPulseModuleCooldownTicks = 92;
        internal const int MinimumProjectileImpactDelayTicks = 12;
        internal const int MaximumProjectileImpactDelayTicks = 52;
        internal const int OverdriveCooldownBonusTicks = 10;
        internal const double OverdriveDamageMultiplier = 1.55d;
    }
}
