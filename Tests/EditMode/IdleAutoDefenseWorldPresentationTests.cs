using System;
using System.Collections.Generic;
using Deucarian.Attacks.Authoring;
using Deucarian.AutoDefense;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Deucarian.TemplateGameIdleAutoDefense.Tests
{
    public sealed class IdleAutoDefenseWorldPresentationTests
    {
        [Test]
        public void WorldInitializationCreatesAudioThenCallsUiBeforeCameraAndPrefabConstruction()
        {
            var world = CreateWorld();
            bool called = false;
            try
            {
                Assert.Throws<InvalidOperationException>(() => world.Initialize(Vector3.zero, Array.Empty<EnemyDefinitionAsset>(), () =>
                {
                    called = true;
                    Assert.That(world.Resources.Root != null, Is.True);
                    Assert.That(world.Resources.AudioSource.playOnAwake, Is.False);
                    Assert.That(world.Resources.AudioSource.spatialBlend, Is.Zero);
                    Assert.That(world.Resources.AudioSource.volume, Is.EqualTo(0.75f));
                    Assert.That(world.Resources.CameraShake, Is.Null);
                    Assert.That(world.Resources.EnemyPrefab, Is.Null);
                    Assert.That(world.Resources.ProjectilePrefab, Is.Null);
                    throw new InvalidOperationException("Stop after initialization callback.");
                }));
                Assert.That(called, Is.True);
            }
            finally { world.Dispose(true); }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void RuntimePrefabCleanupReleasesGeneratedTintAndPreservesBorrowedPrefabMaterial(bool destroySceneObjects)
        {
            var borrowedPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var borrowedMaterial = new Material(Shader.Find("Standard"));
            borrowedPrefab.GetComponent<Renderer>().sharedMaterial = borrowedMaterial;
            borrowedPrefab.SetActive(false);
            var world = CreateWorld();
            var root = new GameObject("Owned World");
            world.Resources.Root = root;
            GameObject generated = null;
            try
            {
                generated = world.Assets.CreateRuntimeVisualPrefab("Runtime Clone", borrowedPrefab,
                    PrimitiveType.Cube, Color.red, string.Empty, Vector3.zero, Vector3.one, false, 20,
                    "EnemyVisual", "enemy.world-test", string.Empty, string.Empty, "EnemyPrefab");
                Material tint = generated.GetComponent<Renderer>().sharedMaterial;
                Assert.That(tint, Is.Not.SameAs(borrowedMaterial));
                Assert.That(generated.activeSelf, Is.False);

                world.Dispose(destroySceneObjects);

                Assert.That(generated == null, Is.EqualTo(destroySceneObjects));
                Assert.That(tint == null, Is.True, "Never-active generated prefabs must release their tint materials.");
                Assert.That(borrowedPrefab != null, Is.True);
                Assert.That(borrowedMaterial != null, Is.True);
            }
            finally
            {
                world.Dispose(true);
                if (generated != null) Object.DestroyImmediate(generated);
                if (root != null) Object.DestroyImmediate(root);
                Object.DestroyImmediate(borrowedPrefab);
                Object.DestroyImmediate(borrowedMaterial);
            }
        }

        [Test]
        public void DestroyingCloneDoesNotReleaseMaterialsOwnedByItsPrefab()
        {
            var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Material material = IdleAutoDefenseMaterialLifetime.Own(prefab, new Material(Shader.Find("Standard")));
            prefab.GetComponent<Renderer>().sharedMaterial = material;
            GameObject clone = Object.Instantiate(prefab);
            try
            {
                Object.DestroyImmediate(clone);
                Assert.That(material != null, Is.True);
                Object.DestroyImmediate(prefab);
                Assert.That(material == null, Is.True);
            }
            finally
            {
                if (clone != null) Object.DestroyImmediate(clone);
                if (prefab != null) Object.DestroyImmediate(prefab);
                if (material != null) Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void KenneyModelReleasesTintMaterialsAndKeepsResourceMaterialsAlive()
        {
            GameObject source = Resources.Load<GameObject>(IdleAutoDefenseVisualAssets.Kenney3DResourceRoot + "weapon-ballista");
            Assert.That(source, Is.Not.Null, "The packaged starter weapon model must be available.");
            var borrowed = new List<Material>();
            foreach (Renderer renderer in source.GetComponentsInChildren<Renderer>(true)) borrowed.AddRange(renderer.sharedMaterials);
            var root = new GameObject("Kenney Runtime Model Test");
            try
            {
                IdleAutoDefenseKenneyModelPrefab model = root.AddComponent<IdleAutoDefenseKenneyModelPrefab>();
                model.ConfigureForTests("weapon-ballista", Color.red, Vector3.zero, Vector3.zero, Vector3.one);
                GameObject instance = model.EnsureModel();
                Assert.That(instance, Is.Not.Null);
                var generated = new List<Material>();
                foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true)) generated.AddRange(renderer.sharedMaterials);
                Assert.That(generated, Is.Not.Empty);

                IdleAutoDefenseMaterialLifetime.Release(root);
                Object.DestroyImmediate(root);

                foreach (Material material in generated) Assert.That(material == null, Is.True);
                foreach (Material material in borrowed) Assert.That(material != null, Is.True);
            }
            finally { if (root != null) Object.DestroyImmediate(root); }
        }

        [Test]
        public void BeamDestructionReleasesBothGeneratedLineMaterialsAndKeepsSourceMaterial()
        {
            var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var borrowed = new Material(Shader.Find("Standard"));
            prefab.GetComponent<Renderer>().sharedMaterial = borrowed;
            var instance = new GameObject("Runtime Beam");
            var world = CreateWorld();
            try
            {
                world.Beams.ConfigureBeamLineRenderer(instance, prefab, null);
                LineRenderer line = instance.GetComponent<LineRenderer>();
                Material configured = line.sharedMaterial;
                IdleAutoDefenseBeamPresentation.PrepareBeamRenderers(instance, Color.cyan);
                Material prepared = line.sharedMaterial;
                Assert.That(prepared, Is.Not.SameAs(configured));

                Object.DestroyImmediate(instance);

                Assert.That(configured == null, Is.True);
                Assert.That(prepared == null, Is.True);
                Assert.That(borrowed != null, Is.True);
            }
            finally
            {
                world.Dispose(true);
                if (instance != null) Object.DestroyImmediate(instance);
                Object.DestroyImmediate(prefab);
                Object.DestroyImmediate(borrowed);
            }
        }

        [Test]
        public void EnemyPrefabCacheOwnsRuntimeCloneAndBorrowsAuthoredSource()
        {
            var authoredPrefab = new GameObject("Authored Enemy");
            EnemyDefinitionAsset enemy = EnemyDefinitionAsset.CreateTransient("enemy.world-test", "Test Enemy",
                EnemyRole.Basic, 20, 1, 5, 4, BasicIdleAutoDefenseGame.DamageType.Value, prefab: authoredPrefab);
            var world = CreateWorld();
            try
            {
                GameObject first = world.Spawnables.GetEnemyPrefab(enemy);
                Assert.That(world.Spawnables.GetEnemyPrefab(enemy), Is.SameAs(first));
                Assert.That(first, Is.Not.SameAs(authoredPrefab));

                world.Dispose(true);

                Assert.That(first == null, Is.True);
                Assert.That(authoredPrefab != null, Is.True);
                Assert.That(enemy != null, Is.True);
            }
            finally
            {
                world.Dispose(true);
                Object.DestroyImmediate(enemy.Stats);
                Object.DestroyImmediate(enemy.Presentation);
                Object.DestroyImmediate(enemy);
                Object.DestroyImmediate(authoredPrefab);
            }
        }

        private static IdleAutoDefenseWorldPresentation CreateWorld()
        {
            var binding = new IdleAutoDefenseContentBinding(_ => { }, _ => { }, _ => { });
            var queries = new IdleAutoDefensePresentationQueries(_ => null, _ => null, _ => null, _ => 1d,
                (AutoDefenseRuntimeSnapshot snapshot, double range, out AutoDefenseEnemySnapshot enemy) => { enemy = default; return false; },
                (long id, out AutoDefenseEnemySnapshot enemy) => { enemy = default; return false; },
                () => false);
            return new IdleAutoDefenseWorldPresentation(binding, queries, () => false, () => 0f);
        }
    }
}
