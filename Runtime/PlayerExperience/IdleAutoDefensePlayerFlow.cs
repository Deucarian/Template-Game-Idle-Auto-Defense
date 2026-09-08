namespace Deucarian.TemplateGameIdleAutoDefense
{
    public enum IdleAutoDefensePlayerFlowState
    {
        MainMenu = 0,
        Running = 1,
        Paused = 2,
        Tutorial = 3,
        RunSummary = 4,
        OfflineClaim = 5
    }

    internal enum IdleAutoDefensePauseSection { Main, Build, Settings }

    internal interface IIdleAutoDefenseRunClock
    {
        void Tick(float deltaSeconds);
    }

    /// <summary>Player navigation and simulation eligibility; independent of Unity UI and storage.</summary>
    internal sealed class IdleAutoDefensePlayerFlow
    {
        internal IdleAutoDefensePlayerFlowState State { get; set; } = IdleAutoDefensePlayerFlowState.MainMenu;
        internal IdleAutoDefensePlayerFlowState BeforeModal { get; set; }
        internal bool RunActive { get; set; }
        internal bool SummaryRecorded { get; set; }
        internal int TutorialStepIndex { get; set; }

        internal bool CanTick(bool portraitBlocked) =>
            RunActive && State == IdleAutoDefensePlayerFlowState.Running && !portraitBlocked;

        internal void Advance(IIdleAutoDefenseRunClock run, bool portraitBlocked, float deltaSeconds)
        {
            if (CanTick(portraitBlocked)) run.Tick(deltaSeconds);
        }

        internal void StartRun()
        {
            RunActive = true;
            SummaryRecorded = false;
            State = IdleAutoDefensePlayerFlowState.Running;
            BeforeModal = State;
        }

        internal void ReturnToMenu()
        {
            RunActive = false;
            SummaryRecorded = false;
            ShowMenu();
        }

        internal void ShowMenu()
        {
            State = IdleAutoDefensePlayerFlowState.MainMenu;
            BeforeModal = State;
        }

        internal void OpenTutorial(bool firstRun)
        {
            BeforeModal = firstRun ? IdleAutoDefensePlayerFlowState.Running : State;
            State = IdleAutoDefensePlayerFlowState.Tutorial;
            TutorialStepIndex = 0;
        }

        internal void CompleteTutorial() => State = RunActive
            ? IdleAutoDefensePlayerFlowState.Running : IdleAutoDefensePlayerFlowState.MainMenu;

        internal bool TryRecordSummary()
        {
            if (SummaryRecorded) return false;
            SummaryRecorded = true;
            RunActive = false;
            State = IdleAutoDefensePlayerFlowState.RunSummary;
            return true;
        }
    }
}
