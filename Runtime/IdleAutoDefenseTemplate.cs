using System;
using System.Collections.Generic;
using System.Threading;
using Deucarian.Attacks;
using Deucarian.Attacks.Authoring;
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
        public static readonly DamageTypeId FireDamageType = new DamageTypeId("damage.template.fire");
        public static readonly DamageTypeId ArcDamageType = new DamageTypeId("damage.template.arc");
        public static readonly AttackDefinitionId HitscanAttackId = new AttackDefinitionId("attack.template.hitscan-beam");
        public static readonly AttackDefinitionId FireOrbAttackId = new AttackDefinitionId("attack.template.fire-orb");
        public static readonly AttackDefinitionId HomingPulseAttackId = new AttackDefinitionId("attack.template.homing-pulse");
        public static readonly AttackDefinitionId AttackId = FireOrbAttackId;
        public static readonly ProjectileDefinitionId FireOrbProjectileId = new ProjectileDefinitionId("projectile.template.fire-orb");
        public static readonly ProjectileDefinitionId HomingPulseProjectileId = new ProjectileDefinitionId("projectile.template.homing-pulse");
        public static readonly ProjectileDefinitionId ProjectileId = FireOrbProjectileId;
        public static readonly WorldSpawnableId EnemySpawnableId = new WorldSpawnableId("enemy.template.basic");
        public static readonly WorldSpawnableId FireOrbProjectileSpawnableId = new WorldSpawnableId("projectile.template.fire-orb");
        public static readonly WorldSpawnableId HomingPulseProjectileSpawnableId = new WorldSpawnableId("projectile.template.homing-pulse");
        public static readonly WorldSpawnableId ProjectileSpawnableId = FireOrbProjectileSpawnableId;
        public static readonly CurrencyId Credits = new CurrencyId("currency.template.credits");
        public static readonly CurrencyId Parts = new CurrencyId("currency.template.parts");

        public static AutoDefenseDefinition CreateDefinition()
        {
            var directWeaponId = new WeaponDefinitionId("weapon.template.direct");
            var projectileWeaponId = new WeaponDefinitionId("weapon.template.projectile");
            var homingWeaponId = new WeaponDefinitionId("weapon.template.homing-pulse");
            var directMount = new AutoDefenseMountId("mount.template.direct");
            var projectileMount = new AutoDefenseMountId("mount.template.projectile");
            var homingMount = new AutoDefenseMountId("mount.template.homing-pulse");
            return new AutoDefenseDefinition(
                new AutoDefenseObjectiveDefinition(new DefenseObjectiveId("template-core"), Vector3.zero, 28, DamageType, 0.45f, 3, 3),
                AutoDefenseSpawnRingDefinition.FourWay(7f),
                new[] { new AutoDefenseEnemyDefinition(EnemySpawnableId, 8, 2.2f, 3, DamageType, 0.3f) },
                new[]
                {
                    new AutoDefenseMountDefinition(directMount, new Vector3(-1.7f, 0f, 0f), new WeaponSlotId("slot.template.direct"), directWeaponId),
                    new AutoDefenseMountDefinition(projectileMount, new Vector3(0f, 0f, 1.35f), new WeaponSlotId("slot.template.projectile"), projectileWeaponId),
                    new AutoDefenseMountDefinition(homingMount, new Vector3(1.7f, 0f, 0f), new WeaponSlotId("slot.template.homing-pulse"), homingWeaponId)
                },
                new[]
                {
                    new AutoDefenseWeaponModuleDefinition(directMount, new WeaponDefinition(directWeaponId, WeaponFireMode.DirectAttack, HitscanAttackId, 12), Source("direct")),
                    new AutoDefenseWeaponModuleDefinition(projectileMount, new WeaponDefinition(projectileWeaponId, WeaponFireMode.Projectile, FireOrbAttackId, 5, FireOrbProjectileId), Source("projectile")),
                    new AutoDefenseWeaponModuleDefinition(homingMount, new WeaponDefinition(homingWeaponId, WeaponFireMode.Projectile, HomingPulseAttackId, 18, HomingPulseProjectileId), Source("homing-pulse"))
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

        public static CombatCatalog CreateCombatCatalog(IReadOnlyList<AttackDefinitionAsset> attackRecipes = null)
        {
            attackRecipes = attackRecipes ?? CreateAttackRecipes();
            var damageTypes = new List<DamageTypeDefinition>();
            var damageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddDamageType(damageTypes, damageIds, DamageType);
            for (int i = 0; i < attackRecipes.Count; i++)
            {
                AttackDefinitionAsset recipe = attackRecipes[i];
                if (recipe == null || recipe.Mechanics == null || string.IsNullOrWhiteSpace(recipe.Mechanics.DamageTypeId)) continue;
                AddDamageType(damageTypes, damageIds, new DamageTypeId(recipe.Mechanics.DamageTypeId));
            }

            var statuses = new List<StatusEffectDefinition>();
            var statusIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < attackRecipes.Count; i++)
            {
                AttackDefinitionAsset recipe = attackRecipes[i];
                if (recipe == null) continue;
                StatusEffectDefinition[] definitions = recipe.CreateStatusDefinitions();
                for (int j = 0; j < definitions.Length; j++)
                {
                    if (definitions[j] != null && statusIds.Add(definitions[j].Id.Value))
                        statuses.Add(definitions[j]);
                }
            }

            return new CombatCatalog(damageTypes, statuses);
        }

        public static AttackRuntime CreateAttackRuntime(CombatCatalog catalog, AutoDefenseDefinition definition, IReadOnlyList<AttackDefinitionAsset> attackRecipes = null)
        {
            AttackDefinition[] attacks = CreateAttackDefinitions(attackRecipes ?? CreateAttackRecipes());
            var runtime = new AttackRuntime(catalog, attacks);
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
            return CreateProjectileDefinitions()[0];
        }

        public static ProjectileDefinition[] CreateProjectileDefinitions(IReadOnlyList<AttackDefinitionAsset> attackRecipes = null)
        {
            attackRecipes = attackRecipes ?? CreateAttackRecipes();
            var definitions = new List<ProjectileDefinition>();
            for (int i = 0; i < attackRecipes.Count; i++)
            {
                AttackDefinitionAsset recipe = attackRecipes[i];
                if (recipe == null || recipe.Delivery == null || recipe.Mechanics == null) continue;
                if (recipe.Delivery.Mode != AttackRecipeDeliveryMode.Projectile) continue;
                definitions.Add(new ProjectileDefinition(
                    new ProjectileDefinitionId(recipe.Delivery.ProjectileDefinitionId),
                    new WorldSpawnableId(recipe.Delivery.ProjectileSpawnableId),
                    new DamageTypeId(recipe.Mechanics.DamageTypeId),
                    recipe.Mechanics.DamageAmount,
                    recipe.Delivery.ProjectileLifetimeTicks,
                    recipe.Delivery.ProjectileSpeed,
                    recipe.Delivery.MaxImpacts));
            }

            return definitions.ToArray();
        }

        public static AttackDefinitionAsset[] CreateAttackRecipes()
        {
            return new[]
            {
                AttackDefinitionAsset.CreateTransient(
                    HitscanAttackId.Value,
                    "Template Beam",
                    AttackRecipeDeliveryMode.Hitscan,
                    DamageType.Value,
                    8,
                    0,
                    6,
                    AttackRecipeTargetingMode.Nearest),
                AttackDefinitionAsset.CreateTransient(
                    FireOrbAttackId.Value,
                    "Template Fire Orb",
                    AttackRecipeDeliveryMode.Projectile,
                    FireDamageType.Value,
                    10,
                    0,
                    7,
                    AttackRecipeTargetingMode.Strongest,
                    projectileDefinitionId: FireOrbProjectileId.Value,
                    projectileSpawnableId: FireOrbProjectileSpawnableId.Value,
                    projectileSpeed: 8f,
                    projectileLifetimeTicks: 120,
                    pierceCount: 0),
                AttackDefinitionAsset.CreateTransient(
                    HomingPulseAttackId.Value,
                    "Template Homing Pulse",
                    AttackRecipeDeliveryMode.Projectile,
                    ArcDamageType.Value,
                    7,
                    0,
                    8,
                    AttackRecipeTargetingMode.LowestHealth,
                    new[] { new AttackStatusEffectRecipe("status.template.slow", 90, 30, 0.5f, effectNote: "Placeholder slow/status hook.") },
                    HomingPulseProjectileId.Value,
                    HomingPulseProjectileSpawnableId.Value,
                    projectileSpeed: 6.5f,
                    projectileLifetimeTicks: 150,
                    homing: true,
                    pierceCount: 1)
            };
        }

        public static AttackDefinition[] CreateAttackDefinitions(IReadOnlyList<AttackDefinitionAsset> attackRecipes)
        {
            if (attackRecipes == null || attackRecipes.Count == 0) throw new ArgumentException("At least one attack recipe is required.", nameof(attackRecipes));
            var definitions = new AttackDefinition[attackRecipes.Count];
            for (int i = 0; i < attackRecipes.Count; i++)
            {
                if (attackRecipes[i] == null) throw new ArgumentException("Attack recipe cannot be null.", nameof(attackRecipes));
                definitions[i] = attackRecipes[i].ToRuntimeDefinition();
            }

            return definitions;
        }

        public static RunUpgradeCatalog CreateRunUpgradeCatalog()
        {
            return new RunUpgradeCatalog(new[]
            {
                Upgrade("upgrade.template.direct.damage", "template.direct.damage_bonus", "weapon.template.direct", 1),
                Upgrade("upgrade.template.projectile.speed", "template.projectile.speed_multiplier", FireOrbProjectileId.Value, 0.5),
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

        private static void AddDamageType(List<DamageTypeDefinition> damageTypes, HashSet<string> seen, DamageTypeId id)
        {
            if (!id.IsEmpty && seen.Add(id.Value)) damageTypes.Add(new DamageTypeDefinition(id));
        }
    }

    public class IdleAutoDefenseTemplateController : MonoBehaviour
    {
        private readonly SpawnRequest[] _spawnBuffer = new SpawnRequest[16];
        [SerializeField] private AttackDefinitionAsset[] _attackRecipes;
        private AttackDefinitionAsset[] _resolvedAttackRecipes;
        private readonly Dictionary<string, AttackDefinitionAsset> _attackRecipeById = new Dictionary<string, AttackDefinitionAsset>(StringComparer.OrdinalIgnoreCase);
        private AutoDefenseDefinition _definition;
        private CombatCatalog _combatCatalog;
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

        public AutoDefenseRuntime Runtime => _runtime;
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
        public int PresentationEventCount { get; private set; }
        public int StatusHookApplicationCount { get; private set; }
        public int EnemySpawnDelayTicks { get; private set; }
        public long OfflineRewardCredits { get; private set; }
        public long OfflineRewardParts { get; private set; }
        public long EncounterRewardCredits { get; private set; }
        public long EncounterRewardParts { get; private set; }
        public IdleProgressionResultCode LastOfflineRewardCode { get; private set; } = IdleProgressionResultCode.NoElapsedTime;
        public bool EncounterCompleted => _runtime != null && _runtime.State == AutoDefenseRuntimeState.Completed;
        public bool EncounterFailed => _runtime != null && _runtime.State == AutoDefenseRuntimeState.Failed;

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
            _resolvedAttackRecipes = ResolveAttackRecipes();
            CacheAttackRecipes(_resolvedAttackRecipes);
            _definition = BasicIdleAutoDefenseGame.CreateDefinition();
            _combatCatalog = BasicIdleAutoDefenseGame.CreateCombatCatalog(_resolvedAttackRecipes);
            AttackRuntime attacks = BasicIdleAutoDefenseGame.CreateAttackRuntime(_combatCatalog, _definition, _resolvedAttackRecipes);
            WeaponRuntime weapons = BasicIdleAutoDefenseGame.CreateWeaponRuntime(_definition, attacks);

            _root = new GameObject("BasicIdleAutoDefenseGame");
            CreatePrimitive("Template Core", PrimitiveType.Cube, _definition.Objective.Position, new Vector3(1.1f, 0.6f, 1.1f), Color.cyan);
            for (int i = 0; i < _definition.Mounts.Count; i++)
                CreatePrimitive(_definition.Mounts[i].Id.Value, PrimitiveType.Cube, _definition.Objective.Position + _definition.Mounts[i].LocalOffset, new Vector3(0.45f, 0.35f, 0.45f), Color.yellow);

            _enemyPrefab = CreatePrefab("TemplateIdleEnemyPrefab", PrimitiveType.Capsule, Color.red);
            _projectilePrefab = CreatePrefab("TemplateIdleProjectilePrefab", PrimitiveType.Sphere, Color.magenta);

            var poseResolver = new AutoDefensePerimeterPoseResolver(_definition.Objective, _definition.SpawnRing);
            _enemySpawning = new WorldSpawnService(
                new SpawnableCatalog(new[] { new SpawnableDefinition(BasicIdleAutoDefenseGame.EnemySpawnableId, new GameObjectPrefabProvider(_enemyPrefab), 8, 32) }),
                poseResolver,
                rootName: "TemplateIdleEnemies");
            _navigation = new WorldNavigationService();
            _encounter = new EncounterRuntime(BasicIdleAutoDefenseGame.CreateEncounterDefinition());
            _runtime = new AutoDefenseRuntime(_definition, _enemySpawning, _navigation, weapons, _combatCatalog, _encounter, poses: poseResolver, candidateCapacity: 64);

            var projectilePoseResolver = new ChannelPoseResolver(new Dictionary<WorldSpawnChannelId, SpawnPose>
            {
                { new WorldSpawnChannelId("projectile-origin"), new SpawnPose(_definition.Objective.Position, Quaternion.identity) }
            });
            ProjectileDefinition[] projectileDefinitions = BasicIdleAutoDefenseGame.CreateProjectileDefinitions(_resolvedAttackRecipes);
            _projectileSpawning = new WorldSpawnService(
                new SpawnableCatalog(CreateProjectileSpawnables(projectileDefinitions, _projectilePrefab)),
                projectilePoseResolver,
                rootName: "TemplateIdleProjectiles");
            _projectileNavigation = new WorldNavigationService();
            _projectiles = new ProjectileRuntime(
                _combatCatalog,
                projectileDefinitions,
                new WorldSpawnProjectileSpawner(_projectileSpawning, new WorldSpawnChannelId("projectile-origin")),
                new WorldNavigationProjectileNavigator(_projectileNavigation));
            _upgradeCatalog = BasicIdleAutoDefenseGame.CreateRunUpgradeCatalog();
            _upgradeState = new RunUpgradeState();
            _progressionCatalog = BasicIdleAutoDefenseGame.CreateProgressionCatalog();
            _progressionState = new ProgressionState();
            _offlineDefinition = BasicIdleAutoDefenseGame.CreateOfflineProgressionDefinition();

            _runtime.Start();
            ApplyStatusHookSmoke();
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
            DispatchDirectPresentation(result);
            DirectOrCombatKillCount += result.Killed;
            ObjectiveReachCount += result.ReachedObjective;
            if (result.ReachedObjective > 0) ObjectiveDamageEvents += result.ReachedObjective;

            for (int i = 0; i < result.ProjectileLaunches.Count; i++)
            {
                ProjectileLaunchRequest request = result.ProjectileLaunches[i];
                InvokePresentation(request.AttackDefinitionId, AttackPresentationEventKind.OnFire, _runtime.Objective.Definition.Position);
                ProjectileLaunchResult launch = _projectiles.Launch(request);
                if (!launch.Succeeded) continue;
                ProjectileLaunchCount++;
                TryApplySampleProjectileHit(launch.ProjectileId, request.AttackDefinitionId);
            }

            _projectiles.Tick(ticks);
            _projectileNavigation.Tick((float)(deltaSeconds * ProjectileSpeedMultiplier));
            ApplyDirectDamageBonusIfReady();
            ApplyEncounterRewardIfTerminal();
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

        private void TryApplySampleProjectileHit(ProjectileInstanceId projectileId, AttackDefinitionId attackDefinitionId)
        {
            AutoDefenseRuntimeSnapshot snapshot = _runtime.CreateSnapshot();
            for (int i = 0; i < snapshot.Enemies.Count; i++)
            {
                AutoDefenseEnemySnapshot enemy = snapshot.Enemies[i];
                if (enemy.Lifecycle != AutoDefenseEnemyLifecycle.Active) continue;
                if (enemy.Health <= 0d) continue;

                var projectedHealth = new HealthState(enemy.CombatantId, Math.Max(1d, enemy.Health), Math.Max(0.01d, enemy.Health));
                ProjectileImpactResult impact = _projectiles.ReportImpact(new ProjectileImpactRequest(projectileId, enemy.CombatantId, projectedHealth));
                if (!impact.Succeeded) return;

                DamageResolutionResult damage = CombatDamageResolver.Resolve(impact.DamageRequest);
                InvokePresentation(attackDefinitionId, AttackPresentationEventKind.OnImpact, enemy.Position);
                if (damage.Current.LifeState == LifeState.Dead && _runtime.TryKillEnemy(enemy.Id))
                {
                    ProjectileAdapterKillCount++;
                    return;
                }
            }
        }

        private AttackDefinitionAsset[] ResolveAttackRecipes()
        {
            if (_attackRecipes != null && _attackRecipes.Length > 0)
            {
                var recipes = new List<AttackDefinitionAsset>();
                for (int i = 0; i < _attackRecipes.Length; i++)
                    if (_attackRecipes[i] != null)
                        recipes.Add(_attackRecipes[i]);
                if (recipes.Count > 0) return recipes.ToArray();
            }

            return BasicIdleAutoDefenseGame.CreateAttackRecipes();
        }

        private void CacheAttackRecipes(IReadOnlyList<AttackDefinitionAsset> recipes)
        {
            _attackRecipeById.Clear();
            if (recipes == null) return;
            for (int i = 0; i < recipes.Count; i++)
            {
                AttackDefinitionAsset recipe = recipes[i];
                if (recipe != null && !string.IsNullOrWhiteSpace(recipe.Id))
                    _attackRecipeById[recipe.Id] = recipe;
            }
        }

        private SpawnableDefinition[] CreateProjectileSpawnables(IReadOnlyList<ProjectileDefinition> projectileDefinitions, GameObject projectilePrefab)
        {
            var spawnables = new List<SpawnableDefinition>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < projectileDefinitions.Count; i++)
            {
                ProjectileDefinition definition = projectileDefinitions[i];
                if (definition == null || !seen.Add(definition.SpawnableId.Value)) continue;
                spawnables.Add(new SpawnableDefinition(definition.SpawnableId, new GameObjectPrefabProvider(projectilePrefab), 4, 32));
            }

            return spawnables.ToArray();
        }

        private void DispatchDirectPresentation(AutoDefenseRunResult result)
        {
            if (result == null || result.WeaponFireResult == null) return;
            for (int i = 0; i < result.WeaponFireResult.Intents.Count; i++)
            {
                WeaponIntent intent = result.WeaponFireResult.Intents[i];
                if (intent.Kind != WeaponIntentKind.DirectAttack || intent.AttackIntent == null) continue;
                InvokePresentation(intent.AttackIntent.DefinitionId, AttackPresentationEventKind.OnFire, _runtime.Objective.Definition.Position);
                Vector3 impactPosition = _runtime.Objective.Definition.Position;
                if (intent.AttackIntent.Selection.Found)
                {
                    AutoDefenseRuntimeSnapshot snapshot = _runtime.CreateSnapshot();
                    for (int j = 0; j < snapshot.Enemies.Count; j++)
                    {
                        if (snapshot.Enemies[j].CombatantId.Equals(intent.AttackIntent.Selection.Target.CombatantId))
                        {
                            impactPosition = snapshot.Enemies[j].Position;
                            break;
                        }
                    }
                }

                InvokePresentation(intent.AttackIntent.DefinitionId, AttackPresentationEventKind.OnImpact, impactPosition);
            }
        }

        private void InvokePresentation(AttackDefinitionId attackId, AttackPresentationEventKind eventKind, Vector3 position)
        {
            if (_attackRecipeById.TryGetValue(attackId.Value, out AttackDefinitionAsset recipe))
            {
                AttackPresentationInvocationResult presentation = AttackPresentationRuntimeInvoker.Invoke(
                    recipe,
                    eventKind,
                    position,
                    Quaternion.identity);
                if (presentation.Invoked) PresentationEventCount++;
            }
        }

        private void ApplyStatusHookSmoke()
        {
            if (_combatCatalog == null || !_attackRecipeById.TryGetValue(BasicIdleAutoDefenseGame.HomingPulseAttackId.Value, out AttackDefinitionAsset recipe))
                return;

            AttackDefinition definition = recipe.ToRuntimeDefinition();
            var runtime = new AttackRuntime(_combatCatalog, new[] { definition });
            AttackSourceSnapshot source = new AttackSourceSnapshot(new AttackSourceId("source.template.status-smoke"), new CombatantId("template-core"));
            runtime.RegisterSource(source);
            var target = new HealthState(new CombatantId("combatant.template.status-smoke"), 20, 20);
            AttackResult attack = runtime.TryAttack(source.Id, definition.Id, new[] { new AttackTargetCandidate(target.Id, target, 1) });
            if (!attack.Succeeded) return;

            var statusState = new StatusState();
            DamageResolutionResult result = CombatDamageResolver.Resolve(_combatCatalog, target, statusState, attack.Intent.DamageRequest);
            if (!result.Succeeded) return;
            if (statusState.Contains(new StatusEffectId("status.template.slow")))
                StatusHookApplicationCount++;
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
            return instance;
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
