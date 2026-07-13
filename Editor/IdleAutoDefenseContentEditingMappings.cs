using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Deucarian.Attacks.Authoring;
using Deucarian.GameContentAuthoring.Editor;
using Deucarian.RunUpgrades;
using Deucarian.RunUpgrades.Authoring;
using Deucarian.WeaponSystems.Authoring;
using UnityEditor;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense.Editor
{
    internal sealed class IdleAutoDefenseSerializedFieldMapping
    {
        public IdleAutoDefenseSerializedFieldMapping(
            GameContentFieldDescriptor descriptor,
            string propertyPath,
            SerializedPropertyType propertyType)
        {
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            PropertyPath = string.IsNullOrWhiteSpace(propertyPath) ? string.Empty : propertyPath.Trim();
            PropertyType = propertyType;
        }

        public GameContentFieldDescriptor Descriptor { get; }
        public string PropertyPath { get; }
        public SerializedPropertyType PropertyType { get; }

        public bool TryRead(UnityEngine.Object source, out GameContentFieldValue value, out string reason)
        {
            value = null;
            if (source == null)
            {
                reason = "The mapped source asset is missing.";
                return false;
            }

            var serialized = new SerializedObject(source);
            serialized.Update();
            SerializedProperty property = serialized.FindProperty(PropertyPath);
            if (property == null)
            {
                reason = "Serialized property '" + PropertyPath + "' was not found.";
                return false;
            }

            if (property.propertyType != PropertyType)
            {
                reason = "Serialized property '" + PropertyPath + "' has type " + property.propertyType +
                         " instead of " + PropertyType + ".";
                return false;
            }

            switch (Descriptor.FieldType)
            {
                case GameContentFieldType.Integer:
                    value = GameContentFieldValue.FromInteger(property.intValue);
                    break;
                case GameContentFieldType.Number:
                    value = GameContentFieldValue.FromNumber(property.floatValue);
                    break;
                case GameContentFieldType.Enum:
                    if (property.enumValueIndex < 0 || property.enumValueIndex >= property.enumNames.Length)
                    {
                        reason = "Serialized enum property '" + PropertyPath + "' has an invalid index.";
                        return false;
                    }
                    value = GameContentFieldValue.FromEnum(property.enumNames[property.enumValueIndex]);
                    break;
                default:
                    reason = "Field '" + Descriptor.FieldId + "' uses an unsupported scalar mapping type.";
                    return false;
            }

            reason = string.Empty;
            return true;
        }

        public bool TryWrite(SerializedObject serialized, GameContentFieldValue value, out string reason)
        {
            if (serialized == null)
            {
                reason = "The serialized edit target is unavailable.";
                return false;
            }

            if (!Descriptor.Accepts(value, out reason)) return false;
            SerializedProperty property = serialized.FindProperty(PropertyPath);
            if (property == null)
            {
                reason = "Serialized property '" + PropertyPath + "' was not found.";
                return false;
            }

            if (property.propertyType != PropertyType)
            {
                reason = "Serialized property '" + PropertyPath + "' has type " + property.propertyType +
                         " instead of " + PropertyType + ".";
                return false;
            }

            switch (Descriptor.FieldType)
            {
                case GameContentFieldType.Integer:
                    if (value.IntegerValue < int.MinValue || value.IntegerValue > int.MaxValue)
                    {
                        reason = "The integer is outside the supported 32-bit range.";
                        return false;
                    }
                    property.intValue = (int)value.IntegerValue;
                    break;
                case GameContentFieldType.Number:
                    if (value.NumberValue < -float.MaxValue || value.NumberValue > float.MaxValue)
                    {
                        reason = "The number is outside the supported single-precision range.";
                        return false;
                    }
                    property.floatValue = (float)value.NumberValue;
                    break;
                case GameContentFieldType.Enum:
                    int enumIndex = Array.FindIndex(
                        property.enumNames,
                        candidate => string.Equals(candidate, value.StringValue, StringComparison.Ordinal));
                    if (enumIndex < 0)
                    {
                        reason = "The enum token is not available on the serialized property.";
                        return false;
                    }
                    property.enumValueIndex = enumIndex;
                    break;
                default:
                    reason = "Field '" + Descriptor.FieldId + "' uses an unsupported scalar mapping type.";
                    return false;
            }

            reason = string.Empty;
            return true;
        }
    }

    internal static class IdleAutoDefenseContentEditMappings
    {
        private const string CombatGroup = "Combat";
        private const string EconomyGroup = "Economy";

        private static readonly IReadOnlyList<IdleAutoDefenseSerializedFieldMapping> AttackMappings = new[]
        {
            Integer("attack.cooldownTicks", "combat.cooldown-ticks", "Cooldown Ticks", "Simulation ticks between attacks.", "_cooldownTicks", 10, CombatGroup, 0),
            Number("attack.range", "combat.range", "Range", "Authored attack reach.", "_range", 20, CombatGroup, 0d),
            Number("attack.damage", "combat.damage", "Damage", "Base damage applied by this attack.", "_damageAmount", 30, CombatGroup, float.Epsilon)
        };

        private static readonly IReadOnlyList<IdleAutoDefenseSerializedFieldMapping> EnemyMappings = new[]
        {
            Number("enemy.maximumHealth", "combat.maximum-health", "Maximum Health", "Authored enemy health before modifiers.", "_maximumHealth", 10, CombatGroup, float.Epsilon),
            Number("enemy.moveSpeed", "movement.speed", "Move Speed", "Authored enemy movement speed.", "_moveSpeed", 20, CombatGroup, float.Epsilon),
            Integer("enemy.rewardValue", "economy.enemy-reward", "Reward Value", "Authored resource reward for defeating this enemy.", "_rewardValue", 30, EconomyGroup, 0),
            Number("enemy.contactDamage", "combat.contact-damage", "Contact Damage", "Damage dealt when this enemy reaches the objective.", "_contactDamage", 40, CombatGroup, 0d),
            Number("enemy.collisionRadius", "combat.collision-radius", "Collision Radius", "Authored collision and targeting radius.", "_collisionRadius", 50, CombatGroup, float.Epsilon)
        };

        private static readonly IReadOnlyList<IdleAutoDefenseSerializedFieldMapping> WeaponMappings = new[]
        {
            Integer("weapon.cooldownTicks", "combat.cooldown-ticks", "Cooldown Ticks", "Simulation ticks between mounted weapon activations.", "_cooldownTicks", 10, CombatGroup, 0),
            Number("weapon.range", "combat.range", "Range", "Mounted weapon targeting range.", "_range", 20, CombatGroup, 0d),
            Integer("weapon.burstCount", "combat.burst-count", "Burst Count", "Attack intents emitted per burst.", "_burstCount", 30, CombatGroup, 1),
            Integer("weapon.volleyCount", "combat.volley-count", "Volley Count", "Projectiles or intents emitted per volley.", "_volleyCount", 40, CombatGroup, 1),
            Number("weapon.spreadDegrees", "combat.spread-degrees", "Spread Degrees", "Angular spread across a volley.", "_spreadDegrees", 50, CombatGroup, 0d),
            Integer("weapon.buildCost", "economy.build-cost", "Build Cost", "Authored cost to unlock or mount this module.", "_buildCost", 60, EconomyGroup, 0)
        };

        private static readonly IReadOnlyList<IdleAutoDefenseSerializedFieldMapping> UpgradeMappings = new[]
        {
            Enum(
                "upgrade.rarity",
                "upgrade.rarity",
                "Rarity",
                "Draft rarity for this run upgrade.",
                "_rarity",
                10,
                EconomyGroup,
                System.Enum.GetNames(typeof(RunUpgradeRarity))),
            Integer("upgrade.weight", "upgrade.draft-weight", "Draft Weight", "Relative draft-selection weight.", "_weight", 20, EconomyGroup, 1),
            Integer("upgrade.maxRank", "upgrade.max-rank", "Maximum Rank", "Maximum number of times this upgrade can be selected.", "_maxRank", 30, EconomyGroup, 1)
        };

        public static bool TryResolve(
            UnityEngine.Object recordAsset,
            out UnityEngine.Object sourceAsset,
            out IReadOnlyList<IdleAutoDefenseSerializedFieldMapping> mappings,
            out string reason)
        {
            sourceAsset = null;
            mappings = Array.Empty<IdleAutoDefenseSerializedFieldMapping>();
            if (recordAsset is AttackDefinitionAsset attack)
            {
                sourceAsset = attack.Mechanics;
                mappings = AttackMappings;
            }
            else if (recordAsset is EnemyDefinitionAsset enemy)
            {
                sourceAsset = enemy.Stats;
                mappings = EnemyMappings;
            }
            else if (recordAsset is WeaponDefinitionAsset weapon)
            {
                sourceAsset = weapon.Stats;
                mappings = WeaponMappings;
            }
            else if (recordAsset is RunUpgradeDefinitionAsset upgrade)
            {
                sourceAsset = upgrade.Economy;
                mappings = UpgradeMappings;
            }
            else
            {
                reason = "This record type has no approved direct scalar fields. IDs, references, lists, waves, rewards, progression, themes, audio, tutorials, and UI structures remain read-only.";
                return false;
            }

            if (sourceAsset == null)
            {
                reason = "The record's standalone scalar section asset is missing.";
                return false;
            }

            var active = new List<IdleAutoDefenseSerializedFieldMapping>();
            var failures = new List<string>();
            for (int i = 0; i < mappings.Count; i++)
            {
                IdleAutoDefenseSerializedFieldMapping mapping = mappings[i];
                if (mapping.TryRead(sourceAsset, out _, out string failure)) active.Add(mapping);
                else failures.Add(mapping.Descriptor.DisplayName + ": " + failure);
            }

            mappings = active
                .OrderBy(value => value.Descriptor.Order)
                .ThenBy(value => value.Descriptor.FieldId, StringComparer.Ordinal)
                .ToArray();
            if (mappings.Count > 0)
            {
                reason = string.Empty;
                return true;
            }

            reason = failures.Count == 0
                ? "This record exposes no approved scalar fields."
                : "The approved scalar mapping no longer matches the serialized schema: " + string.Join("; ", failures) + ".";
            return false;
        }

        public static IReadOnlyDictionary<string, GameContentFieldValue> ReadValues(
            UnityEngine.Object source,
            IReadOnlyList<IdleAutoDefenseSerializedFieldMapping> mappings)
        {
            var values = new Dictionary<string, GameContentFieldValue>(StringComparer.Ordinal);
            for (int i = 0; i < mappings.Count; i++)
            {
                IdleAutoDefenseSerializedFieldMapping mapping = mappings[i];
                if (!mapping.TryRead(source, out GameContentFieldValue value, out string reason))
                    throw new InvalidOperationException(reason);
                values.Add(mapping.Descriptor.FieldId, value);
            }
            return values;
        }

        public static void ApplyValues(
            UnityEngine.Object source,
            IReadOnlyList<IdleAutoDefenseSerializedFieldMapping> mappings,
            IReadOnlyDictionary<string, GameContentFieldValue> values,
            bool withUndo)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var serialized = new SerializedObject(source);
            serialized.Update();
            for (int i = 0; i < mappings.Count; i++)
            {
                IdleAutoDefenseSerializedFieldMapping mapping = mappings[i];
                if (!values.TryGetValue(mapping.Descriptor.FieldId, out GameContentFieldValue value))
                    throw new InvalidOperationException("The staged snapshot is missing field '" + mapping.Descriptor.FieldId + "'.");
                if (!mapping.TryWrite(serialized, value, out string reason))
                    throw new InvalidOperationException(reason);
            }

            if (withUndo) serialized.ApplyModifiedProperties();
            else serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static IdleAutoDefenseSerializedFieldMapping Integer(
            string fieldId,
            string semanticId,
            string displayName,
            string description,
            string propertyPath,
            int order,
            string group,
            int minimum)
        {
            return new IdleAutoDefenseSerializedFieldMapping(
                new GameContentFieldDescriptor(
                    fieldId,
                    semanticId,
                    displayName,
                    description,
                    GameContentFieldType.Integer,
                    order: order,
                    group: group,
                    minimumNumber: minimum,
                    maximumNumber: int.MaxValue),
                propertyPath,
                SerializedPropertyType.Integer);
        }

        private static IdleAutoDefenseSerializedFieldMapping Number(
            string fieldId,
            string semanticId,
            string displayName,
            string description,
            string propertyPath,
            int order,
            string group,
            double minimum)
        {
            return new IdleAutoDefenseSerializedFieldMapping(
                new GameContentFieldDescriptor(
                    fieldId,
                    semanticId,
                    displayName,
                    description,
                    GameContentFieldType.Number,
                    order: order,
                    group: group,
                    minimumNumber: minimum,
                    maximumNumber: float.MaxValue),
                propertyPath,
                SerializedPropertyType.Float);
        }

        private static IdleAutoDefenseSerializedFieldMapping Enum(
            string fieldId,
            string semanticId,
            string displayName,
            string description,
            string propertyPath,
            int order,
            string group,
            IEnumerable<string> tokens)
        {
            return new IdleAutoDefenseSerializedFieldMapping(
                new GameContentFieldDescriptor(
                    fieldId,
                    semanticId,
                    displayName,
                    description,
                    GameContentFieldType.Enum,
                    order: order,
                    group: group,
                    enumOptions: tokens.Select(token => new GameContentEnumOption(token, token))),
                propertyPath,
                SerializedPropertyType.Enum);
        }
    }

    internal sealed class IdleAutoDefenseEditableSource
    {
        public IdleAutoDefenseEditableSource(
            IdleAutoDefenseContentPackIndex index,
            GameContentRecordDescriptor record,
            UnityEngine.Object sourceAsset,
            IReadOnlyList<IdleAutoDefenseSerializedFieldMapping> mappings,
            GameContentSourceTarget sourceTarget,
            string sourcePath,
            string sourceGuid,
            string globalObjectId)
        {
            Index = index;
            Record = record;
            SourceAsset = sourceAsset;
            Mappings = mappings;
            SourceTarget = sourceTarget;
            SourcePath = sourcePath;
            SourceGuid = sourceGuid;
            GlobalObjectId = globalObjectId;
        }

        public IdleAutoDefenseContentPackIndex Index { get; }
        public GameContentRecordDescriptor Record { get; }
        public UnityEngine.Object SourceAsset { get; }
        public IReadOnlyList<IdleAutoDefenseSerializedFieldMapping> Mappings { get; }
        public GameContentSourceTarget SourceTarget { get; }
        public string SourcePath { get; }
        public string SourceGuid { get; }
        public string GlobalObjectId { get; }
    }

    internal static class IdleAutoDefenseWritableSourcePolicy
    {
        public static bool TryValidate(
            UnityEngine.Object source,
            string contentRoot,
            out string assetPath,
            out string guid,
            out string globalObjectId,
            out string reason)
        {
            assetPath = NormalizePath(AssetDatabase.GetAssetPath(source));
            guid = string.Empty;
            globalObjectId = string.Empty;
            if (source == null || string.IsNullOrWhiteSpace(assetPath))
            {
                reason = "The scalar source is not a persisted Unity asset.";
                return false;
            }

            if (!IsAllowedAssetPath(assetPath, contentRoot, out reason)) return false;
            guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrWhiteSpace(guid))
            {
                reason = "The scalar source has no stable asset GUID.";
                return false;
            }

            globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(source).ToString();
            if (string.IsNullOrWhiteSpace(globalObjectId))
            {
                reason = "The scalar source has no stable Unity object identity.";
                return false;
            }

            string fullPath = AssetPathToFullPath(assetPath);
            if (!File.Exists(fullPath))
            {
                reason = "The scalar source file is missing from disk.";
                return false;
            }

            FileAttributes attributes = File.GetAttributes(fullPath);
            if ((attributes & FileAttributes.ReadOnly) != 0)
            {
                reason = "The scalar source file is read-only. Make the generated project asset writable before editing.";
                return false;
            }

            try
            {
                if (!AssetDatabase.IsOpenForEdit(source))
                {
                    reason = "The scalar source is not open for edit in the active version-control workspace.";
                    return false;
                }
            }
            catch
            {
                // Filesystem and source-claim checks remain authoritative when no VCS provider is active.
            }

            reason = string.Empty;
            return true;
        }

        public static bool TryProbeWriteAccess(string assetPath, out string reason)
        {
            string fullPath = AssetPathToFullPath(assetPath);
            try
            {
                using (new FileStream(
                           fullPath,
                           FileMode.Open,
                           FileAccess.ReadWrite,
                           FileShare.ReadWrite | FileShare.Delete))
                {
                }
            }
            catch (Exception exception)
            {
                reason = "The scalar source cannot be opened for writing: " + exception.GetBaseException().Message;
                return false;
            }

            reason = string.Empty;
            return true;
        }

        internal static bool IsAllowedAssetPath(string assetPath, string contentRoot, out string reason)
        {
            string normalizedPath = NormalizePath(assetPath);
            string normalizedRoot = NormalizePath(contentRoot).TrimEnd('/');
            if (!normalizedPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                reason = "Only generated project-owned assets under Assets can be edited; package, TemplateSource~, PackageCache, Library, and Temp sources are read-only.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(normalizedRoot) ||
                !(string.Equals(normalizedPath, normalizedRoot, StringComparison.OrdinalIgnoreCase) ||
                  normalizedPath.StartsWith(normalizedRoot + "/", StringComparison.OrdinalIgnoreCase)))
            {
                reason = "The scalar source is outside the selected named pack's generated content root.";
                return false;
            }

            string assetsRoot = Path.GetFullPath(Application.dataPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string fullPath;
            string fullContentRoot;
            try
            {
                fullPath = AssetPathToFullPath(normalizedPath);
                fullContentRoot = AssetPathToFullPath(normalizedRoot)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch (Exception exception)
            {
                reason = "The scalar source path is invalid: " + exception.GetBaseException().Message;
                return false;
            }

            if (!IsUnder(fullPath, assetsRoot) || !IsUnder(fullPath, fullContentRoot))
            {
                reason = "The scalar source path escapes the Unity project Assets or selected content root.";
                return false;
            }

            string cursor = assetsRoot;
            string relative = fullPath.Substring(assetsRoot.Length)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string[] segments = relative.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < segments.Length; i++)
            {
                cursor = Path.Combine(cursor, segments[i]);
                if (!Directory.Exists(cursor) && !File.Exists(cursor)) continue;
                if ((File.GetAttributes(cursor) & FileAttributes.ReparsePoint) != 0)
                {
                    reason = "The scalar source path crosses a symbolic link, junction, or other reparse point.";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        public static string AssetPathToFullPath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
            return Path.GetFullPath(Path.Combine(
                projectRoot,
                NormalizePath(assetPath).Replace('/', Path.DirectorySeparatorChar)));
        }

        private static bool IsUnder(string path, string root)
        {
            string normalizedPath = Path.GetFullPath(path);
            string normalizedRoot = Path.GetFullPath(root)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return string.Equals(normalizedPath, normalizedRoot, StringComparison.OrdinalIgnoreCase) ||
                   normalizedPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                   normalizedPath.StartsWith(normalizedRoot + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizePath(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? string.Empty : path.Trim().Replace('\\', '/');
        }
    }

    internal static class IdleAutoDefenseSourceRevision
    {
        public const string SchemaToken = "idle-scriptable-object-scalar-v1";

        public static GameContentSourceRevision Create(
            GameContentEditRequest request,
            IdleAutoDefenseEditableSource source)
        {
            string fullPath = IdleAutoDefenseWritableSourcePolicy.AssetPathToFullPath(source.SourcePath);
            byte[] fileBytes = File.ReadAllBytes(fullPath);
            IReadOnlyDictionary<string, GameContentFieldValue> values =
                IdleAutoDefenseContentEditMappings.ReadValues(source.SourceAsset, source.Mappings);
            var payload = new StringBuilder();
            Append(payload, "schema", SchemaToken);
            Append(payload, "guid", source.SourceGuid);
            Append(payload, "globalObjectId", source.GlobalObjectId);
            Append(payload, "assetPath", source.SourcePath);
            Append(payload, "fileSha256", Hash(fileBytes));
            Append(payload, "dependencyHash", AssetDatabase.GetAssetDependencyHash(source.SourcePath).ToString());
            Append(payload, "serializedObjectSha256", Hash(Encoding.UTF8.GetBytes(EditorJsonUtility.ToJson(source.SourceAsset, false))));
            Append(payload, "selectedPackKey", request.SelectedPackKey);
            Append(payload, "recordKey", request.RecordKey.StableKey);
            foreach (IdleAutoDefenseSerializedFieldMapping mapping in source.Mappings
                         .OrderBy(value => value.Descriptor.FieldId, StringComparer.Ordinal))
            {
                GameContentFieldValue value = values[mapping.Descriptor.FieldId];
                Append(
                    payload,
                    "field:" + mapping.Descriptor.FieldId,
                    value.FieldType + ":" + value.ToDisplayString());
            }

            return new GameContentSourceRevision(SchemaToken + ":" + Hash(Encoding.UTF8.GetBytes(payload.ToString())));
        }

        public static string FileSha256(string assetPath)
        {
            return Hash(File.ReadAllBytes(IdleAutoDefenseWritableSourcePolicy.AssetPathToFullPath(assetPath)));
        }

        private static void Append(StringBuilder builder, string key, string value)
        {
            string safe = value ?? string.Empty;
            builder.Append(key)
                .Append('=')
                .Append(safe.Length.ToString(CultureInfo.InvariantCulture))
                .Append(':')
                .Append(safe)
                .Append('\n');
        }

        private static string Hash(byte[] bytes)
        {
            using (SHA256 sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(bytes))
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }
        }
    }
}
