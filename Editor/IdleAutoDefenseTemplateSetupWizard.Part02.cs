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
                   "2. Open `Tools > Deucarian > Authoring > Game Content...` and tune assets under `" + contentRoot + "`.\n" +
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
}
