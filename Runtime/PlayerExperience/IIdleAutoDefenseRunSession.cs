using System;
using System.Collections.Generic;
using System.Globalization;
using Deucarian.IdleProgression;
using Deucarian.Progression;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    internal interface IIdleAutoDefenseRunSession : IIdleAutoDefenseRunClock
    {
        IdleAutoDefenseRunSnapshot Snapshot { get; }
        void Refresh();
        void RestartRun();
        bool RestorePersistentProgression(IdleAutoDefensePersistentProgressionData data);
        void ResetPersistentProgression();
        bool TryPurchaseOverdrive();
        bool TryPurchaseDamageUpgrade();
        bool TryPurchasePulseBeamModule();
        bool TryPurchaseRangeUpgrade();
        bool TryPurchaseArcBurstModule();
        bool TryPurchaseAttackSpeedUpgrade();
        bool TryPurchaseHomingPulseModule();
        bool TryPurchasePersistentUpgrade(string nodeId);
        bool TryChooseRewardDraftChoice(int choiceIndex);
        IdleProgressionResult SimulateOfflineReward(DateTimeOffset lastSeenUtc, DateTimeOffset nowUtc);
        IdleAutoDefensePersistentProgressionData CapturePersistentProgression();
    }
}
