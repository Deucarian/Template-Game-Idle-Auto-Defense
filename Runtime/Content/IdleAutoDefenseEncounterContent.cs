using System;
using System.Collections.Generic;
using Deucarian.Attacks.Authoring;
using Deucarian.AutoDefense;
using Deucarian.DefenseGames;
using Deucarian.Encounters;
using Deucarian.WorldSpawning;
using static Deucarian.TemplateGameIdleAutoDefense.BasicIdleAutoDefenseGame;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    // Owns starter stage schedules and stable authored wave-to-spawn-group conversion.
    internal static class IdleAutoDefenseEncounterContent
    {
        internal static AutoDefenseSpawnRingDefinition CreateSampleSpawnRing()
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

        internal static EncounterDefinition CreateEncounterDefinition(IReadOnlyList<WaveDefinitionAsset> waveDefinitions = null, int seed = 20260623)
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

        internal static StageDefinition[] CreateStageDefinitions()
        {
            return new[]
            {
                new StageDefinition(new StageId("stage.idle-auto-defense.first-orbit"), new EncounterId("encounter.idle-auto-defense.first-orbit"), new[] { new RewardReferenceId("reward.idle-auto-defense.first-orbit") }),
                new StageDefinition(new StageId("stage.idle-auto-defense.pressure-ring"), new EncounterId("encounter.idle-auto-defense.pressure-ring"), new[] { new RewardReferenceId("reward.idle-auto-defense.pressure-ring") }),
                new StageDefinition(new StageId("stage.idle-auto-defense.boss-pulse"), new EncounterId("encounter.idle-auto-defense.boss-pulse"), new[] { new RewardReferenceId("reward.idle-auto-defense.boss-pulse") }),
                new StageDefinition(new StageId("stage.idle-auto-defense.endless-placeholder"), new EncounterId("encounter.idle-auto-defense.endless-placeholder"), new[] { new RewardReferenceId("reward.idle-auto-defense.endless-placeholder") })
            };
        }

        internal static EncounterDefinition[] CreateEncounterDefinitions()
        {
            return new[]
            {
                CreateFirstOrbitEncounterDefinition(),
                CreatePressureRingEncounterDefinition(),
                CreateBossPulseEncounterDefinition(),
                CreateEndlessPlaceholderEncounterDefinition()
            };
        }

        internal static EncounterDefinition CreateFirstOrbitEncounterDefinition()
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

        internal static EncounterDefinition CreatePressureRingEncounterDefinition()
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

        internal static EncounterDefinition CreateBossPulseEncounterDefinition()
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

        internal static EncounterDefinition CreateEndlessPlaceholderEncounterDefinition()
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

        internal static WaveDefinitionAsset[] CreateWaveDefinitions()
        {
            EnemyDefinitionAsset[] enemies = IdleAutoDefenseEnemyContent.CreateEnemyDefinitions();
            return new[]
            {
                WaveDefinitionAsset.CreateTransient(
                    "wave.idle-auto-defense.opening",
                    "Opening Wave",
                    0,
                    new[]
                    {
                        new WaveEntryRecipe("0", enemies[0], 7, 1, 0, 66, "perimeter-north"),
                        new WaveEntryRecipe("1", enemies[1], 3, 1, 220, 84, "perimeter-east"),
                        new WaveEntryRecipe("2", enemies[2], 1, 1, 420, 0, "perimeter-northwest")
                    },
                    new[] { "idle-auto-defense", "opening" }),
                WaveDefinitionAsset.CreateTransient(
                    "wave.idle-auto-defense.runner-pressure",
                    "Runner Pressure",
                    620,
                    new[]
                    {
                        new WaveEntryRecipe("0", enemies[1], 6, 1, 0, 58, "perimeter-southeast", 1),
                        new WaveEntryRecipe("1", enemies[0], 7, 1, 90, 55, "perimeter-northeast", 1)
                    },
                    new[] { "idle-auto-defense", "runner-pressure" }),
                WaveDefinitionAsset.CreateTransient(
                    "wave.idle-auto-defense.pressure",
                    "Mixed Pressure",
                    1180,
                    new[]
                    {
                        new WaveEntryRecipe("0", enemies[3], 3, 1, 0, 72, "perimeter-south", 1),
                        new WaveEntryRecipe("1", enemies[2], 2, 1, 100, 92, "perimeter-west", 2),
                        new WaveEntryRecipe("2", enemies[1], 6, 1, 180, 52, "perimeter-northeast", 2)
                    },
                    new[] { "idle-auto-defense", "pressure" }),
                WaveDefinitionAsset.CreateTransient(
                    "wave.idle-auto-defense.surge",
                    "Tank Break",
                    1760,
                    new[]
                    {
                        new WaveEntryRecipe("0", enemies[0], 9, 1, 0, 42, "perimeter-southwest", 1),
                        new WaveEntryRecipe("1", enemies[1], 6, 1, 130, 54, "perimeter-southeast", 1),
                        new WaveEntryRecipe("2", enemies[3], 3, 1, 260, 80, "perimeter-west", 2)
                    },
                    new[] { "idle-auto-defense", "tank-break" }),
                WaveDefinitionAsset.CreateTransient(
                    "wave.idle-auto-defense.elite",
                    "Elite Pressure",
                    2450,
                    new[]
                    {
                        new WaveEntryRecipe("0", enemies[4], 1, 1, 0, 0, "perimeter-northwest", 3),
                        new WaveEntryRecipe("1", enemies[1], 7, 1, 140, 52, "perimeter-east", 2),
                        new WaveEntryRecipe("2", enemies[3], 3, 1, 300, 80, "perimeter-south", 2)
                    },
                    new[] { "idle-auto-defense", "elite" }),
                WaveDefinitionAsset.CreateTransient(
                    "wave.idle-auto-defense.final",
                    "Final Surge",
                    3150,
                    new[]
                    {
                        new WaveEntryRecipe("0", enemies[2], 3, 1, 0, 100, "perimeter-north", 2),
                        new WaveEntryRecipe("1", enemies[3], 4, 1, 170, 74, "perimeter-east", 2),
                        new WaveEntryRecipe("2", enemies[1], 8, 1, 300, 48, "perimeter-southwest", 3)
                    },
                    new[] { "idle-auto-defense", "final" }),
                WaveDefinitionAsset.CreateTransient(
                    "wave.idle-auto-defense.boss",
                    "Boss Push",
                    3900,
                    new[]
                    {
                        new WaveEntryRecipe("0", enemies[5], 1, 1, 0, 0, "perimeter-south", 4),
                        new WaveEntryRecipe("1", enemies[4], 1, 1, 300, 0, "perimeter-northeast", 3),
                        new WaveEntryRecipe("2", enemies[1], 12, 1, 400, 64, "perimeter-northwest", 3)
                    },
                    new[] { "idle-auto-defense", "boss" })
            };
        }

        internal static WaveDefinitionAsset[] ResolveWaveDefinitionsForTemplate(IReadOnlyList<WaveDefinitionAsset> assignedDefinitions, IReadOnlyList<EnemyDefinitionAsset> enemyDefinitions, out int rejectedDefinitionCount)
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

        internal static WaveDefinition[] CreateEncounterWaves(IReadOnlyList<WaveDefinitionAsset> waveDefinitions)
        {
            if (waveDefinitions == null || waveDefinitions.Count == 0) throw new ArgumentException("At least one wave definition is required.", nameof(waveDefinitions));
            var waves = new WaveDefinition[waveDefinitions.Count];
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < waveDefinitions.Count; i++)
            {
                WaveDefinitionAsset wave = waveDefinitions[i];
                if (wave == null) throw new ArgumentException("Wave definition cannot be null.", nameof(waveDefinitions));
                ContentAuthoringValidationReport report = WaveDefinitionValidator.Validate(wave, WaveDefinitionValidationOptions.RuntimeFriendly);
                if (!report.IsValid) throw new ArgumentException("Wave definition is invalid: " + IdleAutoDefenseContentValidation.GetFirstValidationError(report), nameof(waveDefinitions));
                if (!seen.Add(wave.Id.Trim())) throw new ArgumentException("Duplicate wave definition ID: " + wave.Id, nameof(waveDefinitions));

                IReadOnlyList<WaveEntryRecipe> entries = wave.Entries.Entries;
                var groups = new SpawnGroupDefinition[entries.Count];
                for (int j = 0; j < entries.Count; j++)
                {
                    WaveEntryRecipe entry = entries[j];
                    groups[j] = SpawnGroupDefinition.Fixed(
                        new SpawnGroupId(wave.Id + ".group." + entry.EntryId.Value),
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
    }
}
