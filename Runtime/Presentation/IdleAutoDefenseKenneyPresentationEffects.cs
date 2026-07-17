using Deucarian.Common;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    internal sealed class IdleAutoDefenseTimedPresentationDestroy : MonoBehaviour
    {
        private float _remainingSeconds;

        public void Configure(float lifetimeSeconds)
        {
            _remainingSeconds = Mathf.Max(0.01f, lifetimeSeconds);
        }

        private void Update()
        {
            float delta = Time.deltaTime <= 0f ? 1f / 60f : Time.deltaTime;
            _remainingSeconds -= delta;
            if (_remainingSeconds <= 0f)
                UnityObjectUtility.DestroySafely(gameObject);
        }
    }

    internal sealed class KenneyBillboardVisual : MonoBehaviour
    {
        private bool _enabledBillboard;

        public void Configure(bool enabledBillboard)
        {
            _enabledBillboard = enabledBillboard;
        }

        private void LateUpdate()
        {
            if (!_enabledBillboard) return;
            Camera camera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (camera == null) return;
            Vector3 direction = transform.position - camera.transform.position;
            if (direction.sqrMagnitude <= 0.0001f) return;
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }

    internal sealed class KenneySpriteBurstVisual : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private Vector3 _startScale;
        private Color _startColor;
        private float _duration;
        private float _rise;
        private float _elapsed;

        public void Configure(SpriteRenderer renderer, float duration, float rise, Vector3 startScale)
        {
            _renderer = renderer;
            _duration = Mathf.Max(0.1f, duration);
            _rise = Mathf.Max(0f, rise);
            _startScale = startScale;
            _startColor = renderer != null ? renderer.color : Color.white;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime <= 0f ? 1f / 60f : Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / Mathf.Max(0.1f, _duration));
            float punch = 1f + Mathf.Sin(t * Mathf.PI) * 0.55f;
            transform.localScale = _startScale * punch;
            transform.position += Vector3.up * (_rise * Time.deltaTime / Mathf.Max(0.1f, _duration));
            if (_renderer != null)
            {
                Color color = _startColor;
                color.a *= Mathf.Clamp01(1f - t);
                _renderer.color = color;
            }

            if (t >= 1f)
                UnityObjectUtility.DestroySafely(gameObject);
        }
    }
}
