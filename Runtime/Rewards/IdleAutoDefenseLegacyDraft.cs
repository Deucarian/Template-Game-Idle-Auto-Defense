using System;
using Deucarian.Monetization;
using Deucarian.RunUpgrades;
using Deucarian.RunUpgrades.Authoring;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    internal sealed class IdleAutoDefenseLegacyDraft
    {
        private readonly IdleAutoDefenseRunBuild _build;
        private RunUpgradeCatalog _catalog;
        private RunUpgradeState _state;
        internal RunUpgradeDraft CurrentDraft { get; private set; }
        internal int TickCount { get; private set; }

        internal IdleAutoDefenseLegacyDraft(IdleAutoDefenseRunBuild build) => _build = build;

        internal void Bind(RunUpgradeDefinitionAsset[] definitions, bool authored)
        {
            _catalog = authored ? BasicIdleAutoDefenseGame.CreateRunUpgradeCatalogOrEmpty(definitions)
                : BasicIdleAutoDefenseGame.CreateRunUpgradeCatalog(definitions);
            _state = new RunUpgradeState();
        }

        internal void Clear()
        {
            _catalog = null;
            _state = null;
            CurrentDraft = null;
        }

        internal void ResetCounters() => TickCount = 0;

        internal void Reroll(int encounterSeed)
        {
            if (_catalog == null) return;
            CurrentDraft = RunUpgradeDraftService.Generate(_catalog, _state,
                new RunUpgradeDraftRequest(3, encounterSeed + Math.Max(1, TickCount)));
        }

        internal void DraftAndApplyIfDue(int ticks)
        {
            if (_catalog == null) return;
            TickCount += ticks;
            if (TickCount % 30 != 0) return;
            CurrentDraft = RunUpgradeDraftService.Generate(_catalog, _state, new RunUpgradeDraftRequest(3, 20260623, TickCount / 30));
            if (CurrentDraft.Choices.Count == 0) return;
            RunUpgradeSelectionResult selected = _state.Select(_catalog, CurrentDraft.Choices[0].Id);
            if (!selected.Succeeded) return;
            _build.RecordSelection();
            _build.ApplyUpgrade(CurrentDraft.Choices[0]);
        }
    }
}
