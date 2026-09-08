using System;
using System.Collections.Generic;
using System.Globalization;
using Deucarian.Attacks;
using Deucarian.Attacks.Authoring;
using Deucarian.AutoDefense;
using Deucarian.Combat;
using Deucarian.Projectiles;
using Deucarian.WeaponSystems;
using Deucarian.WeaponSystems.Authoring;
using UnityEngine;
using static Deucarian.TemplateGameIdleAutoDefense.BasicIdleAutoDefenseGame;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    // Owns starter weapon bindings, presentation prefabs and mount/module conversion.
    internal static class IdleAutoDefenseWeaponContent
    {
        private static readonly string[] RequiredTemplateWeaponIds =
        {
            PulseCannonWeaponId.Value,
            ShardLauncherWeaponId.Value,
            ArcBurstTowerWeaponId.Value,
            HomingSpireWeaponId.Value
        };

        internal static WeaponRuntime CreateWeaponRuntime(AutoDefenseDefinition definition, AttackRuntime attacks)
        {
            var weapons = new List<WeaponDefinition>();
            for (int i = 0; i < definition.WeaponModules.Count; i++)
                weapons.Add(definition.WeaponModules[i].WeaponDefinition);
            return new WeaponRuntime(weapons, new AttackRuntimeWeaponAttackAdapter(attacks), new ProjectileLaunchWeaponAdapter());
        }

        internal static WeaponDefinitionAsset[] CreateWeaponDefinitionAssets(IReadOnlyList<AttackDefinitionAsset> attackRecipes = null, IdleAutoDefenseGeneratedContent generated = null)
        {
            if (attackRecipes == null)
            {
                AttackDefinitionAsset[] defaults = IdleAutoDefenseAttackContent.CreateAttackRecipes();
                generated?.OwnGenerated<AttackDefinitionAsset>(null, defaults);
                attackRecipes = defaults;
            }
            AttackDefinitionAsset pulse = IdleAutoDefenseAttackContent.FindAttackRecipe(attackRecipes, PulseAttackId.Value);
            AttackDefinitionAsset shard = IdleAutoDefenseAttackContent.FindAttackRecipe(attackRecipes, ShardAttackId.Value);
            AttackDefinitionAsset arc = IdleAutoDefenseAttackContent.FindAttackRecipe(attackRecipes, ArcBurstAttackId.Value);
            AttackDefinitionAsset homing = IdleAutoDefenseAttackContent.FindAttackRecipe(attackRecipes, HomingPulseAttackId.Value);
            var definitions = new[]
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
            return generated == null ? definitions : generated.OwnGenerated<WeaponDefinitionAsset>(null, definitions);
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

        internal static WeaponDefinitionAsset[] ResolveWeaponDefinitionsForTemplate(IReadOnlyList<WeaponDefinitionAsset> assignedDefinitions, IReadOnlyList<AttackDefinitionAsset> attackRecipes, out int rejectedDefinitionCount)
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

        internal static WeaponDefinition[] CreateWeaponDefinitions(IReadOnlyList<WeaponDefinitionAsset> weaponDefinitions)
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

        private static AttackSourceSnapshot Source(string suffix)
        {
            return new AttackSourceSnapshot(new AttackSourceId("source.idle-auto-defense." + suffix), new CombatantId("objective.idle-auto-defense.core"));
        }

        internal static AutoDefenseMountDefinition[] CreateAutoDefenseMountDefinitions(IReadOnlyList<WeaponDefinitionAsset> weapons)
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
                var mountId = new AutoDefenseMountId("mount.idle-auto-defense." + IdleAutoDefenseContentIdentity.SanitizeRuntimeSegment(id));
                var slotId = new WeaponSlotId("slot.idle-auto-defense." + IdleAutoDefenseContentIdentity.SanitizeRuntimeSegment(id));
                mounts[i] = new AutoDefenseMountDefinition(mountId, new Vector3(start + spacing * i, 0f, 0f), slotId, weaponId, enabled: false);
            }

            return mounts;
        }

        internal static AutoDefenseWeaponModuleDefinition[] CreateAutoDefenseWeaponModuleDefinitions(IReadOnlyList<WeaponDefinitionAsset> weapons, IReadOnlyList<AutoDefenseMountDefinition> mounts)
        {
            var modules = new AutoDefenseWeaponModuleDefinition[weapons.Count];
            for (int i = 0; i < weapons.Count; i++)
            {
                string suffix = weapons[i] == null || string.IsNullOrWhiteSpace(weapons[i].Id)
                    ? i.ToString(CultureInfo.InvariantCulture)
                    : IdleAutoDefenseContentIdentity.SanitizeRuntimeSegment(weapons[i].Id);
                modules[i] = new AutoDefenseWeaponModuleDefinition(mounts[i].Id, weapons[i].ToRuntimeDefinition(), Source(suffix));
            }

            return modules;
        }

        internal static WeaponDefinitionAsset[] CopyWeaponDefinitions(IReadOnlyList<WeaponDefinitionAsset> weapons)
        {
            if (weapons == null || weapons.Count == 0) return Array.Empty<WeaponDefinitionAsset>();
            var copy = new WeaponDefinitionAsset[weapons.Count];
            for (int i = 0; i < weapons.Count; i++) copy[i] = weapons[i];
            return copy;
        }

        internal static WeaponDefinitionAsset FindWeaponDefinition(IReadOnlyList<WeaponDefinitionAsset> weapons, string id)
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

        private static bool WeaponAttackExists(WeaponDefinitionAsset weapon, IReadOnlyList<AttackDefinitionAsset> attackRecipes)
        {
            if (weapon == null || weapon.Stats == null || weapon.Stats.Attack == null) return false;
            return IdleAutoDefenseAttackContent.FindAttackRecipe(attackRecipes, weapon.Stats.Attack.Id) != null;
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
    }
}
