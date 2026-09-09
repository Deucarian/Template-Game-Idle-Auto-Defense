using System;
using Deucarian.Attacks.Authoring;
using Deucarian.Encounters;
using Deucarian.WeaponSystems.Authoring;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    internal sealed class IdleAutoDefenseContentNames
    {
        private readonly IdleAutoDefenseContentBinding _content;
        private readonly Func<EncounterRuntime> _encounter;

        internal IdleAutoDefenseContentNames(IdleAutoDefenseContentBinding content, Func<EncounterRuntime> encounter)
        {
            _content = content;
            _encounter = encounter;
        }

        internal string ResolveWeaponDisplayName(string weaponId)
        {
            for (int i = 0; i < _content.Weapons.Length; i++)
            {
                WeaponDefinitionAsset weapon = _content.Weapons[i];
                if (weapon == null || !string.Equals(weapon.Id, weaponId, StringComparison.OrdinalIgnoreCase)) continue;
                return string.IsNullOrWhiteSpace(weapon.DisplayName) ? weapon.Id : weapon.DisplayName;
            }

            if (string.Equals(weaponId, BasicIdleAutoDefenseGame.ShardLauncherWeaponId.Value, StringComparison.OrdinalIgnoreCase)) return "Shard Launcher";
            if (string.Equals(weaponId, BasicIdleAutoDefenseGame.PulseCannonWeaponId.Value, StringComparison.OrdinalIgnoreCase)) return "Pulse Beam";
            if (string.Equals(weaponId, BasicIdleAutoDefenseGame.ArcBurstTowerWeaponId.Value, StringComparison.OrdinalIgnoreCase)) return "Arc Burst";
            if (string.Equals(weaponId, BasicIdleAutoDefenseGame.HomingSpireWeaponId.Value, StringComparison.OrdinalIgnoreCase)) return "Homing Pulse";
            return string.IsNullOrWhiteSpace(weaponId) ? "Tower" : weaponId;
        }

        internal string ResolveCurrentSpawnProfileName()
        {
            if (_encounter() == null) return "None";
            EncounterSnapshot snapshot = _encounter().CreateSnapshot();
            string lastStarted = string.Empty;
            for (int i = 0; i < snapshot.Waves.Count; i++)
            {
                WaveProgressSnapshot wave = snapshot.Waves[i];
                if (wave.Started && !wave.Emitted)
                    return ResolveWaveDisplayName(wave.WaveId.Value);
                if (wave.Started)
                    lastStarted = wave.WaveId.Value;
            }

            return string.IsNullOrWhiteSpace(lastStarted) ? "None" : ResolveWaveDisplayName(lastStarted);
        }

        internal int ResolveCurrentWaveNumber()
        {
            if (_encounter() == null) return 0;
            EncounterSnapshot snapshot = _encounter().CreateSnapshot();
            int lastStarted = 0;
            for (int i = 0; i < snapshot.Waves.Count; i++)
            {
                WaveProgressSnapshot wave = snapshot.Waves[i];
                if (wave.Started) lastStarted = i + 1;
                if (wave.Started && !wave.Emitted) return i + 1;
            }

            return lastStarted;
        }

        internal string ResolveWaveDisplayName(string waveId)
        {
            if (string.IsNullOrWhiteSpace(waveId)) return "None";
            for (int i = 0; i < _content.Waves.Length; i++)
            {
                WaveDefinitionAsset wave = _content.Waves[i];
                if (wave == null || !string.Equals(wave.Id, waveId, StringComparison.OrdinalIgnoreCase)) continue;
                return string.IsNullOrWhiteSpace(wave.DisplayName) ? wave.Id : wave.DisplayName;
            }

            return waveId;
        }

    }
}
