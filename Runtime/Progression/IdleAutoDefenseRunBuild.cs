using System;
using Deucarian.Combat;
using Deucarian.RunUpgrades;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    /// <summary>Owns run upgrade ranks, module unlocks, effect intents and Overdrive.</summary>
    internal sealed class IdleAutoDefenseRunBuild
    {
        private readonly Func<IdleAutoDefenseGameRulesAsset> _rules;
        private readonly Func<IdleAutoDefenseEconomyAsset> _economy;
        private readonly IdleAutoDefenseRunWallet _wallet;
        private readonly IIdleAutoDefenseBuildEffects _effects;
        private IdleAutoDefenseGameRulesAsset _activeGameRules => _rules();
        private IdleAutoDefenseEconomyAsset _activeEconomy => _economy();

        internal IdleAutoDefenseRunBuild(Func<IdleAutoDefenseGameRulesAsset> rules, Func<IdleAutoDefenseEconomyAsset> economy,
            IdleAutoDefenseRunWallet wallet, IIdleAutoDefenseBuildEffects effects)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _effects = effects ?? throw new ArgumentNullException(nameof(effects));
        }

        private bool CanSpendRuntimeCurrency(int cost) => _wallet.CanSpend(cost, _effects.EncounterRunning);
        private bool SpendRuntimeCurrency(int cost) => _wallet.TrySpend(cost, _effects.EncounterRunning);
        internal void RecordSelection() => SelectedUpgradeCount++;
        internal void SetInitialRewardMultiplier(double value) => RewardCreditMultiplierBonus = value;
        internal void ClearOverdrive()
        {
            _overdriveSecondsRemaining = 0f;
            _overdriveCooldownSecondsRemaining = 0f;
        }
        internal int ShardVolleyBonus => _shardVolleyBonus;
        internal int PulseBeamBonus => _pulseBeamBonus;
        internal int ArcBurstBonus => _arcBurstBonus;
        internal int HomingPulseBonus => _homingPulseBonus;
        internal double RewardDamageMultiplierBonus => _rewardDamageMultiplierBonus;

        internal void Reset()
        {
            DirectDamageBonus = 0d;
            ProjectileSpeedMultiplier = 1d;
            EnemySpawnDelayTicks = 0;
            RewardCreditMultiplierBonus = 0d;
            OfflineRewardMultiplierBonus = 0d;
            DamageUpgradeRank = 0;
            AttackSpeedUpgradeRank = 0;
            RangeUpgradeRank = 0;
            RepairUpgradeRank = 0;
            UnsupportedUpgradeIntentCount = 0;
            PulseBeamUnlocked = false;
            ArcBurstUnlocked = false;
            HomingPulseUnlocked = false;
            OverdriveActivationCount = 0;
            SelectedUpgradeCount = 0;
            _shardVolleyBonus = 0;
            _pulseBeamBonus = 0;
            _arcBurstBonus = 0;
            _homingPulseBonus = 0;
            _rewardDamageMultiplierBonus = 0d;
            _overdriveSecondsRemaining = 0f;
            _overdriveCooldownSecondsRemaining = 0f;
        }

        private const int PulseBeamModuleUnlockCost = 34;

        private const int ArcBurstModuleUnlockCost = 62;

        private const int HomingPulseModuleUnlockCost = 78;

        private const int OverdriveCostCredits = 22;

        private const float OverdriveDurationSeconds = 7f;

        private const float OverdriveCooldownSeconds = 18f;

        internal double DirectDamageBonus { get; private set; }

        internal double ProjectileSpeedMultiplier { get; private set; } = 1d;

        internal int EnemySpawnDelayTicks { get; private set; }

        internal double RewardCreditMultiplierBonus { get; private set; }

        internal double OfflineRewardMultiplierBonus { get; private set; }

        internal int DamageUpgradeRank { get; private set; }

        internal int AttackSpeedUpgradeRank { get; private set; }

        internal int RangeUpgradeRank { get; private set; }

        internal int RepairUpgradeRank { get; private set; }

        internal int UnsupportedUpgradeIntentCount { get; private set; }

        internal bool PulseBeamUnlocked { get; private set; }

        internal bool ArcBurstUnlocked { get; private set; }

        internal bool HomingPulseUnlocked { get; private set; }

        internal int OverdriveActivationCount { get; private set; }

        internal int SelectedUpgradeCount { get; private set; }

        internal int DamageUpgradeCost => ResolveUpgradeCost(_activeEconomy == null ? IdleAutoDefenseEconomyAsset.DamageUpgradeCostId : _activeEconomy.DamageUpgradeCostCurveId, DamageUpgradeRank);

        internal int AttackSpeedUpgradeCost => ResolveUpgradeCost(_activeEconomy == null ? IdleAutoDefenseEconomyAsset.FireRateUpgradeCostId : _activeEconomy.FireRateUpgradeCostCurveId, AttackSpeedUpgradeRank);

        internal int RangeUpgradeCost => ResolveUpgradeCost(_activeEconomy == null ? IdleAutoDefenseEconomyAsset.RangeUpgradeCostId : _activeEconomy.RangeUpgradeCostCurveId, RangeUpgradeRank);

        internal int RepairUpgradeCost => ResolveUpgradeCost(_activeEconomy == null ? IdleAutoDefenseEconomyAsset.RepairUpgradeCostId : _activeEconomy.RepairUpgradeCostCurveId, RepairUpgradeRank);

        internal int PulseBeamUnlockCost => ResolveModuleBuildCost(IdleAutoDefenseModuleRole.PrecisionBeam, PulseBeamModuleUnlockCost);

        internal int ArcBurstUnlockCost => ResolveModuleBuildCost(IdleAutoDefenseModuleRole.AreaBurst, ArcBurstModuleUnlockCost);

        internal int HomingPulseUnlockCost => ResolveModuleBuildCost(IdleAutoDefenseModuleRole.HomingProjectile, HomingPulseModuleUnlockCost);

        internal int OverdriveCost => ResolveUpgradeCost(_activeEconomy == null ? IdleAutoDefenseEconomyAsset.OverdriveCostId : _activeEconomy.OverdriveCostCurveId, 0, OverdriveCostCredits);

        internal bool OverdriveActive => _overdriveSecondsRemaining > 0f;

        internal float OverdriveSecondsRemaining => Mathf.Max(0f, _overdriveSecondsRemaining);

        internal float OverdriveCooldownSecondsRemaining => Mathf.Max(0f, _overdriveCooldownSecondsRemaining);

        internal bool CanPurchasePulseBeamModule => !PulseBeamUnlocked && CanSpendRuntimeCurrency(PulseBeamUnlockCost);

        internal bool CanPurchaseArcBurstModule => !ArcBurstUnlocked && CanSpendRuntimeCurrency(ArcBurstUnlockCost);

        internal bool CanPurchaseHomingPulseModule => !HomingPulseUnlocked && CanSpendRuntimeCurrency(HomingPulseUnlockCost);

        internal bool CanPurchaseOverdrive => !OverdriveActive && OverdriveCooldownSecondsRemaining <= 0f && CanSpendRuntimeCurrency(OverdriveCost);

        internal int UnlockedModuleCount => 1 + (PulseBeamUnlocked ? 1 : 0) + (ArcBurstUnlocked ? 1 : 0) + (HomingPulseUnlocked ? 1 : 0);

        internal bool CanPurchaseDamageUpgrade => CanSpendRuntimeCurrency(DamageUpgradeCost);

        internal bool CanPurchaseAttackSpeedUpgrade => CanSpendRuntimeCurrency(AttackSpeedUpgradeCost);

        internal bool CanPurchaseRangeUpgrade => CanSpendRuntimeCurrency(RangeUpgradeCost);

        internal bool CanPurchaseRepairUpgrade => CanSpendRuntimeCurrency(RepairUpgradeCost);

        private int _shardVolleyBonus;

        private int _pulseBeamBonus;

        private int _arcBurstBonus;

        private int _homingPulseBonus;

        private double _rewardDamageMultiplierBonus;

        private float _overdriveSecondsRemaining;

        private float _overdriveCooldownSecondsRemaining;

        internal bool TryPurchaseDamageUpgrade()
        {
            if (!SpendRuntimeCurrency(DamageUpgradeCost)) return false;
            DamageUpgradeRank++;
            SelectedUpgradeCount++;
            _effects.Feedback("Damage Up", new Color(1f, 0.55f, 0.18f), 0.9f);
            return true;
        }

        internal bool TryPurchaseAttackSpeedUpgrade()
        {
            if (!SpendRuntimeCurrency(AttackSpeedUpgradeCost)) return false;
            AttackSpeedUpgradeRank++;
            SelectedUpgradeCount++;
            _effects.Feedback("Fire Rate Up", new Color(0.35f, 0.9f, 1f), 0.9f);
            return true;
        }

        internal bool TryPurchaseRangeUpgrade()
        {
            if (!SpendRuntimeCurrency(RangeUpgradeCost)) return false;
            RangeUpgradeRank++;
            DirectDamageBonus += _activeGameRules == null ? 0.5d : _activeGameRules.PurchaseRangeDamageBonus;
            SelectedUpgradeCount++;
            _effects.Feedback("Range Up", new Color(0.45f, 1f, 0.6f), 0.9f);
            return true;
        }

        internal bool TryPurchaseRepairUpgrade()
        {
            if (!SpendRuntimeCurrency(RepairUpgradeCost)) return false;
            RepairUpgradeRank++;
            if (_effects.HasObjective)
            {
                double maximumHealth = _activeGameRules == null ? 8d : _activeGameRules.PurchaseRepairMaximumHealth;
                double baseHeal = _activeGameRules == null ? 34d : _activeGameRules.PurchaseRepairBaseHeal;
                double healPerRank = _activeGameRules == null ? 6d : _activeGameRules.PurchaseRepairHealPerRank;
                _effects.ChangeObjectiveMaximum(maximumHealth, MaximumChangePolicy.PreserveAbsolute);
                _effects.HealObjective(baseHeal + RepairUpgradeRank * healPerRank);
            }

            SelectedUpgradeCount++;
            _effects.Feedback("Repair", new Color(0.35f, 1f, 0.55f), 1.0f);
            return true;
        }

        internal bool TryPurchasePulseBeamModule()
        {
            if (!SpendRuntimeCurrency(PulseBeamUnlockCost)) return false;
            UnlockPulseBeamModule();
            SelectedUpgradeCount++;
            _effects.Feedback("Pulse Beam Online", new Color(0.15f, 0.8f, 1f), 1.1f);
            return true;
        }

        internal bool TryPurchaseArcBurstModule()
        {
            if (!SpendRuntimeCurrency(ArcBurstUnlockCost)) return false;
            UnlockArcBurstModule();
            SelectedUpgradeCount++;
            _effects.Feedback("Arc Burst Online", new Color(1f, 0.65f, 0.12f), 1.1f);
            return true;
        }

        internal bool TryPurchaseHomingPulseModule()
        {
            if (!SpendRuntimeCurrency(HomingPulseUnlockCost)) return false;
            UnlockHomingPulseModule();
            SelectedUpgradeCount++;
            _effects.Feedback("Homing Online", new Color(0.68f, 0.38f, 1f), 1.1f);
            return true;
        }

        internal bool TryPurchaseOverdrive()
        {
            if (!CanPurchaseOverdrive || !SpendRuntimeCurrency(OverdriveCost)) return false;
            float duration = _activeGameRules == null ? OverdriveDurationSeconds : _activeGameRules.OverdriveDurationSeconds;
            float cooldown = _activeGameRules == null ? OverdriveCooldownSeconds : _activeGameRules.OverdriveCooldownSeconds;
            _overdriveSecondsRemaining = duration;
            _overdriveCooldownSecondsRemaining = duration + cooldown;
            OverdriveActivationCount++;
            SelectedUpgradeCount++;
            _effects.Feedback("OVERDRIVE", new Color(1f, 0.82f, 0.18f), 1.28f);
            return true;
        }

        internal bool UnlockPulseBeamModule()
        {
            if (PulseBeamUnlocked) return false;
            PulseBeamUnlocked = true;
            IdleAutoDefenseModuleRule module = ResolveModuleRule(IdleAutoDefenseModuleRole.PrecisionBeam);
            _effects.CreateWeapon(
                module == null ? BasicIdleAutoDefenseGame.PulseCannonWeaponId.Value : module.WeaponId,
                module == null ? BasicIdleAutoDefenseGame.PulseAttackId.Value : module.AttackId,
                true);
            return true;
        }

        internal bool UnlockArcBurstModule()
        {
            if (ArcBurstUnlocked) return false;
            ArcBurstUnlocked = true;
            IdleAutoDefenseModuleRule module = ResolveModuleRule(IdleAutoDefenseModuleRole.AreaBurst);
            _effects.CreateWeapon(
                module == null ? BasicIdleAutoDefenseGame.ArcBurstTowerWeaponId.Value : module.WeaponId,
                module == null ? BasicIdleAutoDefenseGame.ArcBurstAttackId.Value : module.AttackId,
                true);
            return true;
        }

        internal bool UnlockHomingPulseModule()
        {
            if (HomingPulseUnlocked) return false;
            HomingPulseUnlocked = true;
            IdleAutoDefenseModuleRule module = ResolveModuleRule(IdleAutoDefenseModuleRole.HomingProjectile);
            _effects.CreateWeapon(
                module == null ? BasicIdleAutoDefenseGame.HomingSpireWeaponId.Value : module.WeaponId,
                module == null ? BasicIdleAutoDefenseGame.HomingPulseAttackId.Value : module.AttackId,
                true);
            return true;
        }

        internal bool TryUnlockWeapon(string weaponId)
        {
            IdleAutoDefenseModuleRule module = _activeGameRules == null ? null : _activeGameRules.GetModuleByWeaponId(weaponId);
            if (module == null)
            {
                if (string.Equals(weaponId, BasicIdleAutoDefenseGame.PulseCannonWeaponId.Value, StringComparison.OrdinalIgnoreCase)) return UnlockPulseBeamModule();
                if (string.Equals(weaponId, BasicIdleAutoDefenseGame.ArcBurstTowerWeaponId.Value, StringComparison.OrdinalIgnoreCase)) return UnlockArcBurstModule();
                if (string.Equals(weaponId, BasicIdleAutoDefenseGame.HomingSpireWeaponId.Value, StringComparison.OrdinalIgnoreCase)) return UnlockHomingPulseModule();
                return string.Equals(weaponId, BasicIdleAutoDefenseGame.ShardLauncherWeaponId.Value, StringComparison.OrdinalIgnoreCase);
            }

            if (module.Role == IdleAutoDefenseModuleRole.PrecisionBeam) return UnlockPulseBeamModule();
            if (module.Role == IdleAutoDefenseModuleRole.AreaBurst) return UnlockArcBurstModule();
            if (module.Role == IdleAutoDefenseModuleRole.HomingProjectile) return UnlockHomingPulseModule();
            return module.StartsUnlocked;
        }

        internal bool IsWeaponUnlocked(string weaponId)
        {
            IdleAutoDefenseModuleRule module = _activeGameRules == null ? null : _activeGameRules.GetModuleByWeaponId(weaponId);
            if (module == null)
            {
                if (string.Equals(weaponId, BasicIdleAutoDefenseGame.ShardLauncherWeaponId.Value, StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(weaponId, BasicIdleAutoDefenseGame.PulseCannonWeaponId.Value, StringComparison.OrdinalIgnoreCase)) return PulseBeamUnlocked;
                if (string.Equals(weaponId, BasicIdleAutoDefenseGame.ArcBurstTowerWeaponId.Value, StringComparison.OrdinalIgnoreCase)) return ArcBurstUnlocked;
                if (string.Equals(weaponId, BasicIdleAutoDefenseGame.HomingSpireWeaponId.Value, StringComparison.OrdinalIgnoreCase)) return HomingPulseUnlocked;
                return false;
            }

            if (module.Role == IdleAutoDefenseModuleRole.PrecisionBeam) return PulseBeamUnlocked;
            if (module.Role == IdleAutoDefenseModuleRole.AreaBurst) return ArcBurstUnlocked;
            if (module.Role == IdleAutoDefenseModuleRole.HomingProjectile) return HomingPulseUnlocked;
            return module.StartsUnlocked;
        }

        internal void ApplyPersistentProgressionEffect(IdleAutoDefenseResearchNodeRecord node)
        {
            if (node == null || node.EffectAmountPerRank == 0d) return;
            if (node.EffectKind == IdleAutoDefenseProgressionEffectKind.ObjectiveMaximumHealth && _effects.HasObjective)
                _effects.ChangeObjectiveMaximum(node.EffectAmountPerRank, MaximumChangePolicy.FillToMaximum);
            else if (node.EffectKind == IdleAutoDefenseProgressionEffectKind.DamageRank)
                DamageUpgradeRank += Math.Max(1, (int)Math.Round(node.EffectAmountPerRank));
            else if (node.EffectKind == IdleAutoDefenseProgressionEffectKind.ExtraProjectile)
                _shardVolleyBonus += Math.Max(1, (int)Math.Round(node.EffectAmountPerRank));
            else if (node.EffectKind == IdleAutoDefenseProgressionEffectKind.OfflineRewardMultiplier)
                OfflineRewardMultiplierBonus += Math.Max(0d, node.EffectAmountPerRank);
        }

        internal void ApplyUpgrade(RunUpgradeDefinition upgrade)
        {
            for (int i = 0; i < upgrade.Effects.Count; i++)
            {
                RunUpgradeEffectDescriptor effect = upgrade.Effects[i];
                if (effect.EffectId.Value == "idle-auto-defense.direct.damage_bonus") DirectDamageBonus += effect.Amount;
                else if (effect.EffectId.Value == "idle-auto-defense.projectile.speed_multiplier") ProjectileSpeedMultiplier += effect.Amount;
                else if (effect.EffectId.Value == "idle-auto-defense.objective.heal") _effects.HealObjective(effect.Amount);
                else if (effect.EffectId.Value == "idle-auto-defense.objective.max_health") _effects.ChangeObjectiveMaximum(effect.Amount, MaximumChangePolicy.FillToMaximum);
                else if (effect.EffectId.Value == "idle-auto-defense.weapon.fire_rate_intent") AttackSpeedUpgradeRank++;
                else if (effect.EffectId.Value == "idle-auto-defense.weapon.range_intent")
                {
                    RangeUpgradeRank++;
                    DirectDamageBonus += Math.Max(0.5d, effect.Amount * 0.5d);
                }
                else if (effect.EffectId.Value == "idle-auto-defense.enemy.spawn_delay_ticks") EnemySpawnDelayTicks += (int)effect.Amount;
                else if (effect.EffectId.Value == "idle-auto-defense.reward.credits_multiplier") RewardCreditMultiplierBonus += effect.Amount;
                else if (effect.EffectId.Value == "idle-auto-defense.offline.credits_multiplier") OfflineRewardMultiplierBonus += effect.Amount;
                else UnsupportedUpgradeIntentCount++;
            }
        }

        internal void UpdateOverdriveTimers(float deltaSeconds)
        {
            float safeDelta = Mathf.Max(0f, deltaSeconds);
            if (_overdriveSecondsRemaining > 0f)
                _overdriveSecondsRemaining = Mathf.Max(0f, _overdriveSecondsRemaining - safeDelta);
            if (_overdriveCooldownSecondsRemaining > 0f)
                _overdriveCooldownSecondsRemaining = Mathf.Max(0f, _overdriveCooldownSecondsRemaining - safeDelta);
        }

        internal int ResolveUpgradeCost(string costId, int rank, int fallbackBaseCost = 0)
        {
            IdleAutoDefenseCostCurve curve = _activeEconomy == null ? null : _activeEconomy.GetUpgradeCostCurve(costId);
            return curve == null ? CalculateUpgradeCost(fallbackBaseCost, rank) : curve.Calculate(rank);
        }

        internal int ResolveModuleBuildCost(IdleAutoDefenseModuleRole role, int fallbackCost)
        {
            IdleAutoDefenseModuleRule module = _activeGameRules == null ? null : _activeGameRules.GetModule(role);
            return module == null ? fallbackCost : module.BuildCost;
        }

        internal static int CalculateUpgradeCost(int baseCost, int rank)
        {
            return baseCost + Math.Max(0, rank) * (baseCost / 2 + 6);
        }

        internal IdleAutoDefenseModuleRule ResolveModuleRule(IdleAutoDefenseModuleRole role)
        {
            return _activeGameRules == null ? null : _activeGameRules.GetModule(role);
        }

        internal bool ApplyRewardDraftChoice(IdleAutoDefenseRewardDraftChoice choice)
        {
            if (choice == null) return false;
            switch (choice.EffectKind)
            {
                case IdleAutoDefenseRewardEffectKind.UnlockWeapon:
                    return TryUnlockWeapon(choice.TargetWeaponId);
                case IdleAutoDefenseRewardEffectKind.DamageRank:
                    DamageUpgradeRank += Math.Max(1, (int)Math.Round(choice.Amount));
                    return true;
                case IdleAutoDefenseRewardEffectKind.FireRateRank:
                    AttackSpeedUpgradeRank += Math.Max(1, (int)Math.Round(choice.Amount));
                    return true;
                case IdleAutoDefenseRewardEffectKind.RangeRank:
                    RangeUpgradeRank += Math.Max(1, (int)Math.Round(choice.Amount));
                    return true;
                case IdleAutoDefenseRewardEffectKind.Repair:
                    RepairUpgradeRank++;
                    if (_effects.HasObjective)
                    {
                        double maximumHealth = _activeGameRules == null ? 8d : _activeGameRules.DraftRepairMaximumHealth;
                        double baseHeal = _activeGameRules == null ? 28d : _activeGameRules.DraftRepairBaseHeal;
                        double healPerRank = _activeGameRules == null ? 4d : _activeGameRules.DraftRepairHealPerRank;
                        _effects.ChangeObjectiveMaximum(maximumHealth, MaximumChangePolicy.PreserveAbsolute);
                        _effects.HealObjective(baseHeal + RepairUpgradeRank * healPerRank);
                    }

                    return true;
                case IdleAutoDefenseRewardEffectKind.RewardMultiplier:
                    RewardCreditMultiplierBonus += Math.Max(0.05d, choice.Amount);
                    return true;
                case IdleAutoDefenseRewardEffectKind.ProjectileSpeed:
                    ProjectileSpeedMultiplier += Math.Max(0.05d, choice.Amount);
                    return true;
                case IdleAutoDefenseRewardEffectKind.ExtraProjectile:
                    _shardVolleyBonus += Math.Max(1, (int)Math.Round(choice.Amount));
                    return true;
                case IdleAutoDefenseRewardEffectKind.PulsePower:
                    _pulseBeamBonus += Math.Max(1, (int)Math.Round(choice.Amount));
                    return true;
                case IdleAutoDefenseRewardEffectKind.ArcPower:
                    _arcBurstBonus += Math.Max(1, (int)Math.Round(choice.Amount));
                    return true;
                case IdleAutoDefenseRewardEffectKind.HomingPower:
                    _homingPulseBonus += Math.Max(1, (int)Math.Round(choice.Amount));
                    return true;
                case IdleAutoDefenseRewardEffectKind.GlobalDamageMultiplier:
                    _rewardDamageMultiplierBonus += Math.Max(0.05d, choice.Amount);
                    return true;
                default:
                    UnsupportedUpgradeIntentCount++;
                    return false;
            }
        }
    }
}
