namespace Deucarian.TemplateGameIdleAutoDefense
{
    internal sealed class IdleAutoDefensePresentationCounters
    {
        internal int AttackVfxSpawnCount;
        internal int BeamVisualSpawnCount;
        internal int BeamVisualInvalidEndpointCount;
        internal int AttackAudioPlayCount;
        internal int EnemyPresentationEventCount;
        internal int Kenney3DModelSpawnCount;
        internal int TurretAimUpdateCount;
        internal int MuzzleFlashSpawnCount;
        internal int RecoilEventCount;
        internal int EnemyFacingUpdateCount;
        internal int EnemyHitFlashCount;
        internal int EnemyDeathPopCount;
        internal int AuthoredVisibleInstanceStampCount;
        internal int FallbackVisibleGameplaySpawnCount;
        internal int AuthoredWeaponPresentationSpawnCount;
        internal int FallbackWeaponPresentationSpawnCount;
        internal int AuthoredWeaponPresentationBindingCount;
        internal int FallbackWeaponPresentationBindingCount;
        internal int AuthoredObjectivePresentationBindingCount;
        internal int FallbackObjectivePresentationBindingCount;
        internal int AuthoredModuleSlotPresentationBindingCount;
        internal int FallbackModuleSlotPresentationBindingCount;
        internal int DebugAimTracerSpawnCount;

        internal void Reset()
        {
            AttackVfxSpawnCount = 0;
            BeamVisualSpawnCount = 0;
            BeamVisualInvalidEndpointCount = 0;
            AttackAudioPlayCount = 0;
            EnemyPresentationEventCount = 0;
            Kenney3DModelSpawnCount = 0;
            TurretAimUpdateCount = 0;
            MuzzleFlashSpawnCount = 0;
            RecoilEventCount = 0;
            EnemyFacingUpdateCount = 0;
            EnemyHitFlashCount = 0;
            EnemyDeathPopCount = 0;
            AuthoredVisibleInstanceStampCount = 0;
            FallbackVisibleGameplaySpawnCount = 0;
            AuthoredWeaponPresentationSpawnCount = 0;
            FallbackWeaponPresentationSpawnCount = 0;
            AuthoredWeaponPresentationBindingCount = 0;
            FallbackWeaponPresentationBindingCount = 0;
            AuthoredObjectivePresentationBindingCount = 0;
            FallbackObjectivePresentationBindingCount = 0;
            AuthoredModuleSlotPresentationBindingCount = 0;
            FallbackModuleSlotPresentationBindingCount = 0;
            DebugAimTracerSpawnCount = 0;
        }
    }
}
