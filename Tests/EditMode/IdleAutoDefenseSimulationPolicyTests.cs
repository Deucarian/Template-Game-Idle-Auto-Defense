using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Deucarian.TemplateGameIdleAutoDefense.Tests
{
    public sealed class IdleAutoDefenseSimulationPolicyTests
    {
        [Test]
        public void FixedClockCarriesFractionalTimeAcrossFrames()
        {
            var ticks = new List<float>();
            var clock = new IdleAutoDefenseFrameClock(delta => { ticks.Add(delta); return true; });
            clock.Advance(0.12f, true, 0.05f);
            Assert.That(ticks.Count, Is.EqualTo(2));
            clock.Advance(0.03f, true, 0.05f);
            Assert.That(ticks, Is.EqualTo(new[] { 0.05f, 0.05f, 0.05f }));
        }

        [Test]
        public void ClockStopsCatchupWhenRunEndsAndRetainsUnspentTime()
        {
            int ticks = 0;
            bool running = false;
            var clock = new IdleAutoDefenseFrameClock(_ => { ticks++; return running; });
            clock.Advance(0.15f, true, 0.05f);
            Assert.That(ticks, Is.EqualTo(1));
            running = true;
            clock.Advance(0.001f, true, 0.05f);
            Assert.That(ticks, Is.EqualTo(3));
        }

        [Test]
        public void ClockResetDuringTickDiscardsRemainingCatchup()
        {
            int ticks = 0;
            IdleAutoDefenseFrameClock clock = null;
            clock = new IdleAutoDefenseFrameClock(_ => { ticks++; clock.Reset(); return true; });
            clock.Advance(1f, true, 0.05f);
            Assert.That(ticks, Is.EqualTo(1));
            clock.Advance(0.01f, true, 0.05f);
            Assert.That(ticks, Is.EqualTo(1));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        public void LegacyClockUsesOriginalFallbackDelta(float delta)
        {
            var ticks = new List<float>();
            var clock = new IdleAutoDefenseFrameClock(seconds => { ticks.Add(seconds); return false; });
            clock.Advance(delta, false, 0f);
            Assert.That(ticks.Count, Is.EqualTo(1));
            Assert.That(ticks[0], Is.EqualTo(1f / 60f));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void NonFiniteFrameIsRejectedWithoutPoisoningResidualTime(float delta)
        {
            int ticks = 0;
            var clock = new IdleAutoDefenseFrameClock(_ => { ticks++; return true; });
            clock.Advance(0.025f, true, 0.05f);
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.Advance(delta, true, 0.05f));
            clock.Advance(0.025f, true, 0.05f);
            Assert.That(ticks, Is.EqualTo(1));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidFixedDurationCannotStartAnUnboundedLoop(float secondsPerTick)
        {
            int ticks = 0;
            var clock = new IdleAutoDefenseFrameClock(_ => { ticks++; return true; });
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.Advance(0.1f, true, secondsPerTick));
            Assert.That(ticks, Is.Zero);
        }

        [Test]
        public void HugeFiniteFrameCannotStallFixedTickSubtraction()
        {
            int ticks = 0;
            var clock = new IdleAutoDefenseFrameClock(_ => { ticks++; return true; });
            clock.Advance(0.025f, true, 0.05f);
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.Advance(float.MaxValue, true, 0.05f));
            clock.Advance(0.025f, true, 0.05f);
            Assert.That(ticks, Is.EqualTo(1));
        }

        [Test]
        public void TinyFixedDurationDoesNotSpendAnAbsoluteToleranceAsTime()
        {
            int ticks = 0;
            var clock = new IdleAutoDefenseFrameClock(_ => { ticks++; return true; });
            clock.Advance(0.0000002f, true, 0.0000001f);
            Assert.That(ticks, Is.EqualTo(2));
        }

        [Test]
        public void CameraShakeRestoresCapturedPoseWhenFinishedAndDisposed()
        {
            var host = new GameObject("shake-camera");
            var camera = host.AddComponent<Camera>();
            var pose = new Vector3(2, 3, 4);
            camera.transform.localPosition = pose;
            var shake = new IdleAutoDefenseCameraShake(() => camera);
            try
            {
                shake.Trigger(0.2f, 0.1f);
                shake.Update(0.05f, 1f);
                Assert.That(camera.transform.localPosition, Is.Not.EqualTo(pose));
                shake.Update(0.3f, 1.3f);
                Assert.That(camera.transform.localPosition, Is.EqualTo(pose));
                shake.Trigger(0.2f, 0.1f);
                shake.Update(0.05f, 2f);
                shake.Dispose();
                shake.Dispose();
                Assert.That(camera.transform.localPosition, Is.EqualTo(pose));
            }
            finally { shake.Dispose(); Object.DestroyImmediate(host); }
        }

        [Test]
        public void CameraReplacementCapturesItsOwnPose()
        {
            var firstHost = new GameObject("first-shake-camera");
            var nextHost = new GameObject("next-shake-camera");
            Camera current = firstHost.AddComponent<Camera>();
            var next = nextHost.AddComponent<Camera>();
            next.transform.localPosition = new Vector3(10, 20, 30);
            var shake = new IdleAutoDefenseCameraShake(() => current);
            try
            {
                shake.Trigger(0.2f, 0.1f);
                shake.Update(0.05f, 1f);
                Object.DestroyImmediate(firstHost);
                current = next;
                shake.Update(0.02f, 1.1f);
                shake.Dispose();
                Assert.That(next.transform.localPosition, Is.EqualTo(new Vector3(10, 20, 30)));
            }
            finally
            {
                shake.Dispose();
                if (firstHost != null) Object.DestroyImmediate(firstHost);
                Object.DestroyImmediate(nextHost);
            }
        }

        [Test]
        public void BeamKeepsLastImpactWhenTargetDisappearsAndClearReleasesInstance()
        {
            var instance = new GameObject("tracked-beam");
            Vector3? target = new Vector3(3, 4, 5);
            var endpoints = new List<Vector3>();
            var beams = new IdleAutoDefenseBeamVisuals(
                _ => target, _ => Vector3.zero,
                (beam, prefab, from, to) => { endpoints.Add(to); return true; });
            try
            {
                beams.Add(instance, null, null, 1, Vector3.one, 1f);
                Assert.That(beams.Update(0.02f), Is.Zero);
                target = null;
                Assert.That(beams.Update(0.02f), Is.Zero);
                Assert.That(endpoints, Is.EqualTo(new[] { new Vector3(3, 4, 5), new Vector3(3, 4, 5) }));
                beams.Clear();
                beams.Clear();
                Assert.That(instance == null, Is.True);
            }
            finally { beams.Clear(); if (instance != null) Object.DestroyImmediate(instance); }
        }

        [Test]
        public void InvalidBeamEndpointIsReportedOnceAndRemoved()
        {
            var instance = new GameObject("invalid-beam");
            var beams = new IdleAutoDefenseBeamVisuals(_ => null, _ => Vector3.zero, (a, b, c, d) => false);
            try
            {
                beams.Add(instance, null, null, 0, Vector3.one, 1f);
                Assert.That(beams.Update(0.02f), Is.EqualTo(1));
                Assert.That(beams.Update(0.02f), Is.Zero);
                Assert.That(instance == null, Is.True);
            }
            finally { beams.Clear(); if (instance != null) Object.DestroyImmediate(instance); }
        }

        [Test]
        public void ExpiredBeamIsReleasedBeforeResolvingEndpoints()
        {
            var instance = new GameObject("expired-beam");
            int alignments = 0;
            var beams = new IdleAutoDefenseBeamVisuals(_ => null, _ => Vector3.zero,
                (a, b, c, d) => { alignments++; return true; });
            try
            {
                beams.Add(instance, null, null, 0, Vector3.one, 0.05f);
                Assert.That(beams.Update(0.1f), Is.Zero);
                Assert.That(alignments, Is.Zero);
                Assert.That(instance == null, Is.True);
            }
            finally { beams.Clear(); if (instance != null) Object.DestroyImmediate(instance); }
        }
    }
}
