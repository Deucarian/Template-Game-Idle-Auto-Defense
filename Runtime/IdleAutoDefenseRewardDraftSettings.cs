using System;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    [Serializable]
    public sealed class IdleAutoDefenseRewardDraftSettings
    {
        [SerializeField] private int _choiceCount = 3;
        [SerializeField] private long _normalEnemyExperience = 9;
        [SerializeField] private long _eliteEnemyExperience = 42;
        [SerializeField] private long _bossEnemyExperience = 120;
        [SerializeField] private long _waveCompletionExperience = 18;
        [SerializeField] private long _baseExperienceToNextLevel = 38;
        [SerializeField] private long _experienceToNextLevelGrowth = 18;
        [SerializeField] private int _normalInvestmentsForEpic = 3;
        [SerializeField] private int _epicInvestmentsForLegendary = 3;
        [SerializeField] private float _projectileRetargetRadius = 3.25f;
        [SerializeField] private double _levelUpUnlockWeightMultiplier = 1.35d;
        [SerializeField] private double _eliteUnlockWeightMultiplier = 1.1d;
        [SerializeField] private double _bossUnlockWeightMultiplier = 1.1d;
        [SerializeField] private IdleAutoDefenseRarityWeights _levelUpRarityWeights = new IdleAutoDefenseRarityWeights(100d, 70d, 36d, 12d, 2d);
        [SerializeField] private IdleAutoDefenseRarityWeights _eliteRarityWeights = new IdleAutoDefenseRarityWeights(22d, 56d, 78d, 44d, 10d);
        [SerializeField] private IdleAutoDefenseRarityWeights _bossRarityWeights = new IdleAutoDefenseRarityWeights(8d, 24d, 58d, 86d, 42d);

        public static IdleAutoDefenseRewardDraftSettings CreateDefault()
        {
            return new IdleAutoDefenseRewardDraftSettings();
        }

        public int ChoiceCount
        {
            get => ClampInt(_choiceCount, 1, 3);
            set => _choiceCount = ClampInt(value, 1, 3);
        }

        public long NormalEnemyExperience
        {
            get => Math.Max(0L, _normalEnemyExperience);
            set => _normalEnemyExperience = Math.Max(0L, value);
        }

        public long EliteEnemyExperience
        {
            get => Math.Max(0L, _eliteEnemyExperience);
            set => _eliteEnemyExperience = Math.Max(0L, value);
        }

        public long BossEnemyExperience
        {
            get => Math.Max(0L, _bossEnemyExperience);
            set => _bossEnemyExperience = Math.Max(0L, value);
        }

        public long WaveCompletionExperience
        {
            get => Math.Max(0L, _waveCompletionExperience);
            set => _waveCompletionExperience = Math.Max(0L, value);
        }

        public long BaseExperienceToNextLevel
        {
            get => Math.Max(1L, _baseExperienceToNextLevel);
            set => _baseExperienceToNextLevel = Math.Max(1L, value);
        }

        public long ExperienceToNextLevelGrowth
        {
            get => Math.Max(0L, _experienceToNextLevelGrowth);
            set => _experienceToNextLevelGrowth = Math.Max(0L, value);
        }

        public int NormalInvestmentsForEpic
        {
            get => ClampInt(_normalInvestmentsForEpic, 1, 3);
            set => _normalInvestmentsForEpic = ClampInt(value, 1, 3);
        }

        public int EpicInvestmentsForLegendary
        {
            get => ClampInt(_epicInvestmentsForLegendary, 1, 3);
            set => _epicInvestmentsForLegendary = ClampInt(value, 1, 3);
        }

        public float ProjectileRetargetRadius
        {
            get => Mathf.Clamp(_projectileRetargetRadius, 0.25f, 12f);
            set => _projectileRetargetRadius = Mathf.Clamp(value, 0.25f, 12f);
        }

        public IdleAutoDefenseRarityWeights LevelUpRarityWeights => _levelUpRarityWeights ??= new IdleAutoDefenseRarityWeights(100d, 70d, 36d, 12d, 2d);
        public IdleAutoDefenseRarityWeights EliteRarityWeights => _eliteRarityWeights ??= new IdleAutoDefenseRarityWeights(22d, 56d, 78d, 44d, 10d);
        public IdleAutoDefenseRarityWeights BossRarityWeights => _bossRarityWeights ??= new IdleAutoDefenseRarityWeights(8d, 24d, 58d, 86d, 42d);

        public long CalculateExperienceToNextLevel(int level)
        {
            return BaseExperienceToNextLevel + Math.Max(0, level - 1) * ExperienceToNextLevelGrowth;
        }

        public double GetRarityWeight(IdleAutoDefenseRewardDraftKind kind, IdleAutoDefenseRewardRarity rarity)
        {
            if (kind == IdleAutoDefenseRewardDraftKind.BossDefeated)
                return BossRarityWeights.GetWeight(rarity);
            if (kind == IdleAutoDefenseRewardDraftKind.EliteDefeated)
                return EliteRarityWeights.GetWeight(rarity);
            return LevelUpRarityWeights.GetWeight(rarity);
        }

        public double GetUnlockWeightMultiplier(IdleAutoDefenseRewardDraftKind kind)
        {
            if (kind == IdleAutoDefenseRewardDraftKind.BossDefeated)
                return Math.Max(0.1d, _bossUnlockWeightMultiplier);
            if (kind == IdleAutoDefenseRewardDraftKind.EliteDefeated)
                return Math.Max(0.1d, _eliteUnlockWeightMultiplier);
            return Math.Max(0.1d, _levelUpUnlockWeightMultiplier);
        }

        private static int ClampInt(int value, int minimum, int maximum)
        {
            if (value < minimum) return minimum;
            if (value > maximum) return maximum;
            return value;
        }
    }

    [Serializable]
    public sealed class IdleAutoDefenseRarityWeights
    {
        [SerializeField] private double _common;
        [SerializeField] private double _uncommon;
        [SerializeField] private double _rare;
        [SerializeField] private double _epic;
        [SerializeField] private double _legendary;

        public IdleAutoDefenseRarityWeights(double common, double uncommon, double rare, double epic, double legendary)
        {
            _common = common;
            _uncommon = uncommon;
            _rare = rare;
            _epic = epic;
            _legendary = legendary;
        }

        public double Common { get => Math.Max(0d, _common); set => _common = Math.Max(0d, value); }
        public double Uncommon { get => Math.Max(0d, _uncommon); set => _uncommon = Math.Max(0d, value); }
        public double Rare { get => Math.Max(0d, _rare); set => _rare = Math.Max(0d, value); }
        public double Epic { get => Math.Max(0d, _epic); set => _epic = Math.Max(0d, value); }
        public double Legendary { get => Math.Max(0d, _legendary); set => _legendary = Math.Max(0d, value); }

        public double GetWeight(IdleAutoDefenseRewardRarity rarity)
        {
            if (rarity == IdleAutoDefenseRewardRarity.Legendary) return Legendary;
            if (rarity == IdleAutoDefenseRewardRarity.Epic) return Epic;
            if (rarity == IdleAutoDefenseRewardRarity.Rare) return Rare;
            if (rarity == IdleAutoDefenseRewardRarity.Uncommon) return Uncommon;
            return Common;
        }
    }
}
