using System;
using System.Collections.Generic;
using System.Threading;
using Deucarian.Attacks;
using Deucarian.AutoDefense;
using Deucarian.Combat;
using Deucarian.DefenseGames;
using Deucarian.Encounters;
using Deucarian.IdleProgression;
using Deucarian.Monetization;
using Deucarian.Persistence;
using Deucarian.Progression;
using Deucarian.Projectiles;
using Deucarian.RunUpgrades;
using Deucarian.WeaponSystems;
using Deucarian.WorldNavigation;
using Deucarian.WorldSpawning;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    public static class BasicIdleAutoDefenseGame
    {
        public static readonly DamageTypeId DamageType = new DamageTypeId("damage.template.basic");
        public static readonly AttackDefinitionId PulseAttackId = new AttackDefinitionId("attack.template.pulse-cannon");
        public static readonly AttackDefinitionId ShardAttackId = new AttackDefinitionId("attack.template.shard-launcher");
        public static readonly AttackDefinitionId AttackId = PulseAttackId;
        public static readonly ProjectileDefinitionId ShardProjectileId = new ProjectileDefinitionId("projectile.template.shard");
        public static readonly ProjectileDefinitionId ProjectileId = ShardProjectileId;
        public static readonly WorldSpawnableId SwarmEnemySpawnableId = new WorldSpawnableId("enemy.template.swarm");
        public static readonly WorldSpawnableId RunnerEnemySpawnableId = new WorldSpawnableId("enemy.template.runner");
        public static readonly WorldSpawnableId TankEnemySpawnableId = new WorldSpawnableId("enemy.template.tank");
        public static readonly WorldSpawnableId ShieldedEnemySpawnableId = new WorldSpawnableId("enemy.template.shielded");
        public static readonly WorldSpawnableId EliteEnemySpawnableId = new WorldSpawnableId("enemy.template.elite");
        public static readonly WorldSpawnableId BossEnemySpawnableId = new WorldSpawnableId("enemy.template.boss");
        public static readonly WorldSpawnableId EnemySpawnableId = SwarmEnemySpawnableId;
        public static readonly WorldSpawnableId ProjectileSpawnableId = new WorldSpawnableId("projectile.template.shard");
        public static readonly WeaponDefinitionId PulseCannonWeaponId = new WeaponDefinitionId("weapon.template.pulse-cannon");
        public static readonly WeaponDefinitionId ShardLauncherWeaponId = new WeaponDefinitionId("weapon.template.shard-launcher");
        public static readonly WeaponDefinitionId ArcEmitterWeaponId = new WeaponDefinitionId("weapon.template.arc-emitter");
        public static readonly WeaponDefinitionId OrbitalShotWeaponId = new WeaponDefinitionId("weapon.template.orbital-shot");
        public static readonly CurrencyId Credits = new CurrencyId("currency.template.credits");
        public static readonly CurrencyId Parts = new CurrencyId("currency.template.parts");
        public static readonly TrackId AccountXp = new TrackId("track.template.account");
        public static readonly UnlockId StarterUnlock = new UnlockId("unlock.template.starter");
        public static readonly UnlockId Stage2Unlock = new UnlockId("unlock.template.stage.pressure-ring");
        public static readonly UnlockId Stage3Unlock = new UnlockId("unlock.template.stage.boss-pulse");
        public static readonly UnlockId PulseCannonUnlock = new UnlockId("unlock.template.module.pulse-cannon");
        public static readonly UnlockId ShardLauncherUnlock = new UnlockId("unlock.template.module.shard-launcher");
        public static readonly ResearchNodeId CorePlatingResearch = new ResearchNodeId("research.template.core-plating");
        public static readonly ResearchNodeId PulseCapacitorResearch = new ResearchNodeId("research.template.pulse-capacitor");
        public static readonly ResearchNodeId ShardLoaderResearch = new ResearchNodeId("research.template.shard-loader");
        public static readonly ResearchNodeId OfflineRoutingResearch = new ResearchNodeId("research.template.offline-routing");

        public static AutoDefenseDefinition CreateDefinition()
        {
            var pulseMount = new AutoDefenseMountId("mount.template.pulse-cannon");
            var shardMount = new AutoDefenseMountId("mount.template.shard-launcher");
            return new AutoDefenseDefinition(
                new AutoDefenseObjectiveDefinition(new DefenseObjectiveId("template-core"), Vector3.zero, 42, DamageType, 0.45f, 4, 2),
                AutoDefenseSpawnRingDefinition.FourWay(7f),
                new[]
                {
                    Enemy(SwarmEnemySpawnableId, 7, 2.8f, 2, 0.25f),
                    Enemy(RunnerEnemySpawnableId, 6, 4.0f, 2, 0.24f),
                    Enemy(TankEnemySpawnableId, 24, 1.35f, 5, 0.42f),
                    Enemy(ShieldedEnemySpawnableId, 18, 1.8f, 4, 0.34f),
                    Enemy(EliteEnemySpawnableId, 34, 2.15f, 7, 0.36f),
                    Enemy(BossEnemySpawnableId, 96, 0.95f, 16, 0.65f)
                },
                new[]
                {
                    new AutoDefenseMountDefinition(pulseMount, new Vector3(-1.6f, 0f, 0f), new WeaponSlotId("slot.template.pulse-cannon"), PulseCannonWeaponId),
                    new AutoDefenseMountDefinition(shardMount, new Vector3(1.6f, 0f, 0f), new WeaponSlotId("slot.template.shard-launcher"), ShardLauncherWeaponId)
                },
                new[]
                {
                    new AutoDefenseWeaponModuleDefinition(pulseMount, new WeaponDefinition(PulseCannonWeaponId, WeaponFireMode.DirectAttack, PulseAttackId, 7), Source("pulse-cannon")),
                    new AutoDefenseWeaponModuleDefinition(shardMount, new WeaponDefinition(ShardLauncherWeaponId, WeaponFireMode.Projectile, ShardAttackId, 13, ShardProjectileId, burstCount: 1), Source("shard-launcher"))
                });
        }

        public static EncounterDefinition CreateEncounterDefinition()
        {
            return CreateFirstOrbitEncounterDefinition();
        }

        public static StageDefinition[] CreateStageDefinitions()
        {
            return new[]
            {
                new StageDefinition(new StageId("stage.template.first-orbit"), new EncounterId("encounter.template.first-orbit"), new[] { new RewardReferenceId("reward.template.first-orbit") }),
                new StageDefinition(new StageId("stage.template.pressure-ring"), new EncounterId("encounter.template.pressure-ring"), new[] { new RewardReferenceId("reward.template.pressure-ring") }),
                new StageDefinition(new StageId("stage.template.boss-pulse"), new EncounterId("encounter.template.boss-pulse"), new[] { new RewardReferenceId("reward.template.boss-pulse") }),
                new StageDefinition(new StageId("stage.template.endless-placeholder"), new EncounterId("encounter.template.endless-placeholder"), new[] { new RewardReferenceId("reward.template.endless-placeholder") })
            };
        }

        public static EncounterDefinition[] CreateEncounterDefinitions()
        {
            return new[]
            {
                CreateFirstOrbitEncounterDefinition(),
                CreatePressureRingEncounterDefinition(),
                CreateBossPulseEncounterDefinition(),
                CreateEndlessPlaceholderEncounterDefinition()
            };
        }

        public static EncounterDefinition CreateFirstOrbitEncounterDefinition()
        {
            var channels = new[]
            {
                "perimeter-north",
                "perimeter-east",
                "perimeter-south",
                "perimeter-west"
            };
            var groups = new List<SpawnGroupDefinition>();
            for (int i = 0; i < channels.Length; i++)
            {
                groups.Add(SpawnGroupDefinition.Fixed(
                    new SpawnGroupId("group.template.first-orbit.swarm." + channels[i]),
                    new SpawnableId(SwarmEnemySpawnableId.Value),
                    3,
                    1,
                    i * 12,
                    20,
                    new SpawnChannelId(channels[i])));
            }

            groups.Add(SpawnGroupDefinition.Fixed(new SpawnGroupId("group.template.first-orbit.runner-east"), new SpawnableId(RunnerEnemySpawnableId.Value), 2, 1, 42, 18, new SpawnChannelId("perimeter-east")));
            groups.Add(SpawnGroupDefinition.Fixed(new SpawnGroupId("group.template.first-orbit.tank-west"), new SpawnableId(TankEnemySpawnableId.Value), 1, 1, 78, 0, new SpawnChannelId("perimeter-west")));

            return new EncounterDefinition(
                new EncounterId("encounter.template.first-orbit"),
                null,
                new[]
                {
                    new WaveDefinition(new WaveId("wave.template.first-orbit.opening"), 0, groups.GetRange(0, 4)),
                    new WaveDefinition(new WaveId("wave.template.first-orbit.pressure"), 36, groups.GetRange(4, 2))
                },
                new[] { ObjectiveDefinition.AllWavesEmitted(new EncounterObjectiveId("all-waves-emitted")) },
                seed: 20260623);
        }

        public static EncounterDefinition CreatePressureRingEncounterDefinition()
        {
            return new EncounterDefinition(
                new EncounterId("encounter.template.pressure-ring"),
                null,
                new[]
                {
                    new WaveDefinition(new WaveId("wave.template.pressure-ring.runners"), 0, new[]
                    {
                        SpawnGroupDefinition.Fixed(new SpawnGroupId("group.template.pressure-ring.runner-north"), new SpawnableId(RunnerEnemySpawnableId.Value), 4, 1, 0, 12, new SpawnChannelId("perimeter-north")),
                        SpawnGroupDefinition.Fixed(new SpawnGroupId("group.template.pressure-ring.runner-south"), new SpawnableId(RunnerEnemySpawnableId.Value), 4, 1, 8, 12, new SpawnChannelId("perimeter-south"))
                    }),
                    new WaveDefinition(new WaveId("wave.template.pressure-ring.armor"), 48, new[]
                    {
                        SpawnGroupDefinition.Fixed(new SpawnGroupId("group.template.pressure-ring.shielded-east"), new SpawnableId(ShieldedEnemySpawnableId.Value), 3, 1, 0, 20, new SpawnChannelId("perimeter-east")),
                        SpawnGroupDefinition.Fixed(new SpawnGroupId("group.template.pressure-ring.tank-west"), new SpawnableId(TankEnemySpawnableId.Value), 2, 1, 18, 28, new SpawnChannelId("perimeter-west"))
                    }),
                    new WaveDefinition(new WaveId("wave.template.pressure-ring.elite"), 108, new[]
                    {
                        SpawnGroupDefinition.Fixed(new SpawnGroupId("group.template.pressure-ring.elite-north"), new SpawnableId(EliteEnemySpawnableId.Value), 1, 1, 0, 0, new SpawnChannelId("perimeter-north")),
                        SpawnGroupDefinition.Fixed(new SpawnGroupId("group.template.pressure-ring.swarm-all"), new SpawnableId(SwarmEnemySpawnableId.Value), 8, 2, 8, 18, new SpawnChannelId("perimeter-south"))
                    })
                },
                new[] { ObjectiveDefinition.AllWavesEmitted(new EncounterObjectiveId("all-waves-emitted")) },
                seed: 20260624);
        }

        public static EncounterDefinition CreateBossPulseEncounterDefinition()
        {
            return new EncounterDefinition(
                new EncounterId("encounter.template.boss-pulse"),
                null,
                new[]
                {
                    new WaveDefinition(new WaveId("wave.template.boss-pulse.breakers"), 0, new[]
                    {
                        SpawnGroupDefinition.Fixed(new SpawnGroupId("group.template.boss-pulse.runner-burst-north"), new SpawnableId(RunnerEnemySpawnableId.Value), 8, 4, 0, 8, new SpawnChannelId("perimeter-north")),
                        SpawnGroupDefinition.Fixed(new SpawnGroupId("group.template.boss-pulse.runner-burst-east"), new SpawnableId(RunnerEnemySpawnableId.Value), 8, 4, 0, 8, new SpawnChannelId("perimeter-east"))
                    }),
                    new WaveDefinition(new WaveId("wave.template.boss-pulse.guard"), 28, new[]
                    {
                        SpawnGroupDefinition.Fixed(new SpawnGroupId("group.template.boss-pulse.shielded-ring"), new SpawnableId(ShieldedEnemySpawnableId.Value), 4, 2, 0, 18, new SpawnChannelId("perimeter-west")),
                        SpawnGroupDefinition.Fixed(new SpawnGroupId("group.template.boss-pulse.tank-ring"), new SpawnableId(TankEnemySpawnableId.Value), 3, 1, 12, 24, new SpawnChannelId("perimeter-south"))
                    }),
                    new WaveDefinition(new WaveId("wave.template.boss-pulse.boss"), 80, new[]
                    {
                        SpawnGroupDefinition.Fixed(new SpawnGroupId("group.template.boss-pulse.elite"), new SpawnableId(EliteEnemySpawnableId.Value), 2, 1, 0, 18, new SpawnChannelId("perimeter-east")),
                        SpawnGroupDefinition.Fixed(new SpawnGroupId("group.template.boss-pulse.boss"), new SpawnableId(BossEnemySpawnableId.Value), 1, 1, 18, 0, new SpawnChannelId("perimeter-north"))
                    })
                },
                new[] { ObjectiveDefinition.AllWavesEmitted(new EncounterObjectiveId("all-waves-emitted")) },
                seed: 20260625);
        }

        public static EncounterDefinition CreateEndlessPlaceholderEncounterDefinition()
        {
            return new EncounterDefinition(
                new EncounterId("encounter.template.endless-placeholder"),
                null,
                new[]
                {
                    new WaveDefinition(new WaveId("wave.template.endless-placeholder.loop-seed"), 0, new[]
                    {
                        SpawnGroupDefinition.Fixed(new SpawnGroupId("group.template.endless-placeholder.swarm"), new SpawnableId(SwarmEnemySpawnableId.Value), 4, 1, 0, 16, new SpawnChannelId("perimeter-north")),
                        SpawnGroupDefinition.Fixed(new SpawnGroupId("group.template.endless-placeholder.runner"), new SpawnableId(RunnerEnemySpawnableId.Value), 2, 1, 24, 20, new SpawnChannelId("perimeter-east"))
                    })
                },
                new[] { ObjectiveDefinition.AllWavesEmitted(new EncounterObjectiveId("all-waves-emitted")) },
                seed: 20260626);
        }

        public static CombatCatalog CreateCombatCatalog()
        {
            return new CombatCatalog(new[] { new DamageTypeDefinition(DamageType) });
        }

        public static AttackRuntime CreateAttackRuntime(CombatCatalog catalog, AutoDefenseDefinition definition)
        {
            var runtime = new AttackRuntime(catalog, new[]
            {
                new AttackDefinition(PulseAttackId, 0, DamageType, 9),
                new AttackDefinition(ShardAttackId, 0, DamageType, 6)
            });
            for (int i = 0; i < definition.WeaponModules.Count; i++)
                runtime.RegisterSource(definition.WeaponModules[i].Source);
            return runtime;
        }

        public static WeaponRuntime CreateWeaponRuntime(AutoDefenseDefinition definition, AttackRuntime attacks)
        {
            var weapons = new List<WeaponDefinition>();
            for (int i = 0; i < definition.WeaponModules.Count; i++)
                weapons.Add(definition.WeaponModules[i].WeaponDefinition);
            return new WeaponRuntime(weapons, new AttackRuntimeWeaponAttackAdapter(attacks), new ProjectileLaunchWeaponAdapter());
        }

        public static ProjectileDefinition CreateProjectileDefinition()
        {
            return new ProjectileDefinition(ShardProjectileId, ProjectileSpawnableId, DamageType, 6, 120, 8.5f, 1);
        }

        public static RunUpgradeCatalog CreateRunUpgradeCatalog()
        {
            return new RunUpgradeCatalog(new[]
            {
                Upgrade("upgrade.template.damage-up", "template.direct.damage_bonus", PulseCannonWeaponId.Value, 1.5, RunUpgradeRarity.Common, 6, 5),
                Upgrade("upgrade.template.fire-rate-up", "template.weapon.fire_rate_intent", PulseCannonWeaponId.Value, 1, RunUpgradeRarity.Common, 5, 3),
                Upgrade("upgrade.template.projectile-count-up", "template.projectile.volley_intent", ShardLauncherWeaponId.Value, 1, RunUpgradeRarity.Uncommon, 3, 2),
                Upgrade("upgrade.template.projectile-speed-up", "template.projectile.speed_multiplier", ShardProjectileId.Value, 0.35, RunUpgradeRarity.Common, 5, 4),
                Upgrade("upgrade.template.objective-max-health-up", "template.objective.max_health", "objective.template-core", 6, RunUpgradeRarity.Uncommon, 3, 3),
                Upgrade("upgrade.template.objective-repair", "template.objective.heal", "objective.template-core", 5, RunUpgradeRarity.Common, 5, 4),
                Upgrade("upgrade.template.shield-restore-intent", "template.objective.shield_restore_intent", "objective.template-core", 4, RunUpgradeRarity.Uncommon, 2, 2),
                Upgrade("upgrade.template.enemy-reward-up", "template.reward.credits_multiplier", "reward.template.run", 0.15, RunUpgradeRarity.Uncommon, 3, 3),
                Upgrade("upgrade.template.offline-gain-up", "template.offline.credits_multiplier", "offline.template.credits", 0.10, RunUpgradeRarity.Common, 4, 3),
                Upgrade("upgrade.template.reroll-bonus", "template.reroll.bonus_intent", "monetization.template.reroll", 1, RunUpgradeRarity.Rare, 2, 1),
                Upgrade("upgrade.template.crit-chance-intent", "template.attack.crit_chance_intent", PulseCannonWeaponId.Value, 0.05, RunUpgradeRarity.Rare, 2, 2),
                Upgrade("upgrade.template.crit-damage-intent", "template.attack.crit_damage_intent", PulseCannonWeaponId.Value, 0.20, RunUpgradeRarity.Rare, 2, 2),
                Upgrade("upgrade.template.direct-specialization", "template.direct.damage_bonus", PulseCannonWeaponId.Value, 3, RunUpgradeRarity.Epic, 1, 1, new[] { new RunUpgradeId("upgrade.template.damage-up") }),
                Upgrade("upgrade.template.projectile-specialization", "template.projectile.speed_multiplier", ShardProjectileId.Value, 0.75, RunUpgradeRarity.Epic, 1, 1, new[] { new RunUpgradeId("upgrade.template.projectile-speed-up") })
            });
        }

        public static IdleProgressionDefinition CreateOfflineProgressionDefinition()
        {
            return new IdleProgressionDefinition(
                TimeSpan.FromHours(8),
                new[] { new IdleProductionRate(Credits, 0.35d) },
                new[] { new IdleCycleReward(Parts, new ProgressionAmount(1), TimeSpan.FromMinutes(4)) });
        }

        public static ProgressionCatalog CreateProgressionCatalog()
        {
            return new ProgressionCatalog(
                new[]
                {
                    new CurrencyDefinition(Credits, new ProgressionAmount(250_000)),
                    new CurrencyDefinition(Parts, new ProgressionAmount(25_000))
                },
                new[]
                {
                    new ProgressionTrackDefinition(AccountXp, 0, new[] { new ProgressionAmount(100), new ProgressionAmount(250), new ProgressionAmount(500), new ProgressionAmount(900) })
                },
                new[]
                {
                    new ResearchNodeDefinition(CorePlatingResearch, 3, new[] { Debit(Credits, 25), Debit(Credits, 75), Debit(Credits, 160) }),
                    new ResearchNodeDefinition(PulseCapacitorResearch, 2, new[] { Debit(Parts, 2), Debit(Parts, 5) }, requiredUnlocks: new[] { PulseCannonUnlock }),
                    new ResearchNodeDefinition(ShardLoaderResearch, 2, new[] { Debit(Parts, 2), Debit(Parts, 5) }, requiredUnlocks: new[] { ShardLauncherUnlock }),
                    new ResearchNodeDefinition(OfflineRoutingResearch, 2, new[] { Debit(Credits, 40), Debit(Credits, 120) }, new[] { new ResearchPrerequisite(CorePlatingResearch, 1) })
                });
        }

        public static RewardBundle CreateEncounterCompletionReward()
        {
            return new RewardBundle(
                new[]
                {
                    new CurrencyLine(Credits, new ProgressionAmount(60), true),
                    new CurrencyLine(Parts, new ProgressionAmount(3), true)
                },
                new[] { new XpGrant(AccountXp, new ProgressionAmount(35)) },
                new[] { StarterUnlock, Stage2Unlock, PulseCannonUnlock, ShardLauncherUnlock });
        }

        private static AutoDefenseEnemyDefinition Enemy(WorldSpawnableId id, double health, float speed, double contactDamage, float radius)
        {
            return new AutoDefenseEnemyDefinition(id, health, speed, contactDamage, DamageType, radius);
        }

        private static RunUpgradeDefinition Upgrade(
            string id,
            string effect,
            string target,
            double amount,
            RunUpgradeRarity rarity,
            int weight,
            int maxRank,
            IReadOnlyList<RunUpgradeId> prerequisites = null)
        {
            return new RunUpgradeDefinition(
                new RunUpgradeId(id),
                rarity,
                weight,
                maxRank,
                new[] { new RunUpgradeEffectDescriptor(new RunUpgradeEffectId(effect), new RunUpgradeTargetId(target), amount) },
                prerequisites);
        }

        private static CurrencyLine Debit(CurrencyId currencyId, long amount)
        {
            return new CurrencyLine(currencyId, new ProgressionAmount(amount), false);
        }

        private static AttackSourceSnapshot Source(string suffix)
        {
            return new AttackSourceSnapshot(new AttackSourceId("source.template." + suffix), new CombatantId("template-core"));
        }
    }

    public static class IdleAutoDefenseTemplateMonetization
    {
        private const string InterstitialCooldownGroup = "template.interstitial.global";

        public static readonly MonetizationPlacementId DoubleRunReward = new MonetizationPlacementId("template.rewarded.double-run-reward");
        public static readonly MonetizationPlacementId ReviveAfterFailure = new MonetizationPlacementId("template.rewarded.revive-after-failure");
        public static readonly MonetizationPlacementId RerollUpgradeDraft = new MonetizationPlacementId("template.rewarded.reroll-upgrade-draft");
        public static readonly MonetizationPlacementId DoubleOfflineReward = new MonetizationPlacementId("template.rewarded.double-offline-reward");
        public static readonly MonetizationPlacementId SmallCurrencyBonus = new MonetizationPlacementId("template.rewarded.small-currency-bonus");
        public static readonly MonetizationPlacementId InterstitialAfterRunCompletion = new MonetizationPlacementId("template.interstitial.after-run-completion");
        public static readonly MonetizationPlacementId InterstitialAfterRunFailure = new MonetizationPlacementId("template.interstitial.after-run-failure");

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

    public class IdleAutoDefenseTemplateController : MonoBehaviour
    {
        private readonly SpawnRequest[] _spawnBuffer = new SpawnRequest[16];
        private AutoDefenseRuntime _runtime;
        private EncounterRuntime _encounter;
        private ProjectileRuntime _projectiles;
        private WorldSpawnService _enemySpawning;
        private WorldSpawnService _projectileSpawning;
        private WorldNavigationService _navigation;
        private WorldNavigationService _projectileNavigation;
        private RunUpgradeCatalog _upgradeCatalog;
        private RunUpgradeState _upgradeState;
        private RunUpgradeDraft _currentDraft;
        private ProgressionCatalog _progressionCatalog;
        private ProgressionState _progressionState;
        private IdleProgressionDefinition _offlineDefinition;
        private bool _completionRewardApplied;
        private GameObject _enemyPrefab;
        private GameObject _projectilePrefab;
        private GameObject _root;
        private bool _terminalStateLogged;
        private MonetizationSession _monetizationSession;

        public AutoDefenseRuntime Runtime => _runtime;
        public MonetizationSession MonetizationSession
        {
            get => _monetizationSession ??= IdleAutoDefenseTemplateMonetization.CreateMockSession();
            set => _monetizationSession = value;
        }

        public string RuntimeStateName => RuntimeState.ToString();
        public int SpawnedCount { get; private set; }
        public int DirectOrCombatKillCount { get; private set; }
        public int ProjectileLaunchCount { get; private set; }
        public int ProjectileAdapterKillCount { get; private set; }
        public int ObjectiveReachCount { get; private set; }
        public int ObjectiveDamageEvents { get; private set; }
        public int DraftTickCount { get; private set; }
        public int SelectedUpgradeCount { get; private set; }
        public double DirectDamageBonus { get; private set; }
        public double ProjectileSpeedMultiplier { get; private set; } = 1d;
        public int EnemySpawnDelayTicks { get; private set; }
        public double RewardCreditMultiplierBonus { get; private set; }
        public double OfflineRewardMultiplierBonus { get; private set; }
        public int UnsupportedUpgradeIntentCount { get; private set; }
        public long OfflineRewardCredits { get; private set; }
        public long OfflineRewardParts { get; private set; }
        public long EncounterRewardCredits { get; private set; }
        public long EncounterRewardParts { get; private set; }
        public IdleProgressionResultCode LastOfflineRewardCode { get; private set; } = IdleProgressionResultCode.NoElapsedTime;
        public bool ReviveOfferAccepted { get; private set; }
        public bool EncounterCompleted => _runtime != null && _runtime.State == AutoDefenseRuntimeState.Completed;
        public bool EncounterFailed => _runtime != null && _runtime.State == AutoDefenseRuntimeState.Failed;
        public string StatusSummary => "State=" + RuntimeState +
            " Spawned=" + SpawnedCount +
            " Kills=" + (DirectOrCombatKillCount + ProjectileAdapterKillCount) +
            " Projectiles=" + ProjectileLaunchCount +
            " Upgrades=" + SelectedUpgradeCount +
            " ObjectiveHits=" + ObjectiveDamageEvents;

        private AutoDefenseRuntimeState RuntimeState => _runtime == null ? AutoDefenseRuntimeState.Created : _runtime.State;

        private void Awake()
        {
            Build();
        }

        private void Update()
        {
            Step(1, Time.deltaTime <= 0f ? 1f / 60f : Time.deltaTime);
        }

        public void Build()
        {
            Build(BasicIdleAutoDefenseGame.CreateEncounterDefinition());
        }

        public void Build(EncounterDefinition encounterDefinition)
        {
            if (_runtime != null) return;
            ResetRunStateCounters();
            AutoDefenseDefinition definition = BasicIdleAutoDefenseGame.CreateDefinition();
            CombatCatalog catalog = BasicIdleAutoDefenseGame.CreateCombatCatalog();
            AttackRuntime attacks = BasicIdleAutoDefenseGame.CreateAttackRuntime(catalog, definition);
            WeaponRuntime weapons = BasicIdleAutoDefenseGame.CreateWeaponRuntime(definition, attacks);

            _root = new GameObject("Basic Idle Auto Defense Runtime");
            CreatePrimitive("Central Objective - Template Core", PrimitiveType.Cube, definition.Objective.Position, new Vector3(1.1f, 0.6f, 1.1f), Color.cyan);
            CreateSpawnMarkers(definition.SpawnRing.Radius);
            for (int i = 0; i < definition.Mounts.Count; i++)
            {
                string mountName = definition.Mounts[i].Id.Value.Contains("pulse")
                    ? "Pulse Cannon Mount - Direct Single Target"
                    : "Shard Launcher Mount - Projectile";
                CreatePrimitive(mountName, PrimitiveType.Cube, definition.Objective.Position + definition.Mounts[i].LocalOffset, new Vector3(0.45f, 0.35f, 0.45f), Color.yellow);
            }
            CreatePrimitive("Enemy Placeholder Preview - Replace Me", PrimitiveType.Capsule, new Vector3(0f, 0f, -3.25f), new Vector3(0.45f, 0.9f, 0.45f), Color.red);

            _enemyPrefab = CreatePrefab("Template Idle Enemy Runtime Prefab", PrimitiveType.Capsule, Color.red);
            _projectilePrefab = CreatePrefab("Template Idle Projectile Runtime Prefab", PrimitiveType.Sphere, Color.magenta);

            var poseResolver = new AutoDefensePerimeterPoseResolver(definition.Objective, definition.SpawnRing);
            _enemySpawning = new WorldSpawnService(
                new SpawnableCatalog(CreateEnemySpawnables(definition, _enemyPrefab)),
                poseResolver,
                rootName: "TemplateIdleEnemies");
            _navigation = new WorldNavigationService();
            _encounter = new EncounterRuntime(encounterDefinition ?? BasicIdleAutoDefenseGame.CreateEncounterDefinition());
            _runtime = new AutoDefenseRuntime(definition, _enemySpawning, _navigation, weapons, catalog, _encounter, poses: poseResolver, candidateCapacity: 64);

            var projectilePoseResolver = new ChannelPoseResolver(new Dictionary<WorldSpawnChannelId, SpawnPose>
            {
                { new WorldSpawnChannelId("projectile-origin"), new SpawnPose(definition.Objective.Position, Quaternion.identity) }
            });
            _projectileSpawning = new WorldSpawnService(
                new SpawnableCatalog(new[] { new SpawnableDefinition(BasicIdleAutoDefenseGame.ProjectileSpawnableId, new GameObjectPrefabProvider(_projectilePrefab), 4, 32) }),
                projectilePoseResolver,
                rootName: "TemplateIdleProjectiles");
            _projectileNavigation = new WorldNavigationService();
            _projectiles = new ProjectileRuntime(
                catalog,
                new[] { BasicIdleAutoDefenseGame.CreateProjectileDefinition() },
                new WorldSpawnProjectileSpawner(_projectileSpawning, new WorldSpawnChannelId("projectile-origin")),
                new WorldNavigationProjectileNavigator(_projectileNavigation));
            _upgradeCatalog = BasicIdleAutoDefenseGame.CreateRunUpgradeCatalog();
            _upgradeState = new RunUpgradeState();
            _progressionCatalog ??= BasicIdleAutoDefenseGame.CreateProgressionCatalog();
            _progressionState ??= new ProgressionState();
            _offlineDefinition = BasicIdleAutoDefenseGame.CreateOfflineProgressionDefinition();

            _runtime.Start();
            Debug.Log("[Idle Auto Defense Template] Starter scene built. Open the Game view to watch the core, spawn markers, weapon mounts, and placeholder enemies.");
        }

        public void RestartRun()
        {
            RestartRun(BasicIdleAutoDefenseGame.CreateEncounterDefinition());
        }

        public void RestartRun(EncounterDefinition encounterDefinition)
        {
            DisposeRuntimeObjects();
            _runtime = null;
            _encounter = null;
            _projectiles = null;
            _navigation = null;
            _projectileNavigation = null;
            _upgradeCatalog = null;
            _upgradeState = null;
            _currentDraft = null;
            Build(encounterDefinition);
        }

        public IdleProgressionResult SimulateOfflineReward(DateTimeOffset lastSeenUtc, DateTimeOffset nowUtc)
        {
            if (_runtime == null) Build();
            IdleProgressionResult result = IdleProgressionCalculator.Calculate(lastSeenUtc, nowUtc, _offlineDefinition);
            LastOfflineRewardCode = result.Code;
            if (result.Reward.CurrencyLines.Count > 0)
            {
                _progressionState.ApplyReward(_progressionCatalog, new ProgressionOperationId("template.offline." + nowUtc.UtcTicks), result.Reward);
            }

            long bonusCredits = CalculateOfflineBonusCredits(result);
            if (bonusCredits > 0)
            {
                _progressionState.ApplyReward(
                    _progressionCatalog,
                    new ProgressionOperationId("template.offline.bonus." + nowUtc.UtcTicks),
                    new RewardBundle(new[] { new CurrencyLine(BasicIdleAutoDefenseGame.Credits, new ProgressionAmount(bonusCredits), true) }));
            }

            OfflineRewardCredits = _progressionState.GetBalance(BasicIdleAutoDefenseGame.Credits).Value;
            OfflineRewardParts = _progressionState.GetBalance(BasicIdleAutoDefenseGame.Parts).Value;
            return result;
        }

        public MonetizationAvailability ResolveMonetizationAvailability(
            MonetizationPlacementId placementId,
            MonetizationPlacementKind kind,
            DateTimeOffset nowUtc)
        {
            return MonetizationSession.GetAvailability(placementId, kind, CreateMonetizationContext(nowUtc));
        }

        public MonetizationResult OfferDoubleOfflineReward(RewardClaimId claimId, DateTimeOffset nowUtc)
        {
            MonetizationResult result = MonetizationSession.ShowRewarded(
                IdleAutoDefenseTemplateMonetization.DoubleOfflineReward,
                claimId,
                CreateMonetizationContext(nowUtc));
            if (result.Succeeded)
            {
                OfflineRewardCredits += OfflineRewardCredits;
                OfflineRewardParts += OfflineRewardParts;
            }

            return result;
        }

        public MonetizationResult OfferUpgradeDraftReroll(RewardClaimId claimId, DateTimeOffset nowUtc)
        {
            if (_runtime == null) Build();
            MonetizationResult result = MonetizationSession.ShowRewarded(
                IdleAutoDefenseTemplateMonetization.RerollUpgradeDraft,
                claimId,
                CreateMonetizationContext(nowUtc));
            if (result.Succeeded && _upgradeCatalog != null)
            {
                _currentDraft = RunUpgradeDraftService.Generate(
                    _upgradeCatalog,
                    _upgradeState,
                    new RunUpgradeDraftRequest(3, 20260623 + Math.Max(1, DraftTickCount)));
            }

            return result;
        }

        public MonetizationResult OfferReviveAfterFailure(RewardClaimId claimId, DateTimeOffset nowUtc)
        {
            MonetizationResult result = MonetizationSession.ShowRewarded(
                IdleAutoDefenseTemplateMonetization.ReviveAfterFailure,
                claimId,
                CreateMonetizationContext(nowUtc));
            if (result.Succeeded) ReviveOfferAccepted = true;
            return result;
        }

        public MonetizationResult OfferDoubleRunReward(RewardClaimId claimId, DateTimeOffset nowUtc)
        {
            MonetizationResult result = MonetizationSession.ShowRewarded(
                IdleAutoDefenseTemplateMonetization.DoubleRunReward,
                claimId,
                CreateMonetizationContext(nowUtc));
            if (result.Succeeded)
            {
                EncounterRewardCredits += EncounterRewardCredits;
                EncounterRewardParts += EncounterRewardParts;
            }

            return result;
        }

        public MonetizationResult OfferSmallCurrencyBonus(RewardClaimId claimId, DateTimeOffset nowUtc)
        {
            MonetizationResult result = MonetizationSession.ShowRewarded(
                IdleAutoDefenseTemplateMonetization.SmallCurrencyBonus,
                claimId,
                CreateMonetizationContext(nowUtc));
            if (result.Succeeded) EncounterRewardCredits += 5;
            return result;
        }

        public MonetizationResult TryShowTransitionInterstitial(bool afterFailure, DateTimeOffset nowUtc)
        {
            MonetizationPlacementId placementId = afterFailure
                ? IdleAutoDefenseTemplateMonetization.InterstitialAfterRunFailure
                : IdleAutoDefenseTemplateMonetization.InterstitialAfterRunCompletion;
            return MonetizationSession.ShowInterstitial(placementId, CreateMonetizationContext(nowUtc));
        }

        public void Step(int ticks, float deltaSeconds)
        {
            if (_runtime == null || _runtime.State != AutoDefenseRuntimeState.Running) return;
            DraftAndApplyUpgradeIfDue(ticks);
            _encounter.AdvanceTicks(EnemySpawnDelayTicks);
            _encounter.DrainSpawnRequests(_spawnBuffer);
            for (int i = 0; i < _spawnBuffer.Length; i++)
            {
                if (_spawnBuffer[i].SpawnableId.IsEmpty) continue;
                AutoDefenseRunResult spawn = _runtime.ConsumeSpawnRequest(_spawnBuffer[i]);
                if (spawn.Succeeded) SpawnedCount += spawn.Spawned;
                _spawnBuffer[i] = default;
            }

            AutoDefenseRunResult result = _runtime.Tick(ticks, deltaSeconds);
            DirectOrCombatKillCount += result.Killed;
            ObjectiveReachCount += result.ReachedObjective;
            if (result.ReachedObjective > 0) ObjectiveDamageEvents += result.ReachedObjective;

            for (int i = 0; i < result.ProjectileLaunches.Count; i++)
            {
                ProjectileLaunchResult launch = _projectiles.Launch(result.ProjectileLaunches[i]);
                if (!launch.Succeeded) continue;
                ProjectileLaunchCount++;
                TryApplySampleProjectileHit();
            }

            _projectiles.Tick(ticks);
            _projectileNavigation.Tick((float)(deltaSeconds * ProjectileSpeedMultiplier));
            ApplyDirectDamageBonusIfReady();
            ApplyEncounterRewardIfTerminal();
            LogTerminalStateIfNeeded();
        }

        private void DraftAndApplyUpgradeIfDue(int ticks)
        {
            if (_upgradeCatalog == null) return;
            DraftTickCount += ticks;
            if (DraftTickCount % 30 != 0) return;
            _currentDraft = RunUpgradeDraftService.Generate(_upgradeCatalog, _upgradeState, new RunUpgradeDraftRequest(3, 20260623, DraftTickCount / 30));
            if (_currentDraft.Choices.Count == 0) return;
            RunUpgradeSelectionResult selected = _upgradeState.Select(_upgradeCatalog, _currentDraft.Choices[0].Id);
            if (!selected.Succeeded) return;
            SelectedUpgradeCount++;
            ApplyUpgrade(_currentDraft.Choices[0]);
        }

        private void ApplyUpgrade(RunUpgradeDefinition upgrade)
        {
            for (int i = 0; i < upgrade.Effects.Count; i++)
            {
                RunUpgradeEffectDescriptor effect = upgrade.Effects[i];
                if (effect.EffectId.Value == "template.direct.damage_bonus") DirectDamageBonus += effect.Amount;
                else if (effect.EffectId.Value == "template.projectile.speed_multiplier") ProjectileSpeedMultiplier += effect.Amount;
                else if (effect.EffectId.Value == "template.objective.heal") _runtime.Objective.Health.Heal(effect.Amount);
                else if (effect.EffectId.Value == "template.objective.max_health") _runtime.Objective.Health.ChangeMaximumHealth(_runtime.Objective.Health.MaximumHealth + effect.Amount, MaximumChangePolicy.FillToMaximum);
                else if (effect.EffectId.Value == "template.enemy.spawn_delay_ticks") EnemySpawnDelayTicks += (int)effect.Amount;
                else if (effect.EffectId.Value == "template.reward.credits_multiplier") RewardCreditMultiplierBonus += effect.Amount;
                else if (effect.EffectId.Value == "template.offline.credits_multiplier") OfflineRewardMultiplierBonus += effect.Amount;
                else UnsupportedUpgradeIntentCount++;
            }
        }

        private void ApplyDirectDamageBonusIfReady()
        {
            if (DirectDamageBonus <= 0d) return;
            AutoDefenseRuntimeSnapshot snapshot = _runtime.CreateSnapshot();
            for (int i = 0; i < snapshot.Enemies.Count; i++)
            {
                AutoDefenseEnemySnapshot enemy = snapshot.Enemies[i];
                if (enemy.Lifecycle != AutoDefenseEnemyLifecycle.Active) continue;
                if (enemy.Health <= DirectDamageBonus && _runtime.TryKillEnemy(enemy.Id))
                {
                    DirectOrCombatKillCount++;
                    return;
                }
            }
        }

        private void TryApplySampleProjectileHit()
        {
            AutoDefenseRuntimeSnapshot snapshot = _runtime.CreateSnapshot();
            for (int i = 0; i < snapshot.Enemies.Count; i++)
            {
                AutoDefenseEnemySnapshot enemy = snapshot.Enemies[i];
                if (enemy.Lifecycle != AutoDefenseEnemyLifecycle.Active) continue;
                if (_runtime.TryKillEnemy(enemy.Id))
                {
                    ProjectileAdapterKillCount++;
                    return;
                }
            }
        }

        private void ApplyEncounterRewardIfTerminal()
        {
            if (_completionRewardApplied || _progressionState == null || _runtime.State == AutoDefenseRuntimeState.Running) return;
            ProgressionResult result = _progressionState.ApplyReward(_progressionCatalog, new ProgressionOperationId("template.encounter.terminal.1"), BasicIdleAutoDefenseGame.CreateEncounterCompletionReward());
            if (!result.Succeeded) return;
            long bonusCredits = (long)Math.Ceiling(60d * RewardCreditMultiplierBonus);
            if (bonusCredits > 0)
            {
                _progressionState.ApplyReward(
                    _progressionCatalog,
                    new ProgressionOperationId("template.encounter.terminal.1.reward-bonus"),
                    new RewardBundle(new[] { new CurrencyLine(BasicIdleAutoDefenseGame.Credits, new ProgressionAmount(bonusCredits), true) }));
            }

            _completionRewardApplied = true;
            EncounterRewardCredits = _progressionState.GetBalance(BasicIdleAutoDefenseGame.Credits).Value;
            EncounterRewardParts = _progressionState.GetBalance(BasicIdleAutoDefenseGame.Parts).Value;
        }

        private static SpawnableDefinition[] CreateEnemySpawnables(AutoDefenseDefinition definition, GameObject prefab)
        {
            var spawnables = new SpawnableDefinition[definition.Enemies.Count];
            for (int i = 0; i < definition.Enemies.Count; i++)
                spawnables[i] = new SpawnableDefinition(definition.Enemies[i].SpawnableId, new GameObjectPrefabProvider(prefab), 4, 64);
            return spawnables;
        }

        private long CalculateOfflineBonusCredits(IdleProgressionResult result)
        {
            if (OfflineRewardMultiplierBonus <= 0d || result == null || result.Reward == null) return 0;
            for (int i = 0; i < result.Reward.CurrencyLines.Count; i++)
            {
                CurrencyLine line = result.Reward.CurrencyLines[i];
                if (line.CurrencyId.Equals(BasicIdleAutoDefenseGame.Credits))
                    return (long)Math.Ceiling(line.Amount.Value * OfflineRewardMultiplierBonus);
            }

            return 0;
        }

        private GameObject CreatePrefab(string name, PrimitiveType primitiveType, Color color)
        {
            GameObject prefab = GameObject.CreatePrimitive(primitiveType);
            prefab.name = name;
            ApplyColor(prefab, color);
            prefab.SetActive(false);
            return prefab;
        }

        private GameObject CreatePrimitive(string name, PrimitiveType primitiveType, Vector3 position, Vector3 scale, Color color)
        {
            GameObject instance = GameObject.CreatePrimitive(primitiveType);
            instance.name = name;
            instance.transform.SetParent(_root.transform, false);
            instance.transform.position = position;
            instance.transform.localScale = scale;
            ApplyColor(instance, color);
            Collider collider = instance.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            return instance;
        }

        private void CreateSpawnMarkers(float radius)
        {
            CreatePrimitive("Spawn Marker - North Perimeter", PrimitiveType.Cylinder, new Vector3(0f, 0f, radius), new Vector3(0.45f, 0.08f, 0.45f), Color.green);
            CreatePrimitive("Spawn Marker - East Perimeter", PrimitiveType.Cylinder, new Vector3(radius, 0f, 0f), new Vector3(0.45f, 0.08f, 0.45f), Color.green);
            CreatePrimitive("Spawn Marker - South Perimeter", PrimitiveType.Cylinder, new Vector3(0f, 0f, -radius), new Vector3(0.45f, 0.08f, 0.45f), Color.green);
            CreatePrimitive("Spawn Marker - West Perimeter", PrimitiveType.Cylinder, new Vector3(-radius, 0f, 0f), new Vector3(0.45f, 0.08f, 0.45f), Color.green);
        }

        private void LogTerminalStateIfNeeded()
        {
            if (_terminalStateLogged || _runtime == null || _runtime.State == AutoDefenseRuntimeState.Running) return;
            _terminalStateLogged = true;
            Debug.Log("[Idle Auto Defense Template] Run ended. " + StatusSummary);
        }

        private MonetizationFlowContext CreateMonetizationContext(DateTimeOffset nowUtc)
        {
            bool inCombat = _runtime != null && _runtime.State == AutoDefenseRuntimeState.Running;
            int terminalRuns = EncounterCompleted || EncounterFailed ? 1 : 0;
            return new MonetizationFlowContext(nowUtc, inCombat, terminalRuns);
        }

        private static void ApplyColor(GameObject instance, Color color)
        {
            Renderer renderer = instance.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = new Material(Shader.Find("Standard")) { color = color };
        }

        private void OnDestroy()
        {
            DisposeRuntimeObjects();
        }

        private void ResetRunStateCounters()
        {
            SpawnedCount = 0;
            DirectOrCombatKillCount = 0;
            ProjectileLaunchCount = 0;
            ProjectileAdapterKillCount = 0;
            ObjectiveReachCount = 0;
            ObjectiveDamageEvents = 0;
            DraftTickCount = 0;
            SelectedUpgradeCount = 0;
            DirectDamageBonus = 0d;
            ProjectileSpeedMultiplier = 1d;
            EnemySpawnDelayTicks = 0;
            RewardCreditMultiplierBonus = 0d;
            OfflineRewardMultiplierBonus = 0d;
            UnsupportedUpgradeIntentCount = 0;
            EncounterRewardCredits = 0;
            EncounterRewardParts = 0;
            ReviveOfferAccepted = false;
            _completionRewardApplied = false;
            _terminalStateLogged = false;
        }

        private void DisposeRuntimeObjects()
        {
            _enemySpawning?.Dispose();
            _projectileSpawning?.Dispose();
            DestroyTemplateObject(_enemyPrefab);
            DestroyTemplateObject(_projectilePrefab);
            DestroyTemplateObject(_root);
            _enemySpawning = null;
            _projectileSpawning = null;
            _enemyPrefab = null;
            _projectilePrefab = null;
            _root = null;
        }

        private static void DestroyTemplateObject(UnityEngine.Object instance)
        {
            if (instance == null) return;
            if (Application.isPlaying) Destroy(instance);
            else DestroyImmediate(instance);
        }
    }

    public static class IdleAutoDefenseTemplateSaveProgressionComposition
    {
        private static readonly TrackId AccountXp = new TrackId("track.template.account");
        private static readonly UnlockId StarterUnlock = new UnlockId("unlock.template.starter");
        private static readonly DocumentId ProfileDocumentId = new DocumentId("idle-auto-defense-template-profile");
        private static readonly DocumentId RunDocumentId = new DocumentId("idle-auto-defense-template-run");
        private static readonly DocumentId SettingsDocumentId = new DocumentId("idle-auto-defense-template-settings");

        public static IdleAutoDefenseTemplateCompositionSmokeResult RunSmoke()
        {
            return RunSmokeAsync().GetAwaiter().GetResult();
        }

        private static async System.Threading.Tasks.Task<IdleAutoDefenseTemplateCompositionSmokeResult> RunSmokeAsync()
        {
            var storage = new InMemoryTextStorage();
            using var service = new PersistenceService(storage);
            ProgressionCatalog progressionCatalog = CreateProgressionCatalog();
            var progressionState = new ProgressionState();
            RunUpgradeCatalog upgradeCatalog = BasicIdleAutoDefenseGame.CreateRunUpgradeCatalog();

            DocumentDefinition<ProfileDto> profileDefinition = CreateProfileDefinition();
            DocumentDefinition<RunResumeDto> runDefinition = CreateRunDefinition();
            DocumentDefinition<SettingsDto> settingsDefinition = CreateSettingsDefinition();
            SaveSlotId slot = SaveSlotId.Default;

            DateTimeOffset lastSeen = DateTimeOffset.UnixEpoch;
            var profile = new ProfileDto
            {
                Credits = 10,
                Parts = 1,
                Experience = 0,
                HasStarterUnlock = false,
                LastSeenUtcTicks = lastSeen.UtcTicks
            };
            WriteResult profileSave = await service.SaveAsync(profileDefinition, profile, slot, CancellationToken.None);
            LoadResult<ProfileDto> profileLoad = await service.LoadAsync(profileDefinition, slot, CancellationToken.None);

            var upgradeState = new RunUpgradeState();
            upgradeState.Select(upgradeCatalog, new RunUpgradeId("upgrade.template.damage-up"));
            RunUpgradeSnapshot upgradeSnapshot = upgradeState.CreateSnapshot();
            var run = RunResumeDto.FromSnapshot("run.template.1", 42, upgradeSnapshot, lastSeen.UtcTicks);
            WriteResult runSave = await service.SaveAsync(runDefinition, run, slot, CancellationToken.None);
            LoadResult<RunResumeDto> runLoad = await service.LoadAsync(runDefinition, slot, CancellationToken.None);
            RunUpgradeState restoredUpgradeState = RunUpgradeState.FromSnapshot(runLoad.Document.ToSnapshot());

            var settings = new SettingsDto { AudioVolume = 0.8f, ReducedMotion = true };
            WriteResult settingsSave = await service.SaveAsync(settingsDefinition, settings, slot, CancellationToken.None);
            LoadResult<SettingsDto> settingsLoad = await service.LoadAsync(settingsDefinition, slot, CancellationToken.None);

            RewardBundle runReward = BasicIdleAutoDefenseGame.CreateEncounterCompletionReward();
            ProgressionResult runRewardResult = progressionState.ApplyReward(progressionCatalog, new ProgressionOperationId("template.run.complete.1"), runReward);

            IdleProgressionResult offline = IdleProgressionCalculator.Calculate(
                new DateTimeOffset(profileLoad.Document.LastSeenUtcTicks, TimeSpan.Zero),
                lastSeen.AddHours(1),
                BasicIdleAutoDefenseGame.CreateOfflineProgressionDefinition());
            ProgressionResult offlineRewardResult = progressionState.ApplyReward(progressionCatalog, new ProgressionOperationId("template.offline.1"), offline.Reward);

            LoadResult<ProfileDto> missingDefaults = await service.LoadAsync(profileDefinition, new SaveSlotId("empty"), CancellationToken.None);

            await service.SaveAsync(profileDefinition, new ProfileDto { Credits = 2, LastSeenUtcTicks = lastSeen.UtcTicks }, new SaveSlotId("recover"), CancellationToken.None);
            await service.SaveAsync(profileDefinition, new ProfileDto { Credits = 3, LastSeenUtcTicks = lastSeen.UtcTicks }, new SaveSlotId("recover"), CancellationToken.None);
            storage.Files[new DocumentLocation(ProfileDocumentId, new SaveSlotId("recover")).FileStem + ".json"] = "{ broken";
            LoadResult<ProfileDto> recovered = await service.LoadAsync(profileDefinition, new SaveSlotId("recover"), CancellationToken.None);

            LoadResult<ProfileDto> migrated = await LoadMigratedProfile(service, storage);

            return new IdleAutoDefenseTemplateCompositionSmokeResult
            {
                ProfileSavedAndLoaded = profileSave.Succeeded && profileLoad.Succeeded && profileLoad.Document.Credits == 10,
                RunSavedAndLoaded = runSave.Succeeded && runLoad.Succeeded && runLoad.Document.Tick == 42,
                SettingsSavedAndLoaded = settingsSave.Succeeded && settingsLoad.Succeeded && settingsLoad.Document.ReducedMotion,
                RunRewardApplied = runRewardResult.Succeeded &&
                    progressionState.GetBalance(BasicIdleAutoDefenseGame.Credits).Value >= 60 &&
                    progressionState.GetTrackTotal(AccountXp).Value == 35 &&
                    progressionState.IsUnlocked(StarterUnlock) &&
                    progressionState.IsUnlocked(BasicIdleAutoDefenseGame.Stage2Unlock),
                RunUpgradeSnapshotRestored = restoredUpgradeState.GetRank(new RunUpgradeId("upgrade.template.damage-up")) == 1,
                OfflineRewardCalculated = offline.Code == IdleProgressionResultCode.Success && offlineRewardResult.Succeeded,
                MissingSaveDefaulted = missingDefaults.Succeeded && missingDefaults.Outcome == LoadOutcome.CreatedDefault,
                CorruptedPrimaryRecovered = recovered.Succeeded && recovered.Outcome == LoadOutcome.RecoveredFromBackup && recovered.Document.Credits == 2,
                MigrationApplied = migrated.Succeeded && migrated.Outcome == LoadOutcome.Migrated && migrated.Document.Parts == 0,
                Credits = progressionState.GetBalance(BasicIdleAutoDefenseGame.Credits).Value,
                Parts = progressionState.GetBalance(BasicIdleAutoDefenseGame.Parts).Value,
                Experience = progressionState.GetTrackTotal(AccountXp).Value
            };
        }

        private static ProgressionCatalog CreateProgressionCatalog()
        {
            return BasicIdleAutoDefenseGame.CreateProgressionCatalog();
        }

        private static DocumentDefinition<ProfileDto> CreateProfileDefinition()
        {
            var migrations = new DocumentMigrationSet(new[]
            {
                new DelegateDocumentMigration(ProfileDocumentId, new SchemaVersion(1), new SchemaVersion(2), (payload, serializer) =>
                {
                    ProfileV1Dto legacy = serializer.Deserialize<ProfileV1Dto>(payload);
                    return serializer.Serialize(new ProfileDto
                    {
                        Credits = legacy.Credits,
                        Parts = 0,
                        Experience = 0,
                        HasStarterUnlock = false,
                        LastSeenUtcTicks = legacy.LastSeenUtcTicks
                    });
                })
            });
            return new DocumentDefinition<ProfileDto>(
                ProfileDocumentId,
                new SchemaVersion(2),
                () => new ProfileDto { LastSeenUtcTicks = DateTimeOffset.UnixEpoch.UtcTicks },
                new DelegateDocumentValidator<ProfileDto>(document => document.Credits >= 0 && document.Parts >= 0 && document.Experience >= 0
                    ? ValidationResult.Success()
                    : ValidationResult.Failure("Profile values cannot be negative.")),
                migrations);
        }

        private static DocumentDefinition<RunResumeDto> CreateRunDefinition()
        {
            return new DocumentDefinition<RunResumeDto>(
                RunDocumentId,
                new SchemaVersion(1),
                () => new RunResumeDto(),
                new DelegateDocumentValidator<RunResumeDto>(document => document.Tick >= 0 ? ValidationResult.Success() : ValidationResult.Failure("Tick cannot be negative.")));
        }

        private static DocumentDefinition<SettingsDto> CreateSettingsDefinition()
        {
            return new DocumentDefinition<SettingsDto>(
                SettingsDocumentId,
                new SchemaVersion(1),
                () => new SettingsDto { AudioVolume = 1f },
                new DelegateDocumentValidator<SettingsDto>(document => document.AudioVolume >= 0f && document.AudioVolume <= 1f
                    ? ValidationResult.Success()
                    : ValidationResult.Failure("Audio volume must be normalized.")));
        }

        private static async System.Threading.Tasks.Task<LoadResult<ProfileDto>> LoadMigratedProfile(PersistenceService service, InMemoryTextStorage storage)
        {
            var serializer = new NewtonsoftPersistenceSerializer();
            var legacyDefinition = new DocumentDefinition<ProfileV1Dto>(
                ProfileDocumentId,
                new SchemaVersion(1),
                () => new ProfileV1Dto());
            var legacy = new ProfileV1Dto { Credits = 7, LastSeenUtcTicks = DateTimeOffset.UnixEpoch.UtcTicks };
            storage.Files[new DocumentLocation(ProfileDocumentId, new SaveSlotId("migration")).FileStem + ".json"] =
                SaveEnvelopeCodec.Create(legacyDefinition, legacy, serializer, DateTimeOffset.UnixEpoch);
            return await service.LoadAsync(CreateProfileDefinition(), new SaveSlotId("migration"), CancellationToken.None);
        }

        public sealed class ProfileDto
        {
            public long Credits;
            public long Parts;
            public long Experience;
            public bool HasStarterUnlock;
            public long LastSeenUtcTicks;
        }

        public sealed class ProfileV1Dto
        {
            public long Credits;
            public long LastSeenUtcTicks;
        }

        public sealed class SettingsDto
        {
            public float AudioVolume;
            public bool ReducedMotion;
        }

        public sealed class RunResumeDto
        {
            public string RunId;
            public int Tick;
            public string[] UpgradeIds = Array.Empty<string>();
            public int[] UpgradeRanks = Array.Empty<int>();
            public string[] BanishedUpgradeIds = Array.Empty<string>();
            public long LastOfflineClaimUtcTicks;

            public static RunResumeDto FromSnapshot(string runId, int tick, RunUpgradeSnapshot snapshot, long lastOfflineClaimUtcTicks)
            {
                var ids = new string[snapshot.Ranks.Count];
                var ranks = new int[snapshot.Ranks.Count];
                for (int i = 0; i < snapshot.Ranks.Count; i++)
                {
                    ids[i] = snapshot.Ranks[i].Id.Value;
                    ranks[i] = snapshot.Ranks[i].Rank;
                }

                var banished = new string[snapshot.Banished.Count];
                for (int i = 0; i < banished.Length; i++) banished[i] = snapshot.Banished[i].Value;

                return new RunResumeDto
                {
                    RunId = runId,
                    Tick = tick,
                    UpgradeIds = ids,
                    UpgradeRanks = ranks,
                    BanishedUpgradeIds = banished,
                    LastOfflineClaimUtcTicks = lastOfflineClaimUtcTicks
                };
            }

            public RunUpgradeSnapshot ToSnapshot()
            {
                int count = Math.Min(UpgradeIds == null ? 0 : UpgradeIds.Length, UpgradeRanks == null ? 0 : UpgradeRanks.Length);
                var ranks = new RunUpgradeRankSnapshot[count];
                for (int i = 0; i < count; i++) ranks[i] = new RunUpgradeRankSnapshot(new RunUpgradeId(UpgradeIds[i]), UpgradeRanks[i]);
                var banished = new RunUpgradeId[BanishedUpgradeIds == null ? 0 : BanishedUpgradeIds.Length];
                for (int i = 0; i < banished.Length; i++) banished[i] = new RunUpgradeId(BanishedUpgradeIds[i]);
                return new RunUpgradeSnapshot(ranks, banished);
            }
        }
    }

    public sealed class IdleAutoDefenseTemplateCompositionSmokeResult
    {
        public bool ProfileSavedAndLoaded;
        public bool RunSavedAndLoaded;
        public bool SettingsSavedAndLoaded;
        public bool RunRewardApplied;
        public bool RunUpgradeSnapshotRestored;
        public bool OfflineRewardCalculated;
        public bool MissingSaveDefaulted;
        public bool CorruptedPrimaryRecovered;
        public bool MigrationApplied;
        public long Credits;
        public long Parts;
        public long Experience;

        public bool Succeeded => ProfileSavedAndLoaded &&
            RunSavedAndLoaded &&
            SettingsSavedAndLoaded &&
            RunRewardApplied &&
            RunUpgradeSnapshotRestored &&
            OfflineRewardCalculated &&
            MissingSaveDefaulted &&
            CorruptedPrimaryRecovered &&
            MigrationApplied;
    }
}
