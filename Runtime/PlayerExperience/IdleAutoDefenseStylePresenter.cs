using System;
using System.Collections.Generic;
using System.Globalization;
using Deucarian.IdleProgression;
using Deucarian.Progression;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    internal sealed class IdleAutoDefenseStylePresenter
    {
        private readonly IdleAutoDefensePlayerView _view;
        internal IdleAutoDefenseStylePresenter(IdleAutoDefensePlayerView view) => _view = view;

        internal VisualElement Overlay(string name, float alpha)
        {
            var overlay = new VisualElement { name = name };
            FillAbsolute(overlay);
            overlay.style.justifyContent = Justify.Center;
            overlay.style.alignItems = Align.Center;
            overlay.style.paddingLeft = 18;
            overlay.style.paddingRight = 18;
            overlay.style.paddingTop = 14;
            overlay.style.paddingBottom = 14;
            overlay.style.backgroundColor = WithAlpha(_view.App._activeTheme == null ? Color.black : _view.App._activeTheme.Background, alpha);
            overlay.pickingMode = PickingMode.Position;
            return overlay;
        }

        internal VisualElement ModalPanel(VisualElement parent, float maxWidth, float maxHeight)
        {
            var panel = new VisualElement { name = "modal-panel" };
            panel.style.width = Length.Percent(92);
            panel.style.maxWidth = maxWidth;
            panel.style.maxHeight = maxHeight;
            panel.style.flexGrow = 1;
            panel.style.paddingLeft = 22;
            panel.style.paddingRight = 22;
            panel.style.paddingTop = 20;
            panel.style.paddingBottom = 20;
            panel.style.backgroundColor = _view.App._activeTheme == null ? new Color(0.04f, 0.06f, 0.08f, 0.98f) : _view.App._activeTheme.Panel;
            SetBorder(panel, 2, _view.App._activeTheme == null ? Color.cyan : _view.App._activeTheme.Accent, 7);
            parent.Add(panel);
            return panel;
        }

        internal VisualElement HudBlock(float width)
        {
            var block = new VisualElement { name = "hud-block" };
            block.style.width = width;
            block.style.minHeight = 58;
            block.style.paddingLeft = 9;
            block.style.paddingRight = 9;
            block.style.paddingTop = 6;
            block.style.paddingBottom = 6;
            block.style.backgroundColor = _view.App._activeTheme == null ? new Color(0.02f, 0.03f, 0.04f, 0.82f) : WithAlpha(_view.App._activeTheme.Panel, 0.9f);
            SetBorder(block, 1, _view.App._activeTheme == null ? Color.cyan : _view.App._activeTheme.Accent, 5);
            return block;
        }

        internal VisualElement ButtonRow(VisualElement parent)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexWrap = Wrap.Wrap;
            row.style.justifyContent = Justify.Center;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 8;
            row.style.marginBottom = 8;
            parent.Add(row);
            return row;
        }

        internal void AddSectionHeading(VisualElement parent, string text)
        {
            Label label = AddLabel(parent, text, 21, FontStyle.Bold);
            label.style.marginTop = 12;
            label.style.marginBottom = 8;
            label.style.color = _view.App._activeTheme.Accent;
        }

        internal Label AddLabel(VisualElement parent, string text, int fontSize, FontStyle style = FontStyle.Normal)
        {
            var label = new Label(text);
            IdleAutoDefenseTemplateController.ApplyRuntimeUiFont(label);
            label.style.fontSize = fontSize;
            label.style.unityFontStyleAndWeight = style;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.color = _view.App._activeTheme == null ? Color.white : _view.App._activeTheme.PrimaryText;
            label.style.minHeight = fontSize + 4;
            parent.Add(label);
            return label;
        }

        internal Button AddButton(VisualElement parent, string text, Action clicked, float minWidth, float height)
        {
            var button = new Button(clicked) { text = text };
            IdleAutoDefenseTemplateController.ApplyRuntimeUiFont(button);
            button.style.minWidth = minWidth;
            button.style.height = Mathf.Max(_view.App._effectiveExperience.UiSettings.MinimumTouchTarget, height);
            button.style.minHeight = Mathf.Max(_view.App._effectiveExperience.UiSettings.MinimumTouchTarget, height);
            button.style.marginLeft = 4;
            button.style.marginRight = 4;
            button.style.marginTop = 4;
            button.style.marginBottom = 4;
            button.style.fontSize = 14;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.backgroundColor = _view.App._activeTheme == null ? new Color(0.08f, 0.12f, 0.16f, 1f) : _view.App._activeTheme.PanelRaised;
            button.style.color = _view.App._activeTheme == null ? Color.white : _view.App._activeTheme.PrimaryText;
            SetBorder(button, 1, _view.App._activeTheme == null ? Color.cyan : _view.App._activeTheme.Accent, 6);
            button.RegisterCallback<PointerEnterEvent>(_ => button.style.borderTopWidth = 3);
            button.RegisterCallback<PointerLeaveEvent>(_ => button.style.borderTopWidth = 1);
            parent.Add(button);
            return button;
        }

        internal VisualElement BarTrack(float width, float height)
        {
            var track = new VisualElement();
            track.style.width = width;
            track.style.height = height;
            track.style.backgroundColor = new Color(0f, 0f, 0f, 0.65f);
            track.style.overflow = Overflow.Hidden;
            SetBorder(track, 1, new Color(1f, 1f, 1f, 0.55f), 3);
            return track;
        }

        internal VisualElement BarFill(VisualElement track)
        {
            var fill = new VisualElement();
            fill.style.position = Position.Absolute;
            fill.style.left = 0;
            fill.style.top = 0;
            fill.style.bottom = 0;
            fill.style.width = Length.Percent(100);
            track.Add(fill);
            return fill;
        }

        internal void FillAbsolute(VisualElement element)
        {
            element.style.position = Position.Absolute;
            element.style.left = 0;
            element.style.right = 0;
            element.style.top = 0;
            element.style.bottom = 0;
            element.style.width = Length.Percent(100);
            element.style.height = Length.Percent(100);
        }

        internal void SetBorder(VisualElement element, float width, Color color, float radius)
        {
            element.style.borderTopWidth = width;
            element.style.borderBottomWidth = width;
            element.style.borderLeftWidth = width;
            element.style.borderRightWidth = width;
            element.style.borderTopColor = color;
            element.style.borderBottomColor = color;
            element.style.borderLeftColor = color;
            element.style.borderRightColor = color;
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
        }

        internal void SetPercentWidth(VisualElement element, float normalized)
        {
            if (element != null) element.style.width = Length.Percent(Mathf.Clamp01(normalized) * 100f);
        }

        internal Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        internal string FormatDuration(double seconds)
        {
            int total = Math.Max(0, (int)Math.Ceiling(seconds));
            int minutes = total / 60;
            int remainder = total % 60;
            return minutes.ToString("00", CultureInfo.InvariantCulture) + ":" + remainder.ToString("00", CultureInfo.InvariantCulture);
        }

        internal string Nicify(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var result = new System.Text.StringBuilder(value.Length + 8);
            for (int i = 0; i < value.Length; i++)
            {
                if (i > 0 && char.IsUpper(value[i]) && !char.IsUpper(value[i - 1])) result.Append(' ');
                result.Append(value[i]);
            }
            return result.ToString();
        }
    }
}
