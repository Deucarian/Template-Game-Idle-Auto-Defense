using System;
using System.Collections.Generic;
using System.IO;
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

    public sealed class IdleAutoDefenseTemplateSetupRequest
    {
        public string TargetRootAssetPath = "Assets/Games/MyIdleAutoDefense";
        public string GameNamespace = "MyCompany.MyIdleAutoDefense";
        public string GamePrefix = "MyIdle";
        public bool AllowOverwrite;
        public bool OpenCreatedScene;
        public bool RefreshAssetDatabase = true;
    }

    public sealed class IdleAutoDefenseTemplateSetupResult
    {
        private readonly List<string> _createdFiles = new List<string>();
        private readonly List<string> _createdDirectories = new List<string>();
        private readonly List<string> _blockedFiles = new List<string>();
        private readonly List<string> _messages = new List<string>();

        public IdleAutoDefenseTemplateSetupStatus Status { get; internal set; }
        public string TargetRootAssetPath { get; internal set; } = string.Empty;
        public string CreatedSceneAssetPath { get; internal set; } = string.Empty;
        public string SetupReportAssetPath { get; internal set; } = string.Empty;
        public IReadOnlyList<string> CreatedFiles => _createdFiles;
        public IReadOnlyList<string> CreatedDirectories => _createdDirectories;
        public IReadOnlyList<string> BlockedFiles => _blockedFiles;
        public IReadOnlyList<string> Messages => _messages;
        public bool Succeeded => Status == IdleAutoDefenseTemplateSetupStatus.Succeeded;

        internal void AddCreatedFile(string path) => _createdFiles.Add(path);
        internal void AddCreatedDirectory(string path) => _createdDirectories.Add(path);
        internal void AddBlockedFile(string path) => _blockedFiles.Add(path);
        internal void AddMessage(string message) => _messages.Add(message);

        public string CreateSummary()
        {
            if (Succeeded)
            {
                return "Created Idle Auto Defense game folder at " + TargetRootAssetPath +
                    "\nScene: " + CreatedSceneAssetPath +
                    "\nReport: " + SetupReportAssetPath +
                    "\nNext: replace content assets, tune JSON, then press Play.";
            }

            if (Status == IdleAutoDefenseTemplateSetupStatus.BlockedByExistingFiles)
                return "Setup blocked because existing files would be overwritten. Enable overwrite only after reviewing the target folder.";

            return Messages.Count == 0 ? "Setup failed." : string.Join("\n", Messages);
        }
    }

    public static class IdleAutoDefenseTemplateSetupService
    {
        private const string SampleFolderName = "BasicIdleAutoDefenseGame";
        private const string SampleSceneName = "BasicIdleAutoDefenseGame.unity";
        private const string SampleScriptName = "BasicIdleAutoDefenseGameBootstrap.cs";
        private const string SampleBootstrapClass = "BasicIdleAutoDefenseGameBootstrap";
        private const string SampleSaveClass = "BasicIdleAutoDefenseSampleSave";
        private const string SampleNamespace = "Deucarian.TemplateGameIdleAutoDefense.Samples";
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

        public static IdleAutoDefenseTemplateSetupResult CreateGameFromTemplate(IdleAutoDefenseTemplateSetupRequest request)
        {
            request ??= new IdleAutoDefenseTemplateSetupRequest();
            var result = new IdleAutoDefenseTemplateSetupResult();
            try
            {
                string targetRoot = NormalizeAssetPath(request.TargetRootAssetPath);
                string gameNamespace = NormalizeNamespace(request.GameNamespace);
                string prefix = ToIdentifierPrefix(request.GamePrefix);
                if (string.IsNullOrEmpty(prefix)) prefix = LastNamespacePart(gameNamespace);

                result.TargetRootAssetPath = targetRoot;
                string targetFullRoot = AssetPathToFullPath(targetRoot);
                if (!targetRoot.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) ||
                    !IsPathInsideDirectory(targetFullRoot, Application.dataPath))
                {
                    throw new ArgumentException("Target root must be a product folder under Assets.");
                }

                if (!IsValidNamespace(gameNamespace))
                    throw new ArgumentException("Game namespace must be a valid C# namespace.");

                string sourceRoot = FindSourceSampleRoot();
                string className = prefix + "IdleAutoDefenseGameBootstrap";
                string saveClassName = prefix + "IdleAutoDefenseSave";
                string sceneAssetPath = targetRoot + "/Scenes/" + prefix + "IdleAutoDefense.unity";
                string reportAssetPath = targetRoot + "/Docs/setup-report.md";
                result.CreatedSceneAssetPath = sceneAssetPath;
                result.SetupReportAssetPath = reportAssetPath;

                string sourceScriptMeta = Path.Combine(sourceRoot, "Scripts", SampleScriptName + ".meta");
                string sourceScriptGuid = TryReadGuid(sourceScriptMeta);
                string generatedScriptGuid = GenerateUnityGuid();

                var operations = new List<FileOperation>();
                AddDirectoryCopyOperations(operations, Path.Combine(sourceRoot, "Content"), Path.Combine(targetFullRoot, "Content"));
                AddDirectoryCopyOperations(operations, Path.Combine(sourceRoot, "Prefabs"), Path.Combine(targetFullRoot, "Prefabs"));
                AddTextOperation(
                    operations,
                    Path.Combine(targetFullRoot, "README.md"),
                    TransformReadme(File.ReadAllText(Path.Combine(sourceRoot, "README.md")), request.GamePrefix, gameNamespace, className));
                AddTextOperation(
                    operations,
                    Path.Combine(targetFullRoot, gameNamespace + ".asmdef"),
                    CreateAsmdef(gameNamespace));
                AddTextOperation(
                    operations,
                    Path.Combine(targetFullRoot, "Scripts", className + ".cs"),
                    TransformScript(File.ReadAllText(Path.Combine(sourceRoot, "Scripts", SampleScriptName)), gameNamespace, className, saveClassName, prefix));
                AddTextOperation(
                    operations,
                    Path.Combine(targetFullRoot, "Scripts", className + ".cs.meta"),
                    CreateMonoScriptMeta(generatedScriptGuid));
                string sceneText = File.ReadAllText(Path.Combine(sourceRoot, "Scenes", SampleSceneName));
                if (!string.IsNullOrEmpty(sourceScriptGuid))
                    sceneText = sceneText.Replace(sourceScriptGuid, generatedScriptGuid);
                AddTextOperation(operations, AssetPathToFullPath(sceneAssetPath), sceneText);
                AddTextOperation(
                    operations,
                    Path.Combine(targetFullRoot, "Docs", "asset-flip-checklist.md"),
                    CreateAssetFlipChecklist(request.GamePrefix, gameNamespace));
                AddTextOperation(
                    operations,
                    Path.Combine(targetFullRoot, "Docs", "setup-report.md"),
                    CreateSetupReport(targetRoot, gameNamespace, prefix, sceneAssetPath));

                for (int i = 0; i < operations.Count; i++)
                {
                    string assetPath = FullPathToAssetPath(operations[i].DestinationFullPath);
                    if (File.Exists(operations[i].DestinationFullPath) && !request.AllowOverwrite)
                        result.AddBlockedFile(assetPath);
                }

                if (result.BlockedFiles.Count > 0)
                {
                    result.Status = IdleAutoDefenseTemplateSetupStatus.BlockedByExistingFiles;
                    return result;
                }

                CreateDirectory(targetFullRoot, result);
                CreateDirectory(Path.Combine(targetFullRoot, "Scripts"), result);
                CreateDirectory(Path.Combine(targetFullRoot, "Scenes"), result);
                CreateDirectory(Path.Combine(targetFullRoot, "Docs"), result);

                for (int i = 0; i < operations.Count; i++)
                {
                    FileOperation operation = operations[i];
                    string directory = Path.GetDirectoryName(operation.DestinationFullPath);
                    if (!string.IsNullOrEmpty(directory)) CreateDirectory(directory, result);
                    File.WriteAllBytes(operation.DestinationFullPath, operation.ContentBytes);
                    result.AddCreatedFile(FullPathToAssetPath(operation.DestinationFullPath));
                }

                result.Status = IdleAutoDefenseTemplateSetupStatus.Succeeded;
                result.AddMessage("Created product-owned Idle Auto Defense starter folder.");
                if (request.RefreshAssetDatabase) AssetDatabase.Refresh();
                if (request.OpenCreatedScene && !string.IsNullOrEmpty(sceneAssetPath))
                    EditorSceneManager.OpenScene(sceneAssetPath);
                return result;
            }
            catch (Exception ex)
            {
                result.Status = IdleAutoDefenseTemplateSetupStatus.Failed;
                result.AddMessage(ex.Message);
                return result;
            }
        }

        internal static string NormalizeAssetPath(string value)
        {
            string path = (value ?? string.Empty).Trim().Replace('\\', '/');
            while (path.Contains("//")) path = path.Replace("//", "/");
            return path.TrimEnd('/');
        }

        internal static string ToIdentifierPrefix(string value)
        {
            string source = value ?? string.Empty;
            var builder = new StringBuilder();
            bool nextUpper = true;
            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];
                if (char.IsLetterOrDigit(c))
                {
                    if (builder.Length == 0 && char.IsDigit(c)) builder.Append('_');
                    builder.Append(nextUpper ? char.ToUpperInvariant(c) : c);
                    nextUpper = false;
                }
                else
                {
                    nextUpper = true;
                }
            }

            return builder.ToString();
        }

        private static string FindSourceSampleRoot()
        {
            PackageInfo packageInfo = PackageInfo.FindForAssembly(typeof(IdleAutoDefenseTemplateController).Assembly);
            if (packageInfo == null || string.IsNullOrWhiteSpace(packageInfo.resolvedPath))
                throw new InvalidOperationException("Could not find installed Idle Auto Defense template package.");
            string sourceRoot = Path.Combine(packageInfo.resolvedPath, "Samples~", SampleFolderName);
            if (!Directory.Exists(sourceRoot))
                throw new DirectoryNotFoundException("Could not find template sample folder: " + sourceRoot);
            return sourceRoot;
        }

        private static void AddDirectoryCopyOperations(List<FileOperation> operations, string sourceDirectory, string destinationDirectory)
        {
            if (!Directory.Exists(sourceDirectory)) return;
            string[] files = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                if (files[i].EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
                string relative = files[i].Substring(sourceDirectory.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                AddFileOperation(operations, Path.Combine(destinationDirectory, relative), File.ReadAllBytes(files[i]));
            }
        }

        private static void AddTextOperation(List<FileOperation> operations, string destinationFullPath, string content)
        {
            AddFileOperation(operations, destinationFullPath, Utf8NoBom.GetBytes(content ?? string.Empty));
        }

        private static void AddFileOperation(List<FileOperation> operations, string destinationFullPath, byte[] contentBytes)
        {
            operations.Add(new FileOperation(Path.GetFullPath(destinationFullPath), contentBytes ?? Array.Empty<byte>()));
        }

        private static string TransformScript(string source, string gameNamespace, string className, string saveClassName, string prefix)
        {
            return source
                .Replace("namespace " + SampleNamespace, "namespace " + gameNamespace)
                .Replace(SampleBootstrapClass, className)
                .Replace(SampleSaveClass, saveClassName)
                .Replace("IdleAutoDefenseTemplateSample", prefix + "IdleAutoDefense")
                .Replace("Idle Auto Defense Starter", prefix + " Idle Auto Defense")
                .Replace("Content/starter-content.json and Scripts/BasicIdleAutoDefenseGameBootstrap.cs", "Content/starter-content.json and Scripts/" + className + ".cs");
        }

        private static string TransformReadme(string source, string displayName, string gameNamespace, string className)
        {
            return source
                .Replace("# Basic Idle Auto Defense Game", "# " + displayName + " Idle Auto Defense")
                .Replace("BasicIdleAutoDefenseGameBootstrap.cs", className + ".cs")
                .Replace("Deucarian.TemplateGameIdleAutoDefense.Samples", gameNamespace);
        }

        private static string CreateAsmdef(string gameNamespace)
        {
            return "{\n" +
                   "  \"name\": \"" + EscapeJson(gameNamespace) + "\",\n" +
                   "  \"rootNamespace\": \"" + EscapeJson(gameNamespace) + "\",\n" +
                   "  \"references\": [\n" +
                   "    \"Deucarian.TemplateGameIdleAutoDefense\"\n" +
                   "  ],\n" +
                   "  \"includePlatforms\": [],\n" +
                   "  \"excludePlatforms\": [],\n" +
                   "  \"allowUnsafeCode\": false,\n" +
                   "  \"overrideReferences\": false,\n" +
                   "  \"precompiledReferences\": [],\n" +
                   "  \"autoReferenced\": true,\n" +
                   "  \"defineConstraints\": [],\n" +
                   "  \"versionDefines\": [],\n" +
                   "  \"noEngineReferences\": false\n" +
                   "}\n";
        }

        private static string CreateMonoScriptMeta(string guid)
        {
            return "fileFormatVersion: 2\n" +
                   "guid: " + guid + "\n" +
                   "MonoImporter:\n" +
                   "  externalObjects: {}\n" +
                   "  serializedVersion: 2\n" +
                   "  defaultReferences: []\n" +
                   "  executionOrder: 0\n" +
                   "  icon: {instanceID: 0}\n" +
                   "  userData: \n" +
                   "  assetBundleName: \n" +
                   "  assetBundleVariant: \n";
        }

        private static string CreateAssetFlipChecklist(string displayName, string gameNamespace)
        {
            return "# Asset Flip Checklist\n\n" +
                   "Game: " + displayName + "\n\n" +
                   "Namespace: `" + gameNamespace + "`\n\n" +
                   "## Replace First\n\n" +
                   "- Enemies\n" +
                   "- Weapons\n" +
                   "- Projectiles\n" +
                   "- Stages\n" +
                   "- Waves\n" +
                   "- Run upgrades\n" +
                   "- Progression values\n" +
                   "- Monetization placements\n" +
                   "- Save/profile names\n\n" +
                   "The generated folder is product-owned. Do not copy package `Runtime` or `Editor` source into it.\n\n" +
                   "## Workflow\n\n" +
                   "1. Replace enemy visuals in `Prefabs/Enemies` or update the bootstrap prefab providers.\n" +
                   "2. Replace weapon and projectile visuals in `Prefabs/Weapons` and `Prefabs/Projectiles`.\n" +
                   "3. Tune `Content/DefaultStages/stages.json` and `Content/DefaultWaves/stages-and-encounters.json`.\n" +
                   "4. Tune `Content/DefaultUpgrades/common-run-upgrades.json`.\n" +
                   "5. Tune rewards and offline progression in `Content/DefaultProgression/currencies-rewards-saves.json`.\n" +
                   "6. Rename template IDs into your product namespace as content becomes product-owned.\n" +
                   "7. Keep Deucarian package source out of this folder.\n";
        }

        private static string CreateSetupReport(string targetRoot, string gameNamespace, string prefix, string sceneAssetPath)
        {
            return "# Idle Auto Defense Setup Report\n\n" +
                   "- Created UTC: " + DateTimeOffset.UtcNow.ToString("O") + "\n" +
                   "- Target root: `" + targetRoot + "`\n" +
                   "- Namespace: `" + gameNamespace + "`\n" +
                   "- Prefix: `" + prefix + "`\n" +
                   "- Scene: `" + sceneAssetPath + "`\n" +
                   "- Dependencies: kept in Deucarian packages; generated assembly references `Deucarian.TemplateGameIdleAutoDefense`.\n\n" +
                   "## Next Steps\n\n" +
                   "1. Open the created scene and press Play.\n" +
                   "2. Replace placeholder visuals.\n" +
                   "3. Rename product content IDs.\n" +
                   "4. Tune stages, waves, upgrades, rewards, and offline progression.\n" +
                   "5. Keep reusable framework code in Deucarian packages.\n";
        }

        private static string AssetPathToFullPath(string assetPath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static bool IsPathInsideDirectory(string fullPath, string directory)
        {
            string normalizedPath = EnsureTrailingSeparator(Path.GetFullPath(fullPath));
            string normalizedDirectory = EnsureTrailingSeparator(Path.GetFullPath(directory));
            return normalizedPath.StartsWith(normalizedDirectory, StringComparison.OrdinalIgnoreCase);
        }

        private static string EnsureTrailingSeparator(string path)
        {
            if (path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ||
                path.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                return path;
            }

            return path + Path.DirectorySeparatorChar;
        }

        private static string FullPathToAssetPath(string fullPath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string normalizedFull = Path.GetFullPath(fullPath);
            string relative = normalizedFull.Substring(projectRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return relative.Replace('\\', '/');
        }

        private static string NormalizeNamespace(string value)
        {
            return (value ?? string.Empty).Trim();
        }

        private static bool IsValidNamespace(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            string[] parts = value.Split('.');
            for (int i = 0; i < parts.Length; i++)
            {
                if (!IsValidIdentifier(parts[i])) return false;
            }

            return true;
        }

        private static bool IsValidIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            if (!(char.IsLetter(value[0]) || value[0] == '_')) return false;
            for (int i = 1; i < value.Length; i++)
            {
                if (!(char.IsLetterOrDigit(value[i]) || value[i] == '_')) return false;
            }

            return true;
        }

        private static string LastNamespacePart(string gameNamespace)
        {
            string[] parts = gameNamespace.Split('.');
            return ToIdentifierPrefix(parts[parts.Length - 1]);
        }

        private static void CreateDirectory(string fullPath, IdleAutoDefenseTemplateSetupResult result)
        {
            if (Directory.Exists(fullPath)) return;
            Directory.CreateDirectory(fullPath);
            result.AddCreatedDirectory(FullPathToAssetPath(fullPath));
        }

        private static string TryReadGuid(string metaPath)
        {
            if (!File.Exists(metaPath)) return string.Empty;
            string[] lines = File.ReadAllLines(metaPath);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("guid:", StringComparison.Ordinal))
                    return line.Substring("guid:".Length).Trim();
            }

            return string.Empty;
        }

        private static string GenerateUnityGuid()
        {
            return Guid.NewGuid().ToString("N");
        }

        private static string EscapeJson(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private readonly struct FileOperation
        {
            public FileOperation(string destinationFullPath, byte[] contentBytes)
            {
                DestinationFullPath = destinationFullPath;
                ContentBytes = contentBytes;
            }

            public string DestinationFullPath { get; }
            public byte[] ContentBytes { get; }
        }
    }

    public sealed class IdleAutoDefenseTemplateSetupWizardWindow : EditorWindow
    {
        private string _targetRoot = "Assets/Games/MyIdleAutoDefense";
        private string _gameNamespace = "MyCompany.MyIdleAutoDefense";
        private string _gamePrefix = "MyIdle";
        private bool _openScene = true;
        private bool _allowOverwrite;
        private string _lastSummary = string.Empty;

        public static void Open()
        {
            var window = GetWindow<IdleAutoDefenseTemplateSetupWizardWindow>("Idle Auto Defense Setup");
            window.minSize = new Vector2(430f, 280f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Create Game From Template", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            _targetRoot = EditorGUILayout.TextField("Target root", _targetRoot);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUIUtility.labelWidth);
                if (GUILayout.Button("Choose Folder", GUILayout.Width(120f)))
                    ChooseFolder();
            }

            _gameNamespace = EditorGUILayout.TextField("Namespace", _gameNamespace);
            _gamePrefix = EditorGUILayout.TextField("Game prefix", _gamePrefix);
            _openScene = EditorGUILayout.Toggle("Open created scene", _openScene);
            _allowOverwrite = EditorGUILayout.Toggle("Allow overwrite", _allowOverwrite);

            EditorGUILayout.HelpBox(
                "Copies the starter scene, content, prefabs, and bootstrap script into a project-owned folder. Deucarian package source stays in packages.",
                MessageType.Info);

            if (GUILayout.Button("Create Game"))
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
                GameNamespace = _gameNamespace,
                GamePrefix = _gamePrefix,
                AllowOverwrite = _allowOverwrite,
                OpenCreatedScene = _openScene
            };

            IdleAutoDefenseTemplateSetupResult result = IdleAutoDefenseTemplateSetupService.CreateGameFromTemplate(request);
            _lastSummary = result.CreateSummary();
            if (result.Status == IdleAutoDefenseTemplateSetupStatus.BlockedByExistingFiles)
            {
                EditorUtility.DisplayDialog(
                    "Idle Auto Defense Setup",
                    "Existing files would be overwritten. Review the target folder or enable Allow overwrite.",
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
