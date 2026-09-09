using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.TemplateGameIdleAutoDefense.Tests
{
    public sealed class IdleAutoDefenseRuntimeUiTests
    {
        [Test]
        public void PanelAndFeedbackAreOwnedOnceAndReleasedWithoutResolvingTheParentAgain()
        {
            var parent = new GameObject("UI owner test");
            int resolutions = 0;
            var ui = new IdleAutoDefenseRuntimeUi(() => { resolutions++; return parent.transform; });
            try
            {
                UIDocument document = ui.EnsureRuntimeUiDocument();
                PanelSettings panel = document.panelSettings;
                ThemeStyleSheet borrowedTheme = panel.themeStyleSheet;
                Assert.That(ui.EnsureRuntimeUiDocument(), Is.SameAs(document));
                Assert.That(parent.GetComponentsInChildren<UIDocument>().Length, Is.EqualTo(1));
                ui.EmitDamageNumber(Vector3.zero, 4.1d, Color.red, "-");
                ui.EmitFloatingStatusText(Vector3.zero, "Upgrade ready", Color.green);
                Assert.That(ui.RuntimeUiRoot.Q<Label>("damage-number").text, Is.EqualTo("-5"));
                Assert.That(ui.VisibleCount, Is.EqualTo(2));
                Assert.That(ui.DamageNumberSpawnCount, Is.EqualTo(2));
                Assert.That(ui.UpgradeFeedbackSpawnCount, Is.EqualTo(1));
                ui.Dispose();
                ui.Dispose();
                Assert.That(resolutions, Is.EqualTo(1));
                Assert.That(ui.VisibleCount, Is.Zero);
                Assert.That(ui.RuntimeUiDocumentReady, Is.False);
                Assert.That(panel == null, Is.True);
                Assert.That(document == null, Is.True);
                if (!ReferenceEquals(borrowedTheme, null)) Assert.That(borrowedTheme != null, Is.True);
                Assert.Throws<ObjectDisposedException>(() => ui.EnsureRuntimeUiDocument());
            }
            finally { ui.Dispose(); UnityEngine.Object.DestroyImmediate(parent); }
        }

        [Test]
        public void FeedbackExpiryAndRunResetRemoveLabelsAndResetOnlyRunCounters()
        {
            var parent = new GameObject("UI feedback test");
            using (var ui = new IdleAutoDefenseRuntimeUi(() => parent.transform))
            {
                try
                {
                    ui.EmitDamageNumber(Vector3.zero, 0d, Color.white, null);
                    ui.EmitFloatingStatusText(Vector3.zero, " ", Color.white);
                    Assert.That(ui.RuntimeUiDocumentReady, Is.False);
                    ui.EmitDamageNumber(Vector3.zero, 3d, Color.white, null);
                    VisualElement layer = ui.RuntimeUiRoot.Q("damage-number-layer");
                    ui.UpdateDamageNumbers(1.15f);
                    Assert.That(ui.VisibleCount, Is.Zero);
                    Assert.That(layer.childCount, Is.Zero);
                    Assert.That(ui.DamageNumberSpawnCount, Is.EqualTo(1));
                    ui.EmitFloatingStatusText(Vector3.zero, "Ready", Color.green);
                    ui.ResetFeedback();
                    Assert.That(layer.childCount, Is.Zero);
                    Assert.That(ui.DamageNumberSpawnCount, Is.Zero);
                    Assert.That(ui.UpgradeFeedbackSpawnCount, Is.Zero);
                    Assert.That(ui.RuntimeUiDocumentReady, Is.True);
                }
                finally { ui.Dispose(); UnityEngine.Object.DestroyImmediate(parent); }
            }
        }

        [Test]
        public void SceneTeardownStillReleasesTheOwnedTransientPanel()
        {
            var parent = new GameObject("UI scene teardown test");
            var ui = new IdleAutoDefenseRuntimeUi(() => parent.transform);
            try
            {
                UIDocument document = ui.EnsureRuntimeUiDocument();
                PanelSettings panel = document.panelSettings;
                ui.Release(false);
                Assert.That(document != null, Is.True);
                Assert.That(panel == null, Is.True);
                Assert.That(ui.RuntimeUiDocumentReady, Is.False);
            }
            finally { ui.Dispose(); UnityEngine.Object.DestroyImmediate(parent); }
        }
    }
}
