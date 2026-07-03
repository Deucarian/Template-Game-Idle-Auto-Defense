using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    /// <summary>Local weapon/turret presentation binding used by the idle-defense sample before any package extraction.</summary>
    internal sealed class IdleAutoDefenseWeaponVisualBinding : MonoBehaviour
    {
        private Transform _yawPivot;
        private Transform _recoilPivot;
        private Transform _muzzle;
        private Vector3 _recoilRestLocalPosition;
        private Color _flashColor;
        private float _turnSpeedDegrees = 360f;
        private float _recoilSeconds;

        public Transform Muzzle => _muzzle;

        public void Configure(Transform yawPivot, Transform recoilPivot, Transform muzzle, float turnSpeedDegrees, Color flashColor)
        {
            _yawPivot = yawPivot;
            _recoilPivot = recoilPivot;
            _muzzle = muzzle;
            _turnSpeedDegrees = Mathf.Max(30f, turnSpeedDegrees);
            _flashColor = flashColor;
            _recoilRestLocalPosition = _recoilPivot != null ? _recoilPivot.localPosition : Vector3.zero;
        }

        public bool AimAt(Vector3 worldTarget, bool snap, float deltaSeconds)
        {
            if (_yawPivot == null) return false;
            Vector3 flatDirection = worldTarget - _yawPivot.position;
            flatDirection.y = 0f;
            if (flatDirection.sqrMagnitude <= 0.0001f) return false;

            Quaternion desired = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);
            _yawPivot.rotation = snap
                ? desired
                : Quaternion.RotateTowards(_yawPivot.rotation, desired, _turnSpeedDegrees * Mathf.Max(0.016f, deltaSeconds));
            return true;
        }

        public bool Rest(float deltaSeconds)
        {
            if (_yawPivot == null) return false;
            Quaternion desired = Quaternion.identity;
            _yawPivot.localRotation = Quaternion.RotateTowards(_yawPivot.localRotation, desired, _turnSpeedDegrees * 0.32f * Mathf.Max(0.016f, deltaSeconds));
            return true;
        }

        public bool TriggerRecoil()
        {
            if (_recoilPivot == null) return false;
            _recoilSeconds = 0.18f;
            _recoilPivot.localPosition = _recoilRestLocalPosition + Vector3.back * 0.18f;
            return true;
        }

        public bool EmitMuzzleFlash(Transform parent, Color color)
        {
            if (_muzzle == null) return false;

            GameObject flash = new GameObject("Idle Auto Defense Muzzle Flash");
            flash.transform.SetParent(parent, true);
            flash.transform.position = _muzzle.position;
            flash.transform.rotation = _muzzle.rotation;
            ParticleSystem particles = flash.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = particles.main;
            main.playOnAwake = false;
            main.duration = 0.12f;
            main.startLifetime = 0.1f;
            main.startSpeed = 2.2f;
            main.startSize = 0.26f;
            Color flashColor = color == default(Color) ? _flashColor : color;
            main.startColor = flashColor;
            main.loop = false;
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 9) });
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 18f;
            shape.radius = 0.06f;
            particles.Play(true);
            flash.AddComponent<IdleAutoDefenseTimedPresentationDestroy>().Configure(0.35f);
            return true;
        }

        private void Update()
        {
            if (_recoilPivot == null || _recoilSeconds <= 0f) return;
            float delta = Time.deltaTime <= 0f ? 1f / 60f : Time.deltaTime;
            _recoilSeconds = Mathf.Max(0f, _recoilSeconds - delta);
            _recoilPivot.localPosition = Vector3.Lerp(_recoilPivot.localPosition, _recoilRestLocalPosition, 1f - Mathf.Pow(0.001f, delta));
        }
    }
}
