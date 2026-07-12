using System;
using System.Collections.Generic;
using Deucarian.Attacks.Authoring;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    public enum IdleAutoDefenseRunTimeUnit
    {
        SimulationTicks = 0
    }

    public enum IdleAutoDefenseTickSemantics
    {
        FixedRate = 0
    }

    public enum IdleAutoDefenseVictoryRule
    {
        AllAuthoredWavesCleared = 0,
        SurviveSessionDuration = 1
    }

    public enum IdleAutoDefenseDefeatRule
    {
        ObjectiveDestroyed = 0
    }

    [CreateAssetMenu(menuName = "Deucarian/Idle Auto Defense/Run Profile", fileName = "IdleAutoDefenseRunProfile")]
    public sealed class IdleAutoDefenseRunProfileAsset : ScriptableObject
    {
        [SerializeField] private string _id = "run-profile.idle-auto-defense.playable";
        [SerializeField] private string _displayName = "Basic Idle Auto Defense Run";
        [SerializeField] private IdleAutoDefenseRunTimeUnit _timeUnit = IdleAutoDefenseRunTimeUnit.SimulationTicks;
        [SerializeField] private IdleAutoDefenseTickSemantics _tickSemantics = IdleAutoDefenseTickSemantics.FixedRate;
        [SerializeField] private int _simulationTicksPerSecond = 20;
        [SerializeField] private int _sessionLengthTicks = 5600;
        [SerializeField] private WaveDefinitionAsset[] _waves = Array.Empty<WaveDefinitionAsset>();
        [SerializeField] private float _difficultyMultiplier = 1f;
        [SerializeField] private bool _endless;
        [SerializeField] private IdleAutoDefenseVictoryRule _victoryRule = IdleAutoDefenseVictoryRule.AllAuthoredWavesCleared;
        [SerializeField] private IdleAutoDefenseDefeatRule _defeatRule = IdleAutoDefenseDefeatRule.ObjectiveDestroyed;
        [SerializeField] private float _rewardMultiplier = 1f;
        [SerializeField] private int _preparationTicks;
        [SerializeField] private int _encounterSeed = 20260623;

        public string Id => _id ?? string.Empty;
        public string DisplayName => _displayName ?? string.Empty;
        public IdleAutoDefenseRunTimeUnit TimeUnit => _timeUnit;
        public IdleAutoDefenseTickSemantics TickSemantics => _tickSemantics;
        public int SimulationTicksPerSecond => _simulationTicksPerSecond;
        public float SecondsPerSimulationTick => 1f / Math.Max(1, SimulationTicksPerSecond);
        public int SessionLengthTicks => _sessionLengthTicks;
        public double SessionLengthSeconds => (double)Math.Max(0, SessionLengthTicks) / Math.Max(1, SimulationTicksPerSecond);
        public IReadOnlyList<WaveDefinitionAsset> Waves => _waves ?? Array.Empty<WaveDefinitionAsset>();
        public float DifficultyMultiplier => _difficultyMultiplier;
        public bool Endless => _endless;
        public IdleAutoDefenseVictoryRule VictoryRule => _victoryRule;
        public IdleAutoDefenseDefeatRule DefeatRule => _defeatRule;
        public float RewardMultiplier => _rewardMultiplier;
        public int PreparationTicks => _preparationTicks;
        public int EncounterSeed => _encounterSeed;

        public void Configure(
            string id,
            string displayName,
            int simulationTicksPerSecond,
            int sessionLengthTicks,
            IReadOnlyList<WaveDefinitionAsset> waves,
            float difficultyMultiplier,
            bool endless,
            IdleAutoDefenseVictoryRule victoryRule,
            IdleAutoDefenseDefeatRule defeatRule,
            float rewardMultiplier,
            int preparationTicks,
            int encounterSeed)
        {
            _id = id ?? string.Empty;
            _displayName = displayName ?? string.Empty;
            _timeUnit = IdleAutoDefenseRunTimeUnit.SimulationTicks;
            _tickSemantics = IdleAutoDefenseTickSemantics.FixedRate;
            _simulationTicksPerSecond = simulationTicksPerSecond;
            _sessionLengthTicks = sessionLengthTicks;
            _waves = Copy(waves);
            _difficultyMultiplier = difficultyMultiplier;
            _endless = endless;
            _victoryRule = victoryRule;
            _defeatRule = defeatRule;
            _rewardMultiplier = rewardMultiplier;
            _preparationTicks = preparationTicks;
            _encounterSeed = encounterSeed;
        }

        public static IdleAutoDefenseRunProfileAsset CreateTransient(
            IReadOnlyList<WaveDefinitionAsset> waves,
            float difficultyMultiplier = 1f,
            int sessionLengthTicks = 5600,
            bool endless = false,
            float rewardMultiplier = 1f,
            int simulationTicksPerSecond = 20)
        {
            var asset = CreateInstance<IdleAutoDefenseRunProfileAsset>();
            asset.hideFlags = HideFlags.HideAndDontSave;
            asset.Configure(
                "run-profile.idle-auto-defense.transient-fallback",
                "Transient Idle Auto Defense Fallback Run",
                simulationTicksPerSecond,
                sessionLengthTicks,
                waves,
                difficultyMultiplier,
                endless,
                IdleAutoDefenseVictoryRule.AllAuthoredWavesCleared,
                IdleAutoDefenseDefeatRule.ObjectiveDestroyed,
                rewardMultiplier,
                0,
                20260623);
            return asset;
        }

        private static WaveDefinitionAsset[] Copy(IReadOnlyList<WaveDefinitionAsset> source)
        {
            if (source == null || source.Count == 0) return Array.Empty<WaveDefinitionAsset>();
            var copy = new WaveDefinitionAsset[source.Count];
            for (int i = 0; i < source.Count; i++) copy[i] = source[i];
            return copy;
        }

    }
}
