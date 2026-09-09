using System;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    internal sealed class IdleAutoDefenseRunTimeline
    {
        private readonly IdleAutoDefenseFrameClock _clock;
        internal int ElapsedTicks { get; set; }
        internal int Sequence { get; private set; }
        internal float SurvivalSeconds { get; set; }
        internal int SpawnedCount { get; set; }
        internal int ObjectiveReachCount { get; set; }
        internal int ObjectiveDamageEvents { get; set; }
        internal bool VictoryReached { get; set; }
        internal bool EndlessRestartPending { get; set; }
        internal bool RewardDraftPausesCombat { get; set; } = true;

        internal IdleAutoDefenseRunTimeline(Func<float, bool> simulationTick)
            => _clock = new IdleAutoDefenseFrameClock(simulationTick);

        internal void BeginRun() => Sequence++;

        internal bool ConsumeRestartRequest()
        {
            if (!EndlessRestartPending) return false;
            EndlessRestartPending = false;
            return true;
        }

        internal void AdvanceFrame(float deltaSeconds, IdleAutoDefenseRunProfileAsset profile)
        {
            bool fixedRate = profile != null && profile.TickSemantics == IdleAutoDefenseTickSemantics.FixedRate;
            _clock.Advance(deltaSeconds, fixedRate, fixedRate ? profile.SecondsPerSimulationTick : 0f);
        }

        internal void Reset()
        {
            ElapsedTicks = 0;
            SurvivalSeconds = 0f;
            SpawnedCount = 0;
            ObjectiveReachCount = 0;
            ObjectiveDamageEvents = 0;
            VictoryReached = false;
            EndlessRestartPending = false;
            _clock.Reset();
        }
    }
}
