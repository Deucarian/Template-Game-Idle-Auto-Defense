using System;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    /// <summary>Owns a camera's temporary shake offset and restores its captured local pose.</summary>
    internal sealed class IdleAutoDefenseCameraShake : IDisposable
    {
        private readonly Func<Camera> _resolveCamera;
        private Camera _camera;
        private Vector3 _baseLocalPosition;
        private bool _baseCaptured;
        private float _secondsRemaining;
        private float _duration;
        private float _magnitude;

        internal IdleAutoDefenseCameraShake(Func<Camera> resolveCamera)
        {
            _resolveCamera = resolveCamera ?? throw new ArgumentNullException(nameof(resolveCamera));
        }

        internal void Bind(Camera camera)
        {
            _camera = camera;
            _baseCaptured = camera != null;
            if (_baseCaptured) _baseLocalPosition = camera.transform.localPosition;
        }

        internal void Trigger(float durationSeconds, float magnitude)
        {
            if (durationSeconds <= 0f || magnitude <= 0f) return;
            _secondsRemaining = Mathf.Max(_secondsRemaining, durationSeconds);
            _duration = Mathf.Max(_duration, durationSeconds);
            _magnitude = Mathf.Max(_magnitude, magnitude);
        }

        internal void Update(float deltaSeconds, float survivalSeconds)
        {
            if (_secondsRemaining <= 0f)
            {
                Restore();
                return;
            }

            if (_camera == null) Bind(_resolveCamera());
            if (_camera == null) return;
            float safeDelta = Mathf.Max(0.016f, deltaSeconds);
            _secondsRemaining = Mathf.Max(0f, _secondsRemaining - safeDelta);
            float normalized = Mathf.Clamp01(_secondsRemaining / Mathf.Max(0.001f, _duration));
            float magnitude = _magnitude * normalized * normalized;
            float phase = survivalSeconds * 58.7f + _secondsRemaining * 19.3f;
            Vector3 offset = new Vector3(
                Mathf.Sin(phase) * magnitude,
                Mathf.Cos(phase * 0.7f) * magnitude * 0.35f,
                Mathf.Sin(phase * 1.37f) * magnitude * 0.45f);
            _camera.transform.localPosition = _baseLocalPosition + offset;
            if (_secondsRemaining <= 0f)
            {
                _magnitude = 0f;
                _duration = 0f;
                Restore();
            }
        }

        internal void ResetEnvelope()
        {
            _secondsRemaining = 0f;
            _duration = 0f;
            _magnitude = 0f;
        }

        private void Restore()
        {
            if (_camera != null && _baseCaptured)
                _camera.transform.localPosition = _baseLocalPosition;
        }

        public void Dispose()
        {
            Restore();
            _camera = null;
            _baseCaptured = false;
            ResetEnvelope();
        }
    }
}
