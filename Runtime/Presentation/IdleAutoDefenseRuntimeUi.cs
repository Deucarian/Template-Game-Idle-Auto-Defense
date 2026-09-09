using System;
using System.Collections.Generic;
using System.Globalization;
using Deucarian.Common;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    /// <summary>Owns the runtime panel and world-space floating feedback, independently of run state.</summary>
    internal sealed class IdleAutoDefenseRuntimeUi : IDisposable
    {
        private const float RuntimeUiFallbackWidth = 1280f;
        private const float RuntimeUiFallbackHeight = 720f;
        private readonly Func<Transform> _runtimeParent;
        private readonly List<DamageNumberView> _damageNumbers = new List<DamageNumberView>();
        private UIDocument _runtimeUiDocument;
        private PanelSettings _runtimePanelSettings;
        private GameObject _runtimeUiObject;
        private VisualElement _runtimeUiRoot;
        private VisualElement _damageNumberLayer;
        private bool _disposed;

        internal IdleAutoDefenseRuntimeUi(Func<Transform> runtimeParent)
        {
            _runtimeParent = runtimeParent ?? throw new ArgumentNullException(nameof(runtimeParent));
        }

        internal bool RuntimeUiDocumentReady => _runtimeUiDocument != null && _runtimeUiRoot != null && _damageNumberLayer != null;
        internal bool RuntimeUiThemeAssigned => _runtimePanelSettings != null && (_runtimePanelSettings.themeStyleSheet != null || RuntimeUiDirectStylesApplied);
        internal bool RuntimeUiDirectStylesApplied { get; private set; }
        internal int DamageNumberSpawnCount { get; private set; }
        internal int UpgradeFeedbackSpawnCount { get; private set; }
        internal int VisibleCount => _damageNumbers.Count;

        internal void ResetFeedback()
        {
            ClearDamageNumbers();
            DamageNumberSpawnCount = 0;
            UpgradeFeedbackSpawnCount = 0;
        }

        public void Dispose() => Release(true);

        // Scene teardown owns scene objects when false. The transient panel is
        // always ours; a discovered theme is borrowed and never destroyed here.
        internal void Release(bool destroySceneObjects)
        {
            if (_disposed) return;
            _disposed = true;
            ClearDamageNumbers();
            UnityObjectUtility.DestroySafely(_runtimePanelSettings);
            if (destroySceneObjects) UnityObjectUtility.DestroySafely(_runtimeUiObject);
            _runtimePanelSettings = null;
            _runtimeUiDocument = null;
            _runtimeUiObject = null;
            _runtimeUiRoot = null;
            _damageNumberLayer = null;
            RuntimeUiDirectStylesApplied = false;
        }

        internal UIDocument EnsureRuntimeUiDocument()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(IdleAutoDefenseRuntimeUi));
            if (_runtimeUiDocument != null && _runtimeUiRoot != null && _damageNumberLayer != null)
                return _runtimeUiDocument;

            if (_runtimeUiObject == null)
            {
                _runtimeUiObject = new GameObject("Basic Idle Auto Defense UI");
                Transform parent = _runtimeParent();
                if (parent != null)
                    _runtimeUiObject.transform.SetParent(parent, false);
            }

            _runtimeUiObject.SetActive(true);
            _runtimeUiObject.transform.SetAsLastSibling();
            _runtimePanelSettings ??= CreateRuntimePanelSettings();
            _runtimeUiDocument = _runtimeUiObject.GetComponent<UIDocument>();
            if (_runtimeUiDocument == null)
                _runtimeUiDocument = _runtimeUiObject.AddComponent<UIDocument>();
            _runtimeUiDocument.panelSettings = _runtimePanelSettings;
            _runtimeUiDocument.sortingOrder = 32767;
            _runtimeUiDocument.enabled = true;

            _runtimeUiRoot = _runtimeUiDocument.rootVisualElement;
            _runtimeUiRoot.name = "idle-auto-defense-ui-root";
            ApplyRuntimeUiRootStyles(_runtimeUiRoot, PickingMode.Position);
            RuntimeUiDirectStylesApplied = true;

            _damageNumberLayer = _runtimeUiRoot.Q<VisualElement>("damage-number-layer");
            if (_damageNumberLayer == null)
            {
                _damageNumberLayer = new VisualElement { name = "damage-number-layer", pickingMode = PickingMode.Ignore };
                ApplyRuntimeUiRootStyles(_damageNumberLayer, PickingMode.Ignore);
                _runtimeUiRoot.Add(_damageNumberLayer);
            }
            else
            {
                ApplyRuntimeUiRootStyles(_damageNumberLayer, PickingMode.Ignore);
            }

            return _runtimeUiDocument;
        }

        internal VisualElement RuntimeUiRoot
        {
            get
            {
                EnsureRuntimeUiDocument();
                return _runtimeUiRoot;
            }
        }

        private PanelSettings CreateRuntimePanelSettings()
        {
            PanelSettings settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.name = "Basic Idle Auto Defense Runtime Panel Settings";
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int((int)RuntimeUiFallbackWidth, (int)RuntimeUiFallbackHeight);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 0.5f;
            settings.scale = 1f;
            settings.sortingOrder = 32767;
            settings.clearColor = false;
            settings.clearDepthStencil = false;
            settings.colorClearValue = Color.clear;
            settings.targetDisplay = 0;
            settings.targetTexture = null;
            ThemeStyleSheet themeStyleSheet = ResolveRuntimeThemeStyleSheet();
            if (themeStyleSheet != null)
                settings.themeStyleSheet = themeStyleSheet;
            settings.hideFlags = HideFlags.HideAndDontSave;
            return settings;
        }

        private static void ApplyRuntimeUiRootStyles(VisualElement element, PickingMode pickingMode)
        {
            if (element == null) return;
            element.pickingMode = pickingMode;
            element.style.display = DisplayStyle.Flex;
            element.style.visibility = Visibility.Visible;
            element.style.opacity = 1f;
            element.style.position = Position.Absolute;
            element.style.left = 0;
            element.style.right = 0;
            element.style.top = 0;
            element.style.bottom = 0;
            element.style.width = Length.Percent(100);
            element.style.height = Length.Percent(100);
            element.style.minWidth = 0;
            element.style.minHeight = 0;
            element.style.backgroundColor = Color.clear;
            element.style.flexDirection = FlexDirection.Column;
            element.style.flexGrow = 1f;
            element.style.overflow = Overflow.Visible;
        }

        internal static void ApplyRuntimeUiFont(VisualElement element)
        {
            if (element == null) return;
            Font font = ResolveRuntimeUiFont();
            if (font != null)
                element.style.unityFont = font;
        }

        private static Font ResolveRuntimeUiFont()
        {
            Font legacyRuntimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (legacyRuntimeFont != null)
                return legacyRuntimeFont;
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static ThemeStyleSheet ResolveRuntimeThemeStyleSheet()
        {
            ThemeStyleSheet packageTheme = Resources.Load<ThemeStyleSheet>("IdleAutoDefenseRuntimeTheme");
            if (packageTheme != null)
                return packageTheme;

            UnityEngine.Object[] loadedThemes = Resources.FindObjectsOfTypeAll(typeof(ThemeStyleSheet));
            foreach (UnityEngine.Object loadedTheme in loadedThemes)
            {
                if (loadedTheme is ThemeStyleSheet themeStyleSheet)
                    return themeStyleSheet;
            }

            UnityEngine.Object[] resourceThemes = Resources.LoadAll(string.Empty, typeof(ThemeStyleSheet));
            foreach (UnityEngine.Object resourceTheme in resourceThemes)
            {
                if (resourceTheme is ThemeStyleSheet themeStyleSheet)
                    return themeStyleSheet;
            }

            return null;
        }

        internal void EmitDamageNumber(Vector3 worldPosition, double amount, Color color, string prefix)
        {
            if (amount <= 0d) return;
            EnsureRuntimeUiDocument();
            if (_damageNumberLayer == null) return;

            string text = (prefix ?? string.Empty) + Math.Ceiling(amount).ToString(CultureInfo.InvariantCulture);
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.name = "damage-number";
            label.style.position = Position.Absolute;
            ApplyRuntimeUiFont(label);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.fontSize = 24;
            label.style.color = color;
            label.style.backgroundColor = new Color(0f, 0f, 0f, 0.28f);
            label.style.borderTopLeftRadius = 12;
            label.style.borderTopRightRadius = 12;
            label.style.borderBottomLeftRadius = 12;
            label.style.borderBottomRightRadius = 12;
            label.style.unityTextOutlineColor = new Color(0f, 0f, 0f, 0.85f);
            label.style.unityTextOutlineWidth = 2f;
            label.style.width = 90;
            label.style.height = 32;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            _damageNumberLayer.Add(label);

            DamageNumberSpawnCount++;
            _damageNumbers.Add(new DamageNumberView(label, worldPosition, 0f));
            PositionDamageNumber(_damageNumbers[_damageNumbers.Count - 1], 0f);
        }

        internal void EmitFloatingStatusText(Vector3 worldPosition, string text, Color color)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            EnsureRuntimeUiDocument();
            if (_damageNumberLayer == null) return;

            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.name = "upgrade-feedback";
            label.style.position = Position.Absolute;
            ApplyRuntimeUiFont(label);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.fontSize = 20;
            label.style.color = color;
            label.style.backgroundColor = new Color(0.01f, 0.015f, 0.02f, 0.78f);
            label.style.borderTopColor = color;
            label.style.borderBottomColor = color;
            label.style.borderLeftColor = color;
            label.style.borderRightColor = color;
            label.style.borderTopWidth = 2;
            label.style.borderBottomWidth = 1;
            label.style.borderLeftWidth = 1;
            label.style.borderRightWidth = 1;
            label.style.borderTopLeftRadius = 14;
            label.style.borderTopRightRadius = 14;
            label.style.borderBottomLeftRadius = 14;
            label.style.borderBottomRightRadius = 14;
            label.style.unityTextOutlineColor = new Color(0f, 0f, 0f, 0.9f);
            label.style.unityTextOutlineWidth = 2f;
            label.style.width = 292;
            label.style.height = 36;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            _damageNumberLayer.Add(label);

            DamageNumberSpawnCount++;
            UpgradeFeedbackSpawnCount++;
            _damageNumbers.Add(new DamageNumberView(label, worldPosition, 0f));
            PositionDamageNumber(_damageNumbers[_damageNumbers.Count - 1], 0f);
        }

        internal void UpdateDamageNumbers(float deltaSeconds)
        {
            if (_damageNumbers.Count == 0) return;
            float safeDelta = Mathf.Max(0.016f, deltaSeconds);
            for (int i = _damageNumbers.Count - 1; i >= 0; i--)
            {
                DamageNumberView number = _damageNumbers[i];
                number.ElapsedSeconds += safeDelta;
                if (number.ElapsedSeconds >= 1.15f || number.Label == null)
                {
                    number.Label?.RemoveFromHierarchy();
                    _damageNumbers.RemoveAt(i);
                    continue;
                }

                PositionDamageNumber(number, number.ElapsedSeconds);
                float alpha = Mathf.Clamp01(1f - number.ElapsedSeconds / 1.15f);
                StyleColor color = number.Label.style.color;
                Color resolved = color.value;
                resolved.a = alpha;
                number.Label.style.color = resolved;
                _damageNumbers[i] = number;
            }
        }

        private void PositionDamageNumber(DamageNumberView number, float elapsedSeconds)
        {
            if (number.Label == null) return;
            Vector2 point = WorldToRuntimePanelPoint(number.WorldPosition);
            float width = number.Label.resolvedStyle.width;
            if (float.IsNaN(width) || width <= 1f)
                width = number.Label.name == "upgrade-feedback" ? 292f : 90f;
            number.Label.style.left = point.x - width * 0.5f;
            number.Label.style.top = point.y - 56f - elapsedSeconds * 48f;
        }

        private Vector2 WorldToRuntimePanelPoint(Vector3 worldPosition)
        {
            Vector2 panelSize = ResolveRuntimePanelSize();
            Camera camera = Camera.main;
            if (camera == null)
                camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (camera == null)
                return new Vector2(panelSize.x * 0.5f, panelSize.y * 0.45f);

            Vector3 screen = camera.WorldToScreenPoint(worldPosition);
            if (screen.z < 0f)
                return new Vector2(panelSize.x * 0.5f, panelSize.y * 0.5f);

            float screenWidth = Screen.width > 1 ? Screen.width : panelSize.x;
            float screenHeight = Screen.height > 1 ? Screen.height : panelSize.y;
            float x = screenWidth <= 0f ? panelSize.x * 0.5f : screen.x / screenWidth * panelSize.x;
            float y = screenHeight <= 0f ? panelSize.y * 0.5f : (screenHeight - screen.y) / screenHeight * panelSize.y;
            return new Vector2(
                Mathf.Clamp(x, 16f, Mathf.Max(16f, panelSize.x - 16f)),
                Mathf.Clamp(y, 16f, Mathf.Max(16f, panelSize.y - 16f)));
        }

        internal Vector2 ResolveRuntimePanelSize()
        {
            EnsureRuntimeUiDocument();
            float width = _runtimeUiRoot != null ? _runtimeUiRoot.resolvedStyle.width : 0f;
            float height = _runtimeUiRoot != null ? _runtimeUiRoot.resolvedStyle.height : 0f;
            if (float.IsNaN(width) || width <= 1f)
                width = Screen.width > 1 ? Screen.width : RuntimeUiFallbackWidth;
            if (float.IsNaN(height) || height <= 1f)
                height = Screen.height > 1 ? Screen.height : RuntimeUiFallbackHeight;
            return new Vector2(width, height);
        }

        internal void ClearDamageNumbers()
        {
            for (int i = 0; i < _damageNumbers.Count; i++)
                _damageNumbers[i].Label?.RemoveFromHierarchy();
            _damageNumbers.Clear();
        }

        private struct DamageNumberView
        {
            public DamageNumberView(Label label, Vector3 worldPosition, float elapsedSeconds)
            {
                Label = label;
                WorldPosition = worldPosition;
                ElapsedSeconds = elapsedSeconds;
            }

            public Label Label;
            public Vector3 WorldPosition;
            public float ElapsedSeconds;
        }
    }
}
