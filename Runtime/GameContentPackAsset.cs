using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    public sealed class GameContentPackAsset : ScriptableObject
    {
        [SerializeField] private string _id = "contentpack.example.idle-auto-defense";
        [SerializeField] private string _displayName = "Example Idle Auto Defense Pack";
        [SerializeField] private string _description = "A packaged playable idle auto-defense content recipe.";
        [SerializeField] private string _version = "0.1.0";
        [SerializeField] private string _author = "Deucarian";
        [SerializeField] private Sprite _icon;
        [SerializeField] private Texture2D _banner;
        [SerializeField] private GameContentSetAsset[] _contentSets = Array.Empty<GameContentSetAsset>();
        [SerializeField] private GameContentSetAsset _defaultContentSet;
        [SerializeField] private string[] _requiredPackages = Array.Empty<string>();
        [SerializeField] private string[] _minimumPackageVersions = Array.Empty<string>();
        [SerializeField] private string _templateCompatibilityNotes = "Idle Auto Defense template content pack.";
        [SerializeField] private string[] _tags = Array.Empty<string>();

        public string Id => _id ?? string.Empty;
        public string DisplayName => _displayName ?? string.Empty;
        public string Description => _description ?? string.Empty;
        public string Version => _version ?? string.Empty;
        public string Author => _author ?? string.Empty;
        public Sprite Icon => _icon;
        public Texture2D Banner => _banner;
        public IReadOnlyList<GameContentSetAsset> ContentSets => _contentSets ?? Array.Empty<GameContentSetAsset>();
        public GameContentSetAsset DefaultContentSet => _defaultContentSet;
        public IReadOnlyList<string> RequiredPackages => _requiredPackages ?? Array.Empty<string>();
        public IReadOnlyList<string> MinimumPackageVersions => _minimumPackageVersions ?? Array.Empty<string>();
        public string TemplateCompatibilityNotes => _templateCompatibilityNotes ?? string.Empty;
        public IReadOnlyList<string> Tags => _tags ?? Array.Empty<string>();

        public void Configure(
            string id,
            string displayName,
            string description,
            string version,
            string author,
            Sprite icon,
            Texture2D banner,
            IReadOnlyList<GameContentSetAsset> contentSets,
            GameContentSetAsset defaultContentSet,
            IReadOnlyList<string> requiredPackages,
            IReadOnlyList<string> minimumPackageVersions,
            string templateCompatibilityNotes,
            IReadOnlyList<string> tags)
        {
            _id = id ?? string.Empty;
            _displayName = displayName ?? string.Empty;
            _description = description ?? string.Empty;
            _version = version ?? string.Empty;
            _author = author ?? string.Empty;
            _icon = icon;
            _banner = banner;
            _contentSets = CopyAssets(contentSets);
            _defaultContentSet = defaultContentSet;
            _requiredPackages = CopyStrings(requiredPackages);
            _minimumPackageVersions = CopyStrings(minimumPackageVersions);
            _templateCompatibilityNotes = templateCompatibilityNotes ?? string.Empty;
            _tags = CopyStrings(tags);
        }

        public static GameContentPackAsset CreateTransient(
            string id,
            string displayName,
            IReadOnlyList<GameContentSetAsset> contentSets,
            GameContentSetAsset defaultContentSet,
            string version = "0.1.0",
            string author = "Deucarian",
            string description = "",
            IReadOnlyList<string> requiredPackages = null,
            IReadOnlyList<string> minimumPackageVersions = null,
            string templateCompatibilityNotes = "",
            IReadOnlyList<string> tags = null)
        {
            var asset = CreateInstance<GameContentPackAsset>();
            asset.hideFlags = HideFlags.HideAndDontSave;
            asset.Configure(
                id,
                displayName,
                description,
                version,
                author,
                null,
                null,
                contentSets,
                defaultContentSet,
                requiredPackages ?? Array.Empty<string>(),
                minimumPackageVersions ?? Array.Empty<string>(),
                templateCompatibilityNotes,
                tags ?? Array.Empty<string>());
            return asset;
        }

        private static TAsset[] CopyAssets<TAsset>(IReadOnlyList<TAsset> source) where TAsset : UnityEngine.Object
        {
            if (source == null || source.Count == 0) return Array.Empty<TAsset>();
            var copy = new TAsset[source.Count];
            for (int i = 0; i < source.Count; i++) copy[i] = source[i];
            return copy;
        }

        private static string[] CopyStrings(IReadOnlyList<string> source)
        {
            if (source == null || source.Count == 0) return Array.Empty<string>();
            var values = new List<string>();
            for (int i = 0; i < source.Count; i++)
            {
                string value = source[i];
                if (!string.IsNullOrWhiteSpace(value)) values.Add(value.Trim());
            }

            return values.ToArray();
        }
    }
}
