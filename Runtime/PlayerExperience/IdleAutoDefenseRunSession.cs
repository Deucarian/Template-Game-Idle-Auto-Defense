using System;
using System.Collections.Generic;
using System.Globalization;
using Deucarian.IdleProgression;
using Deucarian.Progression;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    internal sealed class IdleAutoDefenseRunSession : IIdleAutoDefenseRunSession
    {
        private readonly IdleAutoDefenseTemplateController _controller;
        public IdleAutoDefenseRunSnapshot Snapshot { get; private set; }
        public IdleAutoDefenseRunSession(IdleAutoDefenseTemplateController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            Refresh();
        }
        public void Refresh() => Snapshot = new IdleAutoDefenseRunSnapshot(_controller);
        public void Tick(float deltaSeconds) => _controller.AdvanceFrame(deltaSeconds);

        public void RestartRun() { _controller.RestartRun(); Refresh(); }

        public bool RestorePersistentProgression(IdleAutoDefensePersistentProgressionData data) { var result = _controller.RestorePersistentProgression(data); Refresh(); return result; }

        public void ResetPersistentProgression() { _controller.ResetPersistentProgression(); Refresh(); }

        public bool TryPurchaseOverdrive() { var result = _controller.TryPurchaseOverdrive(); Refresh(); return result; }

        public bool TryPurchaseDamageUpgrade() { var result = _controller.TryPurchaseDamageUpgrade(); Refresh(); return result; }

        public bool TryPurchasePulseBeamModule() { var result = _controller.TryPurchasePulseBeamModule(); Refresh(); return result; }

        public bool TryPurchaseRangeUpgrade() { var result = _controller.TryPurchaseRangeUpgrade(); Refresh(); return result; }

        public bool TryPurchaseArcBurstModule() { var result = _controller.TryPurchaseArcBurstModule(); Refresh(); return result; }

        public bool TryPurchaseAttackSpeedUpgrade() { var result = _controller.TryPurchaseAttackSpeedUpgrade(); Refresh(); return result; }

        public bool TryPurchaseHomingPulseModule() { var result = _controller.TryPurchaseHomingPulseModule(); Refresh(); return result; }

        public bool TryPurchasePersistentUpgrade(string nodeId) { var result = _controller.TryPurchasePersistentUpgrade(nodeId); Refresh(); return result; }

        public bool TryChooseRewardDraftChoice(int choiceIndex) { var result = _controller.TryChooseRewardDraftChoice(choiceIndex); Refresh(); return result; }

        public IdleProgressionResult SimulateOfflineReward(DateTimeOffset lastSeenUtc, DateTimeOffset nowUtc) { var result = _controller.SimulateOfflineReward(lastSeenUtc, nowUtc); Refresh(); return result; }

        public IdleAutoDefensePersistentProgressionData CapturePersistentProgression() { var result = _controller.CapturePersistentProgression(); Refresh(); return result; }
    }
}
