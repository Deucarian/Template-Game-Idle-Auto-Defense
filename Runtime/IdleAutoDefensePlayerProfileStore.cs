using System;
using System.Collections.Generic;
using System.IO;
using Deucarian.Persistence;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    [Serializable]
    public sealed class IdleAutoDefensePlayerProfile
    {
        public float MasterVolume { get; set; } = 1f;
        public float UiVolume { get; set; } = 0.85f;
        public float CombatVolume { get; set; } = 0.75f;
        public float RewardWarningVolume { get; set; } = 0.9f;
        public float MotionIntensity { get; set; } = 0.7f;
        public string SelectedThemeId { get; set; } = string.Empty;
        public bool TutorialSeen { get; set; }
        public long LastSeenUtcTicks { get; set; }
        public long LastOfflineClaimUtcTicks { get; set; }
        public long LifetimeCredits { get; set; }
        public long LifetimeParts { get; set; }
        public int CompletedRuns { get; set; }
        public int FailedRuns { get; set; }
        public IdleAutoDefensePersistentProgressionData Progression { get; set; } = new IdleAutoDefensePersistentProgressionData();

        public static IdleAutoDefensePlayerProfile CreateDefault()
        {
            return new IdleAutoDefensePlayerProfile
            {
                LastSeenUtcTicks = DateTimeOffset.UtcNow.UtcTicks,
                LastOfflineClaimUtcTicks = DateTimeOffset.UtcNow.UtcTicks
            };
        }
    }

    [Serializable]
    public sealed class IdleAutoDefensePersistentProgressionData
    {
        public List<IdleAutoDefensePersistentLongValue> Balances { get; set; } = new List<IdleAutoDefensePersistentLongValue>();
        public List<IdleAutoDefensePersistentLongValue> Tracks { get; set; } = new List<IdleAutoDefensePersistentLongValue>();
        public List<IdleAutoDefensePersistentIntValue> ResearchRanks { get; set; } = new List<IdleAutoDefensePersistentIntValue>();
        public List<string> UnlockIds { get; set; } = new List<string>();

        public bool HasData => Balances != null && Balances.Count > 0 ||
            Tracks != null && Tracks.Count > 0 ||
            ResearchRanks != null && ResearchRanks.Count > 0 ||
            UnlockIds != null && UnlockIds.Count > 0;
    }

    [Serializable]
    public sealed class IdleAutoDefensePersistentLongValue
    {
        public string Id { get; set; } = string.Empty;
        public long Value { get; set; }
    }

    [Serializable]
    public sealed class IdleAutoDefensePersistentIntValue
    {
        public string Id { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    public sealed class IdleAutoDefensePlayerProfileStore : IDisposable, IIdleAutoDefenseProfileStorage
    {
        public const string DocumentName = "idle-auto-defense-player-profile";
        public const string DefaultProfileScopeId = "contentpack.idle-auto-defense.playable";
        private readonly PersistenceService _service;
        private readonly DocumentDefinition<IdleAutoDefensePlayerProfile> _definition;
        private readonly DocumentId _profileDocumentId;

        public IdleAutoDefensePlayerProfileStore(string rootPath = null, string profileScopeId = null)
        {
            string resolvedRoot = string.IsNullOrWhiteSpace(rootPath)
                ? Path.Combine(Application.persistentDataPath, "Deucarian", "IdleAutoDefense")
                : rootPath;
            ProfileScopeId = string.IsNullOrWhiteSpace(profileScopeId) ? DefaultProfileScopeId : profileScopeId.Trim();
            ProfileDocumentName = BuildDocumentName(ProfileScopeId);
            _profileDocumentId = new DocumentId(ProfileDocumentName);
            _service = new PersistenceService(new FileTextStorage(new FixedPathProvider(resolvedRoot)));
            _definition = new DocumentDefinition<IdleAutoDefensePlayerProfile>(
                _profileDocumentId,
                new SchemaVersion(1),
                IdleAutoDefensePlayerProfile.CreateDefault,
                new DelegateDocumentValidator<IdleAutoDefensePlayerProfile>(Validate),
                backupRetention: 3);
        }

        public string LastStatus { get; private set; } = string.Empty;
        public bool RecoveredFromBackup { get; private set; }
        public string ProfileScopeId { get; }
        public string ProfileDocumentName { get; }

        public IdleAutoDefensePlayerProfile Load()
        {
            try
            {
                LoadResult<IdleAutoDefensePlayerProfile> result = _service
                    .LoadAsync(_definition, SaveSlotId.Default)
                    .GetAwaiter()
                    .GetResult();
                RecoveredFromBackup = result.Outcome == LoadOutcome.RecoveredFromBackup;
                LastStatus = result.Message;
                if (result.Succeeded && result.Document != null)
                    return result.Document;
                LastStatus = string.IsNullOrWhiteSpace(result.Message)
                    ? "Saved profile could not be loaded. Defaults are active."
                    : result.Message;
            }
            catch (Exception exception)
            {
                LastStatus = exception.Message;
            }
            return IdleAutoDefensePlayerProfile.CreateDefault();
        }

        public bool Save(IdleAutoDefensePlayerProfile profile)
        {
            try
            {
                WriteResult result = _service
                    .SaveAsync(_definition, profile, SaveSlotId.Default)
                    .GetAwaiter()
                    .GetResult();
                LastStatus = result.Message;
                return result.Succeeded;
            }
            catch (Exception exception)
            {
                LastStatus = exception.Message;
                return false;
            }
        }

        public bool Reset()
        {
            try
            {
                WriteResult result = _service
                    .DeleteAsync(new DocumentLocation(_profileDocumentId, SaveSlotId.Default))
                    .GetAwaiter()
                    .GetResult();
                if (!result.Succeeded)
                {
                    LastStatus = result.Message;
                    return false;
                }
                WriteResult saveDefault = _service
                    .SaveAsync(_definition, IdleAutoDefensePlayerProfile.CreateDefault(), SaveSlotId.Default)
                    .GetAwaiter()
                    .GetResult();
                LastStatus = saveDefault.Message;
                return saveDefault.Succeeded;
            }
            catch (Exception exception)
            {
                LastStatus = exception.Message;
                return false;
            }
        }

        public void Dispose()
        {
            _service.Dispose();
        }

        private static ValidationResult Validate(IdleAutoDefensePlayerProfile profile)
        {
            if (profile == null) return ValidationResult.Failure("Player profile is missing.");
            if (!VolumeValid(profile.MasterVolume) || !VolumeValid(profile.UiVolume) ||
                !VolumeValid(profile.CombatVolume) || !VolumeValid(profile.RewardWarningVolume))
                return ValidationResult.Failure("Audio settings must be between zero and one.");
            if (!VolumeValid(profile.MotionIntensity))
                return ValidationResult.Failure("Motion intensity must be between zero and one.");
            if (profile.LifetimeCredits < 0 || profile.LifetimeParts < 0 || profile.CompletedRuns < 0 || profile.FailedRuns < 0)
                return ValidationResult.Failure("Progression totals cannot be negative.");
            profile.Progression ??= new IdleAutoDefensePersistentProgressionData();
            profile.Progression.Balances ??= new List<IdleAutoDefensePersistentLongValue>();
            profile.Progression.Tracks ??= new List<IdleAutoDefensePersistentLongValue>();
            profile.Progression.ResearchRanks ??= new List<IdleAutoDefensePersistentIntValue>();
            profile.Progression.UnlockIds ??= new List<string>();
            if (profile.Progression.Balances.Exists(value => value == null || string.IsNullOrWhiteSpace(value.Id) || value.Value < 0) ||
                profile.Progression.Tracks.Exists(value => value == null || string.IsNullOrWhiteSpace(value.Id) || value.Value < 0) ||
                profile.Progression.ResearchRanks.Exists(value => value == null || string.IsNullOrWhiteSpace(value.Id) || value.Value < 0))
                return ValidationResult.Failure("Persistent progression entries are invalid.");
            return ValidationResult.Success();
        }

        private static bool VolumeValid(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f && value <= 1f;
        }

        public static string BuildDocumentName(string profileScopeId)
        {
            if (string.IsNullOrWhiteSpace(profileScopeId) ||
                string.Equals(profileScopeId.Trim(), DefaultProfileScopeId, StringComparison.OrdinalIgnoreCase))
                return DocumentName;

            string scope = profileScopeId.Trim();
            var builder = new System.Text.StringBuilder(scope.Length);
            bool separator = false;
            for (int i = 0; i < scope.Length; i++)
            {
                char value = char.ToLowerInvariant(scope[i]);
                if (char.IsLetterOrDigit(value))
                {
                    builder.Append(value);
                    separator = false;
                }
                else if (!separator && builder.Length > 0)
                {
                    builder.Append('-');
                    separator = true;
                }
            }
            while (builder.Length > 0 && builder[builder.Length - 1] == '-') builder.Length--;
            return DocumentName + "__" + (builder.Length == 0 ? "pack" : builder.ToString());
        }
    }
}
