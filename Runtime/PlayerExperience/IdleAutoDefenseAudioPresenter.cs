using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    internal interface IIdleAutoDefenseAudioOutput : IDisposable
    {
        void Play(AudioClip clip, float volume);
    }

    /// <summary>Feedback edge detection and authored audio policy; output is replaceable.</summary>
    internal sealed class IdleAutoDefenseAudioPresenter : IDisposable
    {
        private readonly IIdleAutoDefenseAudioOutput _output;
        private readonly IdleAutoDefenseAudioPaletteAsset _palette;
        private readonly Func<IdleAutoDefensePlayerProfile> _profile;
        private readonly Func<float> _clock;
        private readonly Dictionary<string, float> _lastEventTimes = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        private int _kills, _damageEvents, _drafts, _wave;
        private bool _overdrive, _ready, _disposed;
        private long _threatId;

        internal IdleAutoDefenseAudioPresenter(IIdleAutoDefenseAudioOutput output,
            IdleAutoDefenseAudioPaletteAsset palette, Func<IdleAutoDefensePlayerProfile> profile, Func<float> clock = null)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _palette = palette;
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _clock = clock ?? (() => Time.unscaledTime);
        }

        internal void Observe(IdleAutoDefenseRunSnapshot state)
        {
            if (_disposed) return;
            int kills = state.DirectOrCombatKillCount + state.ProjectileAdapterKillCount;
            if (kills > _kills) Play("gameplay.enemy-death");
            if (state.ObjectiveDamageEvents > _damageEvents)
            {
                Play("gameplay.base-damage");
                if (state.ObjectiveMaximumHealth > 0d && state.ObjectiveHealth / state.ObjectiveMaximumHealth <= 0.25d)
                    Play("gameplay.low-base-health");
            }
            if (state.RewardDraftOpenedCount > _drafts) Play("reward.draft-open");
            if (state.CurrentWaveNumber > _wave && state.CurrentWaveNumber > 0) Play("gameplay.wave-start");
            if (state.OverdriveActive && !_overdrive) Play("gameplay.overdrive-activate");
            if (!state.OverdriveActive && _overdrive) Play("gameplay.overdrive-end");
            if (state.CanPurchaseOverdrive && !_ready) Play("gameplay.overdrive-ready");
            if (state.TryGetPrimaryMajorThreat(out var threat) && threat.InstanceId != _threatId)
            {
                _threatId = threat.InstanceId;
                Play(threat.Boss ? "gameplay.boss-warning" : "gameplay.elite-warning");
            }
            _kills = kills;
            _damageEvents = state.ObjectiveDamageEvents;
            _drafts = state.RewardDraftOpenedCount;
            _wave = state.CurrentWaveNumber;
            _overdrive = state.OverdriveActive;
            _ready = state.CanPurchaseOverdrive;
        }

        internal void Reset()
        {
            _kills = _damageEvents = _drafts = _wave = 0;
            _overdrive = _ready = false;
            _threatId = 0;
        }

        internal void Play(string eventId)
        {
            if (_disposed || _palette == null) return;
            var profile = _profile();
            var record = _palette.Find(eventId);
            if (profile == null || record == null || record.Clip == null) return;
            float now = _clock();
            if (_lastEventTimes.TryGetValue(record.Id, out float last) && now - last < record.MinimumIntervalSeconds) return;
            _lastEventTimes[record.Id] = now;
            float categoryVolume = record.Category == IdleAutoDefenseAudioCategory.Ui ? profile.UiVolume
                : record.Category == IdleAutoDefenseAudioCategory.Combat ? profile.CombatVolume : profile.RewardWarningVolume;
            _output.Play(record.Clip, record.Volume * categoryVolume * profile.MasterVolume);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _output.Dispose();
        }
    }
}
