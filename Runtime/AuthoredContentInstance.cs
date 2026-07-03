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
        [SerializeField] private string _contentType = string.Empty;
        [SerializeField] private string _ownerContentId = string.Empty;
        [SerializeField] private string _attackContentId = string.Empty;
        [SerializeField] private string _vfxContentId = string.Empty;
        [SerializeField] private string _originSocketId = string.Empty;
        [SerializeField] private string _targetSocketId = string.Empty;
        [SerializeField] private string _spawnedBySystem = string.Empty;
        [SerializeField] private bool _fallbackUsed;
        [SerializeField] private bool _allowed = true;

        public string DefinitionType => _definitionType ?? string.Empty;
        public string ContentId => _contentId ?? string.Empty;
        public string SourceAssetGuid => _sourceAssetGuid ?? string.Empty;
        public string SourceAssetPath => _sourceAssetPath ?? string.Empty;
        public string PrefabName => _prefabName ?? string.Empty;
        public string OwnerWeaponId => _ownerWeaponId ?? string.Empty;
        public string OwnerAttackId => _ownerAttackId ?? string.Empty;
        public string EffectRole => _effectRole ?? string.Empty;
        public string ContentType => string.IsNullOrWhiteSpace(_contentType) ? DefinitionType : _contentType;
        public string OwnerContentId => string.IsNullOrWhiteSpace(_ownerContentId) ? OwnerWeaponId : _ownerContentId;
        public string AttackContentId => string.IsNullOrWhiteSpace(_attackContentId) ? OwnerAttackId : _attackContentId;
        public string VfxContentId => _vfxContentId ?? string.Empty;
        public string OriginSocketId => _originSocketId ?? string.Empty;
        public string TargetSocketId => _targetSocketId ?? string.Empty;
        public string SpawnedBySystem => _spawnedBySystem ?? string.Empty;
        public bool FallbackUsed => _fallbackUsed;
        public bool Allowed => _allowed;
        public bool CameFromGameContentAuthoring => !_fallbackUsed && !string.IsNullOrWhiteSpace(ContentId);

        public static AuthoredContentInstance Stamp(
            GameObject instance,
            string definitionType,
            string contentId,
            string sourceAssetGuid,
            string sourceAssetPath,
            string prefabName,
            string ownerWeaponId,
            string ownerAttackId,
            string effectRole,
            string contentType = "",
            string ownerContentId = "",
            string attackContentId = "",
            string vfxContentId = "",
            string originSocketId = "",
            string targetSocketId = "",
            string spawnedBySystem = "IdleAutoDefenseTemplateController",
            bool fallbackUsed = false,
            bool allowed = true)
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
            stamp._contentType = string.IsNullOrWhiteSpace(contentType) ? definitionType ?? string.Empty : contentType;
            stamp._ownerContentId = string.IsNullOrWhiteSpace(ownerContentId) ? ownerWeaponId ?? string.Empty : ownerContentId;
            stamp._attackContentId = string.IsNullOrWhiteSpace(attackContentId) ? ownerAttackId ?? string.Empty : attackContentId;
            stamp._vfxContentId = vfxContentId ?? string.Empty;
            stamp._originSocketId = originSocketId ?? string.Empty;
            stamp._targetSocketId = targetSocketId ?? string.Empty;
            stamp._spawnedBySystem = spawnedBySystem ?? string.Empty;
            stamp._fallbackUsed = fallbackUsed;
            stamp._allowed = allowed;
            return stamp;
        }
    }
}
