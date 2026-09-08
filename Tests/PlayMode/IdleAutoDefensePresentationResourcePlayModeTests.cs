using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Deucarian.TemplateGameIdleAutoDefense.PlayModeTests
{
    public sealed class IdleAutoDefensePresentationResourcePlayModeTests
    {
        [UnityTest]
        public IEnumerator EnemyMaterialRecacheReleasesEveryGeneratedCopyAndKeepsBorrowedSource()
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var borrowed = new Material(Shader.Find("Standard"));
            Renderer renderer = root.GetComponent<Renderer>();
            renderer.sharedMaterial = borrowed;
            try
            {
                IdleAutoDefenseEnemyModelPresentation presentation = root.AddComponent<IdleAutoDefenseEnemyModelPresentation>();
                presentation.Configure(Color.red, Vector3.one);
                Material configured = renderer.sharedMaterial;
                presentation.Bind(1, Vector3.zero, Color.blue);
                Material bound = renderer.sharedMaterial;
                Assert.That(configured, Is.Not.SameAs(borrowed));
                Assert.That(bound, Is.Not.SameAs(configured));

                Object.Destroy(root);
                yield return null;
                yield return null;

                Assert.That(configured == null, Is.True, "Recaching must not orphan the previous runtime material.");
                Assert.That(bound == null, Is.True);
                Assert.That(borrowed != null, Is.True);
            }
            finally
            {
                if (root != null) Object.Destroy(root);
                Object.Destroy(borrowed);
            }
        }
    }
}
