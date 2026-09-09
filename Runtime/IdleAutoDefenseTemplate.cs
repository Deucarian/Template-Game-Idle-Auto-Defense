using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Deucarian.Attacks;
using Deucarian.Attacks.Authoring;
using Deucarian.AutoDefense;
using Deucarian.Combat;
using Deucarian.Common;
using Deucarian.DefenseGames;
using Deucarian.Encounters;
using Deucarian.IdleProgression;
using Deucarian.Monetization;
using Deucarian.Persistence;
using Deucarian.Progression;
using Deucarian.Projectiles;
using Deucarian.RunUpgrades;
using Deucarian.RunUpgrades.Authoring;
using Deucarian.WeaponSystems;
using Deucarian.WeaponSystems.Authoring;
using Deucarian.WorldNavigation;
using Deucarian.WorldSpawning;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    public partial class IdleAutoDefenseTemplateController : MonoBehaviour
    {
        private void ReportTemplateWarning(string message) => Debug.LogWarning(message, this);

        private void ReportTemplateError(string message) => Debug.LogError(message, this);

        private IdleAutoDefenseRunTimeline _runSession;
        private IdleAutoDefenseRunTimeline RunSession => _runSession ??= new IdleAutoDefenseRunTimeline(AdvanceSimulationTick);
        private IdleAutoDefenseRunLoop _runLoop;
        private IdleAutoDefenseRunFeedback _runFeedback;
        private IdleAutoDefenseRunFeedback RunFeedback => _runFeedback ??= new IdleAutoDefenseRunFeedback(WorldPresentation, RuntimeUi);

        private const long DefaultRuntimeStartingCredits = 10;

        private const long KillRewardCredits = 5;

        private const int PassiveIncomeIntervalTicks = 60;

        private const float OverdriveDurationSeconds = 7f;

        private const float OverdriveCooldownSeconds = 18f;

        private const double OverdriveDamageMultiplier = 1.55d;

        private const string KenneyResourceRoot = "Kenney/IdleAutoDefense/";

        private const string Kenney3DResourceRoot = KenneyResourceRoot + "Models/TowerDefenseKit/FBX/";

        private IdleAutoDefensePersistentProgression _persistentProgression;
        private IdleAutoDefensePersistentProgression PersistentProgression => _persistentProgression ??= new IdleAutoDefensePersistentProgression(ApplyPersistentProgressionEffect);
        private IdleAutoDefenseRunWallet _runWallet;
        private IdleAutoDefenseRunWallet RunWallet => _runWallet ??= new IdleAutoDefenseRunWallet();
        private IdleAutoDefenseRunBuild _runBuild;
        private IdleAutoDefenseRunBuild RunBuild => _runBuild ??= new IdleAutoDefenseRunBuild(
            () => _activeGameRules, () => _activeEconomy, RunWallet,
            new IdleAutoDefenseBuildEffects(() => EncounterRunning, () => _runtime != null,
                (amount, policy) => _runtime.Objective.Health.ChangeMaximumHealth(_runtime.Objective.Health.MaximumHealth + amount, policy),
                amount => _runtime.Objective.Health.Heal(amount),
                (weapon, attack, enabled) => CreateWeaponPresentation(weapon, attack, enabled), EmitUpgradeFeedback));
        private IdleAutoDefenseRewardProgression _rewardProgression;
        private IdleAutoDefenseRewardProgression RewardProgression => _rewardProgression ??= new IdleAutoDefenseRewardProgression(
            RunBuild, () => RewardDraftSettings, () => RewardDraftCatalog, () => SurvivalSeconds, ResolveWeaponDisplayName,
            EmitRewardChoiceFeedback,
            position => RunFeedback.EmitCreditPickup(position),
            () => EmitUpgradeFeedback("Reward Ready", new Color(1f, 0.82f, 0.18f), 1.05f));
        private IdleAutoDefenseRuntimeUi _runtimeUi;

        private IdleAutoDefenseRuntimeUi RuntimeUi => _runtimeUi ??= new IdleAutoDefenseRuntimeUi(() => _root != null ? _root.transform : null);

        private IdleAutoDefenseContentNames _contentNames;
        private IdleAutoDefenseContentNames ContentNames => _contentNames ??= new IdleAutoDefenseContentNames(ContentBinding, () => _encounter);
        private IdleAutoDefenseRuntimeServices _runtimeServices;
        private AutoDefenseRuntime _runtime => _runtimeServices?.Runtime;

        private EncounterRuntime _encounter => _runtimeServices?.Encounter;

        private ProjectileRuntime _projectiles => _runtimeServices?.Projectiles;

        private WorldNavigationService _projectileNavigation => _runtimeServices?.ProjectileNavigation;

        private IdleAutoDefenseLegacyDraft _legacyDraft;
        private IdleAutoDefenseLegacyDraft LegacyDraft => _legacyDraft ??= new IdleAutoDefenseLegacyDraft(RunBuild);
        private IdleAutoDefenseRewardOffers _rewardOffers;
        private IdleAutoDefenseRewardOffers RewardOffers => _rewardOffers ??= new IdleAutoDefenseRewardOffers(
            PersistentProgression, LegacyDraft, () => _activeRunProfile, () => _activeEconomy,
            () => _activeOfflineProgression, () => EncounterRunning, () => EncounterCompleted || EncounterFailed);

        [SerializeField] private IdleAutoDefenseRewardDraftSettings _rewardDraftSettings = IdleAutoDefenseRewardDraftSettings.CreateDefault();

        [SerializeField] private IdleAutoDefenseRewardDraftCatalog _rewardDraftCatalog = IdleAutoDefenseRewardDraftCatalog.CreateDefault();

        [SerializeField] private bool _showDebugAimLines;

        [SerializeField] private bool _showDebugRanges;

        [SerializeField] private bool _showDebugSpawnRing;

        private ProjectileDefinition[] _resolvedProjectileDefinitions => _runtimeServices?.ProjectileDefinitions ?? Array.Empty<ProjectileDefinition>();

        private AutoDefenseRuntimeState RuntimeState => _runtime == null ? AutoDefenseRuntimeState.Created : _runtime.State;

        private IdleAutoDefenseRewardDraftSettings RewardDraftSettings => _rewardDraftSettings ??= IdleAutoDefenseRewardDraftSettings.CreateDefault();

        private IdleAutoDefenseRewardDraftCatalog RewardDraftCatalog => _rewardDraftCatalog ??= IdleAutoDefenseRewardDraftCatalog.CreateDefault();

        private string RuntimeObjectiveId => _activeGameRules == null || string.IsNullOrWhiteSpace(_activeGameRules.ObjectiveId)
            ? "objective.idle-auto-defense.core"
            : _activeGameRules.ObjectiveId;

        protected virtual void Awake()
        {
            Build();
        }

        protected UIDocument EnsureRuntimeUiDocument() => RuntimeUi.EnsureRuntimeUiDocument();

        protected VisualElement RuntimeUiRoot => RuntimeUi.RuntimeUiRoot;

        protected internal static void ApplyRuntimeUiFont(VisualElement element) => IdleAutoDefenseRuntimeUi.ApplyRuntimeUiFont(element);

        public void ConfigureRewardDraftSettings(IdleAutoDefenseRewardDraftSettings settings)
        {
            _rewardDraftSettings = settings ?? IdleAutoDefenseRewardDraftSettings.CreateDefault();
        }

        public void ConfigureRewardDraftCatalog(IdleAutoDefenseRewardDraftCatalog catalog)
        {
            _rewardDraftCatalog = catalog ?? IdleAutoDefenseRewardDraftCatalog.CreateDefault();
        }

        protected virtual void Update() => AdvanceFrame(Time.deltaTime);

        /// <summary>Advances the existing run clock explicitly for composed player experiences.</summary>

        public void AdvanceFrame(float deltaSeconds)
        {
            if (StartupBlocked) return;
            if (RunSession.ConsumeRestartRequest())
            {
                RestartRun();
                return;
            }
            RunSession.AdvanceFrame(deltaSeconds, _activeRunProfile);
        }

        private bool AdvanceSimulationTick(float secondsPerTick)
        {
            Step(1, secondsPerTick);
            return _runtime != null && _runtime.State == AutoDefenseRuntimeState.Running;
        }

        public void Build()
        {
            Build(null);
        }

        public void Build(EncounterDefinition encounterDefinition)
        {
            if (_runtime != null) return;
            ResetRunStateCounters();
            if (!TryUseAssignedContentSet())
            {
                if (_requireAuthoredContent)
                {
                    BlockStrictStartup();
                    return;
                }

                _resolvedAttackRecipes = ResolveAttackRecipes();
                _resolvedEnemyDefinitions = ResolveEnemyDefinitions();
                _resolvedWaveDefinitions = ResolveWaveDefinitions(_resolvedEnemyDefinitions);
                _resolvedWeaponDefinitions = ResolveWeaponDefinitions(_resolvedAttackRecipes);
                _resolvedUpgradeDefinitions = ResolveUpgradeDefinitions();
                BindExplicitFallbackCore();
            }

            try
            {
                var plan = new IdleAutoDefenseRuntimePlan(ContentBinding, ResolveActiveWeaponDefinitionsForRun());
                AutoDefenseDefinition definition = plan.Definition;
                WorldPresentation.Initialize(definition.Objective.Position, _resolvedEnemyDefinitions, () => EnsureRuntimeUiDocument());

                var projectilePoseResolver = new TemplateProjectileMuzzlePoseResolver(CombatRuntime.MuzzleQueries, new WorldSpawnChannelId("projectile-origin"));
                _runtimeServices = new IdleAutoDefenseRuntimeServices(plan, ContentBinding, WorldPresentation.Spawnables, projectilePoseResolver, encounterDefinition);
                LegacyDraft.Bind(_resolvedUpgradeDefinitions, UsingAssignedContentSet);
                PersistentProgression.Bind(_activeProgression, _activeEconomy, _activeOfflineProgression, _resolvedContentSet, UsingAssignedContentSet);
                ApplyContentSetEconomyTuning(_resolvedContentSet);
                RunWallet.SetStartingBalance(ResolveRuntimeStartingCredits(_resolvedContentSet));
                RunSession.BeginRun();
                ApplyAllPersistentProgressionEffects();

                _runLoop = new IdleAutoDefenseRunLoop(_runtimeServices, ContentBinding, RunSession, CombatRuntime,
                    WorldPresentation, RunFeedback, RunBuild, RunWallet, RewardProgression, PersistentProgression);
                _runtime.Start();
            }
            catch
            {
                DisposeRuntimeObjects();
                _runtimeServices = null;
                _legacyDraft?.Clear();
                throw;
            }
        }

        public void RestartRun()
        {
            RestartRun(null);
        }

        public void RestartRun(EncounterDefinition encounterDefinition)
        {
            DisposeRuntimeObjects();
            _runtimeServices = null;
            _legacyDraft?.Clear();
            Build(encounterDefinition);
        }

        public IdleProgressionResult SimulateOfflineReward(DateTimeOffset lastSeenUtc, DateTimeOffset nowUtc)
        {
            if (_runtime == null) Build();
            return PersistentProgression.SimulateOfflineReward(lastSeenUtc, nowUtc, OfflineRewardMultiplierBonus);
        }

        public MonetizationAvailability ResolveMonetizationAvailability(
            MonetizationPlacementId placementId,
            MonetizationPlacementKind kind,
            DateTimeOffset nowUtc) => RewardOffers.ResolveMonetizationAvailability(placementId, kind, nowUtc);

        public MonetizationResult OfferDoubleOfflineReward(RewardClaimId claimId, DateTimeOffset nowUtc) => RewardOffers.OfferDoubleOfflineReward(claimId, nowUtc);

        public MonetizationResult OfferUpgradeDraftReroll(RewardClaimId claimId, DateTimeOffset nowUtc)
        {
            if (_runtime == null) Build();
            return RewardOffers.OfferUpgradeDraftReroll(claimId, nowUtc);
        }

        public MonetizationResult OfferReviveAfterFailure(RewardClaimId claimId, DateTimeOffset nowUtc) => RewardOffers.OfferReviveAfterFailure(claimId, nowUtc);

        public MonetizationResult OfferDoubleRunReward(RewardClaimId claimId, DateTimeOffset nowUtc) => RewardOffers.OfferDoubleRunReward(claimId, nowUtc);

        public MonetizationResult OfferSmallCurrencyBonus(RewardClaimId claimId, DateTimeOffset nowUtc) => RewardOffers.OfferSmallCurrencyBonus(claimId, nowUtc);

        public MonetizationResult TryShowTransitionInterstitial(bool afterFailure, DateTimeOffset nowUtc) => RewardOffers.TryShowTransitionInterstitial(afterFailure, nowUtc);

        public void Step(int ticks, float deltaSeconds) => _runLoop?.Step(ticks, deltaSeconds);

        public bool TryPurchaseDamageUpgrade() => RunBuild.TryPurchaseDamageUpgrade();

        public bool TryPurchaseAttackSpeedUpgrade() => RunBuild.TryPurchaseAttackSpeedUpgrade();

        public bool TryPurchaseRangeUpgrade() => RunBuild.TryPurchaseRangeUpgrade();

        public bool TryPurchaseRepairUpgrade() => RunBuild.TryPurchaseRepairUpgrade();

        public bool TryPurchasePulseBeamModule() => RunBuild.TryPurchasePulseBeamModule();

        public bool TryPurchaseArcBurstModule() => RunBuild.TryPurchaseArcBurstModule();

        public bool TryPurchaseHomingPulseModule() => RunBuild.TryPurchaseHomingPulseModule();

        public bool TryPurchaseOverdrive() => RunBuild.TryPurchaseOverdrive();

        public int GetPersistentResearchRank(string nodeId) => PersistentProgression.GetPersistentResearchRank(nodeId);

        public long GetPersistentCurrencyBalance(string currencyId) => PersistentProgression.GetPersistentCurrencyBalance(currencyId);

        public IdleAutoDefensePersistentProgressionData CapturePersistentProgression() => PersistentProgression.CapturePersistentProgression();

        public bool RestorePersistentProgression(IdleAutoDefensePersistentProgressionData data) => PersistentProgression.RestorePersistentProgression(data);

        public void ResetPersistentProgression() => PersistentProgression.ResetPersistentProgression();

        public bool TryPurchasePersistentUpgrade(string nodeId) => PersistentProgression.TryPurchasePersistentUpgrade(nodeId);

        private void ApplyPersistentProgressionEffect(IdleAutoDefenseResearchNodeRecord node) => RunBuild.ApplyPersistentProgressionEffect(node);

        private void ApplyAllPersistentProgressionEffects() => PersistentProgression.ApplyAllPersistentProgressionEffects();

        private void ApplyUpgrade(RunUpgradeDefinition upgrade) => RunBuild.ApplyUpgrade(upgrade);

        private IdleAutoDefenseModuleRule ResolveModuleRule(IdleAutoDefenseModuleRole role) => RunBuild.ResolveModuleRule(role);

        private bool UnlockPulseBeamModule() => RunBuild.UnlockPulseBeamModule();

        private bool UnlockArcBurstModule() => RunBuild.UnlockArcBurstModule();

        private bool UnlockHomingPulseModule() => RunBuild.UnlockHomingPulseModule();

        public bool TryChooseRewardDraftChoice(int choiceIndex) => RewardProgression.TryChooseRewardDraftChoice(choiceIndex);

        public bool TryChooseRewardDraftHotkey(int hotkey) => RewardProgression.TryChooseRewardDraftHotkey(hotkey);

        public void RequestRewardDraft(IdleAutoDefenseRewardDraftKind kind) => RewardProgression.RequestRewardDraft(kind);

        private bool TryUnlockWeapon(string weaponId) => RunBuild.TryUnlockWeapon(weaponId);

        private bool IsWeaponUnlocked(string weaponId) => RunBuild.IsWeaponUnlocked(weaponId);

        private void RecordEnemyDefeatedForRewards(AutoDefenseEnemySnapshot enemy)
        {
            EnemyDefinitionAsset definition = FindEnemyDefinitionForPresentation(enemy.SpawnableId);
            long credits = definition == null || definition.Stats == null ? KillRewardCredits : Math.Max(0, definition.Stats.RewardValue);
            RewardProgression.RecordEnemyDefeated(enemy.Id, credits, IsBossEnemy(enemy), IsEliteEnemy(enemy), CreateEnemyAimPosition(enemy.Position));
        }

        private void QueueOrOpenRewardDraft(IdleAutoDefenseRewardDraftKind kind) => RewardProgression.QueueOrOpenRewardDraft(kind);

        private string ResolveWeaponDisplayName(string weaponId) => ContentNames.ResolveWeaponDisplayName(weaponId);

        private void EmitDamageNumber(Vector3 position, double amount, Color color, string prefix) => RuntimeUi.EmitDamageNumber(position, amount, color, prefix);

        private void EmitRewardChoiceFeedback(IdleAutoDefenseRewardDraftChoice choice) => RunFeedback.EmitRewardChoiceFeedback(choice);

        private void EmitUpgradeFeedback(string text, Color color, float scale) => RunFeedback.EmitUpgradeFeedback(text, color, scale);

        private void EmitFloatingStatusText(Vector3 position, string text, Color color) => RuntimeUi.EmitFloatingStatusText(position, text, color);

        private void UpdateDamageNumbers(float deltaSeconds) => _runtimeUi?.UpdateDamageNumbers(deltaSeconds);

        private Vector2 ResolveRuntimePanelSize() => RuntimeUi.ResolveRuntimePanelSize();

        private static int CalculateUpgradeCost(int baseCost, int rank) => IdleAutoDefenseRunBuild.CalculateUpgradeCost(baseCost, rank);

        private int ResolveUpgradeCost(string costId, int rank, int fallbackBaseCost = 0) => RunBuild.ResolveUpgradeCost(costId, rank, fallbackBaseCost);

        private int ResolveModuleBuildCost(IdleAutoDefenseModuleRole role, int fallbackCost) => RunBuild.ResolveModuleBuildCost(role, fallbackCost);

        private long ResolveRuntimeStartingCredits(GameContentSetResolution resolution)
        {
            if (resolution != null && resolution.IsValid && resolution.ContentSet != null && resolution.ContentSet.Economy != null)
                return Math.Max(0L, resolution.ContentSet.Economy.StartingCredits);
            if (_activeEconomy != null)
                return Math.Max(0L, _activeEconomy.StartingCredits);
            return DefaultRuntimeStartingCredits;
        }

        private string ResolveCurrentSpawnProfileName() => ContentNames.ResolveCurrentSpawnProfileName();

        private int ResolveCurrentWaveNumber() => ContentNames.ResolveCurrentWaveNumber();

        public int GetRewardDraftChoiceCurrentRank(IdleAutoDefenseRewardDraftChoice choice) => RewardProgression.GetRewardDraftChoiceCurrentRank(choice);

        private string ResolveWaveDisplayName(string waveId) => ContentNames.ResolveWaveDisplayName(waveId);

        private void ApplyContentSetEconomyTuning(GameContentSetResolution resolution)
        {
            if (resolution == null || !resolution.IsValid) return;
            RunBuild.SetInitialRewardMultiplier(Math.Max(0d, (_activeRunProfile == null ? 1f : _activeRunProfile.RewardMultiplier) - 1f));
        }

        protected virtual void OnDestroy()
        {
            DisposeRuntimeObjects(!Application.isPlaying);
        }

        protected virtual void OnApplicationQuit()
        {
            ClearSpawnedRuntimeObjects();
        }

        private void ResetRunStateCounters()
        {
            _runSession?.Reset();
            _combatRuntime?.Reset();
            _rewardProgression?.Reset();
            _runBuild?.Reset();
            _presentationCounters.Reset();
            _runtimeUi?.ResetFeedback();
            _contentBinding?.ResetDiagnostics();
            _legacyDraft?.ResetCounters();
            _runWallet?.Reset();
            _persistentProgression?.ResetRunRewards();
            _rewardOffers?.ResetRun();
            _worldPresentation?.ResetTransientFeedback();
            ClearDamageNumbers();
        }

        private void ClearSpawnedRuntimeObjects() => _runtimeServices?.ClearSpawnedObjects();

        private void DisposeRuntimeObjects(bool destroySceneObjects = true)
        {
            _runLoop = null;
            _runFeedback = null;
            _combatRuntime?.Release();
            _rewardProgression?.Release();
            _runBuild?.ClearOverdrive();
            _runtimeUi?.Release(destroySceneObjects);
            _runtimeUi = null;
            _runtimeServices?.Dispose(destroySceneObjects);
            DisposeWorldPresentation(destroySceneObjects);
            _contentNames = null;
            DisposeContentBinding();
        }

        private void ClearDamageNumbers() => _runtimeUi?.ClearDamageNumbers();

    }
}
