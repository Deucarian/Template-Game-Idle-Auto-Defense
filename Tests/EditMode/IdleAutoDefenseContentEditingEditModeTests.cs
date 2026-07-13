using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Deucarian.Attacks.Authoring;
using Deucarian.GameContentAuthoring.Editor;
using Deucarian.RunUpgrades.Authoring;
using Deucarian.TemplateGameIdleAutoDefense.Editor;
using Deucarian.WeaponSystems.Authoring;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense.Tests
{
    public sealed class IdleAutoDefenseContentEditingEditModeTests
    {
        private readonly string[] _sceneRoots =
        {
            "Assets/OPEN_THIS_TO_TEST_IdleAutoDefense_PlayableGame",
            "Assets/OPEN_THIS_TO_TEST_ScrapFrontier_PlayableGame"
        };

        private string _targetRoot;
        private string _contentRoot;
        private string[] _sceneBackups;
        private bool _targetParentExisted;
        private bool _contentParentExisted;
        private GameContentPackAuthoringProvider _provider;

        [OneTimeSetUp]
        public void GenerateDisposableNamedPacks()
        {
            string suffix = Guid.NewGuid().ToString("N").Substring(0, 8);
            _targetRoot = "Assets/T/E" + suffix;
            _contentRoot = "Assets/GameContent/E" + suffix;
            _targetParentExisted = AssetDatabase.IsValidFolder("Assets/T");
            _contentParentExisted = AssetDatabase.IsValidFolder("Assets/GameContent");
            _sceneBackups = _sceneRoots.Select(BackupAssetDirectory).ToArray();
            var request = new IdleAutoDefenseTemplateSetupRequest
            {
                TargetRootAssetPath = _targetRoot,
                ContentRootAssetPath = _contentRoot,
                GameNamespace = "SafeEditingSmoke.IdleAutoDefense",
                GamePrefix = "Safe Editing Smoke",
                PackSelection = IdleAutoDefenseTemplatePackSelection.Both,
                OpenCreatedScene = false,
                RefreshAssetDatabase = false
            };

            IdleAutoDefenseTemplateSetupResult setup = IdleAutoDefenseTemplateSetupService.CreateGameFromTemplate(request);
            Assert.That(setup.Succeeded, Is.True, setup.CreateSummary());
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            _provider = new GameContentPackAuthoringProvider(_contentRoot);
            Assert.That(_provider.GetContentPacks().All(pack => pack.SourceState == GameContentPackSourceState.Available), Is.True);
        }

        [SetUp]
        public void SetUp()
        {
            GameContentEditSessionCoordinator.Shared.Reset();
            _provider = new GameContentPackAuthoringProvider(_contentRoot);
            Assert.That(_provider.GetContentPacks().All(pack => pack.SourceState == GameContentPackSourceState.Available), Is.True);
        }

        [TearDown]
        public void TearDown()
        {
            GameContentEditSessionCoordinator.Shared.Reset();
            Undo.ClearAll();
        }

        [OneTimeTearDown]
        public void DeleteDisposableNamedPacks()
        {
            GameContentEditSessionCoordinator.Shared.Reset();
            if (!string.IsNullOrWhiteSpace(_targetRoot)) AssetDatabase.DeleteAsset(_targetRoot);
            if (!string.IsNullOrWhiteSpace(_contentRoot)) AssetDatabase.DeleteAsset(_contentRoot);
            DeleteEmptyAssetFolderIfCreated("Assets/T", _targetParentExisted);
            DeleteEmptyAssetFolderIfCreated("Assets/GameContent", _contentParentExisted);
            if (_sceneBackups != null)
            {
                for (int i = 0; i < _sceneRoots.Length; i++)
                    RestoreAssetDirectory(_sceneRoots[i], _sceneBackups[i]);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        [Test]
        public void ScalarMappingsAreExplicitDeterministicAndPathPolicyIsClosed()
        {
            AssertMapping(
                IdleAutoDefenseNamedPackDefinition.Basic.PackId,
                GameContentRecordCapabilities.Attack,
                new[] { "attack.cooldownTicks", "attack.range", "attack.damage" },
                new[] { "_cooldownTicks", "_range", "_damageAmount" });
            AssertMapping(
                IdleAutoDefenseNamedPackDefinition.Basic.PackId,
                GameContentRecordCapabilities.Enemy,
                new[] { "enemy.maximumHealth", "enemy.moveSpeed", "enemy.rewardValue", "enemy.contactDamage", "enemy.collisionRadius" },
                new[] { "_maximumHealth", "_moveSpeed", "_rewardValue", "_contactDamage", "_collisionRadius" });
            AssertMapping(
                IdleAutoDefenseNamedPackDefinition.Basic.PackId,
                GameContentRecordCapabilities.Weapon,
                new[] { "weapon.cooldownTicks", "weapon.range", "weapon.burstCount", "weapon.volleyCount", "weapon.spreadDegrees", "weapon.buildCost" },
                new[] { "_cooldownTicks", "_range", "_burstCount", "_volleyCount", "_spreadDegrees", "_buildCost" });
            AssertMapping(
                IdleAutoDefenseNamedPackDefinition.Basic.PackId,
                GameContentRecordCapabilities.Upgrade,
                new[] { "upgrade.rarity", "upgrade.weight", "upgrade.maxRank" },
                new[] { "_rarity", "_weight", "_maxRank" });

            IdleAutoDefenseEditableSource attackSource = ResolveSource(
                IdleAutoDefenseNamedPackDefinition.Basic.PackId,
                GameContentRecordCapabilities.Attack);
            var descriptor = new GameContentFieldDescriptor(
                "test.missing",
                "test.missing",
                "Missing",
                "Test-only missing property.",
                GameContentFieldType.Number);
            var missing = new IdleAutoDefenseSerializedFieldMapping(descriptor, "_missing", SerializedPropertyType.Float);
            Assert.That(missing.TryRead(attackSource.SourceAsset, out _, out string missingReason), Is.False);
            Assert.That(missingReason, Does.Contain("not found"));
            var mismatch = new IdleAutoDefenseSerializedFieldMapping(descriptor, "_damageAmount", SerializedPropertyType.Integer);
            Assert.That(mismatch.TryRead(attackSource.SourceAsset, out _, out string mismatchReason), Is.False);
            Assert.That(mismatchReason, Does.Contain("instead of Integer"));

            Assert.That(IdleAutoDefenseWritableSourcePolicy.IsAllowedAssetPath(
                "Packages/com.deucarian.template.game.idle-auto-defense/TemplateSource~/Attack.asset",
                "Assets/GameContent/IdleAutoDefense",
                out _), Is.False);
            Assert.That(IdleAutoDefenseWritableSourcePolicy.IsAllowedAssetPath(
                "Library/PackageCache/com.deucarian.template/Attack.asset",
                "Assets/GameContent/IdleAutoDefense",
                out _), Is.False);
            Assert.That(IdleAutoDefenseWritableSourcePolicy.IsAllowedAssetPath(
                "Assets/../Packages/com.deucarian.template/Attack.asset",
                "Assets",
                out _), Is.False);
            Assert.That(IdleAutoDefenseWritableSourcePolicy.IsAllowedAssetPath(
                "Assets/GameContent/Other/Attack.asset",
                "Assets/GameContent/Selected",
                out _), Is.False);
        }

        [Test]
        public void NamedPacksExposeSupportedRecordsAndKeepUnsupportedContextsReadOnly()
        {
            foreach (string packId in new[]
                     {
                         IdleAutoDefenseNamedPackDefinition.Basic.PackId,
                         IdleAutoDefenseNamedPackDefinition.ScrapFrontier.PackId
                     })
            {
                GameContentPackDescriptor pack = GetPack(packId);
                Assert.That(pack.Access.CanEditExisting, Is.True);
                foreach (GameContentRecordCapability capability in new[]
                         {
                             GameContentRecordCapabilities.Attack,
                             GameContentRecordCapabilities.Enemy,
                             GameContentRecordCapabilities.Weapon,
                             GameContentRecordCapabilities.Upgrade
                         })
                {
                    GameContentRecordDescriptor record = GetRecord(packId, capability);
                    GameContentEditAvailability availability = _provider.CanEdit(Request(pack, record));
                    Assert.That(availability.IsEditable, Is.True, availability.DisabledReason);
                    Assert.That(availability.SupportedFieldCount, Is.GreaterThan(0));
                    Assert.That(availability.SourceTarget.ProjectRelativeDescription, Does.StartWith("Assets/"));
                }

                GameContentRecordDescriptor wave = GetRecord(packId, GameContentRecordCapabilities.Wave);
                GameContentEditAvailability waveAvailability = _provider.CanEdit(Request(pack, wave));
                Assert.That(waveAvailability.IsEditable, Is.False);
                Assert.That(waveAvailability.DisabledReason, Does.Contain("no approved direct scalar fields"));
            }

            GameContentPackDescriptor basic = GetPack(IdleAutoDefenseNamedPackDefinition.Basic.PackId);
            GameContentPackDescriptor scrap = GetPack(IdleAutoDefenseNamedPackDefinition.ScrapFrontier.PackId);
            GameContentRecordDescriptor basicAttack = GetRecord(basic.PackId, GameContentRecordCapabilities.Attack);
            var allPacksRequest = new GameContentEditRequest(
                GameContentPackContext.AllPacksSelectionKey,
                basicAttack.CanonicalKey,
                _provider.ProviderId);
            Assert.That(_provider.CanEdit(allPacksRequest).IsEditable, Is.False);
            var wrongPackRequest = new GameContentEditRequest(
                scrap.StableKey,
                basicAttack.CanonicalKey,
                _provider.ProviderId);
            Assert.That(_provider.CanEdit(wrongPackRequest).IsEditable, Is.False);
            var unownedKey = new GameContentRecordKey(
                basicAttack.CanonicalKey.OwningPackageId,
                basicAttack.CanonicalKey.PackId,
                basicAttack.CanonicalKey.SourceRecordId,
                "unity-asset-guid::not-claimed");
            Assert.That(_provider.CanEdit(new GameContentEditRequest(basic.StableKey, unownedKey, _provider.ProviderId)).IsEditable, Is.False);

            IdleAutoDefenseEditableSource basicSource = ResolveSource(basic.PackId, GameContentRecordCapabilities.Attack);
            IdleAutoDefenseEditableSource scrapSource = ResolveSource(scrap.PackId, GameContentRecordCapabilities.Attack);
            Assert.That(basicSource.SourceGuid, Is.Not.EqualTo(scrapSource.SourceGuid));
            Assert.That(basicSource.SourcePath, Does.StartWith(_contentRoot + "/Basic/"));
            Assert.That(scrapSource.SourcePath, Does.StartWith(_contentRoot + "/ScrapFrontier/"));

            string fullPath = IdleAutoDefenseWritableSourcePolicy.AssetPathToFullPath(basicSource.SourcePath);
            FileAttributes originalAttributes = File.GetAttributes(fullPath);
            try
            {
                File.SetAttributes(fullPath, originalAttributes | FileAttributes.ReadOnly);
                Assert.That(_provider.CanEdit(Request(basic, basicAttack)).IsEditable, Is.False);
            }
            finally
            {
                File.SetAttributes(fullPath, originalAttributes);
            }

            var projectProvider = new GameContentLibraryProvider();
            GameContentPackCatalog catalog = GameContentPackCatalog.Build(
                new IGameContentAuthoringProvider[] { projectProvider, _provider });
            Assert.That(catalog.SourceClaimConflicts, Is.Empty);
            GameContentPackCatalogEntry projectEntry = catalog.Find(GameContentPackDescriptor.BuildStableKey(
                "com.deucarian.game-content-authoring.project",
                "project-content"));
            Assert.That(projectEntry.Records.Any(record => record.SourcePath.StartsWith(_contentRoot + "/", StringComparison.OrdinalIgnoreCase)), Is.False);

            var missingProvider = new GameContentPackAuthoringProvider("Assets/GameContent/DefinitelyMissingSafeEditingRoot");
            foreach (GameContentPackDescriptor missingPack in missingProvider.GetContentPacks())
            {
                Assert.That(missingPack.Access.CanEditExisting, Is.False);
                Assert.That(missingPack.Access.DisabledReason, Does.Contain("Generate or repair"));
            }
        }

        [Test]
        public void StagingUndoRedoAndCancelNeverMutateTheLiveAsset()
        {
            IdleAutoDefenseEditableSource source = ResolveSource(
                IdleAutoDefenseNamedPackDefinition.Basic.PackId,
                GameContentRecordCapabilities.Enemy);
            GameContentEditRequest request = Request(GetPack(source.Index.Definition.PackId), source.Record);
            string beforeHash = FileHash(source.SourcePath);
            float originalHealth = ((EnemyStatsDefinitionAsset)source.SourceAsset).MaximumHealth;
            using (IGameContentEditSession session = _provider.BeginEdit(request))
            {
                Assert.That(session.State, Is.EqualTo(GameContentEditSessionState.Clean));
                Assert.That(session.Apply(
                    "enemy.maximumHealth",
                    GameContentFieldValue.FromNumber(originalHealth + 3f)).Succeeded, Is.True);
                Assert.That(session.State, Is.EqualTo(GameContentEditSessionState.Dirty));
                Assert.That(((EnemyStatsDefinitionAsset)source.SourceAsset).MaximumHealth, Is.EqualTo(originalHealth));
                Assert.That(FileHash(source.SourcePath), Is.EqualTo(beforeHash));
                Assert.That(EditorUtility.IsDirty(source.SourceAsset), Is.False);
                Assert.That(session.Undo().Succeeded, Is.True);
                Assert.That(session.State, Is.EqualTo(GameContentEditSessionState.Clean));
                Assert.That(session.Redo().Succeeded, Is.True);
                Assert.That(session.State, Is.EqualTo(GameContentEditSessionState.Dirty));
                Assert.That(session.Apply("enemy.maximumHealth", GameContentFieldValue.FromInteger(7)).Succeeded, Is.False);
                Assert.That(session.Apply("enemy.maximumHealth", GameContentFieldValue.FromNumber(double.NaN)).Succeeded, Is.False);
                Assert.That(session.Apply("enemy.maximumHealth", GameContentFieldValue.FromNumber(double.PositiveInfinity)).Succeeded, Is.False);
                Assert.That(session.Apply("enemy.maximumHealth", GameContentFieldValue.FromNumber(0d)).Succeeded, Is.False);
                Assert.That(session.Preview().CanCommit, Is.True);
                Assert.That(session.Rollback().Succeeded, Is.True);
            }

            Assert.That(((EnemyStatsDefinitionAsset)source.SourceAsset).MaximumHealth, Is.EqualTo(originalHealth));
            Assert.That(FileHash(source.SourcePath), Is.EqualTo(beforeHash));
            Assert.That(EditorUtility.IsDirty(source.SourceAsset), Is.False);
        }

        [Test]
        public void ProposedPackWarningsRequireConfirmationAndKeepStagedValues()
        {
            IdleAutoDefenseEditableSource source = ResolveSource(
                IdleAutoDefenseNamedPackDefinition.Basic.PackId,
                GameContentRecordCapabilities.Upgrade,
                record => ((RunUpgradeDefinitionAsset)record.SourceAsset).Economy.Costs.Length > 0);
            RunUpgradeEconomyDefinitionAsset economy = (RunUpgradeEconomyDefinitionAsset)source.SourceAsset;
            string beforeHash = FileHash(source.SourcePath);
            using (IGameContentEditSession session = _provider.BeginEdit(Request(GetPack(source.Index.Definition.PackId), source.Record)))
            {
                Assert.That(session.Apply(
                    "upgrade.maxRank",
                    GameContentFieldValue.FromInteger(economy.MaxRank + 1)).Succeeded, Is.True);
                GameContentValidationPreview preview = session.Preview();
                Assert.That(preview.CanCommit, Is.True);
                Assert.That(preview.RequiresWarningConfirmation, Is.True);
                Assert.That(preview.Issues.Any(issue => issue.Message.Contains("costs", StringComparison.OrdinalIgnoreCase)), Is.True);
                GameContentCommitResult commit = session.Commit(false);
                Assert.That(commit.Succeeded, Is.False);
                Assert.That(session.State, Is.EqualTo(GameContentEditSessionState.Dirty));
                Assert.That(session.Changes.Single().ProposedValue.IntegerValue, Is.EqualTo(economy.MaxRank + 1));
                Assert.That(session.Rollback().Succeeded, Is.True);
            }
            Assert.That(FileHash(source.SourcePath), Is.EqualTo(beforeHash));
        }

        [Test]
        public void InvalidAttackScalarsCannotBeStagedOrCommitted()
        {
            IdleAutoDefenseEditableSource source = ResolveSource(
                IdleAutoDefenseNamedPackDefinition.Basic.PackId,
                GameContentRecordCapabilities.Attack);
            string beforeHash = FileHash(source.SourcePath);
            using (IGameContentEditSession session = _provider.BeginEdit(
                       Request(GetPack(source.Index.Definition.PackId), source.Record)))
            {
                Assert.That(session.Apply(
                    "attack.damage",
                    GameContentFieldValue.FromNumber(0d)).Succeeded, Is.False);
                Assert.That(session.Apply(
                    "attack.range",
                    GameContentFieldValue.FromNumber(-1d)).Succeeded, Is.False);
                Assert.That(session.Apply(
                    "attack.damage",
                    GameContentFieldValue.FromNumber(double.NaN)).Succeeded, Is.False);
                Assert.That(session.Changes, Is.Empty);
                Assert.That(session.State, Is.EqualTo(GameContentEditSessionState.Clean));
                GameContentCommitResult commit = session.Commit(true);
                Assert.That(commit.Succeeded, Is.False);
                Assert.That(FileHash(source.SourcePath), Is.EqualTo(beforeHash));
                Assert.That(session.Rollback().Succeeded, Is.True);
            }

            Assert.That(FileHash(source.SourcePath), Is.EqualTo(beforeHash));
        }

        [Test]
        public void ExternalSourceChangeMakesSessionStaleAndBlocksCommit()
        {
            IdleAutoDefenseEditableSource source = ResolveSource(
                IdleAutoDefenseNamedPackDefinition.Basic.PackId,
                GameContentRecordCapabilities.Attack);
            GameContentEditRequest request = Request(GetPack(source.Index.Definition.PackId), source.Record);
            IReadOnlyDictionary<string, GameContentFieldValue> originals =
                IdleAutoDefenseContentEditMappings.ReadValues(source.SourceAsset, source.Mappings);
            byte[] beforeBytes = ReadFileBytes(source.SourcePath);
            string beforeHash = FileHash(source.SourcePath);
            IGameContentEditSession session = _provider.BeginEdit(request);
            try
            {
                double damage = originals["attack.damage"].NumberValue;
                Assert.That(session.Apply("attack.damage", GameContentFieldValue.FromNumber(damage + 1d)).Succeeded, Is.True);
                Dictionary<string, GameContentFieldValue> external = CopyValues(originals);
                external["attack.range"] = GameContentFieldValue.FromNumber(external["attack.range"].NumberValue + 0.5d);
                PersistValues(source, external);
                Assert.That(session.CheckStale().IsStale, Is.True);
                Assert.That(session.State, Is.EqualTo(GameContentEditSessionState.Stale));
                Assert.That(session.Commit(true).Succeeded, Is.False);
                Assert.That(session.Changes.Single(change => change.FieldId == "attack.damage").ProposedValue.NumberValue, Is.EqualTo(damage + 1d));
            }
            finally
            {
                session.Dispose();
                RestoreFileBytes(source.SourcePath, beforeBytes);
            }

            Assert.That(FileHash(source.SourcePath), Is.EqualTo(beforeHash));
            Assert.That(_provider.ValidatePack(source.Index.Definition.PackId).IsValid, Is.True);
        }

        [Test]
        public void BasicCommitUsesUnityUndoRedoRuntimeValueAndExactRollback()
        {
            GameContentPackDescriptor basic = GetPack(IdleAutoDefenseNamedPackDefinition.Basic.PackId);
            GameContentRecordDescriptor record = GetRecord(basic.PackId, GameContentRecordCapabilities.Attack);
            IdleAutoDefenseEditableSource source = ResolveSource(basic.PackId, GameContentRecordCapabilities.Attack);
            IdleAutoDefenseEditableSource scrapSource = ResolveSource(
                IdleAutoDefenseNamedPackDefinition.ScrapFrontier.PackId,
                GameContentRecordCapabilities.Attack);
            var attack = (AttackDefinitionAsset)record.SourceAsset;
            var mechanics = (AttackMechanicsDefinitionAsset)source.SourceAsset;
            float originalDamage = mechanics.DamageAmount;
            float committedDamage = originalDamage + 2f;
            string sourceHash = FileHash(source.SourcePath);
            string scrapHash = FileHash(scrapSource.SourcePath);
            string sourceGuid = source.SourceGuid;
            string objectId = source.GlobalObjectId;

            GameContentPackCatalog catalog = GameContentPackCatalog.Build(new IGameContentAuthoringProvider[] { _provider });
            GameContentPackContext context = new GameContentPackSelectionState().Select(catalog, basic.StableKey);
            record = context.Records.Single(candidate => candidate.CanonicalKey.Equals(record.CanonicalKey));
            using (var coordinator = new GameContentEditSessionCoordinator())
            {
                int refreshCount = 0;
                coordinator.RefreshRequested += () => refreshCount++;
                GameContentEditBeginResult begin = coordinator.BeginEdit(context, record, "attack");
                Assert.That(begin.Succeeded, Is.True, begin.Message);
                GameContentEditBeginResult attached = coordinator.BeginEdit(context, record, "combat");
                Assert.That(attached.AttachedExisting, Is.True);
                Assert.That(attached.Session, Is.SameAs(begin.Session));
                Assert.That(coordinator.ActiveSourceCount, Is.EqualTo(1));
                Assert.That(coordinator.Apply(
                    begin.Session,
                    "attack.damage",
                    GameContentFieldValue.FromNumber(committedDamage)).Succeeded, Is.True);
                Assert.That(mechanics.DamageAmount, Is.EqualTo(originalDamage));
                GameContentValidationPreview preview = coordinator.Preview(begin.Session);
                Assert.That(preview.CanCommit, Is.True, FormatIssues(preview));
                GameContentCommitResult commit = coordinator.Commit(begin.Session, true);
                Assert.That(commit.Succeeded, Is.True, commit.Message);
                Assert.That(refreshCount, Is.EqualTo(1));
                Assert.That(mechanics.DamageAmount, Is.EqualTo(committedDamage));
                Assert.That(attack.ToRuntimeDefinition().BaseDamage, Is.EqualTo(committedDamage));
                Assert.That(AssetDatabase.AssetPathToGUID(source.SourcePath), Is.EqualTo(sourceGuid));
                Assert.That(GlobalObjectId.GetGlobalObjectIdSlow(mechanics).ToString(), Is.EqualTo(objectId));
                Assert.That(FileHash(scrapSource.SourcePath), Is.EqualTo(scrapHash));

                Undo.PerformUndo();
                Assert.That(mechanics.DamageAmount, Is.EqualTo(originalDamage));
                Assert.That(coordinator.CheckStale(begin.Session).IsStale, Is.True);
                Undo.PerformRedo();
                Assert.That(mechanics.DamageAmount, Is.EqualTo(committedDamage));
                Assert.That(coordinator.CheckStale(begin.Session).IsStale, Is.False);
                Assert.That(begin.Session.State, Is.EqualTo(GameContentEditSessionState.Committed));

                GameContentRollbackResult rollback = coordinator.Rollback(begin.Session);
                Assert.That(rollback.Succeeded, Is.True, rollback.Message);
                Assert.That(coordinator.ActiveSourceCount, Is.Zero);
            }

            IdleAutoDefenseEditableSource restored = ResolveSource(basic.PackId, GameContentRecordCapabilities.Attack);
            Assert.That(((AttackMechanicsDefinitionAsset)restored.SourceAsset).DamageAmount, Is.EqualTo(originalDamage));
            Assert.That(FileHash(restored.SourcePath), Is.EqualTo(sourceHash));
            Assert.That(FileHash(scrapSource.SourcePath), Is.EqualTo(scrapHash));
            Assert.That(_provider.ValidatePack(basic.PackId).IsValid, Is.True);
        }

        [Test]
        public void ScrapCommitChangesOnlyScrapAndRuntimeConsumesMountedWeaponValue()
        {
            IdleAutoDefenseEditableSource scrapSource = ResolveSource(
                IdleAutoDefenseNamedPackDefinition.ScrapFrontier.PackId,
                GameContentRecordCapabilities.Weapon);
            IdleAutoDefenseEditableSource basicSource = ResolveSource(
                IdleAutoDefenseNamedPackDefinition.Basic.PackId,
                GameContentRecordCapabilities.Weapon);
            var weapon = (WeaponDefinitionAsset)scrapSource.Record.SourceAsset;
            var stats = (WeaponStatsDefinitionAsset)scrapSource.SourceAsset;
            int originalCooldown = stats.CooldownTicks;
            int committedCooldown = originalCooldown + 3;
            string scrapHash = FileHash(scrapSource.SourcePath);
            string basicHash = FileHash(basicSource.SourcePath);

            using (IGameContentEditSession session = _provider.BeginEdit(
                       Request(GetPack(scrapSource.Index.Definition.PackId), scrapSource.Record)))
            {
                Assert.That(session.Apply(
                    "weapon.cooldownTicks",
                    GameContentFieldValue.FromInteger(committedCooldown)).Succeeded, Is.True);
                GameContentValidationPreview preview = session.Preview();
                Assert.That(preview.CanCommit, Is.True, FormatIssues(preview));
                Assert.That(session.Commit(true).Succeeded, Is.True);
                Assert.That(stats.CooldownTicks, Is.EqualTo(committedCooldown));
                Assert.That(weapon.ToRuntimeDefinition().CooldownTicks, Is.EqualTo(committedCooldown));
                Assert.That(FileHash(basicSource.SourcePath), Is.EqualTo(basicHash));
                Assert.That(session.Rollback().Succeeded, Is.True);
            }

            IdleAutoDefenseEditableSource restored = ResolveSource(
                IdleAutoDefenseNamedPackDefinition.ScrapFrontier.PackId,
                GameContentRecordCapabilities.Weapon);
            Assert.That(((WeaponStatsDefinitionAsset)restored.SourceAsset).CooldownTicks, Is.EqualTo(originalCooldown));
            Assert.That(FileHash(restored.SourcePath), Is.EqualTo(scrapHash));
            Assert.That(FileHash(basicSource.SourcePath), Is.EqualTo(basicHash));
            Assert.That(_provider.ValidatePack(IdleAutoDefenseNamedPackDefinition.Basic.PackId).IsValid, Is.True);
            Assert.That(_provider.ValidatePack(IdleAutoDefenseNamedPackDefinition.ScrapFrontier.PackId).IsValid, Is.True);
        }

        [Test]
        public void ExplicitRollbackRefusesToOverwriteLaterExternalChange()
        {
            IdleAutoDefenseEditableSource source = ResolveSource(
                IdleAutoDefenseNamedPackDefinition.ScrapFrontier.PackId,
                GameContentRecordCapabilities.Upgrade);
            IReadOnlyDictionary<string, GameContentFieldValue> originals =
                IdleAutoDefenseContentEditMappings.ReadValues(source.SourceAsset, source.Mappings);
            byte[] beforeBytes = ReadFileBytes(source.SourcePath);
            string beforeHash = FileHash(source.SourcePath);
            IGameContentEditSession session = _provider.BeginEdit(
                Request(GetPack(source.Index.Definition.PackId), source.Record));
            try
            {
                long committedWeight = originals["upgrade.weight"].IntegerValue + 1;
                Assert.That(session.Apply("upgrade.weight", GameContentFieldValue.FromInteger(committedWeight)).Succeeded, Is.True);
                Assert.That(session.Commit(true).Succeeded, Is.True);
                IdleAutoDefenseEditableSource committedSource = ResolveSource(
                    IdleAutoDefenseNamedPackDefinition.ScrapFrontier.PackId,
                    GameContentRecordCapabilities.Upgrade,
                    record => record.CanonicalKey.Equals(source.Record.CanonicalKey));
                Dictionary<string, GameContentFieldValue> external = CopyValues(
                    IdleAutoDefenseContentEditMappings.ReadValues(committedSource.SourceAsset, committedSource.Mappings));
                long laterMaxRank = external["upgrade.maxRank"].IntegerValue + 1;
                external["upgrade.maxRank"] = GameContentFieldValue.FromInteger(laterMaxRank);
                PersistValues(committedSource, external);

                GameContentRollbackResult rollback = session.Rollback();
                Assert.That(rollback.Succeeded, Is.False);
                Assert.That(session.State, Is.EqualTo(GameContentEditSessionState.Stale));
                IdleAutoDefenseEditableSource afterRefusal = ResolveSource(
                    IdleAutoDefenseNamedPackDefinition.ScrapFrontier.PackId,
                    GameContentRecordCapabilities.Upgrade,
                    record => record.CanonicalKey.Equals(source.Record.CanonicalKey));
                IReadOnlyDictionary<string, GameContentFieldValue> afterValues =
                    IdleAutoDefenseContentEditMappings.ReadValues(afterRefusal.SourceAsset, afterRefusal.Mappings);
                Assert.That(afterValues["upgrade.weight"].IntegerValue, Is.EqualTo(committedWeight));
                Assert.That(afterValues["upgrade.maxRank"].IntegerValue, Is.EqualTo(laterMaxRank));
            }
            finally
            {
                session.Dispose();
                RestoreFileBytes(source.SourcePath, beforeBytes);
            }

            Assert.That(FileHash(source.SourcePath), Is.EqualTo(beforeHash));
        }

        [Test]
        public void CoordinatorSharesOnePhysicalSourceLockAndReleasesItOnCancel()
        {
            GameContentPackDescriptor basic = GetPack(IdleAutoDefenseNamedPackDefinition.Basic.PackId);
            GameContentRecordDescriptor attack = GetRecord(basic.PackId, GameContentRecordCapabilities.Attack);
            GameContentPackCatalog catalog = GameContentPackCatalog.Build(new IGameContentAuthoringProvider[] { _provider });
            GameContentPackContext context = new GameContentPackSelectionState().Select(catalog, basic.StableKey);
            attack = context.Records.Single(record => record.CanonicalKey.Equals(attack.CanonicalKey));
            using (var coordinator = new GameContentEditSessionCoordinator())
            {
                GameContentEditBeginResult first = coordinator.BeginEdit(context, attack, "attack");
                GameContentEditBeginResult second = coordinator.BeginEdit(context, attack, "weapon");
                Assert.That(first.Succeeded, Is.True, first.Message);
                Assert.That(second.Succeeded, Is.True, second.Message);
                Assert.That(second.AttachedExisting, Is.True);
                Assert.That(second.Session, Is.SameAs(first.Session));
                Assert.That(coordinator.ActiveSourceCount, Is.EqualTo(1));
                Assert.That(coordinator.Cancel(first.Session).Succeeded, Is.True);
                Assert.That(coordinator.ActiveSourceCount, Is.Zero);
            }
        }

        private void AssertMapping(
            string packId,
            GameContentRecordCapability capability,
            IReadOnlyList<string> expectedFieldIds,
            IReadOnlyList<string> expectedPropertyPaths)
        {
            IdleAutoDefenseEditableSource source = ResolveSource(packId, capability);
            Assert.That(source.Mappings.Select(mapping => mapping.Descriptor.FieldId), Is.EqualTo(expectedFieldIds));
            Assert.That(source.Mappings.Select(mapping => mapping.PropertyPath), Is.EqualTo(expectedPropertyPaths));
            Assert.That(source.Mappings.Select(mapping => mapping.Descriptor.Order), Is.Ordered);
            Assert.That(source.Mappings.Any(mapping =>
                mapping.PropertyPath.Contains("_id", StringComparison.OrdinalIgnoreCase) ||
                mapping.PropertyType == SerializedPropertyType.ObjectReference ||
                mapping.PropertyType == SerializedPropertyType.Generic), Is.False);
        }

        private GameContentPackDescriptor GetPack(string packId)
        {
            return _provider.GetContentPacks().Single(pack =>
                string.Equals(pack.PackId, packId, StringComparison.OrdinalIgnoreCase));
        }

        private GameContentRecordDescriptor GetRecord(
            string packId,
            GameContentRecordCapability capability,
            Func<GameContentRecordDescriptor, bool> predicate = null)
        {
            return _provider.GetRecords(packId).First(record =>
                record.HasCapability(capability) && (predicate == null || predicate(record)));
        }

        private IdleAutoDefenseEditableSource ResolveSource(
            string packId,
            GameContentRecordCapability capability,
            Func<GameContentRecordDescriptor, bool> predicate = null)
        {
            GameContentPackDescriptor pack = GetPack(packId);
            GameContentRecordDescriptor record = GetRecord(packId, capability, predicate);
            Assert.That(_provider.TryResolveEditableSource(Request(pack, record), out IdleAutoDefenseEditableSource source, out string reason), Is.True, reason);
            return source;
        }

        private GameContentEditRequest Request(GameContentPackDescriptor pack, GameContentRecordDescriptor record)
        {
            return new GameContentEditRequest(pack.StableKey, record.CanonicalKey, _provider.ProviderId);
        }

        private void PersistValues(
            IdleAutoDefenseEditableSource source,
            IReadOnlyDictionary<string, GameContentFieldValue> values)
        {
            IdleAutoDefenseContentEditMappings.ApplyValues(source.SourceAsset, source.Mappings, values, false);
            EditorUtility.SetDirty(source.SourceAsset);
            AssetDatabase.SaveAssetIfDirty(source.SourceAsset);
            AssetDatabase.ImportAsset(
                source.SourcePath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            _provider.RefreshAfterExternalEdit();
        }

        private static Dictionary<string, GameContentFieldValue> CopyValues(
            IReadOnlyDictionary<string, GameContentFieldValue> source)
        {
            return source.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        }

        private static string FormatIssues(GameContentValidationPreview preview)
        {
            return string.Join(
                Environment.NewLine,
                preview.Issues.Select(issue => issue.Path + ": " + issue.Message));
        }

        private static string FileHash(string assetPath)
        {
            string fullPath = IdleAutoDefenseWritableSourcePolicy.AssetPathToFullPath(assetPath);
            using (SHA256 sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(fullPath)))
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }
        }

        private static byte[] ReadFileBytes(string assetPath)
        {
            return File.ReadAllBytes(IdleAutoDefenseWritableSourcePolicy.AssetPathToFullPath(assetPath));
        }

        private void RestoreFileBytes(string assetPath, byte[] bytes)
        {
            File.WriteAllBytes(IdleAutoDefenseWritableSourcePolicy.AssetPathToFullPath(assetPath), bytes);
            AssetDatabase.ImportAsset(
                assetPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            _provider.RefreshAfterExternalEdit();
        }

        private static string BackupAssetDirectory(string assetRoot)
        {
            string fullPath = AssetPathToFullPath(assetRoot);
            bool hasDirectory = Directory.Exists(fullPath);
            bool hasMeta = File.Exists(fullPath + ".meta");
            if (!hasDirectory && !hasMeta) return string.Empty;
            string backup = Path.Combine(Path.GetTempPath(), "IdleSafeEditingBackup_" + Guid.NewGuid().ToString("N"));
            if (hasDirectory) CopyDirectory(fullPath, Path.Combine(backup, "Root"));
            if (hasMeta)
            {
                Directory.CreateDirectory(backup);
                File.Copy(fullPath + ".meta", Path.Combine(backup, "Root.meta"), true);
            }
            DeleteDirectoryIfExists(fullPath);
            return backup;
        }

        private static void RestoreAssetDirectory(string assetRoot, string backup)
        {
            string fullPath = AssetPathToFullPath(assetRoot);
            DeleteDirectoryIfExists(fullPath);
            if (string.IsNullOrWhiteSpace(backup)) return;
            string directory = Path.Combine(backup, "Root");
            string meta = Path.Combine(backup, "Root.meta");
            if (Directory.Exists(directory)) CopyDirectory(directory, fullPath);
            if (File.Exists(meta)) File.Copy(meta, fullPath + ".meta", true);
            DeleteDirectoryIfExists(backup);
        }

        private static string AssetPathToFullPath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static void DeleteDirectoryIfExists(string path)
        {
            if (Directory.Exists(path)) Directory.Delete(path, true);
            if (File.Exists(path + ".meta")) File.Delete(path + ".meta");
        }

        private static void DeleteEmptyAssetFolderIfCreated(string assetPath, bool existedBeforeTest)
        {
            if (existedBeforeTest) return;
            string fullPath = AssetPathToFullPath(assetPath);
            if (Directory.Exists(fullPath) && !Directory.EnumerateFileSystemEntries(fullPath).Any())
                AssetDatabase.DeleteAsset(assetPath);
        }

        private static void CopyDirectory(string sourcePath, string destinationPath)
        {
            Directory.CreateDirectory(destinationPath);
            string sourceFullPath = Path.GetFullPath(sourcePath);
            string[] files = Directory.GetFiles(sourceFullPath, "*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string relative = files[i].Substring(sourceFullPath.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string destination = Path.Combine(destinationPath, relative);
                string directory = Path.GetDirectoryName(destination);
                if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
                File.Copy(files[i], destination, true);
            }
        }
    }
}
