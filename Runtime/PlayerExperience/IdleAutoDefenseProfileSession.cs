using System;
using Deucarian.IdleProgression;
using Deucarian.Progression;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    internal interface IIdleAutoDefenseProfileStorage : IDisposable
    {
        string ProfileScopeId { get; }
        string ProfileDocumentName { get; }
        IdleAutoDefensePlayerProfile Load();
        bool Save(IdleAutoDefensePlayerProfile profile);
        bool Reset();
    }

    /// <summary>One profile and store lifetime; navigation and presenters never load or dispose storage.</summary>
    internal sealed class IdleAutoDefenseProfileSession : IDisposable
    {
        private readonly IIdleAutoDefenseProfileStorage _store;
        private readonly IIdleAutoDefenseRunSession _run;
        private bool _disposed;
        internal IdleAutoDefensePlayerProfile Profile { get; private set; }
        internal IdleProgressionResult OfflinePreview { get; set; }
        internal DateTimeOffset OfflinePreviewNowUtc { get; private set; }
        internal string Error { get; private set; } = string.Empty;
        internal string ProfileScopeId => _store.ProfileScopeId;
        internal string ProfileDocumentName => _store.ProfileDocumentName;

        internal IdleAutoDefenseProfileSession(IIdleAutoDefenseProfileStorage store, IIdleAutoDefenseRunSession run)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _run = run ?? throw new ArgumentNullException(nameof(run));
        }

        internal void Initialize()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(IdleAutoDefenseProfileSession));
            if (Profile != null) return;
            Profile = _store.Load() ?? IdleAutoDefensePlayerProfile.CreateDefault();
            if (Profile.Progression != null && Profile.Progression.HasData && !_run.RestorePersistentProgression(Profile.Progression))
                Error = "Persistent progression could not be restored. Defaults remain active; reset progress or inspect storage diagnostics.";
        }

        internal void Persist(DateTimeOffset nowUtc)
        {
            if (_disposed || Profile == null) return;
            Profile.LastSeenUtcTicks = nowUtc.UtcTicks;
            Profile.Progression = _run.CapturePersistentProgression();
            if (!_store.Save(Profile)) Error = "Progress could not be saved. Check storage permissions and retry.";
        }

        internal bool Reset()
        {
            if (_disposed) return false;
            bool result = _store.Reset();
            Profile = IdleAutoDefensePlayerProfile.CreateDefault();
            return result;
        }

        internal void PrepareOfflinePreview(DateTimeOffset nowUtc)
        {
            OfflinePreviewNowUtc = nowUtc;
            var settings = _run.Snapshot.ActiveOfflineProgression;
            if (settings == null || !settings.Enabled)
            {
                OfflinePreview = null;
                return;
            }
            DateTimeOffset lastClaim = SafeUtc(Math.Max(Profile.LastSeenUtcTicks, Profile.LastOfflineClaimUtcTicks), nowUtc);
            OfflinePreview = settings.Calculate(lastClaim, nowUtc);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _store.Dispose();
        }

        internal static DateTimeOffset SafeUtc(long ticks, DateTimeOffset fallback)
        {
            if (ticks <= DateTimeOffset.MinValue.UtcTicks || ticks >= DateTimeOffset.MaxValue.UtcTicks) return fallback;
            try { return new DateTimeOffset(ticks, TimeSpan.Zero); }
            catch (ArgumentOutOfRangeException) { return fallback; }
        }

        internal static long GetRewardAmount(IdleProgressionResult result, string currencyId)
        {
            if (result == null || string.IsNullOrWhiteSpace(currencyId)) return 0L;
            for (int i = 0; i < result.Reward.CurrencyLines.Count; i++)
            {
                CurrencyLine line = result.Reward.CurrencyLines[i];
                if (string.Equals(line.CurrencyId.Value, currencyId, StringComparison.OrdinalIgnoreCase)) return line.Amount.Value;
            }
            return 0L;
        }
    }
}
