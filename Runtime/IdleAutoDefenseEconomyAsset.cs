using System;
using System.Collections.Generic;
using Deucarian.Progression;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    [CreateAssetMenu(menuName = "Deucarian/Idle Auto Defense/Economy", fileName = "IdleAutoDefenseEconomy")]
    public sealed class IdleAutoDefenseEconomyAsset : ScriptableObject
    {
        public const string DamageUpgradeCostId = "cost.idle-auto-defense.damage-up";
        public const string FireRateUpgradeCostId = "cost.idle-auto-defense.fire-rate-up";
        public const string RangeUpgradeCostId = "cost.idle-auto-defense.range-up";
        public const string RepairUpgradeCostId = "cost.idle-auto-defense.repair";
        public const string OverdriveCostId = "cost.idle-auto-defense.overdrive";

        [SerializeField] private string _id = "economy.idle-auto-defense.playable";
        [SerializeField] private string _displayName = "Basic Idle Auto Defense Economy";
        [SerializeField] private IdleAutoDefenseCurrencyRecord[] _currencies = CreateDefaultCurrencies();
        [SerializeField] private string _primaryCurrencyId = "currency.idle-auto-defense.credits";
        [SerializeField] private string _secondaryCurrencyId = "currency.idle-auto-defense.parts";
        [SerializeField] private string _passiveIncomeCurrencyId = "currency.idle-auto-defense.credits";
        [SerializeField] private long _passiveIncomeAmount = 1;
        [SerializeField] private int _passiveIncomeIntervalTicks = 60;
        [SerializeField] private IdleAutoDefenseCostCurve[] _upgradeCosts = CreateDefaultUpgradeCosts();
        [SerializeField] private long _encounterCompletionCredits = 60;
        [SerializeField] private long _encounterCompletionParts = 3;
        [SerializeField] private long _encounterCompletionAccountXp = 35;
        [SerializeField] private long _smallCurrencyBonus = 5;
        [SerializeField] private double _runRewardClaimMultiplier = 2d;

        public string Id => _id ?? string.Empty;
        public string DisplayName => _displayName ?? string.Empty;
        public IReadOnlyList<IdleAutoDefenseCurrencyRecord> Currencies => _currencies ?? Array.Empty<IdleAutoDefenseCurrencyRecord>();
        public string PrimaryCurrencyId => _primaryCurrencyId ?? string.Empty;
        public string SecondaryCurrencyId => _secondaryCurrencyId ?? string.Empty;
        public string PassiveIncomeCurrencyId => _passiveIncomeCurrencyId ?? string.Empty;
        public long PassiveIncomeAmount => _passiveIncomeAmount;
        public int PassiveIncomeIntervalTicks => _passiveIncomeIntervalTicks;
        public IReadOnlyList<IdleAutoDefenseCostCurve> UpgradeCosts => _upgradeCosts ?? Array.Empty<IdleAutoDefenseCostCurve>();
        public long EncounterCompletionCredits => _encounterCompletionCredits;
        public long EncounterCompletionParts => _encounterCompletionParts;
        public long EncounterCompletionAccountXp => _encounterCompletionAccountXp;
        public long SmallCurrencyBonus => _smallCurrencyBonus;
        public double RunRewardClaimMultiplier => _runRewardClaimMultiplier;
        public long StartingCredits => GetCurrency(PrimaryCurrencyId)?.StartingAmount ?? 0L;
        public long StartingParts => GetCurrency(SecondaryCurrencyId)?.StartingAmount ?? 0L;

        public CurrencyId PrimaryCurrency => new CurrencyId(PrimaryCurrencyId);
        public CurrencyId SecondaryCurrency => new CurrencyId(SecondaryCurrencyId);

        public IdleAutoDefenseCurrencyRecord GetCurrency(string currencyId)
        {
            if (string.IsNullOrWhiteSpace(currencyId)) return null;
            for (int i = 0; i < Currencies.Count; i++)
            {
                IdleAutoDefenseCurrencyRecord currency = Currencies[i];
                if (currency != null && string.Equals(currency.Id, currencyId, StringComparison.OrdinalIgnoreCase))
                    return currency;
            }

            return null;
        }

        public IdleAutoDefenseCostCurve GetUpgradeCostCurve(string costId)
        {
            if (string.IsNullOrWhiteSpace(costId)) return null;
            for (int i = 0; i < UpgradeCosts.Count; i++)
            {
                IdleAutoDefenseCostCurve cost = UpgradeCosts[i];
                if (cost != null && string.Equals(cost.Id, costId, StringComparison.OrdinalIgnoreCase))
                    return cost;
            }

            return null;
        }

        public int CalculateUpgradeCost(string costId, int rank)
        {
            IdleAutoDefenseCostCurve curve = GetUpgradeCostCurve(costId);
            return curve == null ? 0 : curve.Calculate(rank);
        }

        public void Configure(
            string id,
            string displayName,
            IReadOnlyList<IdleAutoDefenseCurrencyRecord> currencies,
            string primaryCurrencyId,
            string secondaryCurrencyId,
            string passiveIncomeCurrencyId,
            long passiveIncomeAmount,
            int passiveIncomeIntervalTicks,
            IReadOnlyList<IdleAutoDefenseCostCurve> upgradeCosts,
            long encounterCompletionCredits,
            long encounterCompletionParts,
            long encounterCompletionAccountXp,
            long smallCurrencyBonus,
            double runRewardClaimMultiplier)
        {
            _id = id ?? string.Empty;
            _displayName = displayName ?? string.Empty;
            _currencies = Copy(currencies);
            _primaryCurrencyId = primaryCurrencyId ?? string.Empty;
            _secondaryCurrencyId = secondaryCurrencyId ?? string.Empty;
            _passiveIncomeCurrencyId = passiveIncomeCurrencyId ?? string.Empty;
            _passiveIncomeAmount = passiveIncomeAmount;
            _passiveIncomeIntervalTicks = passiveIncomeIntervalTicks;
            _upgradeCosts = Copy(upgradeCosts);
            _encounterCompletionCredits = encounterCompletionCredits;
            _encounterCompletionParts = encounterCompletionParts;
            _encounterCompletionAccountXp = encounterCompletionAccountXp;
            _smallCurrencyBonus = smallCurrencyBonus;
            _runRewardClaimMultiplier = runRewardClaimMultiplier;
        }

        public static IdleAutoDefenseEconomyAsset CreateTransient(int startingCredits = 10, int startingParts = 0)
        {
            var asset = CreateInstance<IdleAutoDefenseEconomyAsset>();
            asset.hideFlags = HideFlags.HideAndDontSave;
            IdleAutoDefenseCurrencyRecord[] currencies = CreateDefaultCurrencies();
            currencies[0].StartingAmount = startingCredits;
            currencies[1].StartingAmount = startingParts;
            asset._currencies = currencies;
            return asset;
        }

        private static IdleAutoDefenseCurrencyRecord[] CreateDefaultCurrencies()
        {
            return new[]
            {
                new IdleAutoDefenseCurrencyRecord("currency.idle-auto-defense.credits", "Credits", 250000L, 10L),
                new IdleAutoDefenseCurrencyRecord("currency.idle-auto-defense.parts", "Parts", 25000L, 0L)
            };
        }

        private static IdleAutoDefenseCostCurve[] CreateDefaultUpgradeCosts()
        {
            return new[]
            {
                new IdleAutoDefenseCostCurve(DamageUpgradeCostId, "Damage Upgrade", "currency.idle-auto-defense.credits", 20, 16),
                new IdleAutoDefenseCostCurve(FireRateUpgradeCostId, "Fire Rate Upgrade", "currency.idle-auto-defense.credits", 18, 15),
                new IdleAutoDefenseCostCurve(RangeUpgradeCostId, "Range Upgrade", "currency.idle-auto-defense.credits", 18, 15),
                new IdleAutoDefenseCostCurve(RepairUpgradeCostId, "Repair Upgrade", "currency.idle-auto-defense.credits", 16, 14)
                ,new IdleAutoDefenseCostCurve(OverdriveCostId, "Overdrive", "currency.idle-auto-defense.credits", 22, 0)
            };
        }

        private static T[] Copy<T>(IReadOnlyList<T> source)
        {
            if (source == null || source.Count == 0) return Array.Empty<T>();
            var copy = new T[source.Count];
            for (int i = 0; i < source.Count; i++) copy[i] = source[i];
            return copy;
        }
    }

    [Serializable]
    public sealed class IdleAutoDefenseCurrencyRecord
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private long _capacity;
        [SerializeField] private long _startingAmount;

        public IdleAutoDefenseCurrencyRecord()
        {
        }

        public IdleAutoDefenseCurrencyRecord(string id, string displayName, long capacity, long startingAmount)
        {
            _id = id ?? string.Empty;
            _displayName = displayName ?? string.Empty;
            _capacity = capacity;
            _startingAmount = startingAmount;
        }

        public string Id => _id ?? string.Empty;
        public string DisplayName => _displayName ?? string.Empty;
        public long Capacity => _capacity;
        public long StartingAmount
        {
            get => _startingAmount;
            set => _startingAmount = value;
        }
    }

    [Serializable]
    public sealed class IdleAutoDefenseCostCurve
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private string _currencyId;
        [SerializeField] private int _baseCost;
        [SerializeField] private int _costPerRank;

        public IdleAutoDefenseCostCurve()
        {
        }

        public IdleAutoDefenseCostCurve(string id, string displayName, string currencyId, int baseCost, int costPerRank)
        {
            _id = id ?? string.Empty;
            _displayName = displayName ?? string.Empty;
            _currencyId = currencyId ?? string.Empty;
            _baseCost = baseCost;
            _costPerRank = costPerRank;
        }

        public string Id => _id ?? string.Empty;
        public string DisplayName => _displayName ?? string.Empty;
        public string CurrencyId => _currencyId ?? string.Empty;
        public int BaseCost => _baseCost;
        public int CostPerRank => _costPerRank;
        public int Calculate(int rank) => Math.Max(0, BaseCost + Math.Max(0, rank) * CostPerRank);
    }
}
