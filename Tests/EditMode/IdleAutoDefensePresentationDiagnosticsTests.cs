using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Deucarian.TemplateGameIdleAutoDefense.Tests
{
    public sealed class IdleAutoDefensePresentationDiagnosticsTests
    {
        [Test]
        public void PresentationDiagnosticsRemainReadableAfterReleaseWithoutCreatingWorldOrContentOwners()
        {
            var host = new GameObject("idle-presentation-diagnostics-test");
            DiagnosticsProbe controller = host.AddComponent<DiagnosticsProbe>();
            try
            {
                Assert.That(controller.Kenney3DModelSpawnCount, Is.Zero);
                Assert.That(controller.AttackVfxSpawnCount, Is.Zero);
                AssertNoWorldOrContentOwner(controller);
                controller.Build();
                int models = controller.Kenney3DModelSpawnCount;
                int authored = controller.AuthoredVisibleInstanceStampCount;
                int fallback = controller.FallbackVisibleGameplaySpawnCount;
                Assert.That(models, Is.GreaterThan(0));

                controller.ReleaseRuntime();

                AssertNoWorldOrContentOwner(controller);
                Assert.That(controller.Kenney3DModelSpawnCount, Is.EqualTo(models));
                Assert.That(controller.AuthoredVisibleInstanceStampCount, Is.EqualTo(authored));
                Assert.That(controller.FallbackVisibleGameplaySpawnCount, Is.EqualTo(fallback));
                AssertNoWorldOrContentOwner(controller);
            }
            finally { Object.DestroyImmediate(host); }
        }

        private static void AssertNoWorldOrContentOwner(IdleAutoDefenseTemplateController controller)
        {
            foreach (string name in new[] { "_worldPresentation", "_contentBinding" })
            {
                FieldInfo field = typeof(IdleAutoDefenseTemplateController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(field, Is.Not.Null);
                Assert.That(field.GetValue(controller), Is.Null, "Diagnostic reads must not initialize " + name);
            }
        }

        private sealed class DiagnosticsProbe : IdleAutoDefenseTemplateController
        {
            protected override void Awake() { }
            internal void ReleaseRuntime() => base.OnDestroy();
        }
    }
}
