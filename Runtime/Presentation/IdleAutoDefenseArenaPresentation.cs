using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Deucarian.Attacks;
using Deucarian.Attacks.Authoring;
using Deucarian.AutoDefense;
using Deucarian.Combat;
using Deucarian.Common;
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
    internal sealed class IdleAutoDefenseArenaPresentation
    {
        private readonly IdleAutoDefenseWorldResources _resources;

        private readonly IdleAutoDefenseVisualAssets _assets;

        private readonly IdleAutoDefenseTargetPresentation _targets;

        private readonly IdleAutoDefenseVisibleInstanceStamping _stamps;

        private readonly IdleAutoDefenseContentBinding _content;

        private readonly IdleAutoDefensePresentationCounters _counters;

        private readonly IdleAutoDefensePresentationQueries _queries;

        internal IdleAutoDefenseArenaPresentation(IdleAutoDefenseWorldResources resources, IdleAutoDefenseVisualAssets assets, IdleAutoDefenseTargetPresentation targets, IdleAutoDefenseVisibleInstanceStamping stamps, IdleAutoDefenseContentBinding content, IdleAutoDefensePresentationCounters counters, IdleAutoDefensePresentationQueries queries)
        {
            _resources = resources;
            _assets = assets;
            _targets = targets;
            _stamps = stamps;
            _content = content;
            _counters = counters;
            _queries = queries;
        }

        private const float TemplateVisibleArenaRadius = 14.75f;
        private IdleAutoDefenseContentSetRuntimeSettings ContentSetRuntimeSettings => _content.ContentSet != null && _content.ContentSet.IsValid && _content.ContentSet.ContentSet != null ? _content.ContentSet.ContentSet.RuntimeSettings : null;
        private IdleAutoDefenseModuleRule ResolveModuleRule(IdleAutoDefenseModuleRole role) => _content.GameRules == null ? null : _content.GameRules.GetModule(role);

        internal void CreateCorePresentation(Vector3 position)
        {
            IdleAutoDefenseObjectivePresentationBinding presentation = ResolveObjectivePresentationBinding();
            GameObject core = new GameObject(presentation.DisplayName);
            core.transform.SetParent(_resources.Root.transform, false);
            core.transform.position = position;
            _stamps.StampAuthoredVisibleInstance(core, "ObjectivePresentation", presentation.ContentId, core.name, string.Empty, string.Empty, "CoreBase", _content.ContentSet == null ? null : _content.ContentSet.ContentSet);
            IReadOnlyList<IdleAutoDefenseKenneyModelBinding> models = presentation.Models;
            for (int i = 0; i < models.Count; i++)
            {
                IdleAutoDefenseKenneyModelBinding model = models[i];
                if (model == null || string.IsNullOrWhiteSpace(model.ModelName)) continue;
                _assets.InstantiateKenneyModel(
                    model.ModelName,
                    core.transform,
                    model.LocalPosition,
                    Quaternion.Euler(model.LocalEulerAngles),
                    model.LocalScale,
                    model.Tint);
            }
            IdleAutoDefenseVisualAssets.DisableColliders(core);

            IdleAutoDefenseModuleRule startingModule = ResolveModuleRule(IdleAutoDefenseModuleRole.StartingProjectile);
            CreateWeaponPresentation(
                startingModule == null ? BasicIdleAutoDefenseGame.ShardLauncherWeaponId.Value : startingModule.WeaponId,
                startingModule == null ? BasicIdleAutoDefenseGame.ShardAttackId.Value : startingModule.AttackId,
                true);
        }

        internal IdleAutoDefenseObjectivePresentationBinding ResolveObjectivePresentationBinding()
        {
            IdleAutoDefenseContentSetRuntimeSettings settings = ContentSetRuntimeSettings;
            if (settings != null && settings.ObjectivePresentation != null)
            {
                _counters.AuthoredObjectivePresentationBindingCount++;
                return settings.ObjectivePresentation;
            }

            _counters.FallbackObjectivePresentationBindingCount++;
            return IdleAutoDefenseObjectivePresentationBinding.CreateDefault();
        }

        internal IdleAutoDefenseWeaponVisualBinding CreateWeaponPresentation(
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
            if (_resources.Root == null || string.IsNullOrWhiteSpace(attackId)) return null;
            GameObject root = new GameObject(displayName + " 3D Mount");
            root.transform.SetParent(_resources.Root.transform, false);
            root.transform.localPosition = position;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            _stamps.StampAuthoredVisibleInstance(root, "WeaponPresentation", weaponId, displayName, weaponId, attackId, "WeaponMount", _content.ContentSet == null ? null : _content.ContentSet.ContentSet, weaponId + ":mount", string.Empty);

            _assets.InstantiateKenneyModel(baseModelName, root.transform, Vector3.zero, Quaternion.identity, Vector3.one * 0.72f, tint);
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
                GameObject authoredInstance = UnityEngine.Object.Instantiate(authoredPrefab, recoilPivot, false);
                authoredInstance.name = displayName + " Authored Weapon Visual";
                _stamps.StampAuthoredVisibleInstance(authoredInstance, "WeaponPrefab", weaponId, authoredPrefab.name, weaponId, attackId, "WeaponVisual", authoredPrefab, weaponId + ":muzzle.primary", string.Empty);
                authoredInstance.transform.localPosition = Vector3.zero;
                authoredInstance.transform.localRotation = Quaternion.identity;
                authoredInstance.transform.localScale = Vector3.one;
                IdleAutoDefenseKenneyModelPrefab[] authoredModels = authoredInstance.GetComponentsInChildren<IdleAutoDefenseKenneyModelPrefab>(true);
                for (int i = 0; i < authoredModels.Length; i++)
                    authoredModels[i].EnsureModel();
                _assets.TintRenderers(authoredInstance, tint);
                IdleAutoDefenseVisualAssets.DisableColliders(authoredInstance);
                _counters.AuthoredWeaponPresentationSpawnCount++;
            }
            else
            {
                _assets.InstantiateKenneyModel(weaponModelName, recoilPivot, Vector3.zero, Quaternion.identity, Vector3.one * 0.74f, tint);
                _counters.FallbackWeaponPresentationSpawnCount++;
                _counters.FallbackVisibleGameplaySpawnCount++;
            }

            Transform muzzle = new GameObject(displayName + " Muzzle").transform;
            muzzle.SetParent(recoilPivot, false);
            muzzle.localPosition = muzzleLocalPosition;
            muzzle.localRotation = Quaternion.identity;
            muzzle.localScale = Vector3.one;

            var binding = root.AddComponent<IdleAutoDefenseWeaponVisualBinding>();
            binding.Configure(yawPivot, recoilPivot, muzzle, turnSpeedDegrees, tint);
            root.SetActive(enabled);
            IdleAutoDefenseVisualAssets.DisableColliders(root);
            _targets.WeaponBindings[attackId] = binding;
            return binding;
        }

        internal IdleAutoDefenseWeaponPresentationBinding ResolveWeaponPresentationBinding(string weaponId, string attackId)
        {
            IdleAutoDefenseContentSetRuntimeSettings settings = ContentSetRuntimeSettings;
            if (settings != null && settings.TryFindWeaponPresentationBinding(weaponId, attackId, out IdleAutoDefenseWeaponPresentationBinding authored) && authored != null)
            {
                _counters.AuthoredWeaponPresentationBindingCount++;
                return authored;
            }

            IdleAutoDefenseWeaponPresentationBinding[] defaults = IdleAutoDefenseWeaponPresentationBinding.CreateDefaultBindings();
            for (int i = 0; i < defaults.Length; i++)
            {
                if (defaults[i] != null && defaults[i].Matches(weaponId, attackId))
                {
                    _counters.FallbackWeaponPresentationBindingCount++;
                    return defaults[i];
                }
            }

            _counters.FallbackWeaponPresentationBindingCount++;
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

        internal WeaponDefinitionAsset FindWeaponDefinitionForPresentation(string weaponId, string attackId)
        {
            if (_content.Weapons == null || _content.Weapons.Length == 0)
                return null;

            if (!string.IsNullOrWhiteSpace(weaponId))
            {
                for (int i = 0; i < _content.Weapons.Length; i++)
                {
                    WeaponDefinitionAsset weapon = _content.Weapons[i];
                    if (weapon != null && string.Equals(weapon.Id, weaponId, StringComparison.OrdinalIgnoreCase))
                        return weapon;
                }
            }

            if (string.IsNullOrWhiteSpace(attackId))
                return null;

            for (int i = 0; i < _content.Weapons.Length; i++)
            {
                WeaponDefinitionAsset weapon = _content.Weapons[i];
                AttackDefinitionAsset attack = weapon != null && weapon.Stats != null ? weapon.Stats.Attack : null;
                if (attack != null && string.Equals(attack.Id, attackId, StringComparison.OrdinalIgnoreCase))
                    return weapon;
            }

            return null;
        }

        internal void CreatePlayAreaMarkers()
        {
            IReadOnlyList<IdleAutoDefenseModuleSlotPresentationBinding> slots = ResolveModuleSlotPresentationBindings();
            for (int i = 0; i < slots.Count; i++)
                CreateModuleSlot(slots[i]);
            if (!_queries.ShowDebugSpawnRing()) return;
            Color warningStrip = new Color(0.95f, 0.68f, 0.18f, 0.95f);
            float radius = _content.GameRules == null ? TemplateVisibleArenaRadius : _content.GameRules.VisibleArenaRadius;
            CreateKenneyMarkerLine("Outer Spawn Zone North", new Vector3(0f, 0f, radius), Quaternion.identity, 6, warningStrip);
            CreateKenneyMarkerLine("Outer Spawn Zone East", new Vector3(radius, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), 6, warningStrip);
            CreateKenneyMarkerLine("Outer Spawn Zone South", new Vector3(0f, 0f, -radius), Quaternion.identity, 6, warningStrip);
            CreateKenneyMarkerLine("Outer Spawn Zone West", new Vector3(-radius, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), 6, warningStrip);
        }

        internal IReadOnlyList<IdleAutoDefenseModuleSlotPresentationBinding> ResolveModuleSlotPresentationBindings()
        {
            IdleAutoDefenseContentSetRuntimeSettings settings = ContentSetRuntimeSettings;
            if (settings != null && settings.ModuleSlotPresentationBindings.Count > 0)
            {
                _counters.AuthoredModuleSlotPresentationBindingCount += settings.ModuleSlotPresentationBindings.Count;
                return settings.ModuleSlotPresentationBindings;
            }

            IdleAutoDefenseModuleSlotPresentationBinding[] defaults = IdleAutoDefenseModuleSlotPresentationBinding.CreateDefaultBindings();
            _counters.FallbackModuleSlotPresentationBindingCount += defaults.Length;
            return defaults;
        }

        internal void CreateModuleSlot(IdleAutoDefenseModuleSlotPresentationBinding binding)
        {
            if (binding == null || string.IsNullOrWhiteSpace(binding.ModelName)) return;
            GameObject slot = _assets.InstantiateKenneyModel(
                binding.ModelName,
                _resources.Root.transform,
                binding.LocalPosition,
                Quaternion.Euler(binding.LocalEulerAngles),
                binding.LocalScale,
                binding.Tint);
            if (slot != null)
            {
                slot.name = binding.DisplayName;
                _stamps.StampAuthoredVisibleInstance(slot, "ModuleSlotPresentation", binding.SlotId, binding.ModelName, binding.WeaponId, string.Empty, "ModuleSlot", _content.ContentSet == null ? null : _content.ContentSet.ContentSet);
            }
        }

        internal void CreateArenaBackdrop()
        {
            if (_resources.Root == null) return;
            GameObject arenaRoot = new GameObject("Kenney Arena Backdrop");
            arenaRoot.transform.SetParent(_resources.Root.transform, false);
            _stamps.StampAuthoredVisibleInstance(
                arenaRoot,
                "EnvironmentPresentation",
                "environment.idle-auto-defense.arena",
                arenaRoot.name,
                string.Empty,
                string.Empty,
                "ArenaBackdrop",
                _content.ContentSet == null ? null : _content.ContentSet.ContentSet);
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
                    _assets.InstantiateKenneyModel(model, arenaRoot.transform, new Vector3(x * 3f, -0.22f, z * 3f), Quaternion.identity, Vector3.one * 1.5f, tint);
                }
            }

            _assets.InstantiateKenneyModel("detail-rocks", arenaRoot.transform, new Vector3(-7.8f, 0f, 6.9f), Quaternion.Euler(0f, 20f, 0f), Vector3.one * 1.15f, Color.white);
            _assets.InstantiateKenneyModel("detail-tree", arenaRoot.transform, new Vector3(7.9f, 0f, -6.7f), Quaternion.Euler(0f, -25f, 0f), Vector3.one * 1.1f, Color.white);
            _assets.InstantiateKenneyModel("tile-crystal", arenaRoot.transform, new Vector3(8.7f, -0.12f, 7.9f), Quaternion.identity, Vector3.one * 0.82f, new Color(0.55f, 0.85f, 1f));
        }

        internal void CreateKenneyMarkerLine(string name, Vector3 center, Quaternion rotation, int count, Color tint)
        {
            for (int i = 0; i < count; i++)
            {
                float offset = (i - (count - 1) * 0.5f) * 2.8f;
                Vector3 local = rotation * new Vector3(offset, 0f, 0f);
                GameObject marker = _assets.InstantiateKenneyModel("tile-spawn", _resources.Root.transform, center + local + new Vector3(0f, -0.11f, 0f), rotation, Vector3.one * 0.52f, tint);
                if (marker != null)
                    marker.name = name + " Marker " + (i + 1).ToString(CultureInfo.InvariantCulture);
            }
        }

        internal void ConfigureGameplayCamera(Vector3 focus)
        {
            Camera camera = Camera.main != null ? Camera.main : UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                var cameraObject = new GameObject("Idle Auto Defense Camera");
                cameraObject.transform.SetParent(_resources.Root != null ? _resources.Root.transform : null, false);
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
            _resources.Shake.Bind(camera);
        }

        internal void ConfigureGameplayLighting()
        {
            Light existing = UnityEngine.Object.FindFirstObjectByType<Light>();
            if (existing != null)
            {
                existing.type = LightType.Directional;
                existing.transform.rotation = Quaternion.Euler(48f, -32f, 18f);
                existing.color = new Color(1f, 0.95f, 0.86f);
                existing.intensity = 1.18f;
                return;
            }

            GameObject lightObject = new GameObject("Idle Auto Defense Key Light");
            lightObject.transform.SetParent(_resources.Root != null ? _resources.Root.transform : null, false);
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 18f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.86f);
            light.intensity = 1.18f;
        }
    }
}
