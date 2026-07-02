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
        public static readonly DamageTypeId DamageType = new DamageTypeId("damage.template.basic");
        public static readonly AttackDefinitionId PulseAttackId = new AttackDefinitionId("attack.template.pulse-cannon");
        public static readonly AttackDefinitionId ShardAttackId = new AttackDefinitionId("attack.template.shard-launcher");
        public static readonly AttackDefinitionId ArcBurstAttackId = new AttackDefinitionId("attack.template.arc-burst");
        public static readonly AttackDefinitionId HomingPulseAttackId = new AttackDefinitionId("attack.template.homing-pulse");
        public static readonly AttackDefinitionId AttackId = PulseAttackId;
        public static readonly ProjectileDefinitionId ShardProjectileId = new ProjectileDefinitionId("projectile.template.shard");
        public static readonly ProjectileDefinitionId HomingPulseProjectileId = new ProjectileDefinitionId("projectile.template.homing-pulse");
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
        public static readonly WeaponDefinitionId ArcBurstTowerWeaponId = new WeaponDefinitionId("weapon.template.arc-burst-tower");
        public static readonly WeaponDefinitionId HomingSpireWeaponId = new WeaponDefinitionId("weapon.template.homing-spire");
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
            ShieldedEnemySpawnableId.Value
        };
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

        public static AutoDefenseDefinition CreateDefinition(IReadOnlyList<EnemyDefinitionAsset> enemyDefinitions = null, IReadOnlyList<WeaponDefinitionAsset> weaponDefinitions = null)
        {
            WeaponDefinitionAsset[] weapons = weaponDefinitions == null || weaponDefinitions.Count == 0
                ? CreateWeaponDefinitionAssets(CreateAttackRecipes())
                : CopyWeaponDefinitions(weaponDefinitions);
            AutoDefenseEnemyDefinition[] enemies = enemyDefinitions == null
                ? CreateDefaultAutoDefenseEnemyDefinitions()
                : CreateAutoDefenseEnemyDefinitions(enemyDefinitions);
            AutoDefenseMountDefinition[] mounts = CreateAutoDefenseMountDefinitions(weapons);
            return new AutoDefenseDefinition(
                new AutoDefenseObjectiveDefinition(new DefenseObjectiveId("template-core"), Vector3.zero, 60, DamageType, 0.45f, 6, 2),
                AutoDefenseSpawnRingDefinition.FourWay(9f),
                enemies,
                mounts,
                CreateAutoDefenseWeaponModuleDefinitions(weapons, mounts));
        }

        public static EncounterDefinition CreateEncounterDefinition(IReadOnlyList<WaveDefinitionAsset> waveDefinitions = null)
        {
            if (waveDefinitions == null || waveDefinitions.Count == 0)
                return CreateFirstOrbitEncounterDefinition();
            return new EncounterDefinition(
                new EncounterId("encounter.template.first-orbit"),
                null,
                CreateEncounterWaves(waveDefinitions),
                new[] { ObjectiveDefinition.AllWavesEmitted(new EncounterObjectiveId("all-waves-emitted")) },
                seed: 20260623);
        }

        public static GameContentSetResolution ResolveGameContentSetForTemplate(GameContentSetAsset contentSet)
        {
            return GameContentSetValidator.Resolve(contentSet);
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

        public static CombatCatalog CreateCombatCatalog(IReadOnlyList<AttackDefinitionAsset> attackRecipes = null, IReadOnlyList<EnemyDefinitionAsset> enemyDefinitions = null)
        {
            attackRecipes ??= CreateAttackRecipes();
            enemyDefinitions ??= CreateEnemyDefinitions();
            var damageTypes = new List<DamageTypeDefinition>();
            var damageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddDamageType(damageTypes, damageIds, DamageType);
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
                    20,
                    5.25f,
                    ShardProjectileId.Value,
                    buildCost: 35,
                    upgradeGroupId: "upgrade.group.template.shard",
                    tags: new[] { "template", "projectile", "tower" }),
                WeaponDefinitionAsset.CreateTransient(
                    PulseCannonWeaponId.Value,
                    "Pulse Beam",
                    WeaponFireMode.DirectAttack,
                    pulse,
                    28,
                    4.75f,
                    buildCost: 25,
                    upgradeGroupId: "upgrade.group.template.pulse",
                    tags: new[] { "template", "hitscan", "tower" }),
                WeaponDefinitionAsset.CreateTransient(
                    ArcBurstTowerWeaponId.Value,
                    "Arc Burst Module",
                    WeaponFireMode.DirectAttack,
                    arc,
                    46,
                    3.75f,
                    buildCost: 65,
                    upgradeGroupId: "upgrade.group.template.arc",
                    tags: new[] { "template", "area", "tower" }),
                WeaponDefinitionAsset.CreateTransient(
                    HomingSpireWeaponId.Value,
                    "Homing Pulse Module",
                    WeaponFireMode.Projectile,
                    homing,
                    34,
                    4.5f,
                    HomingPulseProjectileId.Value,
                    buildCost: 55,
                    upgradeGroupId: "upgrade.group.template.homing",
                    tags: new[] { "template", "homing", "tower" })
            };
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
                EnemyDefinitionAsset.CreateTransient(SwarmEnemySpawnableId.Value, "Swarm", EnemyRole.Swarm, 5f, 1.1f, 1, 1f, DamageType.Value, 0.25f, tags: new[] { "template", "swarm" }),
                EnemyDefinitionAsset.CreateTransient(RunnerEnemySpawnableId.Value, "Runner", EnemyRole.Fast, 8f, 1.6f, 1, 2f, DamageType.Value, 0.24f, tags: new[] { "template", "runner" }),
                EnemyDefinitionAsset.CreateTransient(TankEnemySpawnableId.Value, "Tank", EnemyRole.Tank, 22f, 0.7f, 3, 3f, DamageType.Value, 0.42f, tags: new[] { "template", "tank" }),
                EnemyDefinitionAsset.CreateTransient(ShieldedEnemySpawnableId.Value, "Shielded", EnemyRole.Basic, 14f, 0.95f, 2, 2f, DamageType.Value, 0.34f, tags: new[] { "template", "shielded" }),
                EnemyDefinitionAsset.CreateTransient(EliteEnemySpawnableId.Value, "Elite", EnemyRole.Boss, 34f, 0.9f, 4, 5f, DamageType.Value, 0.36f, tags: new[] { "template", "elite" }),
                EnemyDefinitionAsset.CreateTransient(BossEnemySpawnableId.Value, "Boss", EnemyRole.Boss, 96f, 0.55f, 8, 12f, DamageType.Value, 0.65f, tags: new[] { "template", "boss" })
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

        public static AutoDefenseEnemyDefinition[] CreateAutoDefenseEnemyDefinitions(IReadOnlyList<EnemyDefinitionAsset> enemyDefinitions)
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
                definitions[i] = new AutoDefenseEnemyDefinition(
                    new WorldSpawnableId(enemy.Id),
                    enemy.Stats.MaximumHealth,
                    enemy.Stats.MoveSpeed,
                    enemy.Stats.ContactDamage,
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
                    "wave.template.authored.opening",
                    "Opening Wave",
                    0,
                    new[]
                    {
                        new WaveEntryRecipe(enemies[0], 4, 1, 0, 28, "perimeter-north"),
                        new WaveEntryRecipe(enemies[1], 2, 1, 35, 42, "perimeter-east"),
                        new WaveEntryRecipe(enemies[2], 1, 1, 110, 0, "perimeter-west")
                    },
                    new[] { "template", "opening" }),
                WaveDefinitionAsset.CreateTransient(
                    "wave.template.authored.runner-pressure",
                    "Runner Pressure",
                    130,
                    new[]
                    {
                        new WaveEntryRecipe(enemies[1], 4, 1, 0, 32, "perimeter-east", 1),
                        new WaveEntryRecipe(enemies[0], 4, 1, 24, 30, "perimeter-north", 1)
                    },
                    new[] { "template", "runner-pressure" }),
                WaveDefinitionAsset.CreateTransient(
                    "wave.template.authored.pressure",
                    "Mixed Pressure",
                    230,
                    new[]
                    {
                        new WaveEntryRecipe(enemies[3], 2, 1, 0, 42, "perimeter-south", 1),
                        new WaveEntryRecipe(enemies[2], 2, 1, 30, 36, "perimeter-east", 2),
                        new WaveEntryRecipe(enemies[1], 4, 1, 55, 28, "perimeter-north", 2)
                    },
                    new[] { "template", "pressure" }),
                WaveDefinitionAsset.CreateTransient(
                    "wave.template.authored.surge",
                    "Tank Break",
                    330,
                    new[]
                    {
                        new WaveEntryRecipe(enemies[0], 5, 1, 0, 26, "perimeter-north", 1),
                        new WaveEntryRecipe(enemies[1], 3, 1, 32, 36, "perimeter-south", 1),
                        new WaveEntryRecipe(enemies[3], 2, 1, 68, 42, "perimeter-west", 2)
                    },
                    new[] { "template", "tank-break" }),
                WaveDefinitionAsset.CreateTransient(
                    "wave.template.authored.final",
                    "Final Surge",
                    450,
                    new[]
                    {
                        new WaveEntryRecipe(enemies[2], 2, 1, 0, 52, "perimeter-north", 2),
                        new WaveEntryRecipe(enemies[3], 3, 1, 28, 42, "perimeter-east", 2),
                        new WaveEntryRecipe(enemies[1], 4, 1, 70, 30, "perimeter-south", 2)
                    },
                    new[] { "template", "final" })
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
                    8,
                    0,
                    4.75f,
                    AttackRecipeTargetingMode.Nearest),
                AttackDefinitionAsset.CreateTransient(
                    ShardAttackId.Value,
                    "Shard Projectile",
                    AttackRecipeDeliveryMode.Projectile,
                    DamageType.Value,
                    10,
                    0,
                    5.25f,
                    AttackRecipeTargetingMode.Strongest,
                    projectileDefinitionId: ShardProjectileId.Value,
                    projectileSpawnableId: ProjectileSpawnableId.Value,
                    projectileSpeed: 6f,
                    projectileLifetimeTicks: 120,
                    pierceCount: 0),
                AttackDefinitionAsset.CreateTransient(
                    ArcBurstAttackId.Value,
                    "Arc Burst",
                    AttackRecipeDeliveryMode.Area,
                    DamageType.Value,
                    9,
                    22,
                    3.75f,
                    AttackRecipeTargetingMode.Strongest),
                AttackDefinitionAsset.CreateTransient(
                    HomingPulseAttackId.Value,
                    "Homing Pulse",
                    AttackRecipeDeliveryMode.Projectile,
                    DamageType.Value,
                    7,
                    0,
                    4.5f,
                    AttackRecipeTargetingMode.LowestHealth,
                    projectileDefinitionId: HomingPulseProjectileId.Value,
                    projectileSpawnableId: HomingPulseProjectileId.Value,
                    projectileSpeed: 5.5f,
                    projectileLifetimeTicks: 120,
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
                UpgradeAsset("upgrade.template.damage-up", "Damage Boost", RunUpgradeAuthoringTargetKind.AttackDamage, RunUpgradeModifierType.Additive, 1.5, shard, "10,20,35", RunUpgradeRarity.Common, 6, 3),
                UpgradeAsset("upgrade.template.fire-rate-up", "Fire Rate Boost", RunUpgradeAuthoringTargetKind.AttackRate, RunUpgradeModifierType.Additive, 1, shard, "8,16,28", RunUpgradeRarity.Common, 5, 3),
                UpgradeAsset("upgrade.template.range-up", "Range Boost", RunUpgradeAuthoringTargetKind.Range, RunUpgradeModifierType.Additive, 1.25, shard, "12,24,36", RunUpgradeRarity.Common, 4, 3),
                UpgradeAsset("upgrade.template.projectile-speed-up", "Projectile Speed", RunUpgradeAuthoringTargetKind.ProjectileSpeed, RunUpgradeModifierType.Multiplicative, 0.35, shard, "10,20,40", RunUpgradeRarity.Common, 5, 3),
                UpgradeAsset("upgrade.template.objective-max-health-up", "Core Reinforcement", RunUpgradeAuthoringTargetKind.WeaponStat, RunUpgradeModifierType.Additive, 8, null, "14,28,42", RunUpgradeRarity.Uncommon, 3, 3, "objective.template-core", "template.objective.max_health"),
                UpgradeAsset("upgrade.template.enemy-reward-up", "Credit Reward", RunUpgradeAuthoringTargetKind.EnemyReward, RunUpgradeModifierType.Multiplicative, 0.15, null, "16,32,48", RunUpgradeRarity.Uncommon, 3, 3, "reward.template.run")
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

        private static AutoDefenseMountDefinition[] CreateAutoDefenseMountDefinitions(IReadOnlyList<WeaponDefinitionAsset> weapons)
        {
            var mounts = new AutoDefenseMountDefinition[weapons.Count];
            float spacing = weapons.Count <= 1 ? 0f : 3.2f / Math.Max(1, weapons.Count - 1);
            float start = weapons.Count <= 1 ? 0f : -1.6f;
            for (int i = 0; i < weapons.Count; i++)
            {
                WeaponDefinitionAsset weapon = weapons[i];
                string id = weapon == null || string.IsNullOrWhiteSpace(weapon.Id)
                    ? "weapon.template.missing." + i.ToString(CultureInfo.InvariantCulture)
                    : weapon.Id.Trim();
                var weaponId = new WeaponDefinitionId(id);
                var mountId = new AutoDefenseMountId("mount.template." + SanitizeRuntimeSegment(id));
                var slotId = new WeaponSlotId("slot.template." + SanitizeRuntimeSegment(id));
                mounts[i] = new AutoDefenseMountDefinition(mountId, new Vector3(start + spacing * i, 0f, 0f), slotId, weaponId);
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
                new[] { "template", "upgrade" });
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
                Enemy(SwarmEnemySpawnableId, 5, 1.1f, 1, 0.25f),
                Enemy(RunnerEnemySpawnableId, 8, 1.6f, 2, 0.24f),
                Enemy(TankEnemySpawnableId, 22, 0.7f, 3, 0.42f),
                Enemy(ShieldedEnemySpawnableId, 14, 0.95f, 2, 0.34f),
                Enemy(EliteEnemySpawnableId, 34, 0.9f, 5, 0.36f),
                Enemy(BossEnemySpawnableId, 96, 0.55f, 12, 0.65f)
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
        private const long DefaultRuntimeStartingCredits = 60;
        private const long KillRewardCredits = 6;
        private const int PassiveIncomeIntervalTicks = 90;
        private const int ManualTowerBaseCooldownTicks = 20;
        private const int ManualTowerMinimumCooldownTicks = 12;
        private const double ManualTowerBaseDamage = 4d;
        private const double ManualTowerDamageRankBonus = 4d;
        private const double ManualTowerRangeRankBonus = 1d;
        private const double SampleProjectileFinishThreshold = 3d;
        private const float TemplateSpawnLaneRadius = 9f;
        private const int PulseBeamModuleUnlockCost = 30;
        private const int ArcBurstModuleUnlockCost = 40;
        private const int HomingPulseModuleUnlockCost = 35;
        private const int PulseBeamModuleCooldownTicks = 28;
        private const int ArcBurstModuleCooldownTicks = 46;
        private const int HomingPulseModuleCooldownTicks = 34;
        private const int MinimumProjectileImpactDelayTicks = 8;
        private const int MaximumProjectileImpactDelayTicks = 36;
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
        private AttackDefinitionAsset[] _resolvedAttackRecipes = Array.Empty<AttackDefinitionAsset>();
        private EnemyDefinitionAsset[] _resolvedEnemyDefinitions = Array.Empty<EnemyDefinitionAsset>();
        private WaveDefinitionAsset[] _resolvedWaveDefinitions = Array.Empty<WaveDefinitionAsset>();
        private WeaponDefinitionAsset[] _resolvedWeaponDefinitions = Array.Empty<WeaponDefinitionAsset>();
        private RunUpgradeDefinitionAsset[] _resolvedUpgradeDefinitions = Array.Empty<RunUpgradeDefinitionAsset>();
        private GameContentSetResolution _resolvedContentSet;
        private GameObject _enemyPrefab;
        private GameObject _projectilePrefab;
        private GameObject _root;
        private ProjectileDefinition[] _resolvedProjectileDefinitions = Array.Empty<ProjectileDefinition>();
        private readonly List<PendingProjectileImpact> _pendingProjectileImpacts = new List<PendingProjectileImpact>();
        private readonly HashSet<long> _seenEnemyIds = new HashSet<long>();
        private readonly HashSet<long> _enemyDeathPresentationIds = new HashSet<long>();
        private readonly List<DamageNumberView> _damageNumbers = new List<DamageNumberView>();
        private readonly Dictionary<long, Vector3> _lastProjectileAgentPositions = new Dictionary<long, Vector3>();
        private UIDocument _runtimeUiDocument;
        private PanelSettings _runtimePanelSettings;
        private GameObject _runtimeUiObject;
        private VisualElement _runtimeUiRoot;
        private VisualElement _damageNumberLayer;
        private AudioSource _runtimeAudioSource;
        private AudioClip _fallbackPresentationClip;
        private MonetizationSession _monetizationSession;
        private int _manualTowerCooldownTicks;
        private int _passiveIncomeTicks;
        private int _pulseBeamModuleCooldownTicks;
        private int _arcBurstModuleCooldownTicks;
        private int _homingPulseModuleCooldownTicks;

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
        public int ProjectileVisualSpawnCount { get; private set; }
        public int AuthoredProjectileVisualSpawnCount { get; private set; }
        public int ProjectileMotionObservedCount { get; private set; }
        public int AttackVfxSpawnCount { get; private set; }
        public int AttackAudioPlayCount { get; private set; }
        public int EnemyPresentationEventCount { get; private set; }
        public int DamageNumberSpawnCount { get; private set; }
        public int InvalidAssignedRecipeCount { get; private set; }
        public int InvalidAssignedEnemyCount { get; private set; }
        public int InvalidAssignedWaveCount { get; private set; }
        public int InvalidAssignedWeaponCount { get; private set; }
        public int InvalidAssignedUpgradeCount { get; private set; }
        public int InvalidAssignedContentPackIssueCount { get; private set; }
        public int InvalidAssignedContentSetIssueCount { get; private set; }
        public bool UsingAssignedContentPack { get; private set; }
        public bool UsingAssignedContentSet { get; private set; }
        public string AssignedContentPackStatus { get; private set; } = string.Empty;
        public string AssignedContentSetStatus { get; private set; } = string.Empty;
        public int ObjectiveReachCount { get; private set; }
        public int ObjectiveDamageEvents { get; private set; }
        public int DraftTickCount { get; private set; }
        public int SelectedUpgradeCount { get; private set; }
        public double DirectDamageBonus { get; private set; }
        public double ProjectileSpeedMultiplier { get; private set; } = 1d;
        public int EnemySpawnDelayTicks { get; private set; }
        public double RewardCreditMultiplierBonus { get; private set; }
        public double OfflineRewardMultiplierBonus { get; private set; }
        public long RuntimeCurrency { get; private set; }
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
        public bool EncounterCompleted => _runtime != null && _runtime.State == AutoDefenseRuntimeState.Completed;
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
        public int DamageUpgradeCost => CalculateUpgradeCost(20, DamageUpgradeRank);
        public int AttackSpeedUpgradeCost => CalculateUpgradeCost(18, AttackSpeedUpgradeRank);
        public int RangeUpgradeCost => CalculateUpgradeCost(18, RangeUpgradeRank);
        public int RepairUpgradeCost => CalculateUpgradeCost(16, RepairUpgradeRank);
        public bool PulseBeamUnlocked { get; private set; }
        public bool ArcBurstUnlocked { get; private set; }
        public bool HomingPulseUnlocked { get; private set; }
        public int PulseBeamUnlockCost => PulseBeamModuleUnlockCost;
        public int ArcBurstUnlockCost => ArcBurstModuleUnlockCost;
        public int HomingPulseUnlockCost => HomingPulseModuleUnlockCost;
        public bool CanPurchasePulseBeamModule => !PulseBeamUnlocked && CanSpendRuntimeCurrency(PulseBeamUnlockCost);
        public bool CanPurchaseArcBurstModule => !ArcBurstUnlocked && CanSpendRuntimeCurrency(ArcBurstUnlockCost);
        public bool CanPurchaseHomingPulseModule => !HomingPulseUnlocked && CanSpendRuntimeCurrency(HomingPulseUnlockCost);
        public int UnlockedModuleCount => 1 + (PulseBeamUnlocked ? 1 : 0) + (ArcBurstUnlocked ? 1 : 0) + (HomingPulseUnlocked ? 1 : 0);
        public int ModuleActivationCount { get; private set; }
        public bool CanPurchaseDamageUpgrade => CanSpendRuntimeCurrency(DamageUpgradeCost);
        public bool CanPurchaseAttackSpeedUpgrade => CanSpendRuntimeCurrency(AttackSpeedUpgradeCost);
        public bool CanPurchaseRangeUpgrade => CanSpendRuntimeCurrency(RangeUpgradeCost);
        public bool CanPurchaseRepairUpgrade => CanSpendRuntimeCurrency(RepairUpgradeCost);
        public string StatusSummary => "State=" + RuntimeState +
            " Spawned=" + SpawnedCount +
            " Kills=" + (DirectOrCombatKillCount + ProjectileAdapterKillCount) +
            " Projectiles=" + ProjectileLaunchCount +
            " Upgrades=" + SelectedUpgradeCount +
            " Modules=" + UnlockedModuleCount +
            " ObjectiveHits=" + ObjectiveDamageEvents +
            " Currency=" + RuntimeCurrency +
            " Time=" + SurvivalSeconds.ToString("0.0", CultureInfo.InvariantCulture);

        private AutoDefenseRuntimeState RuntimeState => _runtime == null ? AutoDefenseRuntimeState.Created : _runtime.State;

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

            _runtimePanelSettings ??= CreateRuntimePanelSettings();
            _runtimeUiDocument = _runtimeUiObject.GetComponent<UIDocument>();
            if (_runtimeUiDocument == null)
                _runtimeUiDocument = _runtimeUiObject.AddComponent<UIDocument>();
            _runtimeUiDocument.panelSettings = _runtimePanelSettings;

            _runtimeUiRoot = _runtimeUiDocument.rootVisualElement;
            _runtimeUiRoot.name = "idle-auto-defense-ui-root";
            _runtimeUiRoot.style.position = Position.Absolute;
            _runtimeUiRoot.style.left = 0;
            _runtimeUiRoot.style.right = 0;
            _runtimeUiRoot.style.top = 0;
            _runtimeUiRoot.style.bottom = 0;

            _damageNumberLayer = _runtimeUiRoot.Q<VisualElement>("damage-number-layer");
            if (_damageNumberLayer == null)
            {
                _damageNumberLayer = new VisualElement { name = "damage-number-layer", pickingMode = PickingMode.Ignore };
                _damageNumberLayer.style.position = Position.Absolute;
                _damageNumberLayer.style.left = 0;
                _damageNumberLayer.style.right = 0;
                _damageNumberLayer.style.top = 0;
                _damageNumberLayer.style.bottom = 0;
                _runtimeUiRoot.Add(_damageNumberLayer);
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

        private static PanelSettings CreateRuntimePanelSettings()
        {
            PanelSettings settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.name = "Basic Idle Auto Defense Runtime Panel Settings";
            settings.scaleMode = PanelScaleMode.ConstantPixelSize;
            settings.scale = 1f;
            settings.sortingOrder = 100;
            settings.hideFlags = HideFlags.HideAndDontSave;
            return settings;
        }

        protected void ConfigureContentPack(GameContentPackAsset contentPack, GameContentSetAsset contentSet)
        {
            _contentPack = contentPack;
            _contentSet = contentSet;
        }

        private void Update()
        {
            Step(1, Time.deltaTime <= 0f ? 1f / 60f : Time.deltaTime);
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
                _resolvedAttackRecipes = ResolveAttackRecipes();
                _resolvedEnemyDefinitions = ResolveEnemyDefinitions();
                _resolvedWaveDefinitions = ResolveWaveDefinitions(_resolvedEnemyDefinitions);
                _resolvedWeaponDefinitions = ResolveWeaponDefinitions(_resolvedAttackRecipes);
                _resolvedUpgradeDefinitions = ResolveUpgradeDefinitions();
            }

            AutoDefenseDefinition definition = BasicIdleAutoDefenseGame.CreateDefinition(_resolvedEnemyDefinitions, ResolveActiveWeaponDefinitionsForRun());
            CombatCatalog catalog = BasicIdleAutoDefenseGame.CreateCombatCatalog(_resolvedAttackRecipes, _resolvedEnemyDefinitions);
            AttackRuntime attacks = BasicIdleAutoDefenseGame.CreateAttackRuntime(catalog, definition, _resolvedAttackRecipes);
            WeaponRuntime weapons = BasicIdleAutoDefenseGame.CreateWeaponRuntime(definition, attacks);

            _root = new GameObject("Basic Idle Auto Defense Runtime");
            _runtimeAudioSource = _root.AddComponent<AudioSource>();
            _runtimeAudioSource.playOnAwake = false;
            _runtimeAudioSource.spatialBlend = 0f;
            _runtimeAudioSource.volume = 0.75f;
            EnsureRuntimeUiDocument();
            CreatePrimitive("Player Tower", PrimitiveType.Cylinder, definition.Objective.Position, new Vector3(0.8f, 0.9f, 0.8f), Color.cyan);
            CreatePlayAreaMarkers();

            _enemyPrefab = CreatePrefab("Template Idle Enemy Runtime Prefab", PrimitiveType.Capsule, Color.red);
            _projectilePrefab = CreatePrefab("Template Idle Projectile Runtime Prefab", PrimitiveType.Sphere, Color.magenta);

            var poseResolver = new AutoDefensePerimeterPoseResolver(definition.Objective, definition.SpawnRing);
            _enemySpawning = new WorldSpawnService(
                new SpawnableCatalog(CreateEnemySpawnables(_resolvedEnemyDefinitions)),
                poseResolver,
                rootName: "TemplateIdleEnemies");
            _navigation = new WorldNavigationService();
            _encounter = new EncounterRuntime(encounterDefinition ?? BasicIdleAutoDefenseGame.CreateEncounterDefinition(_resolvedWaveDefinitions));
            _runtime = new AutoDefenseRuntime(definition, _enemySpawning, _navigation, weapons, catalog, _encounter, poses: poseResolver, candidateCapacity: 64);

            var projectilePoseResolver = new ChannelPoseResolver(new Dictionary<WorldSpawnChannelId, SpawnPose>
            {
                { new WorldSpawnChannelId("projectile-origin"), new SpawnPose(CreateTowerMuzzlePosition(definition.Objective.Position), Quaternion.identity) }
            });
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
            _progressionCatalog ??= BasicIdleAutoDefenseGame.CreateProgressionCatalog();
            bool createdProgressionState = _progressionState == null;
            _progressionState ??= new ProgressionState();
            if (createdProgressionState && UsingAssignedContentSet)
                ApplyContentSetStartingResources(_resolvedContentSet);
            _offlineDefinition = BasicIdleAutoDefenseGame.CreateOfflineProgressionDefinition();
            ApplyContentSetEconomyTuning(_resolvedContentSet);
            RuntimeCurrency = ResolveRuntimeStartingCredits(_resolvedContentSet);

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
            SurvivalSeconds += Math.Max(0f, deltaSeconds);
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
            AutoDefenseRunResult result = _runtime.Tick(ticks, deltaSeconds);
            DirectOrCombatKillCount += result.Killed;
            int rewardedKills = result.Killed;
            ObjectiveReachCount += result.ReachedObjective;
            if (result.ReachedObjective > 0)
            {
                ObjectiveDamageEvents += result.ReachedObjective;
                EmitDamageNumber(CreateTowerMuzzlePosition(Vector3.zero), result.ReachedObjective, new Color(1f, 0.25f, 0.18f), "-");
            }
            AutoDefenseRuntimeSnapshot afterCombat = _runtime.CreateSnapshot();
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
            ApplyEncounterRewardIfTerminal();
            UpdateDamageNumbers(deltaSeconds);
        }

        public bool TryPurchaseDamageUpgrade()
        {
            if (!SpendRuntimeCurrency(DamageUpgradeCost)) return false;
            DamageUpgradeRank++;
            DirectDamageBonus += ManualTowerDamageRankBonus;
            SelectedUpgradeCount++;
            return true;
        }

        public bool TryPurchaseAttackSpeedUpgrade()
        {
            if (!SpendRuntimeCurrency(AttackSpeedUpgradeCost)) return false;
            AttackSpeedUpgradeRank++;
            SelectedUpgradeCount++;
            return true;
        }

        public bool TryPurchaseRangeUpgrade()
        {
            if (!SpendRuntimeCurrency(RangeUpgradeCost)) return false;
            RangeUpgradeRank++;
            DirectDamageBonus += 0.5d;
            SelectedUpgradeCount++;
            return true;
        }

        public bool TryPurchaseRepairUpgrade()
        {
            if (!SpendRuntimeCurrency(RepairUpgradeCost)) return false;
            RepairUpgradeRank++;
            if (_runtime != null)
            {
                _runtime.Objective.Health.ChangeMaximumHealth(_runtime.Objective.Health.MaximumHealth + 6d, MaximumChangePolicy.PreserveAbsolute);
                _runtime.Objective.Health.Heal(12d + RepairUpgradeRank * 3d);
            }

            SelectedUpgradeCount++;
            return true;
        }

        public bool TryPurchasePulseBeamModule()
        {
            if (!SpendRuntimeCurrency(PulseBeamUnlockCost)) return false;
            PulseBeamUnlocked = true;
            SelectedUpgradeCount++;
            CreateModuleAttachment("Pulse Beam Module", PrimitiveType.Cube, new Vector3(-0.9f, 0.35f, 0.35f), new Vector3(0.38f, 0.28f, 0.38f), new Color(0.15f, 0.75f, 1f));
            return true;
        }

        public bool TryPurchaseArcBurstModule()
        {
            if (!SpendRuntimeCurrency(ArcBurstUnlockCost)) return false;
            ArcBurstUnlocked = true;
            SelectedUpgradeCount++;
            CreateModuleAttachment("Arc Burst Module", PrimitiveType.Sphere, new Vector3(0f, 0.48f, -0.9f), new Vector3(0.38f, 0.38f, 0.38f), new Color(0.95f, 0.55f, 0.15f));
            return true;
        }

        public bool TryPurchaseHomingPulseModule()
        {
            if (!SpendRuntimeCurrency(HomingPulseUnlockCost)) return false;
            HomingPulseUnlocked = true;
            SelectedUpgradeCount++;
            CreateModuleAttachment("Homing Pulse Module", PrimitiveType.Sphere, new Vector3(0.9f, 0.35f, 0.35f), new Vector3(0.34f, 0.34f, 0.34f), new Color(0.65f, 0.35f, 1f));
            return true;
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
                else if (effect.EffectId.Value == "template.weapon.fire_rate_intent") AttackSpeedUpgradeRank++;
                else if (effect.EffectId.Value == "template.weapon.range_intent")
                {
                    RangeUpgradeRank++;
                    DirectDamageBonus += Math.Max(0.5d, effect.Amount * 0.5d);
                }
                else if (effect.EffectId.Value == "template.enemy.spawn_delay_ticks") EnemySpawnDelayTicks += (int)effect.Amount;
                else if (effect.EffectId.Value == "template.reward.credits_multiplier") RewardCreditMultiplierBonus += effect.Amount;
                else if (effect.EffectId.Value == "template.offline.credits_multiplier") OfflineRewardMultiplierBonus += effect.Amount;
                else UnsupportedUpgradeIntentCount++;
            }
        }

        private int ApplyDirectDamageBonusIfReady()
        {
            if (DirectDamageBonus <= 0d) return 0;
            AutoDefenseRuntimeSnapshot snapshot = _runtime.CreateSnapshot();
            AttackDefinitionAsset attack = FindAttackRecipeForPresentation(BasicIdleAutoDefenseGame.PulseAttackId.Value);
            for (int i = 0; i < snapshot.Enemies.Count; i++)
            {
                AutoDefenseEnemySnapshot enemy = snapshot.Enemies[i];
                if (enemy.Lifecycle != AutoDefenseEnemyLifecycle.Active) continue;
                if (enemy.Health <= DirectDamageBonus && TryKillEnemyWithPresentation(enemy, attack, DirectDamageBonus))
                {
                    DirectOrCombatKillCount++;
                    return 1;
                }
            }

            return 0;
        }

        private int FireManualTowerShotIfReady(int ticks)
        {
            _manualTowerCooldownTicks += Math.Max(1, ticks);
            int cooldownTicks = Math.Max(ManualTowerMinimumCooldownTicks, ManualTowerBaseCooldownTicks - AttackSpeedUpgradeRank * 3);
            if (_manualTowerCooldownTicks < cooldownTicks) return 0;
            _manualTowerCooldownTicks = 0;

            double damageThreshold = ManualTowerBaseDamage + DamageUpgradeRank * ManualTowerDamageRankBonus + RangeUpgradeRank * ManualTowerRangeRankBonus;
            AutoDefenseRuntimeSnapshot snapshot = _runtime.CreateSnapshot();
            AutoDefenseEnemySnapshot selected = default;
            bool hasSelected = false;
            for (int i = 0; i < snapshot.Enemies.Count; i++)
            {
                AutoDefenseEnemySnapshot enemy = snapshot.Enemies[i];
                if (enemy.Lifecycle != AutoDefenseEnemyLifecycle.Active) continue;
                if (enemy.Health > damageThreshold) continue;
                if (hasSelected && enemy.ObjectiveProgress <= selected.ObjectiveProgress) continue;
                selected = enemy;
                hasSelected = true;
            }

            AttackDefinitionAsset attack = FindAttackRecipeForPresentation(BasicIdleAutoDefenseGame.ShardAttackId.Value);
            if (!hasSelected) return 0;
            if (TryLaunchVisibleProjectileAtEnemy(selected, attack, damageThreshold)) return 0;
            if (!TryKillEnemyWithPresentation(selected, attack, damageThreshold)) return 0;
            DirectOrCombatKillCount++;
            return 1;
        }

        private int FireUnlockedModulesIfReady(int ticks)
        {
            int kills = 0;
            if (PulseBeamUnlocked)
            {
                _pulseBeamModuleCooldownTicks += Math.Max(1, ticks);
                if (_pulseBeamModuleCooldownTicks >= Math.Max(18, PulseBeamModuleCooldownTicks - AttackSpeedUpgradeRank * 2))
                {
                    _pulseBeamModuleCooldownTicks = 0;
                    ModuleActivationCount++;
                    kills += TryKillPriorityEnemies(12d + DamageUpgradeRank * 2d + RangeUpgradeRank, 1, BasicIdleAutoDefenseGame.PulseAttackId.Value);
                }
            }

            if (ArcBurstUnlocked)
            {
                _arcBurstModuleCooldownTicks += Math.Max(1, ticks);
                if (_arcBurstModuleCooldownTicks >= Math.Max(28, ArcBurstModuleCooldownTicks - AttackSpeedUpgradeRank * 3))
                {
                    _arcBurstModuleCooldownTicks = 0;
                    ModuleActivationCount++;
                    kills += TryKillPriorityEnemies(16d + DamageUpgradeRank * 2.5d, 2, BasicIdleAutoDefenseGame.ArcBurstAttackId.Value);
                }
            }

            if (HomingPulseUnlocked)
            {
                _homingPulseModuleCooldownTicks += Math.Max(1, ticks);
                if (_homingPulseModuleCooldownTicks >= Math.Max(20, HomingPulseModuleCooldownTicks - AttackSpeedUpgradeRank * 2))
                {
                    _homingPulseModuleCooldownTicks = 0;
                    ModuleActivationCount++;
                    kills += TryKillPriorityEnemies(18d + DamageUpgradeRank * 2d + RangeUpgradeRank, 1, BasicIdleAutoDefenseGame.HomingPulseAttackId.Value, preferProjectileVisual: true);
                }
            }

            return kills;
        }

        private int TryKillPriorityEnemies(double damageThreshold, int maxKills, string attackId, bool preferProjectileVisual = false)
        {
            if (_runtime == null || maxKills <= 0) return 0;
            int kills = 0;
            AttackDefinitionAsset attack = FindAttackRecipeForPresentation(attackId);
            for (int attempt = 0; attempt < maxKills; attempt++)
            {
                AutoDefenseRuntimeSnapshot snapshot = _runtime.CreateSnapshot();
                AutoDefenseEnemySnapshot selected = default;
                bool hasSelected = false;
                for (int i = 0; i < snapshot.Enemies.Count; i++)
                {
                    AutoDefenseEnemySnapshot enemy = snapshot.Enemies[i];
                    if (enemy.Lifecycle != AutoDefenseEnemyLifecycle.Active) continue;
                    if (enemy.Health > damageThreshold) continue;
                    if (hasSelected && enemy.ObjectiveProgress <= selected.ObjectiveProgress) continue;
                    selected = enemy;
                    hasSelected = true;
                }

                if (!hasSelected) break;
                if (preferProjectileVisual && TryLaunchVisibleProjectileAtEnemy(selected, attack, damageThreshold)) continue;
                if (!TryKillEnemyWithPresentation(selected, attack, damageThreshold)) break;
                DirectOrCombatKillCount++;
                kills++;
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

            Vector3 origin = CreateTowerMuzzlePosition(Vector3.zero);
            Vector3 destination = target.Id > 0
                ? CreateEnemyAimPosition(target.Position)
                : (original.Destination == Vector3.zero ? origin + Vector3.forward * 4f : original.Destination);
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
                if (pending.ProjectileId.Value > 0)
                    _projectiles?.Cleanup(pending.ProjectileId, ProjectileExpiryReason.HitLimitReached);

                bool hasActiveTarget = TryFindActiveEnemy(pending.TargetEnemyId, out AutoDefenseEnemySnapshot activeTarget);
                Vector3 impactPosition = hasActiveTarget
                    ? CreateEnemyAimPosition(activeTarget.Position)
                    : pending.Destination;
                EmitAttackEvent(pending.Attack, AttackPresentationEventKind.OnImpact, impactPosition);

                if (!hasActiveTarget) continue;
                EmitEnemyPresentationEvent(activeTarget, EnemyPresentationEventKind.OnHit);
                EmitDamageNumber(impactPosition, pending.DamageThreshold, ResolveAttackColor(pending.Attack), "-");
                if (activeTarget.Health > pending.DamageThreshold) continue;
                if (!TryKillEnemyAfterFeedback(activeTarget)) continue;
                ProjectileAdapterKillCount++;
                kills++;
            }

            return kills;
        }

        private void EmitProjectileExpiryFeedback(ProjectileTickResult result)
        {
            if (result == null || result.Expiries.Count == 0) return;
            for (int i = 0; i < result.Expiries.Count; i++)
            {
                ProjectileExpiryEvent expiry = result.Expiries[i];
                int pendingIndex = FindPendingProjectileImpactIndex(expiry.ProjectileId);
                if (pendingIndex < 0) continue;
                PendingProjectileImpact pending = _pendingProjectileImpacts[pendingIndex];
                _pendingProjectileImpacts.RemoveAt(pendingIndex);
                EmitAttackEvent(pending.Attack, AttackPresentationEventKind.OnExpire, pending.Destination);
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

                EmitAttackEvent(attack, AttackPresentationEventKind.OnCast, CreateTowerMuzzlePosition(Vector3.zero));
                EmitAttackEvent(attack, AttackPresentationEventKind.OnFire, CreateTowerMuzzlePosition(Vector3.zero));
                if (hadBefore || hasAfter)
                    EmitAttackTracer(CreateTowerMuzzlePosition(Vector3.zero), targetPosition, ResolveAttackColor(attack));
                EmitAttackEvent(attack, AttackPresentationEventKind.OnImpact, targetPosition);
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
                    EmitAttackTracer(CreateTowerMuzzlePosition(Vector3.zero), CreateEnemyAimPosition(enemy.Position), ResolveAttackColor(attack));
                    EmitAttackEvent(attack, AttackPresentationEventKind.OnImpact, CreateEnemyAimPosition(enemy.Position));
                }

                EmitDamageNumber(CreateEnemyAimPosition(enemy.Position), Math.Max(ResolveAttackDamage(attack), enemy.Health), ResolveAttackColor(attack), "-");
                EmitEnemyDeathFeedback(enemy);
                emitted++;
                if (emitted >= maxKills) return;
            }
        }

        private bool TryKillEnemyWithPresentation(AutoDefenseEnemySnapshot enemy, AttackDefinitionAsset attack, double damageAmount)
        {
            Vector3 origin = CreateTowerMuzzlePosition(Vector3.zero);
            Vector3 destination = CreateEnemyAimPosition(enemy.Position);
            EmitAttackEvent(attack, AttackPresentationEventKind.OnCast, origin);
            EmitAttackEvent(attack, AttackPresentationEventKind.OnFire, origin);
            EmitAttackTracer(origin, destination, ResolveAttackColor(attack));
            EmitAttackEvent(attack, AttackPresentationEventKind.OnImpact, destination);
            EmitDamageNumber(destination, damageAmount, ResolveAttackColor(attack), "-");
            EmitEnemyPresentationEvent(enemy, EnemyPresentationEventKind.OnHit);
            return TryKillEnemyAfterFeedback(enemy);
        }

        private bool TryLaunchVisibleProjectileAtEnemy(AutoDefenseEnemySnapshot enemy, AttackDefinitionAsset attack, double damageThreshold)
        {
            if (_projectiles == null || attack == null || attack.Delivery == null) return false;
            if (attack.Delivery.Mode != AttackRecipeDeliveryMode.Projectile) return false;
            if (string.IsNullOrWhiteSpace(attack.Delivery.ProjectileDefinitionId)) return false;
            ProjectileDefinition projectile = FindProjectileDefinition(new ProjectileDefinitionId(attack.Delivery.ProjectileDefinitionId));
            if (projectile == null) return false;

            Vector3 origin = CreateTowerMuzzlePosition(Vector3.zero);
            Vector3 destination = CreateEnemyAimPosition(enemy.Position);
            int impactDelayTicks = CalculateProjectileImpactDelayTicks(origin, destination, projectile.Speed);
            var launchRequest = new ProjectileLaunchRequest(
                projectile.Id,
                new AttackSourceId("source.template.visual." + BasicIdleAutoDefenseGame.SanitizeContentSetOperationSegment(attack.Id)),
                new AttackDefinitionId(attack.Id),
                new AttackSourceSnapshot(new AttackSourceId("source.template.visual." + BasicIdleAutoDefenseGame.SanitizeContentSetOperationSegment(attack.Id)), new CombatantId("template-core")),
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
                damageThreshold,
                impactDelayTicks));
            return true;
        }

        private bool TryKillEnemyAfterFeedback(AutoDefenseEnemySnapshot enemy)
        {
            if (_runtime == null || enemy.Id <= 0 || !_runtime.TryKillEnemy(enemy.Id)) return false;
            EmitEnemyDeathFeedback(enemy);
            return true;
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
                EmitEnemyPresentationEvent(enemy, EnemyPresentationEventKind.OnSpawn);
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
            if (definition != null &&
                definition.Presentation != null &&
                definition.Presentation.TryGetEvent(eventKind, out EnemyPresentationEventRecipe recipe))
            {
                Vector3 position = CreateEnemyAimPosition(enemy.Position);
                emittedVfx = EmitPresentationVfx(recipe.VfxPrefab, position);
                emittedAudio = PlayPresentationAudio(recipe.AudioClip);
            }

            if (!emittedVfx)
                EmitFallbackPresentationVfx(CreateEnemyAimPosition(enemy.Position), ResolveEnemyEventColor(eventKind), 0.34f);
            if (!emittedAudio)
                PlayPresentationAudio(null);
        }

        private void EmitAttackEvent(AttackDefinitionAsset attack, AttackPresentationEventKind eventKind, Vector3 eventPosition)
        {
            bool emittedVfx = false;
            bool emittedAudio = false;
            if (attack != null &&
                attack.Presentation != null &&
                attack.Presentation.TryGetEvent(eventKind, out AttackPresentationEventRecipe recipe))
            {
                Vector3 position = ResolveAttackEventPosition(recipe, eventPosition);
                emittedVfx = EmitPresentationVfx(recipe.VfxPrefab, position);
                emittedAudio = PlayPresentationAudio(recipe.AudioClip);
            }

            if (!emittedVfx)
                EmitFallbackPresentationVfx(eventPosition, ResolveAttackColor(attack), ResolveAttackEventScale(eventKind));
            if (!emittedAudio && eventKind != AttackPresentationEventKind.OnTick)
                PlayPresentationAudio(null);
        }

        private bool EmitPresentationVfx(GameObject prefab, Vector3 position)
        {
            if (prefab == null) return false;
            GameObject instance = Instantiate(prefab, position, Quaternion.identity);
            instance.name = prefab.name + " Runtime";
            if (_root != null) instance.transform.SetParent(_root.transform, true);
            instance.SetActive(true);
            DisableColliders(instance);
            ParticleSystem[] particles = instance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].gameObject.SetActive(true);
                particles[i].Play(true);
            }

            AttackVfxSpawnCount++;
            DestroyPresentationObject(instance, 2f);
            return true;
        }

        private void EmitFallbackPresentationVfx(Vector3 position, Color color, float scale)
        {
            if (_root == null) return;
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
            if (_root == null) return;
            GameObject tracer = new GameObject("Template Attack Tracer");
            tracer.transform.SetParent(_root.transform, false);
            LineRenderer line = tracer.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPosition(0, origin);
            line.SetPosition(1, destination);
            line.startWidth = 0.07f;
            line.endWidth = 0.02f;
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            if (shader != null)
                line.material = new Material(shader) { color = color };
            line.startColor = color;
            line.endColor = new Color(color.r, color.g, color.b, 0.2f);
            AttackVfxSpawnCount++;
            DestroyPresentationObject(tracer, 0.2f);
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
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.fontSize = 24;
            label.style.color = color;
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
            number.Label.style.left = point.x - 45f;
            number.Label.style.top = point.y - 56f - elapsedSeconds * 48f;
        }

        private static Vector2 WorldToRuntimePanelPoint(Vector3 worldPosition)
        {
            Camera camera = Camera.main;
            if (camera == null)
                camera = FindFirstObjectByType<Camera>();
            if (camera == null)
                return new Vector2(Screen.width * 0.5f, Screen.height * 0.45f);

            Vector3 screen = camera.WorldToScreenPoint(worldPosition);
            if (screen.z < 0f)
                return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            return new Vector2(screen.x, Screen.height - screen.y);
        }

        private AudioClip GetFallbackPresentationClip()
        {
            if (_fallbackPresentationClip != null) return _fallbackPresentationClip;
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

        private AttackDefinitionAsset FindAttackRecipeForPresentation(string attackId)
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
            if (attack != null && attack.Delivery != null && attack.Delivery.ProjectilePrefab != null)
                return attack.Delivery.ProjectilePrefab;
            return _projectilePrefab;
        }

        private static int CalculateProjectileImpactDelayTicks(Vector3 origin, Vector3 destination, float speed)
        {
            float safeSpeed = Mathf.Max(0.5f, speed);
            int ticks = Mathf.CeilToInt(Vector3.Distance(origin, destination) / safeSpeed / 0.05f);
            return Mathf.Clamp(ticks, MinimumProjectileImpactDelayTicks, MaximumProjectileImpactDelayTicks);
        }

        private static float ResolveProjectileSpeed(AttackDefinitionAsset attack)
        {
            return attack != null && attack.Delivery != null
                ? Mathf.Max(0.5f, attack.Delivery.ProjectileSpeed)
                : 8f;
        }

        private static double ResolveAttackDamage(AttackDefinitionAsset attack)
        {
            return attack != null && attack.Mechanics != null
                ? Math.Max(SampleProjectileFinishThreshold, attack.Mechanics.DamageAmount)
                : SampleProjectileFinishThreshold;
        }

        private static Vector3 CreateTowerMuzzlePosition(Vector3 objectivePosition)
        {
            return objectivePosition + Vector3.up * 0.75f;
        }

        private static Vector3 CreateEnemyAimPosition(Vector3 enemyPosition)
        {
            return enemyPosition + Vector3.up * 0.35f;
        }

        private static Vector3 ResolveAttackEventPosition(AttackPresentationEventRecipe recipe, Vector3 requestedPosition)
        {
            if (recipe == null) return requestedPosition;
            if (recipe.SpawnPointRole == AttackPresentationSpawnPointRole.Caster ||
                recipe.SpawnPointRole == AttackPresentationSpawnPointRole.Muzzle)
            {
                return CreateTowerMuzzlePosition(Vector3.zero);
            }

            return requestedPosition;
        }

        private static Color ResolveAttackColor(AttackDefinitionAsset attack)
        {
            if (attack == null) return Color.white;
            if (string.Equals(attack.Id, BasicIdleAutoDefenseGame.ShardAttackId.Value, StringComparison.OrdinalIgnoreCase)) return new Color(1f, 0.45f, 0.1f);
            if (string.Equals(attack.Id, BasicIdleAutoDefenseGame.PulseAttackId.Value, StringComparison.OrdinalIgnoreCase)) return new Color(0.15f, 0.8f, 1f);
            if (string.Equals(attack.Id, BasicIdleAutoDefenseGame.ArcBurstAttackId.Value, StringComparison.OrdinalIgnoreCase)) return new Color(1f, 0.65f, 0.12f);
            if (string.Equals(attack.Id, BasicIdleAutoDefenseGame.HomingPulseAttackId.Value, StringComparison.OrdinalIgnoreCase)) return new Color(0.68f, 0.38f, 1f);
            return Color.white;
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

        private static void DestroyPresentationObject(GameObject instance, float delaySeconds)
        {
            if (instance == null) return;
            if (Application.isPlaying) Destroy(instance, delaySeconds);
            else DestroyImmediate(instance);
        }

        private void AwardRuntimeCurrencyForKills(int kills)
        {
            if (kills <= 0) return;
            RuntimeCurrency += Math.Max(kills, (long)Math.Ceiling(kills * KillRewardCredits * (1d + RewardCreditMultiplierBonus)));
        }

        private void GrantPassiveIncomeIfReady(int ticks)
        {
            _passiveIncomeTicks += Math.Max(1, ticks);
            if (_passiveIncomeTicks < PassiveIncomeIntervalTicks) return;
            int intervals = _passiveIncomeTicks / PassiveIncomeIntervalTicks;
            _passiveIncomeTicks %= PassiveIncomeIntervalTicks;
            RuntimeCurrency += intervals;
        }

        private bool SpendRuntimeCurrency(int cost)
        {
            if (!CanSpendRuntimeCurrency(cost)) return false;
            RuntimeCurrency -= cost;
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

        private static long ResolveRuntimeStartingCredits(GameContentSetResolution resolution)
        {
            if (resolution != null && resolution.IsValid && resolution.ContentSet != null && resolution.ContentSet.StartingCredits > 0)
                return resolution.ContentSet.StartingCredits;
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

        private bool TryUseAssignedContentSet()
        {
            UsingAssignedContentPack = false;
            UsingAssignedContentSet = false;
            InvalidAssignedContentPackIssueCount = 0;
            InvalidAssignedContentSetIssueCount = 0;
            _resolvedContentSet = null;

            if (_contentPack != null)
            {
                GameContentPackResolution packResolution = GameContentPackValidator.Resolve(_contentPack, _contentSet);
                InvalidAssignedContentPackIssueCount = packResolution.PackReport.ErrorCount;
                if (packResolution.ContentSetResolution != null)
                    InvalidAssignedContentSetIssueCount = packResolution.ContentSetResolution.Report.ErrorCount;

                if (!packResolution.IsValid)
                {
                    AssignedContentPackStatus = "Assigned content pack is invalid; using direct assigned assets or built-in starter content.";
                    AssignedContentSetStatus = "Content pack could not resolve a playable content set.";
                    Debug.LogWarning(
                        "[Idle Auto Defense Template] Assigned GameContentPackAsset '" + _contentPack.name + "' is incomplete or invalid. Falling back safely. " + CreateContentPackIssueSummary(packResolution),
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
                AssignedContentSetStatus = "No content set assigned; using direct assigned assets or built-in starter content.";
                return false;
            }

            GameContentSetResolution resolution = BasicIdleAutoDefenseGame.ResolveGameContentSetForTemplate(_contentSet);
            _resolvedContentSet = resolution;
            InvalidAssignedContentSetIssueCount = resolution.Report.ErrorCount;
            if (!resolution.IsValid)
            {
                AssignedContentSetStatus = "Assigned content set is invalid; using direct assigned assets or built-in starter content.";
                Debug.LogWarning(
                    "[Idle Auto Defense Template] Assigned GameContentSetAsset '" + _contentSet.name + "' is incomplete or invalid. Falling back safely. " + CreateContentSetIssueSummary(resolution.Report),
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
            UsingAssignedContentSet = true;
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
            if (resolution.ContentSet.StartingCredits > 0)
                currencies.Add(new CurrencyLine(BasicIdleAutoDefenseGame.Credits, new ProgressionAmount(resolution.ContentSet.StartingCredits), true));
            if (resolution.ContentSet.StartingParts > 0)
                currencies.Add(new CurrencyLine(BasicIdleAutoDefenseGame.Parts, new ProgressionAmount(resolution.ContentSet.StartingParts), true));
            if (currencies.Count == 0) return;

            _progressionState.ApplyReward(
                _progressionCatalog,
                new ProgressionOperationId("template.content-set.starting-resources." + BasicIdleAutoDefenseGame.SanitizeContentSetOperationSegment(resolution.ContentSet.Id)),
                new RewardBundle(currencies));
        }

        private void ApplyContentSetEconomyTuning(GameContentSetResolution resolution)
        {
            if (resolution == null || !resolution.IsValid) return;
            RewardCreditMultiplierBonus = Math.Max(0d, resolution.ContentSet.RewardMultiplier - 1f);
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
            if (enemy != null && enemy.Presentation != null && enemy.Presentation.Prefab != null)
                return enemy.Presentation.Prefab;
            return _enemyPrefab;
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

        private void CreateModuleAttachment(string name, PrimitiveType primitiveType, Vector3 position, Vector3 scale, Color color)
        {
            if (_root == null) return;
            CreatePrimitive(name, primitiveType, position, scale, color);
        }

        private void CreatePlayAreaMarkers()
        {
            CreatePrimitive("Shard Launcher Module", PrimitiveType.Cube, new Vector3(0f, 0.35f, 0.9f), new Vector3(0.45f, 0.28f, 0.45f), new Color(1f, 0.45f, 0.1f));
            CreatePrimitive("Spawn Lane North", PrimitiveType.Cube, new Vector3(0f, 0.05f, TemplateSpawnLaneRadius), new Vector3(1.2f, 0.08f, 0.35f), Color.yellow);
            CreatePrimitive("Spawn Lane East", PrimitiveType.Cube, new Vector3(TemplateSpawnLaneRadius, 0.05f, 0f), new Vector3(0.35f, 0.08f, 1.2f), Color.yellow);
            CreatePrimitive("Spawn Lane South", PrimitiveType.Cube, new Vector3(0f, 0.05f, -TemplateSpawnLaneRadius), new Vector3(1.2f, 0.08f, 0.35f), Color.yellow);
            CreatePrimitive("Spawn Lane West", PrimitiveType.Cube, new Vector3(-TemplateSpawnLaneRadius, 0.05f, 0f), new Vector3(0.35f, 0.08f, 1.2f), Color.yellow);
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

        private void OnDestroy()
        {
            DisposeRuntimeObjects(!Application.isPlaying);
        }

        private void OnApplicationQuit()
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
            AttackVfxSpawnCount = 0;
            AttackAudioPlayCount = 0;
            EnemyPresentationEventCount = 0;
            DamageNumberSpawnCount = 0;
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
            DirectDamageBonus = 0d;
            ProjectileSpeedMultiplier = 1d;
            EnemySpawnDelayTicks = 0;
            RewardCreditMultiplierBonus = 0d;
            OfflineRewardMultiplierBonus = 0d;
            RuntimeCurrency = 0;
            SurvivalSeconds = 0f;
            DamageUpgradeRank = 0;
            AttackSpeedUpgradeRank = 0;
            RangeUpgradeRank = 0;
            RepairUpgradeRank = 0;
            PulseBeamUnlocked = false;
            ArcBurstUnlocked = false;
            HomingPulseUnlocked = false;
            ModuleActivationCount = 0;
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
            _lastProjectileAgentPositions.Clear();
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
                DestroyTemplateObject(_enemyPrefab);
                DestroyTemplateObject(_projectilePrefab);
                DestroyTemplateObject(_fallbackPresentationClip);
                DestroyTemplateObject(_runtimePanelSettings);
                DestroyTemplateObject(_runtimeUiObject);
                DestroyTemplateObject(_root);
            }
            else
            {
                ClearSpawnedRuntimeObjects();
            }

            _enemySpawning = null;
            _projectileSpawning = null;
            _enemyPrefab = null;
            _projectilePrefab = null;
            _root = null;
            _runtimeAudioSource = null;
            _fallbackPresentationClip = null;
            _runtimePanelSettings = null;
            _runtimeUiDocument = null;
            _runtimeUiObject = null;
            _runtimeUiRoot = null;
            _damageNumberLayer = null;
            _resolvedProjectileDefinitions = Array.Empty<ProjectileDefinition>();
            _pendingProjectileImpacts.Clear();
            _seenEnemyIds.Clear();
            _enemyDeathPresentationIds.Clear();
            _lastProjectileAgentPositions.Clear();
            ClearDamageNumbers();
        }

        private void ClearDamageNumbers()
        {
            for (int i = 0; i < _damageNumbers.Count; i++)
                _damageNumbers[i].Label?.RemoveFromHierarchy();
            _damageNumbers.Clear();
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
