using System;
using System.Collections.Generic;
using System.Linq;
using Deucarian.Encounters;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Deucarian.TemplateGameIdleAutoDefense.Tests
{
    public sealed class IdleAutoDefenseConstructionLifetimeTests
    {
        [Test]
        public void FailedEncounterConstructionReleasesWorldAndAllowsOneCleanRetry()
        {
            var host = new GameObject("idle-construction-lifetime-test");
            host.SetActive(false);
            IdleAutoDefenseTemplateController controller = host.AddComponent<IdleAutoDefenseTemplateController>();
            var before = new HashSet<int>(SceneObjects().Select(item => item.GetInstanceID()));
            var panelsBefore = new HashSet<int>(RuntimePanels().Select(item => item.GetInstanceID()));
            try
            {
                var invalid = new EncounterDefinition(new EncounterId("test.invalid-construction"), null,
                    new[] { new WaveDefinition(new WaveId("test.wave"), 0, Array.Empty<SpawnGroupDefinition>()) }, null);
                // Deliberately corrupt the exposed array after definition validation so the
                // real EncounterRuntime fails after world/UI allocation, without a test seam.
                ((WaveDefinition[])invalid.Waves)[0] = null;

                Assert.Throws<NullReferenceException>(() => controller.Build(invalid));
                Assert.That(controller.RuntimeUiDocumentReady, Is.False);
                Assert.That(NewSceneObjects(before), Is.Empty, "Failed construction must release every created scene object.");
                Assert.That(RuntimePanels().Where(item => !panelsBefore.Contains(item.GetInstanceID())), Is.Empty);

                controller.Build();

                Assert.That(controller.EncounterRunning, Is.True);
                Assert.That(controller.RuntimeUiDocumentReady, Is.True);
                GameObject[] created = NewSceneObjects(before);
                Assert.That(created.Count(item => item.name == "Basic Idle Auto Defense Runtime"), Is.EqualTo(1));
                Assert.That(created.Count(item => item.name == "Basic Idle Auto Defense UI"), Is.EqualTo(1));
                Assert.That(RuntimePanels().Count(item => !panelsBefore.Contains(item.GetInstanceID())), Is.EqualTo(1));
            }
            finally
            {
                // A never-active host need not receive OnDestroy; activate it only after
                // the explicit construction assertions so its normal teardown runs.
                host.SetActive(true);
                Object.DestroyImmediate(host);
                foreach (GameObject item in NewSceneObjects(before))
                {
                    IdleAutoDefenseMaterialLifetime.Release(item);
                    Object.DestroyImmediate(item);
                }
                foreach (PanelSettings panel in RuntimePanels())
                    if (!panelsBefore.Contains(panel.GetInstanceID())) Object.DestroyImmediate(panel);
            }
        }

        private static GameObject[] SceneObjects() => Resources.FindObjectsOfTypeAll<GameObject>()
            .Where(item => item != null && item.scene.IsValid()).ToArray();

        private static GameObject[] NewSceneObjects(HashSet<int> before) => SceneObjects()
            .Where(item => !before.Contains(item.GetInstanceID())).ToArray();

        private static PanelSettings[] RuntimePanels() => Resources.FindObjectsOfTypeAll<PanelSettings>()
            .Where(item => item != null && item.name == "Basic Idle Auto Defense Runtime Panel Settings").ToArray();
    }
}
