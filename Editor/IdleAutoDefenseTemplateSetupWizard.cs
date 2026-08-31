using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Deucarian.TemplateGameIdleAutoDefense;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Deucarian.TemplateGameIdleAutoDefense.Editor
{
    public enum IdleAutoDefenseTemplateSetupStatus
    {
        Succeeded = 0,
        BlockedByExistingFiles = 1,
        Failed = 2
    }

    public enum IdleAutoDefenseTemplatePackSelection
    {
        BasicOnly = 0,
        ScrapFrontierOnly = 1,
        Both = 2
    }

    public sealed class IdleAutoDefenseTemplateSetupRequest
    {
        public string TargetRootAssetPath = "Assets/IdleAutoDefense";
        public string ContentRootAssetPath = "Assets/GameContent/IdleAutoDefense";
        public string GameNamespace = "IdleAutoDefenseGame";
        public string GamePrefix = "Basic";
        public IdleAutoDefenseTemplatePackSelection PackSelection = IdleAutoDefenseTemplatePackSelection.BasicOnly;
        public bool RepairMissingContent;
        public bool AllowOverwrite;
        public bool OpenCreatedScene;
        public bool RefreshAssetDatabase = true;
    }

    public sealed class IdleAutoDefenseTemplateSetupResult
    {
        private readonly List<string> _createdFiles = new List<string>();
        private readonly List<string> _createdDirectories = new List<string>();
        private readonly List<string> _blockedFiles = new List<string>();
        private readonly List<string> _createdSceneAssetPaths = new List<string>();
        private readonly List<string> _messages = new List<string>();

        public IdleAutoDefenseTemplateSetupStatus Status { get; internal set; }
        public string TargetRootAssetPath { get; internal set; } = string.Empty;
        public string ContentRootAssetPath { get; internal set; } = string.Empty;
        public string CreatedSceneAssetPath { get; internal set; } = string.Empty;
        public string SetupReportAssetPath { get; internal set; } = string.Empty;
        public IReadOnlyList<string> CreatedFiles => _createdFiles;
        public IReadOnlyList<string> CreatedDirectories => _createdDirectories;
        public IReadOnlyList<string> BlockedFiles => _blockedFiles;
        public IReadOnlyList<string> CreatedSceneAssetPaths => _createdSceneAssetPaths;
        public IReadOnlyList<string> Messages => _messages;
        public bool Succeeded => Status == IdleAutoDefenseTemplateSetupStatus.Succeeded;

        internal void AddCreatedFile(string path) => _createdFiles.Add(path);
        internal void AddCreatedDirectory(string path) => _createdDirectories.Add(path);
        internal void AddBlockedFile(string path) => _blockedFiles.Add(path);
        internal void AddCreatedScene(string path)
        {
            if (!string.IsNullOrWhiteSpace(path) && !_createdSceneAssetPaths.Contains(path))
                _createdSceneAssetPaths.Add(path);
        }
        internal void AddMessage(string message) => _messages.Add(message);

        public string CreateSummary()
        {
            if (Succeeded)
            {
                return "Created playable Idle Auto Defense game folder at " + TargetRootAssetPath +
                    "\nAuthored content: " + ContentRootAssetPath +
                    "\nScene(s): " + string.Join(", ", CreatedSceneAssetPaths) +
                    "\nReport: " + SetupReportAssetPath +
                    "\nNext: open the created scene and press Play.";
            }

            if (Status == IdleAutoDefenseTemplateSetupStatus.BlockedByExistingFiles)
                return "Setup blocked because existing files would be overwritten. Enable overwrite only after reviewing the target and content folders." +
                    "\nBlocked files:\n" + string.Join("\n", BlockedFiles);

            return Messages.Count == 0 ? "Setup failed." : string.Join("\n", Messages);
        }
    }

    public static partial class IdleAutoDefenseTemplateSetupService
    {
        private const string BasicTemplateSourceFolderName = "BasicIdleAutoDefenseGame";
        private const string ScrapTemplateSourceFolderName = "ScrapFrontierGame";
        private const string BasicSampleSceneName = "OPEN_THIS_TO_TEST_IdleAutoDefense_PlayableGame.unity";
        private const string ScrapSampleSceneName = "OPEN_THIS_TO_TEST_ScrapFrontier_PlayableGame.unity";
        private const string BasicVisibleSceneRootAssetPath = "Assets/OPEN_THIS_TO_TEST_IdleAutoDefense_PlayableGame";
        private const string ScrapVisibleSceneRootAssetPath = "Assets/OPEN_THIS_TO_TEST_ScrapFrontier_PlayableGame";
        private const string SampleScriptName = "BasicIdleAutoDefenseGameBootstrap.cs";
        private const string SampleBootstrapClass = "BasicIdleAutoDefenseGameBootstrap";
        private const string SampleSaveClass = "BasicIdleAutoDefenseSampleSave";
        private const string SampleNamespace = "Deucarian.TemplateGameIdleAutoDefense.Samples";
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);
    }

    [InitializeOnLoad]
    internal static class IdleAutoDefenseGeneratedSceneOpenQueue
    {
        private const string PendingScenePathKey = "Deucarian.IdleAutoDefenseTemplate.PendingScenePath";
        private const string PendingContentRootKey = "Deucarian.IdleAutoDefenseTemplate.PendingContentRoot";
        private const string PendingBootstrapTypeKey = "Deucarian.IdleAutoDefenseTemplate.PendingBootstrapType";
        private const string PendingContentPackRelativePathKey = "Deucarian.IdleAutoDefenseTemplate.PendingContentPackRelativePath";
        private const string PendingContentSetRelativePathKey = "Deucarian.IdleAutoDefenseTemplate.PendingContentSetRelativePath";
        private const string PendingPlayerExperienceRelativePathKey = "Deucarian.IdleAutoDefenseTemplate.PendingPlayerExperienceRelativePath";
        private const string PendingAttemptCountKey = "Deucarian.IdleAutoDefenseTemplate.PendingAttemptCount";
        private const int MaximumOpenAttempts = 300;
        private const string ContentPackAssetRelativePath = "/ContentPacks/contentpack.idle-auto-defense.playable/contentpack.idle-auto-defense.playable_ContentPack.asset";
        private const string ContentSetAssetRelativePath = "/ContentSets/contentset.idle-auto-defense.playable/contentset.idle-auto-defense.playable_GameContentSet.asset";
        private const string PlayerExperienceAssetRelativePath = "/Presentation/player-experience.idle-auto-defense.playable.asset";
        private static bool _queued;

        static IdleAutoDefenseGeneratedSceneOpenQueue()
        {
            QueuePendingOpen();
        }

        internal static void Queue(
            string sceneAssetPath,
            string contentRootAssetPath,
            string bootstrapTypeFullName,
            string contentPackAssetRelativePath,
            string contentSetAssetRelativePath,
            string playerExperienceAssetRelativePath)
        {
            if (string.IsNullOrWhiteSpace(sceneAssetPath) ||
                string.IsNullOrWhiteSpace(contentRootAssetPath) ||
                string.IsNullOrWhiteSpace(bootstrapTypeFullName))
            {
                return;
            }

            SessionState.SetString(PendingScenePathKey, sceneAssetPath);
            SessionState.SetString(PendingContentRootKey, contentRootAssetPath);
            SessionState.SetString(PendingBootstrapTypeKey, bootstrapTypeFullName);
            SessionState.SetString(PendingContentPackRelativePathKey, contentPackAssetRelativePath);
            SessionState.SetString(PendingContentSetRelativePathKey, contentSetAssetRelativePath);
            SessionState.SetString(PendingPlayerExperienceRelativePathKey, playerExperienceAssetRelativePath);
            SessionState.SetInt(PendingAttemptCountKey, 0);
            QueuePendingOpen();
        }

        private static void QueuePendingOpen()
        {
            if (_queued || string.IsNullOrEmpty(SessionState.GetString(PendingScenePathKey, string.Empty))) return;
            _queued = true;
            EditorApplication.delayCall += TryOpenPendingScene;
        }

        private static void TryOpenPendingScene()
        {
            _queued = false;
            string sceneAssetPath = SessionState.GetString(PendingScenePathKey, string.Empty);
            if (string.IsNullOrEmpty(sceneAssetPath)) return;

            int attempts = SessionState.GetInt(PendingAttemptCountKey, 0) + 1;
            SessionState.SetInt(PendingAttemptCountKey, attempts);
            if (attempts > MaximumOpenAttempts)
            {
                Clear();
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                QueuePendingOpen();
                return;
            }

            string bootstrapTypeFullName = SessionState.GetString(PendingBootstrapTypeKey, string.Empty);
            if (!IsTypeAvailable(bootstrapTypeFullName))
            {
                QueuePendingOpen();
                return;
            }

            string fullScenePath = AssetPathToFullPath(sceneAssetPath);
            if (!File.Exists(fullScenePath))
            {
                Clear();
                return;
            }

            string contentRootAssetPath = SessionState.GetString(PendingContentRootKey, string.Empty);
            string contentPackAssetRelativePath = SessionState.GetString(PendingContentPackRelativePathKey, ContentPackAssetRelativePath);
            string contentSetAssetRelativePath = SessionState.GetString(PendingContentSetRelativePathKey, ContentSetAssetRelativePath);
            string playerExperienceAssetRelativePath = SessionState.GetString(PendingPlayerExperienceRelativePathKey, PlayerExperienceAssetRelativePath);
            var scene = EditorSceneManager.OpenScene(sceneAssetPath);
            if (ReapplyGeneratedSceneContent(
                    sceneAssetPath,
                    contentRootAssetPath,
                    bootstrapTypeFullName,
                    contentPackAssetRelativePath,
                    contentSetAssetRelativePath,
                    playerExperienceAssetRelativePath))
                EditorSceneManager.SaveScene(scene);
            Clear();
        }

        private static bool ReapplyGeneratedSceneContent(
            string sceneAssetPath,
            string contentRootAssetPath,
            string bootstrapTypeFullName,
            string contentPackAssetRelativePath,
            string contentSetAssetRelativePath,
            string playerExperienceAssetRelativePath)
        {
            GameContentPackAsset contentPack = AssetDatabase.LoadAssetAtPath<GameContentPackAsset>(contentRootAssetPath + contentPackAssetRelativePath);
            GameContentSetAsset contentSet = AssetDatabase.LoadAssetAtPath<GameContentSetAsset>(contentRootAssetPath + contentSetAssetRelativePath);
            IdleAutoDefensePlayerExperienceAsset playerExperience = AssetDatabase.LoadAssetAtPath<IdleAutoDefensePlayerExperienceAsset>(contentRootAssetPath + playerExperienceAssetRelativePath);
            if (contentPack == null || contentSet == null || playerExperience == null) return false;

            bool changed = false;
            MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null ||
                    behaviour.gameObject == null ||
                    !string.Equals(behaviour.gameObject.scene.path, sceneAssetPath, StringComparison.Ordinal) ||
                    !string.Equals(behaviour.GetType().FullName, bootstrapTypeFullName, StringComparison.Ordinal))
                {
                    continue;
                }

                var serialized = new SerializedObject(behaviour);
                bool behaviourChanged = false;
                behaviourChanged |= AssignObjectReference(serialized, "_templateContentPack", contentPack);
                behaviourChanged |= AssignObjectReference(serialized, "_templateContentSet", contentSet);
                behaviourChanged |= AssignObjectReference(serialized, "_templatePlayerExperience", playerExperience);
                behaviourChanged |= AssignObjectReference(serialized, "_contentPack", contentPack);
                behaviourChanged |= AssignObjectReference(serialized, "_contentSet", contentSet);
                behaviourChanged |= AssignObjectReference(serialized, "_playerExperience", playerExperience);
                if (behaviourChanged)
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                changed |= behaviourChanged;
            }

            if (changed)
                EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetSceneByPath(sceneAssetPath));
            return changed;
        }

        private static bool AssignObjectReference(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || property.objectReferenceValue == value) return false;
            property.objectReferenceValue = value;
            return true;
        }

        private static bool IsTypeAvailable(string typeFullName)
        {
            if (string.IsNullOrWhiteSpace(typeFullName)) return false;
            System.Reflection.Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                if (assemblies[i].GetType(typeFullName, false) != null)
                    return true;
            }

            return false;
        }

        private static string AssetPathToFullPath(string assetPath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static void Clear()
        {
            SessionState.EraseString(PendingScenePathKey);
            SessionState.EraseString(PendingContentRootKey);
            SessionState.EraseString(PendingBootstrapTypeKey);
            SessionState.EraseString(PendingContentPackRelativePathKey);
            SessionState.EraseString(PendingContentSetRelativePathKey);
            SessionState.EraseString(PendingPlayerExperienceRelativePathKey);
            SessionState.EraseInt(PendingAttemptCountKey);
        }
    }

    public sealed class IdleAutoDefenseTemplateSetupWizardWindow : EditorWindow
    {
        private string _targetRoot = "Assets/IdleAutoDefense";
        private string _contentRoot = "Assets/GameContent/IdleAutoDefense";
        private string _gameNamespace = "IdleAutoDefenseGame";
        private string _gamePrefix = "Basic";
        private IdleAutoDefenseTemplatePackSelection _packSelection = IdleAutoDefenseTemplatePackSelection.BasicOnly;
        private bool _openScene = true;
        private bool _repairMissingContent;
        private bool _allowOverwrite;
        private string _lastSummary = string.Empty;

        public static void Open()
        {
            var window = GetWindow<IdleAutoDefenseTemplateSetupWizardWindow>("Create Playable Idle Defense");
            window.minSize = new Vector2(430f, 280f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Create Playable Game", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            _targetRoot = EditorGUILayout.TextField("Target root", _targetRoot);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUIUtility.labelWidth);
                if (GUILayout.Button("Choose Folder", GUILayout.Width(120f)))
                    ChooseFolder();
            }

            _contentRoot = EditorGUILayout.TextField("Content root", _contentRoot);
            _gameNamespace = EditorGUILayout.TextField("Namespace", _gameNamespace);
            _gamePrefix = EditorGUILayout.TextField("Game prefix", _gamePrefix);
            _packSelection = (IdleAutoDefenseTemplatePackSelection)EditorGUILayout.EnumPopup("Content packs", _packSelection);
            _openScene = EditorGUILayout.Toggle("Open created scene", _openScene);
            _repairMissingContent = EditorGUILayout.Toggle("Repair missing content", _repairMissingContent);
            _allowOverwrite = EditorGUILayout.Toggle("Allow overwrite", _allowOverwrite);

            EditorGUILayout.HelpBox(
                "Creates Basic Idle Auto Defense, the Scrap Frontier asset-flip proof, or both as separate strict-authored packs. Each selected pack gets a visible playable scene; shared runtime code remains in the package.",
                MessageType.Info);

            if (GUILayout.Button("Create Playable Game"))
                CreateGame();

            if (!string.IsNullOrEmpty(_lastSummary))
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(_lastSummary, MessageType.None);
            }
        }

        private void ChooseFolder()
        {
            string selected = EditorUtility.OpenFolderPanel("Target folder under Assets", Application.dataPath, string.Empty);
            if (string.IsNullOrEmpty(selected)) return;
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string full = Path.GetFullPath(selected);
            if (!full.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("Idle Auto Defense Setup", "Choose a folder inside this Unity project.", "OK");
                return;
            }

            _targetRoot = full.Substring(projectRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace('\\', '/');
        }

        private void CreateGame()
        {
            var request = new IdleAutoDefenseTemplateSetupRequest
            {
                TargetRootAssetPath = _targetRoot,
                ContentRootAssetPath = _contentRoot,
                GameNamespace = _gameNamespace,
                GamePrefix = _gamePrefix,
                PackSelection = _packSelection,
                RepairMissingContent = _repairMissingContent,
                AllowOverwrite = _allowOverwrite,
                OpenCreatedScene = _openScene
            };

            IdleAutoDefenseTemplateSetupResult result = IdleAutoDefenseTemplateSetupService.CreateGameFromTemplate(request);
            _lastSummary = result.CreateSummary();
            if (result.Status == IdleAutoDefenseTemplateSetupStatus.BlockedByExistingFiles)
            {
                EditorUtility.DisplayDialog(
                    "Idle Auto Defense Setup",
                    "Existing files would be overwritten. Review the target and content folders or enable Allow overwrite.",
                    "OK");
            }
            else if (result.Succeeded)
            {
                EditorUtility.DisplayDialog("Idle Auto Defense Setup", _lastSummary, "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Idle Auto Defense Setup", _lastSummary, "OK");
            }
        }
    }
}
