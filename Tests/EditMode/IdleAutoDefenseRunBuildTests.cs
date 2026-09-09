using System.Collections.Generic;
using Deucarian.Combat;
using NUnit.Framework;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense.Tests
{
    public sealed class IdleAutoDefenseRunBuildTests
    {
        [Test]
        public void WalletPreservesPassiveRemainderAndRejectedPurchasesDoNotSpend()
        {
            var wallet = new IdleAutoDefenseRunWallet();
            wallet.SetStartingBalance(10);
            wallet.GrantPassiveIncome(59, null);
            Assert.That(wallet.Balance, Is.EqualTo(10));
            wallet.GrantPassiveIncome(62, null);
            Assert.That(wallet.Balance, Is.EqualTo(12));
            wallet.GrantPassiveIncome(59, null);
            Assert.That(wallet.Balance, Is.EqualTo(13));
            Assert.That(wallet.TrySpend(3, false), Is.False);
            Assert.That(wallet.TrySpend(0, true), Is.False);
            Assert.That(wallet.TrySpend(14, true), Is.False);
            Assert.That(wallet.Spent, Is.Zero);
            Assert.That(wallet.TrySpend(3, true), Is.True);
            Assert.That(wallet.Balance, Is.EqualTo(10));
            Assert.That(wallet.Earned, Is.EqualTo(3));
            wallet.Reset();
            Assert.That(wallet.AwardKills(2, 0, 0.25d), Is.EqualTo(13));
            Assert.That(wallet.AwardKills(2, 20, 0.25d), Is.EqualTo(25));
            Assert.That(wallet.Earned, Is.EqualTo(38));
        }

        [Test]
        public void PurchasesApplyAuthoredCostsAndHealthCommandsExactlyOnce()
        {
            var economy = IdleAutoDefenseEconomyAsset.CreateTransient();
            var wallet = new IdleAutoDefenseRunWallet();
            wallet.SetStartingBalance(1000);
            var effects = new Effects();
            var build = new IdleAutoDefenseRunBuild(() => null, () => economy, wallet, effects);
            try
            {
                effects.Running = false;
                Assert.That(build.TryPurchaseDamageUpgrade(), Is.False);
                Assert.That(wallet.Spent, Is.Zero);
                effects.Running = true;
                int damageCost = build.DamageUpgradeCost;
                Assert.That(build.TryPurchaseDamageUpgrade(), Is.True);
                Assert.That(build.DamageUpgradeRank, Is.EqualTo(1));
                Assert.That(wallet.Spent, Is.EqualTo(damageCost));
                Assert.That(build.TryPurchaseRepairUpgrade(), Is.True);
                Assert.That(effects.HealthCommands, Is.EqualTo(new[] { "maximum:8:PreserveAbsolute", "heal:40" }));
                Assert.That(build.SelectedUpgradeCount, Is.EqualTo(2));
                Assert.That(effects.FeedbackCount, Is.EqualTo(2));
            }
            finally { Object.DestroyImmediate(economy); }
        }

        [Test]
        public void UnlockCommandsAreIdempotentAndOverdriveRequiresItsFullCooldown()
        {
            var wallet = new IdleAutoDefenseRunWallet();
            wallet.SetStartingBalance(1000);
            var effects = new Effects();
            var build = new IdleAutoDefenseRunBuild(() => null, () => null, wallet, effects);
            Assert.That(build.TryUnlockWeapon(BasicIdleAutoDefenseGame.PulseCannonWeaponId.Value), Is.True);
            Assert.That(build.TryUnlockWeapon(BasicIdleAutoDefenseGame.PulseCannonWeaponId.Value), Is.False);
            Assert.That(effects.WeaponCount, Is.EqualTo(1));
            Assert.That(build.TryPurchaseOverdrive(), Is.True);
            long spent = wallet.Spent;
            Assert.That(build.TryPurchaseOverdrive(), Is.False);
            Assert.That(wallet.Spent, Is.EqualTo(spent));
            build.UpdateOverdriveTimers(7f);
            Assert.That(build.OverdriveActive, Is.False);
            Assert.That(build.TryPurchaseOverdrive(), Is.False);
            build.UpdateOverdriveTimers(18f);
            Assert.That(build.TryPurchaseOverdrive(), Is.True);
            build.Reset();
            Assert.That(build.PulseBeamUnlocked, Is.False);
            Assert.That(build.OverdriveActive, Is.False);
            Assert.That(build.ProjectileSpeedMultiplier, Is.EqualTo(1d));
        }

        private sealed class Effects : IIdleAutoDefenseBuildEffects
        {
            internal bool Running = true;
            internal int WeaponCount;
            internal int FeedbackCount;
            internal readonly List<string> HealthCommands = new List<string>();
            public bool EncounterRunning => Running;
            public bool HasObjective => true;
            public void ChangeObjectiveMaximum(double amount, MaximumChangePolicy policy) => HealthCommands.Add("maximum:" + amount + ":" + policy);
            public void HealObjective(double amount) => HealthCommands.Add("heal:" + amount);
            public void CreateWeapon(string weaponId, string attackId, bool enabled) => WeaponCount++;
            public void Feedback(string text, Color color, float scale) => FeedbackCount++;
        }
    }
}
