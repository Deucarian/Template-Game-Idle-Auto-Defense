using System.Collections.Generic;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    /// <summary>Local enemy model presentation for facing, hit flash, death pop, and readable idle motion.</summary>
    internal sealed class IdleAutoDefenseEnemyModelPresentation : MonoBehaviour
    {
        private readonly List<Material> _materials = new List<Material>();
        private Color _baseTint = Color.white;
        private Vector3 _lastPosition;
        private Vector3 _baseScale = Vector3.one;
        private long _enemyId;
        private float _hitFlashSeconds;
        private float _deathSeconds;
        private bool _bound;

        public bool IsBound => _bound;

        public void Configure(Color tint, Vector3 baseScale)
        {
            _baseTint = tint;
            _baseScale = baseScale == Vector3.zero ? Vector3.one : baseScale;
            CacheMaterials();
        }

        public void Bind(long enemyId, Vector3 worldPosition, Color tint)
        {
            _enemyId = enemyId;
            _bound = true;
            _lastPosition = worldPosition;
            _baseTint = tint;
            CacheMaterials();
        }

        public bool FaceMovement(Vector3 worldPosition, float deltaSeconds)
        {
            if (!_bound) return false;
            Vector3 delta = worldPosition - _lastPosition;
            _lastPosition = worldPosition;
            if (delta.sqrMagnitude <= 0.00001f) return false;

            delta.y = 0f;
            if (delta.sqrMagnitude <= 0.00001f) return false;
            Quaternion desired = Quaternion.LookRotation(delta.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, 520f * Mathf.Max(0.016f, deltaSeconds));
            return true;
        }

        public bool PlayHitFeedback()
        {
            _hitFlashSeconds = 0.16f;
            ApplyTint(Color.white);
            return true;
        }

        public bool PlayDeathFeedback()
        {
            _deathSeconds = 0.32f;
            transform.localScale = _baseScale * 1.28f;
            ApplyTint(new Color(1f, 0.22f, 0.08f));
            return true;
        }

        private void Update()
        {
            float delta = Time.deltaTime <= 0f ? 1f / 60f : Time.deltaTime;
            if (_hitFlashSeconds > 0f)
            {
                _hitFlashSeconds = Mathf.Max(0f, _hitFlashSeconds - delta);
                Color color = Color.Lerp(_baseTint, Color.white, _hitFlashSeconds / 0.16f);
                ApplyTint(color);
            }

            if (_deathSeconds > 0f)
            {
                _deathSeconds = Mathf.Max(0f, _deathSeconds - delta);
                float t = 1f - _deathSeconds / 0.32f;
                transform.localScale = _baseScale * (1.28f + Mathf.Sin(t * Mathf.PI) * 0.38f);
                if (_deathSeconds <= 0f)
                    ApplyTint(_baseTint);
            }
            else if (_bound)
            {
                float bob = Mathf.Sin(Time.time * 7.5f + _enemyId * 0.17f) * 0.035f;
                Vector3 scale = _baseScale;
                scale.y *= 1f + bob;
                transform.localScale = scale;
            }
        }

        private void CacheMaterials()
        {
            _materials.Clear();
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] materials = renderers[i].sharedMaterials;
                if (Application.isPlaying)
                {
                    var runtimeMaterials = new Material[materials.Length];
                    for (int j = 0; j < materials.Length; j++)
                        runtimeMaterials[j] = IdleAutoDefenseMaterialLifetime.Own(gameObject, materials[j] != null ? new Material(materials[j]) : null);
                    renderers[i].sharedMaterials = runtimeMaterials;
                    materials = runtimeMaterials;
                }

                for (int j = 0; j < materials.Length; j++)
                    if (materials[j] != null)
                        _materials.Add(materials[j]);
            }

            ApplyTint(_baseTint);
        }

        private void ApplyTint(Color tint)
        {
            for (int i = 0; i < _materials.Count; i++)
            {
                Material material = _materials[i];
                if (material != null && material.HasProperty("_Color"))
                    material.color = Color.Lerp(material.color, tint, 0.35f);
            }
        }
    }
}
