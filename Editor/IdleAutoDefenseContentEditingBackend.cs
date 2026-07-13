using System;
using System.Collections.Generic;
using System.Linq;
using Deucarian.Attacks.Authoring;
using Deucarian.GameContentAuthoring.Editor;
using Deucarian.RunUpgrades.Authoring;
using Deucarian.WeaponSystems.Authoring;
using UnityEditor;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense.Editor
{
    internal sealed partial class GameContentPackAuthoringProvider
    {
        public GameContentEditAvailability CanEdit(GameContentEditRequest request)
        {
            if (!TryResolveEditableSource(request, out IdleAutoDefenseEditableSource source, out string reason))
                return GameContentEditAvailability.ReadOnly(reason, ProviderId);
            return GameContentEditAvailability.Editable(ProviderId, source.Mappings.Count, source.SourceTarget);
        }

        public IGameContentEditSession BeginEdit(GameContentEditRequest request)
        {
            if (!TryResolveEditableSource(request, out IdleAutoDefenseEditableSource source, out string reason))
                throw new InvalidOperationException(reason);
            if (!IdleAutoDefenseWritableSourcePolicy.TryProbeWriteAccess(source.SourcePath, out reason))
                throw new InvalidOperationException(reason);

            GameContentSourceRevision revision = IdleAutoDefenseSourceRevision.Create(request, source);
            IReadOnlyDictionary<string, GameContentFieldValue> values =
                IdleAutoDefenseContentEditMappings.ReadValues(source.SourceAsset, source.Mappings);
            return new IdleAutoDefenseContentEditSession(this, request, source, revision, values);
        }

        internal bool TryResolveEditableSource(
            GameContentEditRequest request,
            out IdleAutoDefenseEditableSource editableSource,
            out string reason)
        {
            editableSource = null;
            if (request == null || !request.IsValid)
            {
                reason = "Select one record in Basic Idle Auto Defense or Scrap Frontier before editing.";
                return false;
            }

            if (!string.Equals(request.ProviderId, ProviderId, StringComparison.OrdinalIgnoreCase))
            {
                reason = "The edit request targets a different content provider.";
                return false;
            }

            IdleAutoDefenseNamedPackDefinition definition = IdleAutoDefenseNamedPackDefinition.All.FirstOrDefault(value =>
                string.Equals(value.PackId, request.RecordKey.PackId, StringComparison.OrdinalIgnoreCase));
            if (definition == null)
            {
                reason = "Only Basic Idle Auto Defense and Scrap Frontier named-pack records are editable through this backend.";
                return false;
            }

            string expectedSelectionKey = GameContentPackDescriptor.BuildStableKey(
                IdleAutoDefenseContentPackIndex.OwningPackageId,
                definition.PackId);
            if (!string.Equals(request.SelectedPackKey, expectedSelectionKey, StringComparison.OrdinalIgnoreCase))
            {
                reason = "Select the record's owning named pack directly. All Packs and Project Content remain read-only through this backend.";
                return false;
            }

            RefreshContentPackIndexes();
            if (!TryGetIndex(definition.PackId, out IdleAutoDefenseContentPackIndex index))
            {
                reason = "The selected named pack is no longer discoverable. Refresh or regenerate the project content.";
                return false;
            }

            if (index.SourceState != GameContentPackSourceState.Available || !index.Validation.IsValid)
            {
                reason = "Resolve the selected named pack's missing, ambiguous, or invalid generated content before editing.";
                return false;
            }

            GameContentRecordDescriptor record = index.Records.FirstOrDefault(value =>
                value.CanonicalKey != null && value.CanonicalKey.Equals(request.RecordKey));
            if (record == null)
            {
                reason = "The canonical record is not owned by the currently selected named pack. Refresh after setup repair or regeneration.";
                return false;
            }

            if (!IdleAutoDefenseContentEditMappings.TryResolve(
                    record.SourceAsset,
                    out UnityEngine.Object sourceAsset,
                    out IReadOnlyList<IdleAutoDefenseSerializedFieldMapping> mappings,
                    out reason))
                return false;

            if (!IdleAutoDefenseWritableSourcePolicy.TryValidate(
                    sourceAsset,
                    index.ContentRootPath,
                    out string sourcePath,
                    out string sourceGuid,
                    out string globalObjectId,
                    out reason))
                return false;

            if (!GameContentSourceIdentity.TryCreate(sourceAsset, sourcePath, out GameContentSourceIdentity identity))
            {
                reason = "The scalar source has no canonical Unity asset identity.";
                return false;
            }

            bool claimedBySelectedPack = index.SourceClaims.Any(claim =>
                claim != null && claim.SourceIdentity != null && claim.SourceIdentity.Equals(identity));
            if (!claimedBySelectedPack)
            {
                reason = "The scalar source is not claimed by the selected named pack. Refresh or repair the generated content graph.";
                return false;
            }

            string[] otherClaimants = _contentPackIndexes.Values
                .Where(candidate => !string.Equals(candidate.Definition.PackId, definition.PackId, StringComparison.OrdinalIgnoreCase))
                .Where(candidate => candidate.SourceClaims.Any(claim =>
                    claim != null && claim.SourceIdentity != null && claim.SourceIdentity.Equals(identity)))
                .Select(candidate => candidate.Definition.DisplayName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (otherClaimants.Length > 0)
            {
                reason = "The scalar source is claimed by multiple named packs: " + string.Join(", ", otherClaimants) + ". Resolve the ownership conflict first.";
                return false;
            }

            string lockKey = string.Join(
                "|",
                ProviderId.ToLowerInvariant(),
                definition.PackId.ToLowerInvariant(),
                sourceGuid.ToLowerInvariant(),
                globalObjectId,
                sourcePath.ToLowerInvariant());
            var target = new GameContentSourceTarget(
                lockKey,
                record.DisplayName + " scalar source",
                sourcePath,
                IdleAutoDefenseSourceRevision.SchemaToken);
            editableSource = new IdleAutoDefenseEditableSource(
                index,
                record,
                sourceAsset,
                mappings,
                target,
                sourcePath,
                sourceGuid,
                globalObjectId);
            reason = string.Empty;
            return true;
        }

        internal GameContentValidationPreview ValidateActualPack(string packId)
        {
            RefreshContentPackIndexes();
            if (!TryGetIndex(packId, out IdleAutoDefenseContentPackIndex index))
                return GameContentValidationPreview.Error("Content Pack", "The selected named pack disappeared during validation.");
            bool canCommit = index.SourceState == GameContentPackSourceState.Available && index.Validation.IsValid;
            return new GameContentValidationPreview(index.Validation.Issues, canCommit, true);
        }

        internal void RefreshAfterExternalEdit()
        {
            RefreshContentPackIndexes();
        }
    }

    internal static class IdleAutoDefenseProposedPackValidator
    {
        public static GameContentValidationPreview Validate(
            IdleAutoDefenseEditableSource source,
            IReadOnlyDictionary<string, GameContentFieldValue> stagedValues)
        {
            if (source?.Index?.PackAsset == null || source.Index.ContentSetAsset == null)
                return GameContentValidationPreview.Error("Proposed Content Pack", "The selected authored pack graph is unavailable.");

            try
            {
                using (var clones = new CloneScope())
                {
                    ScriptableObject scalarClone = clones.Clone((ScriptableObject)source.SourceAsset);
                    IdleAutoDefenseContentEditMappings.ApplyValues(
                        scalarClone,
                        source.Mappings,
                        stagedValues,
                        false);

                    var replacements = new Dictionary<UnityEngine.Object, UnityEngine.Object>();
                    BuildRecordReplacement(source, scalarClone, replacements, clones);
                    GameContentSetAsset contentSetClone = clones.Clone(source.Index.ContentSetAsset);
                    ReplaceContentSetRecords(contentSetClone, replacements);
                    ReplaceAuthoredCoreReferences(contentSetClone, source.Index.ContentSetAsset, replacements, clones);
                    GameContentPackAsset packClone = clones.Clone(source.Index.PackAsset);
                    ReplacePackContentSet(packClone, source.Index.ContentSetAsset, contentSetClone);

                    GameContentPackValidationReport report = GameContentPackValidator.Validate(packClone);
                    var issues = report.Issues.Select(issue => new GameContentAuthoringValidationIssue(
                        issue.Severity == GameContentPackValidationSeverity.Error
                            ? GameContentAuthoringValidationSeverity.Error
                            : GameContentAuthoringValidationSeverity.Warning,
                        "Proposed " + issue.Path,
                        issue.Message)).ToList();
                    GameContentPackResolution resolution = GameContentPackValidator.Resolve(packClone, contentSetClone);
                    if (report.IsValid && !resolution.IsValid)
                    {
                        issues.Add(GameContentAuthoringValidationIssue.Error(
                            "Proposed Content Pack",
                            "The proposed graph passed surface validation but failed strict runtime resolution."));
                    }

                    return new GameContentValidationPreview(issues, report.IsValid && resolution.IsValid, true);
                }
            }
            catch (Exception exception)
            {
                return GameContentValidationPreview.Error(
                    "Proposed Content Pack",
                    "Proposed-state validation failed safely: " + exception.GetBaseException().Message);
            }
        }

        private static void BuildRecordReplacement(
            IdleAutoDefenseEditableSource source,
            ScriptableObject scalarClone,
            IDictionary<UnityEngine.Object, UnityEngine.Object> replacements,
            CloneScope clones)
        {
            if (source.Record.SourceAsset is AttackDefinitionAsset attack)
            {
                AttackDefinitionAsset attackClone = clones.Clone(attack);
                SetObjectReference(attackClone, "_mechanics", scalarClone);
                foreach (WeaponDefinitionAsset weapon in source.Index.ContentSetAsset.AvailableWeapons)
                {
                    if (weapon == null || weapon.Stats == null || weapon.Stats.Attack != attack) continue;
                    WeaponStatsDefinitionAsset statsClone = clones.Clone(weapon.Stats);
                    SetObjectReference(statsClone, "_attack", attackClone);
                    WeaponDefinitionAsset weaponClone = clones.Clone(weapon);
                    SetObjectReference(weaponClone, "_stats", statsClone);
                    replacements[weapon] = weaponClone;
                }
                if (replacements.Count == 0)
                    throw new InvalidOperationException("The selected attack is not consumed by a mounted weapon in this content set.");
                return;
            }

            if (source.Record.SourceAsset is EnemyDefinitionAsset enemy)
            {
                EnemyDefinitionAsset enemyClone = clones.Clone(enemy);
                SetObjectReference(enemyClone, "_stats", scalarClone);
                replacements[enemy] = enemyClone;
                return;
            }

            if (source.Record.SourceAsset is WeaponDefinitionAsset weaponRecord)
            {
                WeaponDefinitionAsset weaponClone = clones.Clone(weaponRecord);
                SetObjectReference(weaponClone, "_stats", scalarClone);
                replacements[weaponRecord] = weaponClone;
                return;
            }

            if (source.Record.SourceAsset is RunUpgradeDefinitionAsset upgrade)
            {
                RunUpgradeDefinitionAsset upgradeClone = clones.Clone(upgrade);
                SetObjectReference(upgradeClone, "_economy", scalarClone);
                replacements[upgrade] = upgradeClone;
                return;
            }

            throw new InvalidOperationException("The selected record type has no proposed-pack substitution rule.");
        }

        private static void ReplaceContentSetRecords(
            GameContentSetAsset contentSet,
            IReadOnlyDictionary<UnityEngine.Object, UnityEngine.Object> replacements)
        {
            var serialized = new SerializedObject(contentSet);
            serialized.Update();
            ReplaceReference(serialized.FindProperty("_startingWeapon"), replacements);
            ReplaceArrayReferences(serialized.FindProperty("_availableWeapons"), replacements);
            ReplaceArrayReferences(serialized.FindProperty("_enemyPool"), replacements);
            ReplaceArrayReferences(serialized.FindProperty("_upgradePool"), replacements);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ReplacePackContentSet(
            GameContentPackAsset pack,
            GameContentSetAsset original,
            GameContentSetAsset replacement)
        {
            var replacements = new Dictionary<UnityEngine.Object, UnityEngine.Object> { [original] = replacement };
            var serialized = new SerializedObject(pack);
            serialized.Update();
            ReplaceReference(serialized.FindProperty("_defaultContentSet"), replacements);
            ReplaceArrayReferences(serialized.FindProperty("_contentSets"), replacements);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ReplaceAuthoredCoreReferences(
            GameContentSetAsset contentSetClone,
            GameContentSetAsset originalContentSet,
            IReadOnlyDictionary<UnityEngine.Object, UnityEngine.Object> replacements,
            CloneScope clones)
        {
            if (originalContentSet.GameRules == null) return;

            IdleAutoDefenseGameRulesAsset gameRulesClone = clones.Clone(originalContentSet.GameRules);
            var serializedRules = new SerializedObject(gameRulesClone);
            serializedRules.Update();
            ReplaceReference(serializedRules.FindProperty("_eliteEnemy"), replacements);
            ReplaceReference(serializedRules.FindProperty("_bossEnemy"), replacements);
            SerializedProperty modules = serializedRules.FindProperty("_modules");
            if (modules != null && modules.isArray)
            {
                for (int i = 0; i < modules.arraySize; i++)
                    ReplaceReference(modules.GetArrayElementAtIndex(i).FindPropertyRelative("_weapon"), replacements);
            }
            serializedRules.ApplyModifiedPropertiesWithoutUndo();
            SetObjectReference(contentSetClone, "_gameRules", gameRulesClone);
        }

        private static void SetObjectReference(UnityEngine.Object target, string propertyPath, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.Update();
            SerializedProperty property = serialized.FindProperty(propertyPath);
            if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
                throw new InvalidOperationException("Proposed graph property '" + propertyPath + "' is unavailable or no longer an object reference.");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ReplaceArrayReferences(
            SerializedProperty array,
            IReadOnlyDictionary<UnityEngine.Object, UnityEngine.Object> replacements)
        {
            if (array == null || !array.isArray) return;
            for (int i = 0; i < array.arraySize; i++)
                ReplaceReference(array.GetArrayElementAtIndex(i), replacements);
        }

        private static void ReplaceReference(
            SerializedProperty property,
            IReadOnlyDictionary<UnityEngine.Object, UnityEngine.Object> replacements)
        {
            if (property == null || property.propertyType != SerializedPropertyType.ObjectReference) return;
            UnityEngine.Object current = property.objectReferenceValue;
            if (current != null && replacements.TryGetValue(current, out UnityEngine.Object replacement))
                property.objectReferenceValue = replacement;
        }

        private sealed class CloneScope : IDisposable
        {
            private readonly List<UnityEngine.Object> _clones = new List<UnityEngine.Object>();

            public T Clone<T>(T source) where T : UnityEngine.Object
            {
                if (source == null) throw new ArgumentNullException(nameof(source));
                T clone = UnityEngine.Object.Instantiate(source);
                clone.name = source.name + " (Proposed)";
                clone.hideFlags = HideFlags.DontSave;
                _clones.Add(clone);
                return clone;
            }

            public void Dispose()
            {
                for (int i = _clones.Count - 1; i >= 0; i--)
                    GameContentAuthoringEditorAssets.DestroyTransientObject(_clones[i]);
                _clones.Clear();
            }
        }
    }
}
