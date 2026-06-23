using System;
using System.Collections.Generic;
using System.Threading;
using Deucarian.Attacks;
using Deucarian.AutoDefense;
using Deucarian.Combat;
using Deucarian.DefenseGames;
using Deucarian.Encounters;
using Deucarian.IdleProgression;
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
        public static readonly AttackDefinitionId AttackId = new AttackDefinitionId("attack.template.basic");
        public static readonly ProjectileDefinitionId ProjectileId = new ProjectileDefinitionId("projectile.template.basic");
        public static readonly WorldSpawnableId EnemySpawnableId = new WorldSpawnableId("enemy.template.basic");
        public static readonly WorldSpawnableId ProjectileSpawnableId = new WorldSpawnableId("projectile.template.basic");
        public static readonly CurrencyId Credits = new CurrencyId("currency.template.credits");
        public static readonly CurrencyId Parts = new CurrencyId("currency.template.parts");

        public static AutoDefenseDefinition CreateDefinition()
        {
            var directWeaponId = new WeaponDefinitionId("weapon.template.direct");
            var projectileWeaponId = new WeaponDefinitionId("weapon.template.projectile");
            var directMount = new AutoDefenseMountId("mount.template.direct");
            var projectileMount = new AutoDefenseMountId("mount.template.projectile");
            return new AutoDefenseDefinition(
                new AutoDefenseObjectiveDefinition(new DefenseObjectiveId("template-core"), Vector3.zero, 28, DamageType, 0.45f, 3, 3),
                AutoDefenseSpawnRingDefinition.FourWay(7f),
                new[] { new AutoDefenseEnemyDefinition(EnemySpawnableId, 8, 2.2f, 3, DamageType, 0.3f) },
                new[]
                {
                    new AutoDefenseMountDefinition(directMount, new Vector3(-1.4f, 0f, 0f), new WeaponSlotId("slot.template.direct"), directWeaponId),
                    new AutoDefenseMountDefinition(projectileMount, new Vector3(1.4f, 0f, 0f), new WeaponSlotId("slot.template.projectile"), projectileWeaponId)
                },
                new[]
                {
                    new AutoDefenseWeaponModuleDefinition(directMount, new WeaponDefinition(directWeaponId, WeaponFireMode.DirectAttack, AttackId, 15), Source("direct")),
                    new AutoDefenseWeaponModuleDefinition(projectileMount, new WeaponDefinition(projectileWeaponId, WeaponFireMode.Projectile, AttackId, 5, ProjectileId), Source("projectile"))
                });
        }

        public static EncounterDefinition CreateEncounterDefinition()
        {
            var channels = new[]
            {
                "perimeter-north",
                "perimeter-east",
                "perimeter-south",
                "perimeter-west"
            };
            var groups = new SpawnGroupDefinition[channels.Length];
            for (int i = 0; i < channels.Length; i++)
            {
                groups[i] = SpawnGroupDefinition.Fixed(
                    new SpawnGroupId("group.template." + channels[i]),
                    new SpawnableId(EnemySpawnableId.Value),
                    2,
                    1,
                    i * 12,
                    24,
                    new SpawnChannelId(channels[i]));
            }

            return new EncounterDefinition(
                new EncounterId("basic-idle-auto-defense-template"),
                null,
                new[] { new WaveDefinition(new WaveId("wave.template.basic"), 0, groups) },
                new[] { ObjectiveDefinition.AllWavesEmitted(new EncounterObjectiveId("all-waves-emitted")) },
                seed: 20260623);
        }

        public static CombatCatalog CreateCombatCatalog()
        {
            return new CombatCatalog(new[] { new DamageTypeDefinition(DamageType) });
        }

        public static AttackRuntime CreateAttackRuntime(CombatCatalog catalog, AutoDefenseDefinition definition)
        {
            var runtime = new AttackRuntime(catalog, new[] { new AttackDefinition(AttackId, 0, DamageType, 8) });
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
            return new ProjectileDefinition(ProjectileId, ProjectileSpawnableId, DamageType, 6, 120, 8f, 1);
        }

        public static RunUpgradeCatalog CreateRunUpgradeCatalog()
        {
            return new RunUpgradeCatalog(new[]
            {
                Upgrade("upgrade.template.direct.damage", "template.direct.damage_bonus", "weapon.template.direct", 1),
                Upgrade("upgrade.template.projectile.speed", "template.projectile.speed_multiplier", "projectile.template.basic", 0.5),
                Upgrade("upgrade.template.objective.repair", "template.objective.heal", "objective.template-core", 2),
                Upgrade("upgrade.template.enemy.pacing", "template.enemy.spawn_delay_ticks", "encounter.template.basic", 6)
            });
        }

        public static IdleProgressionDefinition CreateOfflineProgressionDefinition()
        {
            return new IdleProgressionDefinition(
                TimeSpan.FromHours(8),
                new[] { new IdleProductionRate(Credits, 0.25d) },
                new[] { new IdleCycleReward(Parts, new ProgressionAmount(1), TimeSpan.FromMinutes(5)) });
        }

        public static ProgressionCatalog CreateProgressionCatalog()
        {
            return new ProgressionCatalog(new[]
            {
                new CurrencyDefinition(Credits, new ProgressionAmount(100_000)),
                new CurrencyDefinition(Parts, new ProgressionAmount(10_000))
            });
        }

        public static RewardBundle CreateEncounterCompletionReward()
        {
            return new RewardBundle(new[]
            {
                new CurrencyLine(Credits, new ProgressionAmount(25), true),
                new CurrencyLine(Parts, new ProgressionAmount(1), true)
            });
        }

        private static RunUpgradeDefinition Upgrade(string id, string effect, string target, double amount)
        {
            return new RunUpgradeDefinition(
                new RunUpgradeId(id),
                RunUpgradeRarity.Common,
                1,
                3,
                new[] { new RunUpgradeEffectDescriptor(new RunUpgradeEffectId(effect), new RunUpgradeTargetId(target), amount) });
        }

        private static AttackSourceSnapshot Source(string suffix)
        {
            return new AttackSourceSnapshot(new AttackSourceId("source.template." + suffix), new CombatantId("template-core"));
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

        public AutoDefenseRuntime Runtime => _runtime;
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
        public long OfflineRewardCredits { get; private set; }
        public long OfflineRewardParts { get; private set; }
        public long EncounterRewardCredits { get; private set; }
        public long EncounterRewardParts { get; private set; }
        public IdleProgressionResultCode LastOfflineRewardCode { get; private set; } = IdleProgressionResultCode.NoElapsedTime;
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
            if (_runtime != null) return;
            AutoDefenseDefinition definition = BasicIdleAutoDefenseGame.CreateDefinition();
            CombatCatalog catalog = BasicIdleAutoDefenseGame.CreateCombatCatalog();
            AttackRuntime attacks = BasicIdleAutoDefenseGame.CreateAttackRuntime(catalog, definition);
            WeaponRuntime weapons = BasicIdleAutoDefenseGame.CreateWeaponRuntime(definition, attacks);

            _root = new GameObject("Basic Idle Auto Defense Runtime");
            CreatePrimitive("Central Objective - Template Core", PrimitiveType.Cube, definition.Objective.Position, new Vector3(1.1f, 0.6f, 1.1f), Color.cyan);
            CreateSpawnMarkers(definition.SpawnRing.Radius);
            for (int i = 0; i < definition.Mounts.Count; i++)
            {
                string mountName = definition.Mounts[i].Id.Value.Contains("direct")
                    ? "Direct Weapon Mount - Close Range"
                    : "Projectile Weapon Mount - Launcher";
                CreatePrimitive(mountName, PrimitiveType.Cube, definition.Objective.Position + definition.Mounts[i].LocalOffset, new Vector3(0.45f, 0.35f, 0.45f), Color.yellow);
            }
            CreatePrimitive("Enemy Placeholder Preview - Replace Me", PrimitiveType.Capsule, new Vector3(0f, 0f, -3.25f), new Vector3(0.45f, 0.9f, 0.45f), Color.red);

            _enemyPrefab = CreatePrefab("Template Idle Enemy Runtime Prefab", PrimitiveType.Capsule, Color.red);
            _projectilePrefab = CreatePrefab("Template Idle Projectile Runtime Prefab", PrimitiveType.Sphere, Color.magenta);

            var poseResolver = new AutoDefensePerimeterPoseResolver(definition.Objective, definition.SpawnRing);
            _enemySpawning = new WorldSpawnService(
                new SpawnableCatalog(new[] { new SpawnableDefinition(BasicIdleAutoDefenseGame.EnemySpawnableId, new GameObjectPrefabProvider(_enemyPrefab), 8, 32) }),
                poseResolver,
                rootName: "TemplateIdleEnemies");
            _navigation = new WorldNavigationService();
            _encounter = new EncounterRuntime(BasicIdleAutoDefenseGame.CreateEncounterDefinition());
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
            _progressionCatalog = BasicIdleAutoDefenseGame.CreateProgressionCatalog();
            _progressionState = new ProgressionState();
            _offlineDefinition = BasicIdleAutoDefenseGame.CreateOfflineProgressionDefinition();

            _runtime.Start();
            Debug.Log("[Idle Auto Defense Template] Starter scene built. Open the Game view to watch the core, spawn markers, weapon mounts, and placeholder enemies.");
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

            OfflineRewardCredits = _progressionState.GetBalance(BasicIdleAutoDefenseGame.Credits).Value;
            OfflineRewardParts = _progressionState.GetBalance(BasicIdleAutoDefenseGame.Parts).Value;
            return result;
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
                else if (effect.EffectId.Value == "template.enemy.spawn_delay_ticks") EnemySpawnDelayTicks += (int)effect.Amount;
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
            _completionRewardApplied = true;
            EncounterRewardCredits = _progressionState.GetBalance(BasicIdleAutoDefenseGame.Credits).Value;
            EncounterRewardParts = _progressionState.GetBalance(BasicIdleAutoDefenseGame.Parts).Value;
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

        private static void ApplyColor(GameObject instance, Color color)
        {
            Renderer renderer = instance.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = new Material(Shader.Find("Standard")) { color = color };
        }

        private void OnDestroy()
        {
            _enemySpawning?.Dispose();
            _projectileSpawning?.Dispose();
            if (_enemyPrefab != null) Destroy(_enemyPrefab);
            if (_projectilePrefab != null) Destroy(_projectilePrefab);
            if (_root != null) Destroy(_root);
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
            upgradeState.Select(upgradeCatalog, new RunUpgradeId("upgrade.template.direct.damage"));
            RunUpgradeSnapshot upgradeSnapshot = upgradeState.CreateSnapshot();
            var run = RunResumeDto.FromSnapshot("run.template.1", 42, upgradeSnapshot, lastSeen.UtcTicks);
            WriteResult runSave = await service.SaveAsync(runDefinition, run, slot, CancellationToken.None);
            LoadResult<RunResumeDto> runLoad = await service.LoadAsync(runDefinition, slot, CancellationToken.None);
            RunUpgradeState restoredUpgradeState = RunUpgradeState.FromSnapshot(runLoad.Document.ToSnapshot());

            var settings = new SettingsDto { AudioVolume = 0.8f, ReducedMotion = true };
            WriteResult settingsSave = await service.SaveAsync(settingsDefinition, settings, slot, CancellationToken.None);
            LoadResult<SettingsDto> settingsLoad = await service.LoadAsync(settingsDefinition, slot, CancellationToken.None);

            RewardBundle runReward = new RewardBundle(
                new[] { new CurrencyLine(BasicIdleAutoDefenseGame.Credits, new ProgressionAmount(25), true) },
                new[] { new XpGrant(AccountXp, new ProgressionAmount(10)) },
                new[] { StarterUnlock });
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
                    progressionState.GetBalance(BasicIdleAutoDefenseGame.Credits).Value >= 25 &&
                    progressionState.GetTrackTotal(AccountXp).Value == 10 &&
                    progressionState.IsUnlocked(StarterUnlock),
                RunUpgradeSnapshotRestored = restoredUpgradeState.GetRank(new RunUpgradeId("upgrade.template.direct.damage")) == 1,
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
            return new ProgressionCatalog(
                new[]
                {
                    new CurrencyDefinition(BasicIdleAutoDefenseGame.Credits, new ProgressionAmount(100_000)),
                    new CurrencyDefinition(BasicIdleAutoDefenseGame.Parts, new ProgressionAmount(10_000))
                },
                new[] { new ProgressionTrackDefinition(AccountXp, 0, new[] { new ProgressionAmount(100), new ProgressionAmount(250) }) });
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
