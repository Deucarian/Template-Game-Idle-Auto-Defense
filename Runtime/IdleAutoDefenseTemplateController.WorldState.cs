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
    public partial class IdleAutoDefenseTemplateController
    {
        private readonly IdleAutoDefensePresentationCounters _presentationCounters = new IdleAutoDefensePresentationCounters();
        private IdleAutoDefenseWorldPresentation _worldPresentation;
        private IdleAutoDefenseWorldPresentation WorldPresentation => _worldPresentation ??= new IdleAutoDefenseWorldPresentation(
            ContentBinding,
            new IdleAutoDefensePresentationQueries(FindEnemyDefinitionForPresentation, FindAttackRecipeForProjectile,
                FindAttackRecipeForPresentation, ResolvePresentationRange, TrySelectPresentationEnemyWithinRange, TryFindActiveEnemy,
                () => _showDebugSpawnRing),
            () => _showDebugAimLines, () => SurvivalSeconds, _presentationCounters);
        private void DisposeWorldPresentation(bool destroySceneObjects) { _worldPresentation?.Dispose(destroySceneObjects); _worldPresentation = null; }
        private GameObject _enemyPrefab { get => WorldPresentation.Resources.EnemyPrefab; set => WorldPresentation.Resources.EnemyPrefab = value; }

        private GameObject _projectilePrefab { get => WorldPresentation.Resources.ProjectilePrefab; set => WorldPresentation.Resources.ProjectilePrefab = value; }

        private GameObject _root { get => WorldPresentation.Resources.Root; set => WorldPresentation.Resources.Root = value; }

        private Dictionary<string, GameObject> _runtimeEnemyPrefabs { get => WorldPresentation.Resources.EnemyPrefabs; }

        private Dictionary<string, GameObject> _runtimeProjectilePrefabs { get => WorldPresentation.Resources.ProjectilePrefabs; }

        private Dictionary<string, IdleAutoDefenseWeaponVisualBinding> _weaponVisualBindings => WorldPresentation.Targets.WeaponBindings;

        private Dictionary<long, IdleAutoDefenseEnemyModelPresentation> _enemyPresentationsById => WorldPresentation.Targets.EnemyBindings;

        private IdleAutoDefenseBeamVisuals _beamVisuals { get => WorldPresentation.Beams.Active; set => WorldPresentation.Beams.Active = value; }

        private AudioSource _runtimeAudioSource { get => WorldPresentation.Resources.AudioSource; set => WorldPresentation.Resources.AudioSource = value; }

        private AudioClip _fallbackPresentationClip { get => WorldPresentation.Resources.FallbackClip; set => WorldPresentation.Resources.FallbackClip = value; }

        private bool _fallbackPresentationClipIsRuntimeOwned { get => WorldPresentation.Resources.FallbackClipOwned; set => WorldPresentation.Resources.FallbackClipOwned = value; }

        private IdleAutoDefenseCameraShake _cameraShake { get => WorldPresentation.Resources.CameraShake; set => WorldPresentation.Resources.CameraShake = value; }

        public int AttackVfxSpawnCount { get => _presentationCounters.AttackVfxSpawnCount; private set => _presentationCounters.AttackVfxSpawnCount = value; }

        public int BeamVisualSpawnCount { get => _presentationCounters.BeamVisualSpawnCount; private set => _presentationCounters.BeamVisualSpawnCount = value; }

        public int BeamVisualInvalidEndpointCount { get => _presentationCounters.BeamVisualInvalidEndpointCount; private set => _presentationCounters.BeamVisualInvalidEndpointCount = value; }

        public int AttackAudioPlayCount { get => _presentationCounters.AttackAudioPlayCount; private set => _presentationCounters.AttackAudioPlayCount = value; }

        public int EnemyPresentationEventCount { get => _presentationCounters.EnemyPresentationEventCount; private set => _presentationCounters.EnemyPresentationEventCount = value; }

        public int Kenney3DModelSpawnCount { get => _presentationCounters.Kenney3DModelSpawnCount; private set => _presentationCounters.Kenney3DModelSpawnCount = value; }

        public int TurretAimUpdateCount { get => _presentationCounters.TurretAimUpdateCount; private set => _presentationCounters.TurretAimUpdateCount = value; }

        public int MuzzleFlashSpawnCount { get => _presentationCounters.MuzzleFlashSpawnCount; private set => _presentationCounters.MuzzleFlashSpawnCount = value; }

        public int RecoilEventCount { get => _presentationCounters.RecoilEventCount; private set => _presentationCounters.RecoilEventCount = value; }

        public int EnemyFacingUpdateCount { get => _presentationCounters.EnemyFacingUpdateCount; private set => _presentationCounters.EnemyFacingUpdateCount = value; }

        public int EnemyHitFlashCount { get => _presentationCounters.EnemyHitFlashCount; private set => _presentationCounters.EnemyHitFlashCount = value; }

        public int EnemyDeathPopCount { get => _presentationCounters.EnemyDeathPopCount; private set => _presentationCounters.EnemyDeathPopCount = value; }

        public int AuthoredVisibleInstanceStampCount { get => _presentationCounters.AuthoredVisibleInstanceStampCount; private set => _presentationCounters.AuthoredVisibleInstanceStampCount = value; }

        public int FallbackVisibleGameplaySpawnCount { get => _presentationCounters.FallbackVisibleGameplaySpawnCount; private set => _presentationCounters.FallbackVisibleGameplaySpawnCount = value; }

        public int AuthoredWeaponPresentationSpawnCount { get => _presentationCounters.AuthoredWeaponPresentationSpawnCount; private set => _presentationCounters.AuthoredWeaponPresentationSpawnCount = value; }

        public int FallbackWeaponPresentationSpawnCount { get => _presentationCounters.FallbackWeaponPresentationSpawnCount; private set => _presentationCounters.FallbackWeaponPresentationSpawnCount = value; }

        public int AuthoredWeaponPresentationBindingCount { get => _presentationCounters.AuthoredWeaponPresentationBindingCount; private set => _presentationCounters.AuthoredWeaponPresentationBindingCount = value; }

        public int FallbackWeaponPresentationBindingCount { get => _presentationCounters.FallbackWeaponPresentationBindingCount; private set => _presentationCounters.FallbackWeaponPresentationBindingCount = value; }

        public int AuthoredObjectivePresentationBindingCount { get => _presentationCounters.AuthoredObjectivePresentationBindingCount; private set => _presentationCounters.AuthoredObjectivePresentationBindingCount = value; }

        public int FallbackObjectivePresentationBindingCount { get => _presentationCounters.FallbackObjectivePresentationBindingCount; private set => _presentationCounters.FallbackObjectivePresentationBindingCount = value; }

        public int AuthoredModuleSlotPresentationBindingCount { get => _presentationCounters.AuthoredModuleSlotPresentationBindingCount; private set => _presentationCounters.AuthoredModuleSlotPresentationBindingCount = value; }

        public int FallbackModuleSlotPresentationBindingCount { get => _presentationCounters.FallbackModuleSlotPresentationBindingCount; private set => _presentationCounters.FallbackModuleSlotPresentationBindingCount = value; }

        public int DebugAimTracerSpawnCount { get => _presentationCounters.DebugAimTracerSpawnCount; private set => _presentationCounters.DebugAimTracerSpawnCount = value; }
    }
}
