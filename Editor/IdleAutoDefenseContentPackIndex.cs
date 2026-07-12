using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Deucarian.Attacks.Authoring;
using Deucarian.GameContentAuthoring.Editor;
using Deucarian.RunUpgrades.Authoring;
using Deucarian.WeaponSystems.Authoring;
using UnityEditor;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense.Editor
{
    internal sealed class IdleAutoDefenseContentPackIndex
    {
        public const string PackId = "contentpack.idle-auto-defense.playable";
        public const string OwningPackageId = "com.deucarian.template.game.idle-auto-defense";
        public const string DisplayName = "Basic Idle Auto Defense";
        public const string PlayableSceneFileName = "OPEN_THIS_TO_TEST_IdleAutoDefense_PlayableGame.unity";
        public const string ValidateActionId = "validate-idle-auto-defense-pack";
        public const string RevealActionId = "reveal-idle-auto-defense-pack";
        public const string OpenSceneActionId = "open-idle-auto-defense-scene";
        public const string OpenSetupActionId = "open-idle-auto-defense-setup";

        private const string GeneratedContentSearchRoot = "Assets/GameContent";

        private IdleAutoDefenseContentPackIndex(
            GameContentPackAsset packAsset,
            GameContentSetAsset contentSetAsset,
            SceneAsset playableScene,
            string packAssetPath,
            string contentRootPath,
            string playableScenePath,
            GameContentPackSourceState sourceState,
            IReadOnlyList<GameContentRecordDescriptor> records,
            IReadOnlyList<GameContentSourceClaim> sourceClaims,
            GameContentAuthoringValidationResult validation)
        {
            PackAsset = packAsset;
            ContentSetAsset = contentSetAsset;
            PlayableScene = playableScene;
            PackAssetPath = packAssetPath ?? string.Empty;
            ContentRootPath = contentRootPath ?? string.Empty;
            PlayableScenePath = playableScenePath ?? string.Empty;
            SourceState = sourceState;
            Records = records ?? Array.Empty<GameContentRecordDescriptor>();
            SourceClaims = sourceClaims ?? Array.Empty<GameContentSourceClaim>();
            Validation = validation ?? GameContentAuthoringValidationResult.Valid;
        }

        public GameContentPackAsset PackAsset { get; }
        public GameContentSetAsset ContentSetAsset { get; }
        public SceneAsset PlayableScene { get; }
        public string PackAssetPath { get; }
        public string ContentRootPath { get; }
        public string PlayableScenePath { get; }
        public GameContentPackSourceState SourceState { get; }
        public IReadOnlyList<GameContentRecordDescriptor> Records { get; }
        public IReadOnlyList<GameContentSourceClaim> SourceClaims { get; }
        public GameContentAuthoringValidationResult Validation { get; }
        public bool HasGeneratedPack => PackAsset != null && SourceState != GameContentPackSourceState.DuplicateConflict;

        public static IdleAutoDefenseContentPackIndex Discover()
        {
            return Discover(GeneratedContentSearchRoot);
        }

        internal static IdleAutoDefenseContentPackIndex Discover(string generatedContentSearchRoot)
        {
            var issues = new List<GameContentAuthoringValidationIssue>();
            string searchRoot = string.IsNullOrWhiteSpace(generatedContentSearchRoot)
                ? GeneratedContentSearchRoot
                : NormalizePath(generatedContentSearchRoot).TrimEnd('/');
            GameContentPackAsset[] candidates = FindGeneratedPackCandidates(searchRoot);
            if (candidates.Length == 0)
            {
                issues.Add(GameContentAuthoringValidationIssue.Error(
                    "Generated Content",
                    "Basic Idle Auto Defense has not been generated under Assets/GameContent. Run the existing template setup wizard."));
                return Empty(GameContentPackSourceState.MissingSource, searchRoot, issues);
            }

            if (candidates.Length > 1)
            {
                issues.Add(GameContentAuthoringValidationIssue.Error(
                    "Generated Content",
                    "Multiple generated GameContentPackAsset instances use stable ID '" + PackId + "': " +
                    string.Join(", ", candidates.Select(AssetDatabase.GetAssetPath).OrderBy(value => value, StringComparer.OrdinalIgnoreCase)) + "."));
                return Empty(GameContentPackSourceState.DuplicateConflict, searchRoot, issues);
            }

            GameContentPackAsset pack = candidates[0];
            string packPath = NormalizePath(AssetDatabase.GetAssetPath(pack));
            string contentRoot = ResolveContentRoot(packPath);
            GameContentSetAsset contentSet = pack.DefaultContentSet;
            SceneAsset scene = FindPlayableScene(packPath, out string scenePath, out string sceneIssue);
            if (!string.IsNullOrWhiteSpace(sceneIssue))
                issues.Add(GameContentAuthoringValidationIssue.Error("Playable Scene", sceneIssue));

            AddPackValidation(pack, issues);
            IReadOnlyList<GameContentRecordDescriptor> records = BuildRecords(pack, contentSet, contentRoot, issues);
            AddExpectedCountIssues(records, issues);
            IReadOnlyList<GameContentSourceClaim> claims = BuildSourceClaims(pack);
            GameContentPackSourceState state = issues.Any(value => value.Severity == GameContentAuthoringValidationSeverity.Error)
                ? GameContentPackSourceState.ValidationFailed
                : GameContentPackSourceState.Available;
            return new IdleAutoDefenseContentPackIndex(
                pack,
                contentSet,
                scene,
                packPath,
                contentRoot,
                scenePath,
                state,
                records,
                claims,
                new GameContentAuthoringValidationResult(issues));
        }

        public GameContentPackDescriptor BuildDescriptor(string providerId)
        {
            var access = new GameContentPackAccessDescriptor(
                GameContentPackBackendCapability.Read |
                GameContentPackBackendCapability.Validate |
                GameContentPackBackendCapability.RevealSource,
                "Read-only ScriptableObject graph",
                "Transactional ScriptableObject editing is deferred.");
            var actions = new List<GameContentActionDescriptor>
            {
                new GameContentActionDescriptor(
                    ValidateActionId,
                    "Validate",
                    "Validate the generated Idle content pack and content-set graph.",
                    PackAsset != null,
                    "Generate Basic Idle Auto Defense first.",
                    GameContentActionKind.Validate),
                new GameContentActionDescriptor(
                    RevealActionId,
                    "Reveal Source",
                    "Select and ping the generated GameContentPackAsset.",
                    PackAsset != null,
                    "The generated pack asset is unavailable.",
                    GameContentActionKind.RevealSource),
                new GameContentActionDescriptor(
                    OpenSceneActionId,
                    "Open Playable Scene",
                    "Open the generated Idle Auto Defense playable scene.",
                    PlayableScene != null,
                    "The generated playable scene could not be resolved.",
                    GameContentActionKind.OpenScene)
            };
            if (PackAsset == null)
            {
                actions.Add(new GameContentActionDescriptor(
                    OpenSetupActionId,
                    "Open Setup Wizard",
                    "Open the existing Idle Auto Defense setup workflow.",
                    true,
                    string.Empty,
                    GameContentActionKind.Custom));
            }

            return new GameContentPackDescriptor(
                PackId,
                OwningPackageId,
                providerId,
                DisplayName,
                PackAsset == null
                    ? "Generate the project-owned Idle Auto Defense content graph to make this pack available."
                    : PackAsset.Description,
                PackAsset == null ? "1" : PackAsset.Version,
                PackAsset == null ? new[] { "idle-auto-defense", "generated" } : PackAsset.Tags,
                GameContentPackSourceKind.Project,
                SourceState,
                PackAssetPath,
                null,
                PlayableScene,
                PackAsset == null ? null : PackAsset.Banner,
                PackAsset == null ? null : PackAsset.Icon,
                null,
                BuildCategories(Records),
                actions,
                Validation,
                Records.Count,
                access,
                BuildPackMetadata());
        }

        private IReadOnlyList<GameContentMetadataDescriptor> BuildPackMetadata()
        {
            return new[]
            {
                Metadata("GameContentPackAsset", PackAsset == null ? "Missing" : PackAsset.name),
                Metadata("GameContentSetAsset", ContentSetAsset == null ? "Missing" : ContentSetAsset.name),
                Metadata("Default Content Set ID", ContentSetAsset == null ? "Missing" : ContentSetAsset.Id),
                Metadata("GameContentSet Source", ContentSetAsset == null ? "Missing" : NormalizePath(AssetDatabase.GetAssetPath(ContentSetAsset))),
                Metadata("Playable Scene", string.IsNullOrWhiteSpace(PlayableScenePath) ? "Missing" : PlayableScenePath),
                Metadata("Availability", SourceState.ToString()),
                Metadata("Strict Authored Binding", ContentSetAsset != null && GameContentSetValidator.Validate(ContentSetAsset).IsValid ? "Ready" : "Blocked by validation"),
                Metadata("Authored Core Sources", ContentSetAsset == null ? "0 / 6" : CountAuthoredCoreSources(ContentSetAsset) + " / 6"),
                Metadata("Reward Catalog", ContentSetAsset == null || ContentSetAsset.RewardCatalog == null ? "Missing" : ContentSetAsset.RewardCatalog.Id),
                Metadata("Economy", ContentSetAsset == null || ContentSetAsset.Economy == null ? "Missing" : ContentSetAsset.Economy.Id),
                Metadata("Run Profile", ContentSetAsset == null || ContentSetAsset.RunProfile == null ? "Missing" : ContentSetAsset.RunProfile.Id),
                Metadata("Progression", ContentSetAsset == null || ContentSetAsset.Progression == null ? "Missing" : ContentSetAsset.Progression.Id),
                Metadata("Offline Progression", ContentSetAsset == null || ContentSetAsset.OfflineProgression == null ? "Missing" : ContentSetAsset.OfflineProgression.Id),
                Metadata("Game Rules", ContentSetAsset == null || ContentSetAsset.GameRules == null ? "Missing" : ContentSetAsset.GameRules.Id),
                Metadata("Projected Records", Records.Count.ToString(CultureInfo.InvariantCulture)),
                Metadata("Validation", Validation.IsValid ? "Valid" : Validation.Issues.Count + " issue(s)"),
                Metadata("Setup State", PackAsset == null ? "Not generated" : "Generated project content"),
                Metadata("Content Root", string.IsNullOrWhiteSpace(ContentRootPath) ? GeneratedContentSearchRoot : ContentRootPath)
            };
        }

        private static int CountAuthoredCoreSources(GameContentSetAsset contentSet)
        {
            int count = 0;
            if (contentSet.RewardCatalog != null) count++;
            if (contentSet.Economy != null) count++;
            if (contentSet.RunProfile != null) count++;
            if (contentSet.Progression != null) count++;
            if (contentSet.OfflineProgression != null) count++;
            if (contentSet.GameRules != null) count++;
            return count;
        }

        private static IdleAutoDefenseContentPackIndex Empty(
            GameContentPackSourceState state,
            string searchRoot,
            IReadOnlyList<GameContentAuthoringValidationIssue> issues)
        {
            return new IdleAutoDefenseContentPackIndex(
                null,
                null,
                null,
                string.Empty,
                searchRoot,
                string.Empty,
                state,
                Array.Empty<GameContentRecordDescriptor>(),
                Array.Empty<GameContentSourceClaim>(),
                new GameContentAuthoringValidationResult(issues));
        }

        private static GameContentPackAsset[] FindGeneratedPackCandidates(string searchRoot)
        {
            if (!AssetDatabase.IsValidFolder(searchRoot)) return Array.Empty<GameContentPackAsset>();
            return AssetDatabase.FindAssets("t:GameContentPackAsset", new[] { searchRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => NormalizePath(path).StartsWith(searchRoot + "/", StringComparison.OrdinalIgnoreCase))
                .Select(AssetDatabase.LoadAssetAtPath<GameContentPackAsset>)
                .Where(asset => asset != null && string.Equals(asset.Id, PackId, StringComparison.OrdinalIgnoreCase))
                .Distinct()
                .OrderBy(AssetDatabase.GetAssetPath, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static SceneAsset FindPlayableScene(string packPath, out string scenePath, out string issue)
        {
            scenePath = string.Empty;
            issue = string.Empty;
            string[] candidates = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(NormalizePath)
                .Where(path => string.Equals(Path.GetFileName(path), PlayableSceneFileName, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            string[] referencingPack = candidates.Where(path => AssetDatabase.GetDependencies(path, true)
                    .Any(dependency => string.Equals(NormalizePath(dependency), packPath, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
            string[] resolved = referencingPack.Length > 0 ? referencingPack : candidates;
            if (resolved.Length == 0)
            {
                issue = "Generated scene '" + PlayableSceneFileName + "' was not found under Assets.";
                return null;
            }

            if (resolved.Length > 1)
            {
                issue = "Multiple generated playable scenes match the selected pack: " + string.Join(", ", resolved) + ".";
                return null;
            }

            scenePath = resolved[0];
            return AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        }

        private static IReadOnlyList<GameContentRecordDescriptor> BuildRecords(
            GameContentPackAsset pack,
            GameContentSetAsset contentSet,
            string contentRoot,
            ICollection<GameContentAuthoringValidationIssue> packIssues)
        {
            if (contentSet == null) return Array.Empty<GameContentRecordDescriptor>();
            AttackDefinitionAsset[] attacks = contentSet.AvailableWeapons
                .Where(weapon => weapon != null && weapon.Stats != null && weapon.Stats.Attack != null)
                .Select(weapon => weapon.Stats.Attack)
                .Distinct()
                .ToArray();
            EnemyDefinitionAsset[] enemies = contentSet.EnemyPool.Where(value => value != null)
                .Distinct().ToArray();
            WaveDefinitionAsset[] waves = contentSet.WaveSet.Where(value => value != null)
                .Distinct().ToArray();
            WeaponDefinitionAsset[] weapons = contentSet.AvailableWeapons.Where(value => value != null)
                .Distinct().ToArray();
            RunUpgradeDefinitionAsset[] upgrades = contentSet.UpgradePool.Where(value => value != null)
                .Distinct().ToArray();

            GameContentLibraryReport library = GameContentLibraryService.Scan(contentRoot);
            var drafts = new List<RecordDraft>();
            int order = 0;
            drafts.AddRange(attacks.Select(asset => AttackDraft(asset, order++, library)));
            drafts.AddRange(enemies.Select(asset => EnemyDraft(asset, order++, library)));
            for (int waveIndex = 0; waveIndex < waves.Length; waveIndex++)
                drafts.Add(WaveDraft(waves[waveIndex], order++, waveIndex, library));
            drafts.AddRange(weapons.Select(asset => WeaponDraft(asset, order++, library)));
            drafts.AddRange(upgrades.Select(asset => UpgradeDraft(asset, order++, library)));
            AddAuthoredCoreDrafts(contentSet, drafts, ref order);

            AddIdentityIssues(drafts, packIssues);
            Dictionary<RecordDraft, GameContentRecordKey> keys = drafts.ToDictionary(
                draft => draft,
                draft => BuildKey(draft, packIssues));
            AddReferences(drafts, keys);
            return BuildDescriptors(drafts, keys);
        }

        private static RecordDraft AttackDraft(AttackDefinitionAsset asset, int order, GameContentLibraryReport library)
        {
            AttackMechanicsDefinitionAsset mechanics = asset.Mechanics;
            AttackDeliveryDefinitionAsset delivery = asset.Delivery;
            string status = asset.StatusEffects == null
                ? "None"
                : string.Join(", ", asset.StatusEffects.StatusEffects.Select(value => value == null ? string.Empty : value.StatusId)
                    .Where(value => !string.IsNullOrWhiteSpace(value)));
            var draft = new RecordDraft(
                asset,
                asset.Id,
                "attacks",
                asset.DisplayName,
                Fallback(asset.BalancingNotes, "Authored Idle Auto Defense attack."),
                mechanics == null ? "Missing attack mechanics" : Number(mechanics.DamageAmount) + " damage, " + mechanics.CooldownTicks.ToString(CultureInfo.InvariantCulture) + " tick cadence",
                order,
                new[] { GameContentRecordCapabilities.Attack },
                ValidationFor(asset, library),
                new[]
                {
                    Metadata("Damage", mechanics == null ? "Missing" : Number(mechanics.DamageAmount)),
                    Metadata("Cooldown Ticks", mechanics == null ? "Missing" : mechanics.CooldownTicks.ToString(CultureInfo.InvariantCulture)),
                    Metadata("Range", mechanics == null ? "Missing" : Number(mechanics.Range)),
                    Metadata("Targeting", asset.Targeting == null ? "Missing" : asset.Targeting.Mode.ToString()),
                    Metadata("Max Targets", asset.Targeting == null ? "Missing" : asset.Targeting.MaxTargets.ToString(CultureInfo.InvariantCulture)),
                    Metadata("Delivery", delivery == null ? "Missing" : delivery.Mode.ToString()),
                    Metadata("Projectile ID", delivery == null ? string.Empty : delivery.ProjectileDefinitionId),
                    Metadata("Projectile Speed", delivery == null ? "0" : Number(delivery.ProjectileSpeed)),
                    Metadata("Projectile Lifetime Ticks", delivery == null ? "0" : delivery.ProjectileLifetimeTicks.ToString(CultureInfo.InvariantCulture)),
                    Metadata("Homing", delivery != null && delivery.Homing ? "Yes" : "No"),
                    Metadata("Pierce", delivery == null ? "0" : delivery.PierceCount.ToString(CultureInfo.InvariantCulture)),
                    Metadata("Area Radius", delivery == null ? "0" : Number(delivery.Radius)),
                    Metadata("Status", string.IsNullOrWhiteSpace(status) ? "None" : status),
                    Metadata("Presentation", asset.Presentation == null ? "Missing" : asset.Presentation.Events.Count.ToString(CultureInfo.InvariantCulture) + " event(s)")
                });
            if (delivery != null && delivery.Mode == AttackRecipeDeliveryMode.Projectile)
                draft.References.Add(ReferenceDraft.External(
                    delivery.ProjectileDefinitionId,
                    "projectiles",
                    "delivers projectile",
                    true,
                    !string.IsNullOrWhiteSpace(delivery.ProjectileDefinitionId)));
            return draft;
        }

        private static RecordDraft EnemyDraft(EnemyDefinitionAsset asset, int order, GameContentLibraryReport library)
        {
            EnemyStatsDefinitionAsset stats = asset.Stats;
            bool elite = HasTag(asset.Tags, "elite");
            bool boss = HasTag(asset.Tags, "boss") || asset.Role == EnemyRole.Boss && !elite;
            var capabilities = new List<GameContentRecordCapability> { GameContentRecordCapabilities.Enemy };
            if (elite) capabilities.Add(GameContentRecordCapabilities.Elite);
            if (boss) capabilities.Add(GameContentRecordCapabilities.Boss);
            if (elite || boss) capabilities.Add(GameContentRecordCapabilities.MajorThreat);
            return new RecordDraft(
                asset,
                asset.Id,
                "enemies",
                asset.DisplayName,
                Fallback(asset.BalancingNotes, "Authored Idle Auto Defense enemy."),
                stats == null ? "Missing enemy stats" : Number(stats.MaximumHealth) + " health, " + Number(stats.MoveSpeed) + " speed",
                order,
                capabilities,
                ValidationFor(asset, library),
                new[]
                {
                    Metadata("Role", asset.Role.ToString()),
                    Metadata("Tags", string.Join(", ", asset.Tags)),
                    Metadata("Health", stats == null ? "Missing" : Number(stats.MaximumHealth)),
                    Metadata("Move Speed", stats == null ? "Missing" : Number(stats.MoveSpeed)),
                    Metadata("Radius", stats == null ? "Missing" : Number(stats.CollisionRadius)),
                    Metadata("Contact Damage", stats == null ? "Missing" : Number(stats.ContactDamage)),
                    Metadata("Reward", stats == null ? "Missing" : stats.RewardValue.ToString(CultureInfo.InvariantCulture)),
                    Metadata("Contact Cadence", "Runtime-owned; not authored"),
                    Metadata("Presentation", asset.Presentation == null || asset.Presentation.Prefab == null ? "Missing prefab" : asset.Presentation.Prefab.name),
                    Metadata("Major Threat", elite || boss ? "Yes" : "No")
                });
        }

        private static RecordDraft WaveDraft(
            WaveDefinitionAsset asset,
            int order,
            int waveOrder,
            GameContentLibraryReport library)
        {
            IReadOnlyList<WaveEntryRecipe> entries = asset.Entries == null
                ? Array.Empty<WaveEntryRecipe>()
                : asset.Entries.Entries;
            bool elite = entries.Any(entry => entry != null && entry.Enemy != null && HasTag(entry.Enemy.Tags, "elite"));
            bool boss = entries.Any(entry => entry != null && entry.Enemy != null &&
                (HasTag(entry.Enemy.Tags, "boss") || entry.Enemy.Role == EnemyRole.Boss && !HasTag(entry.Enemy.Tags, "elite")));
            var capabilities = new List<GameContentRecordCapability>
            {
                GameContentRecordCapabilities.Encounter,
                GameContentRecordCapabilities.Wave
            };
            if (elite) capabilities.Add(GameContentRecordCapabilities.EliteEvent);
            if (boss) capabilities.Add(GameContentRecordCapabilities.BossEvent);
            var draft = new RecordDraft(
                asset,
                asset.Id,
                "waves",
                asset.DisplayName,
                Fallback(asset.BalancingNotes, "Authored Idle Auto Defense defense wave."),
                entries.Count.ToString(CultureInfo.InvariantCulture) + " group(s), " + entries.Sum(entry => entry == null ? 0 : Math.Max(0, entry.Count)).ToString(CultureInfo.InvariantCulture) + " spawn(s)",
                order,
                capabilities,
                ValidationFor(asset, library),
                new[]
                {
                    Metadata("Wave Order", waveOrder.ToString(CultureInfo.InvariantCulture)),
                    Metadata("Start Tick", asset.Schedule == null ? "Missing" : asset.Schedule.StartTick.ToString(CultureInfo.InvariantCulture)),
                    Metadata("Schedule Unit", "Simulation ticks"),
                    Metadata("Entry Count", entries.Count.ToString(CultureInfo.InvariantCulture)),
                    Metadata("Enemy Count", entries.Sum(entry => entry == null ? 0 : Math.Max(0, entry.Count)).ToString(CultureInfo.InvariantCulture)),
                    Metadata("Spawn Channels", string.Join(", ", entries.Where(entry => entry != null).Select(entry => entry.SpawnChannelId).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct())),
                    Metadata("Scaling Tiers", string.Join(", ", entries.Where(entry => entry != null).Select(entry => entry.ScalingTier.ToString(CultureInfo.InvariantCulture)).Distinct())),
                    Metadata("Entry Timing", string.Join(", ", entries.Where(entry => entry != null).Select(entry =>
                        "delay " + entry.InitialDelayTicks.ToString(CultureInfo.InvariantCulture) +
                        " / interval " + entry.IntervalTicks.ToString(CultureInfo.InvariantCulture) + " ticks"))),
                    Metadata("Tags", string.Join(", ", asset.Tags))
                });
            foreach (EnemyDefinitionAsset enemy in entries.Where(entry => entry != null).Select(entry => entry.Enemy).Where(value => value != null).Distinct())
                draft.References.Add(ReferenceDraft.ToAsset(enemy, "enemies", "spawns enemy", true));
            return draft;
        }

        private static RecordDraft WeaponDraft(WeaponDefinitionAsset asset, int order, GameContentLibraryReport library)
        {
            WeaponStatsDefinitionAsset stats = asset.Stats;
            AttackDefinitionAsset attack = stats == null ? null : stats.Attack;
            AttackMechanicsDefinitionAsset mechanics = attack == null ? null : attack.Mechanics;
            var draft = new RecordDraft(
                asset,
                asset.Id,
                "weapons",
                asset.DisplayName,
                Fallback(asset.BalancingNotes, "Authored mounted Idle Auto Defense weapon."),
                stats == null ? "Missing weapon stats" : stats.FireMode + ", " + stats.CooldownTicks.ToString(CultureInfo.InvariantCulture) + " tick cadence",
                order,
                new[] { GameContentRecordCapabilities.Weapon, GameContentRecordCapabilities.Tower },
                ValidationFor(asset, library),
                new[]
                {
                    Metadata("Kind", "Mounted weapon / tower module"),
                    Metadata("Fire Mode", stats == null ? "Missing" : stats.FireMode.ToString()),
                    Metadata("Attack", attack == null ? "Missing" : attack.Id),
                    Metadata("Damage", mechanics == null ? "Missing" : Number(mechanics.DamageAmount)),
                    Metadata("Cooldown Ticks", stats == null ? "Missing" : stats.CooldownTicks.ToString(CultureInfo.InvariantCulture)),
                    Metadata("Range", stats == null ? "Missing" : Number(stats.Range)),
                    Metadata("Targeting", stats == null ? "Missing" : stats.TargetingRoleId),
                    Metadata("Burst", stats == null ? "Missing" : stats.BurstCount.ToString(CultureInfo.InvariantCulture)),
                    Metadata("Volley", stats == null ? "Missing" : stats.VolleyCount.ToString(CultureInfo.InvariantCulture)),
                    Metadata("Build Cost", stats == null ? "Missing" : stats.BuildCost.ToString(CultureInfo.InvariantCulture)),
                    Metadata("Presentation", asset.Presentation == null || asset.Presentation.Prefab == null ? "Missing prefab" : asset.Presentation.Prefab.name)
                });
            draft.References.Add(ReferenceDraft.ToAsset(attack, "attacks", "uses attack", true));
            return draft;
        }

        private static RecordDraft UpgradeDraft(RunUpgradeDefinitionAsset asset, int order, GameContentLibraryReport library)
        {
            RunUpgradeEffectRecipe[] effects = asset.Effects == null
                ? Array.Empty<RunUpgradeEffectRecipe>()
                : asset.Effects.Effects.Where(value => value != null).ToArray();
            bool weaponUpgrade = effects.Any(value => value.Weapon != null);
            var capabilities = new List<GameContentRecordCapability> { GameContentRecordCapabilities.Upgrade };
            if (weaponUpgrade) capabilities.Add(GameContentRecordCapabilities.WeaponUpgrade);
            var draft = new RecordDraft(
                asset,
                asset.Id,
                "upgrades",
                asset.DisplayName,
                asset.Description,
                effects.Length == 0 ? "No authored effects" : string.Join(", ", effects.Select(value => value.TargetKind + " " + Number(value.Amount))),
                order,
                capabilities,
                ValidationFor(asset, library),
                new[]
                {
                    Metadata("Rarity", asset.Economy == null ? "Missing" : asset.Economy.Rarity.ToString()),
                    Metadata("Weight", asset.Economy == null ? "Missing" : asset.Economy.Weight.ToString(CultureInfo.InvariantCulture)),
                    Metadata("Max Rank", asset.Economy == null ? "Missing" : asset.Economy.MaxRank.ToString(CultureInfo.InvariantCulture)),
                    Metadata("Effect", effects.Length == 0 ? "Missing" : string.Join(", ", effects.Select(value => value.TargetKind.ToString()))),
                    Metadata("Amount", effects.Length == 0 ? "0" : string.Join(", ", effects.Select(value => Number(value.Amount)))),
                    Metadata("Target", effects.Length == 0 ? "Missing" : string.Join(", ", effects.Select(value => value.GetTargetId()).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct())),
                    Metadata("Prerequisites", asset.Effects == null ? string.Empty : string.Join(", ", asset.Effects.Prerequisites)),
                    Metadata("Tags", string.Join(", ", asset.Tags))
                });
            foreach (RunUpgradeEffectRecipe effect in effects)
            {
                UnityEngine.Object target = effect.Weapon != null
                    ? (UnityEngine.Object)effect.Weapon
                    : effect.Attack != null
                        ? effect.Attack
                        : effect.Enemy;
                string category = effect.Weapon != null ? "weapons" : effect.Attack != null ? "attacks" : "enemies";
                if (target != null)
                    draft.References.Add(ReferenceDraft.ToAsset(target, category, "targets " + effect.TargetKind, true));
            }
            return draft;
        }

        private static void AddAuthoredCoreDrafts(
            GameContentSetAsset contentSet,
            ICollection<RecordDraft> drafts,
            ref int order)
        {
            GameContentAuthoringValidationResult validation = AuthoredCoreValidationFor(contentSet);
            AddRewardDrafts(contentSet, drafts, ref order, validation);
            AddEconomyDrafts(contentSet, drafts, ref order, validation);
            AddRunProfileDraft(contentSet, drafts, ref order, validation);
            AddProgressionDrafts(contentSet, drafts, ref order, validation);
            AddOfflineProgressionDraft(contentSet, drafts, ref order, validation);
            AddGameRulesDraft(contentSet, drafts, ref order, validation);
        }

        private static void AddRewardDrafts(
            GameContentSetAsset contentSet,
            ICollection<RecordDraft> drafts,
            ref int order,
            GameContentAuthoringValidationResult validation)
        {
            IdleAutoDefenseRewardCatalogAsset asset = contentSet.RewardCatalog;
            if (asset == null) return;
            IdleAutoDefenseRewardDraftSettings settings = asset.Settings;
            IdleAutoDefenseRewardDraftCatalog catalog = asset.Catalog;
            var table = new RecordDraft(
                asset,
                asset.Id,
                "reward-tables",
                asset.DisplayName,
                "Authoritative live draft cadence, rarity tables, thresholds, and player-facing choices.",
                catalog == null ? "Missing reward choices" : RewardChoiceCount(catalog).ToString(CultureInfo.InvariantCulture) + " authored choices",
                order++,
                new[] { GameContentRecordCapabilities.Reward },
                validation,
                new[]
                {
                    Metadata("First Draft", Number(asset.FirstDraftSeconds) + " seconds"),
                    Metadata("Choice Count", settings == null ? "Missing" : settings.ChoiceCount.ToString(CultureInfo.InvariantCulture)),
                    Metadata("Normal / Epic / Legendary", catalog == null ? "Missing" : catalog.NormalWeaponRewards.Count + " / " + catalog.EpicWeaponRewards.Count + " / " + catalog.LegendaryWeaponRewards.Count),
                    Metadata("XP Curve", settings == null ? "Missing" : settings.BaseExperienceToNextLevel + " + " + settings.ExperienceToNextLevelGrowth + " per level")
                });
            drafts.Add(table);
            if (catalog == null) return;

            foreach (IdleAutoDefenseWeaponUnlockReward reward in catalog.WeaponUnlocks.Where(value => value != null))
            {
                var draft = RewardChoiceDraft(
                    asset,
                    reward.Id,
                    reward.DisplayName,
                    reward.EffectDescription,
                    reward.Track.ToString(),
                    "Level " + reward.GetRarity(IdleAutoDefenseRewardDraftKind.LevelUp) + ", Elite " + reward.GetRarity(IdleAutoDefenseRewardDraftKind.EliteDefeated) + ", Boss " + reward.GetRarity(IdleAutoDefenseRewardDraftKind.BossDefeated),
                    reward.Weight,
                    reward.MaxRank,
                    reward.EligibleSources,
                    reward.PrerequisiteIds,
                    order++,
                    validation,
                    Array.Empty<string>());
                if (reward.Weapon != null) draft.References.Add(ReferenceDraft.ToAsset(reward.Weapon, "weapons", "unlocks weapon", true));
                drafts.Add(draft);
            }

            AddWeaponRewardDrafts(asset, catalog.NormalWeaponRewards, "normal-upgrades", drafts, ref order, validation);
            AddWeaponRewardDrafts(asset, catalog.EpicWeaponRewards, "epic-upgrades", drafts, ref order, validation);
            AddWeaponRewardDrafts(asset, catalog.LegendaryWeaponRewards, "legendary-upgrades", drafts, ref order, validation);
            foreach (IdleAutoDefenseBaseRewardDefinition reward in catalog.BaseRewards.Where(value => value != null))
            {
                var draft = RewardChoiceDraft(
                    asset,
                    reward.Id,
                    reward.DisplayName,
                    reward.EffectDescription,
                    reward.Track.ToString(),
                    reward.Rarity.ToString(),
                    reward.Weight,
                    reward.MaxRank,
                    reward.EligibleSources,
                    reward.PrerequisiteIds,
                    order++,
                    validation,
                    Array.Empty<string>(),
                    Metadata("Target", reward.TargetId),
                    Metadata("Effect", reward.EffectKind + " " + Number(reward.Amount)));
                if (contentSet.GameRules != null)
                    draft.References.Add(ReferenceDraft.ToAsset(contentSet.GameRules, "game-rules", "targets authored objective", true));
                drafts.Add(draft);
            }
        }

        private static void AddWeaponRewardDrafts(
            IdleAutoDefenseRewardCatalogAsset asset,
            IEnumerable<IdleAutoDefenseWeaponRewardDefinition> rewards,
            string filteredCategory,
            ICollection<RecordDraft> drafts,
            ref int order,
            GameContentAuthoringValidationResult validation)
        {
            foreach (IdleAutoDefenseWeaponRewardDefinition reward in rewards.Where(value => value != null))
            {
                var draft = RewardChoiceDraft(
                    asset,
                    reward.Id,
                    reward.DisplayName,
                    reward.EffectDescription,
                    reward.Track.ToString(),
                    reward.Rarity.ToString(),
                    reward.Weight,
                    reward.MaxRank,
                    reward.EligibleSources,
                    reward.PrerequisiteIds,
                    order++,
                    validation,
                    new[] { filteredCategory },
                    Metadata("Target", reward.WeaponId),
                    Metadata("Effect", reward.EffectKind + " " + Number(reward.Amount)),
                    Metadata("Required Ranks", "Normal " + reward.RequiredNormalRank + ", Epic " + reward.RequiredEpicRank));
                if (reward.Weapon != null) draft.References.Add(ReferenceDraft.ToAsset(reward.Weapon, "weapons", "upgrades weapon", true));
                drafts.Add(draft);
            }
        }

        private static RecordDraft RewardChoiceDraft(
            IdleAutoDefenseRewardCatalogAsset asset,
            string id,
            string displayName,
            string description,
            string track,
            string rarity,
            double weight,
            int maxRank,
            IdleAutoDefenseRewardSourceEligibility eligibility,
            IEnumerable<string> prerequisites,
            int order,
            GameContentAuthoringValidationResult validation,
            IEnumerable<string> additionalCategories,
            params GameContentMetadataDescriptor[] extraMetadata)
        {
            var metadata = new List<GameContentMetadataDescriptor>
            {
                Metadata("Track", track),
                Metadata("Rarity", rarity),
                Metadata("Weight", Number(weight)),
                Metadata("Max Rank", maxRank.ToString(CultureInfo.InvariantCulture)),
                Metadata("Eligible Sources", eligibility.ToString())
            };
            metadata.AddRange(extraMetadata ?? Array.Empty<GameContentMetadataDescriptor>());
            var draft = new RecordDraft(
                asset,
                id,
                "reward-choices",
                displayName,
                description,
                track + " reward, " + rarity,
                order,
                new[] { GameContentRecordCapabilities.Reward },
                validation,
                metadata,
                additionalCategories);
            foreach (string prerequisite in prerequisites ?? Array.Empty<string>())
                draft.References.Add(ReferenceDraft.External(prerequisite, "reward-choices", "requires reward", true, false));
            return draft;
        }

        private static int RewardChoiceCount(IdleAutoDefenseRewardDraftCatalog catalog)
        {
            return catalog.WeaponUnlocks.Count + catalog.NormalWeaponRewards.Count + catalog.EpicWeaponRewards.Count +
                   catalog.LegendaryWeaponRewards.Count + catalog.BaseRewards.Count;
        }

        private static void AddEconomyDrafts(
            GameContentSetAsset contentSet,
            ICollection<RecordDraft> drafts,
            ref int order,
            GameContentAuthoringValidationResult validation)
        {
            IdleAutoDefenseEconomyAsset asset = contentSet.Economy;
            if (asset == null) return;
            drafts.Add(new RecordDraft(
                asset,
                asset.Id,
                "economy",
                asset.DisplayName,
                "Authoritative currencies, income, purchase curves, and encounter/run rewards.",
                asset.Currencies.Count + " currencies, " + asset.UpgradeCosts.Count + " cost curves",
                order++,
                Array.Empty<GameContentRecordCapability>(),
                validation,
                new[]
                {
                    Metadata("Passive Income", asset.PassiveIncomeAmount + " " + asset.PassiveIncomeCurrencyId + " / " + asset.PassiveIncomeIntervalTicks + " ticks"),
                    Metadata("Encounter Reward", asset.EncounterCompletionCredits + " credits, " + asset.EncounterCompletionParts + " parts, " + asset.EncounterCompletionAccountXp + " XP"),
                    Metadata("Run Claim Multiplier", Number(asset.RunRewardClaimMultiplier))
                }));
            foreach (IdleAutoDefenseCurrencyRecord currency in asset.Currencies.Where(value => value != null))
            {
                drafts.Add(new RecordDraft(
                    asset,
                    currency.Id,
                    "currencies",
                    currency.DisplayName,
                    "Pack-owned resource used by runtime economy and progression.",
                    "Starts at " + currency.StartingAmount,
                    order++,
                    Array.Empty<GameContentRecordCapability>(),
                    validation,
                    new[]
                    {
                        Metadata("Starting Amount", currency.StartingAmount.ToString(CultureInfo.InvariantCulture)),
                        Metadata("Capacity", currency.Capacity.ToString(CultureInfo.InvariantCulture))
                    },
                    new[] { "economy" }));
            }
            foreach (IdleAutoDefenseCostCurve cost in asset.UpgradeCosts.Where(value => value != null))
            {
                var draft = new RecordDraft(
                    asset,
                    cost.Id,
                    "economy",
                    cost.DisplayName,
                    "Authoritative purchase cost curve.",
                    cost.BaseCost + " + " + cost.CostPerRank + " per rank",
                    order++,
                    Array.Empty<GameContentRecordCapability>(),
                    validation,
                    new[]
                    {
                        Metadata("Currency", cost.CurrencyId),
                        Metadata("Base Cost", cost.BaseCost.ToString(CultureInfo.InvariantCulture)),
                        Metadata("Cost Per Rank", cost.CostPerRank.ToString(CultureInfo.InvariantCulture))
                    });
                draft.References.Add(ReferenceDraft.External(cost.CurrencyId, "currencies", "spends currency", true, false));
                drafts.Add(draft);
            }
        }

        private static void AddRunProfileDraft(
            GameContentSetAsset contentSet,
            ICollection<RecordDraft> drafts,
            ref int order,
            GameContentAuthoringValidationResult validation)
        {
            IdleAutoDefenseRunProfileAsset asset = contentSet.RunProfile;
            if (asset == null) return;
            var draft = new RecordDraft(
                asset,
                asset.Id,
                "run-profiles",
                asset.DisplayName,
                "Authoritative fixed-rate session timing, wave sequence, outcome rules, and scaling.",
                Number(asset.SessionLengthSeconds) + " seconds at " + asset.SimulationTicksPerSecond + " Hz",
                order++,
                new[] { GameContentRecordCapabilities.RunProfile },
                validation,
                new[]
                {
                    Metadata("Time Unit", asset.TimeUnit.ToString()),
                    Metadata("Tick Semantics", asset.TickSemantics.ToString()),
                    Metadata("Tick Rate", asset.SimulationTicksPerSecond + " per second"),
                    Metadata("Session Length", asset.SessionLengthTicks + " ticks / " + Number(asset.SessionLengthSeconds) + " seconds"),
                    Metadata("Wave Sequence", asset.Waves.Count.ToString(CultureInfo.InvariantCulture)),
                    Metadata("Difficulty", Number(asset.DifficultyMultiplier)),
                    Metadata("Endless", asset.Endless ? "Enabled" : "Disabled"),
                    Metadata("Victory / Defeat", asset.VictoryRule + " / " + asset.DefeatRule),
                    Metadata("Reward Multiplier", Number(asset.RewardMultiplier))
                });
            foreach (WaveDefinitionAsset wave in asset.Waves.Where(value => value != null))
                draft.References.Add(ReferenceDraft.ToAsset(wave, "waves", "schedules wave", true));
            drafts.Add(draft);
        }

        private static void AddProgressionDrafts(
            GameContentSetAsset contentSet,
            ICollection<RecordDraft> drafts,
            ref int order,
            GameContentAuthoringValidationResult validation)
        {
            IdleAutoDefenseProgressionAsset asset = contentSet.Progression;
            if (asset == null) return;
            drafts.Add(new RecordDraft(
                asset,
                asset.Id,
                "persistent-progression",
                asset.DisplayName,
                "Authoritative account tracks, research nodes, unlocks, and save document IDs.",
                asset.Tracks.Count + " tracks, " + asset.ResearchNodes.Count + " research nodes",
                order++,
                Array.Empty<GameContentRecordCapability>(),
                validation,
                new[]
                {
                    Metadata("Account Track", asset.AccountTrackId),
                    Metadata("Save Version", asset.SaveVersion.ToString(CultureInfo.InvariantCulture)),
                    Metadata("Profile / Run / Settings", asset.ProfileDocumentId + " / " + asset.RunDocumentId + " / " + asset.SettingsDocumentId)
                }));
            foreach (IdleAutoDefenseProgressionTrackRecord track in asset.Tracks.Where(value => value != null))
            {
                drafts.Add(new RecordDraft(
                    asset,
                    track.Id,
                    "persistent-progression",
                    track.DisplayName,
                    "Authored persistent progression track.",
                    track.CumulativeThresholds.Count + " level thresholds",
                    order++,
                    Array.Empty<GameContentRecordCapability>(),
                    validation,
                    new[]
                    {
                        Metadata("Starting Level", track.StartingLevel.ToString(CultureInfo.InvariantCulture)),
                        Metadata("Thresholds", string.Join(", ", track.CumulativeThresholds))
                    }));
            }
            foreach (IdleAutoDefenseResearchNodeRecord node in asset.ResearchNodes.Where(value => value != null))
            {
                var draft = new RecordDraft(
                    asset,
                    node.Id,
                    "persistent-progression",
                    node.DisplayName,
                    "Authored research cost, prerequisites, unlock gates, and applied effect.",
                    node.EffectKind + " " + Number(node.EffectAmountPerRank) + " per rank",
                    order++,
                    new[] { GameContentRecordCapabilities.MetaUpgrade },
                    validation,
                    new[]
                    {
                        Metadata("Max Rank", node.MaxRank.ToString(CultureInfo.InvariantCulture)),
                        Metadata("Costs", string.Join(", ", node.RankCosts)),
                        Metadata("Currency", node.CostCurrencyId),
                        Metadata("Effect Target", node.EffectTargetId),
                        Metadata("Required Unlocks", string.Join(", ", node.RequiredUnlockIds))
                    });
                draft.References.Add(ReferenceDraft.External(node.CostCurrencyId, "currencies", "spends currency", true, false));
                foreach (IdleAutoDefenseResearchPrerequisiteRecord prerequisite in node.Prerequisites.Where(value => value != null))
                    draft.References.Add(ReferenceDraft.External(prerequisite.NodeId, "persistent-progression", "requires research rank " + prerequisite.MinimumRank, true, false));
                AddProgressionTargetReference(contentSet, node.EffectTargetId, draft);
                drafts.Add(draft);
            }
        }

        private static void AddProgressionTargetReference(GameContentSetAsset contentSet, string targetId, RecordDraft draft)
        {
            WeaponDefinitionAsset weapon = contentSet.AvailableWeapons.FirstOrDefault(value => value != null && string.Equals(value.Id, targetId, StringComparison.OrdinalIgnoreCase));
            if (weapon != null)
            {
                draft.References.Add(ReferenceDraft.ToAsset(weapon, "weapons", "applies persistent effect", true));
                return;
            }
            if (contentSet.OfflineProgression != null && string.Equals(contentSet.OfflineProgression.Id, targetId, StringComparison.OrdinalIgnoreCase))
            {
                draft.References.Add(ReferenceDraft.ToAsset(contentSet.OfflineProgression, "offline-progression", "modifies offline settings", true));
                return;
            }
            if (contentSet.GameRules != null)
                draft.References.Add(ReferenceDraft.ToAsset(contentSet.GameRules, "game-rules", "applies objective rule", true));
        }

        private static void AddOfflineProgressionDraft(
            GameContentSetAsset contentSet,
            ICollection<RecordDraft> drafts,
            ref int order,
            GameContentAuthoringValidationResult validation)
        {
            IdleAutoDefenseOfflineProgressionAsset asset = contentSet.OfflineProgression;
            if (asset == null) return;
            var draft = new RecordDraft(
                asset,
                asset.Id,
                "offline-progression",
                asset.DisplayName,
                "Authoritative deterministic offline accumulation, cap, claim, rounding, and save key.",
                Number(asset.ProductionAmountPerSecond) + " " + asset.ProductionCurrencyId + "/second, capped at " + Number(asset.MaximumOfflineSeconds) + " seconds",
                order++,
                Array.Empty<GameContentRecordCapability>(),
                validation,
                new[]
                {
                    Metadata("Enabled", asset.Enabled ? "Yes" : "No"),
                    Metadata("Maximum Duration", Number(asset.MaximumOfflineSeconds) + " seconds"),
                    Metadata("Minimum Duration", Number(asset.MinimumEligibleSeconds) + " seconds"),
                    Metadata("Production", Number(asset.ProductionAmountPerSecond) + " " + asset.ProductionCurrencyId + " / second"),
                    Metadata("Cycle Reward", asset.CycleRewardAmount + " " + asset.CycleCurrencyId + " / " + Number(asset.CycleDurationSeconds) + " seconds"),
                    Metadata("Claim / Rounding", Number(asset.ClaimMultiplier) + "x / " + asset.Rounding),
                    Metadata("Save Timestamp Key", asset.SaveTimestampKey)
                });
            draft.References.Add(ReferenceDraft.External(asset.ProductionCurrencyId, "currencies", "produces currency", true, false));
            draft.References.Add(ReferenceDraft.External(asset.CycleCurrencyId, "currencies", "produces cycle currency", true, false));
            drafts.Add(draft);
        }

        private static void AddGameRulesDraft(
            GameContentSetAsset contentSet,
            ICollection<RecordDraft> drafts,
            ref int order,
            GameContentAuthoringValidationResult validation)
        {
            IdleAutoDefenseGameRulesAsset asset = contentSet.GameRules;
            if (asset == null) return;
            var draft = new RecordDraft(
                asset,
                asset.Id,
                "game-rules",
                asset.DisplayName,
                "Authoritative objective, spawn channels, module roles, projectile, repair, and Overdrive rules.",
                asset.Modules.Count + " module roles, " + asset.SpawnChannels.Count + " spawn channels",
                order++,
                Array.Empty<GameContentRecordCapability>(),
                validation,
                new[]
                {
                    Metadata("Objective", asset.ObjectiveId + ", " + Number(asset.ObjectiveMaximumHealth) + " HP, " + asset.ObjectiveLives + " lives"),
                    Metadata("Run / Offline Targets", asset.RunRewardTargetId + " / " + asset.OfflineRewardTargetId),
                    Metadata("Spawn Ring", Number(asset.SpawnRingRadius)),
                    Metadata("Elite / Boss", asset.EliteEnemyId + " / " + asset.BossEnemyId),
                    Metadata("Overdrive", Number(asset.OverdriveDurationSeconds) + " seconds, " + Number(asset.OverdriveDamageMultiplier) + "x damage")
                });
            foreach (IdleAutoDefenseModuleRule module in asset.Modules.Where(value => value != null && value.Weapon != null))
                draft.References.Add(ReferenceDraft.ToAsset(module.Weapon, "weapons", "assigns " + module.Role + " module", true));
            if (asset.EliteEnemy != null) draft.References.Add(ReferenceDraft.ToAsset(asset.EliteEnemy, "enemies", "assigns elite", true));
            if (asset.BossEnemy != null) draft.References.Add(ReferenceDraft.ToAsset(asset.BossEnemy, "enemies", "assigns boss", true));
            drafts.Add(draft);
        }

        private static GameContentAuthoringValidationResult AuthoredCoreValidationFor(GameContentSetAsset contentSet)
        {
            GameContentSetValidationReport report = GameContentSetValidator.Validate(contentSet);
            return new GameContentAuthoringValidationResult(report.Issues.Select(issue =>
                new GameContentAuthoringValidationIssue(
                    issue.Severity == GameContentSetValidationSeverity.Error
                        ? GameContentAuthoringValidationSeverity.Error
                        : issue.Severity == GameContentSetValidationSeverity.Warning
                            ? GameContentAuthoringValidationSeverity.Warning
                            : GameContentAuthoringValidationSeverity.Info,
                    issue.Path,
                    issue.Message)).ToArray());
        }

        private static void AddReferences(
            IReadOnlyList<RecordDraft> drafts,
            IReadOnlyDictionary<RecordDraft, GameContentRecordKey> keys)
        {
            Dictionary<UnityEngine.Object, RecordDraft> primaryByAsset = drafts
                .Where(value => value.Asset != null)
                .GroupBy(value => value.Asset)
                .ToDictionary(group => group.Key, group => group.First());
            Dictionary<string, RecordDraft> byId = drafts
                .Where(value => !string.IsNullOrWhiteSpace(value.Id))
                .GroupBy(value => value.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            foreach (RecordDraft draft in drafts)
            {
                foreach (ReferenceDraft reference in draft.References)
                {
                    RecordDraft target = null;
                    if (reference.TargetAsset != null)
                        primaryByAsset.TryGetValue(reference.TargetAsset, out target);
                    else if (!string.IsNullOrWhiteSpace(reference.TargetRecordId))
                        byId.TryGetValue(reference.TargetRecordId, out target);
                    if (target == null || !keys.TryGetValue(target, out GameContentRecordKey targetKey)) continue;
                    reference.TargetKey = targetKey;
                    reference.TargetRecordId = targetKey.SourceRecordId;
                    reference.Valid = true;
                }
            }
        }

        private static IReadOnlyList<GameContentRecordDescriptor> BuildDescriptors(
            IReadOnlyList<RecordDraft> drafts,
            IReadOnlyDictionary<RecordDraft, GameContentRecordKey> keys)
        {
            var inbound = drafts.ToDictionary(draft => draft, draft => new List<GameContentRecordReferenceDescriptor>());
            Dictionary<GameContentRecordKey, RecordDraft> draftsByKey = keys.ToDictionary(pair => pair.Value, pair => pair.Key);
            foreach (RecordDraft source in drafts)
            {
                foreach (ReferenceDraft reference in source.References.Where(value => value.TargetKey != null))
                {
                    if (!draftsByKey.TryGetValue(reference.TargetKey, out RecordDraft target)) continue;
                    GameContentRecordKey sourceKey = keys[source];
                    inbound[target].Add(new GameContentRecordReferenceDescriptor(
                        sourceKey.SourceRecordId,
                        source.CategoryId,
                        sourceKey.PackId,
                        reference.RelationshipLabel,
                        reference.Required,
                        true,
                        sourceKey.OwningPackageId,
                        sourceKey));
                }
            }

            return drafts.Select(draft => new GameContentRecordDescriptor(
                PackId + "::" + draft.CategoryId + "::" + draft.Id,
                draft.Id,
                draft.CategoryId,
                draft.CategoryIds,
                draft.DisplayName,
                draft.Description,
                draft.Summary,
                draft.Metadata,
                draft.Asset,
                AssetDatabase.GetAssetPath(draft.Asset),
                keys[draft].SourceId,
                draft.References.Select(reference => new GameContentRecordReferenceDescriptor(
                    reference.TargetRecordId,
                    reference.TargetCategoryId,
                    PackId,
                    reference.RelationshipLabel,
                    reference.Required,
                    reference.Valid,
                    OwningPackageId,
                    reference.TargetKey)).ToArray(),
                inbound[draft],
                draft.Validation,
                draft.Order,
                draft.Asset,
                draft.CategoryId,
                keys[draft],
                draft.Capabilities)).ToArray();
        }

        private static GameContentRecordKey BuildKey(
            RecordDraft draft,
            ICollection<GameContentAuthoringValidationIssue> issues)
        {
            string path = NormalizePath(AssetDatabase.GetAssetPath(draft.Asset));
            if (!GameContentSourceIdentity.TryCreate(draft.Asset, path, out GameContentSourceIdentity identity))
            {
                issues.Add(GameContentAuthoringValidationIssue.Error(
                    draft.CategoryId + "/" + draft.Id,
                    "Record source is not a persisted Unity asset and has no stable asset GUID."));
                return new GameContentRecordKey(OwningPackageId, PackId, draft.Id, "missing-source", path);
            }

            return new GameContentRecordKey(OwningPackageId, PackId, draft.Id, identity.StableKey, path);
        }

        private static void AddIdentityIssues(
            IReadOnlyList<RecordDraft> drafts,
            ICollection<GameContentAuthoringValidationIssue> issues)
        {
            foreach (RecordDraft draft in drafts.Where(value => string.IsNullOrWhiteSpace(value.Id)))
                issues.Add(GameContentAuthoringValidationIssue.Error(draft.CategoryId, "Authored record is missing its stable ID."));
            foreach (IGrouping<string, RecordDraft> duplicate in drafts.Where(value => !string.IsNullOrWhiteSpace(value.Id))
                         .GroupBy(value => value.Id, StringComparer.OrdinalIgnoreCase)
                         .Where(group => group.Count() > 1))
                issues.Add(GameContentAuthoringValidationIssue.Error(
                    "Records/" + duplicate.Key,
                    "Duplicate stable record ID appears in: " + string.Join(", ", duplicate.Select(value => AssetDatabase.GetAssetPath(value.Asset))) + "."));
        }

        private static GameContentAuthoringValidationResult ValidationFor(
            UnityEngine.Object asset,
            GameContentLibraryReport library)
        {
            GameContentLibraryItem item = library == null
                ? null
                : library.Items.FirstOrDefault(value => value.Asset == asset);
            if (item == null)
            {
                return new GameContentAuthoringValidationResult(new[]
                {
                    GameContentAuthoringValidationIssue.Error(
                        AssetDatabase.GetAssetPath(asset),
                        "Authored record was not discoverable from its generated content root.")
                });
            }

            return new GameContentAuthoringValidationResult(item.Issues.Select(issue =>
                new GameContentAuthoringValidationIssue(issue.Severity, issue.Path, issue.Message)).ToArray());
        }

        private static IReadOnlyList<GameContentSourceClaim> BuildSourceClaims(GameContentPackAsset pack)
        {
            if (pack == null) return Array.Empty<GameContentSourceClaim>();
            var objects = new List<UnityEngine.Object> { pack };
            foreach (GameContentSetAsset contentSet in pack.ContentSets.Where(value => value != null))
            {
                objects.Add(contentSet);
                foreach (WeaponDefinitionAsset weapon in contentSet.AvailableWeapons.Where(value => value != null))
                {
                    objects.Add(weapon);
                    objects.Add(weapon.Stats);
                    objects.Add(weapon.Presentation);
                    AttackDefinitionAsset attack = weapon.Stats == null ? null : weapon.Stats.Attack;
                    if (attack == null) continue;
                    objects.Add(attack);
                    objects.Add(attack.Mechanics);
                    objects.Add(attack.Targeting);
                    objects.Add(attack.Delivery);
                    objects.Add(attack.StatusEffects);
                    objects.Add(attack.Presentation);
                }

                foreach (EnemyDefinitionAsset enemy in contentSet.EnemyPool.Where(value => value != null))
                {
                    objects.Add(enemy);
                    objects.Add(enemy.Stats);
                    objects.Add(enemy.Presentation);
                }

                foreach (WaveDefinitionAsset wave in contentSet.WaveSet.Where(value => value != null))
                {
                    objects.Add(wave);
                    objects.Add(wave.Schedule);
                    objects.Add(wave.Entries);
                }

                foreach (RunUpgradeDefinitionAsset upgrade in contentSet.UpgradePool.Where(value => value != null))
                {
                    objects.Add(upgrade);
                    objects.Add(upgrade.Economy);
                    objects.Add(upgrade.Effects);
                }

                objects.Add(contentSet.RewardCatalog);
                objects.Add(contentSet.Economy);
                objects.Add(contentSet.RunProfile);
                objects.Add(contentSet.Progression);
                objects.Add(contentSet.OfflineProgression);
                objects.Add(contentSet.GameRules);
            }

            return objects.Where(value => value != null)
                .Select(GameContentSourceClaim.ForAsset)
                .Where(value => value != null && value.IsValid)
                .GroupBy(value => value.SourceIdentity.StableKey, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(value => value.SourcePath, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static void AddPackValidation(
            GameContentPackAsset pack,
            ICollection<GameContentAuthoringValidationIssue> issues)
        {
            GameContentPackValidationReport report = GameContentPackValidator.Validate(pack);
            for (int i = 0; i < report.Issues.Count; i++)
            {
                GameContentPackValidationIssue issue = report.Issues[i];
                GameContentAuthoringValidationSeverity severity = issue.Severity == GameContentPackValidationSeverity.Error
                    ? GameContentAuthoringValidationSeverity.Error
                    : issue.Severity == GameContentPackValidationSeverity.Warning
                        ? GameContentAuthoringValidationSeverity.Warning
                        : GameContentAuthoringValidationSeverity.Info;
                issues.Add(new GameContentAuthoringValidationIssue(severity, issue.Path, issue.Message));
            }
        }

        private static void AddExpectedCountIssues(
            IReadOnlyList<GameContentRecordDescriptor> records,
            ICollection<GameContentAuthoringValidationIssue> issues)
        {
            RequireCount(records, GameContentRecordCapabilities.Attack, 4, "Attack", issues);
            RequireCount(records, GameContentRecordCapabilities.Enemy, 6, "Enemy", issues);
            RequireCount(records, GameContentRecordCapabilities.Wave, 7, "Wave / Encounter", issues);
            RequireCount(records, GameContentRecordCapabilities.Weapon, 4, "Weapon / Tower", issues);
            RequireCount(records, GameContentRecordCapabilities.Upgrade, 6, "Upgrade", issues);
            RequireCategoryCount(records, "reward-choices", 37, "Reward Choice", issues);
            RequireCategoryCount(records, "reward-tables", 1, "Reward Table", issues);
            RequireCategoryCount(records, "normal-upgrades", 12, "Normal Upgrade", issues);
            RequireCategoryCount(records, "epic-upgrades", 12, "Epic Upgrade", issues);
            RequireCategoryCount(records, "legendary-upgrades", 4, "Legendary Upgrade", issues);
            RequireCategoryCount(records, "economy", 8, "Economy", issues);
            RequireCategoryCount(records, "currencies", 2, "Currency", issues);
            RequireCategoryCount(records, "run-profiles", 1, "Run Profile", issues);
            RequireCategoryCount(records, "persistent-progression", 6, "Persistent Progression", issues);
            RequireCategoryCount(records, "offline-progression", 1, "Offline Progression", issues);
            RequireCategoryCount(records, "game-rules", 1, "Game Rules", issues);
            if (records.Count != 82)
                issues.Add(GameContentAuthoringValidationIssue.Error(
                    "Record Counts/Total",
                    "Expected 82 total pack record(s), found " + records.Count.ToString(CultureInfo.InvariantCulture) + "."));
        }

        private static void RequireCount(
            IEnumerable<GameContentRecordDescriptor> records,
            GameContentRecordCapability capability,
            int expected,
            string label,
            ICollection<GameContentAuthoringValidationIssue> issues)
        {
            int count = records.Count(record => record.HasCapability(capability));
            if (count != expected)
                issues.Add(GameContentAuthoringValidationIssue.Error(
                    "Record Counts/" + label,
                    "Expected " + expected.ToString(CultureInfo.InvariantCulture) + " " + label + " record(s), found " + count.ToString(CultureInfo.InvariantCulture) + "."));
        }

        private static void RequireCategoryCount(
            IEnumerable<GameContentRecordDescriptor> records,
            string category,
            int expected,
            string label,
            ICollection<GameContentAuthoringValidationIssue> issues)
        {
            int count = records.Count(record => record.IsInCategory(category));
            if (count != expected)
                issues.Add(GameContentAuthoringValidationIssue.Error(
                    "Record Counts/" + label,
                    "Expected " + expected.ToString(CultureInfo.InvariantCulture) + " " + label + " record(s), found " + count.ToString(CultureInfo.InvariantCulture) + "."));
        }

        private static IReadOnlyList<GameContentCategoryDescriptor> BuildCategories(
            IReadOnlyList<GameContentRecordDescriptor> records)
        {
            string[] categories =
            {
                "attacks", "enemies", "waves", "weapons", "upgrades",
                "reward-choices", "normal-upgrades", "epic-upgrades", "legendary-upgrades", "reward-tables",
                "economy", "currencies", "run-profiles", "persistent-progression", "offline-progression", "game-rules"
            };
            string[] labels =
            {
                "Attack", "Enemy", "Wave / Encounter", "Weapon / Tower", "Upgrade",
                "Reward Choice", "Normal Upgrade", "Epic Upgrade", "Legendary Upgrade", "Reward Table",
                "Economy", "Currency", "Run Profile", "Persistent Progression", "Offline Progression", "Game Rules"
            };
            return categories.Select((category, index) => new GameContentCategoryDescriptor(
                category,
                labels[index],
                "Read-only authored " + labels[index] + " records from the generated Idle content graph.",
                category,
                index,
                records.Count(record => record.IsInCategory(category)))).ToArray();
        }

        private static string ResolveContentRoot(string packPath)
        {
            const string marker = "/ContentPacks/";
            int index = packPath.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index > 0) return packPath.Substring(0, index);
            string folder = Path.GetDirectoryName(packPath);
            return string.IsNullOrWhiteSpace(folder) ? GeneratedContentSearchRoot : NormalizePath(folder);
        }

        private static bool HasTag(IEnumerable<string> tags, string expected)
        {
            return tags != null && tags.Any(tag => string.Equals(tag, expected, StringComparison.OrdinalIgnoreCase));
        }

        private static GameContentMetadataDescriptor Metadata(string label, string value)
        {
            return new GameContentMetadataDescriptor(label, value ?? string.Empty);
        }

        private static string Fallback(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static string Number(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string NormalizePath(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().Replace("\\", "/");
        }

        private sealed class RecordDraft
        {
            public RecordDraft(
                UnityEngine.Object asset,
                string id,
                string categoryId,
                string displayName,
                string description,
                string summary,
                int order,
                IEnumerable<GameContentRecordCapability> capabilities,
                GameContentAuthoringValidationResult validation,
                IEnumerable<GameContentMetadataDescriptor> metadata,
                IEnumerable<string> categoryIds = null)
            {
                Asset = asset;
                Id = id ?? string.Empty;
                CategoryId = categoryId ?? string.Empty;
                CategoryIds = categoryIds == null
                    ? Array.Empty<string>()
                    : categoryIds.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                DisplayName = displayName ?? string.Empty;
                Description = description ?? string.Empty;
                Summary = summary ?? string.Empty;
                Order = order;
                Capabilities = capabilities == null ? Array.Empty<GameContentRecordCapability>() : capabilities.ToArray();
                Validation = validation ?? GameContentAuthoringValidationResult.Valid;
                Metadata = metadata == null ? Array.Empty<GameContentMetadataDescriptor>() : metadata.ToArray();
            }

            public UnityEngine.Object Asset { get; }
            public string Id { get; }
            public string CategoryId { get; }
            public IReadOnlyList<string> CategoryIds { get; }
            public string DisplayName { get; }
            public string Description { get; }
            public string Summary { get; }
            public int Order { get; }
            public IReadOnlyList<GameContentRecordCapability> Capabilities { get; }
            public GameContentAuthoringValidationResult Validation { get; }
            public IReadOnlyList<GameContentMetadataDescriptor> Metadata { get; }
            public List<ReferenceDraft> References { get; } = new List<ReferenceDraft>();
        }

        private sealed class ReferenceDraft
        {
            private ReferenceDraft(
                UnityEngine.Object targetAsset,
                string targetRecordId,
                string targetCategoryId,
                string relationshipLabel,
                bool required,
                bool valid)
            {
                TargetAsset = targetAsset;
                TargetRecordId = targetRecordId ?? string.Empty;
                TargetCategoryId = targetCategoryId ?? string.Empty;
                RelationshipLabel = relationshipLabel ?? string.Empty;
                Required = required;
                Valid = valid;
            }

            public UnityEngine.Object TargetAsset { get; }
            public string TargetRecordId { get; set; }
            public string TargetCategoryId { get; }
            public string RelationshipLabel { get; }
            public bool Required { get; }
            public bool Valid { get; set; }
            public GameContentRecordKey TargetKey { get; set; }

            public static ReferenceDraft ToAsset(
                UnityEngine.Object targetAsset,
                string category,
                string relationship,
                bool required)
            {
                return new ReferenceDraft(targetAsset, string.Empty, category, relationship, required, false);
            }

            public static ReferenceDraft External(
                string targetRecordId,
                string category,
                string relationship,
                bool required,
                bool valid)
            {
                return new ReferenceDraft(null, targetRecordId, category, relationship, required, valid);
            }
        }
    }
}
