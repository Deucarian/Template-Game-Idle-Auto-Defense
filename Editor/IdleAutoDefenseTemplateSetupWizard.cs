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

    public static class IdleAutoDefenseTemplateSetupService
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

        public static IdleAutoDefenseTemplateSetupResult CreateGameFromTemplate(IdleAutoDefenseTemplateSetupRequest request)
        {
            request ??= new IdleAutoDefenseTemplateSetupRequest();
            var result = new IdleAutoDefenseTemplateSetupResult();
            try
            {
                string targetRoot = NormalizeAssetPath(request.TargetRootAssetPath);
                string contentRoot = NormalizeAssetPath(request.ContentRootAssetPath);
                if (string.IsNullOrEmpty(contentRoot))
                    contentRoot = "Assets/GameContent/IdleAutoDefense";
                string gameNamespace = NormalizeNamespace(request.GameNamespace);
                string prefix = ToIdentifierPrefix(request.GamePrefix);
                if (string.IsNullOrEmpty(prefix)) prefix = LastNamespacePart(gameNamespace);
                IReadOnlyList<PackTemplate> selectedPacks = GetSelectedPackTemplates(request.PackSelection);

                result.TargetRootAssetPath = targetRoot;
                result.ContentRootAssetPath = contentRoot;
                string targetFullRoot = AssetPathToFullPath(targetRoot);
                string contentFullRoot = AssetPathToFullPath(contentRoot);
                if (!targetRoot.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) ||
                    !IsPathInsideDirectory(targetFullRoot, Application.dataPath))
                {
                    throw new ArgumentException("Target root must be a product folder under Assets.");
                }

                if (!IsSameOrChildAssetPath(contentRoot, "Assets/GameContent") ||
                    !IsPathInsideDirectory(contentFullRoot, AssetPathToFullPath("Assets/GameContent")))
                {
                    throw new ArgumentException("Content root must be Assets/GameContent or a folder below Assets/GameContent.");
                }

                if (!IsValidNamespace(gameNamespace))
                    throw new ArgumentException("Game namespace must be a valid C# namespace.");

                string basicSourceRoot = FindTemplateSourceRoot(BasicTemplateSourceFolderName);
                string className = prefix + "IdleAutoDefenseGameBootstrap";
                string saveClassName = prefix + "IdleAutoDefenseSave";
                string reportAssetPath = targetRoot + "/Docs/setup-report.md";
                result.SetupReportAssetPath = reportAssetPath;

                string sourceScriptMeta = Path.Combine(basicSourceRoot, "Scripts", SampleScriptName + ".meta");
                string sourceScriptGuid = TryReadGuid(sourceScriptMeta);
                string generatedScriptMetaPath = Path.Combine(targetFullRoot, "Scripts", className + ".cs.meta");
                string generatedScriptGuid = request.RepairMissingContent
                    ? TryReadGuid(generatedScriptMetaPath)
                    : string.Empty;
                if (string.IsNullOrWhiteSpace(generatedScriptGuid)) generatedScriptGuid = GenerateUnityGuid();
                var guidMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(sourceScriptGuid))
                    guidMap[sourceScriptGuid] = generatedScriptGuid;

                var operations = new List<FileOperation>();
                var generatedPacks = new List<GeneratedPack>();
                for (int i = 0; i < selectedPacks.Count; i++)
                {
                    PackTemplate pack = selectedPacks[i];
                    string sourceRoot = FindTemplateSourceRoot(pack.SourceFolderName);
                    bool usePackSubfolders = request.PackSelection == IdleAutoDefenseTemplatePackSelection.Both;
                    string packTargetRoot = usePackSubfolders
                        ? targetRoot + "/" + pack.OutputFolderName
                        : targetRoot;
                    string packContentRoot = usePackSubfolders
                        ? contentRoot + "/" + pack.OutputFolderName
                        : contentRoot;
                    string packTargetFullRoot = AssetPathToFullPath(packTargetRoot);
                    string packContentFullRoot = AssetPathToFullPath(packContentRoot);
                    string sceneAssetPath = pack.VisibleSceneRootAssetPath + "/" + pack.SceneFileName;

                    AddDirectoryCopyOperations(operations, Path.Combine(sourceRoot, "Content"), packContentFullRoot, true, guidMap);
                    AddDirectoryCopyOperations(operations, Path.Combine(sourceRoot, "Prefabs"), Path.Combine(packTargetFullRoot, "Prefabs"), true, guidMap);
                    AddDirectoryCopyOperations(operations, Path.Combine(sourceRoot, "Visuals"), Path.Combine(packTargetFullRoot, "Visuals"), true, guidMap);
                    AddDirectoryCopyOperations(operations, Path.Combine(sourceRoot, "Audio"), Path.Combine(packTargetFullRoot, "Audio"), true, guidMap);
                    AddDirectoryCopyOperations(operations, Path.Combine(sourceRoot, "Resources"), Path.Combine(packTargetFullRoot, "Resources"), true, guidMap);
                    AddTextOperation(
                        operations,
                        Path.Combine(packTargetFullRoot, "README.md"),
                        TransformReadme(
                            ReadAllText(Path.Combine(sourceRoot, "README.md")),
                            ResolveGeneratedReadmeTitle(request, pack),
                            gameNamespace,
                            className,
                            packContentRoot));

                    string sceneText = ReadAllText(Path.Combine(sourceRoot, "Scenes", pack.SceneFileName));
                    if (!string.IsNullOrEmpty(sourceScriptGuid))
                        sceneText = sceneText.Replace(sourceScriptGuid, generatedScriptGuid);
                    AddTextOperation(operations, AssetPathToFullPath(sceneAssetPath), sceneText);
                    result.AddCreatedScene(sceneAssetPath);
                    generatedPacks.Add(new GeneratedPack(pack, packContentRoot, sceneAssetPath));
                }

                result.CreatedSceneAssetPath = generatedPacks[0].SceneAssetPath;
                string packageRoot = Directory.GetParent(Directory.GetParent(basicSourceRoot).FullName).FullName;
                string thirdPartyNoticesPath = Path.Combine(packageRoot, "ThirdPartyNotices.md");
                if (FileExists(thirdPartyNoticesPath))
                {
                    AddTextOperation(
                        operations,
                        Path.Combine(targetFullRoot, "Docs", "ThirdPartyNotices.md"),
                        ReadAllText(thirdPartyNoticesPath));
                }
                AddTextOperation(
                    operations,
                    Path.Combine(targetFullRoot, gameNamespace + ".asmdef"),
                    CreateAsmdef(gameNamespace));
                AddTextOperation(
                    operations,
                    Path.Combine(targetFullRoot, "Scripts", className + ".cs"),
                    TransformScript(ReadAllText(Path.Combine(basicSourceRoot, "Scripts", SampleScriptName)), gameNamespace, className, saveClassName, prefix));
                AddTextOperation(
                    operations,
                    Path.Combine(targetFullRoot, "Scripts", className + ".cs.meta"),
                    CreateMonoScriptMeta(generatedScriptGuid));
                AddTextOperation(
                    operations,
                    Path.Combine(targetFullRoot, "Docs", "asset-flip-checklist.md"),
                    CreateAssetFlipChecklist(
                        request.PackSelection == IdleAutoDefenseTemplatePackSelection.BasicOnly
                            ? request.GamePrefix
                            : string.Join(" + ", selectedPacks.Select(value => value.DisplayName)),
                        gameNamespace,
                        contentRoot));
                AddTextOperation(
                    operations,
                    Path.Combine(targetFullRoot, "Docs", "setup-report.md"),
                    CreateSetupReport(targetRoot, contentRoot, gameNamespace, prefix, generatedPacks));
                RewriteGuidReferences(operations, guidMap);

                for (int i = 0; i < operations.Count; i++)
                {
                    string assetPath = FullPathToAssetPath(operations[i].DestinationFullPath);
                    if (!FileExists(operations[i].DestinationFullPath)) continue;
                    if (request.RepairMissingContent && BytesEqual(ReadAllBytes(operations[i].DestinationFullPath), operations[i].ContentBytes))
                    {
                        operations[i].SkipWrite = true;
                        continue;
                    }

                    if (!request.AllowOverwrite) result.AddBlockedFile(assetPath);
                }

                if (result.BlockedFiles.Count > 0)
                {
                    result.Status = IdleAutoDefenseTemplateSetupStatus.BlockedByExistingFiles;
                    return result;
                }

                CreateDirectory(targetFullRoot, result);
                CreateDirectory(contentFullRoot, result);
                CreateDirectory(Path.Combine(targetFullRoot, "Scripts"), result);
                CreateDirectory(Path.Combine(targetFullRoot, "Docs"), result);
                for (int i = 0; i < selectedPacks.Count; i++)
                    CreateDirectory(AssetPathToFullPath(selectedPacks[i].VisibleSceneRootAssetPath), result);

                for (int i = 0; i < operations.Count; i++)
                {
                    FileOperation operation = operations[i];
                    if (operation.SkipWrite) continue;
                    string directory = Path.GetDirectoryName(operation.DestinationFullPath);
                    if (!string.IsNullOrEmpty(directory)) CreateDirectory(directory, result);
                    WriteAllBytes(operation.DestinationFullPath, operation.ContentBytes);
                    result.AddCreatedFile(FullPathToAssetPath(operation.DestinationFullPath));
                }

                result.Status = IdleAutoDefenseTemplateSetupStatus.Succeeded;
                result.AddMessage("Created product-owned Idle Auto Defense content for " +
                    string.Join(" and ", selectedPacks.Select(value => value.DisplayName)) + ".");
                string generatedBootstrapTypeName = gameNamespace + "." + className;
                if (request.RefreshAssetDatabase) AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                GeneratedPack launchPack = generatedPacks[0];
                if (request.OpenCreatedScene && !request.RefreshAssetDatabase)
                    EditorSceneManager.OpenScene(launchPack.SceneAssetPath);
                else if (request.OpenCreatedScene && request.RefreshAssetDatabase)
                    IdleAutoDefenseGeneratedSceneOpenQueue.Queue(
                        launchPack.SceneAssetPath,
                        launchPack.ContentRootAssetPath,
                        generatedBootstrapTypeName,
                        launchPack.Template.ContentPackAssetRelativePath,
                        launchPack.Template.ContentSetAssetRelativePath,
                        launchPack.Template.PlayerExperienceAssetRelativePath);
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

        private static IReadOnlyList<PackTemplate> GetSelectedPackTemplates(
            IdleAutoDefenseTemplatePackSelection selection)
        {
            var basic = new PackTemplate(
                BasicTemplateSourceFolderName,
                "Basic",
                "Basic Idle Auto Defense",
                BasicSampleSceneName,
                BasicVisibleSceneRootAssetPath,
                "/ContentPacks/contentpack.idle-auto-defense.playable/contentpack.idle-auto-defense.playable_ContentPack.asset",
                "/ContentSets/contentset.idle-auto-defense.playable/contentset.idle-auto-defense.playable_GameContentSet.asset",
                "/Presentation/player-experience.idle-auto-defense.playable.asset");
            var scrap = new PackTemplate(
                ScrapTemplateSourceFolderName,
                "ScrapFrontier",
                "Scrap Frontier",
                ScrapSampleSceneName,
                ScrapVisibleSceneRootAssetPath,
                "/ContentPacks/contentpack.idle-auto-defense.scrap-frontier/contentpack.idle-auto-defense.scrap-frontier_ContentPack.asset",
                "/ContentSets/contentset.idle-auto-defense.scrap-frontier.playable/contentset.idle-auto-defense.scrap-frontier.playable_GameContentSet.asset",
                "/Presentation/player-experience.idle-auto-defense.scrap-frontier.playable.asset");
            switch (selection)
            {
                case IdleAutoDefenseTemplatePackSelection.ScrapFrontierOnly:
                    return new[] { scrap };
                case IdleAutoDefenseTemplatePackSelection.Both:
                    return new[] { basic, scrap };
                default:
                    return new[] { basic };
            }
        }

        private static string FindTemplateSourceRoot(string sourceFolderName)
        {
            PackageInfo packageInfo = PackageInfo.FindForAssembly(typeof(IdleAutoDefenseTemplateController).Assembly);
            if (packageInfo == null || string.IsNullOrWhiteSpace(packageInfo.resolvedPath))
                throw new InvalidOperationException("Could not find installed Idle Auto Defense template package.");
            string sourceRoot = Path.Combine(packageInfo.resolvedPath, "TemplateSource~", sourceFolderName);
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
                string destinationFullPath = Path.Combine(destinationDirectory, relative);
                byte[] contentBytes = ReadAllBytes(files[i]);
                if (files[i].EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    contentBytes = RemapMetaGuid(files[i], destinationFullPath, contentBytes, guidMap);
                AddFileOperation(operations, destinationFullPath, contentBytes);
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

        private static byte[] RemapMetaGuid(
            string sourceMetaPath,
            string destinationMetaPath,
            byte[] contentBytes,
            Dictionary<string, string> guidMap)
        {
            string oldGuid = TryReadGuid(sourceMetaPath);
            if (string.IsNullOrWhiteSpace(oldGuid)) return contentBytes;

            string newGuid = TryReadGuid(destinationMetaPath);
            if (string.IsNullOrWhiteSpace(newGuid)) newGuid = GenerateUnityGuid();
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

        private static string TransformReadme(string source, string displayName, string gameNamespace, string className, string contentRoot)
        {
            return source
                .Replace("# Basic Idle Auto Defense Template Source", "# " + displayName)
                .Replace("# Basic Idle Auto Defense Game", "# " + displayName)
                .Replace("# Scrap Frontier Template Source", "# " + displayName)
                .Replace(
                    "This folder is private package template source. Unity Package Manager should not present it as an importable sample.",
                    "This folder is product-owned output created by the Idle Auto Defense setup wizard.")
                .Replace("BasicIdleAutoDefenseGameBootstrap.cs", className + ".cs")
                .Replace("Deucarian.TemplateGameIdleAutoDefense.Samples", gameNamespace)
                .Replace("`Assets/GameContent/IdleAutoDefense` or the setup wizard's chosen content root", "`" + contentRoot + "`");
        }

        private static string ResolveGeneratedReadmeTitle(
            IdleAutoDefenseTemplateSetupRequest request,
            PackTemplate pack)
        {
            if (request.PackSelection == IdleAutoDefenseTemplatePackSelection.BasicOnly &&
                string.Equals(pack.SourceFolderName, BasicTemplateSourceFolderName, StringComparison.Ordinal))
                return request.GamePrefix + " Idle Auto Defense";
            return pack.DisplayName;
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

        private static string CreateAssetFlipChecklist(string displayName, string gameNamespace, string contentRoot)
        {
            return "# Asset Flip Checklist\n\n" +
                   "Game: " + displayName + "\n\n" +
                   "Namespace: `" + gameNamespace + "`\n\n" +
                   "Authored content root: `" + contentRoot + "`\n\n" +
                   "## Replace First\n\n" +
                   "- Enemies\n" +
                   "- Weapons\n" +
                   "- Projectiles\n" +
                   "- Stages\n" +
                   "- Spawn profiles\n" +
                   "- Run upgrades\n" +
                   "- Progression values\n" +
                   "- UI themes and presentation tokens\n" +
                   "- Tutorial copy\n" +
                   "- Audio event clips\n" +
                   "- Monetization placements\n" +
                   "- Save/profile names\n\n" +
                   "The generated folder is product-owned. Do not copy package `Runtime` or `Editor` source into it.\n\n" +
                   "## Workflow\n\n" +
                   "1. Replace enemy visuals in `Prefabs/Enemies` or update the bootstrap prefab providers.\n" +
                   "2. Replace weapon and projectile visuals in `Prefabs/Weapons` and `Prefabs/Projectiles`.\n" +
                   "3. Tune the enemy assets in `" + contentRoot + "/Enemies`.\n" +
                   "4. Tune the attack and tower assets in `" + contentRoot + "/Attacks` and `" + contentRoot + "/Weapons`.\n" +
                   "5. Tune the spawn profiles and upgrades in `" + contentRoot + "/Waves` and `" + contentRoot + "/Upgrades`.\n" +
                   "6. Replace themes, tutorial copy, UI settings, and audio clips under `" + contentRoot + "/Presentation`.\n" +
                   "7. Tune live rewards, economy, run timing, progression, offline settings, and game rules in their authored folders under `" + contentRoot + "`.\n" +
                   "8. Validate the named pack; the generated scene uses strict authored startup and must report fallback false.\n" +
                   "9. Test the main menu, complete run, offline claim, summary, and phone-like landscape layout.\n" +
                   "10. Rename template IDs into your product namespace as content becomes product-owned.\n" +
                   "11. Keep Deucarian package source out of this folder.\n";
        }

        private static string CreateSetupReport(
            string targetRoot,
            string contentRoot,
            string gameNamespace,
            string prefix,
            IReadOnlyList<GeneratedPack> generatedPacks)
        {
            return "# Idle Auto Defense Setup Report\n\n" +
                   "- Target root: `" + targetRoot + "`\n" +
                   "- Authored content root: `" + contentRoot + "`\n" +
                   "- Namespace: `" + gameNamespace + "`\n" +
                   "- Prefix: `" + prefix + "`\n" +
                   "- Packs: " + string.Join(", ", generatedPacks.Select(value => value.Template.DisplayName)) + "\n" +
                   "- Scenes: `" + string.Join("`, `", generatedPacks.Select(value => value.SceneAssetPath)) + "`\n" +
                   "- Authored core: reward catalog, economy, run profile, progression, offline progression, and game rules copied with remapped references.\n" +
                   "- Startup: strict authored binding; missing or invalid required content blocks gameplay instead of using fallback.\n" +
                   "- Dependencies: kept in Deucarian packages; generated assembly references `Deucarian.TemplateGameIdleAutoDefense`.\n\n" +
                   "## Next Steps\n\n" +
                   "1. Open the created scene and press Play.\n" +
                   "2. Open `Tools > Deucarian > Game Content Authoring` and tune assets under `" + contentRoot + "`.\n" +
                   "3. Replace starter visuals.\n" +
                   "4. Rename product content IDs.\n" +
                   "5. Tune enemies, attacks, towers, waves, rewards, economy, run profile, progression, offline settings, and game rules.\n" +
                   "6. Confirm `UsingAuthoredCore` is true and `FallbackModeActive` is false in Play Mode.\n" +
                   "7. Keep reusable framework code in Deucarian packages.\n";
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

        private static bool IsSameOrChildAssetPath(string assetPath, string rootAssetPath)
        {
            string normalized = NormalizeAssetPath(assetPath);
            string root = NormalizeAssetPath(rootAssetPath);
            return string.Equals(normalized, root, StringComparison.OrdinalIgnoreCase) ||
                normalized.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase);
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

        private static bool BytesEqual(byte[] left, byte[] right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null || right == null || left.Length != right.Length) return false;
            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i]) return false;
            }

            return true;
        }

        private sealed class PackTemplate
        {
            public PackTemplate(
                string sourceFolderName,
                string outputFolderName,
                string displayName,
                string sceneFileName,
                string visibleSceneRootAssetPath,
                string contentPackAssetRelativePath,
                string contentSetAssetRelativePath,
                string playerExperienceAssetRelativePath)
            {
                SourceFolderName = sourceFolderName;
                OutputFolderName = outputFolderName;
                DisplayName = displayName;
                SceneFileName = sceneFileName;
                VisibleSceneRootAssetPath = visibleSceneRootAssetPath;
                ContentPackAssetRelativePath = contentPackAssetRelativePath;
                ContentSetAssetRelativePath = contentSetAssetRelativePath;
                PlayerExperienceAssetRelativePath = playerExperienceAssetRelativePath;
            }

            public string SourceFolderName { get; }
            public string OutputFolderName { get; }
            public string DisplayName { get; }
            public string SceneFileName { get; }
            public string VisibleSceneRootAssetPath { get; }
            public string ContentPackAssetRelativePath { get; }
            public string ContentSetAssetRelativePath { get; }
            public string PlayerExperienceAssetRelativePath { get; }
        }

        private sealed class GeneratedPack
        {
            public GeneratedPack(PackTemplate template, string contentRootAssetPath, string sceneAssetPath)
            {
                Template = template;
                ContentRootAssetPath = contentRootAssetPath;
                SceneAssetPath = sceneAssetPath;
            }

            public PackTemplate Template { get; }
            public string ContentRootAssetPath { get; }
            public string SceneAssetPath { get; }
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
            public bool SkipWrite { get; set; }
        }
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
