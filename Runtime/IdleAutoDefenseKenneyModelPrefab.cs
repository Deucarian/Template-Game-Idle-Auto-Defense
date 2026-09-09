using System;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    [DisallowMultipleComponent]
    public sealed class IdleAutoDefenseKenneyModelPrefab : MonoBehaviour
    {
        private const string DefaultResourceRoot = "Kenney/IdleAutoDefense/Models/TowerDefenseKit/FBX/";

        [SerializeField] private string _modelName = "tower-round-base";
        [SerializeField] private Color _tint = Color.white;
        [SerializeField] private Vector3 _localPosition = Vector3.zero;
        [SerializeField] private Vector3 _localEulerAngles = Vector3.zero;
        [SerializeField] private Vector3 _localScale = Vector3.one;
        [SerializeField] private bool _disableColliders = true;
        [SerializeField] private bool _tintRenderers = true;

        private GameObject _instance;

        public string ModelName => _modelName ?? string.Empty;
        public bool HasSpawnedModel => _instance != null;

        private void Awake()
        {
            EnsureModel();
        }

        private void OnEnable()
        {
            EnsureModel();
        }

        public GameObject EnsureModel()
        {
            if (_instance != null) return _instance;
            string modelName = (_modelName ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(modelName)) return null;
            string instanceName = "Kenney 3D " + modelName;
            Transform existing = transform.Find(instanceName);
            if (existing != null)
            {
                _instance = existing.gameObject;
                return _instance;
            }

            GameObject source = Resources.Load<GameObject>(DefaultResourceRoot + modelName);
            if (source == null) return null;

            _instance = Instantiate(source, transform, false);
            _instance.name = instanceName;
            _instance.transform.localPosition = _localPosition;
            _instance.transform.localRotation = Quaternion.Euler(_localEulerAngles);
            _instance.transform.localScale = _localScale == Vector3.zero ? Vector3.one : _localScale;
            if (_disableColliders)
                DisableColliders(_instance);
            if (_tintRenderers)
                TintRenderers(_instance, _tint);
            return _instance;
        }

        private static void DisableColliders(GameObject root)
        {
            if (root == null) return;
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;
        }

        private static void TintRenderers(GameObject root, Color tint)
        {
            if (root == null) return;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                Material[] sourceMaterials = renderer.sharedMaterials;
                if (sourceMaterials == null || sourceMaterials.Length == 0) continue;

                var materials = new Material[sourceMaterials.Length];
                for (int j = 0; j < sourceMaterials.Length; j++)
                {
                    Material source = sourceMaterials[j];
                    materials[j] = IdleAutoDefenseMaterialLifetime.Own(root, source != null ? new Material(source) : (shader != null ? new Material(shader) : null));
                    if (materials[j] != null && materials[j].HasProperty("_Color"))
                        materials[j].color = Color.Lerp(materials[j].color, tint, 0.28f);
                }

                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }

        public void ConfigureForTests(string modelName, Color tint, Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale)
        {
            _modelName = modelName ?? throw new ArgumentNullException(nameof(modelName));
            _tint = tint;
            _localPosition = localPosition;
            _localEulerAngles = localEulerAngles;
            _localScale = localScale;
        }
    }
}
