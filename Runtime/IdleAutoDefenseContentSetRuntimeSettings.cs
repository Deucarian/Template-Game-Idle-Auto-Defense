using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    [Serializable]
    public sealed class IdleAutoDefenseContentSetRuntimeSettings
    {
        [SerializeField] private IdleAutoDefenseRewardDraftSettings _rewardDraftSettings = IdleAutoDefenseRewardDraftSettings.CreateDefault();
        [SerializeField] private IdleAutoDefenseRewardDraftCatalog _rewardDraftCatalog = IdleAutoDefenseRewardDraftCatalog.CreateDefault();
        [SerializeField] private IdleAutoDefensePresentationDebugSettings _presentationDebug = IdleAutoDefensePresentationDebugSettings.CreateDefault();
        [SerializeField] private IdleAutoDefenseWeaponPresentationBinding[] _weaponPresentationBindings = IdleAutoDefenseWeaponPresentationBinding.CreateDefaultBindings();

        public static IdleAutoDefenseContentSetRuntimeSettings CreateDefault()
        {
            return new IdleAutoDefenseContentSetRuntimeSettings();
        }

        public IdleAutoDefenseRewardDraftSettings RewardDraftSettings
        {
            get => _rewardDraftSettings ??= IdleAutoDefenseRewardDraftSettings.CreateDefault();
            set => _rewardDraftSettings = value ?? IdleAutoDefenseRewardDraftSettings.CreateDefault();
        }

        public IdleAutoDefenseRewardDraftCatalog RewardDraftCatalog
        {
            get => _rewardDraftCatalog ??= IdleAutoDefenseRewardDraftCatalog.CreateDefault();
            set => _rewardDraftCatalog = value ?? IdleAutoDefenseRewardDraftCatalog.CreateDefault();
        }

        public IdleAutoDefensePresentationDebugSettings PresentationDebug
        {
            get => _presentationDebug ??= IdleAutoDefensePresentationDebugSettings.CreateDefault();
            set => _presentationDebug = value ?? IdleAutoDefensePresentationDebugSettings.CreateDefault();
        }

        public IReadOnlyList<IdleAutoDefenseWeaponPresentationBinding> WeaponPresentationBindings
        {
            get
            {
                if (_weaponPresentationBindings == null || _weaponPresentationBindings.Length == 0)
                    _weaponPresentationBindings = IdleAutoDefenseWeaponPresentationBinding.CreateDefaultBindings();
                return _weaponPresentationBindings;
            }
        }

        public IdleAutoDefenseContentSetRuntimeSettings Clone()
        {
            return new IdleAutoDefenseContentSetRuntimeSettings
            {
                _rewardDraftSettings = RewardDraftSettings.Clone(),
                _rewardDraftCatalog = RewardDraftCatalog.Clone(),
                _presentationDebug = PresentationDebug.Clone(),
                _weaponPresentationBindings = CloneBindings(WeaponPresentationBindings)
            };
        }

        public bool TryFindWeaponPresentationBinding(string weaponId, string attackId, out IdleAutoDefenseWeaponPresentationBinding binding)
        {
            IReadOnlyList<IdleAutoDefenseWeaponPresentationBinding> bindings = WeaponPresentationBindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                IdleAutoDefenseWeaponPresentationBinding candidate = bindings[i];
                if (candidate != null && candidate.Matches(weaponId, attackId))
                {
                    binding = candidate;
                    return true;
                }
            }

            binding = null;
            return false;
        }

        private static IdleAutoDefenseWeaponPresentationBinding[] CloneBindings(IReadOnlyList<IdleAutoDefenseWeaponPresentationBinding> bindings)
        {
            if (bindings == null || bindings.Count == 0)
                return Array.Empty<IdleAutoDefenseWeaponPresentationBinding>();

            var copy = new IdleAutoDefenseWeaponPresentationBinding[bindings.Count];
            for (int i = 0; i < bindings.Count; i++)
                copy[i] = bindings[i] == null ? null : bindings[i].Clone();
            return copy;
        }
    }

    [Serializable]
    public sealed class IdleAutoDefensePresentationDebugSettings
    {
        [SerializeField] private bool _showDebugAimLines;
        [SerializeField] private bool _showDebugRanges;
        [SerializeField] private bool _showDebugSpawnRing;

        public static IdleAutoDefensePresentationDebugSettings CreateDefault()
        {
            return new IdleAutoDefensePresentationDebugSettings();
        }

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

        public bool AnyEnabled => _showDebugAimLines || _showDebugRanges || _showDebugSpawnRing;

        public IdleAutoDefensePresentationDebugSettings Clone()
        {
            return new IdleAutoDefensePresentationDebugSettings
            {
                _showDebugAimLines = _showDebugAimLines,
                _showDebugRanges = _showDebugRanges,
                _showDebugSpawnRing = _showDebugSpawnRing
            };
        }
    }

    [Serializable]
    public sealed class IdleAutoDefenseWeaponPresentationBinding
    {
        [SerializeField] private string _weaponId;
        [SerializeField] private string _attackId;
        [SerializeField] private string _displayName;
        [SerializeField] private Vector3 _mountLocalPosition;
        [SerializeField] private string _baseModelName;
        [SerializeField] private string _weaponModelName;
        [SerializeField] private Color _tint = Color.white;
        [SerializeField] private Vector3 _muzzleLocalPosition;
        [SerializeField] private float _turnSpeedDegrees = 320f;
        [SerializeField] private bool _startsUnlocked;

        public IdleAutoDefenseWeaponPresentationBinding()
        {
        }

        public IdleAutoDefenseWeaponPresentationBinding(
            string weaponId,
            string attackId,
            string displayName,
            Vector3 mountLocalPosition,
            string baseModelName,
            string weaponModelName,
            Color tint,
            Vector3 muzzleLocalPosition,
            float turnSpeedDegrees,
            bool startsUnlocked)
        {
            _weaponId = weaponId ?? string.Empty;
            _attackId = attackId ?? string.Empty;
            _displayName = displayName ?? string.Empty;
            _mountLocalPosition = mountLocalPosition;
            _baseModelName = baseModelName ?? string.Empty;
            _weaponModelName = weaponModelName ?? string.Empty;
            _tint = tint;
            _muzzleLocalPosition = muzzleLocalPosition;
            _turnSpeedDegrees = turnSpeedDegrees;
            _startsUnlocked = startsUnlocked;
        }

        public string WeaponId => _weaponId ?? string.Empty;
        public string AttackId => _attackId ?? string.Empty;
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? WeaponId : _displayName;
        public Vector3 MountLocalPosition => _mountLocalPosition;
        public string BaseModelName => _baseModelName ?? string.Empty;
        public string WeaponModelName => _weaponModelName ?? string.Empty;
        public Color Tint => _tint.a <= 0f ? Color.white : _tint;
        public Vector3 MuzzleLocalPosition => _muzzleLocalPosition;
        public float TurnSpeedDegrees => Mathf.Clamp(_turnSpeedDegrees, 30f, 720f);
        public bool StartsUnlocked => _startsUnlocked;

        public bool Matches(string weaponId, string attackId)
        {
            if (!string.IsNullOrWhiteSpace(weaponId) && string.Equals(WeaponId, weaponId, StringComparison.OrdinalIgnoreCase))
                return true;
            return !string.IsNullOrWhiteSpace(attackId) && string.Equals(AttackId, attackId, StringComparison.OrdinalIgnoreCase);
        }

        public IdleAutoDefenseWeaponPresentationBinding Clone()
        {
            return new IdleAutoDefenseWeaponPresentationBinding(
                WeaponId,
                AttackId,
                DisplayName,
                MountLocalPosition,
                BaseModelName,
                WeaponModelName,
                Tint,
                MuzzleLocalPosition,
                TurnSpeedDegrees,
                StartsUnlocked);
        }

        public static IdleAutoDefenseWeaponPresentationBinding[] CreateDefaultBindings()
        {
            return new[]
            {
                new IdleAutoDefenseWeaponPresentationBinding(
                    BasicIdleAutoDefenseGame.ShardLauncherWeaponId.Value,
                    BasicIdleAutoDefenseGame.ShardAttackId.Value,
                    "Shard Launcher",
                    new Vector3(0f, 0.06f, 1.05f),
                    "tower-round-bottom-a",
                    "weapon-ballista",
                    new Color(1f, 0.45f, 0.1f, 1f),
                    new Vector3(0f, 0.64f, 0.66f),
                    340f,
                    true),
                new IdleAutoDefenseWeaponPresentationBinding(
                    BasicIdleAutoDefenseGame.PulseCannonWeaponId.Value,
                    BasicIdleAutoDefenseGame.PulseAttackId.Value,
                    "Pulse Beam",
                    new Vector3(-1.02f, 0.06f, 0.36f),
                    "tower-square-bottom-a",
                    "weapon-turret",
                    new Color(0.15f, 0.75f, 1f, 1f),
                    new Vector3(0f, 0.44f, 0.72f),
                    420f,
                    false),
                new IdleAutoDefenseWeaponPresentationBinding(
                    BasicIdleAutoDefenseGame.ArcBurstTowerWeaponId.Value,
                    BasicIdleAutoDefenseGame.ArcBurstAttackId.Value,
                    "Arc Burst",
                    new Vector3(0f, 0.06f, -1.05f),
                    "tower-round-bottom-a",
                    "weapon-catapult",
                    new Color(0.95f, 0.55f, 0.15f, 1f),
                    new Vector3(0f, 0.5f, 0.82f),
                    240f,
                    false),
                new IdleAutoDefenseWeaponPresentationBinding(
                    BasicIdleAutoDefenseGame.HomingSpireWeaponId.Value,
                    BasicIdleAutoDefenseGame.HomingPulseAttackId.Value,
                    "Homing Pulse",
                    new Vector3(1.02f, 0.06f, 0.36f),
                    "tower-square-bottom-a",
                    "weapon-cannon",
                    new Color(0.65f, 0.35f, 1f, 1f),
                    new Vector3(0f, 0.42f, 0.8f),
                    320f,
                    false)
            };
        }
    }
}
