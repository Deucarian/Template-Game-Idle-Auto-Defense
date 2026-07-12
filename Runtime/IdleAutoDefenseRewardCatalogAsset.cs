using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    [CreateAssetMenu(menuName = "Deucarian/Idle Auto Defense/Reward Catalog", fileName = "IdleAutoDefenseRewardCatalog")]
    public sealed class IdleAutoDefenseRewardCatalogAsset : ScriptableObject
    {
        [SerializeField] private string _id = "reward-catalog.idle-auto-defense.playable";
        [SerializeField] private string _displayName = "Basic Idle Auto Defense Reward Catalog";
        [SerializeField] private float _firstDraftSeconds = 30f;
        [SerializeField] private IdleAutoDefenseRewardDraftSettings _settings = IdleAutoDefenseRewardDraftSettings.CreateDefault();
        [SerializeField] private IdleAutoDefenseRewardDraftCatalog _catalog = IdleAutoDefenseRewardDraftCatalog.CreateDefault();

        public string Id => _id ?? string.Empty;
        public string DisplayName => _displayName ?? string.Empty;
        public float FirstDraftSeconds => Mathf.Max(0f, _firstDraftSeconds);
        public IdleAutoDefenseRewardDraftSettings Settings => _settings;
        public IdleAutoDefenseRewardDraftCatalog Catalog => _catalog;

        public void Configure(
            string id,
            string displayName,
            float firstDraftSeconds,
            IdleAutoDefenseRewardDraftSettings settings,
            IdleAutoDefenseRewardDraftCatalog catalog)
        {
            _id = id ?? string.Empty;
            _displayName = displayName ?? string.Empty;
            _firstDraftSeconds = firstDraftSeconds;
            _settings = settings == null ? null : settings.Clone();
            _catalog = catalog == null ? null : catalog.Clone();
        }

        public static IdleAutoDefenseRewardCatalogAsset CreateTransient(
            IdleAutoDefenseRewardDraftSettings settings = null,
            IdleAutoDefenseRewardDraftCatalog catalog = null)
        {
            var asset = CreateInstance<IdleAutoDefenseRewardCatalogAsset>();
            asset.hideFlags = HideFlags.HideAndDontSave;
            asset.Configure(
                "reward-catalog.idle-auto-defense.transient-fallback",
                "Transient Idle Auto Defense Fallback Rewards",
                30f,
                settings ?? IdleAutoDefenseRewardDraftSettings.CreateDefault(),
                catalog ?? IdleAutoDefenseRewardDraftCatalog.CreateDefault());
            return asset;
        }
    }
}
