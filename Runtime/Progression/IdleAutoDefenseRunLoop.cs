using System;
using Deucarian.AutoDefense;
using Deucarian.Encounters;
using Deucarian.Projectiles;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    internal sealed class IdleAutoDefenseRunLoop
    {
        private readonly SpawnRequest[] _spawnBuffer = new SpawnRequest[16];
        private readonly IdleAutoDefenseRuntimeServices _services;
        private readonly IdleAutoDefenseContentBinding _content;
        private readonly IdleAutoDefenseRunTimeline _session;
        private readonly IdleAutoDefenseCombatRuntime _combat;
        private readonly IdleAutoDefenseWorldPresentation _world;
        private readonly IdleAutoDefenseRunFeedback _feedback;
        private readonly IdleAutoDefenseRunBuild _build;
        private readonly IdleAutoDefenseRunWallet _wallet;
        private readonly IdleAutoDefenseRewardProgression _rewards;
        private readonly IdleAutoDefensePersistentProgression _persistent;

        internal IdleAutoDefenseRunLoop(IdleAutoDefenseRuntimeServices services, IdleAutoDefenseContentBinding content,
            IdleAutoDefenseRunTimeline session, IdleAutoDefenseCombatRuntime combat, IdleAutoDefenseWorldPresentation world,
            IdleAutoDefenseRunFeedback feedback, IdleAutoDefenseRunBuild build, IdleAutoDefenseRunWallet wallet,
            IdleAutoDefenseRewardProgression rewards, IdleAutoDefensePersistentProgression persistent)
        {
            _services = services;
            _content = content;
            _session = session;
            _combat = combat;
            _world = world;
            _feedback = feedback;
            _build = build;
            _wallet = wallet;
            _rewards = rewards;
            _persistent = persistent;
        }

        internal void Step(int ticks, float deltaSeconds)
        {
            AutoDefenseRuntime runtime = _services.Runtime;
            if (runtime == null || runtime.State != AutoDefenseRuntimeState.Running) return;
            _session.ElapsedTicks += Math.Max(1, ticks);
            _build.UpdateOverdriveTimers(deltaSeconds);
            if (PauseForDraft(deltaSeconds)) return;
            _session.SurvivalSeconds += Math.Max(0f, deltaSeconds);
            _rewards.OfferFirstRewardDraftIfReady(_content.RewardCatalog == null ? 30f : _content.RewardCatalog.FirstDraftSeconds);
            if (PauseForDraft(deltaSeconds)) return;

            _services.Encounter.AdvanceTicks(_build.EnemySpawnDelayTicks);
            _services.Encounter.DrainSpawnRequests(_spawnBuffer);
            for (int i = 0; i < _spawnBuffer.Length; i++)
            {
                if (_spawnBuffer[i].SpawnableId.IsEmpty) continue;
                AutoDefenseRunResult spawn = runtime.ConsumeSpawnRequest(_spawnBuffer[i]);
                if (spawn.Succeeded) _session.SpawnedCount += spawn.Spawned;
                _spawnBuffer[i] = default;
            }

            _combat.Feedback.EmitSpawnFeedbackForNewEnemies();
            AutoDefenseRuntimeSnapshot beforeCombat = runtime.CreateSnapshot();
            _world.Targets.UpdateWeaponPresentationTargets(beforeCombat, deltaSeconds);
            _world.Targets.UpdateEnemyModelPresentations(beforeCombat, deltaSeconds);
            AutoDefenseRunResult result = runtime.Tick(ticks, deltaSeconds);
            _combat.Statistics.RecordDirectKills(result.Killed);
            int rewardedKills = result.Killed;
            _session.ObjectiveReachCount += result.ReachedObjective;
            if (result.ReachedObjective > 0)
            {
                _session.ObjectiveDamageEvents += result.ReachedObjective;
                _feedback.EmitObjectiveDamage(result.ReachedObjective);
            }
            AutoDefenseRuntimeSnapshot afterCombat = runtime.CreateSnapshot();
            _world.Targets.UpdateEnemyModelPresentations(afterCombat, deltaSeconds);
            _combat.Feedback.ObserveEnemyPressure(afterCombat);
            _combat.Feedback.EmitDirectWeaponPresentation(result.WeaponFireResult, beforeCombat, afterCombat);
            _combat.Feedback.EmitMissingKillFeedback(beforeCombat, afterCombat, result.Killed, null);

            _combat.Projectiles.LaunchFromWeaponResult(result.ProjectileLaunches, afterCombat);
            ProjectileTickResult projectileTick = _services.Projectiles.Tick(ticks);
            _services.ProjectileNavigation.Tick((float)(deltaSeconds * _build.ProjectileSpeedMultiplier));
            _combat.Projectiles.ObserveProjectileMotion();
            _combat.Projectiles.EmitProjectileExpiryFeedback(projectileTick);
            rewardedKills += _combat.Projectiles.ResolvePendingProjectileImpacts(ticks);
            rewardedKills += _combat.Cadence.ApplyDirectDamageBonusIfReady();
            rewardedKills += _combat.Cadence.FireManualTowerShotIfReady(ticks);
            rewardedKills += _combat.Cadence.FireUnlockedModulesIfReady(ticks);
            if (rewardedKills > 0)
            {
                long earned = _wallet.AwardKills(rewardedKills, _rewards.ConsumeKillCredits(), _build.RewardCreditMultiplierBonus);
                _feedback.EmitKillCredits(earned);
            }
            _wallet.GrantPassiveIncome(ticks, _content.Economy);
            _rewards.AwardExperienceForCompletedWaves(_services.Encounter.CreateSnapshot());
            ApplyTerminalReward();
            _feedback.Update(deltaSeconds);
            EvaluateTerminalState();
        }

        private bool PauseForDraft(float deltaSeconds)
        {
            if (!_rewards.RewardDraftActive || !_session.RewardDraftPausesCombat) return false;
            _feedback.Update(deltaSeconds);
            return true;
        }

        private void ApplyTerminalReward() => _persistent.ApplyEncounterRewardIfTerminal(
            _services.Runtime.State, _session.Sequence, _build.RewardCreditMultiplierBonus);

        private void EvaluateTerminalState()
        {
            IdleAutoDefenseRunProfileAsset profile = _content.RunProfile;
            if (profile == null) return;
            AutoDefenseRuntime runtime = _services.Runtime;
            if (runtime.State == AutoDefenseRuntimeState.Running &&
                profile.VictoryRule == IdleAutoDefenseVictoryRule.SurviveSessionDuration &&
                _session.ElapsedTicks >= profile.SessionLengthTicks)
            {
                _session.VictoryReached = true;
                runtime.Stop();
                ApplyTerminalReward();
            }
            if ((_session.VictoryReached || runtime.State == AutoDefenseRuntimeState.Completed) && profile.Endless)
                _session.EndlessRestartPending = true;
        }
    }
}
