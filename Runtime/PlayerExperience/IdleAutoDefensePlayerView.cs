using System;
using System.Collections.Generic;
using System.Globalization;
using Deucarian.IdleProgression;
using Deucarian.Progression;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    internal sealed class IdleAutoDefensePlayerView : IDisposable
    {
        internal VisualElement _playerUiRoot;

        internal VisualElement _safeAreaRoot;

        internal VisualElement _portraitOverlay;

        internal VisualElement _hudLayer;

        internal VisualElement _moduleBar;

        internal VisualElement _mainMenuOverlay;

        internal VisualElement _pauseOverlay;

        internal VisualElement _pauseBody;

        internal VisualElement _rewardOverlay;

        internal VisualElement _rewardCardRow;

        internal VisualElement _tutorialOverlay;

        internal VisualElement _offlineOverlay;

        internal VisualElement _summaryOverlay;

        internal VisualElement _resetConfirmationOverlay;

        internal VisualElement _debugPanel;

        internal VisualElement _threatBar;

        internal VisualElement _threatFill;

        internal Label _threatLabel;

        internal Label _threatMarker;

        internal Label _timerLabel;

        internal Label _waveLabel;

        internal Label _profileLabel;

        internal Label _healthLabel;

        internal VisualElement _healthFill;

        internal Label _currencyLabel;

        internal Label _levelLabel;

        internal VisualElement _xpFill;

        internal Label _overdriveStatusLabel;

        internal Button _overdriveButton;

        internal Label _toastLabel;

        internal Label _mainProfileLabel;

        internal Label _mainErrorLabel;

        internal Label _themeFallbackLabel;

        internal Label _tutorialProgressLabel;

        internal Label _tutorialTitleLabel;

        internal Label _tutorialBodyLabel;

        internal Label _offlineTitleLabel;

        internal Label _offlineBodyLabel;

        internal Label _summaryTitleLabel;

        internal Label _summaryBodyLabel;

        internal Label _debugLabel;

        internal Label _buildStatsLabel;

        internal Slider _masterSlider;

        internal Slider _uiSlider;

        internal Slider _combatSlider;

        internal Slider _rewardSlider;

        internal Slider _motionSlider;

        internal readonly Button[] _rewardButtons = new Button[3];

        internal readonly Button[] _moduleButtons = new Button[4];

        internal readonly Label[] _moduleLabels = new Label[4];

        internal bool _compactLayout;

        internal readonly IdleAutoDefensePlayerExperience App;
        private readonly Func<VisualElement> _getRuntimeRoot;
        internal VisualElement RuntimeRoot => _getRuntimeRoot();
        internal readonly IdleAutoDefenseHudPresenter Hud;
        internal readonly IdleAutoDefenseMenuPresenter Menu;
        internal readonly IdleAutoDefenseModalPresenter Modal;
        internal readonly IdleAutoDefenseStylePresenter Style;
        internal IdleAutoDefensePlayerView(IdleAutoDefensePlayerExperience app, Func<VisualElement> runtimeRoot)
        {
            App = app;
            _getRuntimeRoot = runtimeRoot ?? throw new ArgumentNullException(nameof(runtimeRoot));
            Hud = new IdleAutoDefenseHudPresenter(this);
            Menu = new IdleAutoDefenseMenuPresenter(this);
            Modal = new IdleAutoDefenseModalPresenter(this);
            Style = new IdleAutoDefenseStylePresenter(this);
        }
        public void Dispose()
        {
            _playerUiRoot?.UnregisterCallback<GeometryChangedEvent>(OnPlayerUiGeometryChanged);
            _playerUiRoot?.RemoveFromHierarchy();
        }

        internal void EnsureAttached()
        {
            VisualElement root = RuntimeRoot;
            if (_playerUiRoot != null && _playerUiRoot.parent != root) root.Add(_playerUiRoot);
        }

        internal static bool IsVisible(VisualElement element)
        {
            return element != null && element.style.display != DisplayStyle.None;
        }

        internal static void SetVisible(VisualElement element, bool visible)
        {
            if (element != null) element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        internal void ApplyTheme()
        {
            if (_playerUiRoot == null || App._activeTheme == null) return;
            ApplyThemeRecursive(_playerUiRoot);
            if (_mainMenuOverlay != null) _mainMenuOverlay.style.backgroundColor = Style.WithAlpha(App._activeTheme.Background, 0.94f);
            if (_timerLabel != null) _timerLabel.style.color = App._activeTheme.PrimaryText;
            if (_healthFill != null) _healthFill.style.backgroundColor = App._activeTheme.Success;
            if (_xpFill != null) _xpFill.style.backgroundColor = App._activeTheme.Accent;
            if (_themeFallbackLabel != null) _themeFallbackLabel.style.color = App._activeTheme.Warning;
            Hud.RefreshPlayerUi();
        }

        internal void ApplyThemeRecursive(VisualElement element)
        {
            if (element is Label label) label.style.color = App._activeTheme.PrimaryText;
            if (element is Button button)
            {
                button.style.backgroundColor = App._activeTheme.PanelRaised;
                button.style.color = App._activeTheme.PrimaryText;
                Style.SetBorder(button, 1, App._activeTheme.Accent, 6);
            }
            if (element.name != null && (element.name.Contains("panel") || element.name.Contains("objective-health")))
                element.style.backgroundColor = App._activeTheme.Panel;
            for (int i = 0; i < element.childCount; i++) ApplyThemeRecursive(element[i]);
        }

        internal void OnPlayerUiGeometryChanged(GeometryChangedEvent evt)
        {
            ApplyResponsiveLayout(evt.newRect.width, evt.newRect.height);
        }

        internal void ApplyResponsiveLayout(float width, float height)
        {
            if (_playerUiRoot == null || width <= 0f || height <= 0f) return;
            IdleAutoDefenseUiSettingsAsset settings = App._effectiveExperience.UiSettings;
            _compactLayout = ShouldUseCompactLayout(width, height, settings);
            bool portrait = ShouldShowPortraitMessage(width, height, settings);
            IdleAutoDefensePlayerView.SetVisible(_portraitOverlay, portrait);
            ApplySafeArea(width, height);
            if (_moduleBar != null) _moduleBar.style.height = _compactLayout ? 118 : 142;
            if (_toastLabel != null) _toastLabel.style.bottom = _compactLayout ? 132 : 160;
            for (int i = 0; i < _moduleButtons.Length; i++)
                if (_moduleButtons[i] != null) _moduleButtons[i].style.fontSize = _compactLayout ? 10 : 11;
            for (int i = 0; i < _rewardButtons.Length; i++)
                if (_rewardButtons[i] != null) _rewardButtons[i].style.fontSize = _compactLayout ? 12 : 15;
        }

        internal void ApplySafeArea(float panelWidth, float panelHeight)
        {
            if (_safeAreaRoot == null) return;
            if (!App._effectiveExperience.UiSettings.RespectSafeArea || Screen.width <= 0 || Screen.height <= 0)
            {
                _safeAreaRoot.style.paddingLeft = 0;
                _safeAreaRoot.style.paddingRight = 0;
                _safeAreaRoot.style.paddingTop = 0;
                _safeAreaRoot.style.paddingBottom = 0;
                return;
            }
            Vector4 insets = CalculateSafeAreaInsets(new Rect(0, 0, Screen.width, Screen.height), Screen.safeArea);
            _safeAreaRoot.style.paddingLeft = insets.x * panelWidth / Screen.width;
            _safeAreaRoot.style.paddingTop = insets.y * panelHeight / Screen.height;
            _safeAreaRoot.style.paddingRight = insets.z * panelWidth / Screen.width;
            _safeAreaRoot.style.paddingBottom = insets.w * panelHeight / Screen.height;
        }

        public static Vector4 CalculateSafeAreaInsets(Rect screen, Rect safeArea)
        {
            return new Vector4(
                Mathf.Max(0f, safeArea.xMin - screen.xMin),
                Mathf.Max(0f, screen.yMax - safeArea.yMax),
                Mathf.Max(0f, screen.xMax - safeArea.xMax),
                Mathf.Max(0f, safeArea.yMin - screen.yMin));
        }

        public static bool ShouldUseCompactLayout(float width, float height, IdleAutoDefenseUiSettingsAsset settings)
        {
            if (settings == null) return width < 1000f || height < 560f;
            return width < settings.CompactWidthThreshold || height < settings.CompactHeightThreshold;
        }

        public static bool ShouldShowPortraitMessage(float width, float height, IdleAutoDefenseUiSettingsAsset settings)
        {
            float threshold = settings == null ? 0.95f : settings.PortraitAspectThreshold;
            return height / Mathf.Max(1f, width) > threshold;
        }

        internal void HideAllModalOverlays()
        {
            IdleAutoDefensePlayerView.SetVisible(_mainMenuOverlay, false);
            IdleAutoDefensePlayerView.SetVisible(_pauseOverlay, false);
            IdleAutoDefensePlayerView.SetVisible(_rewardOverlay, false);
            IdleAutoDefensePlayerView.SetVisible(_tutorialOverlay, false);
            IdleAutoDefensePlayerView.SetVisible(_offlineOverlay, false);
            IdleAutoDefensePlayerView.SetVisible(_summaryOverlay, false);
            IdleAutoDefensePlayerView.SetVisible(_resetConfirmationOverlay, false);
        }

        internal void BuildPlayerUi()
        {
            VisualElement runtimeRoot = RuntimeRoot;
            _playerUiRoot?.RemoveFromHierarchy();
            _playerUiRoot = new VisualElement { name = "idle-player-experience" };
            Style.FillAbsolute(_playerUiRoot);
            _playerUiRoot.pickingMode = PickingMode.Ignore;
            runtimeRoot.Add(_playerUiRoot);

            _safeAreaRoot = new VisualElement { name = "safe-area" };
            Style.FillAbsolute(_safeAreaRoot);
            _safeAreaRoot.pickingMode = PickingMode.Ignore;
            _playerUiRoot.Add(_safeAreaRoot);

            Hud.BuildHud();
            Menu.BuildMainMenu();
            Menu.BuildPauseMenu();
            Modal.BuildRewardDraft();
            Modal.BuildTutorial();
            Modal.BuildOfflineClaim();
            Modal.BuildRunSummary();
            Modal.BuildResetConfirmation();
            Modal.BuildPortraitMessage();
            Modal.BuildDebugPanel();

            _playerUiRoot.RegisterCallback<GeometryChangedEvent>(OnPlayerUiGeometryChanged);
            ApplyTheme();
            ApplyResponsiveLayout(_playerUiRoot.resolvedStyle.width, _playerUiRoot.resolvedStyle.height);
        }
    }
}
