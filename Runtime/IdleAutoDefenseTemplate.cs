using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Deucarian.Attacks;
using Deucarian.Attacks.Authoring;
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
using Deucarian.RunUpgrades.Authoring;
using Deucarian.WeaponSystems;
using Deucarian.WeaponSystems.Authoring;
using Deucarian.WorldNavigation;
using Deucarian.WorldSpawning;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    public static class BasicIdleAutoDefenseGame
    {
        public static readonly DamageTypeId DamageType = new DamageTypeId("damage.idle-auto-defense.basic");
        public static readonly AttackDefinitionId PulseAttackId = new AttackDefinitionId("attack.idle-auto-defense.pulse-beam");
        public static readonly AttackDefinitionId ShardAttackId = new AttackDefinitionId("attack.idle-auto-defense.shard-projectile");
        public static readonly AttackDefinitionId ArcBurstAttackId = new AttackDefinitionId("attack.idle-auto-defense.arc-burst");
        public static readonly AttackDefinitionId HomingPulseAttackId = new AttackDefinitionId("attack.idle-auto-defense.homing-pulse");
        public static readonly AttackDefinitionId AttackId = PulseAttackId;
        public static readonly ProjectileDefinitionId ShardProjectileId = new ProjectileDefinitionId("projectile.idle-auto-defense.shard");
        public static readonly ProjectileDefinitionId HomingPulseProjectileId = new ProjectileDefinitionId("projectile.idle-auto-defense.homing-pulse");
        public static readonly ProjectileDefinitionId ProjectileId = ShardProjectileId;
        public static readonly WorldSpawnableId SwarmEnemySpawnableId = new WorldSpawnableId("enemy.idle-auto-defense.swarm");
        public static readonly WorldSpawnableId RunnerEnemySpawnableId = new WorldSpawnableId("enemy.idle-auto-defense.runner");
        public static readonly WorldSpawnableId TankEnemySpawnableId = new WorldSpawnableId("enemy.idle-auto-defense.tank");
        public static readonly WorldSpawnableId ShieldedEnemySpawnableId = new WorldSpawnableId("enemy.idle-auto-defense.shielded");
        public static readonly WorldSpawnableId EliteEnemySpawnableId = new WorldSpawnableId("enemy.idle-auto-defense.elite");
        public static readonly WorldSpawnableId BossEnemySpawnableId = new WorldSpawnableId("enemy.idle-auto-defense.boss");
        public static readonly WorldSpawnableId EnemySpawnableId = SwarmEnemySpawnableId;
        public static readonly WorldSpawnableId ProjectileSpawnableId = new WorldSpawnableId("projectile.idle-auto-defense.shard");
        public static readonly WeaponDefinitionId PulseCannonWeaponId = new WeaponDefinitionId("weapon.idle-auto-defense.pulse-beam");
        public static readonly WeaponDefinitionId ShardLauncherWeaponId = new WeaponDefinitionId("weapon.idle-auto-defense.shard-launcher");
        public static readonly WeaponDefinitionId ArcBurstTowerWeaponId = new WeaponDefinitionId("weapon.idle-auto-defense.arc-burst");
        public static readonly WeaponDefinitionId HomingSpireWeaponId = new WeaponDefinitionId("weapon.idle-auto-defense.homing-pulse");
        public static readonly WeaponDefinitionId ArcEmitterWeaponId = ArcBurstTowerWeaponId;
        public static readonly WeaponDefinitionId OrbitalShotWeaponId = HomingSpireWeaponId;
        private static readonly string[] RequiredTemplateAttackIds =
        {
            PulseAttackId.Value,
            ShardAttackId.Value,
            ArcBurstAttackId.Value,
            HomingPulseAttackId.Value
        };
        private static readonly string[] RequiredTemplateWeaponIds =
        {
            PulseCannonWeaponId.Value,
            ShardLauncherWeaponId.Value,
            ArcBurstTowerWeaponId.Value,
            HomingSpireWeaponId.Value
        };
        private static readonly string[] RequiredTemplateEnemyIds =
        {
            SwarmEnemySpawnableId.Value,
            RunnerEnemySpawnableId.Value,
            TankEnemySpawnableId.Value,
            ShieldedEnemySpawnableId.Value,
            EliteEnemySpawnableId.Value,
            BossEnemySpawnableId.Value
        };
        public static readonly CurrencyId Credits = new CurrencyId("currency.idle-auto-defense.credits");
        public static readonly CurrencyId Parts = new CurrencyId("currency.idle-auto-defense.parts");
        public static readonly TrackId AccountXp = new TrackId("track.idle-auto-defense.account");
        public static readonly UnlockId StarterUnlock = new UnlockId("unlock.idle-auto-defense.starter");
        public static readonly UnlockId Stage2Unlock = new UnlockId("unlock.idle-auto-defense.stage.pressure-ring");
        public static readonly UnlockId Stage3Unlock = new UnlockId("unlock.idle-auto-defense.stage.boss-pulse");
        public static readonly UnlockId PulseCannonUnlock = new UnlockId("unlock.idle-auto-defense.module.pulse-cannon");
        public static readonly UnlockId ShardLauncherUnlock = new UnlockId("unlock.idle-auto-defense.module.shard-launcher");
        public static readonly ResearchNodeId CorePlatingResearch = new ResearchNodeId("research.idle-auto-defense.core-plating");
        public static readonly ResearchNodeId PulseCapacitorResearch = new ResearchNodeId("research.idle-auto-defense.pulse-capacitor");
        public static readonly ResearchNodeId ShardLoaderResearch = new ResearchNodeId("research.idle-auto-defense.shard-loader");
        public static readonly ResearchNodeId OfflineRoutingResearch = new ResearchNodeId("research.idle-auto-defense.offline-routing");

        public static AutoDefenseDefinition CreateDefinition(
            IReadOnlyList<EnemyDefinitionAsset> enemyDefinitions = null,
            IReadOnlyList<WeaponDefinitionAsset> weaponDefinitions = null,
            IdleAutoDefenseGameRulesAsset gameRules = null,
            float difficultyMultiplier = 1f)
        {
            WeaponDefinitionAsset[] weapons = weaponDefinitions == null || weaponDefinitions.Count == 0
                ? CreateWeaponDefinitionAssets(CreateAttackRecipes())
                : CopyWeaponDefinitions(weaponDefinitions);
            AutoDefenseEnemyDefinition[] enemies = enemyDefinitions == null
                ? CreateDefaultAutoDefenseEnemyDefinitions()
                : CreateAutoDefenseEnemyDefinitions(enemyDefinitions, difficultyMultiplier);
            AutoDefenseMountDefinition[] mounts = CreateAutoDefenseMountDefinitions(weapons);
            return new AutoDefenseDefinition(
                gameRules == null
                    ? new AutoDefenseObjectiveDefinition(new DefenseObjectiveId("objective.idle-auto-defense.core"), Vector3.zero, 240, DamageType, 0.45f, 60, 2)
                    : gameRules.CreateObjectiveDefinition(),
                gameRules == null ? CreateSampleSpawnRing() : gameRules.CreateSpawnRingDefinition(),
                enemies,
                mounts,
                CreateAutoDefenseWeaponModuleDefinitions(weapons, mounts));
        }

        public static AutoDefenseSpawnRingDefinition CreateSampleSpawnRing()
        {
            const float radius = 18.5f;
            return new AutoDefenseSpawnRingDefinition(radius, new[]
            {
                new AutoDefenseSpawnChannelDefinition(new WorldSpawnChannelId("perimeter-north"), 0f),
                new AutoDefenseSpawnChannelDefinition(new WorldSpawnChannelId("perimeter-northeast"), 45f),
                new AutoDefenseSpawnChannelDefinition(new WorldSpawnChannelId("perimeter-east"), 90f),
                new AutoDefenseSpawnChannelDefinition(new WorldSpawnChannelId("perimeter-southeast"), 135f),
                new AutoDefenseSpawnChannelDefinition(new WorldSpawnChannelId("perimeter-south"), 180f),
                new AutoDefenseSpawnChannelDefinition(new WorldSpawnChannelId("perimeter-southwest"), 225f),
                new AutoDefenseSpawnChannelDefinition(new WorldSpawnChannelId("perimeter-west"), 270f),
                new AutoDefenseSpawnChannelDefinition(new WorldSpawnChannelId("perimeter-northwest"), 315f)
            });
        }

        public static EncounterDefinition CreateEncounterDefinition(IReadOnlyList<WaveDefinitionAsset> waveDefinitions = null, int seed = 20260623)
        {
            if (waveDefinitions == null || waveDefinitions.Count == 0)
                waveDefinitions = CreateWaveDefinitions();
            return new EncounterDefinition(
                new EncounterId("encounter.idle-auto-defense.first-orbit"),
                null,
                CreateEncounterWaves(waveDefinitions),
                new[] { ObjectiveDefinition.AllWavesEmitted(new EncounterObjectiveId("all-waves-emitted")) },
                seed: seed);
        }

        public static GameContentSetResolution ResolveGameContentSetForTemplate(GameContentSetAsset contentSet)
        {
            return GameContentSetValidator.Resolve(contentSet);
        }

        public static StageDefinition[] CreateStageDefinitions()
        {
            return new[]
            {
                new StageDefinition(new StageId("stage.idle-auto-defense.first-orbit"), new EncounterId("encounter.idle-auto-defense.first-orbit"), new[] { new RewardReferenceId("reward.idle-auto-defense.first-orbit") }),
                new StageDefinition(new StageId("stage.idle-auto-defense.pressure-ring"), new EncounterId("encounter.idle-auto-defense.pressure-ring"), new[] { new RewardReferenceId("reward.idle-auto-defense.pressure-ring") }),
                new StageDefinition(new StageId("stage.idle-auto-defense.boss-pulse"), new EncounterId("encounter.idle-auto-defense.boss-pulse"), new[] { new RewardReferenceId("reward.idle-auto-defense.boss-pulse") }),
                new StageDefinition(new StageId("stage.idle-auto-defense.endless-placeholder"), new EncounterId("encounter.idle-auto-defense.endless-placeholder"), new[] { new RewardReferenceId("reward.idle-auto-defense.endless-placeholder") })
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
                    new SpawnGroupId("group.idle-auto-defense.first-orbit.swarm." + channels[i]),
                    new SpawnableId(SwarmEnemySpawnableId.Value),
                    3,
                    1,
                    i * 12,
                    20,
                    new SpawnChannelId(channels[i])));
            }

            groups.Add(SpawnGroupDefinition.Fixed(new SpawnGroupId("group.idle-auto-defense.first-orbit.runner-east"), new SpawnableId(RunnerEnemySpawnableId.Value), 2, 1, 42, 18, new SpawnChannelId("perimeter-east")));
            groups.Add(SpawnGroupDefinition.Fixed(new SpawnGroupId("group.idle-auto-defense.first-orbit.tank-west"), new SpawnableId(TankEnemySpawnableId.Value), 1, 1, 78, 0, new SpawnChannelId("perimeter-west")));

            return new EncounterDefinition(
                new EncounterId("encounter.idle-auto-defense.first-orbit"),
                null,
                new[]
                {
                    new WaveDefinition(new WaveId("wave.idle-auto-defense.first-orbit.opening"), 0, groups.GetRange(0, 4)),
                    new WaveDefinition(new WaveId("wave.idle-auto-defense.first-orbit.pressure"), 36, groups.GetRange(4, 2))
                },
                new[] { ObjectiveDefinition.AllWavesEmitted(new EncounterObjectiveId("all-waves-emitted")) },
                seed: 20260623);
        }

        public static EncounterDefinition CreatePressureRingEncounterDefinition()
        {
            return new EncounterDefinition(
                new EncounterId("encounter.idle-auto-defense.pressure-ring"),
                null,
                new[]
                {
                    new WaveDefinition(new WaveId("wave.idle-auto-defense.pressure-ring.runners"), 0, new[]
                    {
                        SpawnGroupDefinition.Fixed(new SpawnGroupId("group.idle-auto-defense.pressure-ring.runner-north"), new SpawnableId(RunnerEnemySpawnableId.Value), 4, 1, 0, 12, new SpawnChannelId("perimeter-north")),
                        SpawnGroupDefinition.Fixed(new SpawnGroupId("group.idle-auto-defense.pressure-ring.runner-south"), new SpawnableId(RunnerEnemySpawnableId.Value), 4, 1, 8, 12, new SpawnChannelId("perimeter-south"))
                    }),
                    new WaveDefinition(new WaveId("wave.idle-auto-defense.pressure-ring.armor"), 48, new[]
                    {
                        SpawnGroupDefinition.Fixed(new SpawnGroupId("group.idle-auto-defense.pressure-ring.shielded-east"), new SpawnableId(ShieldedEnemySpawnableId.Value), 3, 1, 0, 20, new SpawnChannelId("perimeter-east")),
                        SpawnGroupDefinition.Fixed(new SpawnGroupId("group.idle-auto-defense.pressure-ring.tank-west"), new SpawnableId(TankEnemySpawnableId.Value), 2, 1, 18, 28, new SpawnChannelId("perimeter-west"))
                    }),
                    new WaveDefinition(new WaveId("wave.idle-auto-defense.pressure-ring.elite"), 108, new[]
                    {
                        SpawnGroupDefinition.Fixed(new SpawnGroupId("group.idle-auto-defense.pressure-ring.elite-north"), new SpawnableId(EliteEnemySpawnableId.Value), 1, 1, 0, 0, new SpawnChannelId("perimeter-north")),
                        SpawnGroupDefinition.Fixed(new SpawnGroupId("group.idle-auto-defense.pressure-ring.swarm-all"), new SpawnableId(SwarmEnemySpawnableId.Value), 8, 2, 8, 18, new SpawnChannelId("perimeter-south"))
                    })
                },
                new[] { ObjectiveDefinition.AllWavesEmitted(new EncounterObjectiveId("all-waves-emitted")) },
                seed: 20260624);
        }

        public static EncounterDefinition CreateBossPulseEncounterDefinition()
        {
            return new EncounterDefinition(
                new EncounterId("encounter.idle-auto-defense.boss-pulse"),
                null,
                new[]
                {
                    new WaveDefinition(new WaveId("wave.idle-auto-defense.boss-pulse.breakers"), 0, new[]
                    {
                        SpawnGroupDefinition.Fixed(new SpawnGroupId("group.idle-auto-defense.boss-pulse.runner-burst-north"), new SpawnableId(RunnerEnemySpawnableId.Value), 8, 4, 0, 8, new SpawnChannelId("perimeter-north")),
                        SpawnGroupDefinition.Fixed(new SpawnGroupId("group.idle-auto-defense.boss-pulse.runner-burst-east"), new SpawnableId(RunnerEnemySpawnableId.Value), 8, 4, 0, 8, new SpawnChannelId("perimeter-east"))
                    }),
                    new WaveDefinition(new WaveId("wave.idle-auto-defense.boss-pulse.guard"), 28, new[]
                    {
                        SpawnGroupDefinition.Fixed(new SpawnGroupId("group.idle-auto-defense.boss-pulse.shielded-ring"), new SpawnableId(ShieldedEnemySpawnableId.Value), 4, 2, 0, 18, new SpawnChannelId("perimeter-west")),
                        SpawnGroupDefinition.Fixed(new SpawnGroupId("group.idle-auto-defense.boss-pulse.tank-ring"), new SpawnableId(TankEnemySpawnableId.Value), 3, 1, 12, 24, new SpawnChannelId("perimeter-south"))
                    }),
                    new WaveDefinition(new WaveId("wave.idle-auto-defense.boss-pulse.boss"), 80, new[]
                    {
                        SpawnGroupDefinition.Fixed(new SpawnGroupId("group.idle-auto-defense.boss-pulse.elite"), new SpawnableId(EliteEnemySpawnableId.Value), 2, 1, 0, 18, new SpawnChannelId("perimeter-east")),
                        SpawnGroupDefinition.Fixed(new SpawnGroupId("group.idle-auto-defense.boss-pulse.boss"), new SpawnableId(BossEnemySpawnableId.Value), 1, 1, 18, 0, new SpawnChannelId("perimeter-north"))
                    })
                },
                new[] { ObjectiveDefinition.AllWavesEmitted(new EncounterObjectiveId("all-waves-emitted")) },
                seed: 20260625);
        }

        public static EncounterDefinition CreateEndlessPlaceholderEncounterDefinition()
        {
            return new EncounterDefinition(
                new EncounterId("encounter.idle-auto-defense.endless-placeholder"),
                null,
                new[]
                {
                    new WaveDefinition(new WaveId("wave.idle-auto-defense.endless-placeholder.loop-seed"), 0, new[]
                    {
                        SpawnGroupDefinition.Fixed(new SpawnGroupId("group.idle-auto-defense.endless-placeholder.swarm"), new SpawnableId(SwarmEnemySpawnableId.Value), 4, 1, 0, 16, new SpawnChannelId("perimeter-north")),
                        SpawnGroupDefinition.Fixed(new SpawnGroupId("group.idle-auto-defense.endless-placeholder.runner"), new SpawnableId(RunnerEnemySpawnableId.Value), 2, 1, 24, 20, new SpawnChannelId("perimeter-east"))
                    })
                },
                new[] { ObjectiveDefinition.AllWavesEmitted(new EncounterObjectiveId("all-waves-emitted")) },
                seed: 20260626);
        }

        public static CombatCatalog CreateCombatCatalog(
            IReadOnlyList<AttackDefinitionAsset> attackRecipes = null,
            IReadOnlyList<EnemyDefinitionAsset> enemyDefinitions = null,
            IdleAutoDefenseGameRulesAsset gameRules = null)
        {
            attackRecipes ??= CreateAttackRecipes();
            enemyDefinitions ??= CreateEnemyDefinitions();
            var damageTypes = new List<DamageTypeDefinition>();
            var damageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddDamageType(damageTypes, damageIds, DamageType);
            if (gameRules != null && !string.IsNullOrWhiteSpace(gameRules.DamageTypeId))
                AddDamageType(damageTypes, damageIds, new DamageTypeId(gameRules.DamageTypeId));
            for (int i = 0; i < attackRecipes.Count; i++)
            {
                AttackDefinitionAsset recipe = attackRecipes[i];
                if (recipe == null || recipe.Mechanics == null || string.IsNullOrWhiteSpace(recipe.Mechanics.DamageTypeId)) continue;
                AddDamageType(damageTypes, damageIds, new DamageTypeId(recipe.Mechanics.DamageTypeId));
            }

            for (int i = 0; i < enemyDefinitions.Count; i++)
            {
                EnemyDefinitionAsset enemy = enemyDefinitions[i];
                if (enemy == null || enemy.Stats == null || string.IsNullOrWhiteSpace(enemy.Stats.DamageTypeId)) continue;
                AddDamageType(damageTypes, damageIds, new DamageTypeId(enemy.Stats.DamageTypeId));
            }

            var statuses = new List<StatusEffectDefinition>();
            var statusIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < attackRecipes.Count; i++)
            {
                AttackDefinitionAsset recipe = attackRecipes[i];
                if (recipe == null) continue;
                StatusEffectDefinition[] definitions = recipe.CreateStatusDefinitions();
                for (int j = 0; j < definitions.Length; j++)
                    if (definitions[j] != null && statusIds.Add(definitions[j].Id.Value))
                        statuses.Add(definitions[j]);
            }

            return new CombatCatalog(damageTypes, statuses);
        }

        public static AttackRuntime CreateAttackRuntime(CombatCatalog catalog, AutoDefenseDefinition definition, IReadOnlyList<AttackDefinitionAsset> attackRecipes = null)
        {
            var runtime = new AttackRuntime(catalog, CreateAttackDefinitions(attackRecipes ?? CreateAttackRecipes()));
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

        public static WeaponDefinitionAsset[] CreateWeaponDefinitionAssets(IReadOnlyList<AttackDefinitionAsset> attackRecipes = null)
        {
            attackRecipes ??= CreateAttackRecipes();
            AttackDefinitionAsset pulse = FindAttackRecipe(attackRecipes, PulseAttackId.Value);
            AttackDefinitionAsset shard = FindAttackRecipe(attackRecipes, ShardAttackId.Value);
            AttackDefinitionAsset arc = FindAttackRecipe(attackRecipes, ArcBurstAttackId.Value);
            AttackDefinitionAsset homing = FindAttackRecipe(attackRecipes, HomingPulseAttackId.Value);
            return new[]
            {
                WeaponDefinitionAsset.CreateTransient(
                    ShardLauncherWeaponId.Value,
                    "Shard Launcher",
                    WeaponFireMode.Projectile,
                    shard,
                    34,
                    4.7f,
                    ShardProjectileId.Value,
                    buildCost: 35,
                    upgradeGroupId: "upgrade.group.idle-auto-defense.shard",
                    prefab: CreateTransientWeaponPresentationPrefab("Idle Auto Defense Transient Shard Launcher Presentation", "weapon-ballista", new Color(1f, 0.45f, 0.1f)),
                    tags: new[] { "idle-auto-defense", "projectile", "tower" }),
                WeaponDefinitionAsset.CreateTransient(
                    PulseCannonWeaponId.Value,
                    "Pulse Beam",
                    WeaponFireMode.DirectAttack,
                    pulse,
                    72,
                    5.0f,
                    buildCost: 34,
                    upgradeGroupId: "upgrade.group.idle-auto-defense.pulse",
                    prefab: CreateTransientWeaponPresentationPrefab("Idle Auto Defense Transient Pulse Beam Presentation", "weapon-turret", new Color(0.35f, 0.82f, 1f)),
                    tags: new[] { "idle-auto-defense", "hitscan", "tower" }),
                WeaponDefinitionAsset.CreateTransient(
                    ArcBurstTowerWeaponId.Value,
                    "Arc Burst Module",
                    WeaponFireMode.DirectAttack,
                    arc,
                    108,
                    4.1f,
                    buildCost: 62,
                    upgradeGroupId: "upgrade.group.idle-auto-defense.arc",
                    prefab: CreateTransientWeaponPresentationPrefab("Idle Auto Defense Transient Arc Burst Presentation", "weapon-catapult", new Color(1f, 0.72f, 0.18f)),
                    tags: new[] { "idle-auto-defense", "area", "tower" }),
                WeaponDefinitionAsset.CreateTransient(
                    HomingSpireWeaponId.Value,
                    "Homing Pulse Module",
                    WeaponFireMode.Projectile,
                    homing,
                    92,
                    6.3f,
                    HomingPulseProjectileId.Value,
                    buildCost: 78,
                    upgradeGroupId: "upgrade.group.idle-auto-defense.homing",
                    prefab: CreateTransientWeaponPresentationPrefab("Idle Auto Defense Transient Homing Pulse Presentation", "weapon-cannon", new Color(0.8f, 0.42f, 1f)),
                    tags: new[] { "idle-auto-defense", "homing", "tower" })
            };
        }

        private static GameObject CreateTransientWeaponPresentationPrefab(string name, string modelName, Color tint)
        {
            var prefab = new GameObject(name);
            prefab.hideFlags = HideFlags.HideAndDontSave;
            IdleAutoDefenseKenneyModelPrefab model = prefab.AddComponent<IdleAutoDefenseKenneyModelPrefab>();
            model.ConfigureForTests(modelName, tint, Vector3.zero, Vector3.zero, Vector3.one * 0.74f);
            prefab.SetActive(false);
            return prefab;
        }

        public static WeaponDefinitionAsset[] ResolveWeaponDefinitionsForTemplate(IReadOnlyList<WeaponDefinitionAsset> assignedDefinitions, IReadOnlyList<AttackDefinitionAsset> attackRecipes, out int rejectedDefinitionCount)
        {
            rejectedDefinitionCount = 0;
            if (assignedDefinitions == null || assignedDefinitions.Count == 0)
                return CreateWeaponDefinitionAssets(attackRecipes);

            var definitions = new List<WeaponDefinitionAsset>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < assignedDefinitions.Count; i++)
            {
                WeaponDefinitionAsset definition = assignedDefinitions[i];
                if (definition == null)
                {
                    rejectedDefinitionCount++;
                    continue;
                }

                WeaponDefinitionValidationReport report = WeaponDefinitionValidator.Validate(definition, WeaponDefinitionValidationOptions.RuntimeFriendly);
                if (!report.IsValid || !WeaponAttackExists(definition, attackRecipes))
                {
                    rejectedDefinitionCount++;
                    continue;
                }

                string id = definition.Id.Trim();
                if (!seen.Add(id))
                {
                    rejectedDefinitionCount++;
                    continue;
                }

                definitions.Add(definition);
            }

            if (definitions.Count == 0)
                return CreateWeaponDefinitionAssets(attackRecipes);

            int missingRequired = CountMissingRequiredTemplateWeaponIds(definitions);
            if (missingRequired > 0)
            {
                rejectedDefinitionCount += missingRequired;
                return CreateWeaponDefinitionAssets(attackRecipes);
            }

            return definitions.ToArray();
        }

        public static WeaponDefinition[] CreateWeaponDefinitions(IReadOnlyList<WeaponDefinitionAsset> weaponDefinitions)
        {
            if (weaponDefinitions == null || weaponDefinitions.Count == 0) throw new ArgumentException("At least one weapon definition is required.", nameof(weaponDefinitions));
            var definitions = new WeaponDefinition[weaponDefinitions.Count];
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < weaponDefinitions.Count; i++)
            {
                if (weaponDefinitions[i] == null) throw new ArgumentException("Weapon definition cannot be null.", nameof(weaponDefinitions));
                WeaponDefinitionValidationReport report = WeaponDefinitionValidator.Validate(weaponDefinitions[i], WeaponDefinitionValidationOptions.RuntimeFriendly);
                if (!report.IsValid) throw new ArgumentException("Weapon definition is invalid.", nameof(weaponDefinitions));
                if (!seen.Add(weaponDefinitions[i].Id.Trim())) throw new ArgumentException("Duplicate weapon definition ID: " + weaponDefinitions[i].Id, nameof(weaponDefinitions));
                definitions[i] = weaponDefinitions[i].ToRuntimeDefinition();
            }

            return definitions;
        }

        public static ProjectileDefinition CreateProjectileDefinition()
        {
            return CreateProjectileDefinitions()[0];
        }

        public static ProjectileDefinition[] CreateProjectileDefinitions(IReadOnlyList<AttackDefinitionAsset> attackRecipes = null)
        {
            attackRecipes ??= CreateAttackRecipes();
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

        public static EnemyDefinitionAsset[] CreateEnemyDefinitions()
        {
            return new[]
            {
                EnemyDefinitionAsset.CreateTransient(SwarmEnemySpawnableId.Value, "Swarm", EnemyRole.Swarm, 20f, 0.72f, 5, 4f, DamageType.Value, 0.28f, tags: new[] { "idle-auto-defense", "swarm" }),
                EnemyDefinitionAsset.CreateTransient(RunnerEnemySpawnableId.Value, "Runner", EnemyRole.Fast, 26f, 1.1f, 5, 5f, DamageType.Value, 0.27f, tags: new[] { "idle-auto-defense", "runner" }),
                EnemyDefinitionAsset.CreateTransient(TankEnemySpawnableId.Value, "Tank", EnemyRole.Tank, 74f, 0.48f, 5, 10f, DamageType.Value, 0.48f, tags: new[] { "idle-auto-defense", "tank" }),
                EnemyDefinitionAsset.CreateTransient(ShieldedEnemySpawnableId.Value, "Shielded", EnemyRole.Basic, 46f, 0.64f, 5, 7f, DamageType.Value, 0.38f, tags: new[] { "idle-auto-defense", "shielded" }),
                EnemyDefinitionAsset.CreateTransient(EliteEnemySpawnableId.Value, "Elite", EnemyRole.Boss, 170f, 0.54f, 5, 26f, DamageType.Value, 0.54f, tags: new[] { "idle-auto-defense", "elite" }),
                EnemyDefinitionAsset.CreateTransient(BossEnemySpawnableId.Value, "Boss", EnemyRole.Boss, 390f, 0.36f, 5, 60f, DamageType.Value, 0.82f, tags: new[] { "idle-auto-defense", "boss" })
            };
        }

        public static EnemyDefinitionAsset[] ResolveEnemyDefinitionsForTemplate(IReadOnlyList<EnemyDefinitionAsset> assignedDefinitions, out int rejectedDefinitionCount)
        {
            rejectedDefinitionCount = 0;
            if (assignedDefinitions == null || assignedDefinitions.Count == 0)
                return CreateEnemyDefinitions();

            var definitions = new List<EnemyDefinitionAsset>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < assignedDefinitions.Count; i++)
            {
                EnemyDefinitionAsset definition = assignedDefinitions[i];
                if (definition == null)
                {
                    rejectedDefinitionCount++;
                    continue;
                }

                ContentAuthoringValidationReport report = EnemyDefinitionValidator.Validate(definition, EnemyDefinitionValidationOptions.AssetCreation);
                if (!report.IsValid)
                {
                    rejectedDefinitionCount++;
                    continue;
                }

                string id = definition.Id.Trim();
                if (!seen.Add(id))
                {
                    rejectedDefinitionCount++;
                    continue;
                }

                definitions.Add(definition);
            }

            if (definitions.Count == 0)
                return CreateEnemyDefinitions();

            int missingRequired = CountMissingRequiredTemplateEnemyIds(definitions);
            if (missingRequired > 0)
            {
                rejectedDefinitionCount += missingRequired;
                return CreateEnemyDefinitions();
            }

            return definitions.ToArray();
        }

        public static AutoDefenseEnemyDefinition[] CreateAutoDefenseEnemyDefinitions(IReadOnlyList<EnemyDefinitionAsset> enemyDefinitions, float difficultyMultiplier = 1f)
        {
            if (enemyDefinitions == null || enemyDefinitions.Count == 0) throw new ArgumentException("At least one enemy definition is required.", nameof(enemyDefinitions));
            var definitions = new AutoDefenseEnemyDefinition[enemyDefinitions.Count];
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < enemyDefinitions.Count; i++)
            {
                EnemyDefinitionAsset enemy = enemyDefinitions[i];
                if (enemy == null) throw new ArgumentException("Enemy definition cannot be null.", nameof(enemyDefinitions));
                ContentAuthoringValidationReport report = EnemyDefinitionValidator.Validate(enemy, EnemyDefinitionValidationOptions.RuntimeFriendly);
                if (!report.IsValid) throw new ArgumentException("Enemy definition is invalid: " + GetFirstValidationError(report), nameof(enemyDefinitions));
                if (!seen.Add(enemy.Id.Trim())) throw new ArgumentException("Duplicate enemy definition ID: " + enemy.Id, nameof(enemyDefinitions));
                double difficulty = float.IsNaN(difficultyMultiplier) || float.IsInfinity(difficultyMultiplier)
                    ? 1d
                    : Math.Max(0.01d, difficultyMultiplier);
                definitions[i] = new AutoDefenseEnemyDefinition(
                    new WorldSpawnableId(enemy.Id),
                    enemy.Stats.MaximumHealth * difficulty,
                    enemy.Stats.MoveSpeed,
                    enemy.Stats.ContactDamage * difficulty,
                    new DamageTypeId(enemy.Stats.DamageTypeId),
                    enemy.Stats.CollisionRadius);
            }

            return definitions;
        }

        public static WaveDefinitionAsset[] CreateWaveDefinitions()
        {
            EnemyDefinitionAsset[] enemies = CreateEnemyDefinitions();
            return new[]
            {
                WaveDefinitionAsset.CreateTransient(
                    "wave.idle-auto-defense.opening",
                    "Opening Wave",
                    0,
                    new[]
                    {
                        new WaveEntryRecipe(enemies[0], 7, 1, 0, 66, "perimeter-north"),
                        new WaveEntryRecipe(enemies[1], 3, 1, 220, 84, "perimeter-east"),
                        new WaveEntryRecipe(enemies[2], 1, 1, 420, 0, "perimeter-northwest")
                    },
                    new[] { "idle-auto-defense", "opening" }),
                WaveDefinitionAsset.CreateTransient(
                    "wave.idle-auto-defense.runner-pressure",
                    "Runner Pressure",
                    620,
                    new[]
                    {
                        new WaveEntryRecipe(enemies[1], 6, 1, 0, 58, "perimeter-southeast", 1),
                        new WaveEntryRecipe(enemies[0], 7, 1, 90, 55, "perimeter-northeast", 1)
                    },
                    new[] { "idle-auto-defense", "runner-pressure" }),
                WaveDefinitionAsset.CreateTransient(
                    "wave.idle-auto-defense.pressure",
                    "Mixed Pressure",
                    1180,
                    new[]
                    {
                        new WaveEntryRecipe(enemies[3], 3, 1, 0, 72, "perimeter-south", 1),
                        new WaveEntryRecipe(enemies[2], 2, 1, 100, 92, "perimeter-west", 2),
                        new WaveEntryRecipe(enemies[1], 6, 1, 180, 52, "perimeter-northeast", 2)
                    },
                    new[] { "idle-auto-defense", "pressure" }),
                WaveDefinitionAsset.CreateTransient(
                    "wave.idle-auto-defense.surge",
                    "Tank Break",
                    1760,
                    new[]
                    {
                        new WaveEntryRecipe(enemies[0], 9, 1, 0, 42, "perimeter-southwest", 1),
                        new WaveEntryRecipe(enemies[1], 6, 1, 130, 54, "perimeter-southeast", 1),
                        new WaveEntryRecipe(enemies[3], 3, 1, 260, 80, "perimeter-west", 2)
                    },
                    new[] { "idle-auto-defense", "tank-break" }),
                WaveDefinitionAsset.CreateTransient(
                    "wave.idle-auto-defense.elite",
                    "Elite Pressure",
                    2450,
                    new[]
                    {
                        new WaveEntryRecipe(enemies[4], 1, 1, 0, 0, "perimeter-northwest", 3),
                        new WaveEntryRecipe(enemies[1], 7, 1, 140, 52, "perimeter-east", 2),
                        new WaveEntryRecipe(enemies[3], 3, 1, 300, 80, "perimeter-south", 2)
                    },
                    new[] { "idle-auto-defense", "elite" }),
                WaveDefinitionAsset.CreateTransient(
                    "wave.idle-auto-defense.final",
                    "Final Surge",
                    3150,
                    new[]
                    {
                        new WaveEntryRecipe(enemies[2], 3, 1, 0, 100, "perimeter-north", 2),
                        new WaveEntryRecipe(enemies[3], 4, 1, 170, 74, "perimeter-east", 2),
                        new WaveEntryRecipe(enemies[1], 8, 1, 300, 48, "perimeter-southwest", 3)
                    },
                    new[] { "idle-auto-defense", "final" }),
                WaveDefinitionAsset.CreateTransient(
                    "wave.idle-auto-defense.boss",
                    "Boss Push",
                    3900,
                    new[]
                    {
                        new WaveEntryRecipe(enemies[5], 1, 1, 0, 0, "perimeter-south", 4),
                        new WaveEntryRecipe(enemies[4], 1, 1, 300, 0, "perimeter-northeast", 3),
                        new WaveEntryRecipe(enemies[1], 12, 1, 400, 64, "perimeter-northwest", 3)
                    },
                    new[] { "idle-auto-defense", "boss" })
            };
        }

        public static WaveDefinitionAsset[] ResolveWaveDefinitionsForTemplate(IReadOnlyList<WaveDefinitionAsset> assignedDefinitions, IReadOnlyList<EnemyDefinitionAsset> enemyDefinitions, out int rejectedDefinitionCount)
        {
            rejectedDefinitionCount = 0;
            if (assignedDefinitions == null || assignedDefinitions.Count == 0)
                return CreateWaveDefinitions();

            var enemyIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (enemyDefinitions != null)
                for (int i = 0; i < enemyDefinitions.Count; i++)
                    if (enemyDefinitions[i] != null && !string.IsNullOrWhiteSpace(enemyDefinitions[i].Id))
                        enemyIds.Add(enemyDefinitions[i].Id.Trim());

            var definitions = new List<WaveDefinitionAsset>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < assignedDefinitions.Count; i++)
            {
                WaveDefinitionAsset definition = assignedDefinitions[i];
                if (definition == null)
                {
                    rejectedDefinitionCount++;
                    continue;
                }

                ContentAuthoringValidationReport report = WaveDefinitionValidator.Validate(definition, WaveDefinitionValidationOptions.RuntimeFriendly);
                if (!report.IsValid || !WaveReferencesKnownEnemies(definition, enemyIds))
                {
                    rejectedDefinitionCount++;
                    continue;
                }

                string id = definition.Id.Trim();
                if (!seen.Add(id))
                {
                    rejectedDefinitionCount++;
                    continue;
                }

                definitions.Add(definition);
            }

            return definitions.Count == 0 ? CreateWaveDefinitions() : definitions.ToArray();
        }

        public static WaveDefinition[] CreateEncounterWaves(IReadOnlyList<WaveDefinitionAsset> waveDefinitions)
        {
            if (waveDefinitions == null || waveDefinitions.Count == 0) throw new ArgumentException("At least one wave definition is required.", nameof(waveDefinitions));
            var waves = new WaveDefinition[waveDefinitions.Count];
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < waveDefinitions.Count; i++)
            {
                WaveDefinitionAsset wave = waveDefinitions[i];
                if (wave == null) throw new ArgumentException("Wave definition cannot be null.", nameof(waveDefinitions));
                ContentAuthoringValidationReport report = WaveDefinitionValidator.Validate(wave, WaveDefinitionValidationOptions.RuntimeFriendly);
                if (!report.IsValid) throw new ArgumentException("Wave definition is invalid: " + GetFirstValidationError(report), nameof(waveDefinitions));
                if (!seen.Add(wave.Id.Trim())) throw new ArgumentException("Duplicate wave definition ID: " + wave.Id, nameof(waveDefinitions));

                IReadOnlyList<WaveEntryRecipe> entries = wave.Entries.Entries;
                var groups = new SpawnGroupDefinition[entries.Count];
                for (int j = 0; j < entries.Count; j++)
                {
                    WaveEntryRecipe entry = entries[j];
                    groups[j] = SpawnGroupDefinition.Fixed(
                        new SpawnGroupId(wave.Id + ".group." + j.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                        new SpawnableId(entry.Enemy.Id),
                        entry.Count,
                        entry.BatchSize,
                        entry.InitialDelayTicks,
                        entry.IntervalTicks,
                        new SpawnChannelId(entry.SpawnChannelId),
                        entry.ScalingTier);
                }

                waves[i] = new WaveDefinition(new WaveId(wave.Id), wave.Schedule.StartTick, groups);
            }

            return waves;
        }

        public static AttackDefinitionAsset[] CreateAttackRecipes()
        {
            return new[]
            {
                AttackDefinitionAsset.CreateTransient(
                    PulseAttackId.Value,
                    "Pulse Beam",
                    AttackRecipeDeliveryMode.Hitscan,
                    DamageType.Value,
                    5.0f,
                    72,
                    5.0f,
                    AttackRecipeTargetingMode.Nearest),
                AttackDefinitionAsset.CreateTransient(
                    ShardAttackId.Value,
                    "Shard Projectile",
                    AttackRecipeDeliveryMode.Projectile,
                    DamageType.Value,
                    3.0f,
                    0,
                    4.7f,
                    AttackRecipeTargetingMode.Strongest,
                    projectileDefinitionId: ShardProjectileId.Value,
                    projectileSpawnableId: ProjectileSpawnableId.Value,
                    projectileSpeed: 4.2f,
                    projectileLifetimeTicks: 150,
                    pierceCount: 0),
                AttackDefinitionAsset.CreateTransient(
                    ArcBurstAttackId.Value,
                    "Arc Burst",
                    AttackRecipeDeliveryMode.Area,
                    DamageType.Value,
                    8.0f,
                    108,
                    4.1f,
                    AttackRecipeTargetingMode.Strongest),
                AttackDefinitionAsset.CreateTransient(
                    HomingPulseAttackId.Value,
                    "Homing Pulse",
                    AttackRecipeDeliveryMode.Projectile,
                    DamageType.Value,
                    8.0f,
                    92,
                    6.3f,
                    AttackRecipeTargetingMode.LowestHealth,
                    projectileDefinitionId: HomingPulseProjectileId.Value,
                    projectileSpawnableId: HomingPulseProjectileId.Value,
                    projectileSpeed: 4.4f,
                    projectileLifetimeTicks: 150,
                    homing: true,
                    pierceCount: 1)
            };
        }

        public static AttackDefinitionAsset[] ResolveAttackRecipesForTemplate(IReadOnlyList<AttackDefinitionAsset> assignedRecipes, out int rejectedRecipeCount)
        {
            rejectedRecipeCount = 0;
            if (assignedRecipes == null || assignedRecipes.Count == 0)
                return CreateAttackRecipes();

            var recipes = new List<AttackDefinitionAsset>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < assignedRecipes.Count; i++)
            {
                AttackDefinitionAsset recipe = assignedRecipes[i];
                if (recipe == null)
                {
                    rejectedRecipeCount++;
                    continue;
                }

                AttackRecipeValidationReport report = AttackRecipeValidator.Validate(recipe, AttackRecipeValidationOptions.RuntimeFriendly);
                if (!report.IsValid)
                {
                    rejectedRecipeCount++;
                    continue;
                }

                string id = recipe.Id.Trim();
                if (!seen.Add(id))
                {
                    rejectedRecipeCount++;
                    continue;
                }

                recipes.Add(recipe);
            }

            if (recipes.Count == 0)
                return CreateAttackRecipes();

            int missingRequired = CountMissingRequiredTemplateAttackIds(recipes);
            if (missingRequired > 0)
            {
                rejectedRecipeCount += missingRequired;
                return CreateAttackRecipes();
            }

            return recipes.ToArray();
        }

        public static AttackDefinition[] CreateAttackDefinitions(IReadOnlyList<AttackDefinitionAsset> attackRecipes)
        {
            if (attackRecipes == null || attackRecipes.Count == 0) throw new ArgumentException("At least one attack recipe is required.", nameof(attackRecipes));
            var definitions = new AttackDefinition[attackRecipes.Count];
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < attackRecipes.Count; i++)
            {
                if (attackRecipes[i] == null) throw new ArgumentException("Attack recipe cannot be null.", nameof(attackRecipes));
                AttackRecipeValidationReport report = AttackRecipeValidator.Validate(attackRecipes[i], AttackRecipeValidationOptions.RuntimeFriendly);
                if (!report.IsValid) throw new ArgumentException("Attack recipe is invalid: " + GetFirstValidationError(report), nameof(attackRecipes));
                if (!seen.Add(attackRecipes[i].Id.Trim())) throw new ArgumentException("Duplicate attack recipe ID: " + attackRecipes[i].Id, nameof(attackRecipes));
                definitions[i] = attackRecipes[i].ToRuntimeDefinition();
            }

            return definitions;
        }

        public static RunUpgradeCatalog CreateRunUpgradeCatalog(IReadOnlyList<RunUpgradeDefinitionAsset> upgradeDefinitions = null)
        {
            if (upgradeDefinitions == null || upgradeDefinitions.Count == 0)
                return CreateDefaultRunUpgradeCatalog();
            return new RunUpgradeCatalog(CreateRunUpgradeDefinitions(upgradeDefinitions));
        }

        public static RunUpgradeCatalog CreateRunUpgradeCatalogOrEmpty(IReadOnlyList<RunUpgradeDefinitionAsset> upgradeDefinitions)
        {
            if (upgradeDefinitions == null || upgradeDefinitions.Count == 0)
                return new RunUpgradeCatalog(Array.Empty<RunUpgradeDefinition>());
            return new RunUpgradeCatalog(CreateRunUpgradeDefinitions(upgradeDefinitions));
        }

        public static RunUpgradeDefinitionAsset[] CreateRunUpgradeDefinitionAssets(IReadOnlyList<WeaponDefinitionAsset> weaponDefinitions = null)
        {
            weaponDefinitions ??= CreateWeaponDefinitionAssets(CreateAttackRecipes());
            WeaponDefinitionAsset shard = FindWeaponDefinition(weaponDefinitions, ShardLauncherWeaponId.Value);
            return new[]
            {
                UpgradeAsset("upgrade.idle-auto-defense.damage-up", "Damage Boost", RunUpgradeAuthoringTargetKind.AttackDamage, RunUpgradeModifierType.Additive, 1.5, shard, "10,20,35", RunUpgradeRarity.Common, 6, 3),
                UpgradeAsset("upgrade.idle-auto-defense.fire-rate-up", "Fire Rate Boost", RunUpgradeAuthoringTargetKind.AttackRate, RunUpgradeModifierType.Additive, 1, shard, "8,16,28", RunUpgradeRarity.Common, 5, 3),
                UpgradeAsset("upgrade.idle-auto-defense.range-up", "Range Boost", RunUpgradeAuthoringTargetKind.Range, RunUpgradeModifierType.Additive, 1.25, shard, "12,24,36", RunUpgradeRarity.Common, 4, 3),
                UpgradeAsset("upgrade.idle-auto-defense.projectile-speed-up", "Projectile Speed", RunUpgradeAuthoringTargetKind.ProjectileSpeed, RunUpgradeModifierType.Multiplicative, 0.35, shard, "10,20,40", RunUpgradeRarity.Common, 5, 3),
                UpgradeAsset("upgrade.idle-auto-defense.objective-max-health-up", "Core Reinforcement", RunUpgradeAuthoringTargetKind.WeaponStat, RunUpgradeModifierType.Additive, 8, null, "14,28,42", RunUpgradeRarity.Uncommon, 3, 3, "objective.idle-auto-defense.core", "idle-auto-defense.objective.max_health"),
                UpgradeAsset("upgrade.idle-auto-defense.enemy-reward-up", "Credit Reward", RunUpgradeAuthoringTargetKind.EnemyReward, RunUpgradeModifierType.Multiplicative, 0.15, null, "16,32,48", RunUpgradeRarity.Uncommon, 3, 3, "reward.idle-auto-defense.run")
            };
        }

        public static RunUpgradeDefinitionAsset[] ResolveUpgradeDefinitionsForTemplate(IReadOnlyList<RunUpgradeDefinitionAsset> assignedDefinitions, out int rejectedDefinitionCount)
        {
            rejectedDefinitionCount = 0;
            if (assignedDefinitions == null || assignedDefinitions.Count == 0)
                return CreateRunUpgradeDefinitionAssets();

            var definitions = new List<RunUpgradeDefinitionAsset>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < assignedDefinitions.Count; i++)
            {
                RunUpgradeDefinitionAsset definition = assignedDefinitions[i];
                if (definition == null)
                {
                    rejectedDefinitionCount++;
                    continue;
                }

                RunUpgradeDefinitionValidationReport report = RunUpgradeDefinitionValidator.Validate(definition);
                if (!report.IsValid)
                {
                    rejectedDefinitionCount++;
                    continue;
                }

                string id = definition.Id.Trim();
                if (!seen.Add(id))
                {
                    rejectedDefinitionCount++;
                    continue;
                }

                definitions.Add(definition);
            }

            if (definitions.Count == 0)
                return CreateRunUpgradeDefinitionAssets();
            if (rejectedDefinitionCount > 0)
                return CreateRunUpgradeDefinitionAssets();

            return definitions.ToArray();
        }

        public static RunUpgradeDefinition[] CreateRunUpgradeDefinitions(IReadOnlyList<RunUpgradeDefinitionAsset> upgradeDefinitions)
        {
            if (upgradeDefinitions == null || upgradeDefinitions.Count == 0) throw new ArgumentException("At least one upgrade definition is required.", nameof(upgradeDefinitions));
            var definitions = new RunUpgradeDefinition[upgradeDefinitions.Count];
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < upgradeDefinitions.Count; i++)
            {
                if (upgradeDefinitions[i] == null) throw new ArgumentException("Upgrade definition cannot be null.", nameof(upgradeDefinitions));
                RunUpgradeDefinitionValidationReport report = RunUpgradeDefinitionValidator.Validate(upgradeDefinitions[i]);
                if (!report.IsValid) throw new ArgumentException("Upgrade definition is invalid.", nameof(upgradeDefinitions));
                if (!seen.Add(upgradeDefinitions[i].Id.Trim())) throw new ArgumentException("Duplicate upgrade definition ID: " + upgradeDefinitions[i].Id, nameof(upgradeDefinitions));
                definitions[i] = upgradeDefinitions[i].ToRuntimeDefinition();
            }

            return definitions;
        }

        private static RunUpgradeCatalog CreateDefaultRunUpgradeCatalog()
        {
            return new RunUpgradeCatalog(new[]
            {
                Upgrade("upgrade.idle-auto-defense.damage-up", "idle-auto-defense.direct.damage_bonus", PulseCannonWeaponId.Value, 1.5, RunUpgradeRarity.Common, 6, 5),
                Upgrade("upgrade.idle-auto-defense.fire-rate-up", "idle-auto-defense.weapon.fire_rate_intent", PulseCannonWeaponId.Value, 1, RunUpgradeRarity.Common, 5, 3),
                Upgrade("upgrade.idle-auto-defense.projectile-count-up", "idle-auto-defense.projectile.volley_intent", ShardLauncherWeaponId.Value, 1, RunUpgradeRarity.Uncommon, 3, 2),
                Upgrade("upgrade.idle-auto-defense.projectile-speed-up", "idle-auto-defense.projectile.speed_multiplier", ShardProjectileId.Value, 0.35, RunUpgradeRarity.Common, 5, 4),
                Upgrade("upgrade.idle-auto-defense.objective-max-health-up", "idle-auto-defense.objective.max_health", "objective.idle-auto-defense.core", 6, RunUpgradeRarity.Uncommon, 3, 3),
                Upgrade("upgrade.idle-auto-defense.objective-repair", "idle-auto-defense.objective.heal", "objective.idle-auto-defense.core", 5, RunUpgradeRarity.Common, 5, 4),
                Upgrade("upgrade.idle-auto-defense.shield-restore-intent", "idle-auto-defense.objective.shield_restore_intent", "objective.idle-auto-defense.core", 4, RunUpgradeRarity.Uncommon, 2, 2),
                Upgrade("upgrade.idle-auto-defense.enemy-reward-up", "idle-auto-defense.reward.credits_multiplier", "reward.idle-auto-defense.run", 0.15, RunUpgradeRarity.Uncommon, 3, 3),
                Upgrade("upgrade.idle-auto-defense.offline-gain-up", "idle-auto-defense.offline.credits_multiplier", "offline.idle-auto-defense.credits", 0.10, RunUpgradeRarity.Common, 4, 3),
                Upgrade("upgrade.idle-auto-defense.reroll-bonus", "idle-auto-defense.reroll.bonus_intent", "monetization.idle-auto-defense.reroll", 1, RunUpgradeRarity.Rare, 2, 1),
                Upgrade("upgrade.idle-auto-defense.crit-chance-intent", "idle-auto-defense.attack.crit_chance_intent", PulseCannonWeaponId.Value, 0.05, RunUpgradeRarity.Rare, 2, 2),
                Upgrade("upgrade.idle-auto-defense.crit-damage-intent", "idle-auto-defense.attack.crit_damage_intent", PulseCannonWeaponId.Value, 0.20, RunUpgradeRarity.Rare, 2, 2),
                Upgrade("upgrade.idle-auto-defense.direct-specialization", "idle-auto-defense.direct.damage_bonus", PulseCannonWeaponId.Value, 3, RunUpgradeRarity.Epic, 1, 1, new[] { new RunUpgradeId("upgrade.idle-auto-defense.damage-up") }),
                Upgrade("upgrade.idle-auto-defense.projectile-specialization", "idle-auto-defense.projectile.speed_multiplier", ShardProjectileId.Value, 0.75, RunUpgradeRarity.Epic, 1, 1, new[] { new RunUpgradeId("upgrade.idle-auto-defense.projectile-speed-up") })
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
            return new AttackSourceSnapshot(new AttackSourceId("source.idle-auto-defense." + suffix), new CombatantId("objective.idle-auto-defense.core"));
        }

        private static AutoDefenseMountDefinition[] CreateAutoDefenseMountDefinitions(IReadOnlyList<WeaponDefinitionAsset> weapons)
        {
            var mounts = new AutoDefenseMountDefinition[weapons.Count];
            float spacing = weapons.Count <= 1 ? 0f : 3.2f / Math.Max(1, weapons.Count - 1);
            float start = weapons.Count <= 1 ? 0f : -1.6f;
            for (int i = 0; i < weapons.Count; i++)
            {
                WeaponDefinitionAsset weapon = weapons[i];
                string id = weapon == null || string.IsNullOrWhiteSpace(weapon.Id)
                    ? "weapon.idle-auto-defense.missing." + i.ToString(CultureInfo.InvariantCulture)
                    : weapon.Id.Trim();
                var weaponId = new WeaponDefinitionId(id);
                var mountId = new AutoDefenseMountId("mount.idle-auto-defense." + SanitizeRuntimeSegment(id));
                var slotId = new WeaponSlotId("slot.idle-auto-defense." + SanitizeRuntimeSegment(id));
                mounts[i] = new AutoDefenseMountDefinition(mountId, new Vector3(start + spacing * i, 0f, 0f), slotId, weaponId, enabled: false);
            }

            return mounts;
        }

        private static AutoDefenseWeaponModuleDefinition[] CreateAutoDefenseWeaponModuleDefinitions(IReadOnlyList<WeaponDefinitionAsset> weapons, IReadOnlyList<AutoDefenseMountDefinition> mounts)
        {
            var modules = new AutoDefenseWeaponModuleDefinition[weapons.Count];
            for (int i = 0; i < weapons.Count; i++)
            {
                string suffix = weapons[i] == null || string.IsNullOrWhiteSpace(weapons[i].Id)
                    ? i.ToString(CultureInfo.InvariantCulture)
                    : SanitizeRuntimeSegment(weapons[i].Id);
                modules[i] = new AutoDefenseWeaponModuleDefinition(mounts[i].Id, weapons[i].ToRuntimeDefinition(), Source(suffix));
            }

            return modules;
        }

        private static WeaponDefinitionAsset[] CopyWeaponDefinitions(IReadOnlyList<WeaponDefinitionAsset> weapons)
        {
            if (weapons == null || weapons.Count == 0) return Array.Empty<WeaponDefinitionAsset>();
            var copy = new WeaponDefinitionAsset[weapons.Count];
            for (int i = 0; i < weapons.Count; i++) copy[i] = weapons[i];
            return copy;
        }

        private static AttackDefinitionAsset FindAttackRecipe(IReadOnlyList<AttackDefinitionAsset> recipes, string id)
        {
            if (recipes == null || string.IsNullOrWhiteSpace(id)) return null;
            for (int i = 0; i < recipes.Count; i++)
            {
                AttackDefinitionAsset recipe = recipes[i];
                if (recipe != null && string.Equals(recipe.Id, id, StringComparison.OrdinalIgnoreCase))
                    return recipe;
            }

            return null;
        }

        private static WeaponDefinitionAsset FindWeaponDefinition(IReadOnlyList<WeaponDefinitionAsset> weapons, string id)
        {
            if (weapons == null || string.IsNullOrWhiteSpace(id)) return null;
            for (int i = 0; i < weapons.Count; i++)
            {
                WeaponDefinitionAsset weapon = weapons[i];
                if (weapon != null && string.Equals(weapon.Id, id, StringComparison.OrdinalIgnoreCase))
                    return weapon;
            }

            return null;
        }

        private static RunUpgradeDefinitionAsset UpgradeAsset(
            string id,
            string displayName,
            RunUpgradeAuthoringTargetKind targetKind,
            RunUpgradeModifierType modifierType,
            double amount,
            WeaponDefinitionAsset weapon,
            string costsCsv,
            RunUpgradeRarity rarity,
            int weight,
            int maxRank,
            string targetIdOverride = "",
            string effectIdOverride = "")
        {
            string targetId = string.IsNullOrWhiteSpace(targetIdOverride)
                ? weapon == null ? string.Empty : weapon.Id
                : targetIdOverride;
            return RunUpgradeDefinitionAsset.CreateTransient(
                id,
                displayName,
                rarity,
                weight,
                maxRank,
                new[]
                {
                    new RunUpgradeEffectRecipe(targetKind, modifierType, amount, weapon: weapon, targetIdOverride: targetId, effectIdOverride: effectIdOverride)
                },
                ParseIntCsv(costsCsv),
                displayName + " authored upgrade.",
                new[] { "idle-auto-defense", "upgrade" });
        }

        private static bool WeaponAttackExists(WeaponDefinitionAsset weapon, IReadOnlyList<AttackDefinitionAsset> attackRecipes)
        {
            if (weapon == null || weapon.Stats == null || weapon.Stats.Attack == null) return false;
            return FindAttackRecipe(attackRecipes, weapon.Stats.Attack.Id) != null;
        }

        private static int CountMissingRequiredTemplateWeaponIds(IReadOnlyList<WeaponDefinitionAsset> weapons)
        {
            int missing = 0;
            for (int i = 0; i < RequiredTemplateWeaponIds.Length; i++)
                if (!ContainsWeaponDefinitionId(weapons, RequiredTemplateWeaponIds[i]))
                    missing++;
            return missing;
        }

        private static bool ContainsWeaponDefinitionId(IReadOnlyList<WeaponDefinitionAsset> weapons, string id)
        {
            return FindWeaponDefinition(weapons, id) != null;
        }

        private static int[] ParseIntCsv(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) return Array.Empty<int>();
            string[] parts = csv.Split(',');
            var values = new List<int>();
            for (int i = 0; i < parts.Length; i++)
                if (int.TryParse(parts[i].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                    values.Add(value);
            return values.ToArray();
        }

        public static string SanitizeContentSetOperationSegment(string value)
        {
            return SanitizeRuntimeSegment(value);
        }

        private static string SanitizeRuntimeSegment(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "unnamed";
            var chars = new char[value.Length];
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                chars[i] = char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '_' ? c : '-';
            }

            return new string(chars).Trim('-', '.', '_');
        }

        private static AutoDefenseEnemyDefinition[] CreateDefaultAutoDefenseEnemyDefinitions()
        {
            return new[]
            {
                Enemy(SwarmEnemySpawnableId, 20, 0.72f, 1, 0.28f),
                Enemy(RunnerEnemySpawnableId, 26, 1.1f, 2, 0.27f),
                Enemy(TankEnemySpawnableId, 74, 0.48f, 5, 0.48f),
                Enemy(ShieldedEnemySpawnableId, 46, 0.64f, 4, 0.38f),
                Enemy(EliteEnemySpawnableId, 170, 0.54f, 10, 0.54f),
                Enemy(BossEnemySpawnableId, 390, 0.36f, 22, 0.82f)
            };
        }

        private static void AddDamageType(List<DamageTypeDefinition> damageTypes, HashSet<string> seen, DamageTypeId id)
        {
            if (!id.IsEmpty && seen.Add(id.Value)) damageTypes.Add(new DamageTypeDefinition(id));
        }

        private static int CountMissingRequiredTemplateAttackIds(IReadOnlyList<AttackDefinitionAsset> recipes)
        {
            int missing = 0;
            for (int i = 0; i < RequiredTemplateAttackIds.Length; i++)
                if (!ContainsRecipeId(recipes, RequiredTemplateAttackIds[i]))
                    missing++;
            return missing;
        }

        private static int CountMissingRequiredTemplateEnemyIds(IReadOnlyList<EnemyDefinitionAsset> recipes)
        {
            int missing = 0;
            for (int i = 0; i < RequiredTemplateEnemyIds.Length; i++)
                if (!ContainsEnemyRecipeId(recipes, RequiredTemplateEnemyIds[i]))
                    missing++;
            return missing;
        }

        private static bool ContainsRecipeId(IReadOnlyList<AttackDefinitionAsset> recipes, string id)
        {
            for (int i = 0; i < recipes.Count; i++)
            {
                AttackDefinitionAsset recipe = recipes[i];
                if (recipe != null && string.Equals(recipe.Id, id, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool ContainsEnemyRecipeId(IReadOnlyList<EnemyDefinitionAsset> recipes, string id)
        {
            for (int i = 0; i < recipes.Count; i++)
            {
                EnemyDefinitionAsset recipe = recipes[i];
                if (recipe != null && string.Equals(recipe.Id, id, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool WaveReferencesKnownEnemies(WaveDefinitionAsset wave, HashSet<string> enemyIds)
        {
            if (wave == null || wave.Entries == null) return false;
            IReadOnlyList<WaveEntryRecipe> entries = wave.Entries.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                WaveEntryRecipe entry = entries[i];
                if (entry == null || entry.Enemy == null || string.IsNullOrWhiteSpace(entry.Enemy.Id)) return false;
                if (!enemyIds.Contains(entry.Enemy.Id.Trim())) return false;
            }

            return true;
        }

        private static string GetFirstValidationError(AttackRecipeValidationReport report)
        {
            for (int i = 0; i < report.Issues.Count; i++)
                if (report.Issues[i].IsError)
                    return report.Issues[i].Path + ": " + report.Issues[i].Message;
            return "Unknown validation error.";
        }

        private static string GetFirstValidationError(ContentAuthoringValidationReport report)
        {
            for (int i = 0; i < report.Issues.Count; i++)
                if (report.Issues[i].IsError)
                    return report.Issues[i].Path + ": " + report.Issues[i].Message;
            return "Unknown validation error.";
        }
    }

    public static class IdleAutoDefenseTemplateMonetization
    {
        private const string InterstitialCooldownGroup = "idle-auto-defense.interstitial.global";

        public static readonly MonetizationPlacementId DoubleRunReward = new MonetizationPlacementId("idle-auto-defense.rewarded.double-run-reward");
        public static readonly MonetizationPlacementId ReviveAfterFailure = new MonetizationPlacementId("idle-auto-defense.rewarded.revive-after-failure");
        public static readonly MonetizationPlacementId RerollUpgradeDraft = new MonetizationPlacementId("idle-auto-defense.rewarded.reroll-upgrade-draft");
        public static readonly MonetizationPlacementId DoubleOfflineReward = new MonetizationPlacementId("idle-auto-defense.rewarded.double-offline-reward");
        public static readonly MonetizationPlacementId SmallCurrencyBonus = new MonetizationPlacementId("idle-auto-defense.rewarded.small-currency-bonus");
        public static readonly MonetizationPlacementId InterstitialAfterRunCompletion = new MonetizationPlacementId("idle-auto-defense.interstitial.after-run-completion");
        public static readonly MonetizationPlacementId InterstitialAfterRunFailure = new MonetizationPlacementId("idle-auto-defense.interstitial.after-run-failure");

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

    public enum IdleAutoDefenseRewardRarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4
    }

    public enum IdleAutoDefenseRewardDraftKind
    {
        LevelUp = 0,
        WaveComplete = 1,
        EliteDefeated = 2,
        BossDefeated = 3
    }

    public enum IdleAutoDefenseRewardEffectKind
    {
        None = 0,
        UnlockWeapon = 1,
        DamageRank = 2,
        FireRateRank = 3,
        RangeRank = 4,
        Repair = 5,
        RewardMultiplier = 6,
        ProjectileSpeed = 7,
        ExtraProjectile = 8,
        PulsePower = 9,
        ArcPower = 10,
        HomingPower = 11,
        GlobalDamageMultiplier = 12
    }

    public sealed class IdleAutoDefenseRewardDraftChoice
    {
        internal IdleAutoDefenseRewardDraftChoice(
            string id,
            string displayName,
            IdleAutoDefenseRewardRarity rarity,
            string typeName,
            string targetName,
            string effectDescription,
            string hotkeyLabel,
            bool isUnlock,
            string targetWeaponId,
            IdleAutoDefenseRewardEffectKind effectKind,
            double amount,
            string dedupeKey,
            double weight = 1d)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Rarity = rarity;
            TypeName = typeName ?? string.Empty;
            TargetName = targetName ?? string.Empty;
            EffectDescription = effectDescription ?? string.Empty;
            HotkeyLabel = hotkeyLabel ?? string.Empty;
            IsUnlock = isUnlock;
            TargetWeaponId = targetWeaponId ?? string.Empty;
            EffectKind = effectKind;
            Amount = amount;
            DedupeKey = dedupeKey ?? Id;
            Weight = weight;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public IdleAutoDefenseRewardRarity Rarity { get; }
        public string RarityName => Rarity.ToString();
        public string TypeName { get; }
        public string TargetName { get; }
        public string EffectDescription { get; }
        public string HotkeyLabel { get; }
        public bool IsUnlock { get; }
        public double AuthoredAmount => Amount;
        public double AuthoredWeight => Weight;
        public string TargetWeaponId { get; }
        public IdleAutoDefenseRewardEffectKind EffectKind { get; }
        public string EffectKindName => EffectKind.ToString();
        internal double Amount { get; }
        internal string DedupeKey { get; }
        internal double Weight { get; }
    }

    public readonly struct IdleAutoDefenseMajorThreatSnapshot
    {
        public IdleAutoDefenseMajorThreatSnapshot(
            long instanceId,
            string displayName,
            bool boss,
            double health,
            double maximumHealth,
            Vector3 worldPosition)
        {
            InstanceId = instanceId;
            DisplayName = displayName ?? string.Empty;
            Boss = boss;
            Health = Math.Max(0d, health);
            MaximumHealth = Math.Max(1d, maximumHealth);
            WorldPosition = worldPosition;
        }

        public long InstanceId { get; }
        public string DisplayName { get; }
        public bool Boss { get; }
        public double Health { get; }
        public double MaximumHealth { get; }
        public float HealthNormalized => Mathf.Clamp01((float)(Health / MaximumHealth));
        public Vector3 WorldPosition { get; }
    }

    public class IdleAutoDefenseTemplateController : MonoBehaviour
    {
        private readonly SpawnRequest[] _spawnBuffer = new SpawnRequest[16];
        private const long DefaultRuntimeStartingCredits = 10;
        private const long KillRewardCredits = 5;
        private const int PassiveIncomeIntervalTicks = 60;
        private const int ManualTowerBaseCooldownTicks = 34;
        private const int ManualTowerMinimumCooldownTicks = 18;
        private const double ManualTowerBaseDamage = 3.2d;
        private const double ManualTowerDamageRankBonus = 1.6d;
        private const double ManualTowerBaseRange = 8.4d;
        private const double ManualTowerRangeRankBonus = 0.4d;
        private const double ManualTowerMaximumRange = 10.6d;
        private const double PulseBeamModuleBaseRange = 7.4d;
        private const double ArcBurstModuleBaseRange = 6.2d;
        private const double HomingPulseModuleBaseRange = 8.8d;
        private const double ModuleRangeRankBonus = 0.35d;
        private const double SampleProjectileFinishThreshold = 3d;
        private const float TemplateSpawnLaneRadius = 18.5f;
        private const float TemplateVisibleArenaRadius = 14.75f;
        private const int PulseBeamModuleUnlockCost = 34;
        private const int ArcBurstModuleUnlockCost = 62;
        private const int HomingPulseModuleUnlockCost = 78;
        private const int PulseBeamModuleCooldownTicks = 72;
        private const int ArcBurstModuleCooldownTicks = 108;
        private const int HomingPulseModuleCooldownTicks = 92;
        private const int MinimumProjectileImpactDelayTicks = 12;
        private const int MaximumProjectileImpactDelayTicks = 52;
        private const float FirstRewardDraftTargetSeconds = 30f;
        private const int OverdriveCostCredits = 22;
        private const float OverdriveDurationSeconds = 7f;
        private const float OverdriveCooldownSeconds = 18f;
        private const int OverdriveCooldownBonusTicks = 10;
        private const double OverdriveDamageMultiplier = 1.55d;
        private const string KenneyResourceRoot = "Kenney/IdleAutoDefense/";
        private const string Kenney3DResourceRoot = KenneyResourceRoot + "Models/TowerDefenseKit/FBX/";
        private const float RuntimeUiFallbackWidth = 1280f;
        private const float RuntimeUiFallbackHeight = 720f;
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
        [SerializeField] protected GameContentPackAsset _contentPack;
        [SerializeField] protected GameContentSetAsset _contentSet;
        [SerializeField] protected AttackDefinitionAsset[] _attackRecipes = Array.Empty<AttackDefinitionAsset>();
        [SerializeField] protected EnemyDefinitionAsset[] _enemyDefinitions = Array.Empty<EnemyDefinitionAsset>();
        [SerializeField] protected WaveDefinitionAsset[] _waveDefinitions = Array.Empty<WaveDefinitionAsset>();
        [SerializeField] protected WeaponDefinitionAsset[] _weaponDefinitions = Array.Empty<WeaponDefinitionAsset>();
        [SerializeField] protected RunUpgradeDefinitionAsset[] _upgradeDefinitions = Array.Empty<RunUpgradeDefinitionAsset>();
        [SerializeField] private IdleAutoDefenseRewardDraftSettings _rewardDraftSettings = IdleAutoDefenseRewardDraftSettings.CreateDefault();
        [SerializeField] private IdleAutoDefenseRewardDraftCatalog _rewardDraftCatalog = IdleAutoDefenseRewardDraftCatalog.CreateDefault();
        [SerializeField] private bool _requireAuthoredContent;
        [SerializeField] private bool _showDebugAimLines;
        [SerializeField] private bool _showDebugRanges;
        [SerializeField] private bool _showDebugSpawnRing;
        private AttackDefinitionAsset[] _resolvedAttackRecipes = Array.Empty<AttackDefinitionAsset>();
        private EnemyDefinitionAsset[] _resolvedEnemyDefinitions = Array.Empty<EnemyDefinitionAsset>();
        private WaveDefinitionAsset[] _resolvedWaveDefinitions = Array.Empty<WaveDefinitionAsset>();
        private WeaponDefinitionAsset[] _resolvedWeaponDefinitions = Array.Empty<WeaponDefinitionAsset>();
        private RunUpgradeDefinitionAsset[] _resolvedUpgradeDefinitions = Array.Empty<RunUpgradeDefinitionAsset>();
        private GameContentSetResolution _resolvedContentSet;
        private IdleAutoDefenseRewardCatalogAsset _activeRewardCatalog;
        private IdleAutoDefenseEconomyAsset _activeEconomy;
        private IdleAutoDefenseRunProfileAsset _activeRunProfile;
        private IdleAutoDefenseProgressionAsset _activeProgression;
        private IdleAutoDefenseOfflineProgressionAsset _activeOfflineProgression;
        private IdleAutoDefenseGameRulesAsset _activeGameRules;
        private float _simulationTickAccumulator;
        private int _sessionElapsedTicks;
        private int _runSequence;
        private bool _endlessRestartPending;
        private GameObject _enemyPrefab;
        private GameObject _projectilePrefab;
        private GameObject _root;
        private readonly Dictionary<string, GameObject> _runtimeEnemyPrefabs = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, GameObject> _runtimeProjectilePrefabs = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, IdleAutoDefenseWeaponVisualBinding> _weaponVisualBindings = new Dictionary<string, IdleAutoDefenseWeaponVisualBinding>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<long, IdleAutoDefenseEnemyModelPresentation> _enemyPresentationsById = new Dictionary<long, IdleAutoDefenseEnemyModelPresentation>();
        private ProjectileDefinition[] _resolvedProjectileDefinitions = Array.Empty<ProjectileDefinition>();
        private readonly List<PendingProjectileImpact> _pendingProjectileImpacts = new List<PendingProjectileImpact>();
        private readonly HashSet<long> _seenEnemyIds = new HashSet<long>();
        private readonly HashSet<long> _enemyDeathPresentationIds = new HashSet<long>();
        private readonly Dictionary<long, double> _sampleEnemyDamageById = new Dictionary<long, double>();
        private readonly HashSet<long> _rewardedEnemyDefeatIds = new HashSet<long>();
        private readonly HashSet<string> _rewardedCompletedWaveIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Queue<IdleAutoDefenseRewardDraftKind> _queuedRewardDrafts = new Queue<IdleAutoDefenseRewardDraftKind>();
        private readonly Dictionary<string, int> _weaponNormalUpgradeRanks = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _weaponEpicUpgradeRanks = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _weaponLegendaryUnlocks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _selectedRewardIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _baseRewardRanks = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly List<DamageNumberView> _damageNumbers = new List<DamageNumberView>();
        private readonly List<ActiveBeamVisual> _activeBeamVisuals = new List<ActiveBeamVisual>();
        private readonly Dictionary<long, Vector3> _lastProjectileAgentPositions = new Dictionary<long, Vector3>();
        private UIDocument _runtimeUiDocument;
        private PanelSettings _runtimePanelSettings;
        private ThemeStyleSheet _runtimeThemeStyleSheet;
        private GameObject _runtimeUiObject;
        private VisualElement _runtimeUiRoot;
        private VisualElement _damageNumberLayer;
        private AudioSource _runtimeAudioSource;
        private AudioClip _fallbackPresentationClip;
        private bool _fallbackPresentationClipIsRuntimeOwned;
        private Camera _shakeCamera;
        private Vector3 _shakeCameraBaseLocalPosition;
        private bool _shakeCameraBaseCaptured;
        private float _cameraShakeSecondsRemaining;
        private float _cameraShakeDuration;
        private float _cameraShakeMagnitude;
        private MonetizationSession _monetizationSession;
        private int _manualTowerCooldownTicks;
        private int _passiveIncomeTicks;
        private int _pulseBeamModuleCooldownTicks;
        private int _arcBurstModuleCooldownTicks;
        private int _homingPulseModuleCooldownTicks;
        private IdleAutoDefenseRewardDraftChoice[] _rewardDraftChoices = Array.Empty<IdleAutoDefenseRewardDraftChoice>();
        private IdleAutoDefenseRewardDraftKind _activeRewardDraftKind;
        private int _rewardDraftSeed;
        private int _shardVolleyBonus;
        private int _pulseBeamBonus;
        private int _arcBurstBonus;
        private int _homingPulseBonus;
        private double _rewardDamageMultiplierBonus;
        private long _pendingAuthoredKillCredits;
        private bool _starterRewardDraftOffered;
        private float _overdriveSecondsRemaining;
        private float _overdriveCooldownSecondsRemaining;
        private float _minimumEnemySpawnDistance = float.MaxValue;
        private float _closestEnemyDistanceToObjective = float.MaxValue;

        public AutoDefenseRuntime Runtime => _runtime;
        public MonetizationSession MonetizationSession
        {
            get => _monetizationSession ??= IdleAutoDefenseTemplateMonetization.CreateMockSession();
            set => _monetizationSession = value;
        }

        public string RuntimeStateName => RunProfileVictoryReached ? AutoDefenseRuntimeState.Completed.ToString() : RuntimeState.ToString();
        public int SpawnedCount { get; private set; }
        public int DirectOrCombatKillCount { get; private set; }
        public int ProjectileLaunchCount { get; private set; }
        public int ProjectileAdapterKillCount { get; private set; }
        public int ProjectileVisualSpawnCount { get; private set; }
        public int AuthoredProjectileVisualSpawnCount { get; private set; }
        public int ProjectileMotionObservedCount { get; private set; }
        public int ProjectileDamageAppliedCount { get; private set; }
        public int ProjectileImpactCallbackCount { get; private set; }
        public int ProjectileDamageResolvedFromImpactCount { get; private set; }
        public int ProjectileImpactRetargetCount { get; private set; }
        public int ProjectileImpactMissCount { get; private set; }
        public int ProjectileImpactRejectedCount { get; private set; }
        public int ProjectileExpiryDeferralCount { get; private set; }
        public int AttackVfxSpawnCount { get; private set; }
        public int BeamVisualSpawnCount { get; private set; }
        public int BeamVisualInvalidEndpointCount { get; private set; }
        public int AttackAudioPlayCount { get; private set; }
        public int EnemyPresentationEventCount { get; private set; }
        public int Kenney3DModelSpawnCount { get; private set; }
        public int TurretAimUpdateCount { get; private set; }
        public int MuzzleProjectileLaunchCount { get; private set; }
        public int MuzzleFlashSpawnCount { get; private set; }
        public int RecoilEventCount { get; private set; }
        public int EnemyFacingUpdateCount { get; private set; }
        public int EnemyHitFlashCount { get; private set; }
        public int EnemyDeathPopCount { get; private set; }
        public int DamageNumberSpawnCount { get; private set; }
        public int AuthoredVisibleInstanceStampCount { get; private set; }
        public int FallbackVisibleGameplaySpawnCount { get; private set; }
        public int AuthoredWeaponPresentationSpawnCount { get; private set; }
        public int FallbackWeaponPresentationSpawnCount { get; private set; }
        public int AuthoredWeaponPresentationBindingCount { get; private set; }
        public int FallbackWeaponPresentationBindingCount { get; private set; }
        public int AuthoredObjectivePresentationBindingCount { get; private set; }
        public int FallbackObjectivePresentationBindingCount { get; private set; }
        public int AuthoredModuleSlotPresentationBindingCount { get; private set; }
        public int FallbackModuleSlotPresentationBindingCount { get; private set; }
        public bool UsingContentSetRuntimeSettings { get; private set; }
        public int DebugAimTracerSpawnCount { get; private set; }
        public int EnemyDamageSurvivedCount { get; private set; }
        public int RangeRejectedTargetCount { get; private set; }
        public int EnemiesSpawnedBeyondStartingRangeCount { get; private set; }
        public int EliteOrBossSpawnCount { get; private set; }
        public float MinimumEnemySpawnDistance => _minimumEnemySpawnDistance == float.MaxValue ? 0f : _minimumEnemySpawnDistance;
        public float ClosestEnemyDistanceToObjective => _closestEnemyDistanceToObjective == float.MaxValue ? 0f : _closestEnemyDistanceToObjective;
        public bool RuntimeUiDocumentReady => _runtimeUiDocument != null && _runtimeUiRoot != null && _damageNumberLayer != null;
        public bool RuntimeUiThemeAssigned => _runtimePanelSettings != null && (_runtimePanelSettings.themeStyleSheet != null || RuntimeUiDirectStylesApplied);
        public bool RuntimeUiDirectStylesApplied { get; private set; }
        public int RuntimeDamageNumberVisibleCount => _damageNumbers.Count;
        public float RuntimeUiRootResolvedWidth => ResolveRuntimePanelSize().x;
        public float RuntimeUiRootResolvedHeight => ResolveRuntimePanelSize().y;
        public bool ShowDebugAimLines
        {
            get => _showDebugAimLines;
            set => _showDebugAimLines = value;
        }

        public bool ShowDebugRanges
        {
            get => _showDebugRanges;
            set => _showDebugRanges = value;
        }

        public bool ShowDebugSpawnRing
        {
            get => _showDebugSpawnRing;
            set => _showDebugSpawnRing = value;
        }
        public int InvalidAssignedRecipeCount { get; private set; }
        public int InvalidAssignedEnemyCount { get; private set; }
        public int InvalidAssignedWaveCount { get; private set; }
        public int InvalidAssignedWeaponCount { get; private set; }
        public int InvalidAssignedUpgradeCount { get; private set; }
        public int InvalidAssignedContentPackIssueCount { get; private set; }
        public int InvalidAssignedContentSetIssueCount { get; private set; }
        public bool UsingAssignedContentPack { get; private set; }
        public bool UsingAssignedContentSet { get; private set; }
        public bool StrictAuthoredStartup => _requireAuthoredContent;
        public bool StartupBlocked { get; private set; }
        public string StartupError { get; private set; } = string.Empty;
        public bool FallbackModeActive { get; private set; }
        public bool UsingAuthoredCore => UsingAssignedContentSet && !FallbackModeActive &&
            _activeRewardCatalog != null && _activeEconomy != null && _activeRunProfile != null &&
            _activeProgression != null && _activeOfflineProgression != null && _activeGameRules != null;
        public string ActiveContentPackId => _contentPack == null ? string.Empty : _contentPack.Id;
        public string ActiveContentPackDisplayName => _contentPack == null ? string.Empty : _contentPack.DisplayName;
        public string ActiveContentSetId => _resolvedContentSet != null && _resolvedContentSet.IsValid && _resolvedContentSet.ContentSet != null
            ? _resolvedContentSet.ContentSet.Id
            : string.Empty;
        public string ActiveRewardCatalogId => _activeRewardCatalog == null ? string.Empty : _activeRewardCatalog.Id;
        public string ActiveEconomyId => _activeEconomy == null ? string.Empty : _activeEconomy.Id;
        public string ActiveRunProfileId => _activeRunProfile == null ? string.Empty : _activeRunProfile.Id;
        public string ActiveProgressionId => _activeProgression == null ? string.Empty : _activeProgression.Id;
        public string ActiveOfflineProgressionId => _activeOfflineProgression == null ? string.Empty : _activeOfflineProgression.Id;
        public string ActiveGameRulesId => _activeGameRules == null ? string.Empty : _activeGameRules.Id;
        public int SessionElapsedTicks => _sessionElapsedTicks;
        public int SessionLengthTicks => _activeRunProfile == null ? 0 : _activeRunProfile.SessionLengthTicks;
        public double SessionLengthSeconds => _activeRunProfile == null ? 0d : _activeRunProfile.SessionLengthSeconds;
        public int SimulationTicksPerSecond => _activeRunProfile == null ? 0 : _activeRunProfile.SimulationTicksPerSecond;
        public bool EndlessEnabled => _activeRunProfile != null && _activeRunProfile.Endless;
        public string AssignedContentPackStatus { get; private set; } = string.Empty;
        public string AssignedContentSetStatus { get; private set; } = string.Empty;
        public int ObjectiveReachCount { get; private set; }
        public int ObjectiveDamageEvents { get; private set; }
        public int DraftTickCount { get; private set; }
        public int SelectedUpgradeCount { get; private set; }
        public int RewardDraftOpenedCount { get; private set; }
        public int RewardDraftSelectionCount { get; private set; }
        public float FirstRewardDraftSeconds { get; private set; } = -1f;
        public int LevelUpRewardDraftCount { get; private set; }
        public int EliteRewardDraftCount { get; private set; }
        public int BossRewardDraftCount { get; private set; }
        public int WaveRewardExperienceCount { get; private set; }
        public int EpicRewardSelectionCount { get; private set; }
        public int LegendaryRewardSelectionCount { get; private set; }
        public int EliteDefeatCount { get; private set; }
        public int BossDefeatCount { get; private set; }
        public int UpgradeFeedbackSpawnCount { get; private set; }
        public int CommanderLevel { get; private set; } = 1;
        public long CommanderExperience { get; private set; }
        public long ExperienceToNextLevel => RewardDraftSettings.CalculateExperienceToNextLevel(CommanderLevel);
        public bool RewardDraftPausesCombat { get; set; } = true;
        public bool RewardDraftActive => _rewardDraftChoices.Length > 0;
        public IReadOnlyList<IdleAutoDefenseRewardDraftChoice> RewardDraftChoices => _rewardDraftChoices;
        public int RewardDraftChoiceCount => _rewardDraftChoices.Length;
        public string ActiveRewardDraftKindName => RewardDraftActive ? _activeRewardDraftKind.ToString() : string.Empty;
        public double DirectDamageBonus { get; private set; }
        public double ProjectileSpeedMultiplier { get; private set; } = 1d;
        public int EnemySpawnDelayTicks { get; private set; }
        public double RewardCreditMultiplierBonus { get; private set; }
        public double OfflineRewardMultiplierBonus { get; private set; }
        public long RuntimeCurrency { get; private set; }
        public long RuntimeCurrencyEarned { get; private set; }
        public long RuntimeCurrencySpent { get; private set; }
        public float SurvivalSeconds { get; private set; }
        public int DamageUpgradeRank { get; private set; }
        public int AttackSpeedUpgradeRank { get; private set; }
        public int RangeUpgradeRank { get; private set; }
        public int RepairUpgradeRank { get; private set; }
        public int UnsupportedUpgradeIntentCount { get; private set; }
        public long OfflineRewardCredits { get; private set; }
        public long OfflineRewardParts { get; private set; }
        public long EncounterRewardCredits { get; private set; }
        public long EncounterRewardParts { get; private set; }
        public IdleProgressionResultCode LastOfflineRewardCode { get; private set; } = IdleProgressionResultCode.NoElapsedTime;
        public bool ReviveOfferAccepted { get; private set; }
        public bool RunProfileVictoryReached { get; private set; }
        public bool EncounterCompleted => RunProfileVictoryReached || _runtime != null && _runtime.State == AutoDefenseRuntimeState.Completed;
        public bool EncounterFailed => _runtime != null && _runtime.State == AutoDefenseRuntimeState.Failed;
        public bool EncounterRunning => _runtime != null && _runtime.State == AutoDefenseRuntimeState.Running;
        public int ActiveEnemyCount => _runtime == null ? 0 : _runtime.ActiveEnemyCount;
        public int ObjectiveLivesRemaining => _runtime == null ? 0 : _runtime.Objective.LivesRemaining;
        public double ObjectiveHealth => _runtime == null ? 0d : _runtime.Objective.Health.CurrentHealth;
        public double ObjectiveMaximumHealth => _runtime == null ? 0d : _runtime.Objective.Health.MaximumHealth;
        public string ObjectiveHealthText => ObjectiveMaximumHealth <= 0d
            ? "--"
            : ObjectiveHealth.ToString("0", CultureInfo.InvariantCulture) + "/" + ObjectiveMaximumHealth.ToString("0", CultureInfo.InvariantCulture);
        public string CurrentSpawnProfileName => ResolveCurrentSpawnProfileName();
        public int DamageUpgradeCost => ResolveUpgradeCost(_activeEconomy == null ? IdleAutoDefenseEconomyAsset.DamageUpgradeCostId : _activeEconomy.DamageUpgradeCostCurveId, DamageUpgradeRank);
        public int AttackSpeedUpgradeCost => ResolveUpgradeCost(_activeEconomy == null ? IdleAutoDefenseEconomyAsset.FireRateUpgradeCostId : _activeEconomy.FireRateUpgradeCostCurveId, AttackSpeedUpgradeRank);
        public int RangeUpgradeCost => ResolveUpgradeCost(_activeEconomy == null ? IdleAutoDefenseEconomyAsset.RangeUpgradeCostId : _activeEconomy.RangeUpgradeCostCurveId, RangeUpgradeRank);
        public int RepairUpgradeCost => ResolveUpgradeCost(_activeEconomy == null ? IdleAutoDefenseEconomyAsset.RepairUpgradeCostId : _activeEconomy.RepairUpgradeCostCurveId, RepairUpgradeRank);
        public bool PulseBeamUnlocked { get; private set; }
        public bool ArcBurstUnlocked { get; private set; }
        public bool HomingPulseUnlocked { get; private set; }
        public int PulseBeamUnlockCost => ResolveModuleBuildCost(IdleAutoDefenseModuleRole.PrecisionBeam, PulseBeamModuleUnlockCost);
        public int ArcBurstUnlockCost => ResolveModuleBuildCost(IdleAutoDefenseModuleRole.AreaBurst, ArcBurstModuleUnlockCost);
        public int HomingPulseUnlockCost => ResolveModuleBuildCost(IdleAutoDefenseModuleRole.HomingProjectile, HomingPulseModuleUnlockCost);
        public int OverdriveCost => ResolveUpgradeCost(_activeEconomy == null ? IdleAutoDefenseEconomyAsset.OverdriveCostId : _activeEconomy.OverdriveCostCurveId, 0, OverdriveCostCredits);
        public bool OverdriveActive => _overdriveSecondsRemaining > 0f;
        public float OverdriveSecondsRemaining => Mathf.Max(0f, _overdriveSecondsRemaining);
        public float OverdriveCooldownSecondsRemaining => Mathf.Max(0f, _overdriveCooldownSecondsRemaining);
        public bool CanPurchasePulseBeamModule => !PulseBeamUnlocked && CanSpendRuntimeCurrency(PulseBeamUnlockCost);
        public bool CanPurchaseArcBurstModule => !ArcBurstUnlocked && CanSpendRuntimeCurrency(ArcBurstUnlockCost);
        public bool CanPurchaseHomingPulseModule => !HomingPulseUnlocked && CanSpendRuntimeCurrency(HomingPulseUnlockCost);
        public bool CanPurchaseOverdrive => !OverdriveActive && OverdriveCooldownSecondsRemaining <= 0f && CanSpendRuntimeCurrency(OverdriveCost);
        public int UnlockedModuleCount => 1 + (PulseBeamUnlocked ? 1 : 0) + (ArcBurstUnlocked ? 1 : 0) + (HomingPulseUnlocked ? 1 : 0);
        public IdleAutoDefenseRewardCatalogAsset ActiveRewardCatalog => _activeRewardCatalog;
        public IdleAutoDefenseEconomyAsset ActiveEconomy => _activeEconomy;
        public IdleAutoDefenseRunProfileAsset ActiveRunProfile => _activeRunProfile;
        public IdleAutoDefenseProgressionAsset ActiveProgression => _activeProgression;
        public IdleAutoDefenseOfflineProgressionAsset ActiveOfflineProgression => _activeOfflineProgression;
        public IdleAutoDefenseGameRulesAsset ActiveGameRules => _activeGameRules;
        public int TotalWaveCount => _resolvedWaveDefinitions.Length;
        public int CurrentWaveNumber => ResolveCurrentWaveNumber();
        public int ModuleActivationCount { get; private set; }
        public int OverdriveActivationCount { get; private set; }
        public bool CanPurchaseDamageUpgrade => CanSpendRuntimeCurrency(DamageUpgradeCost);
        public bool CanPurchaseAttackSpeedUpgrade => CanSpendRuntimeCurrency(AttackSpeedUpgradeCost);
        public bool CanPurchaseRangeUpgrade => CanSpendRuntimeCurrency(RangeUpgradeCost);
        public bool CanPurchaseRepairUpgrade => CanSpendRuntimeCurrency(RepairUpgradeCost);
        public string StatusSummary => "State=" + RuntimeState +
            " Spawned=" + SpawnedCount +
            " Kills=" + (DirectOrCombatKillCount + ProjectileAdapterKillCount) +
            " Projectiles=" + ProjectileLaunchCount +
            " ProjectileImpacts=" + ProjectileImpactCallbackCount +
            " BeamVisuals=" + BeamVisualSpawnCount +
            " Upgrades=" + SelectedUpgradeCount +
            " Drafts=" + RewardDraftOpenedCount +
            " Modules=" + UnlockedModuleCount +
            " ObjectiveHits=" + ObjectiveDamageEvents +
            " RangeRejects=" + RangeRejectedTargetCount +
            " ClosestEnemy=" + ClosestEnemyDistanceToObjective.ToString("0.0", CultureInfo.InvariantCulture) +
            " Kenney3D=" + Kenney3DModelSpawnCount +
            " Aim=" + TurretAimUpdateCount +
            " MuzzleProjectiles=" + MuzzleProjectileLaunchCount +
            " MuzzleFlash=" + MuzzleFlashSpawnCount +
            " Recoil=" + RecoilEventCount +
            " AuthoredWeapons=" + AuthoredWeaponPresentationSpawnCount +
            " FallbackWeapons=" + FallbackWeaponPresentationSpawnCount +
            " AuthoredBindings=" + AuthoredWeaponPresentationBindingCount +
            " FallbackBindings=" + FallbackWeaponPresentationBindingCount +
            " AuthoredObjective=" + AuthoredObjectivePresentationBindingCount +
            " FallbackObjective=" + FallbackObjectivePresentationBindingCount +
            " AuthoredSlots=" + AuthoredModuleSlotPresentationBindingCount +
            " FallbackSlots=" + FallbackModuleSlotPresentationBindingCount +
            " DebugAimLines=" + DebugAimTracerSpawnCount +
            " EnemyFacing=" + EnemyFacingUpdateCount +
            " EnemyHitFlash=" + EnemyHitFlashCount +
            " EnemyDeathPop=" + EnemyDeathPopCount +
            " AuthoredStamps=" + AuthoredVisibleInstanceStampCount +
            " FallbackVisible=" + FallbackVisibleGameplaySpawnCount +
            " Currency=" + RuntimeCurrency +
            " Level=" + CommanderLevel +
            " Overdrive=" + (OverdriveActive ? "on" : "off") +
            " Time=" + SurvivalSeconds.ToString("0.0", CultureInfo.InvariantCulture);

        private AutoDefenseRuntimeState RuntimeState => _runtime == null ? AutoDefenseRuntimeState.Created : _runtime.State;
        private IdleAutoDefenseRewardDraftSettings RewardDraftSettings => _rewardDraftSettings ??= IdleAutoDefenseRewardDraftSettings.CreateDefault();
        private IdleAutoDefenseRewardDraftCatalog RewardDraftCatalog => _rewardDraftCatalog ??= IdleAutoDefenseRewardDraftCatalog.CreateDefault();
        private CurrencyId RuntimeCredits => _activeEconomy == null ? BasicIdleAutoDefenseGame.Credits : _activeEconomy.PrimaryCurrency;
        private CurrencyId RuntimeParts => _activeEconomy == null ? BasicIdleAutoDefenseGame.Parts : _activeEconomy.SecondaryCurrency;
        private string RuntimeObjectiveId => _activeGameRules == null || string.IsNullOrWhiteSpace(_activeGameRules.ObjectiveId)
            ? "objective.idle-auto-defense.core"
            : _activeGameRules.ObjectiveId;
        private IdleAutoDefenseContentSetRuntimeSettings ContentSetRuntimeSettings => _resolvedContentSet != null && _resolvedContentSet.IsValid && _resolvedContentSet.ContentSet != null
            ? _resolvedContentSet.ContentSet.RuntimeSettings
            : null;

        protected virtual void Awake()
        {
            Build();
        }

        protected UIDocument EnsureRuntimeUiDocument()
        {
            if (_runtimeUiDocument != null && _runtimeUiRoot != null && _damageNumberLayer != null)
                return _runtimeUiDocument;

            if (_runtimeUiObject == null)
            {
                _runtimeUiObject = new GameObject("Basic Idle Auto Defense UI");
                if (_root != null)
                    _runtimeUiObject.transform.SetParent(_root.transform, false);
            }

            _runtimeUiObject.SetActive(true);
            _runtimeUiObject.transform.SetAsLastSibling();
            _runtimePanelSettings ??= CreateRuntimePanelSettings();
            _runtimeUiDocument = _runtimeUiObject.GetComponent<UIDocument>();
            if (_runtimeUiDocument == null)
                _runtimeUiDocument = _runtimeUiObject.AddComponent<UIDocument>();
            _runtimeUiDocument.panelSettings = _runtimePanelSettings;
            _runtimeUiDocument.sortingOrder = 32767;
            _runtimeUiDocument.enabled = true;

            _runtimeUiRoot = _runtimeUiDocument.rootVisualElement;
            _runtimeUiRoot.name = "idle-auto-defense-ui-root";
            ApplyRuntimeUiRootStyles(_runtimeUiRoot, PickingMode.Position);
            RuntimeUiDirectStylesApplied = true;

            _damageNumberLayer = _runtimeUiRoot.Q<VisualElement>("damage-number-layer");
            if (_damageNumberLayer == null)
            {
                _damageNumberLayer = new VisualElement { name = "damage-number-layer", pickingMode = PickingMode.Ignore };
                ApplyRuntimeUiRootStyles(_damageNumberLayer, PickingMode.Ignore);
                _runtimeUiRoot.Add(_damageNumberLayer);
            }
            else
            {
                ApplyRuntimeUiRootStyles(_damageNumberLayer, PickingMode.Ignore);
            }

            return _runtimeUiDocument;
        }

        protected VisualElement RuntimeUiRoot
        {
            get
            {
                EnsureRuntimeUiDocument();
                return _runtimeUiRoot;
            }
        }

        private PanelSettings CreateRuntimePanelSettings()
        {
            PanelSettings settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.name = "Basic Idle Auto Defense Runtime Panel Settings";
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int((int)RuntimeUiFallbackWidth, (int)RuntimeUiFallbackHeight);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 0.5f;
            settings.scale = 1f;
            settings.sortingOrder = 32767;
            settings.clearColor = false;
            settings.clearDepthStencil = false;
            settings.colorClearValue = Color.clear;
            settings.targetDisplay = 0;
            settings.targetTexture = null;
            ThemeStyleSheet themeStyleSheet = ResolveRuntimeThemeStyleSheet();
            if (themeStyleSheet != null)
                settings.themeStyleSheet = themeStyleSheet;
            settings.hideFlags = HideFlags.HideAndDontSave;
            return settings;
        }

        private static void ApplyRuntimeUiRootStyles(VisualElement element, PickingMode pickingMode)
        {
            if (element == null) return;
            element.pickingMode = pickingMode;
            element.style.display = DisplayStyle.Flex;
            element.style.visibility = Visibility.Visible;
            element.style.opacity = 1f;
            element.style.position = Position.Absolute;
            element.style.left = 0;
            element.style.right = 0;
            element.style.top = 0;
            element.style.bottom = 0;
            element.style.width = Length.Percent(100);
            element.style.height = Length.Percent(100);
            element.style.minWidth = 0;
            element.style.minHeight = 0;
            element.style.backgroundColor = Color.clear;
            element.style.flexDirection = FlexDirection.Column;
            element.style.flexGrow = 1f;
            element.style.overflow = Overflow.Visible;
        }

        protected static void ApplyRuntimeUiFont(VisualElement element)
        {
            if (element == null) return;
            Font font = ResolveRuntimeUiFont();
            if (font != null)
                element.style.unityFont = font;
        }

        private static Font ResolveRuntimeUiFont()
        {
            Font legacyRuntimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (legacyRuntimeFont != null)
                return legacyRuntimeFont;
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static ThemeStyleSheet ResolveRuntimeThemeStyleSheet()
        {
            ThemeStyleSheet packageTheme = Resources.Load<ThemeStyleSheet>("IdleAutoDefenseRuntimeTheme");
            if (packageTheme != null)
                return packageTheme;

            UnityEngine.Object[] loadedThemes = Resources.FindObjectsOfTypeAll(typeof(ThemeStyleSheet));
            foreach (UnityEngine.Object loadedTheme in loadedThemes)
            {
                if (loadedTheme is ThemeStyleSheet themeStyleSheet)
                    return themeStyleSheet;
            }

            UnityEngine.Object[] resourceThemes = Resources.LoadAll(string.Empty, typeof(ThemeStyleSheet));
            foreach (UnityEngine.Object resourceTheme in resourceThemes)
            {
                if (resourceTheme is ThemeStyleSheet themeStyleSheet)
                    return themeStyleSheet;
            }

            return null;
        }

        protected void ConfigureContentPack(GameContentPackAsset contentPack, GameContentSetAsset contentSet)
        {
            _contentPack = contentPack;
            _contentSet = contentSet;
        }

        protected void RequireAuthoredContentOnStartup()
        {
            _requireAuthoredContent = true;
        }

        public void ConfigureStrictAuthoredStartup(bool required)
        {
            if (_runtime != null) throw new InvalidOperationException("Strict authored startup must be configured before the run is built.");
            _requireAuthoredContent = required;
        }

        public void ConfigureRewardDraftSettings(IdleAutoDefenseRewardDraftSettings settings)
        {
            _rewardDraftSettings = settings ?? IdleAutoDefenseRewardDraftSettings.CreateDefault();
        }

        public void ConfigureRewardDraftCatalog(IdleAutoDefenseRewardDraftCatalog catalog)
        {
            _rewardDraftCatalog = catalog ?? IdleAutoDefenseRewardDraftCatalog.CreateDefault();
        }

        protected virtual void Update()
        {
            if (StartupBlocked) return;
            if (_endlessRestartPending)
            {
                _endlessRestartPending = false;
                RestartRun();
                return;
            }

            float deltaSeconds = Time.deltaTime <= 0f ? 1f / 60f : Time.deltaTime;
            if (_activeRunProfile == null || _activeRunProfile.TickSemantics != IdleAutoDefenseTickSemantics.FixedRate)
            {
                Step(1, deltaSeconds);
                return;
            }

            float secondsPerTick = _activeRunProfile.SecondsPerSimulationTick;
            _simulationTickAccumulator += deltaSeconds;
            while (_simulationTickAccumulator + 0.000001f >= secondsPerTick)
            {
                _simulationTickAccumulator -= secondsPerTick;
                Step(1, secondsPerTick);
                if (_runtime == null || _runtime.State != AutoDefenseRuntimeState.Running) break;
            }
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

            AutoDefenseDefinition definition = BasicIdleAutoDefenseGame.CreateDefinition(
                _resolvedEnemyDefinitions,
                ResolveActiveWeaponDefinitionsForRun(),
                _activeGameRules,
                _activeRunProfile == null ? 1f : _activeRunProfile.DifficultyMultiplier);
            CombatCatalog catalog = BasicIdleAutoDefenseGame.CreateCombatCatalog(_resolvedAttackRecipes, _resolvedEnemyDefinitions, _activeGameRules);
            AttackRuntime attacks = BasicIdleAutoDefenseGame.CreateAttackRuntime(catalog, definition, _resolvedAttackRecipes);
            WeaponRuntime weapons = BasicIdleAutoDefenseGame.CreateWeaponRuntime(definition, attacks);

            _root = new GameObject("Basic Idle Auto Defense Runtime");
            _runtimeAudioSource = _root.AddComponent<AudioSource>();
            _runtimeAudioSource.playOnAwake = false;
            _runtimeAudioSource.spatialBlend = 0f;
            _runtimeAudioSource.volume = 0.75f;
            if (FindFirstObjectByType<AudioListener>() == null)
                _root.AddComponent<AudioListener>();
            EnsureRuntimeUiDocument();
            ConfigureGameplayCamera(definition.Objective.Position);
            ConfigureGameplayLighting();
            CreateArenaBackdrop();
            CreateCorePresentation(definition.Objective.Position);
            CreatePlayAreaMarkers();

            string firstEnemyId = _resolvedEnemyDefinitions.Length == 0 || _resolvedEnemyDefinitions[0] == null
                ? BasicIdleAutoDefenseGame.SwarmEnemySpawnableId.Value
                : _resolvedEnemyDefinitions[0].Id;
            _enemyPrefab = CreateEnemyModelPrefab("Template Idle Enemy Runtime Prefab", firstEnemyId, ResolveEnemyFallbackColor(firstEnemyId));
            _projectilePrefab = CreateProjectileModelPrefab("Template Idle Projectile Runtime Prefab", "weapon-ammo-arrow", new Color(1f, 0.45f, 0.1f));

            var poseResolver = new TemplateJitteredPerimeterPoseResolver(definition.Objective, definition.SpawnRing);
            _enemySpawning = new WorldSpawnService(
                new SpawnableCatalog(CreateEnemySpawnables(_resolvedEnemyDefinitions)),
                poseResolver,
                rootName: "TemplateIdleEnemies");
            _navigation = new WorldNavigationService();
            _encounter = new EncounterRuntime(encounterDefinition ?? BasicIdleAutoDefenseGame.CreateEncounterDefinition(
                _resolvedWaveDefinitions,
                _activeRunProfile == null ? 20260623 : _activeRunProfile.EncounterSeed));
            _runtime = new AutoDefenseRuntime(definition, _enemySpawning, _navigation, weapons, catalog, _encounter, poses: poseResolver, candidateCapacity: 64);

            var projectilePoseResolver = new TemplateProjectileMuzzlePoseResolver(this, new WorldSpawnChannelId("projectile-origin"));
            _resolvedProjectileDefinitions = BasicIdleAutoDefenseGame.CreateProjectileDefinitions(_resolvedAttackRecipes);
            _projectileSpawning = new WorldSpawnService(
                new SpawnableCatalog(CreateProjectileSpawnables(_resolvedProjectileDefinitions)),
                projectilePoseResolver,
                rootName: "TemplateIdleProjectiles");
            _projectileNavigation = new WorldNavigationService();
            _projectiles = new ProjectileRuntime(
                catalog,
                _resolvedProjectileDefinitions,
                new WorldSpawnProjectileSpawner(_projectileSpawning, new WorldSpawnChannelId("projectile-origin")),
                new WorldNavigationProjectileNavigator(_projectileNavigation));
            _upgradeCatalog = UsingAssignedContentSet
                ? BasicIdleAutoDefenseGame.CreateRunUpgradeCatalogOrEmpty(_resolvedUpgradeDefinitions)
                : BasicIdleAutoDefenseGame.CreateRunUpgradeCatalog(_resolvedUpgradeDefinitions);
            _upgradeState = new RunUpgradeState();
            _progressionCatalog ??= _activeProgression == null || _activeEconomy == null
                ? BasicIdleAutoDefenseGame.CreateProgressionCatalog()
                : _activeProgression.CreateRuntimeCatalog(_activeEconomy);
            bool createdProgressionState = _progressionState == null;
            _progressionState ??= new ProgressionState();
            if (createdProgressionState && UsingAssignedContentSet)
                ApplyContentSetStartingResources(_resolvedContentSet);
            _offlineDefinition = _activeOfflineProgression == null
                ? BasicIdleAutoDefenseGame.CreateOfflineProgressionDefinition()
                : _activeOfflineProgression.CreateRuntimeDefinition();
            ApplyContentSetEconomyTuning(_resolvedContentSet);
            RuntimeCurrency = ResolveRuntimeStartingCredits(_resolvedContentSet);
            _runSequence++;
            ApplyAllPersistentProgressionEffects();

            _runtime.Start();
        }

        public void RestartRun()
        {
            RestartRun(null);
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
            if (_offlineDefinition == null || _progressionState == null || _progressionCatalog == null)
                throw new InvalidOperationException("Offline rewards require a successfully bound authored core or an explicit fallback host.");
            IdleProgressionResult result = _activeOfflineProgression == null
                ? IdleProgressionCalculator.Calculate(lastSeenUtc, nowUtc, _offlineDefinition)
                : _activeOfflineProgression.Calculate(lastSeenUtc, nowUtc);
            LastOfflineRewardCode = result.Code;
            if (result.Reward.CurrencyLines.Count > 0)
            {
                _progressionState.ApplyReward(_progressionCatalog, new ProgressionOperationId("idle-auto-defense.offline." + nowUtc.UtcTicks), result.Reward);
            }

            long bonusCredits = CalculateOfflineBonusCredits(result);
            if (bonusCredits > 0)
            {
                _progressionState.ApplyReward(
                    _progressionCatalog,
                    new ProgressionOperationId("idle-auto-defense.offline.bonus." + nowUtc.UtcTicks),
                    new RewardBundle(new[] { new CurrencyLine(RuntimeCredits, new ProgressionAmount(bonusCredits), true) }));
            }

            OfflineRewardCredits = _progressionState.GetBalance(RuntimeCredits).Value;
            OfflineRewardParts = _progressionState.GetBalance(RuntimeParts).Value;
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
                double multiplier = _activeOfflineProgression == null ? 2d : _activeOfflineProgression.ClaimMultiplier;
                OfflineRewardCredits = (long)Math.Ceiling(OfflineRewardCredits * multiplier);
                OfflineRewardParts = (long)Math.Ceiling(OfflineRewardParts * multiplier);
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
                    new RunUpgradeDraftRequest(3, (_activeRunProfile == null ? 20260623 : _activeRunProfile.EncounterSeed) + Math.Max(1, DraftTickCount)));
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
                double multiplier = _activeEconomy == null ? 2d : _activeEconomy.RunRewardClaimMultiplier;
                EncounterRewardCredits = (long)Math.Ceiling(EncounterRewardCredits * multiplier);
                EncounterRewardParts = (long)Math.Ceiling(EncounterRewardParts * multiplier);
            }

            return result;
        }

        public MonetizationResult OfferSmallCurrencyBonus(RewardClaimId claimId, DateTimeOffset nowUtc)
        {
            MonetizationResult result = MonetizationSession.ShowRewarded(
                IdleAutoDefenseTemplateMonetization.SmallCurrencyBonus,
                claimId,
                CreateMonetizationContext(nowUtc));
            if (result.Succeeded) EncounterRewardCredits += _activeEconomy == null ? 5 : Math.Max(0L, _activeEconomy.SmallCurrencyBonus);
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
            int safeTicks = Math.Max(1, ticks);
            _sessionElapsedTicks += safeTicks;
            UpdateOverdriveTimers(deltaSeconds);
            if (RewardDraftActive && RewardDraftPausesCombat)
            {
                UpdateActiveBeamVisuals(deltaSeconds);
                UpdateDamageNumbers(deltaSeconds);
                UpdateCameraShake(deltaSeconds);
                return;
            }

            SurvivalSeconds += Math.Max(0f, deltaSeconds);
            OfferFirstRewardDraftIfReady();
            if (RewardDraftActive && RewardDraftPausesCombat)
            {
                UpdateActiveBeamVisuals(deltaSeconds);
                UpdateDamageNumbers(deltaSeconds);
                UpdateCameraShake(deltaSeconds);
                return;
            }
            _encounter.AdvanceTicks(EnemySpawnDelayTicks);
            _encounter.DrainSpawnRequests(_spawnBuffer);
            for (int i = 0; i < _spawnBuffer.Length; i++)
            {
                if (_spawnBuffer[i].SpawnableId.IsEmpty) continue;
                AutoDefenseRunResult spawn = _runtime.ConsumeSpawnRequest(_spawnBuffer[i]);
                if (spawn.Succeeded) SpawnedCount += spawn.Spawned;
                _spawnBuffer[i] = default;
            }

            EmitSpawnFeedbackForNewEnemies();
            AutoDefenseRuntimeSnapshot beforeCombat = _runtime.CreateSnapshot();
            UpdateWeaponPresentationTargets(beforeCombat, deltaSeconds);
            UpdateEnemyModelPresentations(beforeCombat, deltaSeconds);
            AutoDefenseRunResult result = _runtime.Tick(ticks, deltaSeconds);
            DirectOrCombatKillCount += result.Killed;
            int rewardedKills = result.Killed;
            ObjectiveReachCount += result.ReachedObjective;
            if (result.ReachedObjective > 0)
            {
                ObjectiveDamageEvents += result.ReachedObjective;
                Vector3 towerImpactPosition = CreateTowerMuzzlePosition(Vector3.zero);
                EmitDamageNumber(towerImpactPosition, result.ReachedObjective, new Color(1f, 0.25f, 0.18f), "-");
                EmitKenneySpriteBurst("Tower Damage Burst", "Art/impact_flame", towerImpactPosition, new Color(1f, 0.18f, 0.08f), 1.1f, 0.46f, 0.25f, 52);
                TriggerCameraShake(0.2f, 0.13f);
            }
            AutoDefenseRuntimeSnapshot afterCombat = _runtime.CreateSnapshot();
            UpdateEnemyModelPresentations(afterCombat, deltaSeconds);
            ObserveEnemyPressure(afterCombat);
            EmitDirectWeaponPresentation(result.WeaponFireResult, beforeCombat, afterCombat);
            EmitMissingKillFeedback(beforeCombat, afterCombat, result.Killed, null);

            for (int i = 0; i < result.ProjectileLaunches.Count; i++)
            {
                ProjectileLaunchRequest launchRequest = CreateVisibleProjectileLaunchRequest(
                    result.ProjectileLaunches[i],
                    afterCombat,
                    out AttackDefinitionAsset attack,
                    out AutoDefenseEnemySnapshot target,
                    out int impactDelayTicks,
                    out double damageThreshold);
                EmitAttackEvent(attack, AttackPresentationEventKind.OnCast, launchRequest.Origin);
                EmitAttackEvent(attack, AttackPresentationEventKind.OnFire, launchRequest.Origin);
                ProjectileLaunchResult launch = _projectiles.Launch(launchRequest);
                if (!launch.Succeeded) continue;
                ProjectileLaunchCount++;
                RecordProjectileVisualSpawn(attack);
                _pendingProjectileImpacts.Add(new PendingProjectileImpact(
                    launch.ProjectileId,
                    target.Id,
                    attack,
                    launchRequest.Destination,
                    damageThreshold,
                    impactDelayTicks));
            }

            ProjectileTickResult projectileTick = _projectiles.Tick(ticks);
            _projectileNavigation.Tick((float)(deltaSeconds * ProjectileSpeedMultiplier));
            ObserveProjectileMotion();
            EmitProjectileExpiryFeedback(projectileTick);
            rewardedKills += ResolvePendingProjectileImpacts(ticks);
            rewardedKills += ApplyDirectDamageBonusIfReady();
            rewardedKills += FireManualTowerShotIfReady(ticks);
            rewardedKills += FireUnlockedModulesIfReady(ticks);
            AwardRuntimeCurrencyForKills(rewardedKills);
            GrantPassiveIncomeIfReady(ticks);
            AwardExperienceForCompletedWaves();
            ApplyEncounterRewardIfTerminal();
            UpdateActiveBeamVisuals(deltaSeconds);
            UpdateDamageNumbers(deltaSeconds);
            UpdateCameraShake(deltaSeconds);
            EvaluateAuthoredRunProfileTerminalState();
        }

        private void EvaluateAuthoredRunProfileTerminalState()
        {
            if (_runtime == null || _activeRunProfile == null) return;
            if (_runtime.State == AutoDefenseRuntimeState.Running &&
                _activeRunProfile.VictoryRule == IdleAutoDefenseVictoryRule.SurviveSessionDuration &&
                _sessionElapsedTicks >= _activeRunProfile.SessionLengthTicks)
            {
                RunProfileVictoryReached = true;
                _runtime.Stop();
                ApplyEncounterRewardIfTerminal();
            }

            if (EncounterCompleted && _activeRunProfile.Endless)
                _endlessRestartPending = true;
        }

        public bool TryPurchaseDamageUpgrade()
        {
            if (!SpendRuntimeCurrency(DamageUpgradeCost)) return false;
            DamageUpgradeRank++;
            SelectedUpgradeCount++;
            EmitUpgradeFeedback("Damage Up", new Color(1f, 0.55f, 0.18f), 0.9f);
            return true;
        }

        public bool TryPurchaseAttackSpeedUpgrade()
        {
            if (!SpendRuntimeCurrency(AttackSpeedUpgradeCost)) return false;
            AttackSpeedUpgradeRank++;
            SelectedUpgradeCount++;
            EmitUpgradeFeedback("Fire Rate Up", new Color(0.35f, 0.9f, 1f), 0.9f);
            return true;
        }

        public bool TryPurchaseRangeUpgrade()
        {
            if (!SpendRuntimeCurrency(RangeUpgradeCost)) return false;
            RangeUpgradeRank++;
            DirectDamageBonus += _activeGameRules == null ? 0.5d : _activeGameRules.PurchaseRangeDamageBonus;
            SelectedUpgradeCount++;
            EmitUpgradeFeedback("Range Up", new Color(0.45f, 1f, 0.6f), 0.9f);
            return true;
        }

        public bool TryPurchaseRepairUpgrade()
        {
            if (!SpendRuntimeCurrency(RepairUpgradeCost)) return false;
            RepairUpgradeRank++;
            if (_runtime != null)
            {
                double maximumHealth = _activeGameRules == null ? 8d : _activeGameRules.PurchaseRepairMaximumHealth;
                double baseHeal = _activeGameRules == null ? 34d : _activeGameRules.PurchaseRepairBaseHeal;
                double healPerRank = _activeGameRules == null ? 6d : _activeGameRules.PurchaseRepairHealPerRank;
                _runtime.Objective.Health.ChangeMaximumHealth(_runtime.Objective.Health.MaximumHealth + maximumHealth, MaximumChangePolicy.PreserveAbsolute);
                _runtime.Objective.Health.Heal(baseHeal + RepairUpgradeRank * healPerRank);
            }

            SelectedUpgradeCount++;
            EmitUpgradeFeedback("Repair", new Color(0.35f, 1f, 0.55f), 1.0f);
            return true;
        }

        public bool TryPurchasePulseBeamModule()
        {
            if (!SpendRuntimeCurrency(PulseBeamUnlockCost)) return false;
            UnlockPulseBeamModule();
            SelectedUpgradeCount++;
            EmitUpgradeFeedback("Pulse Beam Online", new Color(0.15f, 0.8f, 1f), 1.1f);
            return true;
        }

        public bool TryPurchaseArcBurstModule()
        {
            if (!SpendRuntimeCurrency(ArcBurstUnlockCost)) return false;
            UnlockArcBurstModule();
            SelectedUpgradeCount++;
            EmitUpgradeFeedback("Arc Burst Online", new Color(1f, 0.65f, 0.12f), 1.1f);
            return true;
        }

        public bool TryPurchaseHomingPulseModule()
        {
            if (!SpendRuntimeCurrency(HomingPulseUnlockCost)) return false;
            UnlockHomingPulseModule();
            SelectedUpgradeCount++;
            EmitUpgradeFeedback("Homing Online", new Color(0.68f, 0.38f, 1f), 1.1f);
            return true;
        }

        public bool TryPurchaseOverdrive()
        {
            if (!CanPurchaseOverdrive || !SpendRuntimeCurrency(OverdriveCost)) return false;
            float duration = _activeGameRules == null ? OverdriveDurationSeconds : _activeGameRules.OverdriveDurationSeconds;
            float cooldown = _activeGameRules == null ? OverdriveCooldownSeconds : _activeGameRules.OverdriveCooldownSeconds;
            _overdriveSecondsRemaining = duration;
            _overdriveCooldownSecondsRemaining = duration + cooldown;
            OverdriveActivationCount++;
            SelectedUpgradeCount++;
            EmitUpgradeFeedback("OVERDRIVE", new Color(1f, 0.82f, 0.18f), 1.28f);
            return true;
        }

        public int GetPersistentResearchRank(string nodeId)
        {
            return _progressionState == null || string.IsNullOrWhiteSpace(nodeId)
                ? 0
                : _progressionState.GetResearchRank(new ResearchNodeId(nodeId));
        }

        public long GetPersistentCurrencyBalance(string currencyId)
        {
            return _progressionState == null || string.IsNullOrWhiteSpace(currencyId)
                ? 0L
                : _progressionState.GetBalance(new CurrencyId(currencyId)).Value;
        }

        public IdleAutoDefensePersistentProgressionData CapturePersistentProgression()
        {
            var data = new IdleAutoDefensePersistentProgressionData();
            if (_progressionState == null) return data;
            ProgressionSnapshot snapshot = _progressionState.CreateSnapshot();
            for (int i = 0; i < snapshot.Balances.Count; i++)
                data.Balances.Add(new IdleAutoDefensePersistentLongValue { Id = snapshot.Balances[i].Id.Value, Value = snapshot.Balances[i].Value });
            for (int i = 0; i < snapshot.Tracks.Count; i++)
                data.Tracks.Add(new IdleAutoDefensePersistentLongValue { Id = snapshot.Tracks[i].Id.Value, Value = snapshot.Tracks[i].Value });
            for (int i = 0; i < snapshot.Research.Count; i++)
                data.ResearchRanks.Add(new IdleAutoDefensePersistentIntValue { Id = snapshot.Research[i].Id.Value, Value = snapshot.Research[i].Value });
            for (int i = 0; i < snapshot.Unlocks.Count; i++) data.UnlockIds.Add(snapshot.Unlocks[i].Value);
            return data;
        }

        public bool RestorePersistentProgression(IdleAutoDefensePersistentProgressionData data)
        {
            if (data == null || !data.HasData || _progressionCatalog == null || _activeProgression == null) return false;
            var restored = new ProgressionState();
            var balances = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            var researchFunds = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < data.Balances.Count; i++)
            {
                IdleAutoDefensePersistentLongValue value = data.Balances[i];
                if (value != null && !string.IsNullOrWhiteSpace(value.Id)) balances[value.Id] = Math.Max(0L, value.Value);
            }

            var targetRanks = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < data.ResearchRanks.Count; i++)
            {
                IdleAutoDefensePersistentIntValue value = data.ResearchRanks[i];
                if (value != null && !string.IsNullOrWhiteSpace(value.Id)) targetRanks[value.Id] = Math.Max(0, value.Value);
            }
            for (int i = 0; i < _activeProgression.ResearchNodes.Count; i++)
            {
                IdleAutoDefenseResearchNodeRecord node = _activeProgression.ResearchNodes[i];
                if (node == null || !targetRanks.TryGetValue(node.Id, out int targetRank)) continue;
                long cost = 0L;
                for (int rank = 0; rank < Math.Min(targetRank, node.RankCosts.Count); rank++) cost += Math.Max(0L, node.RankCosts[rank]);
                researchFunds.TryGetValue(node.CostCurrencyId, out long current);
                researchFunds[node.CostCurrencyId] = current + cost;
            }

            var researchCurrencyLines = new List<CurrencyLine>();
            foreach (KeyValuePair<string, long> balance in researchFunds)
                if (balance.Value > 0L) researchCurrencyLines.Add(new CurrencyLine(new CurrencyId(balance.Key), new ProgressionAmount(balance.Value), true));
            var finalCurrencyLines = new List<CurrencyLine>();
            foreach (KeyValuePair<string, long> balance in balances)
                if (balance.Value > 0L) finalCurrencyLines.Add(new CurrencyLine(new CurrencyId(balance.Key), new ProgressionAmount(balance.Value), true));
            var xp = new List<XpGrant>();
            for (int i = 0; i < data.Tracks.Count; i++)
            {
                IdleAutoDefensePersistentLongValue value = data.Tracks[i];
                if (value != null && !string.IsNullOrWhiteSpace(value.Id) && value.Value > 0L)
                    xp.Add(new XpGrant(new TrackId(value.Id), new ProgressionAmount(value.Value)));
            }
            var unlocks = new List<UnlockId>();
            for (int i = 0; i < data.UnlockIds.Count; i++)
                if (!string.IsNullOrWhiteSpace(data.UnlockIds[i])) unlocks.Add(new UnlockId(data.UnlockIds[i]));
            ProgressionResult seed = restored.ApplyReward(
                _progressionCatalog,
                new ProgressionOperationId("idle-auto-defense.profile.restore.seed"),
                new RewardBundle(researchCurrencyLines, xp, unlocks));
            if (!seed.Succeeded) return false;

            int remaining = 0;
            foreach (int target in targetRanks.Values) remaining += target;
            for (int pass = 0; pass < remaining + 1 && remaining > 0; pass++)
            {
                bool progressed = false;
                for (int i = 0; i < _activeProgression.ResearchNodes.Count; i++)
                {
                    IdleAutoDefenseResearchNodeRecord node = _activeProgression.ResearchNodes[i];
                    if (node == null || !targetRanks.TryGetValue(node.Id, out int targetRank)) continue;
                    int currentRank = restored.GetResearchRank(new ResearchNodeId(node.Id));
                    if (currentRank >= targetRank) continue;
                    ProgressionResult purchase = restored.PurchaseResearch(
                        _progressionCatalog,
                        new ProgressionOperationId("idle-auto-defense.profile.restore." + BasicIdleAutoDefenseGame.SanitizeContentSetOperationSegment(node.Id) + "." + (currentRank + 1).ToString(CultureInfo.InvariantCulture)),
                        new ResearchNodeId(node.Id));
                    if (!purchase.Succeeded) continue;
                    remaining--;
                    progressed = true;
                }
                if (!progressed) break;
            }
            if (remaining > 0) return false;

            ProgressionResult finalBalances = restored.ApplyReward(
                _progressionCatalog,
                new ProgressionOperationId("idle-auto-defense.profile.restore.balances"),
                new RewardBundle(finalCurrencyLines));
            if (!finalBalances.Succeeded) return false;

            _progressionState = restored;
            ApplyAllPersistentProgressionEffects();
            OfflineRewardCredits = GetPersistentCurrencyBalance(_activeEconomy.PrimaryCurrencyId);
            OfflineRewardParts = GetPersistentCurrencyBalance(_activeEconomy.SecondaryCurrencyId);
            return true;
        }

        public void ResetPersistentProgression()
        {
            _progressionState = new ProgressionState();
            if (_progressionCatalog == null && _activeProgression != null && _activeEconomy != null)
                _progressionCatalog = _activeProgression.CreateRuntimeCatalog(_activeEconomy);
            ApplyContentSetStartingResources(_resolvedContentSet);
            OfflineRewardCredits = GetPersistentCurrencyBalance(_activeEconomy == null ? string.Empty : _activeEconomy.PrimaryCurrencyId);
            OfflineRewardParts = GetPersistentCurrencyBalance(_activeEconomy == null ? string.Empty : _activeEconomy.SecondaryCurrencyId);
        }

        public bool TryPurchasePersistentUpgrade(string nodeId)
        {
            if (_progressionState == null || _progressionCatalog == null || _activeProgression == null || string.IsNullOrWhiteSpace(nodeId))
                return false;
            IdleAutoDefenseResearchNodeRecord node = _activeProgression.FindResearchNode(nodeId);
            if (node == null) return false;
            ProgressionResult result = _progressionState.PurchaseResearch(
                _progressionCatalog,
                new ProgressionOperationId("idle-auto-defense.research." + BasicIdleAutoDefenseGame.SanitizeContentSetOperationSegment(node.Id) + "." + (GetPersistentResearchRank(node.Id) + 1).ToString(CultureInfo.InvariantCulture)),
                new ResearchNodeId(node.Id));
            if (!result.Succeeded) return false;
            ApplyPersistentProgressionEffect(node);
            return true;
        }

        private void ApplyPersistentProgressionEffect(IdleAutoDefenseResearchNodeRecord node)
        {
            if (node == null || node.EffectAmountPerRank == 0d) return;
            if (node.EffectKind == IdleAutoDefenseProgressionEffectKind.ObjectiveMaximumHealth && _runtime != null)
                _runtime.Objective.Health.ChangeMaximumHealth(_runtime.Objective.Health.MaximumHealth + node.EffectAmountPerRank, MaximumChangePolicy.FillToMaximum);
            else if (node.EffectKind == IdleAutoDefenseProgressionEffectKind.DamageRank)
                DamageUpgradeRank += Math.Max(1, (int)Math.Round(node.EffectAmountPerRank));
            else if (node.EffectKind == IdleAutoDefenseProgressionEffectKind.ExtraProjectile)
                _shardVolleyBonus += Math.Max(1, (int)Math.Round(node.EffectAmountPerRank));
            else if (node.EffectKind == IdleAutoDefenseProgressionEffectKind.OfflineRewardMultiplier)
                OfflineRewardMultiplierBonus += Math.Max(0d, node.EffectAmountPerRank);
        }

        private void ApplyAllPersistentProgressionEffects()
        {
            if (_activeProgression == null || _progressionState == null) return;
            for (int i = 0; i < _activeProgression.ResearchNodes.Count; i++)
            {
                IdleAutoDefenseResearchNodeRecord node = _activeProgression.ResearchNodes[i];
                if (node == null) continue;
                int rank = GetPersistentResearchRank(node.Id);
                for (int applied = 0; applied < rank; applied++) ApplyPersistentProgressionEffect(node);
            }
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
                if (effect.EffectId.Value == "idle-auto-defense.direct.damage_bonus") DirectDamageBonus += effect.Amount;
                else if (effect.EffectId.Value == "idle-auto-defense.projectile.speed_multiplier") ProjectileSpeedMultiplier += effect.Amount;
                else if (effect.EffectId.Value == "idle-auto-defense.objective.heal") _runtime.Objective.Health.Heal(effect.Amount);
                else if (effect.EffectId.Value == "idle-auto-defense.objective.max_health") _runtime.Objective.Health.ChangeMaximumHealth(_runtime.Objective.Health.MaximumHealth + effect.Amount, MaximumChangePolicy.FillToMaximum);
                else if (effect.EffectId.Value == "idle-auto-defense.weapon.fire_rate_intent") AttackSpeedUpgradeRank++;
                else if (effect.EffectId.Value == "idle-auto-defense.weapon.range_intent")
                {
                    RangeUpgradeRank++;
                    DirectDamageBonus += Math.Max(0.5d, effect.Amount * 0.5d);
                }
                else if (effect.EffectId.Value == "idle-auto-defense.enemy.spawn_delay_ticks") EnemySpawnDelayTicks += (int)effect.Amount;
                else if (effect.EffectId.Value == "idle-auto-defense.reward.credits_multiplier") RewardCreditMultiplierBonus += effect.Amount;
                else if (effect.EffectId.Value == "idle-auto-defense.offline.credits_multiplier") OfflineRewardMultiplierBonus += effect.Amount;
                else UnsupportedUpgradeIntentCount++;
            }
        }

        private int ApplyDirectDamageBonusIfReady()
        {
            // DirectDamageBonus feeds visible tower/module damage. It must not silently delete enemies.
            return 0;
        }

        private void UpdateOverdriveTimers(float deltaSeconds)
        {
            float safeDelta = Mathf.Max(0f, deltaSeconds);
            if (_overdriveSecondsRemaining > 0f)
                _overdriveSecondsRemaining = Mathf.Max(0f, _overdriveSecondsRemaining - safeDelta);
            if (_overdriveCooldownSecondsRemaining > 0f)
                _overdriveCooldownSecondsRemaining = Mathf.Max(0f, _overdriveCooldownSecondsRemaining - safeDelta);
        }

        private void OfferFirstRewardDraftIfReady()
        {
            float targetSeconds = _activeRewardCatalog == null ? FirstRewardDraftTargetSeconds : _activeRewardCatalog.FirstDraftSeconds;
            if (_starterRewardDraftOffered || FirstRewardDraftSeconds >= 0f || SurvivalSeconds < targetSeconds) return;
            _starterRewardDraftOffered = true;
            QueueOrOpenRewardDraft(IdleAutoDefenseRewardDraftKind.LevelUp);
            EmitUpgradeFeedback("Reward Ready", new Color(1f, 0.82f, 0.18f), 1.05f);
        }

        private int FireManualTowerShotIfReady(int ticks)
        {
            IdleAutoDefenseModuleRule module = ResolveModuleRule(IdleAutoDefenseModuleRole.StartingProjectile);
            _manualTowerCooldownTicks += Math.Max(1, ticks);
            int baseCooldown = module == null ? ManualTowerBaseCooldownTicks : module.BaseCooldownTicks;
            int minimumCooldown = module == null ? ManualTowerMinimumCooldownTicks : module.MinimumCooldownTicks;
            int rankReduction = module == null ? 3 : module.CooldownReductionPerFireRateRank;
            int overdriveReduction = _activeGameRules == null ? OverdriveCooldownBonusTicks : _activeGameRules.OverdriveCooldownBonusTicks;
            int cooldownTicks = Math.Max(minimumCooldown, baseCooldown - AttackSpeedUpgradeRank * rankReduction - (OverdriveActive ? overdriveReduction : 0));
            if (_manualTowerCooldownTicks < cooldownTicks) return 0;
            _manualTowerCooldownTicks = 0;

            int kills = 0;
            int shotCount = Math.Max(1, (module == null ? 1 : module.BaseTargetCount) + _shardVolleyBonus);
            double damageAmount = ResolveManualTowerDamage();
            AttackDefinitionAsset attack = FindAttackRecipeForPresentation(module == null ? BasicIdleAutoDefenseGame.ShardAttackId.Value : module.AttackId);
            for (int shot = 0; shot < shotCount; shot++)
            {
                AutoDefenseRuntimeSnapshot snapshot = _runtime.CreateSnapshot();
                if (!TrySelectPriorityEnemyWithinRange(snapshot, ResolveManualTowerRange(), out AutoDefenseEnemySnapshot selected)) break;
                if (TryLaunchVisibleProjectileAtEnemy(selected, attack, damageAmount)) continue;
                if (!TryDamageEnemyWithPresentation(selected, attack, damageAmount, out bool killed)) continue;
                if (!killed) continue;
                DirectOrCombatKillCount++;
                kills++;
            }

            return kills;
        }

        private int FireUnlockedModulesIfReady(int ticks)
        {
            int kills = 0;
            if (PulseBeamUnlocked)
            {
                IdleAutoDefenseModuleRule module = ResolveModuleRule(IdleAutoDefenseModuleRole.PrecisionBeam);
                _pulseBeamModuleCooldownTicks += Math.Max(1, ticks);
                int overdriveReduction = _activeGameRules == null ? OverdriveCooldownBonusTicks : _activeGameRules.OverdriveCooldownBonusTicks;
                int cooldown = module == null
                    ? Math.Max(28, PulseBeamModuleCooldownTicks - AttackSpeedUpgradeRank * 2 - (OverdriveActive ? overdriveReduction : 0))
                    : Math.Max(module.MinimumCooldownTicks, module.BaseCooldownTicks - AttackSpeedUpgradeRank * module.CooldownReductionPerFireRateRank - (OverdriveActive ? overdriveReduction : 0));
                if (_pulseBeamModuleCooldownTicks >= cooldown)
                {
                    _pulseBeamModuleCooldownTicks = 0;
                    ModuleActivationCount++;
                    kills += TryKillPriorityEnemies(
                        ResolveModuleDamage((module == null ? 5d : module.BaseDamage) + DamageUpgradeRank * (module == null ? 1.25d : module.DamagePerDamageRank) + RangeUpgradeRank * (module == null ? 0.45d : module.DamagePerRangeRank)),
                        (module == null ? 1 : module.BaseTargetCount) + _pulseBeamBonus,
                        module == null ? BasicIdleAutoDefenseGame.PulseAttackId.Value : module.AttackId,
                        ResolveModuleRange(module == null ? PulseBeamModuleBaseRange : module.BaseRange));
                }
            }

            if (ArcBurstUnlocked)
            {
                IdleAutoDefenseModuleRule module = ResolveModuleRule(IdleAutoDefenseModuleRole.AreaBurst);
                _arcBurstModuleCooldownTicks += Math.Max(1, ticks);
                int overdriveReduction = _activeGameRules == null ? OverdriveCooldownBonusTicks : _activeGameRules.OverdriveCooldownBonusTicks;
                int cooldown = module == null
                    ? Math.Max(52, ArcBurstModuleCooldownTicks - AttackSpeedUpgradeRank * 3 - (OverdriveActive ? overdriveReduction : 0))
                    : Math.Max(module.MinimumCooldownTicks, module.BaseCooldownTicks - AttackSpeedUpgradeRank * module.CooldownReductionPerFireRateRank - (OverdriveActive ? overdriveReduction : 0));
                if (_arcBurstModuleCooldownTicks >= cooldown)
                {
                    _arcBurstModuleCooldownTicks = 0;
                    ModuleActivationCount++;
                    kills += TryKillPriorityEnemies(
                        ResolveModuleDamage((module == null ? 8d : module.BaseDamage) + DamageUpgradeRank * (module == null ? 1.55d : module.DamagePerDamageRank) + RangeUpgradeRank * (module == null ? 0d : module.DamagePerRangeRank)),
                        (module == null ? 2 : module.BaseTargetCount) + _arcBurstBonus,
                        module == null ? BasicIdleAutoDefenseGame.ArcBurstAttackId.Value : module.AttackId,
                        ResolveModuleRange(module == null ? ArcBurstModuleBaseRange : module.BaseRange));
                }
            }

            if (HomingPulseUnlocked)
            {
                IdleAutoDefenseModuleRule module = ResolveModuleRule(IdleAutoDefenseModuleRole.HomingProjectile);
                _homingPulseModuleCooldownTicks += Math.Max(1, ticks);
                int overdriveReduction = _activeGameRules == null ? OverdriveCooldownBonusTicks : _activeGameRules.OverdriveCooldownBonusTicks;
                int cooldown = module == null
                    ? Math.Max(42, HomingPulseModuleCooldownTicks - AttackSpeedUpgradeRank * 2 - (OverdriveActive ? overdriveReduction : 0))
                    : Math.Max(module.MinimumCooldownTicks, module.BaseCooldownTicks - AttackSpeedUpgradeRank * module.CooldownReductionPerFireRateRank - (OverdriveActive ? overdriveReduction : 0));
                if (_homingPulseModuleCooldownTicks >= cooldown)
                {
                    _homingPulseModuleCooldownTicks = 0;
                    ModuleActivationCount++;
                    kills += TryKillPriorityEnemies(
                        ResolveModuleDamage((module == null ? 8d : module.BaseDamage) + DamageUpgradeRank * (module == null ? 1.45d : module.DamagePerDamageRank) + RangeUpgradeRank * (module == null ? 0.45d : module.DamagePerRangeRank)),
                        (module == null ? 1 : module.BaseTargetCount) + _homingPulseBonus,
                        module == null ? BasicIdleAutoDefenseGame.HomingPulseAttackId.Value : module.AttackId,
                        ResolveModuleRange(module == null ? HomingPulseModuleBaseRange : module.BaseRange),
                        preferProjectileVisual: true);
                }
            }

            return kills;
        }

        private int TryKillPriorityEnemies(double damageThreshold, int maxKills, string attackId, double range, bool preferProjectileVisual = false)
        {
            if (_runtime == null || maxKills <= 0) return 0;
            int kills = 0;
            AttackDefinitionAsset attack = FindAttackRecipeForPresentation(attackId);
            for (int attempt = 0; attempt < maxKills; attempt++)
            {
                AutoDefenseRuntimeSnapshot snapshot = _runtime.CreateSnapshot();
                if (!TrySelectPriorityEnemyWithinRange(snapshot, range, out AutoDefenseEnemySnapshot selected)) break;
                if (preferProjectileVisual && TryLaunchVisibleProjectileAtEnemy(selected, attack, damageThreshold)) continue;
                if (!TryDamageEnemyWithPresentation(selected, attack, damageThreshold, out bool killed)) break;
                if (killed)
                {
                    DirectOrCombatKillCount++;
                    kills++;
                }
            }

            return kills;
        }

        private ProjectileLaunchRequest CreateVisibleProjectileLaunchRequest(
            ProjectileLaunchRequest original,
            AutoDefenseRuntimeSnapshot snapshot,
            out AttackDefinitionAsset attack,
            out AutoDefenseEnemySnapshot target,
            out int impactDelayTicks,
            out double damageThreshold)
        {
            attack = FindAttackRecipeForPresentation(original.AttackDefinitionId.Value);
            if (!TrySelectAttackTarget(attack, snapshot, out target))
                target = default;

            Vector3 origin = ResolveTowerMuzzlePosition(attack);
            Vector3 destination = target.Id > 0
                ? CreateEnemyAimPosition(target.Position)
                : (original.Destination == Vector3.zero ? origin + Vector3.forward * 4f : original.Destination);
            if (target.Id > 0)
                PlayWeaponFirePresentation(attack, destination);
            MuzzleProjectileLaunchCount++;
            ProjectileDefinition projectile = FindProjectileDefinition(original.DefinitionId);
            float speed = projectile == null ? ResolveProjectileSpeed(attack) : projectile.Speed;
            impactDelayTicks = CalculateProjectileImpactDelayTicks(origin, destination, speed);
            damageThreshold = projectile == null ? ResolveAttackDamage(attack) : projectile.BaseDamage;

            return new ProjectileLaunchRequest(
                original.DefinitionId,
                original.AttackSourceId,
                original.AttackDefinitionId,
                original.Source,
                origin,
                destination,
                original.Path);
        }

        private int ResolvePendingProjectileImpacts(int ticks)
        {
            if (_pendingProjectileImpacts.Count == 0) return 0;
            int kills = 0;
            int step = Math.Max(1, ticks);
            for (int i = _pendingProjectileImpacts.Count - 1; i >= 0; i--)
            {
                PendingProjectileImpact pending = _pendingProjectileImpacts[i];
                pending.RemainingTicks -= step;
                if (pending.RemainingTicks > 0)
                {
                    _pendingProjectileImpacts[i] = pending;
                    continue;
                }

                _pendingProjectileImpacts.RemoveAt(i);
                bool hasImpactTarget = TryFindProjectileImpactTarget(pending, out AutoDefenseEnemySnapshot impactTarget);
                Vector3 impactPosition = hasImpactTarget
                    ? CreateEnemyAimPosition(impactTarget.Position)
                    : pending.Destination;

                if (!hasImpactTarget)
                {
                    ProjectileImpactMissCount++;
                    EmitAttackEvent(pending.Attack, AttackPresentationEventKind.OnExpire, impactPosition);
                    CleanupProjectileWithoutDamage(pending.ProjectileId);
                    continue;
                }

                if (!TryReportProjectileImpact(pending, impactTarget, out _))
                {
                    ProjectileImpactRejectedCount++;
                    EmitAttackEvent(pending.Attack, AttackPresentationEventKind.OnExpire, impactPosition);
                    continue;
                }

                ProjectileImpactCallbackCount++;
                EmitAttackEvent(pending.Attack, AttackPresentationEventKind.OnImpact, impactPosition, impactTarget.Id);
                ProjectileDamageResolvedFromImpactCount++;
                ProjectileDamageAppliedCount++;
                if (!TryApplyVisibleEnemyDamage(impactTarget, pending.Attack, pending.DamageThreshold, impactPosition, out bool killed))
                    continue;
                if (killed)
                {
                    ProjectileAdapterKillCount++;
                    kills++;
                }
            }

            return kills;
        }

        private bool TryReportProjectileImpact(
            PendingProjectileImpact pending,
            AutoDefenseEnemySnapshot impactTarget,
            out ProjectileImpactResult result)
        {
            result = default;
            if (_projectiles == null || pending.ProjectileId.Value <= 0 || impactTarget.Id <= 0 || impactTarget.CombatantId.IsEmpty)
                return false;

            double currentHealth = Math.Max(0.01d, impactTarget.Health);
            double maximumHealth = Math.Max(currentHealth, Math.Max(1d, ResolveAttackDamage(pending.Attack)));
            var targetHealth = new HealthState(impactTarget.CombatantId, maximumHealth, currentHealth);
            result = _projectiles.ReportImpact(new ProjectileImpactRequest(pending.ProjectileId, impactTarget.CombatantId, targetHealth));
            return result.Succeeded;
        }

        private void CleanupProjectileWithoutDamage(ProjectileInstanceId projectileId)
        {
            if (_projectiles == null || projectileId.Value <= 0) return;
            _projectiles.Cleanup(projectileId, ProjectileExpiryReason.ManualCleanup);
        }

        private void EmitProjectileExpiryFeedback(ProjectileTickResult result)
        {
            if (result == null || result.Expiries.Count == 0) return;
            for (int i = 0; i < result.Expiries.Count; i++)
            {
                ProjectileExpiryEvent expiry = result.Expiries[i];
                int pendingIndex = FindPendingProjectileImpactIndex(expiry.ProjectileId);
                if (pendingIndex < 0) continue;
                ProjectileExpiryDeferralCount++;
            }
        }

        private void EmitDirectWeaponPresentation(WeaponFireResult fireResult, AutoDefenseRuntimeSnapshot beforeCombat, AutoDefenseRuntimeSnapshot afterCombat)
        {
            if (fireResult == null) return;
            for (int i = 0; i < fireResult.Intents.Count; i++)
            {
                WeaponIntent intent = fireResult.Intents[i];
                if (intent.Kind != WeaponIntentKind.DirectAttack || intent.AttackIntent == null) continue;

                AttackDefinitionAsset attack = FindAttackRecipeForPresentation(intent.AttackIntent.DefinitionId.Value);
                CombatantId targetId = intent.AttackIntent.Selection.Target.CombatantId;
                bool hadBefore = TryFindEnemyByCombatant(beforeCombat, targetId, out AutoDefenseEnemySnapshot beforeTarget);
                bool hasAfter = TryFindEnemyByCombatant(afterCombat, targetId, out AutoDefenseEnemySnapshot afterTarget);
                AutoDefenseEnemySnapshot target = hasAfter ? afterTarget : beforeTarget;
                Vector3 targetPosition = hadBefore || hasAfter ? CreateEnemyAimPosition(target.Position) : Vector3.zero;

                Vector3 origin = ResolveTowerMuzzlePosition(attack);
                PlayWeaponFirePresentation(attack, targetPosition);
                EmitAttackEvent(attack, AttackPresentationEventKind.OnCast, origin);
                EmitAttackEvent(attack, AttackPresentationEventKind.OnFire, origin);
                if (hadBefore || hasAfter)
                    EmitAttackTracer(origin, targetPosition, ResolveAttackColor(attack));
                EmitAttackEvent(attack, AttackPresentationEventKind.OnImpact, targetPosition, target.Id);
                if (hadBefore)
                {
                    double damage = hasAfter
                        ? Math.Max(0d, beforeTarget.Health - afterTarget.Health)
                        : Math.Max(ResolveAttackDamage(attack), beforeTarget.Health);
                    EmitDamageNumber(targetPosition, damage, ResolveAttackColor(attack), "-");
                }

                if (hasAfter && afterTarget.Lifecycle == AutoDefenseEnemyLifecycle.Active)
                    EmitEnemyPresentationEvent(afterTarget, EnemyPresentationEventKind.OnHit);
                else if (hadBefore)
                    EmitEnemyDeathFeedback(beforeTarget);
            }
        }

        private void EmitMissingKillFeedback(AutoDefenseRuntimeSnapshot beforeCombat, AutoDefenseRuntimeSnapshot afterCombat, int maxKills, AttackDefinitionAsset attack)
        {
            if (maxKills <= 0 || beforeCombat == null) return;
            int emitted = 0;
            for (int i = 0; i < beforeCombat.Enemies.Count; i++)
            {
                AutoDefenseEnemySnapshot enemy = beforeCombat.Enemies[i];
                if (enemy.Lifecycle != AutoDefenseEnemyLifecycle.Active) continue;
                if (TryFindEnemy(afterCombat, enemy.Id, out AutoDefenseEnemySnapshot afterEnemy) &&
                    afterEnemy.Lifecycle == AutoDefenseEnemyLifecycle.Active)
                {
                    continue;
                }

                if (attack != null)
                {
                    Vector3 origin = ResolveTowerMuzzlePosition(attack);
                    Vector3 destination = CreateEnemyAimPosition(enemy.Position);
                    PlayWeaponFirePresentation(attack, destination);
                    EmitAttackTracer(origin, destination, ResolveAttackColor(attack));
                    EmitAttackEvent(attack, AttackPresentationEventKind.OnImpact, CreateEnemyAimPosition(enemy.Position), enemy.Id);
                }

                EmitDamageNumber(CreateEnemyAimPosition(enemy.Position), Math.Max(ResolveAttackDamage(attack), enemy.Health), ResolveAttackColor(attack), "-");
                EmitEnemyDeathFeedback(enemy);
                RecordEnemyDefeatedForRewards(enemy);
                emitted++;
                if (emitted >= maxKills) return;
            }
        }

        private bool TryDamageEnemyWithPresentation(AutoDefenseEnemySnapshot enemy, AttackDefinitionAsset attack, double damageAmount, out bool killed)
        {
            killed = false;
            Vector3 origin = ResolveTowerMuzzlePosition(attack);
            Vector3 destination = CreateEnemyAimPosition(enemy.Position);
            PlayWeaponFirePresentation(attack, destination);
            EmitAttackEvent(attack, AttackPresentationEventKind.OnCast, origin);
            EmitAttackEvent(attack, AttackPresentationEventKind.OnFire, origin);
            EmitAttackTracer(origin, destination, ResolveAttackColor(attack));
            EmitAttackEvent(attack, AttackPresentationEventKind.OnImpact, destination, enemy.Id);
            return TryApplyVisibleEnemyDamage(enemy, attack, damageAmount, destination, out killed);
        }

        private bool TryLaunchVisibleProjectileAtEnemy(AutoDefenseEnemySnapshot enemy, AttackDefinitionAsset attack, double damageAmount)
        {
            if (_projectiles == null || attack == null || attack.Delivery == null) return false;
            if (attack.Delivery.Mode != AttackRecipeDeliveryMode.Projectile) return false;
            if (string.IsNullOrWhiteSpace(attack.Delivery.ProjectileDefinitionId)) return false;
            ProjectileDefinition projectile = FindProjectileDefinition(new ProjectileDefinitionId(attack.Delivery.ProjectileDefinitionId));
            if (projectile == null) return false;

            Vector3 origin = ResolveTowerMuzzlePosition(attack);
            Vector3 destination = CreateEnemyAimPosition(enemy.Position);
            PlayWeaponFirePresentation(attack, destination);
            MuzzleProjectileLaunchCount++;
            int impactDelayTicks = CalculateProjectileImpactDelayTicks(origin, destination, projectile.Speed);
            var launchRequest = new ProjectileLaunchRequest(
                projectile.Id,
                new AttackSourceId("source.idle-auto-defense.visual." + BasicIdleAutoDefenseGame.SanitizeContentSetOperationSegment(attack.Id)),
                new AttackDefinitionId(attack.Id),
                new AttackSourceSnapshot(new AttackSourceId("source.idle-auto-defense.visual." + BasicIdleAutoDefenseGame.SanitizeContentSetOperationSegment(attack.Id)), new CombatantId(RuntimeObjectiveId)),
                origin,
                destination);

            EmitAttackEvent(attack, AttackPresentationEventKind.OnCast, origin);
            EmitAttackEvent(attack, AttackPresentationEventKind.OnFire, origin);
            ProjectileLaunchResult launch = _projectiles.Launch(launchRequest);
            if (!launch.Succeeded) return false;

            ProjectileLaunchCount++;
            RecordProjectileVisualSpawn(attack);
            _pendingProjectileImpacts.Add(new PendingProjectileImpact(
                launch.ProjectileId,
                enemy.Id,
                attack,
                destination,
                damageAmount,
                impactDelayTicks));
            return true;
        }

        private bool TryKillEnemyAfterFeedback(AutoDefenseEnemySnapshot enemy)
        {
            if (_runtime == null || enemy.Id <= 0 || !_runtime.TryKillEnemy(enemy.Id)) return false;
            EmitEnemyDeathFeedback(enemy);
            _sampleEnemyDamageById.Remove(enemy.Id);
            RecordEnemyDefeatedForRewards(enemy);
            return true;
        }

        private bool TryApplyVisibleEnemyDamage(AutoDefenseEnemySnapshot enemy, AttackDefinitionAsset attack, double damageAmount, Vector3 impactPosition, out bool killed)
        {
            killed = false;
            if (_runtime == null || enemy.Id <= 0 || damageAmount <= 0d) return false;
            if (!TryFindActiveEnemy(enemy.Id, out AutoDefenseEnemySnapshot activeEnemy)) return false;

            double previousDamage = _sampleEnemyDamageById.TryGetValue(activeEnemy.Id, out double storedDamage) ? storedDamage : 0d;
            double remainingBefore = Math.Max(0d, activeEnemy.Health - previousDamage);
            double appliedDamage = Math.Max(0.25d, damageAmount);
            _sampleEnemyDamageById[activeEnemy.Id] = previousDamage + appliedDamage;

            EmitEnemyPresentationEvent(activeEnemy, EnemyPresentationEventKind.OnHit);
            EmitDamageNumber(impactPosition, appliedDamage, ResolveAttackColor(attack), "-");

            if (remainingBefore - appliedDamage > 0.001d)
            {
                EnemyDamageSurvivedCount++;
                return true;
            }
            if (!TryKillEnemyAfterFeedback(activeEnemy)) return true;
            killed = true;
            return true;
        }

        private bool TryFindProjectileImpactTarget(PendingProjectileImpact pending, out AutoDefenseEnemySnapshot target)
        {
            if (TryFindActiveEnemy(pending.TargetEnemyId, out target))
                return true;

            target = default;
            if (_runtime == null) return false;
            AutoDefenseRuntimeSnapshot snapshot = _runtime.CreateSnapshot();
            float bestDistance = float.MaxValue;
            bool found = false;
            for (int i = 0; i < snapshot.Enemies.Count; i++)
            {
                AutoDefenseEnemySnapshot candidate = snapshot.Enemies[i];
                if (candidate.Lifecycle != AutoDefenseEnemyLifecycle.Active) continue;
                float distance = Vector3.Distance(CreateEnemyAimPosition(candidate.Position), pending.Destination);
                if (distance > RewardDraftSettings.ProjectileRetargetRadius) continue;
                if (found && distance >= bestDistance) continue;
                target = candidate;
                bestDistance = distance;
                found = true;
            }

            if (found)
                ProjectileImpactRetargetCount++;
            return found;
        }

        private bool TrySelectPriorityEnemy(AutoDefenseRuntimeSnapshot snapshot, out AutoDefenseEnemySnapshot selected)
        {
            selected = default;
            if (snapshot == null || snapshot.Enemies.Count == 0) return false;
            bool hasSelected = false;
            for (int i = 0; i < snapshot.Enemies.Count; i++)
            {
                AutoDefenseEnemySnapshot enemy = snapshot.Enemies[i];
                if (enemy.Lifecycle != AutoDefenseEnemyLifecycle.Active) continue;
                if (hasSelected && enemy.ObjectiveProgress <= selected.ObjectiveProgress) continue;
                selected = enemy;
                hasSelected = true;
            }

            return hasSelected;
        }

        private bool TrySelectPriorityEnemyWithinRange(AutoDefenseRuntimeSnapshot snapshot, double range, out AutoDefenseEnemySnapshot selected)
        {
            selected = default;
            if (snapshot == null || snapshot.Enemies.Count == 0) return false;
            bool hasSelected = false;
            bool sawActiveEnemy = false;
            float maxRange = (float)Math.Max(0.1d, range);
            for (int i = 0; i < snapshot.Enemies.Count; i++)
            {
                AutoDefenseEnemySnapshot enemy = snapshot.Enemies[i];
                if (enemy.Lifecycle != AutoDefenseEnemyLifecycle.Active) continue;
                sawActiveEnemy = true;
                if (Vector3.Distance(enemy.Position, Vector3.zero) > maxRange) continue;
                if (hasSelected && enemy.ObjectiveProgress <= selected.ObjectiveProgress) continue;
                selected = enemy;
                hasSelected = true;
            }

            if (!hasSelected && sawActiveEnemy)
                RangeRejectedTargetCount++;
            return hasSelected;
        }

        private double ResolveManualTowerDamage()
        {
            IdleAutoDefenseModuleRule module = ResolveModuleRule(IdleAutoDefenseModuleRole.StartingProjectile);
            double baseDamage = (module == null ? ManualTowerBaseDamage : module.BaseDamage) +
                DamageUpgradeRank * (_activeGameRules == null ? ManualTowerDamageRankBonus : _activeGameRules.ManualDamageRankBonus) +
                RangeUpgradeRank * (_activeGameRules == null ? ManualTowerRangeRankBonus : _activeGameRules.ManualRangeRankDamageBonus) +
                DirectDamageBonus;
            return ResolveModuleDamage(baseDamage);
        }

        private double ResolveManualTowerRange()
        {
            IdleAutoDefenseModuleRule module = ResolveModuleRule(IdleAutoDefenseModuleRole.StartingProjectile);
            double maximumRange = _activeGameRules == null ? ManualTowerMaximumRange : _activeGameRules.ManualMaximumRange;
            double baseRange = module == null ? ManualTowerBaseRange : module.BaseRange;
            double rankBonus = _activeGameRules == null ? ManualTowerRangeRankBonus : _activeGameRules.ManualRangeRankBonus;
            return Math.Min(maximumRange, baseRange + RangeUpgradeRank * rankBonus);
        }

        private double ResolveModuleRange(double baseRange)
        {
            double maximumRange = (_activeGameRules == null ? ManualTowerMaximumRange : _activeGameRules.ManualMaximumRange) + 1d;
            double rankBonus = _activeGameRules == null ? ModuleRangeRankBonus : _activeGameRules.ModuleRangeRankBonus;
            return Math.Min(maximumRange, baseRange + RangeUpgradeRank * rankBonus);
        }

        private double ResolveModuleDamage(double baseDamage)
        {
            double multiplier = 1d + Math.Max(0d, _rewardDamageMultiplierBonus);
            if (OverdriveActive)
                multiplier *= _activeGameRules == null ? OverdriveDamageMultiplier : _activeGameRules.OverdriveDamageMultiplier;
            return Math.Max(1d, baseDamage * multiplier);
        }

        private IdleAutoDefenseModuleRule ResolveModuleRule(IdleAutoDefenseModuleRole role)
        {
            return _activeGameRules == null ? null : _activeGameRules.GetModule(role);
        }

        private bool UnlockPulseBeamModule()
        {
            if (PulseBeamUnlocked) return false;
            PulseBeamUnlocked = true;
            IdleAutoDefenseModuleRule module = ResolveModuleRule(IdleAutoDefenseModuleRole.PrecisionBeam);
            CreateWeaponPresentation(
                module == null ? BasicIdleAutoDefenseGame.PulseCannonWeaponId.Value : module.WeaponId,
                module == null ? BasicIdleAutoDefenseGame.PulseAttackId.Value : module.AttackId,
                true);
            return true;
        }

        private bool UnlockArcBurstModule()
        {
            if (ArcBurstUnlocked) return false;
            ArcBurstUnlocked = true;
            IdleAutoDefenseModuleRule module = ResolveModuleRule(IdleAutoDefenseModuleRole.AreaBurst);
            CreateWeaponPresentation(
                module == null ? BasicIdleAutoDefenseGame.ArcBurstTowerWeaponId.Value : module.WeaponId,
                module == null ? BasicIdleAutoDefenseGame.ArcBurstAttackId.Value : module.AttackId,
                true);
            return true;
        }

        private bool UnlockHomingPulseModule()
        {
            if (HomingPulseUnlocked) return false;
            HomingPulseUnlocked = true;
            IdleAutoDefenseModuleRule module = ResolveModuleRule(IdleAutoDefenseModuleRole.HomingProjectile);
            CreateWeaponPresentation(
                module == null ? BasicIdleAutoDefenseGame.HomingSpireWeaponId.Value : module.WeaponId,
                module == null ? BasicIdleAutoDefenseGame.HomingPulseAttackId.Value : module.AttackId,
                true);
            return true;
        }

        public bool TryChooseRewardDraftChoice(int choiceIndex)
        {
            if (choiceIndex < 0 || choiceIndex >= _rewardDraftChoices.Length) return false;
            IdleAutoDefenseRewardDraftChoice choice = _rewardDraftChoices[choiceIndex];
            if (!ApplyRewardDraftChoice(choice)) return false;
            _selectedRewardIds.Add(choice.Id);
            EmitRewardChoiceFeedback(choice);

            SelectedUpgradeCount++;
            RewardDraftSelectionCount++;
            if (choice.Rarity == IdleAutoDefenseRewardRarity.Epic) EpicRewardSelectionCount++;
            if (choice.Rarity == IdleAutoDefenseRewardRarity.Legendary) LegendaryRewardSelectionCount++;

            _rewardDraftChoices = Array.Empty<IdleAutoDefenseRewardDraftChoice>();
            OpenNextQueuedRewardDraft();
            return true;
        }

        public bool TryChooseRewardDraftHotkey(int hotkey)
        {
            return TryChooseRewardDraftChoice(hotkey - 1);
        }

        public void RequestRewardDraft(IdleAutoDefenseRewardDraftKind kind)
        {
            QueueOrOpenRewardDraft(kind);
        }

        private bool ApplyRewardDraftChoice(IdleAutoDefenseRewardDraftChoice choice)
        {
            if (choice == null) return false;
            if (!string.IsNullOrEmpty(choice.TargetWeaponId) && !choice.IsUnlock)
                RecordWeaponRewardProgress(choice.TargetWeaponId, choice.Rarity);
            else if (!choice.IsUnlock)
                IncrementRank(_baseRewardRanks, choice.DedupeKey);

            switch (choice.EffectKind)
            {
                case IdleAutoDefenseRewardEffectKind.UnlockWeapon:
                    return TryUnlockWeapon(choice.TargetWeaponId);
                case IdleAutoDefenseRewardEffectKind.DamageRank:
                    DamageUpgradeRank += Math.Max(1, (int)Math.Round(choice.Amount));
                    return true;
                case IdleAutoDefenseRewardEffectKind.FireRateRank:
                    AttackSpeedUpgradeRank += Math.Max(1, (int)Math.Round(choice.Amount));
                    return true;
                case IdleAutoDefenseRewardEffectKind.RangeRank:
                    RangeUpgradeRank += Math.Max(1, (int)Math.Round(choice.Amount));
                    return true;
                case IdleAutoDefenseRewardEffectKind.Repair:
                    RepairUpgradeRank++;
                    if (_runtime != null)
                    {
                        double maximumHealth = _activeGameRules == null ? 8d : _activeGameRules.DraftRepairMaximumHealth;
                        double baseHeal = _activeGameRules == null ? 28d : _activeGameRules.DraftRepairBaseHeal;
                        double healPerRank = _activeGameRules == null ? 4d : _activeGameRules.DraftRepairHealPerRank;
                        _runtime.Objective.Health.ChangeMaximumHealth(_runtime.Objective.Health.MaximumHealth + maximumHealth, MaximumChangePolicy.PreserveAbsolute);
                        _runtime.Objective.Health.Heal(baseHeal + RepairUpgradeRank * healPerRank);
                    }

                    return true;
                case IdleAutoDefenseRewardEffectKind.RewardMultiplier:
                    RewardCreditMultiplierBonus += Math.Max(0.05d, choice.Amount);
                    return true;
                case IdleAutoDefenseRewardEffectKind.ProjectileSpeed:
                    ProjectileSpeedMultiplier += Math.Max(0.05d, choice.Amount);
                    return true;
                case IdleAutoDefenseRewardEffectKind.ExtraProjectile:
                    _shardVolleyBonus += Math.Max(1, (int)Math.Round(choice.Amount));
                    return true;
                case IdleAutoDefenseRewardEffectKind.PulsePower:
                    _pulseBeamBonus += Math.Max(1, (int)Math.Round(choice.Amount));
                    return true;
                case IdleAutoDefenseRewardEffectKind.ArcPower:
                    _arcBurstBonus += Math.Max(1, (int)Math.Round(choice.Amount));
                    return true;
                case IdleAutoDefenseRewardEffectKind.HomingPower:
                    _homingPulseBonus += Math.Max(1, (int)Math.Round(choice.Amount));
                    return true;
                case IdleAutoDefenseRewardEffectKind.GlobalDamageMultiplier:
                    _rewardDamageMultiplierBonus += Math.Max(0.05d, choice.Amount);
                    return true;
                default:
                    UnsupportedUpgradeIntentCount++;
                    return false;
            }
        }

        private void RecordWeaponRewardProgress(string weaponId, IdleAutoDefenseRewardRarity rarity)
        {
            if (string.IsNullOrWhiteSpace(weaponId)) return;
            if (rarity == IdleAutoDefenseRewardRarity.Epic)
                IncrementRank(_weaponEpicUpgradeRanks, weaponId);
            else if (rarity == IdleAutoDefenseRewardRarity.Legendary)
                _weaponLegendaryUnlocks.Add(weaponId);
            else
                IncrementRank(_weaponNormalUpgradeRanks, weaponId);
        }

        private static void IncrementRank(Dictionary<string, int> ranks, string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            ranks[key] = ranks.TryGetValue(key, out int current) ? current + 1 : 1;
        }

        private bool TryUnlockWeapon(string weaponId)
        {
            IdleAutoDefenseModuleRule module = _activeGameRules == null ? null : _activeGameRules.GetModuleByWeaponId(weaponId);
            if (module == null)
            {
                if (string.Equals(weaponId, BasicIdleAutoDefenseGame.PulseCannonWeaponId.Value, StringComparison.OrdinalIgnoreCase)) return UnlockPulseBeamModule();
                if (string.Equals(weaponId, BasicIdleAutoDefenseGame.ArcBurstTowerWeaponId.Value, StringComparison.OrdinalIgnoreCase)) return UnlockArcBurstModule();
                if (string.Equals(weaponId, BasicIdleAutoDefenseGame.HomingSpireWeaponId.Value, StringComparison.OrdinalIgnoreCase)) return UnlockHomingPulseModule();
                return string.Equals(weaponId, BasicIdleAutoDefenseGame.ShardLauncherWeaponId.Value, StringComparison.OrdinalIgnoreCase);
            }

            if (module.Role == IdleAutoDefenseModuleRole.PrecisionBeam) return UnlockPulseBeamModule();
            if (module.Role == IdleAutoDefenseModuleRole.AreaBurst) return UnlockArcBurstModule();
            if (module.Role == IdleAutoDefenseModuleRole.HomingProjectile) return UnlockHomingPulseModule();
            return module.StartsUnlocked;
        }

        private bool IsWeaponUnlocked(string weaponId)
        {
            IdleAutoDefenseModuleRule module = _activeGameRules == null ? null : _activeGameRules.GetModuleByWeaponId(weaponId);
            if (module == null)
            {
                if (string.Equals(weaponId, BasicIdleAutoDefenseGame.ShardLauncherWeaponId.Value, StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(weaponId, BasicIdleAutoDefenseGame.PulseCannonWeaponId.Value, StringComparison.OrdinalIgnoreCase)) return PulseBeamUnlocked;
                if (string.Equals(weaponId, BasicIdleAutoDefenseGame.ArcBurstTowerWeaponId.Value, StringComparison.OrdinalIgnoreCase)) return ArcBurstUnlocked;
                if (string.Equals(weaponId, BasicIdleAutoDefenseGame.HomingSpireWeaponId.Value, StringComparison.OrdinalIgnoreCase)) return HomingPulseUnlocked;
                return false;
            }

            if (module.Role == IdleAutoDefenseModuleRole.PrecisionBeam) return PulseBeamUnlocked;
            if (module.Role == IdleAutoDefenseModuleRole.AreaBurst) return ArcBurstUnlocked;
            if (module.Role == IdleAutoDefenseModuleRole.HomingProjectile) return HomingPulseUnlocked;
            return module.StartsUnlocked;
        }

        private void RecordEnemyDefeatedForRewards(AutoDefenseEnemySnapshot enemy)
        {
            if (enemy.Id <= 0 || !_rewardedEnemyDefeatIds.Add(enemy.Id)) return;
            EnemyDefinitionAsset definition = FindEnemyDefinitionForPresentation(enemy.SpawnableId);
            long authoredReward = definition == null || definition.Stats == null
                ? KillRewardCredits
                : Math.Max(0, definition.Stats.RewardValue);
            _pendingAuthoredKillCredits += authoredReward;
            EmitKenneySpriteBurst("Credit Pickup Burst", "Art/currency_coin_gold", CreateEnemyAimPosition(enemy.Position), new Color(1f, 0.88f, 0.18f), 0.62f, 0.72f, 0.72f, 65);
            if (IsBossEnemy(enemy))
            {
                BossDefeatCount++;
                AddCommanderExperience(RewardDraftSettings.BossEnemyExperience);
                QueueOrOpenRewardDraft(IdleAutoDefenseRewardDraftKind.BossDefeated);
            }
            else if (IsEliteEnemy(enemy))
            {
                EliteDefeatCount++;
                AddCommanderExperience(RewardDraftSettings.EliteEnemyExperience);
                QueueOrOpenRewardDraft(IdleAutoDefenseRewardDraftKind.EliteDefeated);
            }
            else
            {
                AddCommanderExperience(RewardDraftSettings.NormalEnemyExperience);
            }
        }

        private void AwardExperienceForCompletedWaves()
        {
            if (_encounter == null) return;
            EncounterSnapshot snapshot = _encounter.CreateSnapshot();
            for (int i = 0; i < snapshot.Waves.Count; i++)
            {
                WaveProgressSnapshot wave = snapshot.Waves[i];
                if (!wave.Emitted || string.IsNullOrWhiteSpace(wave.WaveId.Value)) continue;
                if (!_rewardedCompletedWaveIds.Add(wave.WaveId.Value)) continue;
                WaveRewardExperienceCount++;
                AddCommanderExperience(RewardDraftSettings.WaveCompletionExperience);
            }
        }

        private void AddCommanderExperience(long amount)
        {
            if (amount <= 0) return;
            CommanderExperience += amount;
            while (CommanderExperience >= ExperienceToNextLevel)
            {
                CommanderExperience -= ExperienceToNextLevel;
                CommanderLevel++;
                QueueOrOpenRewardDraft(IdleAutoDefenseRewardDraftKind.LevelUp);
            }
        }

        private void QueueOrOpenRewardDraft(IdleAutoDefenseRewardDraftKind kind)
        {
            if (RewardDraftActive)
            {
                _queuedRewardDrafts.Enqueue(kind);
                return;
            }

            OpenRewardDraft(kind);
        }

        private void OpenNextQueuedRewardDraft()
        {
            while (_queuedRewardDrafts.Count > 0 && !RewardDraftActive)
                OpenRewardDraft(_queuedRewardDrafts.Dequeue());
        }

        private void OpenRewardDraft(IdleAutoDefenseRewardDraftKind kind)
        {
            IdleAutoDefenseRewardDraftChoice[] choices = GenerateRewardDraftChoices(kind);
            if (choices.Length == 0) return;
            _activeRewardDraftKind = kind;
            _rewardDraftChoices = choices;
            if (FirstRewardDraftSeconds < 0f)
                FirstRewardDraftSeconds = SurvivalSeconds;
            RewardDraftOpenedCount++;
            if (kind == IdleAutoDefenseRewardDraftKind.LevelUp) LevelUpRewardDraftCount++;
            else if (kind == IdleAutoDefenseRewardDraftKind.EliteDefeated) EliteRewardDraftCount++;
            else if (kind == IdleAutoDefenseRewardDraftKind.BossDefeated) BossRewardDraftCount++;
        }

        private IdleAutoDefenseRewardDraftChoice[] GenerateRewardDraftChoices(IdleAutoDefenseRewardDraftKind kind)
        {
            var candidates = new List<IdleAutoDefenseRewardDraftChoice>();
            AddWeaponUnlockRewardCandidates(candidates, kind);
            AddOwnedWeaponRewardCandidates(candidates, kind);
            AddBaseRewardCandidates(candidates, kind);
            return SelectWeightedRewardChoices(candidates, kind);
        }

        private void AddWeaponUnlockRewardCandidates(List<IdleAutoDefenseRewardDraftChoice> candidates, IdleAutoDefenseRewardDraftKind kind)
        {
            IReadOnlyList<IdleAutoDefenseWeaponUnlockReward> unlocks = RewardDraftCatalog.WeaponUnlocks;
            for (int i = 0; i < unlocks.Count; i++)
                AddWeaponUnlockRewardCandidate(candidates, unlocks[i], kind);
        }

        private void AddWeaponUnlockRewardCandidate(List<IdleAutoDefenseRewardDraftChoice> candidates, IdleAutoDefenseWeaponUnlockReward unlock, IdleAutoDefenseRewardDraftKind kind)
        {
            if (unlock == null) return;
            if (!unlock.IsEligible(kind) || !RewardPrerequisitesMet(unlock.PrerequisiteIds)) return;
            string weaponId = unlock.WeaponId;
            if (IsWeaponUnlocked(weaponId)) return;
            candidates.Add(new IdleAutoDefenseRewardDraftChoice(
                unlock.Id,
                unlock.DisplayName,
                unlock.GetRarity(kind),
                "Unlock",
                ResolveWeaponDisplayName(weaponId),
                unlock.EffectDescription,
                string.Empty,
                true,
                weaponId,
                IdleAutoDefenseRewardEffectKind.UnlockWeapon,
                1d,
                "unlock." + weaponId,
                unlock.Weight));
        }

        private void AddOwnedWeaponRewardCandidates(List<IdleAutoDefenseRewardDraftChoice> candidates, IdleAutoDefenseRewardDraftKind kind)
        {
            IReadOnlyList<string> weaponIds = RewardDraftCatalog.GetWeaponIds();
            for (int i = 0; i < weaponIds.Count; i++)
            {
                if (!IsWeaponUnlocked(weaponIds[i])) continue;
                IdleAutoDefenseRewardDraftChoice choice = CreateNextWeaponRewardChoice(weaponIds[i], kind);
                if (choice != null) candidates.Add(choice);
            }
        }

        private IdleAutoDefenseRewardDraftChoice CreateNextWeaponRewardChoice(string weaponId, IdleAutoDefenseRewardDraftKind kind)
        {
            string targetName = ResolveWeaponDisplayName(weaponId);
            int normalRank = GetRank(_weaponNormalUpgradeRanks, weaponId);
            if (normalRank < RewardDraftSettings.NormalInvestmentsForEpic)
            {
                IdleAutoDefenseWeaponRewardDefinition reward = RewardDraftCatalog.GetNormalWeaponReward(weaponId, normalRank);
                return reward == null || !reward.IsEligible(kind) || !reward.IsAvailableAt(normalRank, 0) || !RewardPrerequisitesMet(reward.PrerequisiteIds)
                    ? null
                    : CreateRewardChoice(weaponId, reward, targetName);
            }

            int epicRank = GetRank(_weaponEpicUpgradeRanks, weaponId);
            if (epicRank < RewardDraftSettings.EpicInvestmentsForLegendary)
            {
                IdleAutoDefenseWeaponRewardDefinition reward = RewardDraftCatalog.GetEpicWeaponReward(weaponId, epicRank);
                return reward == null || !reward.IsEligible(kind) || !reward.IsAvailableAt(normalRank, epicRank) || !RewardPrerequisitesMet(reward.PrerequisiteIds)
                    ? null
                    : CreateRewardChoice(weaponId, reward, targetName);
            }

            if (!_weaponLegendaryUnlocks.Contains(weaponId))
            {
                IdleAutoDefenseWeaponRewardDefinition reward = RewardDraftCatalog.GetLegendaryWeaponReward(weaponId);
                return reward == null || !reward.IsEligible(kind) || !reward.IsAvailableAt(normalRank, epicRank) || !RewardPrerequisitesMet(reward.PrerequisiteIds)
                    ? null
                    : CreateRewardChoice(weaponId, reward, targetName);
            }

            return null;
        }

        private IdleAutoDefenseRewardDraftChoice CreateRewardChoice(string weaponId, IdleAutoDefenseWeaponRewardDefinition reward, string targetName)
        {
            return new IdleAutoDefenseRewardDraftChoice(
                reward.Id,
                reward.DisplayName,
                reward.Rarity,
                reward.TypeName,
                targetName,
                reward.EffectDescription,
                string.Empty,
                false,
                weaponId,
                reward.EffectKind,
                reward.Amount,
                weaponId + "." + reward.TierKey,
                reward.Weight);
        }

        private void AddBaseRewardCandidates(List<IdleAutoDefenseRewardDraftChoice> candidates, IdleAutoDefenseRewardDraftKind kind)
        {
            IReadOnlyList<IdleAutoDefenseBaseRewardDefinition> rewards = RewardDraftCatalog.BaseRewards;
            for (int i = 0; i < rewards.Count; i++)
                AddBaseRewardCandidate(candidates, rewards[i], kind);
        }

        private void AddBaseRewardCandidate(List<IdleAutoDefenseRewardDraftChoice> candidates, IdleAutoDefenseBaseRewardDefinition reward, IdleAutoDefenseRewardDraftKind kind)
        {
            if (reward == null) return;
            if (!reward.IsEligible(kind) || !RewardPrerequisitesMet(reward.PrerequisiteIds)) return;
            string key = reward.Key;
            int rank = GetRank(_baseRewardRanks, key);
            if (rank >= reward.MaxRank) return;
            candidates.Add(new IdleAutoDefenseRewardDraftChoice(
                reward.Id,
                reward.DisplayName,
                reward.Rarity,
                reward.TypeName,
                reward.TargetName,
                reward.EffectDescription,
                string.Empty,
                false,
                string.Empty,
                reward.EffectKind,
                reward.Amount,
                key,
                reward.Weight));
        }

        private IdleAutoDefenseRewardDraftChoice[] SelectWeightedRewardChoices(List<IdleAutoDefenseRewardDraftChoice> candidates, IdleAutoDefenseRewardDraftKind kind)
        {
            if (candidates == null || candidates.Count == 0) return Array.Empty<IdleAutoDefenseRewardDraftChoice>();
            int choiceCount = RewardDraftSettings.ChoiceCount;
            var selected = new List<IdleAutoDefenseRewardDraftChoice>(choiceCount);
            var random = new System.Random(20260623 + CommanderLevel * 17 + (int)kind * 1009 + _rewardDraftSeed++ * 97);
            var remaining = new List<IdleAutoDefenseRewardDraftChoice>(candidates);
            TrySelectPreferredEarlyUnlock(remaining, selected, kind);
            TrySelectExcitingReward(remaining, selected);
            while (selected.Count < choiceCount && remaining.Count > 0)
            {
                double totalWeight = 0d;
                for (int i = 0; i < remaining.Count; i++)
                    totalWeight += CalculateRewardChoiceWeight(remaining[i], kind);

                double roll = random.NextDouble() * Math.Max(0.001d, totalWeight);
                int selectedIndex = 0;
                for (int i = 0; i < remaining.Count; i++)
                {
                    roll -= CalculateRewardChoiceWeight(remaining[i], kind);
                    if (roll > 0d) continue;
                    selectedIndex = i;
                    break;
                }

                IdleAutoDefenseRewardDraftChoice choice = remaining[selectedIndex];
                selected.Add(WithRewardHotkey(choice, selected.Count + 1));
                RemoveRewardChoicesWithDedupeKey(remaining, choice.DedupeKey);
            }

            return selected.ToArray();
        }

        private bool TrySelectPreferredEarlyUnlock(List<IdleAutoDefenseRewardDraftChoice> remaining, List<IdleAutoDefenseRewardDraftChoice> selected, IdleAutoDefenseRewardDraftKind kind)
        {
            if (remaining == null || selected == null || selected.Count > 0) return false;
            if (kind != IdleAutoDefenseRewardDraftKind.LevelUp || CommanderLevel > 2) return false;
            for (int i = 0; i < remaining.Count; i++)
            {
                IdleAutoDefenseRewardDraftChoice choice = remaining[i];
                if (choice == null || !choice.IsUnlock) continue;
                selected.Add(WithRewardHotkey(choice, selected.Count + 1));
                RemoveRewardChoicesWithDedupeKey(remaining, choice.DedupeKey);
                return true;
            }

            return false;
        }

        private static bool TrySelectExcitingReward(List<IdleAutoDefenseRewardDraftChoice> remaining, List<IdleAutoDefenseRewardDraftChoice> selected)
        {
            if (remaining == null || selected == null || selected.Count >= 3) return false;
            for (int i = 0; i < selected.Count; i++)
                if (IsExcitingRewardChoice(selected[i]))
                    return false;

            int bestIndex = -1;
            IdleAutoDefenseRewardRarity bestRarity = IdleAutoDefenseRewardRarity.Common;
            for (int i = 0; i < remaining.Count; i++)
            {
                IdleAutoDefenseRewardDraftChoice choice = remaining[i];
                if (!IsExcitingRewardChoice(choice)) continue;
                if (bestIndex >= 0 && choice.Rarity < bestRarity) continue;
                bestIndex = i;
                bestRarity = choice.Rarity;
            }

            if (bestIndex < 0) return false;
            IdleAutoDefenseRewardDraftChoice selectedChoice = remaining[bestIndex];
            selected.Add(WithRewardHotkey(selectedChoice, selected.Count + 1));
            RemoveRewardChoicesWithDedupeKey(remaining, selectedChoice.DedupeKey);
            return true;
        }

        private static bool IsExcitingRewardChoice(IdleAutoDefenseRewardDraftChoice choice)
        {
            if (choice == null) return false;
            if (choice.IsUnlock) return true;
            if (choice.Rarity >= IdleAutoDefenseRewardRarity.Epic) return true;
            switch (choice.EffectKind)
            {
                case IdleAutoDefenseRewardEffectKind.ExtraProjectile:
                case IdleAutoDefenseRewardEffectKind.PulsePower:
                case IdleAutoDefenseRewardEffectKind.ArcPower:
                case IdleAutoDefenseRewardEffectKind.HomingPower:
                    return true;
                case IdleAutoDefenseRewardEffectKind.GlobalDamageMultiplier:
                case IdleAutoDefenseRewardEffectKind.ProjectileSpeed:
                    return choice.Rarity >= IdleAutoDefenseRewardRarity.Rare;
                default:
                    return false;
            }
        }

        private static void RemoveRewardChoicesWithDedupeKey(List<IdleAutoDefenseRewardDraftChoice> choices, string dedupeKey)
        {
            if (choices == null) return;
            for (int i = choices.Count - 1; i >= 0; i--)
                if (choices[i] != null && string.Equals(choices[i].DedupeKey, dedupeKey, StringComparison.OrdinalIgnoreCase))
                    choices.RemoveAt(i);
        }

        private static IdleAutoDefenseRewardDraftChoice WithRewardHotkey(IdleAutoDefenseRewardDraftChoice choice, int hotkey)
        {
            return new IdleAutoDefenseRewardDraftChoice(
                choice.Id,
                choice.DisplayName,
                choice.Rarity,
                choice.TypeName,
                choice.TargetName,
                choice.EffectDescription,
                hotkey.ToString(CultureInfo.InvariantCulture),
                choice.IsUnlock,
                choice.TargetWeaponId,
                choice.EffectKind,
                choice.Amount,
                choice.DedupeKey,
                choice.Weight);
        }

        private double CalculateRewardChoiceWeight(IdleAutoDefenseRewardDraftChoice choice, IdleAutoDefenseRewardDraftKind kind)
        {
            double rarityWeight = RewardDraftSettings.GetRarityWeight(kind, choice.Rarity);
            if (choice.IsUnlock) rarityWeight *= RewardDraftSettings.GetUnlockWeightMultiplier(kind);
            return Math.Max(0.001d, rarityWeight * Math.Max(0.001d, choice.Weight));
        }

        private bool RewardPrerequisitesMet(IReadOnlyList<string> prerequisiteIds)
        {
            for (int i = 0; i < prerequisiteIds.Count; i++)
                if (!_selectedRewardIds.Contains(prerequisiteIds[i]))
                    return false;
            return true;
        }

        private string ResolveWeaponDisplayName(string weaponId)
        {
            for (int i = 0; i < _resolvedWeaponDefinitions.Length; i++)
            {
                WeaponDefinitionAsset weapon = _resolvedWeaponDefinitions[i];
                if (weapon == null || !string.Equals(weapon.Id, weaponId, StringComparison.OrdinalIgnoreCase)) continue;
                return string.IsNullOrWhiteSpace(weapon.DisplayName) ? weapon.Id : weapon.DisplayName;
            }

            if (string.Equals(weaponId, BasicIdleAutoDefenseGame.ShardLauncherWeaponId.Value, StringComparison.OrdinalIgnoreCase)) return "Shard Launcher";
            if (string.Equals(weaponId, BasicIdleAutoDefenseGame.PulseCannonWeaponId.Value, StringComparison.OrdinalIgnoreCase)) return "Pulse Beam";
            if (string.Equals(weaponId, BasicIdleAutoDefenseGame.ArcBurstTowerWeaponId.Value, StringComparison.OrdinalIgnoreCase)) return "Arc Burst";
            if (string.Equals(weaponId, BasicIdleAutoDefenseGame.HomingSpireWeaponId.Value, StringComparison.OrdinalIgnoreCase)) return "Homing Pulse";
            return string.IsNullOrWhiteSpace(weaponId) ? "Tower" : weaponId;
        }

        private static int GetRank(Dictionary<string, int> ranks, string key)
        {
            return ranks != null && !string.IsNullOrWhiteSpace(key) && ranks.TryGetValue(key, out int rank) ? rank : 0;
        }

        private bool IsEliteEnemy(AutoDefenseEnemySnapshot enemy)
        {
            string eliteId = _activeGameRules == null || string.IsNullOrWhiteSpace(_activeGameRules.EliteEnemyId)
                ? BasicIdleAutoDefenseGame.EliteEnemySpawnableId.Value
                : _activeGameRules.EliteEnemyId;
            return string.Equals(enemy.SpawnableId.Value, eliteId, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsBossEnemy(AutoDefenseEnemySnapshot enemy)
        {
            string bossId = _activeGameRules == null || string.IsNullOrWhiteSpace(_activeGameRules.BossEnemyId)
                ? BasicIdleAutoDefenseGame.BossEnemySpawnableId.Value
                : _activeGameRules.BossEnemyId;
            return string.Equals(enemy.SpawnableId.Value, bossId, StringComparison.OrdinalIgnoreCase);
        }

        private static string SanitizeRewardSegment(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "none";
            return value.Replace(" ", "-").Replace("/", "-").Replace("\\", "-").Replace(":", "-").ToLowerInvariant();
        }

        private void EmitSpawnFeedbackForNewEnemies()
        {
            if (_runtime == null) return;
            AutoDefenseRuntimeSnapshot snapshot = _runtime.CreateSnapshot();
            for (int i = 0; i < snapshot.Enemies.Count; i++)
            {
                AutoDefenseEnemySnapshot enemy = snapshot.Enemies[i];
                if (enemy.Lifecycle != AutoDefenseEnemyLifecycle.Active) continue;
                if (!_seenEnemyIds.Add(enemy.Id)) continue;
                float distance = Vector3.Distance(enemy.Position, Vector3.zero);
                _minimumEnemySpawnDistance = Mathf.Min(_minimumEnemySpawnDistance, distance);
                if (distance > ManualTowerBaseRange + 0.5d)
                    EnemiesSpawnedBeyondStartingRangeCount++;
                if (IsEliteEnemy(enemy) || IsBossEnemy(enemy))
                    EliteOrBossSpawnCount++;
                EmitEnemyPresentationEvent(enemy, EnemyPresentationEventKind.OnSpawn);
            }
        }

        private void ObserveEnemyPressure(AutoDefenseRuntimeSnapshot snapshot)
        {
            if (snapshot == null) return;
            for (int i = 0; i < snapshot.Enemies.Count; i++)
            {
                AutoDefenseEnemySnapshot enemy = snapshot.Enemies[i];
                if (enemy.Lifecycle != AutoDefenseEnemyLifecycle.Active) continue;
                _closestEnemyDistanceToObjective = Mathf.Min(_closestEnemyDistanceToObjective, Vector3.Distance(enemy.Position, Vector3.zero));
            }
        }

        private void EmitEnemyDeathFeedback(AutoDefenseEnemySnapshot enemy)
        {
            if (enemy.Id <= 0 || !_enemyDeathPresentationIds.Add(enemy.Id)) return;
            EmitEnemyPresentationEvent(enemy, EnemyPresentationEventKind.OnDeath);
        }

        private void EmitEnemyPresentationEvent(AutoDefenseEnemySnapshot enemy, EnemyPresentationEventKind eventKind)
        {
            EnemyPresentationEventCount++;
            EnemyDefinitionAsset definition = FindEnemyDefinitionForPresentation(enemy.SpawnableId);
            bool emittedVfx = false;
            bool emittedAudio = false;
            Vector3 position = CreateEnemyAimPosition(enemy.Position);
            IdleAutoDefenseEnemyModelPresentation presentation = BindEnemyModelPresentation(enemy);
            if (presentation != null)
            {
                if (eventKind == EnemyPresentationEventKind.OnHit && presentation.PlayHitFeedback())
                    EnemyHitFlashCount++;
                if (eventKind == EnemyPresentationEventKind.OnDeath && presentation.PlayDeathFeedback())
                    EnemyDeathPopCount++;
            }

            if (definition != null &&
                definition.Presentation != null &&
                definition.Presentation.TryGetEvent(eventKind, out EnemyPresentationEventRecipe recipe))
            {
                emittedVfx = EmitPresentationVfx(
                    recipe.VfxPrefab,
                    position,
                    "EnemyPresentation",
                    definition.Id,
                    string.Empty,
                    string.Empty,
                    eventKind.ToString(),
                    recipe.VfxPrefab,
                    string.Empty,
                    eventKind == EnemyPresentationEventKind.OnSpawn ? string.Empty : "enemy.center");
                emittedAudio = PlayPresentationAudio(recipe.AudioClip);
            }

            if (!emittedVfx)
                EmitFallbackPresentationVfx(position, ResolveEnemyEventColor(eventKind), 0.34f);
            if (!emittedAudio)
                PlayPresentationAudio(null);
        }

        private void UpdateEnemyModelPresentations(AutoDefenseRuntimeSnapshot snapshot, float deltaSeconds)
        {
            if (snapshot == null) return;
            for (int i = 0; i < snapshot.Enemies.Count; i++)
            {
                AutoDefenseEnemySnapshot enemy = snapshot.Enemies[i];
                if (enemy.Lifecycle != AutoDefenseEnemyLifecycle.Active) continue;
                IdleAutoDefenseEnemyModelPresentation presentation = BindEnemyModelPresentation(enemy);
                if (presentation == null) continue;
                if (presentation.FaceMovement(enemy.Position, deltaSeconds))
                    EnemyFacingUpdateCount++;
            }
        }

        private IdleAutoDefenseEnemyModelPresentation BindEnemyModelPresentation(AutoDefenseEnemySnapshot enemy)
        {
            if (enemy.Id <= 0) return null;
            if (_enemyPresentationsById.TryGetValue(enemy.Id, out IdleAutoDefenseEnemyModelPresentation cached) && cached != null)
                return cached;

            IdleAutoDefenseEnemyModelPresentation[] presentations = FindObjectsByType<IdleAutoDefenseEnemyModelPresentation>(FindObjectsSortMode.None);
            IdleAutoDefenseEnemyModelPresentation best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < presentations.Length; i++)
            {
                IdleAutoDefenseEnemyModelPresentation candidate = presentations[i];
                if (candidate == null || candidate.IsBound) continue;
                float distance = Vector3.Distance(candidate.transform.position, enemy.Position);
                if (distance >= bestDistance) continue;
                best = candidate;
                bestDistance = distance;
            }

            if (best == null) return null;
            best.Bind(enemy.Id, enemy.Position, ResolveEnemyFallbackColor(enemy.SpawnableId.Value));
            _enemyPresentationsById[enemy.Id] = best;
            return best;
        }

        private void EmitAttackEvent(AttackDefinitionAsset attack, AttackPresentationEventKind eventKind, Vector3 eventPosition, long targetEnemyId = 0)
        {
            bool emittedVfx = false;
            bool emittedAudio = false;
            if (eventKind == AttackPresentationEventKind.OnImpact)
                emittedVfx = TryEmitBeamVfx(attack, eventPosition, targetEnemyId);
            if (attack != null &&
                attack.Presentation != null &&
                attack.Presentation.TryGetEvent(eventKind, out AttackPresentationEventRecipe recipe))
            {
                Vector3 position = ResolveAttackEventPosition(attack, recipe, eventPosition);
                bool recipeUsesBeamPrefab = IsBeamPresentationPrefab(attack, recipe.VfxPrefab);
                if (recipeUsesBeamPrefab)
                {
                    if (eventKind == AttackPresentationEventKind.OnImpact && !emittedVfx)
                        emittedVfx = TryEmitBeamVfx(attack, eventPosition, targetEnemyId);
                    else
                        emittedVfx = true;
                }
                else
                {
                    emittedVfx = EmitPresentationVfx(
                        recipe.VfxPrefab,
                        position,
                        "AttackPresentation",
                        attack.Id,
                        ResolveWeaponIdForAttack(attack),
                        attack.Id,
                        eventKind.ToString(),
                        recipe.VfxPrefab,
                        ResolveMuzzleSocketId(attack),
                        ResolveTargetSocketId(recipe)) || emittedVfx;
                }
                emittedAudio = PlayPresentationAudio(recipe.AudioClip);
            }

            if (!emittedVfx)
            {
                EmitFallbackPresentationVfx(eventPosition, ResolveAttackColor(attack), ResolveAttackEventScale(eventKind));
                if (eventKind == AttackPresentationEventKind.OnFire)
                    MuzzleFlashSpawnCount++;
            }
            if (!emittedAudio && eventKind != AttackPresentationEventKind.OnTick)
                PlayPresentationAudio(null);
        }

        private bool TryEmitBeamVfx(AttackDefinitionAsset attack, Vector3 impactPosition, long targetEnemyId)
        {
            if (!TryGetBeamVfxPrefab(attack, out GameObject prefab)) return false;
            Vector3 origin = ResolveTowerMuzzlePosition(attack);
            if (!IsFiniteVector(origin) || !IsFiniteVector(impactPosition))
            {
                BeamVisualInvalidEndpointCount++;
                return false;
            }

            Vector3 delta = impactPosition - origin;
            float distance = delta.magnitude;
            if (distance <= 0.05f)
            {
                BeamVisualInvalidEndpointCount++;
                return false;
            }

            GameObject instance = Instantiate(prefab);
            instance.name = prefab.name + " Runtime Beam";
            if (_root != null) instance.transform.SetParent(_root.transform, true);
            StampAuthoredVisibleInstance(
                instance,
                "BeamVfx",
                attack == null ? string.Empty : attack.Id,
                prefab.name,
                ResolveWeaponIdForAttack(attack),
                attack == null ? string.Empty : attack.Id,
                "Beam",
                prefab,
                ResolveMuzzleSocketId(attack),
                "target.center");
            ConfigureBeamLineRenderer(instance, prefab, attack);
            HideBeamMeshRenderers(instance);
            AlignBeamInstance(instance, prefab, origin, impactPosition);
            instance.SetActive(true);
            PrepareBeamRenderers(instance, ResolveAttackColor(attack));
            DisableColliders(instance);

            ParticleSystem[] particles = instance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].gameObject.SetActive(true);
                particles[i].Play(true);
            }

            AttackVfxSpawnCount++;
            BeamVisualSpawnCount++;
            _activeBeamVisuals.Add(new ActiveBeamVisual(instance, prefab, attack, targetEnemyId, impactPosition, ResolveBeamDurationSeconds(attack)));
            return true;
        }

        private static bool TryGetBeamVfxPrefab(AttackDefinitionAsset attack, out GameObject prefab)
        {
            prefab = null;
            if (attack == null || attack.Delivery == null) return false;
            if (attack.Delivery.Mode != AttackRecipeDeliveryMode.Hitscan) return false;
            prefab = attack.Delivery.BeamVfxPrefab;
            return prefab != null;
        }

        private static bool IsBeamPresentationPrefab(AttackDefinitionAsset attack, GameObject prefab)
        {
            if (prefab == null) return false;
            return TryGetBeamVfxPrefab(attack, out GameObject beamPrefab) && prefab == beamPrefab;
        }

        private static Vector3 ResolveBeamWorldScale(GameObject prefab, float distance)
        {
            Vector3 sourceScale = prefab == null ? Vector3.one : prefab.transform.localScale;
            float width = Mathf.Clamp(Mathf.Max(Mathf.Abs(sourceScale.x), Mathf.Abs(sourceScale.y), 0.08f), 0.08f, 0.38f);
            return new Vector3(width, width, Mathf.Max(0.05f, distance));
        }

        private static float ResolveBeamDurationSeconds(AttackDefinitionAsset attack)
        {
            float authoredTick = attack != null && attack.Delivery != null ? attack.Delivery.TickIntervalSeconds : 0.5f;
            return Mathf.Clamp(authoredTick * 0.44f, 0.14f, 0.3f);
        }

        private static bool AlignBeamInstance(GameObject instance, GameObject prefab, Vector3 origin, Vector3 impactPosition)
        {
            if (instance == null || !IsFiniteVector(origin) || !IsFiniteVector(impactPosition)) return false;
            Vector3 delta = impactPosition - origin;
            float distance = delta.magnitude;
            if (distance <= 0.05f) return false;
            LineRenderer lineRenderer = instance.GetComponentInChildren<LineRenderer>(true);
            if (lineRenderer != null)
            {
                instance.transform.position = origin;
                instance.transform.rotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
                lineRenderer.useWorldSpace = true;
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, origin);
                lineRenderer.SetPosition(1, impactPosition);
                return true;
            }

            instance.transform.position = origin + delta * 0.5f;
            instance.transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
            instance.transform.localScale = ResolveBeamWorldScale(prefab, distance);
            return true;
        }

        private void ConfigureBeamLineRenderer(GameObject instance, GameObject prefab, AttackDefinitionAsset attack)
        {
            if (instance == null) return;
            LineRenderer lineRenderer = instance.GetComponentInChildren<LineRenderer>(true);
            if (lineRenderer == null)
                lineRenderer = instance.AddComponent<LineRenderer>();

            float width = ResolveBeamLineWidth(prefab);
            lineRenderer.enabled = true;
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 2;
            lineRenderer.widthMultiplier = width;
            lineRenderer.widthCurve = new AnimationCurve(
                new Keyframe(0f, 0.35f),
                new Keyframe(0.08f, 1f),
                new Keyframe(0.82f, 0.72f),
                new Keyframe(1f, 0.22f));
            lineRenderer.numCapVertices = 8;
            lineRenderer.numCornerVertices = 2;
            lineRenderer.alignment = LineAlignment.View;
            lineRenderer.textureMode = LineTextureMode.Stretch;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
            lineRenderer.generateLightingData = false;
            lineRenderer.sortingOrder = 18;
            lineRenderer.sharedMaterial = CreateBeamLineMaterial(prefab);
            ApplyBeamLineColors(lineRenderer, ResolveAttackColor(attack));
        }

        private static float ResolveBeamLineWidth(GameObject prefab)
        {
            Vector3 sourceScale = prefab == null ? Vector3.one : prefab.transform.localScale;
            float width = Mathf.Max(Mathf.Abs(sourceScale.x), Mathf.Abs(sourceScale.y), 0.08f);
            return Mathf.Clamp(width * 1.8f, 0.16f, 0.42f);
        }

        private static Material ResolveBeamSourceMaterial(GameObject prefab)
        {
            if (prefab != null)
            {
                Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null && renderers[i].sharedMaterial != null)
                        return renderers[i].sharedMaterial;
                }
            }

            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            return shader != null ? new Material(shader) : null;
        }

        private static Material CreateBeamLineMaterial(GameObject prefab)
        {
            Material source = ResolveBeamSourceMaterial(prefab);
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Standard");
            Material material = shader != null ? new Material(shader) : (source != null ? new Material(source) : null);
            if (material == null) return null;
            material.name = (source != null ? source.name : "PulseBeamEnergy") + " Runtime Line";
            Color sourceColor = Color.white;
            if (source != null)
            {
                if (source.HasProperty("_EmissionColor"))
                    sourceColor = source.GetColor("_EmissionColor");
                else if (source.HasProperty("_Color"))
                    sourceColor = source.color;
            }

            sourceColor.a = 1f;
            if (material.HasProperty("_Color"))
                material.color = sourceColor;
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", sourceColor * 1.35f);
            return material;
        }

        private static void HideBeamMeshRenderers(GameObject instance)
        {
            if (instance == null) return;
            MeshRenderer[] meshRenderers = instance.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < meshRenderers.Length; i++)
                meshRenderers[i].enabled = false;
        }

        private static void ApplyBeamLineColors(LineRenderer lineRenderer, Color color)
        {
            if (lineRenderer == null) return;
            Color start = Color.Lerp(Color.white, color, 0.18f);
            start.a = 1f;
            Color end = Color.Lerp(Color.white, color, 0.52f);
            end.a = 1f;
            lineRenderer.startColor = start;
            lineRenderer.endColor = end;
        }

        private static void PrepareBeamRenderers(GameObject instance, Color color)
        {
            if (instance == null) return;
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                Material source = renderer.sharedMaterial;
                Material material = source != null ? new Material(source) : (shader != null ? new Material(shader) : null);
                if (material != null)
                {
                    if (material.HasProperty("_Color"))
                        material.color = Color.Lerp(Color.white, color, 0.5f);
                    if (material.HasProperty("_EmissionColor"))
                        material.SetColor("_EmissionColor", color * 1.25f);
                    renderer.sharedMaterial = material;
                }

                if (renderer is LineRenderer lineRenderer)
                    ApplyBeamLineColors(lineRenderer, color);
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private void UpdateActiveBeamVisuals(float deltaSeconds)
        {
            if (_activeBeamVisuals.Count == 0) return;
            float safeDelta = Mathf.Max(0.016f, deltaSeconds);
            for (int i = _activeBeamVisuals.Count - 1; i >= 0; i--)
            {
                ActiveBeamVisual visual = _activeBeamVisuals[i];
                visual.ElapsedSeconds += safeDelta;
                if (visual.Instance == null || visual.ElapsedSeconds >= visual.DurationSeconds)
                {
                    DestroyTemplateObject(visual.Instance);
                    _activeBeamVisuals.RemoveAt(i);
                    continue;
                }

                Vector3 impactPosition = visual.LastImpactPosition;
                if (visual.TargetEnemyId > 0 && TryFindActiveEnemy(visual.TargetEnemyId, out AutoDefenseEnemySnapshot enemy))
                    impactPosition = CreateEnemyAimPosition(enemy.Position);
                if (!AlignBeamInstance(visual.Instance, visual.Prefab, ResolveTowerMuzzlePosition(visual.Attack), impactPosition))
                {
                    BeamVisualInvalidEndpointCount++;
                    DestroyTemplateObject(visual.Instance);
                    _activeBeamVisuals.RemoveAt(i);
                    continue;
                }

                visual.LastImpactPosition = impactPosition;
                _activeBeamVisuals[i] = visual;
            }
        }

        private static bool IsFiniteVector(Vector3 value)
        {
            return !(float.IsNaN(value.x) || float.IsNaN(value.y) || float.IsNaN(value.z) ||
                float.IsInfinity(value.x) || float.IsInfinity(value.y) || float.IsInfinity(value.z));
        }

        private bool EmitPresentationVfx(
            GameObject prefab,
            Vector3 position,
            string definitionType,
            string contentId,
            string ownerWeaponId,
            string ownerAttackId,
            string effectRole,
            UnityEngine.Object sourceAsset = null,
            string originSocketId = "",
            string targetSocketId = "")
        {
            if (prefab == null) return false;
            GameObject instance = Instantiate(prefab, position, Quaternion.identity);
            instance.name = prefab.name + " Runtime";
            if (_root != null) instance.transform.SetParent(_root.transform, true);
            StampAuthoredVisibleInstance(instance, definitionType, contentId, prefab.name, ownerWeaponId, ownerAttackId, effectRole, sourceAsset ?? prefab, originSocketId, targetSocketId);
            instance.SetActive(true);
            DisableColliders(instance);
            ParticleSystem[] particles = instance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].gameObject.SetActive(true);
                particles[i].Play(true);
            }

            AttackVfxSpawnCount++;
            if (string.Equals(effectRole, AttackPresentationEventKind.OnFire.ToString(), StringComparison.OrdinalIgnoreCase))
                MuzzleFlashSpawnCount++;
            DestroyPresentationObject(instance, 2f);
            return true;
        }

        private void EmitFallbackPresentationVfx(Vector3 position, Color color, float scale)
        {
            if (_root == null) return;
            FallbackVisibleGameplaySpawnCount++;
            if (EmitKenneySpriteBurst("Kenney Presentation Burst", "Art/impact_flame", position, color, Mathf.Max(0.45f, scale * 1.8f), 0.42f, 0.18f, 50))
            {
                AttackVfxSpawnCount++;
                return;
            }

            GameObject instance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            instance.name = "Template Presentation VFX";
            instance.transform.SetParent(_root.transform, false);
            instance.transform.position = position;
            instance.transform.localScale = Vector3.one * Mathf.Max(0.08f, scale);
            ApplyColor(instance, color);
            DisableColliders(instance);
            AttackVfxSpawnCount++;
            DestroyPresentationObject(instance, 0.45f);
        }

        private void EmitAttackTracer(Vector3 origin, Vector3 destination, Color color)
        {
            if (!_showDebugAimLines) return;
            if (_root == null) return;
            GameObject tracer = new GameObject("Template Attack Tracer");
            tracer.transform.SetParent(_root.transform, false);
            LineRenderer line = tracer.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPosition(0, origin);
            line.SetPosition(1, destination);
            line.startWidth = 0.12f;
            line.endWidth = 0.035f;
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            if (shader != null)
                line.material = new Material(shader) { color = color };
            line.startColor = color;
            line.endColor = new Color(color.r, color.g, color.b, 0.2f);
            AttackVfxSpawnCount++;
            DebugAimTracerSpawnCount++;
            DestroyPresentationObject(tracer, 0.32f);
        }

        private void EmitKenneyEnemyEventBurst(Vector3 position, EnemyPresentationEventKind eventKind)
        {
            if (eventKind == EnemyPresentationEventKind.OnSpawn)
            {
                EmitKenneySpriteBurst("Enemy Spawn Ring", "Art/build_pad_target", position + Vector3.down * 0.25f, new Color(0.35f, 1f, 0.55f, 0.9f), 0.72f, 0.36f, 0.05f, 18);
                return;
            }

            if (eventKind == EnemyPresentationEventKind.OnHit)
            {
                EmitKenneySpriteBurst("Enemy Hit Spark", "Art/impact_flame", position, new Color(1f, 0.92f, 0.22f), 0.52f, 0.34f, 0.18f, 60);
                TriggerCameraShake(0.045f, 0.025f);
                return;
            }

            if (eventKind == EnemyPresentationEventKind.OnDeath)
            {
                EmitKenneySpriteBurst("Enemy Death Pop", "Art/impact_flame", position, new Color(1f, 0.24f, 0.08f), 1.08f, 0.58f, 0.42f, 62);
                TriggerCameraShake(0.12f, 0.065f);
            }
        }

        private void EmitKenneyAttackEventBurst(AttackDefinitionAsset attack, AttackPresentationEventKind eventKind, Vector3 eventPosition)
        {
            Color color = ResolveAttackColor(attack);
            if (eventKind == AttackPresentationEventKind.OnFire)
            {
                EmitKenneySpriteBurst("Muzzle Flash", "Art/impact_flame", ResolveTowerMuzzlePosition(attack), color, 0.45f, 0.26f, 0.1f, 58);
                return;
            }

            if (eventKind == AttackPresentationEventKind.OnImpact)
            {
                EmitKenneySpriteBurst("Attack Impact Pop", "Art/impact_flame", eventPosition, color, 0.72f, 0.42f, 0.2f, 61);
                TriggerCameraShake(0.06f, 0.035f);
            }
        }

        private bool EmitKenneySpriteBurst(string name, string artPath, Vector3 position, Color color, float scale, float duration, float rise, int sortingOrder)
        {
            if (_root == null || string.IsNullOrWhiteSpace(artPath)) return false;
            Sprite sprite = Resources.Load<Sprite>(KenneyResourceRoot + artPath);
            if (sprite == null) return false;

            GameObject instance = new GameObject(name);
            instance.transform.SetParent(_root.transform, false);
            instance.transform.position = position;
            instance.transform.localScale = Vector3.one * Mathf.Max(0.05f, scale);
            SpriteRenderer renderer = instance.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            var billboard = instance.AddComponent<KenneyBillboardVisual>();
            billboard.Configure(true);
            var burst = instance.AddComponent<KenneySpriteBurstVisual>();
            burst.Configure(renderer, Mathf.Max(0.1f, duration), Mathf.Max(0f, rise), instance.transform.localScale);
            DestroyPresentationObject(instance, Mathf.Max(0.12f, duration) + 0.08f);
            return true;
        }

        private bool PlayPresentationAudio(AudioClip clip)
        {
            AudioClip playableClip = clip != null ? clip : GetFallbackPresentationClip();
            if (playableClip == null) return false;
            AttackAudioPlayCount++;
            if (_runtimeAudioSource != null && Application.isPlaying)
                _runtimeAudioSource.PlayOneShot(playableClip);
            return true;
        }

        private void RecordProjectileVisualSpawn(AttackDefinitionAsset attack)
        {
            ProjectileVisualSpawnCount++;
            if (attack != null && attack.Delivery != null && attack.Delivery.ProjectilePrefab != null)
                AuthoredProjectileVisualSpawnCount++;
        }

        private void ObserveProjectileMotion()
        {
            if (_projectileNavigation == null) return;
            MovementSnapshot snapshot = _projectileNavigation.CreateSnapshot();
            var activeIds = new HashSet<long>();
            for (int i = 0; i < snapshot.Agents.Count; i++)
            {
                MovementAgentSnapshot agent = snapshot.Agents[i];
                long id = agent.Id.Value;
                activeIds.Add(id);
                if (_lastProjectileAgentPositions.TryGetValue(id, out Vector3 previous) &&
                    Vector3.Distance(previous, agent.Position) > 0.01f)
                {
                    ProjectileMotionObservedCount++;
                }

                _lastProjectileAgentPositions[id] = agent.Position;
            }

            var staleIds = new List<long>();
            foreach (long id in _lastProjectileAgentPositions.Keys)
                if (!activeIds.Contains(id))
                    staleIds.Add(id);
            for (int i = 0; i < staleIds.Count; i++)
                _lastProjectileAgentPositions.Remove(staleIds[i]);
        }

        private void EmitDamageNumber(Vector3 worldPosition, double amount, Color color, string prefix)
        {
            if (amount <= 0d) return;
            EnsureRuntimeUiDocument();
            if (_damageNumberLayer == null) return;

            string text = (prefix ?? string.Empty) + Math.Ceiling(amount).ToString(CultureInfo.InvariantCulture);
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.name = "damage-number";
            label.style.position = Position.Absolute;
            ApplyRuntimeUiFont(label);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.fontSize = 24;
            label.style.color = color;
            label.style.backgroundColor = new Color(0f, 0f, 0f, 0.28f);
            label.style.borderTopLeftRadius = 12;
            label.style.borderTopRightRadius = 12;
            label.style.borderBottomLeftRadius = 12;
            label.style.borderBottomRightRadius = 12;
            label.style.unityTextOutlineColor = new Color(0f, 0f, 0f, 0.85f);
            label.style.unityTextOutlineWidth = 2f;
            label.style.width = 90;
            label.style.height = 32;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            _damageNumberLayer.Add(label);

            DamageNumberSpawnCount++;
            _damageNumbers.Add(new DamageNumberView(label, worldPosition, 0f));
            PositionDamageNumber(_damageNumbers[_damageNumbers.Count - 1], 0f);
        }

        private void EmitRewardChoiceFeedback(IdleAutoDefenseRewardDraftChoice choice)
        {
            if (choice == null) return;
            Color color = ResolveRewardRarityColor(choice.Rarity);
            string prefix = choice.IsUnlock ? "UNLOCK: " : choice.RarityName.ToUpperInvariant() + ": ";
            EmitFloatingStatusText(CreateTowerMuzzlePosition(Vector3.zero), prefix + choice.DisplayName, color);
            EmitKenneySpriteBurst("Reward Choice Burst", "Art/currency_coin_gold", CreateTowerMuzzlePosition(Vector3.zero), color, 1.22f, 0.72f, 0.68f, 70);
            bool bigReward = (int)choice.Rarity >= (int)IdleAutoDefenseRewardRarity.Epic;
            TriggerCameraShake(bigReward ? 0.22f : 0.14f, bigReward ? 0.12f : 0.07f);
        }

        private void EmitUpgradeFeedback(string text, Color color, float scale)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            EmitFloatingStatusText(CreateTowerMuzzlePosition(Vector3.zero), text, color);
            EmitKenneySpriteBurst("Upgrade Feedback Burst", "Art/impact_flame", CreateTowerMuzzlePosition(Vector3.zero), color, Mathf.Max(0.35f, scale), 0.5f, 0.45f, 68);
            TriggerCameraShake(0.08f, 0.045f);
        }

        private void EmitFloatingStatusText(Vector3 worldPosition, string text, Color color)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            EnsureRuntimeUiDocument();
            if (_damageNumberLayer == null) return;

            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.name = "upgrade-feedback";
            label.style.position = Position.Absolute;
            ApplyRuntimeUiFont(label);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.fontSize = 20;
            label.style.color = color;
            label.style.backgroundColor = new Color(0.01f, 0.015f, 0.02f, 0.78f);
            label.style.borderTopColor = color;
            label.style.borderBottomColor = color;
            label.style.borderLeftColor = color;
            label.style.borderRightColor = color;
            label.style.borderTopWidth = 2;
            label.style.borderBottomWidth = 1;
            label.style.borderLeftWidth = 1;
            label.style.borderRightWidth = 1;
            label.style.borderTopLeftRadius = 14;
            label.style.borderTopRightRadius = 14;
            label.style.borderBottomLeftRadius = 14;
            label.style.borderBottomRightRadius = 14;
            label.style.unityTextOutlineColor = new Color(0f, 0f, 0f, 0.9f);
            label.style.unityTextOutlineWidth = 2f;
            label.style.width = 292;
            label.style.height = 36;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            _damageNumberLayer.Add(label);

            DamageNumberSpawnCount++;
            UpgradeFeedbackSpawnCount++;
            _damageNumbers.Add(new DamageNumberView(label, worldPosition, 0f));
            PositionDamageNumber(_damageNumbers[_damageNumbers.Count - 1], 0f);
        }

        private void UpdateDamageNumbers(float deltaSeconds)
        {
            if (_damageNumbers.Count == 0) return;
            float safeDelta = Mathf.Max(0.016f, deltaSeconds);
            for (int i = _damageNumbers.Count - 1; i >= 0; i--)
            {
                DamageNumberView number = _damageNumbers[i];
                number.ElapsedSeconds += safeDelta;
                if (number.ElapsedSeconds >= 1.15f || number.Label == null)
                {
                    number.Label?.RemoveFromHierarchy();
                    _damageNumbers.RemoveAt(i);
                    continue;
                }

                PositionDamageNumber(number, number.ElapsedSeconds);
                float alpha = Mathf.Clamp01(1f - number.ElapsedSeconds / 1.15f);
                StyleColor color = number.Label.style.color;
                Color resolved = color.value;
                resolved.a = alpha;
                number.Label.style.color = resolved;
                _damageNumbers[i] = number;
            }
        }

        private void PositionDamageNumber(DamageNumberView number, float elapsedSeconds)
        {
            if (number.Label == null) return;
            Vector2 point = WorldToRuntimePanelPoint(number.WorldPosition);
            float width = number.Label.resolvedStyle.width;
            if (float.IsNaN(width) || width <= 1f)
                width = number.Label.name == "upgrade-feedback" ? 292f : 90f;
            number.Label.style.left = point.x - width * 0.5f;
            number.Label.style.top = point.y - 56f - elapsedSeconds * 48f;
        }

        private Vector2 WorldToRuntimePanelPoint(Vector3 worldPosition)
        {
            Vector2 panelSize = ResolveRuntimePanelSize();
            Camera camera = Camera.main;
            if (camera == null)
                camera = FindFirstObjectByType<Camera>();
            if (camera == null)
                return new Vector2(panelSize.x * 0.5f, panelSize.y * 0.45f);

            Vector3 screen = camera.WorldToScreenPoint(worldPosition);
            if (screen.z < 0f)
                return new Vector2(panelSize.x * 0.5f, panelSize.y * 0.5f);

            float screenWidth = Screen.width > 1 ? Screen.width : panelSize.x;
            float screenHeight = Screen.height > 1 ? Screen.height : panelSize.y;
            float x = screenWidth <= 0f ? panelSize.x * 0.5f : screen.x / screenWidth * panelSize.x;
            float y = screenHeight <= 0f ? panelSize.y * 0.5f : (screenHeight - screen.y) / screenHeight * panelSize.y;
            return new Vector2(
                Mathf.Clamp(x, 16f, Mathf.Max(16f, panelSize.x - 16f)),
                Mathf.Clamp(y, 16f, Mathf.Max(16f, panelSize.y - 16f)));
        }

        private void TriggerCameraShake(float durationSeconds, float magnitude)
        {
            if (durationSeconds <= 0f || magnitude <= 0f) return;
            _cameraShakeSecondsRemaining = Mathf.Max(_cameraShakeSecondsRemaining, durationSeconds);
            _cameraShakeDuration = Mathf.Max(_cameraShakeDuration, durationSeconds);
            _cameraShakeMagnitude = Mathf.Max(_cameraShakeMagnitude, magnitude);
        }

        private void UpdateCameraShake(float deltaSeconds)
        {
            if (_cameraShakeSecondsRemaining <= 0f)
            {
                RestoreShakenCamera();
                return;
            }

            Camera camera = ResolveShakeCamera();
            if (camera == null) return;
            float safeDelta = Mathf.Max(0.016f, deltaSeconds);
            _cameraShakeSecondsRemaining = Mathf.Max(0f, _cameraShakeSecondsRemaining - safeDelta);
            float duration = Mathf.Max(0.001f, _cameraShakeDuration);
            float normalized = Mathf.Clamp01(_cameraShakeSecondsRemaining / duration);
            float magnitude = _cameraShakeMagnitude * normalized * normalized;
            float phase = SurvivalSeconds * 58.7f + _cameraShakeSecondsRemaining * 19.3f;
            Vector3 offset = new Vector3(
                Mathf.Sin(phase) * magnitude,
                Mathf.Cos(phase * 0.7f) * magnitude * 0.35f,
                Mathf.Sin(phase * 1.37f) * magnitude * 0.45f);
            camera.transform.localPosition = _shakeCameraBaseLocalPosition + offset;
            if (_cameraShakeSecondsRemaining <= 0f)
            {
                _cameraShakeMagnitude = 0f;
                _cameraShakeDuration = 0f;
                RestoreShakenCamera();
            }
        }

        private Camera ResolveShakeCamera()
        {
            if (_shakeCamera == null)
                _shakeCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (_shakeCamera == null) return null;
            if (!_shakeCameraBaseCaptured)
            {
                _shakeCameraBaseLocalPosition = _shakeCamera.transform.localPosition;
                _shakeCameraBaseCaptured = true;
            }

            return _shakeCamera;
        }

        private void RestoreShakenCamera()
        {
            if (_shakeCamera == null || !_shakeCameraBaseCaptured) return;
            _shakeCamera.transform.localPosition = _shakeCameraBaseLocalPosition;
        }

        private Vector2 ResolveRuntimePanelSize()
        {
            EnsureRuntimeUiDocument();
            float width = _runtimeUiRoot != null ? _runtimeUiRoot.resolvedStyle.width : 0f;
            float height = _runtimeUiRoot != null ? _runtimeUiRoot.resolvedStyle.height : 0f;
            if (float.IsNaN(width) || width <= 1f)
                width = Screen.width > 1 ? Screen.width : RuntimeUiFallbackWidth;
            if (float.IsNaN(height) || height <= 1f)
                height = Screen.height > 1 ? Screen.height : RuntimeUiFallbackHeight;
            return new Vector2(width, height);
        }

        private AudioClip GetFallbackPresentationClip()
        {
            if (_fallbackPresentationClip != null) return _fallbackPresentationClip;
            AudioClip kenneyClip = Resources.Load<AudioClip>(KenneyResourceRoot + "Audio/laserSmall_000");
            if (kenneyClip != null)
            {
                _fallbackPresentationClip = kenneyClip;
                _fallbackPresentationClipIsRuntimeOwned = false;
                return _fallbackPresentationClip;
            }

            const int sampleRate = 22050;
            const int sampleCount = 2205;
            float[] samples = new float[sampleCount];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = 1f - (float)i / samples.Length;
                samples[i] = Mathf.Sin(2f * Mathf.PI * 660f * t) * 0.18f * envelope;
            }

            _fallbackPresentationClip = AudioClip.Create("Template Presentation Click", sampleCount, 1, sampleRate, false);
            _fallbackPresentationClip.hideFlags = HideFlags.HideAndDontSave;
            _fallbackPresentationClip.SetData(samples, 0);
            _fallbackPresentationClipIsRuntimeOwned = true;
            return _fallbackPresentationClip;
        }

        private bool TrySelectAttackTarget(AttackDefinitionAsset attack, AutoDefenseRuntimeSnapshot snapshot, out AutoDefenseEnemySnapshot selected)
        {
            selected = default;
            if (snapshot == null || snapshot.Enemies.Count == 0) return false;
            AttackRecipeTargetingMode mode = attack != null && attack.Targeting != null
                ? attack.Targeting.Mode
                : AttackRecipeTargetingMode.Nearest;
            bool hasSelected = false;
            for (int i = 0; i < snapshot.Enemies.Count; i++)
            {
                AutoDefenseEnemySnapshot enemy = snapshot.Enemies[i];
                if (enemy.Lifecycle != AutoDefenseEnemyLifecycle.Active) continue;
                if (!hasSelected || IsBetterTarget(enemy, selected, mode))
                {
                    selected = enemy;
                    hasSelected = true;
                }
            }

            return hasSelected;
        }

        private static bool IsBetterTarget(AutoDefenseEnemySnapshot candidate, AutoDefenseEnemySnapshot current, AttackRecipeTargetingMode mode)
        {
            if (mode == AttackRecipeTargetingMode.LowestHealth)
                return candidate.Health < current.Health || (Math.Abs(candidate.Health - current.Health) < 0.001d && candidate.ObjectiveProgress > current.ObjectiveProgress);
            if (mode == AttackRecipeTargetingMode.Strongest)
                return candidate.Health > current.Health || (Math.Abs(candidate.Health - current.Health) < 0.001d && candidate.ObjectiveProgress > current.ObjectiveProgress);
            if (mode == AttackRecipeTargetingMode.Random)
                return candidate.Id < current.Id;
            return candidate.ObjectiveProgress > current.ObjectiveProgress;
        }

        private bool TryFindActiveEnemy(long enemyId, out AutoDefenseEnemySnapshot enemy)
        {
            enemy = default;
            if (_runtime == null || enemyId <= 0) return false;
            return TryFindEnemy(_runtime.CreateSnapshot(), enemyId, out enemy) &&
                enemy.Lifecycle == AutoDefenseEnemyLifecycle.Active;
        }

        private static bool TryFindEnemy(AutoDefenseRuntimeSnapshot snapshot, long enemyId, out AutoDefenseEnemySnapshot enemy)
        {
            enemy = default;
            if (snapshot == null || enemyId <= 0) return false;
            for (int i = 0; i < snapshot.Enemies.Count; i++)
            {
                if (snapshot.Enemies[i].Id != enemyId) continue;
                enemy = snapshot.Enemies[i];
                return true;
            }

            return false;
        }

        private static bool TryFindEnemyByCombatant(AutoDefenseRuntimeSnapshot snapshot, CombatantId combatantId, out AutoDefenseEnemySnapshot enemy)
        {
            enemy = default;
            if (snapshot == null || combatantId.IsEmpty) return false;
            for (int i = 0; i < snapshot.Enemies.Count; i++)
            {
                if (!snapshot.Enemies[i].CombatantId.Equals(combatantId)) continue;
                enemy = snapshot.Enemies[i];
                return true;
            }

            return false;
        }

        internal AttackDefinitionAsset FindAttackRecipeForPresentation(string attackId)
        {
            if (string.IsNullOrWhiteSpace(attackId)) return null;
            for (int i = 0; i < _resolvedAttackRecipes.Length; i++)
            {
                AttackDefinitionAsset attack = _resolvedAttackRecipes[i];
                if (attack != null && string.Equals(attack.Id, attackId, StringComparison.OrdinalIgnoreCase))
                    return attack;
            }

            return null;
        }

        private AttackDefinitionAsset FindAttackRecipeForProjectile(ProjectileDefinition definition)
        {
            if (definition == null) return null;
            for (int i = 0; i < _resolvedAttackRecipes.Length; i++)
            {
                AttackDefinitionAsset attack = _resolvedAttackRecipes[i];
                if (attack == null || attack.Delivery == null) continue;
                if (string.Equals(attack.Delivery.ProjectileDefinitionId, definition.Id.Value, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(attack.Delivery.ProjectileSpawnableId, definition.SpawnableId.Value, StringComparison.OrdinalIgnoreCase))
                {
                    return attack;
                }
            }

            return null;
        }

        private EnemyDefinitionAsset FindEnemyDefinitionForPresentation(WorldSpawnableId spawnableId)
        {
            if (spawnableId.IsEmpty) return null;
            for (int i = 0; i < _resolvedEnemyDefinitions.Length; i++)
            {
                EnemyDefinitionAsset enemy = _resolvedEnemyDefinitions[i];
                if (enemy != null && string.Equals(enemy.Id, spawnableId.Value, StringComparison.OrdinalIgnoreCase))
                    return enemy;
            }

            return null;
        }

        private ProjectileDefinition FindProjectileDefinition(ProjectileDefinitionId id)
        {
            for (int i = 0; i < _resolvedProjectileDefinitions.Length; i++)
            {
                ProjectileDefinition definition = _resolvedProjectileDefinitions[i];
                if (definition != null && definition.Id.Equals(id))
                    return definition;
            }

            return null;
        }

        private int FindPendingProjectileImpactIndex(ProjectileInstanceId projectileId)
        {
            for (int i = 0; i < _pendingProjectileImpacts.Count; i++)
                if (_pendingProjectileImpacts[i].ProjectileId.Equals(projectileId))
                    return i;
            return -1;
        }

        private GameObject GetProjectilePrefab(ProjectileDefinition definition)
        {
            AttackDefinitionAsset attack = FindAttackRecipeForProjectile(definition);
            string key = definition == null || definition.Id.IsEmpty ? "projectile.default" : definition.Id.Value;
            if (_runtimeProjectilePrefabs.TryGetValue(key, out GameObject cached) && cached != null)
                return cached;

            GameObject authoredPrefab = attack != null && attack.Delivery != null ? attack.Delivery.ProjectilePrefab : null;
            Color color = ResolveAttackColor(attack);
            GameObject prefab = authoredPrefab != null
                ? CreateRuntimeVisualPrefab(
                    "Kenney Projectile Runtime Prefab " + key,
                    authoredPrefab,
                    PrimitiveType.Sphere,
                    color,
                    "Art/projectile_rocket",
                    new Vector3(0f, 0f, -0.02f),
                    new Vector3(0.72f, 0.72f, 1f),
                    true,
                    35,
                    "ProjectileVisual",
                    attack != null && attack.Delivery != null ? attack.Delivery.ProjectileDefinitionId : key,
                    ResolveWeaponIdForAttack(attack),
                    attack == null ? string.Empty : attack.Id,
                    "ProjectilePrefab")
                : CreateProjectileModelPrefab("Kenney Projectile Runtime Prefab " + key, ResolveProjectileModelName(attack), color);
            _runtimeProjectilePrefabs[key] = prefab;
            return prefab;
        }

        private int CalculateProjectileImpactDelayTicks(Vector3 origin, Vector3 destination, float speed)
        {
            float safeSpeed = Mathf.Max(0.5f, speed);
            float secondsPerTick = _activeRunProfile == null ? 0.05f : _activeRunProfile.SecondsPerSimulationTick;
            int ticks = Mathf.CeilToInt(Vector3.Distance(origin, destination) / safeSpeed / secondsPerTick);
            int minimum = _activeGameRules == null ? MinimumProjectileImpactDelayTicks : _activeGameRules.MinimumProjectileImpactDelayTicks;
            int maximum = _activeGameRules == null ? MaximumProjectileImpactDelayTicks : _activeGameRules.MaximumProjectileImpactDelayTicks;
            return Mathf.Clamp(ticks, minimum, maximum);
        }

        private static float ResolveProjectileSpeed(AttackDefinitionAsset attack)
        {
            return attack != null && attack.Delivery != null
                ? Mathf.Max(0.5f, attack.Delivery.ProjectileSpeed)
                : 8f;
        }

        private double ResolveAttackDamage(AttackDefinitionAsset attack)
        {
            double threshold = _activeGameRules == null ? SampleProjectileFinishThreshold : _activeGameRules.ProjectileFinishDamageThreshold;
            return attack != null && attack.Mechanics != null
                ? Math.Max(threshold, attack.Mechanics.DamageAmount)
                : threshold;
        }

        internal Vector3 ResolveTowerMuzzlePosition(AttackDefinitionAsset attack)
        {
            IdleAutoDefenseWeaponVisualBinding binding = FindWeaponVisualBinding(attack);
            if (binding != null && binding.Muzzle != null)
                return binding.Muzzle.position;
            return CreateTowerMuzzlePosition(Vector3.zero);
        }

        private IdleAutoDefenseWeaponVisualBinding FindWeaponVisualBinding(AttackDefinitionAsset attack)
        {
            if (attack == null || string.IsNullOrWhiteSpace(attack.Id)) return null;
            return _weaponVisualBindings.TryGetValue(attack.Id, out IdleAutoDefenseWeaponVisualBinding binding) ? binding : null;
        }

        private string ResolveWeaponIdForAttack(AttackDefinitionAsset attack)
        {
            if (attack == null || string.IsNullOrWhiteSpace(attack.Id) || _resolvedWeaponDefinitions == null)
                return string.Empty;

            for (int i = 0; i < _resolvedWeaponDefinitions.Length; i++)
            {
                WeaponDefinitionAsset weapon = _resolvedWeaponDefinitions[i];
                AttackDefinitionAsset weaponAttack = weapon != null && weapon.Stats != null ? weapon.Stats.Attack : null;
                if (weaponAttack != null && string.Equals(weaponAttack.Id, attack.Id, StringComparison.OrdinalIgnoreCase))
                    return weapon.Id;
            }

            return string.Empty;
        }

        private void StampAuthoredVisibleInstance(
            GameObject instance,
            string definitionType,
            string contentId,
            string prefabName,
            string ownerWeaponId,
            string ownerAttackId,
            string effectRole,
            UnityEngine.Object sourceAsset = null,
            string originSocketId = "",
            string targetSocketId = "",
            bool fallbackUsed = false,
            bool allowed = true)
        {
            if (AuthoredContentInstance.Stamp(
                    instance,
                    definitionType,
                    contentId,
                    ResolveAssetGuid(sourceAsset),
                    ResolveAssetPath(sourceAsset),
                    prefabName,
                    ownerWeaponId,
                    ownerAttackId,
                    effectRole,
                    definitionType,
                    ownerWeaponId,
                    ownerAttackId,
                    effectRole,
                    originSocketId,
                    targetSocketId,
                    "IdleAutoDefenseTemplateController",
                    fallbackUsed,
                    allowed) != null)
            {
                AuthoredVisibleInstanceStampCount++;
            }
        }

        private static string ResolveAssetPath(UnityEngine.Object asset)
        {
#if UNITY_EDITOR
            return asset == null ? string.Empty : UnityEditor.AssetDatabase.GetAssetPath(asset);
#else
            return string.Empty;
#endif
        }

        private static string ResolveAssetGuid(UnityEngine.Object asset)
        {
#if UNITY_EDITOR
            string path = ResolveAssetPath(asset);
            return string.IsNullOrWhiteSpace(path) ? string.Empty : UnityEditor.AssetDatabase.AssetPathToGUID(path);
#else
            return string.Empty;
#endif
        }

        private static string ResolveMuzzleSocketId(AttackDefinitionAsset attack)
        {
            return attack == null || string.IsNullOrWhiteSpace(attack.Id) ? string.Empty : attack.Id + ":muzzle.primary";
        }

        private static string ResolveTargetSocketId(AttackPresentationEventRecipe recipe)
        {
            if (recipe == null) return string.Empty;
            if (recipe.SpawnPointRole == AttackPresentationSpawnPointRole.Target) return "target.center";
            if (recipe.SpawnPointRole == AttackPresentationSpawnPointRole.ImpactPoint) return "target.impact";
            return string.Empty;
        }

        private void PlayWeaponFirePresentation(AttackDefinitionAsset attack, Vector3 targetPosition)
        {
            IdleAutoDefenseWeaponVisualBinding binding = FindWeaponVisualBinding(attack);
            if (binding == null) return;
            if (binding.AimAt(targetPosition, true, Time.deltaTime <= 0f ? 1f / 60f : Time.deltaTime))
                TurretAimUpdateCount++;
            if (binding.TriggerRecoil())
                RecoilEventCount++;
        }

        private void UpdateWeaponPresentationTargets(AutoDefenseRuntimeSnapshot snapshot, float deltaSeconds)
        {
            if (snapshot == null || _weaponVisualBindings.Count == 0) return;
            foreach (KeyValuePair<string, IdleAutoDefenseWeaponVisualBinding> pair in _weaponVisualBindings)
            {
                AttackDefinitionAsset attack = FindAttackRecipeForPresentation(pair.Key);
                if (attack == null) continue;
                double range = ResolvePresentationRange(attack);
                if (TrySelectPresentationEnemyWithinRange(snapshot, range, out AutoDefenseEnemySnapshot enemy))
                {
                    if (pair.Value.AimAt(CreateEnemyAimPosition(enemy.Position), false, deltaSeconds))
                        TurretAimUpdateCount++;
                }
                else if (pair.Value.Rest(deltaSeconds))
                {
                    TurretAimUpdateCount++;
                }
            }
        }

        private double ResolvePresentationRange(AttackDefinitionAsset attack)
        {
            if (attack == null) return ManualTowerBaseRange;
            IdleAutoDefenseModuleRule module = _activeGameRules == null ? null : _activeGameRules.GetModuleByAttackId(attack.Id);
            if (module != null)
            {
                return module.Role == IdleAutoDefenseModuleRole.StartingProjectile
                    ? ResolveManualTowerRange()
                    : ResolveModuleRange(module.BaseRange);
            }
            if (_activeGameRules == null)
            {
                if (string.Equals(attack.Id, BasicIdleAutoDefenseGame.ShardAttackId.Value, StringComparison.OrdinalIgnoreCase)) return ResolveManualTowerRange();
                if (string.Equals(attack.Id, BasicIdleAutoDefenseGame.PulseAttackId.Value, StringComparison.OrdinalIgnoreCase)) return ResolveModuleRange(PulseBeamModuleBaseRange);
                if (string.Equals(attack.Id, BasicIdleAutoDefenseGame.ArcBurstAttackId.Value, StringComparison.OrdinalIgnoreCase)) return ResolveModuleRange(ArcBurstModuleBaseRange);
                if (string.Equals(attack.Id, BasicIdleAutoDefenseGame.HomingPulseAttackId.Value, StringComparison.OrdinalIgnoreCase)) return ResolveModuleRange(HomingPulseModuleBaseRange);
            }
            return attack.Mechanics == null ? ManualTowerBaseRange : attack.Mechanics.Range;
        }

        private static bool TrySelectPresentationEnemyWithinRange(AutoDefenseRuntimeSnapshot snapshot, double range, out AutoDefenseEnemySnapshot selected)
        {
            selected = default;
            if (snapshot == null || snapshot.Enemies.Count == 0) return false;
            bool hasSelected = false;
            float maxRange = (float)Math.Max(0.1d, range);
            for (int i = 0; i < snapshot.Enemies.Count; i++)
            {
                AutoDefenseEnemySnapshot enemy = snapshot.Enemies[i];
                if (enemy.Lifecycle != AutoDefenseEnemyLifecycle.Active) continue;
                if (Vector3.Distance(enemy.Position, Vector3.zero) > maxRange) continue;
                if (hasSelected && enemy.ObjectiveProgress <= selected.ObjectiveProgress) continue;
                selected = enemy;
                hasSelected = true;
            }

            return hasSelected;
        }

        internal bool TrySelectPresentationEnemyWithinAnyRange(out AutoDefenseEnemySnapshot selected)
        {
            selected = default;
            if (_runtime == null) return false;
            return TrySelectPriorityEnemy(_runtime.CreateSnapshot(), out selected);
        }

        private static Vector3 CreateTowerMuzzlePosition(Vector3 objectivePosition)
        {
            return objectivePosition + Vector3.up * 0.75f;
        }

        internal static Vector3 CreateEnemyAimPosition(Vector3 enemyPosition)
        {
            return enemyPosition + Vector3.up * 0.35f;
        }

        private Vector3 ResolveAttackEventPosition(AttackDefinitionAsset attack, AttackPresentationEventRecipe recipe, Vector3 requestedPosition)
        {
            if (recipe == null) return requestedPosition;
            if (recipe.SpawnPointRole == AttackPresentationSpawnPointRole.Caster ||
                recipe.SpawnPointRole == AttackPresentationSpawnPointRole.Muzzle)
            {
                return ResolveTowerMuzzlePosition(attack);
            }

            return requestedPosition;
        }

        private Color ResolveAttackColor(AttackDefinitionAsset attack)
        {
            if (attack == null) return Color.white;
            IdleAutoDefenseModuleRule module = _activeGameRules == null ? null : _activeGameRules.GetModuleByAttackId(attack.Id);
            if (module != null)
            {
                if (module.Role == IdleAutoDefenseModuleRole.StartingProjectile) return new Color(1f, 0.45f, 0.1f);
                if (module.Role == IdleAutoDefenseModuleRole.PrecisionBeam) return new Color(0.15f, 0.8f, 1f);
                if (module.Role == IdleAutoDefenseModuleRole.AreaBurst) return new Color(1f, 0.65f, 0.12f);
                if (module.Role == IdleAutoDefenseModuleRole.HomingProjectile) return new Color(0.68f, 0.38f, 1f);
            }
            if (_activeGameRules == null)
            {
                if (string.Equals(attack.Id, BasicIdleAutoDefenseGame.ShardAttackId.Value, StringComparison.OrdinalIgnoreCase)) return new Color(1f, 0.45f, 0.1f);
                if (string.Equals(attack.Id, BasicIdleAutoDefenseGame.PulseAttackId.Value, StringComparison.OrdinalIgnoreCase)) return new Color(0.15f, 0.8f, 1f);
                if (string.Equals(attack.Id, BasicIdleAutoDefenseGame.ArcBurstAttackId.Value, StringComparison.OrdinalIgnoreCase)) return new Color(1f, 0.65f, 0.12f);
                if (string.Equals(attack.Id, BasicIdleAutoDefenseGame.HomingPulseAttackId.Value, StringComparison.OrdinalIgnoreCase)) return new Color(0.68f, 0.38f, 1f);
            }
            return Color.white;
        }

        private static Color ResolveRewardRarityColor(IdleAutoDefenseRewardRarity rarity)
        {
            if (rarity == IdleAutoDefenseRewardRarity.Legendary) return new Color(1f, 0.78f, 0.16f);
            if (rarity == IdleAutoDefenseRewardRarity.Epic) return new Color(0.78f, 0.42f, 1f);
            if (rarity == IdleAutoDefenseRewardRarity.Rare) return new Color(0.25f, 0.65f, 1f);
            if (rarity == IdleAutoDefenseRewardRarity.Uncommon) return new Color(0.35f, 1f, 0.55f);
            return new Color(0.92f, 0.96f, 1f);
        }

        private static Color ResolveEnemyEventColor(EnemyPresentationEventKind eventKind)
        {
            if (eventKind == EnemyPresentationEventKind.OnDeath) return new Color(1f, 0.25f, 0.18f);
            if (eventKind == EnemyPresentationEventKind.OnHit) return new Color(1f, 0.95f, 0.25f);
            return new Color(0.35f, 1f, 0.55f);
        }

        private static float ResolveAttackEventScale(AttackPresentationEventKind eventKind)
        {
            if (eventKind == AttackPresentationEventKind.OnImpact) return 0.42f;
            if (eventKind == AttackPresentationEventKind.OnExpire) return 0.28f;
            return 0.3f;
        }

        private static void DisableColliders(GameObject instance)
        {
            if (instance == null) return;
            Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;
        }

        private static void HideMeshRenderers(GameObject instance)
        {
            if (instance == null) return;
            MeshRenderer[] renderers = instance.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].enabled = false;
        }

        private static void DestroyPresentationObject(GameObject instance, float delaySeconds)
        {
            if (instance == null) return;
            if (Application.isPlaying) Destroy(instance, delaySeconds);
            else DestroyImmediate(instance);
        }

        private void AwardRuntimeCurrencyForKills(int kills)
        {
            if (kills <= 0) return;
            long authoredBase = _pendingAuthoredKillCredits > 0L ? _pendingAuthoredKillCredits : kills * KillRewardCredits;
            _pendingAuthoredKillCredits = 0L;
            long earned = Math.Max(kills, (long)Math.Ceiling(authoredBase * (1d + RewardCreditMultiplierBonus)));
            RuntimeCurrency += earned;
            RuntimeCurrencyEarned += earned;
            EmitFloatingStatusText(CreateTowerMuzzlePosition(Vector3.zero), "+" + earned.ToString(CultureInfo.InvariantCulture) + " credits", new Color(1f, 0.86f, 0.2f));
        }

        private void GrantPassiveIncomeIfReady(int ticks)
        {
            _passiveIncomeTicks += Math.Max(1, ticks);
            int intervalTicks = _activeEconomy == null ? PassiveIncomeIntervalTicks : Math.Max(1, _activeEconomy.PassiveIncomeIntervalTicks);
            if (_passiveIncomeTicks < intervalTicks) return;
            int intervals = _passiveIncomeTicks / intervalTicks;
            _passiveIncomeTicks %= intervalTicks;
            long amount = _activeEconomy == null ? 1L : Math.Max(0L, _activeEconomy.PassiveIncomeAmount);
            long earned = intervals * amount;
            RuntimeCurrency += earned;
            RuntimeCurrencyEarned += earned;
        }

        private bool SpendRuntimeCurrency(int cost)
        {
            if (!CanSpendRuntimeCurrency(cost)) return false;
            RuntimeCurrency -= cost;
            RuntimeCurrencySpent += cost;
            return true;
        }

        private bool CanSpendRuntimeCurrency(int cost)
        {
            return EncounterRunning && cost > 0 && RuntimeCurrency >= cost;
        }

        private static int CalculateUpgradeCost(int baseCost, int rank)
        {
            return baseCost + Math.Max(0, rank) * (baseCost / 2 + 6);
        }

        private int ResolveUpgradeCost(string costId, int rank, int fallbackBaseCost = 0)
        {
            IdleAutoDefenseCostCurve curve = _activeEconomy == null ? null : _activeEconomy.GetUpgradeCostCurve(costId);
            return curve == null ? CalculateUpgradeCost(fallbackBaseCost, rank) : curve.Calculate(rank);
        }

        private int ResolveModuleBuildCost(IdleAutoDefenseModuleRole role, int fallbackCost)
        {
            IdleAutoDefenseModuleRule module = _activeGameRules == null ? null : _activeGameRules.GetModule(role);
            return module == null ? fallbackCost : module.BuildCost;
        }

        private long ResolveRuntimeStartingCredits(GameContentSetResolution resolution)
        {
            if (resolution != null && resolution.IsValid && resolution.ContentSet != null && resolution.ContentSet.Economy != null)
                return Math.Max(0L, resolution.ContentSet.Economy.StartingCredits);
            if (_activeEconomy != null)
                return Math.Max(0L, _activeEconomy.StartingCredits);
            return DefaultRuntimeStartingCredits;
        }

        private string ResolveCurrentSpawnProfileName()
        {
            if (_encounter == null) return "None";
            EncounterSnapshot snapshot = _encounter.CreateSnapshot();
            string lastStarted = string.Empty;
            for (int i = 0; i < snapshot.Waves.Count; i++)
            {
                WaveProgressSnapshot wave = snapshot.Waves[i];
                if (wave.Started && !wave.Emitted)
                    return ResolveWaveDisplayName(wave.WaveId.Value);
                if (wave.Started)
                    lastStarted = wave.WaveId.Value;
            }

            return string.IsNullOrWhiteSpace(lastStarted) ? "None" : ResolveWaveDisplayName(lastStarted);
        }

        private int ResolveCurrentWaveNumber()
        {
            if (_encounter == null) return 0;
            EncounterSnapshot snapshot = _encounter.CreateSnapshot();
            int lastStarted = 0;
            for (int i = 0; i < snapshot.Waves.Count; i++)
            {
                WaveProgressSnapshot wave = snapshot.Waves[i];
                if (wave.Started) lastStarted = i + 1;
                if (wave.Started && !wave.Emitted) return i + 1;
            }

            return lastStarted;
        }

        public int GetRewardDraftChoiceCurrentRank(IdleAutoDefenseRewardDraftChoice choice)
        {
            if (choice == null || choice.IsUnlock) return 0;
            if (!string.IsNullOrWhiteSpace(choice.TargetWeaponId))
            {
                if (choice.Rarity == IdleAutoDefenseRewardRarity.Epic)
                    return GetRank(_weaponEpicUpgradeRanks, choice.TargetWeaponId);
                if (choice.Rarity == IdleAutoDefenseRewardRarity.Legendary)
                    return _weaponLegendaryUnlocks.Contains(choice.TargetWeaponId) ? 1 : 0;
                return GetRank(_weaponNormalUpgradeRanks, choice.TargetWeaponId);
            }

            switch (choice.EffectKind)
            {
                case IdleAutoDefenseRewardEffectKind.DamageRank: return DamageUpgradeRank;
                case IdleAutoDefenseRewardEffectKind.FireRateRank: return AttackSpeedUpgradeRank;
                case IdleAutoDefenseRewardEffectKind.RangeRank: return RangeUpgradeRank;
                case IdleAutoDefenseRewardEffectKind.Repair: return RepairUpgradeRank;
                default: return GetRank(_baseRewardRanks, choice.DedupeKey);
            }
        }

        public bool TryGetPrimaryMajorThreat(out IdleAutoDefenseMajorThreatSnapshot threat)
        {
            threat = default;
            if (_runtime == null) return false;
            AutoDefenseRuntimeSnapshot snapshot = _runtime.CreateSnapshot();
            AutoDefenseEnemySnapshot selected = default;
            bool found = false;
            bool selectedBoss = false;
            for (int i = 0; i < snapshot.Enemies.Count; i++)
            {
                AutoDefenseEnemySnapshot candidate = snapshot.Enemies[i];
                if (candidate.Lifecycle != AutoDefenseEnemyLifecycle.Active) continue;
                bool boss = IsBossEnemy(candidate);
                if (!boss && !IsEliteEnemy(candidate)) continue;
                if (!found || boss && !selectedBoss || boss == selectedBoss && candidate.ObjectiveProgress > selected.ObjectiveProgress)
                {
                    selected = candidate;
                    selectedBoss = boss;
                    found = true;
                }
            }

            if (!found) return false;
            EnemyDefinitionAsset definition = FindEnemyDefinitionForPresentation(selected.SpawnableId);
            string displayName = definition == null || string.IsNullOrWhiteSpace(definition.DisplayName)
                ? (selectedBoss ? "Boss" : "Elite")
                : definition.DisplayName;
            double maximumHealth = definition == null || definition.Stats == null
                ? Math.Max(1d, selected.Health)
                : Math.Max(1d, definition.Stats.MaximumHealth);
            threat = new IdleAutoDefenseMajorThreatSnapshot(
                selected.Id,
                displayName,
                selectedBoss,
                selected.Health,
                maximumHealth,
                selected.Position);
            return true;
        }

        private string ResolveWaveDisplayName(string waveId)
        {
            if (string.IsNullOrWhiteSpace(waveId)) return "None";
            for (int i = 0; i < _resolvedWaveDefinitions.Length; i++)
            {
                WaveDefinitionAsset wave = _resolvedWaveDefinitions[i];
                if (wave == null || !string.Equals(wave.Id, waveId, StringComparison.OrdinalIgnoreCase)) continue;
                return string.IsNullOrWhiteSpace(wave.DisplayName) ? wave.Id : wave.DisplayName;
            }

            return waveId;
        }

        private void ApplyEncounterRewardIfTerminal()
        {
            if (_completionRewardApplied || _progressionState == null || _runtime.State == AutoDefenseRuntimeState.Running) return;
            RewardBundle authoredReward = _activeProgression == null || _activeEconomy == null
                ? BasicIdleAutoDefenseGame.CreateEncounterCompletionReward()
                : _activeProgression.CreateEncounterCompletionReward(_activeEconomy);
            ProgressionResult result = _progressionState.ApplyReward(
                _progressionCatalog,
                new ProgressionOperationId("idle-auto-defense.encounter.terminal." + _runSequence.ToString(CultureInfo.InvariantCulture)),
                authoredReward);
            if (!result.Succeeded) return;
            long baseCredits = _activeEconomy == null ? 60L : Math.Max(0L, _activeEconomy.EncounterCompletionCredits);
            long bonusCredits = (long)Math.Ceiling(baseCredits * RewardCreditMultiplierBonus);
            if (bonusCredits > 0)
            {
                _progressionState.ApplyReward(
                    _progressionCatalog,
                    new ProgressionOperationId("idle-auto-defense.encounter.terminal.1.reward-bonus"),
                    new RewardBundle(new[] { new CurrencyLine(RuntimeCredits, new ProgressionAmount(bonusCredits), true) }));
            }

            _completionRewardApplied = true;
            EncounterRewardCredits = _progressionState.GetBalance(RuntimeCredits).Value;
            EncounterRewardParts = _progressionState.GetBalance(RuntimeParts).Value;
        }

        private void BlockStrictStartup()
        {
            StartupBlocked = true;
            FallbackModeActive = false;
            string source = _contentPack != null
                ? "content pack '" + _contentPack.name + "'"
                : _contentSet != null
                    ? "content set '" + _contentSet.name + "'"
                    : "the generated scene assignment";
            string details = !string.IsNullOrWhiteSpace(AssignedContentPackStatus)
                ? AssignedContentPackStatus + " " + AssignedContentSetStatus
                : AssignedContentSetStatus;
            StartupError = "Strict authored startup blocked: " + source + " is missing or invalid. " + details;
            Debug.LogError("[Idle Auto Defense Template] " + StartupError, this);
        }

        private void BindExplicitFallbackCore()
        {
            FallbackModeActive = true;
            StartupBlocked = false;
            StartupError = string.Empty;
            _activeRewardCatalog = IdleAutoDefenseRewardCatalogAsset.CreateTransient(RewardDraftSettings, RewardDraftCatalog);
            _activeEconomy = IdleAutoDefenseEconomyAsset.CreateTransient(DefaultRuntimeStartingCredits > int.MaxValue ? int.MaxValue : (int)DefaultRuntimeStartingCredits, 0);
            _activeRunProfile = IdleAutoDefenseRunProfileAsset.CreateTransient(_resolvedWaveDefinitions);
            _activeProgression = IdleAutoDefenseProgressionAsset.CreateTransient();
            _activeOfflineProgression = IdleAutoDefenseOfflineProgressionAsset.CreateTransient();
            _activeGameRules = IdleAutoDefenseGameRulesAsset.CreateTransient(_resolvedWeaponDefinitions, _resolvedEnemyDefinitions);
            AssignedContentPackStatus = string.IsNullOrWhiteSpace(AssignedContentPackStatus)
                ? "Explicit unbound fallback host."
                : AssignedContentPackStatus;
            AssignedContentSetStatus = string.IsNullOrWhiteSpace(AssignedContentSetStatus)
                ? "Using documented transient fallback content for an unbound debug/test host."
                : AssignedContentSetStatus;
        }

        private bool TryUseAssignedContentSet()
        {
            UsingAssignedContentPack = false;
            UsingAssignedContentSet = false;
            StartupBlocked = false;
            StartupError = string.Empty;
            FallbackModeActive = false;
            InvalidAssignedContentPackIssueCount = 0;
            InvalidAssignedContentSetIssueCount = 0;
            _resolvedContentSet = null;
            _activeRewardCatalog = null;
            _activeEconomy = null;
            _activeRunProfile = null;
            _activeProgression = null;
            _activeOfflineProgression = null;
            _activeGameRules = null;

            if (_contentPack != null)
            {
                GameContentPackResolution packResolution = GameContentPackValidator.Resolve(_contentPack, _contentSet);
                InvalidAssignedContentPackIssueCount = packResolution.PackReport.ErrorCount;
                if (packResolution.ContentSetResolution != null)
                    InvalidAssignedContentSetIssueCount = packResolution.ContentSetResolution.Report.ErrorCount;

                if (!packResolution.IsValid)
                {
                    AssignedContentPackStatus = _requireAuthoredContent
                        ? "Assigned content pack is invalid; strict startup will block gameplay."
                        : "Assigned content pack is invalid; explicit fallback mode may be used by this debug/test host.";
                    AssignedContentSetStatus = "Content pack could not resolve a playable content set.";
                    if (!_requireAuthoredContent)
                        Debug.LogWarning(
                            "[Idle Auto Defense Template] Assigned GameContentPackAsset '" + _contentPack.name + "' is incomplete or invalid. Entering explicit fallback mode. " + CreateContentPackIssueSummary(packResolution),
                            this);
                    return false;
                }

                ApplyResolvedContentSet(packResolution.ContentSetResolution);
                UsingAssignedContentPack = true;
                AssignedContentPackStatus = "Using assigned content pack: " + packResolution.ContentPack.DisplayName;
                AssignedContentSetStatus = "Using content set from pack: " + packResolution.SelectedContentSet.DisplayName;
                if (packResolution.PackReport.WarningCount > 0 || packResolution.ContentSetResolution.Report.WarningCount > 0)
                {
                    Debug.LogWarning(
                        "[Idle Auto Defense Template] Assigned GameContentPackAsset '" + _contentPack.name + "' is playable with warnings. " + CreateContentPackIssueSummary(packResolution),
                        this);
                }

                return true;
            }

            if (_contentSet == null)
            {
                AssignedContentPackStatus = "No content pack assigned.";
                AssignedContentSetStatus = _requireAuthoredContent
                    ? "No content set assigned; strict startup will block gameplay."
                    : "No content set assigned; this unbound debug/test host may use explicit fallback mode.";
                return false;
            }

            GameContentSetResolution resolution = BasicIdleAutoDefenseGame.ResolveGameContentSetForTemplate(_contentSet);
            _resolvedContentSet = resolution;
            InvalidAssignedContentSetIssueCount = resolution.Report.ErrorCount;
            if (!resolution.IsValid)
            {
                AssignedContentSetStatus = _requireAuthoredContent
                    ? "Assigned content set is invalid; strict startup will block gameplay."
                    : "Assigned content set is invalid; this debug/test host may use explicit fallback mode.";
                if (!_requireAuthoredContent)
                    Debug.LogWarning(
                        "[Idle Auto Defense Template] Assigned GameContentSetAsset '" + _contentSet.name + "' is incomplete or invalid. Entering explicit fallback mode. " + CreateContentSetIssueSummary(resolution.Report),
                        this);
                return false;
            }

            ApplyResolvedContentSet(resolution);
            AssignedContentSetStatus = "Using assigned content set: " + resolution.ContentSet.DisplayName;
            if (resolution.Report.WarningCount > 0)
            {
                Debug.LogWarning(
                    "[Idle Auto Defense Template] Assigned GameContentSetAsset '" + _contentSet.name + "' is playable with warnings. " + CreateContentSetIssueSummary(resolution.Report),
                    this);
            }

            return true;
        }

        private void ApplyResolvedContentSet(GameContentSetResolution resolution)
        {
            _resolvedContentSet = resolution;
            _resolvedAttackRecipes = CopyResolved(resolution.AttackRecipes);
            _resolvedEnemyDefinitions = CopyResolved(resolution.Enemies);
            _resolvedWaveDefinitions = CopyResolved(resolution.Waves);
            _resolvedWeaponDefinitions = CopyResolved(resolution.Weapons);
            _resolvedUpgradeDefinitions = CopyResolved(resolution.Upgrades);
            _activeRewardCatalog = resolution.ContentSet.RewardCatalog;
            _activeEconomy = resolution.ContentSet.Economy;
            _activeRunProfile = resolution.ContentSet.RunProfile;
            _activeProgression = resolution.ContentSet.Progression;
            _activeOfflineProgression = resolution.ContentSet.OfflineProgression;
            _activeGameRules = resolution.ContentSet.GameRules;
            ApplyContentSetRuntimeSettings(resolution.ContentSet);
            FallbackModeActive = false;
            StartupBlocked = false;
            StartupError = string.Empty;
            UsingAssignedContentSet = true;
        }

        private void ApplyContentSetRuntimeSettings(GameContentSetAsset contentSet)
        {
            if (contentSet == null || contentSet.RuntimeSettings == null) return;
            IdleAutoDefenseContentSetRuntimeSettings settings = contentSet.RuntimeSettings;
            if (contentSet.RewardCatalog != null)
            {
                _rewardDraftSettings = contentSet.RewardCatalog.Settings.Clone();
                _rewardDraftCatalog = contentSet.RewardCatalog.Catalog.Clone();
            }
            IdleAutoDefensePresentationDebugSettings debug = settings.PresentationDebug;
            _showDebugAimLines = debug != null && debug.ShowDebugAimLines;
            _showDebugRanges = debug != null && debug.ShowDebugRanges;
            _showDebugSpawnRing = debug != null && debug.ShowDebugSpawnRing;
            UsingContentSetRuntimeSettings = true;
        }

        private WeaponDefinitionAsset[] ResolveActiveWeaponDefinitionsForRun()
        {
            WeaponDefinitionAsset startingWeapon = null;
            if (_resolvedContentSet != null && _resolvedContentSet.IsValid && _resolvedContentSet.ContentSet != null)
                startingWeapon = FindWeaponDefinitionForRun(_resolvedWeaponDefinitions, _resolvedContentSet.ContentSet.StartingWeapon);
            startingWeapon ??= FindWeaponDefinitionForRun(_resolvedWeaponDefinitions, BasicIdleAutoDefenseGame.ShardLauncherWeaponId.Value);
            startingWeapon ??= _resolvedWeaponDefinitions.Length > 0 ? _resolvedWeaponDefinitions[0] : null;
            return startingWeapon == null ? _resolvedWeaponDefinitions : new[] { startingWeapon };
        }

        private static WeaponDefinitionAsset FindWeaponDefinitionForRun(IReadOnlyList<WeaponDefinitionAsset> weapons, WeaponDefinitionAsset target)
        {
            return target == null ? null : FindWeaponDefinitionForRun(weapons, target.Id);
        }

        private static WeaponDefinitionAsset FindWeaponDefinitionForRun(IReadOnlyList<WeaponDefinitionAsset> weapons, string id)
        {
            if (weapons == null || string.IsNullOrWhiteSpace(id)) return null;
            for (int i = 0; i < weapons.Count; i++)
            {
                WeaponDefinitionAsset weapon = weapons[i];
                if (weapon != null && string.Equals(weapon.Id, id, StringComparison.OrdinalIgnoreCase))
                    return weapon;
            }

            return null;
        }

        private void ApplyContentSetStartingResources(GameContentSetResolution resolution)
        {
            if (resolution == null || !resolution.IsValid || _progressionState == null || _progressionCatalog == null) return;
            var currencies = new List<CurrencyLine>();
            if (_activeEconomy != null && _activeEconomy.StartingCredits > 0)
                currencies.Add(new CurrencyLine(RuntimeCredits, new ProgressionAmount(_activeEconomy.StartingCredits), true));
            if (_activeEconomy != null && _activeEconomy.StartingParts > 0)
                currencies.Add(new CurrencyLine(RuntimeParts, new ProgressionAmount(_activeEconomy.StartingParts), true));
            if (currencies.Count == 0) return;

            _progressionState.ApplyReward(
                _progressionCatalog,
                new ProgressionOperationId("idle-auto-defense.content-set.starting-resources." + BasicIdleAutoDefenseGame.SanitizeContentSetOperationSegment(resolution.ContentSet.Id)),
                new RewardBundle(currencies));
        }

        private void ApplyContentSetEconomyTuning(GameContentSetResolution resolution)
        {
            if (resolution == null || !resolution.IsValid) return;
            RewardCreditMultiplierBonus = Math.Max(0d, (_activeRunProfile == null ? 1f : _activeRunProfile.RewardMultiplier) - 1f);
        }

        private static string CreateContentSetIssueSummary(GameContentSetValidationReport report)
        {
            if (report == null || report.Issues.Count == 0) return "No validation details were reported.";
            var messages = new List<string>();
            for (int i = 0; i < report.Issues.Count; i++)
            {
                GameContentSetValidationIssue issue = report.Issues[i];
                messages.Add(issue.Path + ": " + issue.Message);
            }

            return string.Join(" | ", messages);
        }

        private static string CreateContentPackIssueSummary(GameContentPackResolution resolution)
        {
            if (resolution == null) return "No validation details were reported.";
            var messages = new List<string>();
            AddContentPackIssues(messages, resolution.PackReport);
            if (resolution.ContentSetResolution != null)
                AddContentSetIssues(messages, resolution.ContentSetResolution.Report);
            return messages.Count == 0 ? "No validation details were reported." : string.Join(" | ", messages);
        }

        private static void AddContentPackIssues(List<string> messages, GameContentPackValidationReport report)
        {
            if (messages == null || report == null) return;
            for (int i = 0; i < report.Issues.Count; i++)
            {
                GameContentPackValidationIssue issue = report.Issues[i];
                messages.Add(issue.Path + ": " + issue.Message);
            }
        }

        private static void AddContentSetIssues(List<string> messages, GameContentSetValidationReport report)
        {
            if (messages == null || report == null) return;
            for (int i = 0; i < report.Issues.Count; i++)
            {
                GameContentSetValidationIssue issue = report.Issues[i];
                messages.Add("ContentSet." + issue.Path + ": " + issue.Message);
            }
        }

        private static T[] CopyResolved<T>(IReadOnlyList<T> source)
        {
            if (source == null || source.Count == 0) return Array.Empty<T>();
            var copy = new T[source.Count];
            for (int i = 0; i < source.Count; i++) copy[i] = source[i];
            return copy;
        }

        private AttackDefinitionAsset[] ResolveAttackRecipes()
        {
            AttackDefinitionAsset[] recipes = BasicIdleAutoDefenseGame.ResolveAttackRecipesForTemplate(_attackRecipes, out int rejectedRecipeCount);
            InvalidAssignedRecipeCount = rejectedRecipeCount;
            if (InvalidAssignedRecipeCount > 0)
            {
                Debug.LogWarning(
                    "Idle Auto Defense template ignored invalid, duplicate, empty, or incomplete assigned attack recipe entries. The starter weapons require pulse, shard, arc, and homing attack recipes; missing required recipes fall back to built-in transient recipes.",
                    this);
            }

            return recipes;
        }

        private EnemyDefinitionAsset[] ResolveEnemyDefinitions()
        {
            EnemyDefinitionAsset[] definitions = BasicIdleAutoDefenseGame.ResolveEnemyDefinitionsForTemplate(_enemyDefinitions, out int rejectedDefinitionCount);
            InvalidAssignedEnemyCount = rejectedDefinitionCount;
            if (InvalidAssignedEnemyCount > 0)
            {
                Debug.LogWarning(
                    "Idle Auto Defense template ignored invalid, duplicate, empty, prefabless, or incomplete assigned enemy definition entries. The starter waves require swarm, runner, tank, shielded, elite, and boss enemy IDs; missing required enemies fall back to built-in transient enemies.",
                    this);
            }

            return definitions;
        }

        private WaveDefinitionAsset[] ResolveWaveDefinitions(IReadOnlyList<EnemyDefinitionAsset> enemyDefinitions)
        {
            WaveDefinitionAsset[] definitions = BasicIdleAutoDefenseGame.ResolveWaveDefinitionsForTemplate(_waveDefinitions, enemyDefinitions, out int rejectedDefinitionCount);
            InvalidAssignedWaveCount = rejectedDefinitionCount;
            if (InvalidAssignedWaveCount > 0)
            {
                Debug.LogWarning(
                    "Idle Auto Defense template ignored invalid, duplicate, empty, or enemy-mismatched assigned wave definition entries. Missing or invalid waves fall back to built-in transient starter waves.",
                    this);
            }

            return definitions;
        }

        private WeaponDefinitionAsset[] ResolveWeaponDefinitions(IReadOnlyList<AttackDefinitionAsset> attackRecipes)
        {
            WeaponDefinitionAsset[] definitions = BasicIdleAutoDefenseGame.ResolveWeaponDefinitionsForTemplate(_weaponDefinitions, attackRecipes, out int rejectedDefinitionCount);
            InvalidAssignedWeaponCount = rejectedDefinitionCount;
            if (InvalidAssignedWeaponCount > 0)
            {
                Debug.LogWarning(
                    "Idle Auto Defense template ignored invalid, duplicate, empty, or attack-mismatched assigned weapon definition entries. The starter mounts require shard launcher, pulse beam, arc burst, and homing pulse weapon IDs; missing required weapons fall back to built-in transient weapons.",
                    this);
            }

            return definitions;
        }

        private RunUpgradeDefinitionAsset[] ResolveUpgradeDefinitions()
        {
            RunUpgradeDefinitionAsset[] definitions = BasicIdleAutoDefenseGame.ResolveUpgradeDefinitionsForTemplate(_upgradeDefinitions, out int rejectedDefinitionCount);
            InvalidAssignedUpgradeCount = rejectedDefinitionCount;
            if (InvalidAssignedUpgradeCount > 0)
            {
                Debug.LogWarning(
                    "Idle Auto Defense template ignored invalid, duplicate, empty, or incomplete assigned upgrade definition entries. Missing or invalid upgrade sets fall back to built-in transient upgrades.",
                    this);
            }

            return definitions;
        }

        private SpawnableDefinition[] CreateProjectileSpawnables(IReadOnlyList<ProjectileDefinition> projectileDefinitions)
        {
            var spawnables = new List<SpawnableDefinition>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < projectileDefinitions.Count; i++)
            {
                ProjectileDefinition definition = projectileDefinitions[i];
                if (definition == null || !seen.Add(definition.SpawnableId.Value)) continue;
                spawnables.Add(new SpawnableDefinition(definition.SpawnableId, new GameObjectPrefabProvider(GetProjectilePrefab(definition)), 4, 32));
            }

            return spawnables.ToArray();
        }

        private SpawnableDefinition[] CreateEnemySpawnables(IReadOnlyList<EnemyDefinitionAsset> enemyDefinitions)
        {
            var spawnables = new List<SpawnableDefinition>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < enemyDefinitions.Count; i++)
            {
                EnemyDefinitionAsset definition = enemyDefinitions[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.Id) || !seen.Add(definition.Id)) continue;
                spawnables.Add(new SpawnableDefinition(new WorldSpawnableId(definition.Id), new GameObjectPrefabProvider(GetEnemyPrefab(definition)), 4, 64));
            }

            return spawnables.ToArray();
        }

        private static SpawnableDefinition[] CreateEnemySpawnables(AutoDefenseDefinition definition, GameObject prefab)
        {
            var spawnables = new SpawnableDefinition[definition.Enemies.Count];
            for (int i = 0; i < definition.Enemies.Count; i++)
                spawnables[i] = new SpawnableDefinition(definition.Enemies[i].SpawnableId, new GameObjectPrefabProvider(prefab), 4, 64);
            return spawnables;
        }

        private GameObject GetEnemyPrefab(EnemyDefinitionAsset enemy)
        {
            string id = enemy == null ? string.Empty : enemy.Id;
            if (string.IsNullOrWhiteSpace(id)) return _enemyPrefab;
            if (_runtimeEnemyPrefabs.TryGetValue(id, out GameObject cached) && cached != null)
                return cached;

            Color color = ResolveEnemyFallbackColor(id);
            GameObject authoredPrefab = enemy != null && enemy.Presentation != null ? enemy.Presentation.Prefab : null;
            GameObject prefab = authoredPrefab != null
                ? CreateRuntimeVisualPrefab(
                    "Kenney Enemy Runtime Prefab " + id,
                    authoredPrefab,
                    PrimitiveType.Capsule,
                    color,
                    ResolveEnemyKenneyArtPath(id),
                    new Vector3(0f, 0.62f, -0.08f),
                    ResolveEnemySpriteScale(id),
                    false,
                    24,
                    "EnemyVisual",
                    id,
                    string.Empty,
                    string.Empty,
                    "EnemyPrefab")
                : CreateEnemyModelPrefab("Kenney Enemy Runtime Prefab " + id, id, color);
            EnsureEnemyModelPresentation(prefab, color);
            _runtimeEnemyPrefabs[id] = prefab;
            return prefab;
        }

        private long CalculateOfflineBonusCredits(IdleProgressionResult result)
        {
            if (OfflineRewardMultiplierBonus <= 0d || result == null || result.Reward == null) return 0;
            for (int i = 0; i < result.Reward.CurrencyLines.Count; i++)
            {
                CurrencyLine line = result.Reward.CurrencyLines[i];
                if (line.CurrencyId.Equals(RuntimeCredits))
                    return (long)Math.Ceiling(line.Amount.Value * OfflineRewardMultiplierBonus);
            }

            return 0;
        }

        private void CreateCorePresentation(Vector3 position)
        {
            IdleAutoDefenseObjectivePresentationBinding presentation = ResolveObjectivePresentationBinding();
            GameObject core = new GameObject(presentation.DisplayName);
            core.transform.SetParent(_root.transform, false);
            core.transform.position = position;
            StampAuthoredVisibleInstance(core, "ObjectivePresentation", presentation.ContentId, core.name, string.Empty, string.Empty, "CoreBase", _resolvedContentSet == null ? null : _resolvedContentSet.ContentSet);
            IReadOnlyList<IdleAutoDefenseKenneyModelBinding> models = presentation.Models;
            for (int i = 0; i < models.Count; i++)
            {
                IdleAutoDefenseKenneyModelBinding model = models[i];
                if (model == null || string.IsNullOrWhiteSpace(model.ModelName)) continue;
                InstantiateKenneyModel(
                    model.ModelName,
                    core.transform,
                    model.LocalPosition,
                    Quaternion.Euler(model.LocalEulerAngles),
                    model.LocalScale,
                    model.Tint);
            }
            DisableColliders(core);

            IdleAutoDefenseModuleRule startingModule = ResolveModuleRule(IdleAutoDefenseModuleRole.StartingProjectile);
            CreateWeaponPresentation(
                startingModule == null ? BasicIdleAutoDefenseGame.ShardLauncherWeaponId.Value : startingModule.WeaponId,
                startingModule == null ? BasicIdleAutoDefenseGame.ShardAttackId.Value : startingModule.AttackId,
                true);
        }

        private IdleAutoDefenseObjectivePresentationBinding ResolveObjectivePresentationBinding()
        {
            IdleAutoDefenseContentSetRuntimeSettings settings = ContentSetRuntimeSettings;
            if (settings != null && settings.ObjectivePresentation != null)
            {
                AuthoredObjectivePresentationBindingCount++;
                return settings.ObjectivePresentation;
            }

            FallbackObjectivePresentationBindingCount++;
            return IdleAutoDefenseObjectivePresentationBinding.CreateDefault();
        }

        private IdleAutoDefenseWeaponVisualBinding CreateWeaponPresentation(
            string weaponId,
            string attackId,
            bool enabled)
        {
            IdleAutoDefenseWeaponPresentationBinding presentation = ResolveWeaponPresentationBinding(weaponId, attackId);
            string displayName = presentation.DisplayName;
            Vector3 position = presentation.MountLocalPosition;
            string baseModelName = presentation.BaseModelName;
            string weaponModelName = presentation.WeaponModelName;
            Color tint = presentation.Tint;
            Vector3 muzzleLocalPosition = presentation.MuzzleLocalPosition;
            float turnSpeedDegrees = presentation.TurnSpeedDegrees;
            if (_root == null || string.IsNullOrWhiteSpace(attackId)) return null;
            GameObject root = new GameObject(displayName + " 3D Mount");
            root.transform.SetParent(_root.transform, false);
            root.transform.localPosition = position;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            StampAuthoredVisibleInstance(root, "WeaponPresentation", weaponId, displayName, weaponId, attackId, "WeaponMount", _resolvedContentSet == null ? null : _resolvedContentSet.ContentSet, weaponId + ":mount", string.Empty);

            InstantiateKenneyModel(baseModelName, root.transform, Vector3.zero, Quaternion.identity, Vector3.one * 0.72f, tint);
            Transform yawPivot = new GameObject(displayName + " Yaw Pivot").transform;
            yawPivot.SetParent(root.transform, false);
            yawPivot.localPosition = new Vector3(0f, 0.48f, 0f);
            yawPivot.localRotation = Quaternion.identity;
            yawPivot.localScale = Vector3.one;

            Transform recoilPivot = new GameObject(displayName + " Recoil Pivot").transform;
            recoilPivot.SetParent(yawPivot, false);
            recoilPivot.localPosition = Vector3.zero;
            recoilPivot.localRotation = Quaternion.identity;
            recoilPivot.localScale = Vector3.one;

            WeaponDefinitionAsset authoredWeapon = FindWeaponDefinitionForPresentation(weaponId, attackId);
            GameObject authoredPrefab = authoredWeapon != null && authoredWeapon.Presentation != null
                ? authoredWeapon.Presentation.Prefab
                : null;
            if (authoredPrefab != null)
            {
                GameObject authoredInstance = Instantiate(authoredPrefab, recoilPivot, false);
                authoredInstance.name = displayName + " Authored Weapon Visual";
                StampAuthoredVisibleInstance(authoredInstance, "WeaponPrefab", weaponId, authoredPrefab.name, weaponId, attackId, "WeaponVisual", authoredPrefab, weaponId + ":muzzle.primary", string.Empty);
                authoredInstance.transform.localPosition = Vector3.zero;
                authoredInstance.transform.localRotation = Quaternion.identity;
                authoredInstance.transform.localScale = Vector3.one;
                IdleAutoDefenseKenneyModelPrefab[] authoredModels = authoredInstance.GetComponentsInChildren<IdleAutoDefenseKenneyModelPrefab>(true);
                for (int i = 0; i < authoredModels.Length; i++)
                    authoredModels[i].EnsureModel();
                TintRenderers(authoredInstance, tint);
                DisableColliders(authoredInstance);
                AuthoredWeaponPresentationSpawnCount++;
            }
            else
            {
                InstantiateKenneyModel(weaponModelName, recoilPivot, Vector3.zero, Quaternion.identity, Vector3.one * 0.74f, tint);
                FallbackWeaponPresentationSpawnCount++;
                FallbackVisibleGameplaySpawnCount++;
            }

            Transform muzzle = new GameObject(displayName + " Muzzle").transform;
            muzzle.SetParent(recoilPivot, false);
            muzzle.localPosition = muzzleLocalPosition;
            muzzle.localRotation = Quaternion.identity;
            muzzle.localScale = Vector3.one;

            var binding = root.AddComponent<IdleAutoDefenseWeaponVisualBinding>();
            binding.Configure(yawPivot, recoilPivot, muzzle, turnSpeedDegrees, tint);
            root.SetActive(enabled);
            DisableColliders(root);
            _weaponVisualBindings[attackId] = binding;
            return binding;
        }

        private IdleAutoDefenseWeaponPresentationBinding ResolveWeaponPresentationBinding(string weaponId, string attackId)
        {
            IdleAutoDefenseContentSetRuntimeSettings settings = ContentSetRuntimeSettings;
            if (settings != null && settings.TryFindWeaponPresentationBinding(weaponId, attackId, out IdleAutoDefenseWeaponPresentationBinding authored) && authored != null)
            {
                AuthoredWeaponPresentationBindingCount++;
                return authored;
            }

            IdleAutoDefenseWeaponPresentationBinding[] defaults = IdleAutoDefenseWeaponPresentationBinding.CreateDefaultBindings();
            for (int i = 0; i < defaults.Length; i++)
            {
                if (defaults[i] != null && defaults[i].Matches(weaponId, attackId))
                {
                    FallbackWeaponPresentationBindingCount++;
                    return defaults[i];
                }
            }

            FallbackWeaponPresentationBindingCount++;
            return new IdleAutoDefenseWeaponPresentationBinding(
                weaponId,
                attackId,
                string.IsNullOrWhiteSpace(weaponId) ? "Tower Module" : weaponId,
                Vector3.zero,
                "tower-round-bottom-a",
                "weapon-ballista",
                Color.white,
                new Vector3(0f, 0.5f, 0.7f),
                300f,
                false);
        }

        private WeaponDefinitionAsset FindWeaponDefinitionForPresentation(string weaponId, string attackId)
        {
            if (_resolvedWeaponDefinitions == null || _resolvedWeaponDefinitions.Length == 0)
                return null;

            if (!string.IsNullOrWhiteSpace(weaponId))
            {
                for (int i = 0; i < _resolvedWeaponDefinitions.Length; i++)
                {
                    WeaponDefinitionAsset weapon = _resolvedWeaponDefinitions[i];
                    if (weapon != null && string.Equals(weapon.Id, weaponId, StringComparison.OrdinalIgnoreCase))
                        return weapon;
                }
            }

            if (string.IsNullOrWhiteSpace(attackId))
                return null;

            for (int i = 0; i < _resolvedWeaponDefinitions.Length; i++)
            {
                WeaponDefinitionAsset weapon = _resolvedWeaponDefinitions[i];
                AttackDefinitionAsset attack = weapon != null && weapon.Stats != null ? weapon.Stats.Attack : null;
                if (attack != null && string.Equals(attack.Id, attackId, StringComparison.OrdinalIgnoreCase))
                    return weapon;
            }

            return null;
        }

        private GameObject CreateEnemyModelPrefab(string name, string enemyId, Color tint)
        {
            GameObject prefab = new GameObject(name);
            GameObject model = InstantiateKenneyModel(ResolveEnemyModelName(enemyId), prefab.transform, Vector3.zero, Quaternion.identity, ResolveEnemyModelScale(enemyId), tint);
            if (model == null)
            {
                GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                fallback.name = "Hidden Enemy Fallback Mesh";
                fallback.transform.SetParent(prefab.transform, false);
                fallback.transform.localScale = ResolveEnemyModelScale(enemyId);
                ApplyColor(fallback, tint);
            }

            var presentation = prefab.AddComponent<IdleAutoDefenseEnemyModelPresentation>();
            presentation.Configure(tint, Vector3.one);
            DisableColliders(prefab);
            prefab.SetActive(false);
            return prefab;
        }

        private static IdleAutoDefenseEnemyModelPresentation EnsureEnemyModelPresentation(GameObject prefab, Color tint)
        {
            if (prefab == null) return null;
            IdleAutoDefenseEnemyModelPresentation presentation = prefab.GetComponent<IdleAutoDefenseEnemyModelPresentation>();
            if (presentation == null)
                presentation = prefab.AddComponent<IdleAutoDefenseEnemyModelPresentation>();
            presentation.Configure(tint, Vector3.one);
            return presentation;
        }

        private GameObject CreateProjectileModelPrefab(string name, string modelName, Color tint)
        {
            GameObject prefab = new GameObject(name);
            GameObject model = InstantiateKenneyModel(modelName, prefab.transform, Vector3.zero, Quaternion.Euler(0f, 90f, 0f), Vector3.one * 0.34f, tint);
            if (model == null)
            {
                GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                fallback.name = "Hidden Projectile Fallback Mesh";
                fallback.transform.SetParent(prefab.transform, false);
                fallback.transform.localScale = Vector3.one * 0.2f;
                ApplyColor(fallback, tint);
            }

            AddProjectileTrail(prefab, tint);
            DisableColliders(prefab);
            prefab.SetActive(false);
            return prefab;
        }

        private GameObject InstantiateKenneyModel(string modelName, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Color? tint = null)
        {
            if (string.IsNullOrWhiteSpace(modelName)) return null;
            GameObject source = Resources.Load<GameObject>(Kenney3DResourceRoot + modelName);
            if (source == null) return null;

            GameObject instance = Instantiate(source, parent, false);
            instance.name = "Kenney 3D " + modelName;
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;
            instance.transform.localScale = localScale;
            DisableColliders(instance);
            if (tint.HasValue)
                TintRenderers(instance, tint.Value);
            Kenney3DModelSpawnCount++;
            return instance;
        }

        private static void TintRenderers(GameObject instance, Color tint)
        {
            if (instance == null) return;
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                Material source = renderer.sharedMaterial;
                Material material = source != null ? new Material(source) : (shader != null ? new Material(shader) : null);
                if (material == null) continue;
                if (material.HasProperty("_Color"))
                    material.color = Color.Lerp(material.color, tint, 0.22f);
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }

        private GameObject CreateRuntimeVisualPrefab(
            string name,
            GameObject sourcePrefab,
            PrimitiveType fallbackPrimitive,
            Color color,
            string kenneyArtPath,
            Vector3 spriteLocalPosition,
            Vector3 spriteScale,
            bool projectile,
            int sortingOrder,
            string definitionType,
            string contentId,
            string ownerWeaponId,
            string ownerAttackId,
            string effectRole)
        {
            GameObject prefab = sourcePrefab != null
                ? Instantiate(sourcePrefab)
                : GameObject.CreatePrimitive(fallbackPrimitive);
            prefab.name = name;
            prefab.transform.SetParent(_root != null ? _root.transform : null, false);
            prefab.transform.localPosition = Vector3.zero;
            prefab.transform.localRotation = Quaternion.identity;
            prefab.transform.localScale = Vector3.one;
            DisableColliders(prefab);
            if (sourcePrefab != null)
            {
                IdleAutoDefenseKenneyModelPrefab[] authoredModels = prefab.GetComponentsInChildren<IdleAutoDefenseKenneyModelPrefab>(true);
                for (int i = 0; i < authoredModels.Length; i++)
                    authoredModels[i].EnsureModel();
                TintRenderers(prefab, color);
                StampAuthoredVisibleInstance(prefab, definitionType, contentId, sourcePrefab.name, ownerWeaponId, ownerAttackId, effectRole, sourcePrefab);
            }
            else
            {
                FallbackVisibleGameplaySpawnCount++;
                ApplyColor(prefab, color);
                bool attachedSprite = prefab.GetComponentInChildren<SpriteRenderer>(true) != null ||
                    AttachKenneySprite(prefab, kenneyArtPath, false, spriteLocalPosition, spriteScale, sortingOrder, color);
                if (attachedSprite)
                {
                    TintSpriteRenderers(prefab, color);
                    HideMeshRenderers(prefab);
                }
            }

            if (projectile)
                AddProjectileTrail(prefab, color);
            prefab.SetActive(false);
            return prefab;
        }

        private void CreatePlayAreaMarkers()
        {
            IReadOnlyList<IdleAutoDefenseModuleSlotPresentationBinding> slots = ResolveModuleSlotPresentationBindings();
            for (int i = 0; i < slots.Count; i++)
                CreateModuleSlot(slots[i]);
            if (!_showDebugSpawnRing) return;
            Color warningStrip = new Color(0.95f, 0.68f, 0.18f, 0.95f);
            float radius = _activeGameRules == null ? TemplateVisibleArenaRadius : _activeGameRules.VisibleArenaRadius;
            CreateKenneyMarkerLine("Outer Spawn Zone North", new Vector3(0f, 0f, radius), Quaternion.identity, 6, warningStrip);
            CreateKenneyMarkerLine("Outer Spawn Zone East", new Vector3(radius, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), 6, warningStrip);
            CreateKenneyMarkerLine("Outer Spawn Zone South", new Vector3(0f, 0f, -radius), Quaternion.identity, 6, warningStrip);
            CreateKenneyMarkerLine("Outer Spawn Zone West", new Vector3(-radius, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), 6, warningStrip);
        }

        private IReadOnlyList<IdleAutoDefenseModuleSlotPresentationBinding> ResolveModuleSlotPresentationBindings()
        {
            IdleAutoDefenseContentSetRuntimeSettings settings = ContentSetRuntimeSettings;
            if (settings != null && settings.ModuleSlotPresentationBindings.Count > 0)
            {
                AuthoredModuleSlotPresentationBindingCount += settings.ModuleSlotPresentationBindings.Count;
                return settings.ModuleSlotPresentationBindings;
            }

            IdleAutoDefenseModuleSlotPresentationBinding[] defaults = IdleAutoDefenseModuleSlotPresentationBinding.CreateDefaultBindings();
            FallbackModuleSlotPresentationBindingCount += defaults.Length;
            return defaults;
        }

        private void CreateModuleSlot(IdleAutoDefenseModuleSlotPresentationBinding binding)
        {
            if (binding == null || string.IsNullOrWhiteSpace(binding.ModelName)) return;
            GameObject slot = InstantiateKenneyModel(
                binding.ModelName,
                _root.transform,
                binding.LocalPosition,
                Quaternion.Euler(binding.LocalEulerAngles),
                binding.LocalScale,
                binding.Tint);
            if (slot != null)
            {
                slot.name = binding.DisplayName;
                StampAuthoredVisibleInstance(slot, "ModuleSlotPresentation", binding.SlotId, binding.ModelName, binding.WeaponId, string.Empty, "ModuleSlot", _resolvedContentSet == null ? null : _resolvedContentSet.ContentSet);
            }
        }

        private void CreateArenaBackdrop()
        {
            if (_root == null) return;
            GameObject arenaRoot = new GameObject("Kenney Arena Backdrop");
            arenaRoot.transform.SetParent(_root.transform, false);
            StampAuthoredVisibleInstance(
                arenaRoot,
                "EnvironmentPresentation",
                "environment.idle-auto-defense.arena",
                arenaRoot.name,
                string.Empty,
                string.Empty,
                "ArenaBackdrop",
                _resolvedContentSet == null ? null : _resolvedContentSet.ContentSet);
            Color grass = new Color(0.52f, 0.86f, 0.46f);
            Color dirt = new Color(0.92f, 0.66f, 0.36f);
            const int half = 5;
            for (int x = -half; x <= half; x++)
            {
                for (int z = -half; z <= half; z++)
                {
                    bool path = Math.Abs(x) <= 1 || Math.Abs(z) <= 1;
                    string model = path ? "tile-dirt" : "tile";
                    Color tint = path ? dirt : grass;
                    InstantiateKenneyModel(model, arenaRoot.transform, new Vector3(x * 3f, -0.22f, z * 3f), Quaternion.identity, Vector3.one * 1.5f, tint);
                }
            }

            InstantiateKenneyModel("detail-rocks", arenaRoot.transform, new Vector3(-7.8f, 0f, 6.9f), Quaternion.Euler(0f, 20f, 0f), Vector3.one * 1.15f, Color.white);
            InstantiateKenneyModel("detail-tree", arenaRoot.transform, new Vector3(7.9f, 0f, -6.7f), Quaternion.Euler(0f, -25f, 0f), Vector3.one * 1.1f, Color.white);
            InstantiateKenneyModel("tile-crystal", arenaRoot.transform, new Vector3(8.7f, -0.12f, 7.9f), Quaternion.identity, Vector3.one * 0.82f, new Color(0.55f, 0.85f, 1f));
        }

        private void CreateKenneyMarkerLine(string name, Vector3 center, Quaternion rotation, int count, Color tint)
        {
            for (int i = 0; i < count; i++)
            {
                float offset = (i - (count - 1) * 0.5f) * 2.8f;
                Vector3 local = rotation * new Vector3(offset, 0f, 0f);
                GameObject marker = InstantiateKenneyModel("tile-spawn", _root.transform, center + local + new Vector3(0f, -0.11f, 0f), rotation, Vector3.one * 0.52f, tint);
                if (marker != null)
                    marker.name = name + " Marker " + (i + 1).ToString(CultureInfo.InvariantCulture);
            }
        }

        private void ConfigureGameplayCamera(Vector3 focus)
        {
            Camera camera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                var cameraObject = new GameObject("Idle Auto Defense Camera");
                cameraObject.transform.SetParent(_root != null ? _root.transform : null, false);
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            camera.orthographic = false;
            camera.fieldOfView = 44f;
            camera.transform.position = focus + new Vector3(2.4f, 16.8f, -15.6f);
            camera.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            camera.backgroundColor = new Color(0.06f, 0.09f, 0.12f, 1f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 120f;
            _shakeCamera = camera;
            _shakeCameraBaseLocalPosition = camera.transform.localPosition;
            _shakeCameraBaseCaptured = true;
        }

        private void ConfigureGameplayLighting()
        {
            Light existing = FindFirstObjectByType<Light>();
            if (existing != null)
            {
                existing.type = LightType.Directional;
                existing.transform.rotation = Quaternion.Euler(48f, -32f, 18f);
                existing.color = new Color(1f, 0.95f, 0.86f);
                existing.intensity = 1.18f;
                return;
            }

            GameObject lightObject = new GameObject("Idle Auto Defense Key Light");
            lightObject.transform.SetParent(_root != null ? _root.transform : null, false);
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 18f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.86f);
            light.intensity = 1.18f;
        }

        private static bool AttachKenneySprite(GameObject instance, string artPath, bool groundSprite, Vector3 localPosition, Vector3 localScale, int sortingOrder = 20, Color? tint = null)
        {
            if (instance == null || string.IsNullOrWhiteSpace(artPath)) return false;
            Sprite sprite = Resources.Load<Sprite>(KenneyResourceRoot + artPath);
            if (sprite == null) return false;

            var spriteObject = new GameObject("Kenney Visual");
            Transform spriteParent = groundSprite && instance.transform.parent != null
                ? instance.transform.parent
                : instance.transform;
            spriteObject.transform.SetParent(spriteParent, false);
            spriteObject.transform.localPosition = groundSprite
                ? instance.transform.localPosition + localPosition
                : localPosition;
            spriteObject.transform.localRotation = groundSprite ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.identity;
            spriteObject.transform.localScale = localScale;
            SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = tint ?? Color.white;
            renderer.sortingOrder = sortingOrder;
            if (!groundSprite)
            {
                var billboard = spriteObject.AddComponent<KenneyBillboardVisual>();
                billboard.Configure(true);
            }

            return true;
        }

        private static void AddProjectileTrail(GameObject instance, Color color)
        {
            if (instance == null) return;
            TrailRenderer trail = instance.GetComponentInChildren<TrailRenderer>(true);
            if (trail == null)
                trail = instance.AddComponent<TrailRenderer>();
            trail.time = 0.32f;
            trail.startWidth = 0.22f;
            trail.endWidth = 0.02f;
            trail.minVertexDistance = 0.03f;
            trail.autodestruct = false;
            trail.emitting = true;
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            if (shader != null)
                trail.sharedMaterial = new Material(shader) { color = color };
            trail.startColor = new Color(color.r, color.g, color.b, 0.88f);
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
        }

        private static string ResolveEnemyKenneyArtPath(string enemyId)
        {
            if (string.IsNullOrWhiteSpace(enemyId)) return "Art/enemy_basic_green";
            if (enemyId.IndexOf("runner", StringComparison.OrdinalIgnoreCase) >= 0) return "Art/enemy_fast_gray";
            if (enemyId.IndexOf("boss", StringComparison.OrdinalIgnoreCase) >= 0 ||
                enemyId.IndexOf("elite", StringComparison.OrdinalIgnoreCase) >= 0 ||
                enemyId.IndexOf("shield", StringComparison.OrdinalIgnoreCase) >= 0 ||
                enemyId.IndexOf("tank", StringComparison.OrdinalIgnoreCase) >= 0) return "Art/enemy_tank_brown";
            return "Art/enemy_basic_green";
        }

        private static Color ResolveEnemyFallbackColor(string enemyId)
        {
            if (string.IsNullOrWhiteSpace(enemyId)) return Color.red;
            if (enemyId.IndexOf("runner", StringComparison.OrdinalIgnoreCase) >= 0) return new Color(0.82f, 0.82f, 0.9f);
            if (enemyId.IndexOf("boss", StringComparison.OrdinalIgnoreCase) >= 0) return new Color(1f, 0.25f, 0.18f);
            if (enemyId.IndexOf("elite", StringComparison.OrdinalIgnoreCase) >= 0) return new Color(1f, 0.72f, 0.22f);
            if (enemyId.IndexOf("shield", StringComparison.OrdinalIgnoreCase) >= 0) return new Color(0.32f, 0.58f, 1f);
            if (enemyId.IndexOf("tank", StringComparison.OrdinalIgnoreCase) >= 0) return new Color(0.78f, 0.48f, 0.22f);
            return new Color(0.46f, 1f, 0.5f);
        }

        private static Vector3 ResolveEnemySpriteScale(string enemyId)
        {
            if (string.IsNullOrWhiteSpace(enemyId)) return new Vector3(1.1f, 1.1f, 1f);
            if (enemyId.IndexOf("boss", StringComparison.OrdinalIgnoreCase) >= 0) return new Vector3(2.7f, 2.7f, 1f);
            if (enemyId.IndexOf("elite", StringComparison.OrdinalIgnoreCase) >= 0) return new Vector3(1.9f, 1.9f, 1f);
            if (enemyId.IndexOf("tank", StringComparison.OrdinalIgnoreCase) >= 0) return new Vector3(1.45f, 1.45f, 1f);
            if (enemyId.IndexOf("shield", StringComparison.OrdinalIgnoreCase) >= 0) return new Vector3(1.32f, 1.32f, 1f);
            if (enemyId.IndexOf("runner", StringComparison.OrdinalIgnoreCase) >= 0) return new Vector3(1.0f, 1.0f, 1f);
            return new Vector3(1.15f, 1.15f, 1f);
        }

        private static string ResolveEnemyModelName(string enemyId)
        {
            if (string.IsNullOrWhiteSpace(enemyId)) return "enemy-ufo-a";
            if (enemyId.IndexOf("runner", StringComparison.OrdinalIgnoreCase) >= 0) return "enemy-ufo-b";
            if (enemyId.IndexOf("boss", StringComparison.OrdinalIgnoreCase) >= 0) return "enemy-ufo-d";
            if (enemyId.IndexOf("elite", StringComparison.OrdinalIgnoreCase) >= 0) return "enemy-ufo-d-weapon";
            if (enemyId.IndexOf("shield", StringComparison.OrdinalIgnoreCase) >= 0) return "enemy-ufo-c-weapon";
            if (enemyId.IndexOf("tank", StringComparison.OrdinalIgnoreCase) >= 0) return "enemy-ufo-c";
            return "enemy-ufo-a";
        }

        private static Vector3 ResolveEnemyModelScale(string enemyId)
        {
            if (string.IsNullOrWhiteSpace(enemyId)) return Vector3.one * 0.72f;
            if (enemyId.IndexOf("boss", StringComparison.OrdinalIgnoreCase) >= 0) return Vector3.one * 1.48f;
            if (enemyId.IndexOf("elite", StringComparison.OrdinalIgnoreCase) >= 0) return Vector3.one * 1.16f;
            if (enemyId.IndexOf("tank", StringComparison.OrdinalIgnoreCase) >= 0) return Vector3.one * 1.02f;
            if (enemyId.IndexOf("shield", StringComparison.OrdinalIgnoreCase) >= 0) return Vector3.one * 0.95f;
            if (enemyId.IndexOf("runner", StringComparison.OrdinalIgnoreCase) >= 0) return Vector3.one * 0.66f;
            return Vector3.one * 0.72f;
        }

        private static string ResolveProjectileModelName(AttackDefinitionAsset attack)
        {
            string attackId = attack == null ? string.Empty : attack.Id;
            if (attackId.IndexOf("homing", StringComparison.OrdinalIgnoreCase) >= 0) return "weapon-ammo-cannonball";
            if (attackId.IndexOf("arc", StringComparison.OrdinalIgnoreCase) >= 0) return "weapon-ammo-boulder";
            if (attackId.IndexOf("pulse", StringComparison.OrdinalIgnoreCase) >= 0) return "weapon-ammo-bullet";
            return "weapon-ammo-arrow";
        }

        private static void TintSpriteRenderers(GameObject instance, Color tint)
        {
            if (instance == null) return;
            SpriteRenderer[] renderers = instance.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].color = tint;
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
            Shader shader = Shader.Find("Standard");
            if (renderer != null && shader != null) renderer.sharedMaterial = new Material(shader) { color = color };
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
            SpawnedCount = 0;
            DirectOrCombatKillCount = 0;
            ProjectileLaunchCount = 0;
            ProjectileVisualSpawnCount = 0;
            AuthoredProjectileVisualSpawnCount = 0;
            ProjectileMotionObservedCount = 0;
            ProjectileDamageAppliedCount = 0;
            ProjectileImpactCallbackCount = 0;
            ProjectileDamageResolvedFromImpactCount = 0;
            ProjectileImpactRetargetCount = 0;
            ProjectileImpactMissCount = 0;
            ProjectileImpactRejectedCount = 0;
            ProjectileExpiryDeferralCount = 0;
            AttackVfxSpawnCount = 0;
            BeamVisualSpawnCount = 0;
            BeamVisualInvalidEndpointCount = 0;
            AttackAudioPlayCount = 0;
            EnemyPresentationEventCount = 0;
            Kenney3DModelSpawnCount = 0;
            TurretAimUpdateCount = 0;
            MuzzleProjectileLaunchCount = 0;
            MuzzleFlashSpawnCount = 0;
            RecoilEventCount = 0;
            EnemyFacingUpdateCount = 0;
            EnemyHitFlashCount = 0;
            EnemyDeathPopCount = 0;
            DamageNumberSpawnCount = 0;
            AuthoredVisibleInstanceStampCount = 0;
            FallbackVisibleGameplaySpawnCount = 0;
            AuthoredWeaponPresentationSpawnCount = 0;
            FallbackWeaponPresentationSpawnCount = 0;
            AuthoredWeaponPresentationBindingCount = 0;
            FallbackWeaponPresentationBindingCount = 0;
            AuthoredObjectivePresentationBindingCount = 0;
            FallbackObjectivePresentationBindingCount = 0;
            AuthoredModuleSlotPresentationBindingCount = 0;
            FallbackModuleSlotPresentationBindingCount = 0;
            UsingContentSetRuntimeSettings = false;
            DebugAimTracerSpawnCount = 0;
            EnemyDamageSurvivedCount = 0;
            RangeRejectedTargetCount = 0;
            EnemiesSpawnedBeyondStartingRangeCount = 0;
            EliteOrBossSpawnCount = 0;
            FirstRewardDraftSeconds = -1f;
            ProjectileAdapterKillCount = 0;
            InvalidAssignedRecipeCount = 0;
            InvalidAssignedEnemyCount = 0;
            InvalidAssignedWaveCount = 0;
            InvalidAssignedWeaponCount = 0;
            InvalidAssignedUpgradeCount = 0;
            InvalidAssignedContentPackIssueCount = 0;
            InvalidAssignedContentSetIssueCount = 0;
            UsingAssignedContentPack = false;
            UsingAssignedContentSet = false;
            AssignedContentPackStatus = string.Empty;
            AssignedContentSetStatus = string.Empty;
            ObjectiveReachCount = 0;
            ObjectiveDamageEvents = 0;
            DraftTickCount = 0;
            SelectedUpgradeCount = 0;
            RewardDraftOpenedCount = 0;
            RewardDraftSelectionCount = 0;
            LevelUpRewardDraftCount = 0;
            EliteRewardDraftCount = 0;
            BossRewardDraftCount = 0;
            WaveRewardExperienceCount = 0;
            EpicRewardSelectionCount = 0;
            LegendaryRewardSelectionCount = 0;
            EliteDefeatCount = 0;
            BossDefeatCount = 0;
            UpgradeFeedbackSpawnCount = 0;
            CommanderLevel = 1;
            CommanderExperience = 0;
            DirectDamageBonus = 0d;
            ProjectileSpeedMultiplier = 1d;
            EnemySpawnDelayTicks = 0;
            RewardCreditMultiplierBonus = 0d;
            OfflineRewardMultiplierBonus = 0d;
            RuntimeCurrency = 0;
            RuntimeCurrencyEarned = 0;
            RuntimeCurrencySpent = 0;
            SurvivalSeconds = 0f;
            _sessionElapsedTicks = 0;
            _simulationTickAccumulator = 0f;
            _endlessRestartPending = false;
            RunProfileVictoryReached = false;
            DamageUpgradeRank = 0;
            AttackSpeedUpgradeRank = 0;
            RangeUpgradeRank = 0;
            RepairUpgradeRank = 0;
            PulseBeamUnlocked = false;
            ArcBurstUnlocked = false;
            HomingPulseUnlocked = false;
            ModuleActivationCount = 0;
            OverdriveActivationCount = 0;
            UnsupportedUpgradeIntentCount = 0;
            EncounterRewardCredits = 0;
            EncounterRewardParts = 0;
            ReviveOfferAccepted = false;
            _completionRewardApplied = false;
            _manualTowerCooldownTicks = 0;
            _passiveIncomeTicks = 0;
            _pulseBeamModuleCooldownTicks = 0;
            _arcBurstModuleCooldownTicks = 0;
            _homingPulseModuleCooldownTicks = 0;
            _pendingProjectileImpacts.Clear();
            _seenEnemyIds.Clear();
            _enemyDeathPresentationIds.Clear();
            _sampleEnemyDamageById.Clear();
            _rewardedEnemyDefeatIds.Clear();
            _rewardedCompletedWaveIds.Clear();
            _queuedRewardDrafts.Clear();
            _weaponNormalUpgradeRanks.Clear();
            _weaponEpicUpgradeRanks.Clear();
            _weaponLegendaryUnlocks.Clear();
            _selectedRewardIds.Clear();
            _baseRewardRanks.Clear();
            _lastProjectileAgentPositions.Clear();
            _rewardDraftChoices = Array.Empty<IdleAutoDefenseRewardDraftChoice>();
            _activeRewardDraftKind = IdleAutoDefenseRewardDraftKind.LevelUp;
            _rewardDraftSeed = 0;
            _shardVolleyBonus = 0;
            _pulseBeamBonus = 0;
            _arcBurstBonus = 0;
            _homingPulseBonus = 0;
            _rewardDamageMultiplierBonus = 0d;
            _pendingAuthoredKillCredits = 0L;
            _starterRewardDraftOffered = false;
            _overdriveSecondsRemaining = 0f;
            _overdriveCooldownSecondsRemaining = 0f;
            _minimumEnemySpawnDistance = float.MaxValue;
            _closestEnemyDistanceToObjective = float.MaxValue;
            _cameraShakeSecondsRemaining = 0f;
            _cameraShakeDuration = 0f;
            _cameraShakeMagnitude = 0f;
            ClearActiveBeamVisuals();
            ClearDamageNumbers();
        }

        private void ClearSpawnedRuntimeObjects()
        {
            _enemySpawning?.Clear(false);
            _projectileSpawning?.Clear(false);
        }

        private void DisposeRuntimeObjects(bool destroySceneObjects = true)
        {
            if (destroySceneObjects)
            {
                _enemySpawning?.Dispose();
                _projectileSpawning?.Dispose();
                foreach (GameObject prefab in _runtimeEnemyPrefabs.Values)
                    DestroyTemplateObject(prefab);
                foreach (GameObject prefab in _runtimeProjectilePrefabs.Values)
                    DestroyTemplateObject(prefab);
                _runtimeEnemyPrefabs.Clear();
                _runtimeProjectilePrefabs.Clear();
                DestroyTemplateObject(_enemyPrefab);
                DestroyTemplateObject(_projectilePrefab);
                if (_fallbackPresentationClipIsRuntimeOwned)
                    DestroyTemplateObject(_fallbackPresentationClip);
                DestroyTemplateObject(_runtimePanelSettings);
                DestroyTemplateObject(_runtimeThemeStyleSheet);
                DestroyTemplateObject(_runtimeUiObject);
                DestroyTemplateObject(_root);
            }
            else
            {
                ClearSpawnedRuntimeObjects();
            }

            _enemySpawning = null;
            _projectileSpawning = null;
            _runtimeEnemyPrefabs.Clear();
            _runtimeProjectilePrefabs.Clear();
            _weaponVisualBindings.Clear();
            _enemyPresentationsById.Clear();
            _enemyPrefab = null;
            _projectilePrefab = null;
            _root = null;
            _runtimeAudioSource = null;
            _fallbackPresentationClip = null;
            _fallbackPresentationClipIsRuntimeOwned = false;
            _runtimePanelSettings = null;
            _runtimeThemeStyleSheet = null;
            _runtimeUiDocument = null;
            _runtimeUiObject = null;
            _runtimeUiRoot = null;
            _damageNumberLayer = null;
            RuntimeUiDirectStylesApplied = false;
            _resolvedProjectileDefinitions = Array.Empty<ProjectileDefinition>();
            _pendingProjectileImpacts.Clear();
            _seenEnemyIds.Clear();
            _enemyDeathPresentationIds.Clear();
            _enemyPresentationsById.Clear();
            _sampleEnemyDamageById.Clear();
            _rewardedEnemyDefeatIds.Clear();
            _rewardedCompletedWaveIds.Clear();
            _queuedRewardDrafts.Clear();
            _weaponNormalUpgradeRanks.Clear();
            _weaponEpicUpgradeRanks.Clear();
            _weaponLegendaryUnlocks.Clear();
            _baseRewardRanks.Clear();
            _lastProjectileAgentPositions.Clear();
            ClearActiveBeamVisuals();
            _rewardDraftChoices = Array.Empty<IdleAutoDefenseRewardDraftChoice>();
            _starterRewardDraftOffered = false;
            _overdriveSecondsRemaining = 0f;
            _overdriveCooldownSecondsRemaining = 0f;
            RestoreShakenCamera();
            _shakeCamera = null;
            _shakeCameraBaseCaptured = false;
            _cameraShakeSecondsRemaining = 0f;
            _cameraShakeDuration = 0f;
            _cameraShakeMagnitude = 0f;
            ClearDamageNumbers();
        }

        private void ClearDamageNumbers()
        {
            for (int i = 0; i < _damageNumbers.Count; i++)
                _damageNumbers[i].Label?.RemoveFromHierarchy();
            _damageNumbers.Clear();
        }

        private void ClearActiveBeamVisuals()
        {
            for (int i = _activeBeamVisuals.Count - 1; i >= 0; i--)
                DestroyTemplateObject(_activeBeamVisuals[i].Instance);
            _activeBeamVisuals.Clear();
        }

        private sealed class TemplateJitteredPerimeterPoseResolver : IAutoDefensePoseResolver, ISpawnPoseResolver
        {
            private const float AngleJitterDegrees = 17.5f;
            private const float RadiusJitter = 2.5f;
            private readonly AutoDefenseObjectiveDefinition _objective;
            private readonly Dictionary<WorldSpawnChannelId, AutoDefenseSpawnChannelDefinition> _channels = new Dictionary<WorldSpawnChannelId, AutoDefenseSpawnChannelDefinition>();
            private readonly float _radius;

            public TemplateJitteredPerimeterPoseResolver(AutoDefenseObjectiveDefinition objective, AutoDefenseSpawnRingDefinition ring)
            {
                _objective = objective ?? throw new ArgumentNullException(nameof(objective));
                if (ring == null) throw new ArgumentNullException(nameof(ring));
                _radius = ring.Radius;
                for (int i = 0; i < ring.Channels.Count; i++)
                    _channels.Add(ring.Channels[i].Id, ring.Channels[i]);
            }

            public bool TryResolvePose(WorldSpawnChannelId channelId, out SpawnPose pose)
            {
                return TryResolvePose(channelId, 0L, 0, string.Empty, out pose);
            }

            public SpawnPoseResult TryResolvePose(WorldSpawnRequest request)
            {
                return TryResolvePose(request.ChannelId, request.Sequence, request.Context.Tick, request.Context.GroupId, out SpawnPose pose)
                    ? SpawnPoseResult.Success(pose)
                    : SpawnPoseResult.Failure("Unknown auto-defense channel: " + request.ChannelId);
            }

            private bool TryResolvePose(WorldSpawnChannelId channelId, long sequence, int tick, string groupId, out SpawnPose pose)
            {
                if (!_channels.TryGetValue(channelId, out AutoDefenseSpawnChannelDefinition channel))
                {
                    pose = default;
                    return false;
                }

                int hash = StableHash(channelId.Value);
                hash = CombineHash(hash, StableHash(groupId));
                hash = CombineHash(hash, sequence.GetHashCode());
                hash = CombineHash(hash, tick);
                float angleOffset = (Hash01(hash) * 2f - 1f) * AngleJitterDegrees;
                float distanceOffset = Hash01(CombineHash(hash, 7919)) * RadiusJitter;
                float radians = (channel.AngleDegrees + angleOffset) * Mathf.Deg2Rad;
                Vector3 direction = new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians));
                float distance = _radius + distanceOffset;
                pose = new SpawnPose(_objective.Position + direction * distance, Quaternion.LookRotation(-direction, Vector3.up));
                return true;
            }

            private static int CombineHash(int current, int value)
            {
                unchecked { return (current * 397) ^ value; }
            }

            private static float Hash01(int hash)
            {
                unchecked
                {
                    uint value = (uint)hash;
                    value ^= value >> 16;
                    value *= 2246822519u;
                    value ^= value >> 13;
                    value *= 3266489917u;
                    value ^= value >> 16;
                    return (value & 0x00FFFFFFu) / 16777215f;
                }
            }

            private static int StableHash(string value)
            {
                unchecked
                {
                    int hash = 5381;
                    if (!string.IsNullOrEmpty(value))
                    {
                        for (int i = 0; i < value.Length; i++)
                            hash = ((hash << 5) + hash) ^ value[i];
                    }

                    return hash;
                }
            }
        }

        private struct PendingProjectileImpact
        {
            public PendingProjectileImpact(
                ProjectileInstanceId projectileId,
                long targetEnemyId,
                AttackDefinitionAsset attack,
                Vector3 destination,
                double damageThreshold,
                int remainingTicks)
            {
                ProjectileId = projectileId;
                TargetEnemyId = targetEnemyId;
                Attack = attack;
                Destination = destination;
                DamageThreshold = damageThreshold;
                RemainingTicks = remainingTicks;
            }

            public ProjectileInstanceId ProjectileId;
            public long TargetEnemyId;
            public AttackDefinitionAsset Attack;
            public Vector3 Destination;
            public double DamageThreshold;
            public int RemainingTicks;
        }

        private struct DamageNumberView
        {
            public DamageNumberView(Label label, Vector3 worldPosition, float elapsedSeconds)
            {
                Label = label;
                WorldPosition = worldPosition;
                ElapsedSeconds = elapsedSeconds;
            }

            public Label Label;
            public Vector3 WorldPosition;
            public float ElapsedSeconds;
        }

        private struct ActiveBeamVisual
        {
            public ActiveBeamVisual(GameObject instance, GameObject prefab, AttackDefinitionAsset attack, long targetEnemyId, Vector3 lastImpactPosition, float durationSeconds)
            {
                Instance = instance;
                Prefab = prefab;
                Attack = attack;
                TargetEnemyId = targetEnemyId;
                LastImpactPosition = lastImpactPosition;
                DurationSeconds = Mathf.Max(0.05f, durationSeconds);
                ElapsedSeconds = 0f;
            }

            public GameObject Instance;
            public GameObject Prefab;
            public AttackDefinitionAsset Attack;
            public long TargetEnemyId;
            public Vector3 LastImpactPosition;
            public float DurationSeconds;
            public float ElapsedSeconds;
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
        private static readonly TrackId AccountXp = new TrackId("track.idle-auto-defense.account");
        private static readonly UnlockId StarterUnlock = new UnlockId("unlock.idle-auto-defense.starter");
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
            upgradeState.Select(upgradeCatalog, new RunUpgradeId("upgrade.idle-auto-defense.damage-up"));
            RunUpgradeSnapshot upgradeSnapshot = upgradeState.CreateSnapshot();
            var run = RunResumeDto.FromSnapshot("run.template.1", 42, upgradeSnapshot, lastSeen.UtcTicks);
            WriteResult runSave = await service.SaveAsync(runDefinition, run, slot, CancellationToken.None);
            LoadResult<RunResumeDto> runLoad = await service.LoadAsync(runDefinition, slot, CancellationToken.None);
            RunUpgradeState restoredUpgradeState = RunUpgradeState.FromSnapshot(runLoad.Document.ToSnapshot());

            var settings = new SettingsDto { AudioVolume = 0.8f, ReducedMotion = true };
            WriteResult settingsSave = await service.SaveAsync(settingsDefinition, settings, slot, CancellationToken.None);
            LoadResult<SettingsDto> settingsLoad = await service.LoadAsync(settingsDefinition, slot, CancellationToken.None);

            RewardBundle runReward = BasicIdleAutoDefenseGame.CreateEncounterCompletionReward();
            ProgressionResult runRewardResult = progressionState.ApplyReward(progressionCatalog, new ProgressionOperationId("idle-auto-defense.run.complete.1"), runReward);

            IdleProgressionResult offline = IdleProgressionCalculator.Calculate(
                new DateTimeOffset(profileLoad.Document.LastSeenUtcTicks, TimeSpan.Zero),
                lastSeen.AddHours(1),
                BasicIdleAutoDefenseGame.CreateOfflineProgressionDefinition());
            ProgressionResult offlineRewardResult = progressionState.ApplyReward(progressionCatalog, new ProgressionOperationId("idle-auto-defense.offline.1"), offline.Reward);

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
                RunUpgradeSnapshotRestored = restoredUpgradeState.GetRank(new RunUpgradeId("upgrade.idle-auto-defense.damage-up")) == 1,
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
