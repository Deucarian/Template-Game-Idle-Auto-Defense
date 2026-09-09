using System;
using Deucarian.Monetization;
using Deucarian.RunUpgrades;
using Deucarian.RunUpgrades.Authoring;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    internal sealed class IdleAutoDefenseRewardOffers
    {
        private readonly IdleAutoDefensePersistentProgression _progression;
        private readonly IdleAutoDefenseLegacyDraft _draft;
        private readonly Func<IdleAutoDefenseRunProfileAsset> _profile;
        private readonly Func<IdleAutoDefenseEconomyAsset> _economy;
        private readonly Func<IdleAutoDefenseOfflineProgressionAsset> _offline;
        private readonly Func<bool> _running;
        private readonly Func<bool> _terminal;
        private MonetizationSession _session;

        internal IdleAutoDefenseRewardOffers(IdleAutoDefensePersistentProgression progression,
            IdleAutoDefenseLegacyDraft draft, Func<IdleAutoDefenseRunProfileAsset> profile,
            Func<IdleAutoDefenseEconomyAsset> economy, Func<IdleAutoDefenseOfflineProgressionAsset> offline,
            Func<bool> running, Func<bool> terminal)
        {
            _progression = progression;
            _draft = draft;
            _profile = profile;
            _economy = economy;
            _offline = offline;
            _running = running;
            _terminal = terminal;
        }

        internal MonetizationSession Session
        {
            get => _session ??= IdleAutoDefenseTemplateMonetization.CreateMockSession();
            set => _session = value;
        }

        internal bool ReviveOfferAccepted { get; private set; }
        internal void ResetRun() => ReviveOfferAccepted = false;
        private IdleAutoDefenseEconomyAsset _activeEconomy => _economy();
        private IdleAutoDefenseOfflineProgressionAsset _activeOfflineProgression => _offline();

        private MonetizationFlowContext CreateMonetizationContext(DateTimeOffset nowUtc)
            => new MonetizationFlowContext(nowUtc, _running(), _terminal() ? 1 : 0);

        internal MonetizationAvailability ResolveMonetizationAvailability(
            MonetizationPlacementId placementId,
            MonetizationPlacementKind kind,
            DateTimeOffset nowUtc)
        {
            return Session.GetAvailability(placementId, kind, CreateMonetizationContext(nowUtc));
        }

        internal MonetizationResult OfferDoubleOfflineReward(RewardClaimId claimId, DateTimeOffset nowUtc)
        {
            MonetizationResult result = Session.ShowRewarded(
                IdleAutoDefenseTemplateMonetization.DoubleOfflineReward,
                claimId,
                CreateMonetizationContext(nowUtc));
            if (result.Succeeded)
            {
                double multiplier = _activeOfflineProgression == null ? 2d : _activeOfflineProgression.ClaimMultiplier;
                _progression.MultiplyOfflineClaim(multiplier);
            }

            return result;
        }

        internal MonetizationResult OfferUpgradeDraftReroll(RewardClaimId claimId, DateTimeOffset nowUtc)
        {
            MonetizationResult result = Session.ShowRewarded(
                IdleAutoDefenseTemplateMonetization.RerollUpgradeDraft,
                claimId,
                CreateMonetizationContext(nowUtc));
            if (result.Succeeded) _draft.Reroll(_profile() == null ? 20260623 : _profile().EncounterSeed);

            return result;
        }

        internal MonetizationResult OfferReviveAfterFailure(RewardClaimId claimId, DateTimeOffset nowUtc)
        {
            MonetizationResult result = Session.ShowRewarded(
                IdleAutoDefenseTemplateMonetization.ReviveAfterFailure,
                claimId,
                CreateMonetizationContext(nowUtc));
            if (result.Succeeded) ReviveOfferAccepted = true;
            return result;
        }

        internal MonetizationResult OfferDoubleRunReward(RewardClaimId claimId, DateTimeOffset nowUtc)
        {
            MonetizationResult result = Session.ShowRewarded(
                IdleAutoDefenseTemplateMonetization.DoubleRunReward,
                claimId,
                CreateMonetizationContext(nowUtc));
            if (result.Succeeded)
            {
                double multiplier = _activeEconomy == null ? 2d : _activeEconomy.RunRewardClaimMultiplier;
                _progression.MultiplyEncounterClaim(multiplier);
            }

            return result;
        }

        internal MonetizationResult OfferSmallCurrencyBonus(RewardClaimId claimId, DateTimeOffset nowUtc)
        {
            MonetizationResult result = Session.ShowRewarded(
                IdleAutoDefenseTemplateMonetization.SmallCurrencyBonus,
                claimId,
                CreateMonetizationContext(nowUtc));
            if (result.Succeeded) _progression.AddCurrencyClaim(_activeEconomy == null ? 5 : Math.Max(0L, _activeEconomy.SmallCurrencyBonus));
            return result;
        }

        internal MonetizationResult TryShowTransitionInterstitial(bool afterFailure, DateTimeOffset nowUtc)
        {
            MonetizationPlacementId placementId = afterFailure
                ? IdleAutoDefenseTemplateMonetization.InterstitialAfterRunFailure
                : IdleAutoDefenseTemplateMonetization.InterstitialAfterRunCompletion;
            return Session.ShowInterstitial(placementId, CreateMonetizationContext(nowUtc));
        }

    }
}
