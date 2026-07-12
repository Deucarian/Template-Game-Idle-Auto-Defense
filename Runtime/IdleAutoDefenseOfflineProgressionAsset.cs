using System;
using Deucarian.IdleProgression;
using Deucarian.Progression;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    public enum IdleAutoDefenseOfflineRounding
    {
        Floor = 0
    }

    [CreateAssetMenu(menuName = "Deucarian/Idle Auto Defense/Offline Progression", fileName = "IdleAutoDefenseOfflineProgression")]
    public sealed class IdleAutoDefenseOfflineProgressionAsset : ScriptableObject
    {
        [SerializeField] private string _id = "offline-progression.idle-auto-defense.playable";
        [SerializeField] private string _displayName = "Basic Idle Auto Defense Offline Progression";
        [SerializeField] private bool _enabled = true;
        [SerializeField] private double _maximumOfflineSeconds = 28800d;
        [SerializeField] private double _minimumEligibleSeconds;
        [SerializeField] private string _productionCurrencyId = "currency.idle-auto-defense.credits";
        [SerializeField] private double _productionAmountPerSecond = 0.35d;
        [SerializeField] private string _cycleCurrencyId = "currency.idle-auto-defense.parts";
        [SerializeField] private long _cycleRewardAmount = 1;
        [SerializeField] private double _cycleDurationSeconds = 240d;
        [SerializeField] private double _claimMultiplier = 2d;
        [SerializeField] private IdleAutoDefenseOfflineRounding _rounding = IdleAutoDefenseOfflineRounding.Floor;
        [SerializeField] private string _saveTimestampKey = "idle-auto-defense.last-seen-utc";

        public string Id => _id ?? string.Empty;
        public string DisplayName => _displayName ?? string.Empty;
        public bool Enabled => _enabled;
        public double MaximumOfflineSeconds => _maximumOfflineSeconds;
        public double MinimumEligibleSeconds => _minimumEligibleSeconds;
        public string ProductionCurrencyId => _productionCurrencyId ?? string.Empty;
        public double ProductionAmountPerSecond => _productionAmountPerSecond;
        public string CycleCurrencyId => _cycleCurrencyId ?? string.Empty;
        public long CycleRewardAmount => _cycleRewardAmount;
        public double CycleDurationSeconds => _cycleDurationSeconds;
        public double ClaimMultiplier => _claimMultiplier;
        public IdleAutoDefenseOfflineRounding Rounding => _rounding;
        public string SaveTimestampKey => _saveTimestampKey ?? string.Empty;

        public IdleProgressionDefinition CreateRuntimeDefinition()
        {
            return new IdleProgressionDefinition(
                TimeSpan.FromSeconds(Math.Max(1d, MaximumOfflineSeconds)),
                ProductionAmountPerSecond <= 0d
                    ? Array.Empty<IdleProductionRate>()
                    : new[] { new IdleProductionRate(new CurrencyId(ProductionCurrencyId), ProductionAmountPerSecond) },
                CycleRewardAmount <= 0L || CycleDurationSeconds <= 0d
                    ? Array.Empty<IdleCycleReward>()
                    : new[]
                    {
                        new IdleCycleReward(
                            new CurrencyId(CycleCurrencyId),
                            new ProgressionAmount(CycleRewardAmount),
                            TimeSpan.FromSeconds(CycleDurationSeconds))
                    });
        }

        public IdleProgressionResult Calculate(DateTimeOffset lastSeenUtc, DateTimeOffset nowUtc)
        {
            TimeSpan raw = nowUtc - lastSeenUtc;
            if (!Enabled || raw >= TimeSpan.Zero && raw.TotalSeconds < MinimumEligibleSeconds)
                return new IdleProgressionResult(IdleProgressionResultCode.NoElapsedTime, raw, TimeSpan.Zero, false, new RewardBundle());
            return IdleProgressionCalculator.Calculate(lastSeenUtc, nowUtc, CreateRuntimeDefinition());
        }

        public void Configure(
            string id,
            string displayName,
            bool enabled,
            double maximumOfflineSeconds,
            double minimumEligibleSeconds,
            string productionCurrencyId,
            double productionAmountPerSecond,
            string cycleCurrencyId,
            long cycleRewardAmount,
            double cycleDurationSeconds,
            double claimMultiplier,
            string saveTimestampKey)
        {
            _id = id ?? string.Empty;
            _displayName = displayName ?? string.Empty;
            _enabled = enabled;
            _maximumOfflineSeconds = maximumOfflineSeconds;
            _minimumEligibleSeconds = minimumEligibleSeconds;
            _productionCurrencyId = productionCurrencyId ?? string.Empty;
            _productionAmountPerSecond = productionAmountPerSecond;
            _cycleCurrencyId = cycleCurrencyId ?? string.Empty;
            _cycleRewardAmount = cycleRewardAmount;
            _cycleDurationSeconds = cycleDurationSeconds;
            _claimMultiplier = claimMultiplier;
            _rounding = IdleAutoDefenseOfflineRounding.Floor;
            _saveTimestampKey = saveTimestampKey ?? string.Empty;
        }

        public static IdleAutoDefenseOfflineProgressionAsset CreateTransient()
        {
            var asset = CreateInstance<IdleAutoDefenseOfflineProgressionAsset>();
            asset.hideFlags = HideFlags.HideAndDontSave;
            return asset;
        }
    }
}
