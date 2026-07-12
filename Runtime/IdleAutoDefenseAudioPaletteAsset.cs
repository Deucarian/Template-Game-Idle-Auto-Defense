using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    public enum IdleAutoDefenseAudioCategory
    {
        Ui = 0,
        Combat = 1,
        RewardWarning = 2
    }

    [Serializable]
    public sealed class IdleAutoDefenseAudioEventRecord
    {
        [SerializeField] private string _id;
        [SerializeField] private IdleAutoDefenseAudioCategory _category;
        [SerializeField] private AudioClip _clip;
        [SerializeField] private float _volume = 1f;
        [SerializeField] private float _minimumIntervalSeconds = 0.08f;

        public IdleAutoDefenseAudioEventRecord() { }

        public IdleAutoDefenseAudioEventRecord(
            string id,
            IdleAutoDefenseAudioCategory category,
            AudioClip clip,
            float volume,
            float minimumIntervalSeconds)
        {
            _id = id ?? string.Empty;
            _category = category;
            _clip = clip;
            _volume = volume;
            _minimumIntervalSeconds = minimumIntervalSeconds;
        }

        public string Id => _id ?? string.Empty;
        public IdleAutoDefenseAudioCategory Category => _category;
        public AudioClip Clip => _clip;
        public float Volume => Mathf.Clamp01(_volume);
        public float MinimumIntervalSeconds => Mathf.Max(0f, _minimumIntervalSeconds);
    }

    [CreateAssetMenu(menuName = "Deucarian/Idle Auto Defense/Audio Palette", fileName = "IdleAutoDefenseAudioPalette")]
    public sealed class IdleAutoDefenseAudioPaletteAsset : ScriptableObject
    {
        public static readonly string[] RequiredEventIds =
        {
            "ui.hover", "ui.select", "ui.back", "ui.error", "ui.purchase", "ui.insufficient-funds",
            "gameplay.attack-impact", "gameplay.enemy-death", "gameplay.elite-warning", "gameplay.boss-warning",
            "gameplay.base-damage", "gameplay.low-base-health", "gameplay.wave-start", "gameplay.wave-complete",
            "gameplay.overdrive-ready", "gameplay.overdrive-activate", "gameplay.overdrive-end",
            "reward.draft-open", "reward.card-select", "reward.rarity-reveal", "reward.epic", "reward.legendary",
            "flow.victory", "flow.defeat", "flow.run-summary", "flow.offline-claim"
        };

        [SerializeField] private string _id = "audio-palette.idle-auto-defense.playable";
        [SerializeField] private string _displayName = "Bastion Event Palette";
        [SerializeField] private IdleAutoDefenseAudioEventRecord[] _events = Array.Empty<IdleAutoDefenseAudioEventRecord>();

        public string Id => _id ?? string.Empty;
        public string DisplayName => _displayName ?? string.Empty;
        public IReadOnlyList<IdleAutoDefenseAudioEventRecord> Events => _events ?? Array.Empty<IdleAutoDefenseAudioEventRecord>();

        public IdleAutoDefenseAudioEventRecord Find(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId)) return null;
            for (int i = 0; i < Events.Count; i++)
                if (Events[i] != null && string.Equals(Events[i].Id, eventId, StringComparison.OrdinalIgnoreCase))
                    return Events[i];
            return null;
        }

        public void Configure(string id, string displayName, IReadOnlyList<IdleAutoDefenseAudioEventRecord> events)
        {
            _id = id ?? string.Empty;
            _displayName = displayName ?? string.Empty;
            if (events == null)
            {
                _events = Array.Empty<IdleAutoDefenseAudioEventRecord>();
                return;
            }
            _events = new IdleAutoDefenseAudioEventRecord[events.Count];
            for (int i = 0; i < events.Count; i++) _events[i] = events[i];
        }

        public bool IsValid(out string message)
        {
            if (string.IsNullOrWhiteSpace(Id))
            {
                message = "Audio palette ID is required.";
                return false;
            }

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < Events.Count; i++)
            {
                IdleAutoDefenseAudioEventRecord record = Events[i];
                if (record == null || string.IsNullOrWhiteSpace(record.Id) || !ids.Add(record.Id))
                {
                    message = "Audio events require unique IDs.";
                    return false;
                }
            }

            for (int i = 0; i < RequiredEventIds.Length; i++)
            {
                if (!ids.Contains(RequiredEventIds[i]))
                {
                    message = "Missing required audio event '" + RequiredEventIds[i] + "'.";
                    return false;
                }
            }

            message = string.Empty;
            return true;
        }

        public static IdleAutoDefenseAudioPaletteAsset CreateTransient()
        {
            var asset = CreateInstance<IdleAutoDefenseAudioPaletteAsset>();
            asset.hideFlags = HideFlags.HideAndDontSave;
            var records = new IdleAutoDefenseAudioEventRecord[RequiredEventIds.Length];
            for (int i = 0; i < records.Length; i++)
            {
                string id = RequiredEventIds[i];
                IdleAutoDefenseAudioCategory category = id.StartsWith("ui.", StringComparison.Ordinal)
                    ? IdleAutoDefenseAudioCategory.Ui
                    : id.StartsWith("gameplay.attack", StringComparison.Ordinal) || id.StartsWith("gameplay.enemy", StringComparison.Ordinal)
                        ? IdleAutoDefenseAudioCategory.Combat
                        : IdleAutoDefenseAudioCategory.RewardWarning;
                records[i] = new IdleAutoDefenseAudioEventRecord(id, category, null, 0.75f, category == IdleAutoDefenseAudioCategory.Combat ? 0.08f : 0.2f);
            }
            asset.Configure("audio-palette.idle-auto-defense.playable", "Bastion Event Palette", records);
            return asset;
        }
    }
}
