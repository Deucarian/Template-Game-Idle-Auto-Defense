using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    [DisallowMultipleComponent]
    public sealed class AuthoredContentInstance : MonoBehaviour
    {
        [SerializeField] private string _definitionType = string.Empty;
        [SerializeField] private string _contentId = string.Empty;
        [SerializeField] private string _sourceAssetGuid = string.Empty;
        [SerializeField] private string _sourceAssetPath = string.Empty;
        [SerializeField] private string _prefabName = string.Empty;
        [SerializeField] private string _ownerWeaponId = string.Empty;
        [SerializeField] private string _ownerAttackId = string.Empty;
        [SerializeField] private string _effectRole = string.Empty;

        public string DefinitionType => _definitionType ?? string.Empty;
        public string ContentId => _contentId ?? string.Empty;
        public string SourceAssetGuid => _sourceAssetGuid ?? string.Empty;
        public string SourceAssetPath => _sourceAssetPath ?? string.Empty;
        public string PrefabName => _prefabName ?? string.Empty;
        public string OwnerWeaponId => _ownerWeaponId ?? string.Empty;
        public string OwnerAttackId => _ownerAttackId ?? string.Empty;
        public string EffectRole => _effectRole ?? string.Empty;

        public static AuthoredContentInstance Stamp(
            GameObject instance,
            string definitionType,
            string contentId,
            string sourceAssetGuid,
            string sourceAssetPath,
            string prefabName,
            string ownerWeaponId,
            string ownerAttackId,
            string effectRole)
        {
            if (instance == null) return null;
            AuthoredContentInstance stamp = instance.GetComponent<AuthoredContentInstance>();
            if (stamp == null)
                stamp = instance.AddComponent<AuthoredContentInstance>();

            stamp._definitionType = definitionType ?? string.Empty;
            stamp._contentId = contentId ?? string.Empty;
            stamp._sourceAssetGuid = sourceAssetGuid ?? string.Empty;
            stamp._sourceAssetPath = sourceAssetPath ?? string.Empty;
            stamp._prefabName = prefabName ?? string.Empty;
            stamp._ownerWeaponId = ownerWeaponId ?? string.Empty;
            stamp._ownerAttackId = ownerAttackId ?? string.Empty;
            stamp._effectRole = effectRole ?? string.Empty;
            return stamp;
        }
    }
}
