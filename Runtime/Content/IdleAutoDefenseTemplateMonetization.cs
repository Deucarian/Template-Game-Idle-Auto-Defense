using System;
using Deucarian.Monetization;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    public static class IdleAutoDefenseTemplateMonetization
    {
        private const string InterstitialCooldownGroup = "idle-auto-defense.interstitial.global";

        public static readonly MonetizationPlacementId DoubleRunReward = new MonetizationPlacementId("idle-auto-defense.rewarded.double-run-reward");
        public static readonly MonetizationPlacementId ReviveAfterFailure = new MonetizationPlacementId("idle-auto-defense.rewarded.revive-after-failure");
        public static readonly MonetizationPlacementId RerollUpgradeDraft = new MonetizationPlacementId("idle-auto-defense.rewarded.reroll-upgrade-draft");
        public static readonly MonetizationPlacementId DoubleOfflineReward = new MonetizationPlacementId("idle-auto-defense.rewarded.double-offline-reward");
        public static readonly MonetizationPlacementId SmallCurrencyBonus = new MonetizationPlacementId("idle-auto-defense.rewarded.small-currency-bonus");
        public static readonly MonetizationPlacementId InterstitialAfterRunCompletion = new MonetizationPlacementId("idle-auto-defense.interstitial.after-run-completion");
        public static readonly MonetizationPlacementId InterstitialAfterRunFailure = new MonetizationPlacementId("idle-auto-defense.interstitial.after-run-failure");

        public static MonetizationPlacementPolicy[] CreatePlacementPolicies()
        {
            return new[]
            {
                Rewarded(DoubleOfflineReward, TimeSpan.FromSeconds(10), 6),
                Rewarded(RerollUpgradeDraft, TimeSpan.FromSeconds(20), 8),
                Rewarded(ReviveAfterFailure, TimeSpan.FromSeconds(30), 1),
                Rewarded(DoubleRunReward, TimeSpan.FromSeconds(10), 6),
                Rewarded(SmallCurrencyBonus, TimeSpan.FromSeconds(20), 5),
                Interstitial(InterstitialAfterRunCompletion),
                Interstitial(InterstitialAfterRunFailure)
            };
        }

        public static MonetizationSession CreateMockSession()
        {
            return new MonetizationSession(CreatePlacementPolicies(), new MockMonetizationProvider());
        }

        public static MonetizationSession CreateNoOpSession()
        {
            return new MonetizationSession(CreatePlacementPolicies(), new NoOpMonetizationProvider());
        }

        private static MonetizationPlacementPolicy Rewarded(MonetizationPlacementId id, TimeSpan cooldown, int sessionCap)
        {
            return new MonetizationPlacementPolicy(id, MonetizationPlacementKind.Rewarded, cooldown, sessionCap);
        }

        private static MonetizationPlacementPolicy Interstitial(MonetizationPlacementId id)
        {
            return new MonetizationPlacementPolicy(
                id,
                MonetizationPlacementKind.Interstitial,
                TimeSpan.FromSeconds(120),
                sessionCap: 3,
                blockBeforeFirstCompletedOrFailedRun: true,
                blockDuringCombat: true,
                blockWhenNoAdsEntitled: true,
                cooldownGroup: InterstitialCooldownGroup);
        }
    }
}
