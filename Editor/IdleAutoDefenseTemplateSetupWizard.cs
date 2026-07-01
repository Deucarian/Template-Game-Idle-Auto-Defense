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
                return "Created playable Idle Auto Defense game folder at " + TargetRootAssetPath +
                    "\nScene: " + CreatedSceneAssetPath +
                    "\nReport: " + SetupReportAssetPath +
                    "\nNext: open the created scene and press Play.";
            }

            if (Status == IdleAutoDefenseTemplateSetupStatus.BlockedByExistingFiles)
                return "Setup blocked because existing files would be overwritten. Enable overwrite only after reviewing the target folder.";

            return Messages.Count == 0 ? "Setup failed." : string.Join("\n", Messages);
        }
    }

    public static class IdleAutoDefenseTemplateSetupService
    {
        private const string TemplateSourceFolderName = "BasicIdleAutoDefenseGame";
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

                string sourceRoot = FindTemplateSourceRoot();
                string className = prefix + "IdleAutoDefenseGameBootstrap";
                string saveClassName = prefix + "IdleAutoDefenseSave";
                string sceneAssetPath = targetRoot + "/Scenes/" + prefix + "IdleAutoDefense.unity";
                string reportAssetPath = targetRoot + "/Docs/setup-report.md";
                result.CreatedSceneAssetPath = sceneAssetPath;
                result.SetupReportAssetPath = reportAssetPath;

                string sourceScriptMeta = Path.Combine(sourceRoot, "Scripts", SampleScriptName + ".meta");
                string sourceScriptGuid = TryReadGuid(sourceScriptMeta);
                string generatedScriptGuid = GenerateUnityGuid();
                var guidMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(sourceScriptGuid))
                    guidMap[sourceScriptGuid] = generatedScriptGuid;

                var operations = new List<FileOperation>();
                AddDirectoryCopyOperations(operations, Path.Combine(sourceRoot, "Content"), Path.Combine(targetFullRoot, "Content"), true, guidMap);
                AddDirectoryCopyOperations(operations, Path.Combine(sourceRoot, "Prefabs"), Path.Combine(targetFullRoot, "Prefabs"), true, guidMap);
                AddDirectoryCopyOperations(operations, Path.Combine(sourceRoot, "Visuals"), Path.Combine(targetFullRoot, "Visuals"), true, guidMap);
                AddDirectoryCopyOperations(operations, Path.Combine(sourceRoot, "Audio"), Path.Combine(targetFullRoot, "Audio"), true, guidMap);
                AddTextOperation(
                    operations,
                    Path.Combine(targetFullRoot, "README.md"),
                    TransformReadme(ReadAllText(Path.Combine(sourceRoot, "README.md")), request.GamePrefix, gameNamespace, className));
                AddTextOperation(
                    operations,
                    Path.Combine(targetFullRoot, gameNamespace + ".asmdef"),
                    CreateAsmdef(gameNamespace));
                AddTextOperation(
                    operations,
                    Path.Combine(targetFullRoot, "Scripts", className + ".cs"),
                    TransformScript(ReadAllText(Path.Combine(sourceRoot, "Scripts", SampleScriptName)), gameNamespace, className, saveClassName, prefix));
                AddTextOperation(
                    operations,
                    Path.Combine(targetFullRoot, "Scripts", className + ".cs.meta"),
                    CreateMonoScriptMeta(generatedScriptGuid));
                string sceneText = ReadAllText(Path.Combine(sourceRoot, "Scenes", SampleSceneName));
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
                RewriteGuidReferences(operations, guidMap);

                for (int i = 0; i < operations.Count; i++)
                {
                    string assetPath = FullPathToAssetPath(operations[i].DestinationFullPath);
                    if (FileExists(operations[i].DestinationFullPath) && !request.AllowOverwrite)
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
                    WriteAllBytes(operation.DestinationFullPath, operation.ContentBytes);
                    result.AddCreatedFile(FullPathToAssetPath(operation.DestinationFullPath));
                }

                result.Status = IdleAutoDefenseTemplateSetupStatus.Succeeded;
                result.AddMessage("Created product-owned Idle Auto Defense starter folder.");
                string generatedBootstrapTypeName = gameNamespace + "." + className;
                if (request.OpenCreatedScene && request.RefreshAssetDatabase && !string.IsNullOrEmpty(sceneAssetPath))
                    IdleAutoDefenseGeneratedSceneOpenQueue.Queue(sceneAssetPath, targetRoot, generatedBootstrapTypeName);
                if (request.RefreshAssetDatabase) AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                if (request.OpenCreatedScene && !string.IsNullOrEmpty(sceneAssetPath) && !request.RefreshAssetDatabase)
                    EditorSceneManager.OpenScene(sceneAssetPath);
                else if (request.OpenCreatedScene && request.RefreshAssetDatabase && !string.IsNullOrEmpty(sceneAssetPath))
                    IdleAutoDefenseGeneratedSceneOpenQueue.Queue(sceneAssetPath, targetRoot, generatedBootstrapTypeName);
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

        private static string FindTemplateSourceRoot()
        {
            PackageInfo packageInfo = PackageInfo.FindForAssembly(typeof(IdleAutoDefenseTemplateController).Assembly);
            if (packageInfo == null || string.IsNullOrWhiteSpace(packageInfo.resolvedPath))
                throw new InvalidOperationException("Could not find installed Idle Auto Defense template package.");
            string sourceRoot = Path.Combine(packageInfo.resolvedPath, "TemplateSource~", TemplateSourceFolderName);
            if (!DirectoryExists(sourceRoot))
                throw new DirectoryNotFoundException("Could not find Idle Auto Defense template source folder: " + sourceRoot);
            return sourceRoot;
        }

        private static void AddDirectoryCopyOperations(
            List<FileOperation> operations,
            string sourceDirectory,
            string destinationDirectory,
            bool includeMetaFiles,
            Dictionary<string, string> guidMap)
        {
            if (!DirectoryExists(sourceDirectory)) return;
            string[] files = GetFiles(sourceDirectory);
            for (int i = 0; i < files.Length; i++)
            {
                if (!includeMetaFiles && files[i].EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
                string relative = files[i].Substring(Path.GetFullPath(sourceDirectory).Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                byte[] contentBytes = ReadAllBytes(files[i]);
                if (files[i].EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    contentBytes = RemapMetaGuid(files[i], contentBytes, guidMap);
                AddFileOperation(operations, Path.Combine(destinationDirectory, relative), contentBytes);
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

        private static byte[] RemapMetaGuid(string sourceMetaPath, byte[] contentBytes, Dictionary<string, string> guidMap)
        {
            string oldGuid = TryReadGuid(sourceMetaPath);
            if (string.IsNullOrWhiteSpace(oldGuid)) return contentBytes;

            string newGuid = GenerateUnityGuid();
            guidMap[oldGuid] = newGuid;
            string text = Utf8NoBom.GetString(contentBytes);
            return Utf8NoBom.GetBytes(ReplaceGuidLine(text, newGuid));
        }

        private static void RewriteGuidReferences(List<FileOperation> operations, Dictionary<string, string> guidMap)
        {
            if (guidMap == null || guidMap.Count == 0) return;
            for (int i = 0; i < operations.Count; i++)
            {
                FileOperation operation = operations[i];
                if (!CanContainUnityGuidReferences(operation.DestinationFullPath)) continue;
                string text = Utf8NoBom.GetString(operation.ContentBytes);
                string rewritten = ReplaceGuidReferences(text, guidMap);
                if (!string.Equals(text, rewritten, StringComparison.Ordinal))
                    operation.ContentBytes = Utf8NoBom.GetBytes(rewritten);
            }
        }

        private static bool CanContainUnityGuidReferences(string fullPath)
        {
            string extension = Path.GetExtension(fullPath);
            return extension.Equals(".asset", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".prefab", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".mat", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".meta", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".unity", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".asmdef", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".md", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".json", StringComparison.OrdinalIgnoreCase);
        }

        private static string ReplaceGuidReferences(string text, Dictionary<string, string> guidMap)
        {
            if (string.IsNullOrEmpty(text)) return text;
            string rewritten = text;
            foreach (KeyValuePair<string, string> pair in guidMap)
                rewritten = rewritten.Replace(pair.Key, pair.Value);
            return rewritten;
        }

        private static string ReplaceGuidLine(string text, string newGuid)
        {
            if (string.IsNullOrEmpty(text)) return text;
            string[] lines = text.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].TrimStart().StartsWith("guid:", StringComparison.Ordinal))
                {
                    int indentLength = lines[i].Length - lines[i].TrimStart().Length;
                    lines[i] = new string(' ', indentLength) + "guid: " + newGuid;
                    break;
                }
            }

            return string.Join("\n", lines);
        }

        private static string TransformScript(string source, string gameNamespace, string className, string saveClassName, string prefix)
        {
            return source
                .Replace("namespace " + SampleNamespace, "namespace " + gameNamespace)
                .Replace(SampleBootstrapClass, className)
                .Replace(SampleSaveClass, saveClassName)
                .Replace("IdleAutoDefenseTemplateSample", prefix + "IdleAutoDefense")
                .Replace("Idle Auto Defense Starter", prefix + " Idle Auto Defense");
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
                   "- Spawn profiles\n" +
                   "- Run upgrades\n" +
                   "- Progression values\n" +
                   "- Monetization placements\n" +
                   "- Save/profile names\n\n" +
                   "The generated folder is product-owned. Do not copy package `Runtime` or `Editor` source into it.\n\n" +
                   "## Workflow\n\n" +
                   "1. Replace enemy visuals in `Prefabs/Enemies` or update the bootstrap prefab providers.\n" +
                   "2. Replace weapon and projectile visuals in `Prefabs/Weapons` and `Prefabs/Projectiles`.\n" +
                   "3. Tune the enemy assets in `Content/Enemies`.\n" +
                   "4. Tune the attack and tower assets in `Content/Attacks` and `Content/Weapons`.\n" +
                   "5. Tune the spawn profiles and upgrades in `Content/Waves` and `Content/Upgrades`.\n" +
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
                   "2. Replace starter visuals.\n" +
                   "3. Rename product content IDs.\n" +
                   "4. Tune enemies, attacks, towers, spawn profiles, and upgrades.\n" +
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
            if (DirectoryExists(fullPath)) return;
            CreateDirectoryOnDisk(fullPath);
            result.AddCreatedDirectory(FullPathToAssetPath(fullPath));
        }

        private static string TryReadGuid(string metaPath)
        {
            if (!FileExists(metaPath)) return string.Empty;
            string[] lines = ReadAllLines(metaPath);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("guid:", StringComparison.Ordinal))
                    return line.Substring("guid:".Length).Trim();
            }

            return string.Empty;
        }

        private static string[] GetFiles(string fullPath)
        {
            string[] files = Directory.GetFiles(ToLongPath(fullPath), "*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
                files[i] = FromLongPath(files[i]);
            return files;
        }

        private static bool DirectoryExists(string fullPath)
        {
            return Directory.Exists(ToLongPath(fullPath));
        }

        private static bool FileExists(string fullPath)
        {
            return File.Exists(ToLongPath(fullPath));
        }

        private static string ReadAllText(string fullPath)
        {
            return File.ReadAllText(ToLongPath(fullPath));
        }

        private static string[] ReadAllLines(string fullPath)
        {
            return File.ReadAllLines(ToLongPath(fullPath));
        }

        private static byte[] ReadAllBytes(string fullPath)
        {
            return File.ReadAllBytes(ToLongPath(fullPath));
        }

        private static void WriteAllBytes(string fullPath, byte[] content)
        {
            File.WriteAllBytes(ToLongPath(fullPath), content);
        }

        private static void CreateDirectoryOnDisk(string fullPath)
        {
            Directory.CreateDirectory(ToLongPath(fullPath));
        }

        private static string ToLongPath(string fullPath)
        {
#if UNITY_EDITOR_WIN
            if (string.IsNullOrWhiteSpace(fullPath)) return fullPath;
            string normalized = Path.GetFullPath(fullPath);
            if (normalized.StartsWith(@"\\?\", StringComparison.Ordinal)) return normalized;
            if (normalized.StartsWith(@"\\", StringComparison.Ordinal))
                return @"\\?\UNC\" + normalized.Substring(2);
            return @"\\?\" + normalized;
#else
            return fullPath;
#endif
        }

        private static string FromLongPath(string fullPath)
        {
#if UNITY_EDITOR_WIN
            if (string.IsNullOrWhiteSpace(fullPath)) return fullPath;
            if (fullPath.StartsWith(@"\\?\UNC\", StringComparison.Ordinal))
                return @"\\" + fullPath.Substring(8);
            if (fullPath.StartsWith(@"\\?\", StringComparison.Ordinal))
                return fullPath.Substring(4);
#endif
            return fullPath;
        }

        private static string GenerateUnityGuid()
        {
            return Guid.NewGuid().ToString("N");
        }

        private static string EscapeJson(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private sealed class FileOperation
        {
            public FileOperation(string destinationFullPath, byte[] contentBytes)
            {
                DestinationFullPath = destinationFullPath;
                ContentBytes = contentBytes;
            }

            public string DestinationFullPath { get; }
            public byte[] ContentBytes { get; set; }
        }
    }

    [InitializeOnLoad]
    internal static class IdleAutoDefenseGeneratedSceneOpenQueue
    {
        private const string PendingScenePathKey = "Deucarian.IdleAutoDefenseTemplate.PendingScenePath";
        private const string PendingTargetRootKey = "Deucarian.IdleAutoDefenseTemplate.PendingTargetRoot";
        private const string PendingBootstrapTypeKey = "Deucarian.IdleAutoDefenseTemplate.PendingBootstrapType";
        private const string PendingAttemptCountKey = "Deucarian.IdleAutoDefenseTemplate.PendingAttemptCount";
        private const int MaximumOpenAttempts = 300;
        private const string ContentPackAssetRelativePath = "/Content/ContentPacks/contentpack.template.basic-idle-auto-defense/contentpack.template.basic-idle-auto-defense_ContentPack.asset";
        private const string ContentSetAssetRelativePath = "/Content/ContentSets/contentset.template.basic-idle-auto-defense/contentset.template.basic-idle-auto-defense_GameContentSet.asset";
        private static bool _queued;

        static IdleAutoDefenseGeneratedSceneOpenQueue()
        {
            QueuePendingOpen();
        }

        internal static void Queue(string sceneAssetPath, string targetRootAssetPath, string bootstrapTypeFullName)
        {
            if (string.IsNullOrWhiteSpace(sceneAssetPath) ||
                string.IsNullOrWhiteSpace(targetRootAssetPath) ||
                string.IsNullOrWhiteSpace(bootstrapTypeFullName))
            {
                return;
            }

            SessionState.SetString(PendingScenePathKey, sceneAssetPath);
            SessionState.SetString(PendingTargetRootKey, targetRootAssetPath);
            SessionState.SetString(PendingBootstrapTypeKey, bootstrapTypeFullName);
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

            string targetRootAssetPath = SessionState.GetString(PendingTargetRootKey, string.Empty);
            var scene = EditorSceneManager.OpenScene(sceneAssetPath);
            if (ReapplyGeneratedSceneContent(sceneAssetPath, targetRootAssetPath, bootstrapTypeFullName))
                EditorSceneManager.SaveScene(scene);
            Clear();
        }

        private static bool ReapplyGeneratedSceneContent(string sceneAssetPath, string targetRootAssetPath, string bootstrapTypeFullName)
        {
            GameContentPackAsset contentPack = AssetDatabase.LoadAssetAtPath<GameContentPackAsset>(targetRootAssetPath + ContentPackAssetRelativePath);
            GameContentSetAsset contentSet = AssetDatabase.LoadAssetAtPath<GameContentSetAsset>(targetRootAssetPath + ContentSetAssetRelativePath);
            if (contentPack == null || contentSet == null) return false;

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
                behaviourChanged |= AssignObjectReference(serialized, "_contentPack", contentPack);
                behaviourChanged |= AssignObjectReference(serialized, "_contentSet", contentSet);
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
            SessionState.EraseString(PendingTargetRootKey);
            SessionState.EraseString(PendingBootstrapTypeKey);
            SessionState.EraseInt(PendingAttemptCountKey);
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

            _gameNamespace = EditorGUILayout.TextField("Namespace", _gameNamespace);
            _gamePrefix = EditorGUILayout.TextField("Game prefix", _gamePrefix);
            _openScene = EditorGUILayout.Toggle("Open created scene", _openScene);
            _allowOverwrite = EditorGUILayout.Toggle("Allow overwrite", _allowOverwrite);

            EditorGUILayout.HelpBox(
                "Creates a playable product-owned scene with authored content, prefabs, visuals, and a bootstrap script. Deucarian package source stays in packages.",
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
