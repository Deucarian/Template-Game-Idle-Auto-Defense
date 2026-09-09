using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Deucarian.Attacks.Authoring;
using Deucarian.GameContentAuthoring.Editor;
using Deucarian.RunUpgrades.Authoring;
using Deucarian.TemplateGameIdleAutoDefense.Editor;
using Deucarian.WeaponSystems;
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
        public void EditMappingsAreExplicitDeterministicAndPathPolicyIsClosed()
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
                new[] { "weapon.attack", "weapon.cooldownTicks", "weapon.range", "weapon.burstCount", "weapon.volleyCount", "weapon.spreadDegrees", "weapon.buildCost" },
                new[] { "_attack", "_cooldownTicks", "_range", "_burstCount", "_volleyCount", "_spreadDegrees", "_buildCost" });
            AssertMapping(
                IdleAutoDefenseNamedPackDefinition.Basic.PackId,
                GameContentRecordCapabilities.Upgrade,
                new[] { "upgrade.rarity", "upgrade.weight", "upgrade.maxRank" },
                new[] { "_rarity", "_weight", "_maxRank" });

            IdleAutoDefenseEditableSource runProfileSource = ResolveRunProfileSource(
                IdleAutoDefenseNamedPackDefinition.Basic.PackId);
            Assert.That(runProfileSource.Mappings.Select(mapping => mapping.Descriptor.FieldId),
                Is.EqualTo(new[] { "runProfile.waves" }));
            Assert.That(runProfileSource.Mappings.Select(mapping => mapping.PropertyPath),
                Is.EqualTo(new[] { "_waves" }));
            Assert.That(runProfileSource.Mappings.Single().PropertyType, Is.EqualTo(SerializedPropertyType.Generic));

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

                GameContentRecordDescriptor runProfile = GetRunProfileRecord(packId);
                GameContentEditAvailability runProfileAvailability = _provider.CanEdit(Request(pack, runProfile));
                Assert.That(runProfileAvailability.IsEditable, Is.True, runProfileAvailability.DisabledReason);
                Assert.That(runProfileAvailability.SupportedFieldCount, Is.EqualTo(1));
                Assert.That(runProfileAvailability.SourceTarget.ProjectRelativeDescription, Does.StartWith("Assets/"));

                GameContentRecordDescriptor wave = GetRecord(packId, GameContentRecordCapabilities.Wave);
                GameContentEditAvailability waveAvailability = _provider.CanEdit(Request(pack, wave));
                Assert.That(waveAvailability.IsEditable, Is.False);
                Assert.That(waveAvailability.DisabledReason, Does.Contain("no approved direct fields"));
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
        public void PersistedAttackDiscoveryUsesOnlyExactNamedPackRoots()
        {
            string unrelatedPath = "Assets/T/UnrelatedAttack_" + Guid.NewGuid().ToString("N") + ".asset";
            var unrelated = ScriptableObject.CreateInstance<AttackDefinitionAsset>();
            try
            {
                AssetDatabase.CreateAsset(unrelated, unrelatedPath);
                AssetDatabase.ImportAsset(unrelatedPath, ImportAssetOptions.ForceSynchronousImport);
                _provider.RefreshAfterExternalEdit();

                foreach (string packId in new[]
                         {
                             IdleAutoDefenseNamedPackDefinition.Basic.PackId,
                             IdleAutoDefenseNamedPackDefinition.ScrapFrontier.PackId
                         })
                {
                    IdleAutoDefenseEditableSource source = ResolveSource(packId, GameContentRecordCapabilities.Attack);
                    GameContentRecordDescriptor[] attacks = source.Index.Records
                        .Where(record => record.HasCapability(GameContentRecordCapabilities.Attack))
                        .ToArray();
                    Assert.That(attacks, Has.Length.EqualTo(4));
                    Assert.That(attacks.Select(record => record.CanonicalKey).Distinct().Count(), Is.EqualTo(4));
                    Assert.That(attacks.All(record => record.SourceAsset is AttackDefinitionAsset), Is.True);
                    Assert.That(attacks.All(record => record.SourcePath.StartsWith(
                        source.Index.ContentRootPath + "/",
                        StringComparison.OrdinalIgnoreCase)), Is.True);
                    Assert.That(attacks.Any(record => record.SourceAsset == unrelated), Is.False);
                    foreach (GameContentRecordDescriptor attack in attacks)
                    {
                        Assert.That(GameContentSourceIdentity.TryCreate(
                            attack.SourceAsset,
                            attack.SourcePath,
                            out GameContentSourceIdentity identity), Is.True);
                        Assert.That(source.Index.SourceClaims.Count(claim =>
                            claim.SourceIdentity.Equals(identity)), Is.EqualTo(1));
                    }
                }
            }
            finally
            {
                AssetDatabase.DeleteAsset(unrelatedPath);
                _provider.RefreshAfterExternalEdit();
            }
        }

        [Test]
        public void PersistedAttackRemainsCanonicalWithoutAWeaponReference()
        {
            IdleAutoDefenseEditableSource source = ResolveStartingWeaponSource(
                IdleAutoDefenseNamedPackDefinition.Basic.PackId);
            var stats = (WeaponStatsDefinitionAsset)source.SourceAsset;
            AttackDefinitionAsset originalAttack = stats.Attack;
            GameContentRecordDescriptor originalRecord = source.Index.Records.Single(record =>
                record.SourceAsset == originalAttack && record.HasCapability(GameContentRecordCapabilities.Attack));
            GameContentRecordDescriptor replacement = GetAlternateCompatibleAttack(source);
            byte[] originalBytes = ReadFileBytes(source.SourcePath);
            try
            {
                PersistAttackReference(source, (AttackDefinitionAsset)replacement.SourceAsset);
                GameContentRecordDescriptor[] attacks = _provider.GetRecords(source.Index.Definition.PackId)
                    .Where(record => record.HasCapability(GameContentRecordCapabilities.Attack))
                    .ToArray();
                Assert.That(attacks, Has.Length.EqualTo(4));
                GameContentRecordDescriptor unreferenced = attacks.Single(record =>
                    record.CanonicalKey.Equals(originalRecord.CanonicalKey));
                Assert.That(unreferenced.SourceAsset, Is.SameAs(originalAttack));
                Assert.That(unreferenced.InboundReferences, Is.Empty);
            }
            finally
            {
                RestoreFileBytes(source.SourcePath, originalBytes);
            }
            Assert.That(_provider.ValidatePack(source.Index.Definition.PackId).IsValid, Is.True);
        }

        [Test]
        public void DuplicatePersistedAttackIdDisablesOnlyItsNamedPack()
        {
            IdleAutoDefenseEditableSource source = ResolveSource(
                IdleAutoDefenseNamedPackDefinition.Basic.PackId,
                GameContentRecordCapabilities.Attack);
            string duplicatePath = source.Index.ContentRootPath + "/Attacks/DuplicateAttackDefinition.asset";
            try
            {
                Assert.That(AssetDatabase.CopyAsset(source.Record.SourcePath, duplicatePath), Is.True);
                AssetDatabase.ImportAsset(duplicatePath, ImportAssetOptions.ForceSynchronousImport);
                _provider.RefreshAfterExternalEdit();
                GameContentPackDescriptor basic = GetPack(IdleAutoDefenseNamedPackDefinition.Basic.PackId);
                GameContentPackDescriptor scrap = GetPack(IdleAutoDefenseNamedPackDefinition.ScrapFrontier.PackId);
                Assert.That(basic.SourceState, Is.EqualTo(GameContentPackSourceState.ValidationFailed));
                Assert.That(basic.Validation.Issues.Any(issue =>
                    issue.Message.Contains("Duplicate stable record ID", StringComparison.OrdinalIgnoreCase)), Is.True);
                Assert.That(scrap.SourceState, Is.EqualTo(GameContentPackSourceState.Available));
            }
            finally
            {
                AssetDatabase.DeleteAsset(duplicatePath);
                _provider.RefreshAfterExternalEdit();
            }
            Assert.That(GetPack(IdleAutoDefenseNamedPackDefinition.Basic.PackId).SourceState,
                Is.EqualTo(GameContentPackSourceState.Available));
        }

        [Test]
        public void PersistedWaveDiscoveryUsesOnlyExactNamedPackRoots()
        {
            string unrelatedPath = "Assets/T/UnrelatedWave_" + Guid.NewGuid().ToString("N") + ".asset";
            var unrelated = ScriptableObject.CreateInstance<WaveDefinitionAsset>();
            try
            {
                AssetDatabase.CreateAsset(unrelated, unrelatedPath);
                AssetDatabase.ImportAsset(unrelatedPath, ImportAssetOptions.ForceSynchronousImport);
                _provider.RefreshAfterExternalEdit();

                foreach (string packId in new[]
                         {
                             IdleAutoDefenseNamedPackDefinition.Basic.PackId,
                             IdleAutoDefenseNamedPackDefinition.ScrapFrontier.PackId
                         })
                {
                    IdleAutoDefenseEditableSource source = ResolveRunProfileSource(packId);
                    GameContentRecordDescriptor[] waves = source.Index.Records
                        .Where(record => record.HasCapability(GameContentRecordCapabilities.Wave))
                        .ToArray();
                    Assert.That(waves, Has.Length.EqualTo(7));
                    Assert.That(waves.Select(record => record.CanonicalKey).Distinct().Count(), Is.EqualTo(7));
                    Assert.That(waves.All(record => record.SourceAsset is WaveDefinitionAsset), Is.True);
                    Assert.That(waves.All(record => record.SourcePath.StartsWith(
                        source.Index.ContentRootPath + "/",
                        StringComparison.OrdinalIgnoreCase)), Is.True);
                    Assert.That(waves.Any(record => record.SourceAsset == unrelated), Is.False);
                    foreach (GameContentRecordDescriptor wave in waves)
                    {
                        Assert.That(GameContentSourceIdentity.TryCreate(
                            wave.SourceAsset,
                            wave.SourcePath,
                            out GameContentSourceIdentity identity), Is.True);
                        Assert.That(source.Index.SourceClaims.Count(claim =>
                            claim.SourceIdentity.Equals(identity)), Is.EqualTo(1));
                    }
                }
            }
            finally
            {
                AssetDatabase.DeleteAsset(unrelatedPath);
                _provider.RefreshAfterExternalEdit();
            }
        }

        [Test]
        public void PersistedWaveRemainsCanonicalWithoutARunProfileReference()
        {
            IdleAutoDefenseEditableSource source = ResolveRunProfileSource(
                IdleAutoDefenseNamedPackDefinition.Basic.PackId);
            var profile = (IdleAutoDefenseRunProfileAsset)source.SourceAsset;
            WaveDefinitionAsset[] originalWaves = profile.Waves.ToArray();
            WaveDefinitionAsset removedWave = originalWaves[0];
            GameContentRecordDescriptor originalRecord = source.Index.Records.Single(record =>
                record.SourceAsset == removedWave && record.HasCapability(GameContentRecordCapabilities.Wave));
            byte[] originalBytes = ReadFileBytes(source.SourcePath);
            try
            {
                PersistWaveSequence(source, originalWaves.Skip(1).ToArray());
                GameContentRecordDescriptor[] waves = _provider.GetRecords(source.Index.Definition.PackId)
                    .Where(record => record.HasCapability(GameContentRecordCapabilities.Wave))
                    .ToArray();
                Assert.That(waves, Has.Length.EqualTo(7));
                GameContentRecordDescriptor unreferenced = waves.Single(record =>
                    record.CanonicalKey.Equals(originalRecord.CanonicalKey));
                Assert.That(unreferenced.SourceAsset, Is.SameAs(removedWave));
                Assert.That(unreferenced.InboundReferences, Is.Empty);
                Assert.That(_provider.ValidatePack(source.Index.Definition.PackId).IsValid, Is.True);
            }
            finally
            {
                RestoreFileBytes(source.SourcePath, originalBytes);
            }

            Assert.That(((IdleAutoDefenseRunProfileAsset)ResolveRunProfileSource(
                IdleAutoDefenseNamedPackDefinition.Basic.PackId).SourceAsset).Waves,
                Is.EqualTo(originalWaves));
        }

        [Test]
        public void DuplicatePersistedWaveIdDisablesOnlyItsNamedPack()
        {
            IdleAutoDefenseEditableSource source = ResolveRunProfileSource(
                IdleAutoDefenseNamedPackDefinition.Basic.PackId);
            GameContentRecordDescriptor wave = source.Index.Records.First(record =>
                record.HasCapability(GameContentRecordCapabilities.Wave));
            string duplicatePath = source.Index.ContentRootPath + "/Waves/DuplicateWaveDefinition.asset";
            try
            {
                Assert.That(AssetDatabase.CopyAsset(wave.SourcePath, duplicatePath), Is.True);
                AssetDatabase.ImportAsset(duplicatePath, ImportAssetOptions.ForceSynchronousImport);
                _provider.RefreshAfterExternalEdit();
                GameContentPackDescriptor basic = GetPack(IdleAutoDefenseNamedPackDefinition.Basic.PackId);
                GameContentPackDescriptor scrap = GetPack(IdleAutoDefenseNamedPackDefinition.ScrapFrontier.PackId);
                Assert.That(basic.SourceState, Is.EqualTo(GameContentPackSourceState.ValidationFailed));
                Assert.That(basic.Validation.Issues.Any(issue =>
                    issue.Message.Contains("Duplicate stable record ID", StringComparison.OrdinalIgnoreCase)), Is.True);
                Assert.That(scrap.SourceState, Is.EqualTo(GameContentPackSourceState.Available));
            }
            finally
            {
                AssetDatabase.DeleteAsset(duplicatePath);
                _provider.RefreshAfterExternalEdit();
            }
            Assert.That(GetPack(IdleAutoDefenseNamedPackDefinition.Basic.PackId).SourceState,
                Is.EqualTo(GameContentPackSourceState.Available));
        }

        [Test]
        public void WeaponAttackSelectorEnforcesCanonicalPackTypeClaimAndDeliveryRules()
        {
            GameContentPackDescriptor basic = GetPack(IdleAutoDefenseNamedPackDefinition.Basic.PackId);
            IdleAutoDefenseEditableSource source = ResolveStartingWeaponSource(basic.PackId);
            GameContentPackCatalog catalog = GameContentPackCatalog.Build(
                new IGameContentAuthoringProvider[] { _provider });
            GameContentPackContext context = new GameContentPackSelectionState().Select(catalog, basic.StableKey);
            GameContentRecordDescriptor weapon = context.Records.Single(record =>
                record.CanonicalKey.Equals(source.Record.CanonicalKey));
            using (var coordinator = new GameContentEditSessionCoordinator())
            {
                GameContentEditBeginResult begin = coordinator.BeginEdit(context, weapon, "weapon");
                Assert.That(begin.Succeeded, Is.True, begin.Message);
                GameContentFieldDescriptor field = begin.Session.Fields.Single(candidate =>
                    candidate.FieldId == "weapon.attack");
                Assert.That(field.FieldType, Is.EqualTo(GameContentFieldType.RecordReference));
                Assert.That(field.Required, Is.True);
                Assert.That(field.RecordReference.AllowClear, Is.False);
                Assert.That(field.RecordReference.PackPolicy, Is.EqualTo(GameContentReferencePackPolicy.SameSelectedPack));
                Assert.That(field.RecordReference.RequiredCapabilities,
                    Is.EqualTo(new[] { GameContentRecordCapabilities.Attack }));
                Assert.That(begin.Session.Snapshot.FieldValues["weapon.attack"].RecordReferenceValue.IsResolved, Is.True);

                GameContentReferenceCandidateSet candidates = coordinator.GetReferenceCandidates(
                    begin.Session,
                    "weapon.attack");
                Assert.That(candidates.Candidates.Count, Is.EqualTo(2));
                Assert.That(candidates.Candidates.All(candidate =>
                    candidate.Record.CanonicalKey.PackId == basic.PackId &&
                    candidate.Record.SourceAsset is AttackDefinitionAsset), Is.True);
                bool projectileWeapon = ((WeaponStatsDefinitionAsset)source.SourceAsset).FireMode == WeaponFireMode.Projectile;
                Assert.That(candidates.Candidates.All(candidate =>
                    (((AttackDefinitionAsset)candidate.Record.SourceAsset).Delivery.Mode == AttackRecipeDeliveryMode.Projectile) == projectileWeapon), Is.True);

                GameContentRecordDescriptor scrapAttack = GetRecord(
                    IdleAutoDefenseNamedPackDefinition.ScrapFrontier.PackId,
                    GameContentRecordCapabilities.Attack);
                Assert.That(coordinator.EvaluateReferenceTarget(
                    begin.Session,
                    "weapon.attack",
                    scrapAttack.CanonicalKey).IsValid, Is.False);

                GameContentRecordDescriptor enemy = GetRecord(
                    basic.PackId,
                    GameContentRecordCapabilities.Enemy);
                GameContentReferenceEvaluation wrongType = coordinator.EvaluateReferenceTarget(
                    begin.Session,
                    "weapon.attack",
                    enemy.CanonicalKey);
                Assert.That(wrongType.IsValid, Is.False);
                Assert.That(wrongType.RequiredCapabilitiesSatisfied, Is.False);

                GameContentRecordDescriptor incompatible = source.Index.Records.First(record =>
                    record.HasCapability(GameContentRecordCapabilities.Attack) &&
                    ((((AttackDefinitionAsset)record.SourceAsset).Delivery.Mode == AttackRecipeDeliveryMode.Projectile) != projectileWeapon));
                GameContentReferenceEvaluation wrongDelivery = coordinator.EvaluateReferenceTarget(
                    begin.Session,
                    "weapon.attack",
                    incompatible.CanonicalKey);
                Assert.That(wrongDelivery.IsValid, Is.False);
                Assert.That(wrongDelivery.Reason, Does.Contain(projectileWeapon ? "Projectile-delivery" : "non-Projectile"));

                var crafted = new GameContentRecordKey(
                    source.Record.CanonicalKey.OwningPackageId,
                    source.Record.CanonicalKey.PackId,
                    candidates.Candidates[0].Record.CanonicalKey.SourceRecordId,
                    "unity-asset-guid::crafted-or-missing-script");
                Assert.That(coordinator.EvaluateReferenceTarget(
                    begin.Session,
                    "weapon.attack",
                    crafted).IsValid, Is.False);
                Assert.That(coordinator.Apply(
                    begin.Session,
                    "weapon.attack",
                    GameContentFieldValue.FromRecordReference(GameContentRecordReferenceValue.None())).Succeeded, Is.False);
            }

            var transientAttack = ScriptableObject.CreateInstance<AttackDefinitionAsset>();
            var sceneObject = new GameObject("scene-attack-target");
            try
            {
                Assert.That(IdleAutoDefenseAttackReferencePolicy.TryResolveTarget(
                    source.Index,
                    transientAttack,
                    out _,
                    out _,
                    out string transientReason), Is.False);
                Assert.That(transientReason, Does.Contain("transient"));
                Assert.That(IdleAutoDefenseAttackReferencePolicy.TryResolveTarget(
                    source.Index,
                    sceneObject,
                    out _,
                    out _,
                    out _), Is.False);
                GameContentRecordDescriptor foreign = GetRecord(
                    IdleAutoDefenseNamedPackDefinition.ScrapFrontier.PackId,
                    GameContentRecordCapabilities.Attack);
                Assert.That(IdleAutoDefenseAttackReferencePolicy.TryResolveTarget(
                    source.Index,
                    foreign.SourceAsset,
                    out _,
                    out _,
                    out _), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sceneObject);
                UnityEngine.Object.DestroyImmediate(transientAttack);
            }
        }

        [Test]
        public void RunProfileWavesDescriptorAndSelectorAreCanonicalAndPackScoped()
        {
            foreach (string packId in new[]
                     {
                         IdleAutoDefenseNamedPackDefinition.Basic.PackId,
                         IdleAutoDefenseNamedPackDefinition.ScrapFrontier.PackId
                     })
            {
                GameContentPackDescriptor pack = GetPack(packId);
                IdleAutoDefenseEditableSource source = ResolveRunProfileSource(packId);
                GameContentPackCatalog catalog = GameContentPackCatalog.Build(
                    new IGameContentAuthoringProvider[] { _provider });
                GameContentPackContext context = new GameContentPackSelectionState().Select(catalog, pack.StableKey);
                GameContentRecordDescriptor runProfile = context.Records.Single(record =>
                    record.CanonicalKey.Equals(source.Record.CanonicalKey));
                using (var coordinator = new GameContentEditSessionCoordinator())
                {
                    GameContentEditBeginResult begin = coordinator.BeginEdit(context, runProfile, "run-profile");
                    Assert.That(begin.Succeeded, Is.True, begin.Message);
                    GameContentFieldDescriptor field = begin.Session.Fields.Single();
                    Assert.That(field.FieldId, Is.EqualTo("runProfile.waves"));
                    Assert.That(field.DisplayName, Is.EqualTo("Waves"));
                    Assert.That(field.FieldType, Is.EqualTo(GameContentFieldType.OrderedRecordReferenceCollection));
                    Assert.That(field.Required, Is.True);
                    Assert.That(field.Collection.MinimumCount, Is.EqualTo(1));
                    Assert.That(field.Collection.MaximumCount, Is.Null);
                    Assert.That(field.Collection.AllowDuplicates, Is.False);
                    Assert.That(field.Collection.OrderingDescription, Does.Contain("runtime-significant"));
                    Assert.That(field.Collection.RuntimeImpact.HasFlag(GameContentReferenceRuntimeImpact.Rebind), Is.True);
                    Assert.That(field.Collection.RuntimeImpact.HasFlag(GameContentReferenceRuntimeImpact.Restart), Is.True);
                    GameContentFieldDescriptor item = field.Collection.ItemDescriptor;
                    Assert.That(item.DisplayName, Is.EqualTo("Wave"));
                    Assert.That(item.Required, Is.True);
                    Assert.That(item.RecordReference.AllowClear, Is.False);
                    Assert.That(item.RecordReference.PackPolicy, Is.EqualTo(GameContentReferencePackPolicy.SameSelectedPack));
                    Assert.That(item.RecordReference.RequiredCapabilities,
                        Is.EqualTo(new[] { GameContentRecordCapabilities.Wave }));

                    GameContentOrderedCollectionValue original =
                        begin.Session.Snapshot.FieldValues[field.FieldId].OrderedCollectionValue;
                    Assert.That(original.Count, Is.EqualTo(7));
                    Assert.That(original.Items.Select(value => value.ItemKey).Distinct().Count(), Is.EqualTo(7));
                    Assert.That(original.Items.Select(value => value.OriginalIndex), Is.EqualTo(Enumerable.Range(0, 7)));
                    Assert.That(original.Items.All(value =>
                        value.Value.RecordReferenceValue.IsResolved &&
                        value.Value.RecordReferenceValue.TargetKey.PackId == packId), Is.True);

                    GameContentReferenceCandidateSet candidates = coordinator.GetReferenceCandidates(
                        begin.Session,
                        field.FieldId,
                        original.Items[0].ItemKey);
                    Assert.That(candidates.Candidates.Count, Is.EqualTo(1));
                    Assert.That(candidates.Candidates.All(candidate =>
                        candidate.Record.HasCapability(GameContentRecordCapabilities.Wave) &&
                        candidate.Record.CanonicalKey.PackId == packId &&
                        candidate.Record.SourceAsset is WaveDefinitionAsset), Is.True);

                    string otherPackId = packId == IdleAutoDefenseNamedPackDefinition.Basic.PackId
                        ? IdleAutoDefenseNamedPackDefinition.ScrapFrontier.PackId
                        : IdleAutoDefenseNamedPackDefinition.Basic.PackId;
                    GameContentRecordDescriptor foreignWave = GetRecord(otherPackId, GameContentRecordCapabilities.Wave);
                    Assert.That(coordinator.EvaluateReferenceTarget(
                        begin.Session,
                        field.FieldId,
                        foreignWave.CanonicalKey).IsValid, Is.False);

                    GameContentRecordDescriptor weapon = GetRecord(packId, GameContentRecordCapabilities.Weapon);
                    GameContentReferenceEvaluation wrongType = coordinator.EvaluateReferenceTarget(
                        begin.Session,
                        field.FieldId,
                        weapon.CanonicalKey);
                    Assert.That(wrongType.IsValid, Is.False);
                    Assert.That(wrongType.RequiredCapabilitiesSatisfied, Is.False);

                    var crafted = new GameContentRecordKey(
                        source.Record.CanonicalKey.OwningPackageId,
                        source.Record.CanonicalKey.PackId,
                        candidates.Candidates[0].Record.CanonicalKey.SourceRecordId,
                        "unity-asset-guid::crafted-wave");
                    Assert.That(coordinator.EvaluateReferenceTarget(
                        begin.Session,
                        field.FieldId,
                        crafted).IsValid, Is.False);
                }
            }

            IdleAutoDefenseEditableSource basicSource = ResolveRunProfileSource(
                IdleAutoDefenseNamedPackDefinition.Basic.PackId);
            var transientWave = ScriptableObject.CreateInstance<WaveDefinitionAsset>();
            var wrongTypeObject = new GameObject("scene-wave-target");
            try
            {
                Assert.That(IdleAutoDefenseWaveReferencePolicy.TryResolveTarget(
                    basicSource.Index,
                    transientWave,
                    out _,
                    out _,
                    out string transientReason), Is.False);
                Assert.That(transientReason, Does.Contain("transient"));
                Assert.That(IdleAutoDefenseWaveReferencePolicy.TryResolveTarget(
                    basicSource.Index,
                    wrongTypeObject,
                    out _,
                    out _,
                    out _), Is.False);
                GameContentRecordDescriptor foreign = GetRecord(
                    IdleAutoDefenseNamedPackDefinition.ScrapFrontier.PackId,
                    GameContentRecordCapabilities.Wave);
                Assert.That(IdleAutoDefenseWaveReferencePolicy.TryResolveTarget(
                    basicSource.Index,
                    foreign.SourceAsset,
                    out _,
                    out _,
                    out _), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(wrongTypeObject);
                UnityEngine.Object.DestroyImmediate(transientWave);
            }
        }

        [Test]
        public void WaveCollectionOperationsStageOnlyAndPreserveSessionItemIdentity()
        {
            GameContentPackDescriptor basic = GetPack(IdleAutoDefenseNamedPackDefinition.Basic.PackId);
            IdleAutoDefenseEditableSource source = ResolveRunProfileSource(basic.PackId);
            var profile = (IdleAutoDefenseRunProfileAsset)source.SourceAsset;
            WaveDefinitionAsset[] originalWaves = profile.Waves.ToArray();
            string beforeHash = FileHash(source.SourcePath);
            GameContentPackCatalog catalog = GameContentPackCatalog.Build(
                new IGameContentAuthoringProvider[] { _provider });
            GameContentPackContext context = new GameContentPackSelectionState().Select(catalog, basic.StableKey);
            GameContentRecordDescriptor record = context.Records.Single(candidate =>
                candidate.CanonicalKey.Equals(source.Record.CanonicalKey));

            using (var coordinator = new GameContentEditSessionCoordinator())
            {
                GameContentEditBeginResult begin = coordinator.BeginEdit(context, record, "run-profile");
                Assert.That(begin.Succeeded, Is.True, begin.Message);
                GameContentOrderedCollectionValue original = CurrentWaves(begin.Session);
                GameContentCollectionItem first = original.Items[0];
                GameContentCollectionItem second = original.Items[1];

                Assert.That(coordinator.ApplyCollectionOperation(
                    begin.Session,
                    "runProfile.waves",
                    GameContentCollectionOperation.Move(first.ItemKey, 1)).Succeeded, Is.True);
                GameContentOrderedCollectionValue moved = CurrentWaves(begin.Session);
                Assert.That(moved.Items[1].ItemKey, Is.EqualTo(first.ItemKey));
                Assert.That(moved.Items.Select(item => item.Value.RecordReferenceValue.TargetKey),
                    Is.EqualTo(new[]
                    {
                        second.Value.RecordReferenceValue.TargetKey,
                        first.Value.RecordReferenceValue.TargetKey
                    }.Concat(original.Items.Skip(2).Select(item => item.Value.RecordReferenceValue.TargetKey))));
                Assert.That(profile.Waves, Is.EqualTo(originalWaves));
                Assert.That(FileHash(source.SourcePath), Is.EqualTo(beforeHash));
                Assert.That(EditorUtility.IsDirty(profile), Is.False);

                Assert.That(coordinator.Undo(begin.Session).Succeeded, Is.True);
                Assert.That(CurrentWaves(begin.Session).Items.Select(item => item.ItemKey),
                    Is.EqualTo(original.Items.Select(item => item.ItemKey)));
                Assert.That(coordinator.Redo(begin.Session).Succeeded, Is.True);
                Assert.That(CurrentWaves(begin.Session).Items[1].ItemKey, Is.EqualTo(first.ItemKey));
                Assert.That(coordinator.RestoreOriginalCollectionOrder(
                    begin.Session,
                    "runProfile.waves").Succeeded, Is.True);
                Assert.That(CurrentWaves(begin.Session).Items.Select(item => item.Value),
                    Is.EqualTo(original.Items.Select(item => item.Value)));

                Assert.That(coordinator.ApplyCollectionOperation(
                    begin.Session,
                    "runProfile.waves",
                    GameContentCollectionOperation.Add(first.Value)).Succeeded, Is.False);
                Assert.That(coordinator.ApplyCollectionOperation(
                    begin.Session,
                    "runProfile.waves",
                    GameContentCollectionOperation.Remove(GameContentCollectionItemKey.Create())).Succeeded, Is.False);
                Assert.That(coordinator.ApplyCollectionOperation(
                    begin.Session,
                    "runProfile.waves",
                    GameContentCollectionOperation.Remove(first.ItemKey)).Succeeded, Is.True);
                Assert.That(coordinator.ApplyCollectionOperation(
                    begin.Session,
                    "runProfile.waves",
                    GameContentCollectionOperation.Add(first.Value)).Succeeded, Is.True);

                GameContentOrderedCollectionValue afterAdd = CurrentWaves(begin.Session);
                GameContentCollectionItem addedFirst = afterAdd.Items.Single(item =>
                    item.Value.Equals(first.Value));
                GameContentCollectionItem retainedSecond = afterAdd.Items.Single(item =>
                    item.Value.Equals(second.Value));
                Assert.That(addedFirst.IsAdded, Is.True);
                Assert.That(coordinator.ApplyCollectionOperation(
                    begin.Session,
                    "runProfile.waves",
                    GameContentCollectionOperation.Remove(retainedSecond.ItemKey)).Succeeded, Is.True);
                Assert.That(coordinator.ApplyCollectionOperation(
                    begin.Session,
                    "runProfile.waves",
                    GameContentCollectionOperation.Replace(addedFirst.ItemKey, second.Value)).Succeeded, Is.True);
                Assert.That(coordinator.Preview(begin.Session).CanCommit, Is.True);
                Assert.That(profile.Waves, Is.EqualTo(originalWaves));
                Assert.That(FileHash(source.SourcePath), Is.EqualTo(beforeHash));
                Assert.That(coordinator.Cancel(begin.Session).Succeeded, Is.True);
                Assert.That(coordinator.ActiveSourceCount, Is.Zero);
            }

            Assert.That(profile.Waves, Is.EqualTo(originalWaves));
            Assert.That(FileHash(source.SourcePath), Is.EqualTo(beforeHash));
        }

        [Test]
        public void WaveCollectionStructuralRulesRejectEmptyDuplicateBrokenAndCraftedOperations()
        {
            IdleAutoDefenseEditableSource source = ResolveRunProfileSource(
                IdleAutoDefenseNamedPackDefinition.Basic.PackId);
            string beforeHash = FileHash(source.SourcePath);
            using (IGameContentEditSession session = _provider.BeginEdit(
                       Request(GetPack(source.Index.Definition.PackId), source.Record)))
            {
                var collectionSession = (IGameContentOrderedCollectionEditSession)session;
                GameContentOrderedCollectionValue current = CurrentWaves(session);
                GameContentFieldValue duplicate = current.Items[0].Value;
                Assert.That(collectionSession.ApplyCollectionOperation(
                    "runProfile.waves",
                    GameContentCollectionOperation.Add(duplicate)).Succeeded, Is.False);
                Assert.That(collectionSession.ApplyCollectionOperation(
                    "runProfile.waves",
                    GameContentCollectionOperation.Add(GameContentFieldValue.FromRecordReference(
                        GameContentRecordReferenceValue.Broken("missing", "Missing Wave")))).Succeeded, Is.False);

                while ((current = CurrentWaves(session)).Count > 1)
                {
                    Assert.That(collectionSession.ApplyCollectionOperation(
                        "runProfile.waves",
                        GameContentCollectionOperation.Remove(current.Items[0].ItemKey)).Succeeded, Is.True);
                }
                Assert.That(collectionSession.ApplyCollectionOperation(
                    "runProfile.waves",
                    GameContentCollectionOperation.Remove(current.Items[0].ItemKey)).Succeeded, Is.False);
                Assert.That(collectionSession.ApplyCollectionOperation(
                    "runProfile.waves",
                    GameContentCollectionOperation.Remove(GameContentCollectionItemKey.Create())).Succeeded, Is.False);
                Assert.That(session.Rollback().Succeeded, Is.True);
            }

            Assert.That(FileHash(source.SourcePath), Is.EqualTo(beforeHash));
        }

        [Test]
        public void BasicAttackReferenceCommitIsUndoableRuntimeVisibleAndExactlyRollbackSafe()
        {
            IdleAutoDefenseEditableSource source = ResolveStartingWeaponSource(
                IdleAutoDefenseNamedPackDefinition.Basic.PackId);
            IdleAutoDefenseEditableSource scrapSource = ResolveStartingWeaponSource(
                IdleAutoDefenseNamedPackDefinition.ScrapFrontier.PackId);
            var weapon = (WeaponDefinitionAsset)source.Record.SourceAsset;
            var stats = (WeaponStatsDefinitionAsset)source.SourceAsset;
            AttackDefinitionAsset originalAttack = stats.Attack;
            GameContentRecordDescriptor originalRecord = source.Index.Records.Single(record =>
                record.SourceAsset == originalAttack && record.HasCapability(GameContentRecordCapabilities.Attack));
            GameContentRecordDescriptor replacementRecord = GetAlternateCompatibleAttack(source);
            var replacement = (AttackDefinitionAsset)replacementRecord.SourceAsset;
            byte[] originalBytes = ReadFileBytes(source.SourcePath);
            string originalHash = FileHash(source.SourcePath);
            string scrapHash = FileHash(scrapSource.SourcePath);
            string sourceGuid = source.SourceGuid;
            string sourceObjectId = source.GlobalObjectId;

            using (IGameContentEditSession session = _provider.BeginEdit(
                       Request(GetPack(source.Index.Definition.PackId), source.Record)))
            {
                Assert.That(session, Is.InstanceOf<IGameContentRecordReferenceEditSession>());
                Assert.That(session.Fields.Where(field => field.FieldType == GameContentFieldType.RecordReference)
                    .Select(field => field.FieldId), Is.EqualTo(new[] { "weapon.attack" }));
                Assert.That(session.Apply(
                    "weapon.attack",
                    ReferenceValue(replacementRecord)).Succeeded, Is.True);
                Assert.That(stats.Attack, Is.SameAs(originalAttack));
                Assert.That(FileHash(source.SourcePath), Is.EqualTo(originalHash));
                GameContentValidationPreview preview = session.Preview();
                Assert.That(preview.CanCommit, Is.True, FormatIssues(preview));
                GameContentCommitResult commit = session.Commit(true);
                Assert.That(commit.Succeeded, Is.True, commit.Message);
                Assert.That(stats.Attack, Is.SameAs(replacement));
                Assert.That(weapon.ToRuntimeDefinition().AttackDefinitionId.Value, Is.EqualTo(replacement.Id));
                Assert.That(AssetDatabase.AssetPathToGUID(source.SourcePath), Is.EqualTo(sourceGuid));
                Assert.That(GlobalObjectId.GetGlobalObjectIdSlow(stats).ToString(), Is.EqualTo(sourceObjectId));
                Assert.That(FileHash(scrapSource.SourcePath), Is.EqualTo(scrapHash));

                GameContentRecordDescriptor canonicalOriginal = _provider.GetRecords(source.Index.Definition.PackId)
                    .Single(record => record.CanonicalKey.Equals(originalRecord.CanonicalKey));
                Assert.That(canonicalOriginal.SourceAsset, Is.SameAs(originalAttack));
                Assert.That(canonicalOriginal.InboundReferences, Is.Empty);
                Assert.That(_provider.GetSourceClaims(source.Index.Definition.PackId).Any(claim =>
                    claim.SourcePath == originalRecord.SourcePath), Is.True);
                GameContentPackResolution resolution = GameContentPackValidator.Resolve(
                    source.Index.PackAsset,
                    source.Index.ContentSetAsset);
                Assert.That(resolution.IsValid, Is.True);
                Assert.That(resolution.ContentSetResolution.AttackRecipes, Does.Contain(replacement));
                AssertStrictRuntimeConsumes(source.Index.ContentSetAsset, replacement.Id);

                Undo.PerformUndo();
                Assert.That(stats.Attack, Is.SameAs(originalAttack));
                Assert.That(session.CheckStale().IsStale, Is.True);
                Undo.PerformRedo();
                Assert.That(stats.Attack, Is.SameAs(replacement));
                Assert.That(session.CheckStale().IsStale, Is.False);
                Assert.That(session.Rollback().Succeeded, Is.True);
            }

            Assert.That(FileHash(source.SourcePath), Is.EqualTo(originalHash));
            Assert.That(ReadFileBytes(source.SourcePath), Is.EqualTo(originalBytes));
            Assert.That(FileHash(scrapSource.SourcePath), Is.EqualTo(scrapHash));
            Assert.That(_provider.ValidatePack(source.Index.Definition.PackId).IsValid, Is.True);
        }

        [Test]
        public void ScrapAttackReferenceCommitCannotAlterBasic()
        {
            IdleAutoDefenseEditableSource scrap = ResolveStartingWeaponSource(
                IdleAutoDefenseNamedPackDefinition.ScrapFrontier.PackId);
            IdleAutoDefenseEditableSource basic = ResolveStartingWeaponSource(
                IdleAutoDefenseNamedPackDefinition.Basic.PackId);
            var stats = (WeaponStatsDefinitionAsset)scrap.SourceAsset;
            AttackDefinitionAsset original = stats.Attack;
            GameContentRecordDescriptor replacement = GetAlternateCompatibleAttack(scrap);
            string scrapHash = FileHash(scrap.SourcePath);
            string basicHash = FileHash(basic.SourcePath);
            using (IGameContentEditSession session = _provider.BeginEdit(
                       Request(GetPack(scrap.Index.Definition.PackId), scrap.Record)))
            {
                Assert.That(session.Apply("weapon.attack", ReferenceValue(replacement)).Succeeded, Is.True);
                Assert.That(session.Commit(true).Succeeded, Is.True);
                Assert.That(stats.Attack, Is.SameAs(replacement.SourceAsset));
                Assert.That(FileHash(basic.SourcePath), Is.EqualTo(basicHash));
                Assert.That(session.Rollback().Succeeded, Is.True);
            }
            Assert.That(stats.Attack, Is.SameAs(original));
            Assert.That(FileHash(scrap.SourcePath), Is.EqualTo(scrapHash));
            Assert.That(FileHash(basic.SourcePath), Is.EqualTo(basicHash));
        }

        [Test]
        public void DisappearedAttackTargetBlocksCommitWithoutChangingWeapon()
        {
            IdleAutoDefenseEditableSource source = ResolveStartingWeaponSource(
                IdleAutoDefenseNamedPackDefinition.Basic.PackId);
            GameContentRecordDescriptor replacement = GetAlternateCompatibleAttack(source);
            string originalTargetPath = replacement.SourcePath;
            string temporaryTargetPath = AssetDatabase.GenerateUniqueAssetPath(
                _targetRoot + "/Moved_" + Path.GetFileName(originalTargetPath));
            byte[] sourceBytes = ReadFileBytes(source.SourcePath);
            IGameContentEditSession session = _provider.BeginEdit(
                Request(GetPack(source.Index.Definition.PackId), source.Record));
            try
            {
                Assert.That(session.Apply("weapon.attack", ReferenceValue(replacement)).Succeeded, Is.True);
                Assert.That(AssetDatabase.MoveAsset(originalTargetPath, temporaryTargetPath), Is.Empty);
                _provider.RefreshAfterExternalEdit();
                Assert.That(session.Commit(true).Succeeded, Is.False);
                Assert.That(session.State, Is.EqualTo(GameContentEditSessionState.Stale));
                Assert.That(ReadFileBytes(source.SourcePath), Is.EqualTo(sourceBytes));
            }
            finally
            {
                session.Dispose();
                Assert.That(AssetDatabase.MoveAsset(temporaryTargetPath, originalTargetPath), Is.Empty);
                _provider.RefreshAfterExternalEdit();
            }
            GameContentAuthoringValidationResult validation = _provider.ValidatePack(source.Index.Definition.PackId);
            Assert.That(
                validation.IsValid,
                Is.True,
                string.Join(Environment.NewLine, validation.Issues.Select(issue => issue.Path + ": " + issue.Message)));
        }

        [Test]
        public void InvalidatedAttackTargetBlocksPreviewAndCommit()
        {
            IdleAutoDefenseEditableSource source = ResolveStartingWeaponSource(
                IdleAutoDefenseNamedPackDefinition.Basic.PackId);
            GameContentRecordDescriptor replacement = GetAlternateCompatibleAttack(source);
            var attack = (AttackDefinitionAsset)replacement.SourceAsset;
            string mechanicsPath = AssetDatabase.GetAssetPath(attack.Mechanics);
            byte[] mechanicsBytes = ReadFileBytes(mechanicsPath);
            byte[] sourceBytes = ReadFileBytes(source.SourcePath);
            IGameContentEditSession session = _provider.BeginEdit(
                Request(GetPack(source.Index.Definition.PackId), source.Record));
            try
            {
                Assert.That(session.Apply("weapon.attack", ReferenceValue(replacement)).Succeeded, Is.True);
                var serialized = new SerializedObject(attack.Mechanics);
                serialized.Update();
                serialized.FindProperty("_damageAmount").floatValue = 0f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(attack.Mechanics);
                AssetDatabase.SaveAssetIfDirty(attack.Mechanics);
                AssetDatabase.ImportAsset(mechanicsPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                _provider.RefreshAfterExternalEdit();
                Assert.That(session.Preview().CanCommit, Is.False);
                Assert.That(session.Commit(true).Succeeded, Is.False);
                Assert.That(ReadFileBytes(source.SourcePath), Is.EqualTo(sourceBytes));
            }
            finally
            {
                session.Dispose();
                RestoreFileBytes(mechanicsPath, mechanicsBytes);
            }
            Assert.That(_provider.ValidatePack(source.Index.Definition.PackId).IsValid, Is.True);
        }

        [Test]
        public void ReferenceRollbackRefusesToOverwriteLaterSourceEdit()
        {
            IdleAutoDefenseEditableSource source = ResolveStartingWeaponSource(
                IdleAutoDefenseNamedPackDefinition.ScrapFrontier.PackId);
            GameContentRecordDescriptor replacement = GetAlternateCompatibleAttack(source);
            var stats = (WeaponStatsDefinitionAsset)source.SourceAsset;
            byte[] originalBytes = ReadFileBytes(source.SourcePath);
            int laterCooldown = stats.CooldownTicks + 1;
            IGameContentEditSession session = _provider.BeginEdit(
                Request(GetPack(source.Index.Definition.PackId), source.Record));
            try
            {
                Assert.That(session.Apply("weapon.attack", ReferenceValue(replacement)).Succeeded, Is.True);
                Assert.That(session.Commit(true).Succeeded, Is.True);
                var serialized = new SerializedObject(stats);
                serialized.Update();
                serialized.FindProperty("_cooldownTicks").intValue = laterCooldown;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(stats);
                AssetDatabase.SaveAssetIfDirty(stats);
                AssetDatabase.ImportAsset(source.SourcePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                _provider.RefreshAfterExternalEdit();
                GameContentRollbackResult rollback = session.Rollback();
                Assert.That(rollback.Succeeded, Is.False);
                Assert.That(session.State, Is.EqualTo(GameContentEditSessionState.Stale));
                Assert.That(stats.Attack, Is.SameAs(replacement.SourceAsset));
                Assert.That(stats.CooldownTicks, Is.EqualTo(laterCooldown));
            }
            finally
            {
                session.Dispose();
                RestoreFileBytes(source.SourcePath, originalBytes);
            }
            Assert.That(_provider.ValidatePack(source.Index.Definition.PackId).IsValid, Is.True);
        }

        [Test]
        public void BasicWaveCollectionCommitIsUndoableRuntimeVisibleAndExactlyRollbackSafe()
        {
            AssertWaveCollectionCommitIsSafeAndIsolated(
                IdleAutoDefenseNamedPackDefinition.Basic.PackId,
                IdleAutoDefenseNamedPackDefinition.ScrapFrontier.PackId);
        }

        [Test]
        public void ScrapWaveCollectionCommitIsUndoableRuntimeVisibleAndExactlyRollbackSafe()
        {
            AssertWaveCollectionCommitIsSafeAndIsolated(
                IdleAutoDefenseNamedPackDefinition.ScrapFrontier.PackId,
                IdleAutoDefenseNamedPackDefinition.Basic.PackId);
        }

        [Test]
        public void WaveCollectionRollbackRefusesToOverwriteLaterSourceEdit()
        {
            IdleAutoDefenseEditableSource source = ResolveRunProfileSource(
                IdleAutoDefenseNamedPackDefinition.Basic.PackId);
            WaveDefinitionAsset[] original = ((IdleAutoDefenseRunProfileAsset)source.SourceAsset).Waves.ToArray();
            byte[] beforeBytes = ReadFileBytes(source.SourcePath);
            string beforeHash = FileHash(source.SourcePath);
            IGameContentEditSession session = _provider.BeginEdit(
                Request(GetPack(source.Index.Definition.PackId), source.Record));
            try
            {
                var collectionSession = (IGameContentOrderedCollectionEditSession)session;
                GameContentCollectionItem first = CurrentWaves(session).Items[0];
                Assert.That(collectionSession.ApplyCollectionOperation(
                    "runProfile.waves",
                    GameContentCollectionOperation.Move(first.ItemKey, 1)).Succeeded, Is.True);
                Assert.That(session.Commit(true).Succeeded, Is.True);

                IdleAutoDefenseEditableSource committedSource = ResolveRunProfileSource(
                    IdleAutoDefenseNamedPackDefinition.Basic.PackId);
                WaveDefinitionAsset[] laterSequence = original.Skip(1).Concat(original.Take(1)).ToArray();
                PersistWaveSequence(committedSource, laterSequence);
                GameContentRollbackResult rollback = session.Rollback();
                Assert.That(rollback.Succeeded, Is.False);
                Assert.That(session.State, Is.EqualTo(GameContentEditSessionState.Stale));
                Assert.That(((IdleAutoDefenseRunProfileAsset)ResolveRunProfileSource(
                    IdleAutoDefenseNamedPackDefinition.Basic.PackId).SourceAsset).Waves,
                    Is.EqualTo(laterSequence));
            }
            finally
            {
                session.Dispose();
                RestoreFileBytes(source.SourcePath, beforeBytes);
            }

            Assert.That(FileHash(source.SourcePath), Is.EqualTo(beforeHash));
            Assert.That(((IdleAutoDefenseRunProfileAsset)ResolveRunProfileSource(
                IdleAutoDefenseNamedPackDefinition.Basic.PackId).SourceAsset).Waves,
                Is.EqualTo(original));
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
                IdleAutoDefenseContentEditMappings.ReadValues(source.SourceAsset, source.Mappings, source.Index);
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
                IdleAutoDefenseContentEditMappings.ReadValues(source.SourceAsset, source.Mappings, source.Index);
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
                    IdleAutoDefenseContentEditMappings.ReadValues(
                        committedSource.SourceAsset,
                        committedSource.Mappings,
                        committedSource.Index));
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
                    IdleAutoDefenseContentEditMappings.ReadValues(
                        afterRefusal.SourceAsset,
                        afterRefusal.Mappings,
                        afterRefusal.Index);
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

        private IdleAutoDefenseEditableSource ResolveStartingWeaponSource(string packId)
        {
            return ResolveSource(
                packId,
                GameContentRecordCapabilities.Weapon,
                record => record.SourceAsset == GetPackIndex(packId).ContentSetAsset.StartingWeapon);
        }

        private IdleAutoDefenseEditableSource ResolveRunProfileSource(string packId)
        {
            GameContentPackDescriptor pack = GetPack(packId);
            GameContentRecordDescriptor record = GetRunProfileRecord(packId);
            Assert.That(_provider.TryResolveEditableSource(
                Request(pack, record),
                out IdleAutoDefenseEditableSource source,
                out string reason), Is.True, reason);
            return source;
        }

        private GameContentRecordDescriptor GetRunProfileRecord(string packId)
        {
            return _provider.GetRecords(packId).Single(record => record.IsInCategory("run-profiles"));
        }

        private IdleAutoDefenseContentPackIndex GetPackIndex(string packId)
        {
            IdleAutoDefenseEditableSource source = ResolveSource(packId, GameContentRecordCapabilities.Attack);
            return source.Index;
        }

        private static GameContentRecordDescriptor GetAlternateCompatibleAttack(
            IdleAutoDefenseEditableSource source)
        {
            var stats = (WeaponStatsDefinitionAsset)source.SourceAsset;
            bool projectile = stats.FireMode == WeaponFireMode.Projectile;
            return source.Index.Records.First(record =>
                record.HasCapability(GameContentRecordCapabilities.Attack) &&
                record.SourceAsset != stats.Attack &&
                record.SourceAsset is AttackDefinitionAsset attack &&
                attack.Delivery != null &&
                (attack.Delivery.Mode == AttackRecipeDeliveryMode.Projectile) == projectile);
        }

        private static GameContentFieldValue ReferenceValue(GameContentRecordDescriptor target)
        {
            return GameContentFieldValue.FromRecordReference(
                GameContentRecordReferenceValue.Resolved(
                    target.CanonicalKey,
                    target.DisplayName,
                    target.SourcePath));
        }

        private static GameContentOrderedCollectionValue CurrentWaves(IGameContentEditSession session)
        {
            GameContentProposedChange change = session.Changes.FirstOrDefault(candidate =>
                string.Equals(candidate.FieldId, "runProfile.waves", StringComparison.Ordinal));
            return change == null
                ? session.Snapshot.FieldValues["runProfile.waves"].OrderedCollectionValue
                : change.ProposedValue.OrderedCollectionValue;
        }

        private static GameContentOrderedCollectionValue CurrentWaves(GameContentActiveEditSession session)
        {
            return session.GetEffectiveValue("runProfile.waves").OrderedCollectionValue;
        }

        private void PersistAttackReference(
            IdleAutoDefenseEditableSource source,
            AttackDefinitionAsset attack)
        {
            var serialized = new SerializedObject(source.SourceAsset);
            serialized.Update();
            SerializedProperty property = serialized.FindProperty("_attack");
            Assert.That(property, Is.Not.Null);
            Assert.That(property.propertyType, Is.EqualTo(SerializedPropertyType.ObjectReference));
            property.objectReferenceValue = attack;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(source.SourceAsset);
            AssetDatabase.SaveAssetIfDirty(source.SourceAsset);
            AssetDatabase.ImportAsset(
                source.SourcePath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            _provider.RefreshAfterExternalEdit();
        }

        private void PersistWaveSequence(
            IdleAutoDefenseEditableSource source,
            IReadOnlyList<WaveDefinitionAsset> waves)
        {
            var serialized = new SerializedObject(source.SourceAsset);
            serialized.Update();
            SerializedProperty property = serialized.FindProperty("_waves");
            Assert.That(property, Is.Not.Null);
            Assert.That(property.isArray, Is.True);
            property.arraySize = waves == null ? 0 : waves.Count;
            for (int i = 0; i < property.arraySize; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                Assert.That(element.propertyType, Is.EqualTo(SerializedPropertyType.ObjectReference));
                element.objectReferenceValue = waves[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(source.SourceAsset);
            AssetDatabase.SaveAssetIfDirty(source.SourceAsset);
            AssetDatabase.ImportAsset(
                source.SourcePath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            _provider.RefreshAfterExternalEdit();
        }

        private static void AssertStrictRuntimeConsumes(
            GameContentSetAsset contentSet,
            string expectedAttackId)
        {
            Assert.That(contentSet.StartingWeapon.ToRuntimeDefinition().AttackDefinitionId.Value,
                Is.EqualTo(expectedAttackId));
            GameObject host = new GameObject("idle-reference-runtime-proof");
            host.SetActive(false);
            IdleAutoDefenseTemplateController controller = null;
            try
            {
                controller = host.AddComponent<IdleAutoDefenseTemplateController>();
                FieldInfo field = typeof(IdleAutoDefenseTemplateController).GetField(
                    "_contentSet",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(field, Is.Not.Null);
                field.SetValue(controller, contentSet);
                controller.ConfigureStrictAuthoredStartup(true);
                host.SetActive(true);
                controller.Build();
                Assert.That(controller.StartupBlocked, Is.False, controller.StartupError);
                Assert.That(controller.UsingAuthoredCore, Is.True);
                Assert.That(controller.FallbackModeActive, Is.False);
                Assert.That(controller.Runtime, Is.Not.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        private void AssertWaveCollectionCommitIsSafeAndIsolated(
            string editedPackId,
            string untouchedPackId)
        {
            IdleAutoDefenseEditableSource source = ResolveRunProfileSource(editedPackId);
            IdleAutoDefenseEditableSource untouched = ResolveRunProfileSource(untouchedPackId);
            var profile = (IdleAutoDefenseRunProfileAsset)source.SourceAsset;
            WaveDefinitionAsset[] original = profile.Waves.ToArray();
            WaveDefinitionAsset[] committed = original.ToArray();
            committed[0] = original[1];
            committed[1] = original[0];
            string sourceHash = FileHash(source.SourcePath);
            string untouchedHash = FileHash(untouched.SourcePath);
            string sourceGuid = source.SourceGuid;
            string sourceObjectId = source.GlobalObjectId;
            string[] waveGuids = original.Select(wave =>
                AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(wave))).ToArray();

            GameContentPackDescriptor pack = GetPack(editedPackId);
            GameContentPackCatalog catalog = GameContentPackCatalog.Build(
                new IGameContentAuthoringProvider[] { _provider });
            GameContentPackContext context = new GameContentPackSelectionState().Select(catalog, pack.StableKey);
            GameContentRecordDescriptor record = context.Records.Single(candidate =>
                candidate.CanonicalKey.Equals(source.Record.CanonicalKey));
            using (var coordinator = new GameContentEditSessionCoordinator())
            {
                GameContentEditBeginResult begin = coordinator.BeginEdit(context, record, "run-profile");
                Assert.That(begin.Succeeded, Is.True, begin.Message);
                GameContentCollectionItem first = CurrentWaves(begin.Session).Items[0];
                Assert.That(coordinator.ApplyCollectionOperation(
                    begin.Session,
                    "runProfile.waves",
                    GameContentCollectionOperation.Move(first.ItemKey, 1)).Succeeded, Is.True);
                GameContentValidationPreview preview = coordinator.Preview(begin.Session);
                Assert.That(preview.CanCommit, Is.True, FormatIssues(preview));
                GameContentCommitResult commit = coordinator.Commit(begin.Session, true);
                Assert.That(commit.Succeeded, Is.True, commit.Message);

                IdleAutoDefenseEditableSource persisted = ResolveRunProfileSource(editedPackId);
                var persistedProfile = (IdleAutoDefenseRunProfileAsset)persisted.SourceAsset;
                Assert.That(persistedProfile.Waves, Is.EqualTo(committed));
                Assert.That(AssetDatabase.AssetPathToGUID(persisted.SourcePath), Is.EqualTo(sourceGuid));
                Assert.That(GlobalObjectId.GetGlobalObjectIdSlow(persistedProfile).ToString(), Is.EqualTo(sourceObjectId));
                Assert.That(original.Select(wave => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(wave))),
                    Is.EqualTo(waveGuids));
                Assert.That(FileHash(untouched.SourcePath), Is.EqualTo(untouchedHash));
                AssertStrictRuntimeConsumesWaveSequence(persisted.Index.ContentSetAsset, committed);

                Undo.PerformUndo();
                Assert.That(((IdleAutoDefenseRunProfileAsset)ResolveRunProfileSource(editedPackId).SourceAsset).Waves,
                    Is.EqualTo(original));
                Assert.That(coordinator.CheckStale(begin.Session).IsStale, Is.True);
                Undo.PerformRedo();
                IdleAutoDefenseEditableSource redone = ResolveRunProfileSource(editedPackId);
                Assert.That(((IdleAutoDefenseRunProfileAsset)redone.SourceAsset).Waves, Is.EqualTo(committed));
                Assert.That(coordinator.CheckStale(begin.Session).IsStale, Is.False);
                Assert.That(begin.Session.State, Is.EqualTo(GameContentEditSessionState.Committed));
                AssertStrictRuntimeConsumesWaveSequence(redone.Index.ContentSetAsset, committed);

                GameContentRollbackResult rollback = coordinator.Rollback(begin.Session);
                Assert.That(rollback.Succeeded, Is.True, rollback.Message);
                Assert.That(coordinator.ActiveSourceCount, Is.Zero);
            }

            IdleAutoDefenseEditableSource restored = ResolveRunProfileSource(editedPackId);
            Assert.That(((IdleAutoDefenseRunProfileAsset)restored.SourceAsset).Waves, Is.EqualTo(original));
            Assert.That(FileHash(restored.SourcePath), Is.EqualTo(sourceHash));
            Assert.That(FileHash(untouched.SourcePath), Is.EqualTo(untouchedHash));
            Assert.That(_provider.ValidatePack(editedPackId).IsValid, Is.True);
            Assert.That(_provider.ValidatePack(untouchedPackId).IsValid, Is.True);
            AssertStrictRuntimeConsumesWaveSequence(restored.Index.ContentSetAsset, original);
        }

        private static void AssertStrictRuntimeConsumesWaveSequence(
            GameContentSetAsset contentSet,
            IReadOnlyList<WaveDefinitionAsset> expectedWaves)
        {
            GameObject host = new GameObject("idle-wave-runtime-proof");
            host.SetActive(false);
            IdleAutoDefenseTemplateController controller = null;
            try
            {
                controller = host.AddComponent<IdleAutoDefenseTemplateController>();
                FieldInfo contentField = typeof(IdleAutoDefenseTemplateController).GetField(
                    "_contentSet",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(contentField, Is.Not.Null);
                contentField.SetValue(controller, contentSet);
                controller.ConfigureStrictAuthoredStartup(true);
                host.SetActive(true);
                controller.Build();
                Assert.That(controller.StartupBlocked, Is.False, controller.StartupError);
                Assert.That(controller.UsingAuthoredCore, Is.True);
                Assert.That(controller.FallbackModeActive, Is.False);
                Assert.That(controller.ActiveRunProfile, Is.SameAs(contentSet.RunProfile));
                Assert.That(controller.TotalWaveCount, Is.EqualTo(expectedWaves.Count));
                FieldInfo bindingField = typeof(IdleAutoDefenseTemplateController).GetField(
                    "_contentBinding",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(bindingField, Is.Not.Null);
                var binding = (IdleAutoDefenseContentBinding)bindingField.GetValue(controller);
                Assert.That(binding, Is.Not.Null);
                Assert.That(binding.Waves, Is.EqualTo(expectedWaves));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
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
                mapping.PropertyType == SerializedPropertyType.Generic), Is.False);
            Assert.That(source.Mappings.Where(mapping => mapping.PropertyType == SerializedPropertyType.ObjectReference)
                .Select(mapping => mapping.PropertyPath), Is.SubsetOf(new[] { "_attack" }));
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
            IdleAutoDefenseContentEditMappings.ApplyValues(
                source.SourceAsset,
                source.Mappings,
                values,
                false,
                source.Index);
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
