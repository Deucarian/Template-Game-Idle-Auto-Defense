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
    public static partial class IdleAutoDefenseTemplateSetupService
    {


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
    }
}
