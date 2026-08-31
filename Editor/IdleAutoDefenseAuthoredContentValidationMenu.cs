using System.Globalization;
using System.Text;
using UnityEditor;

namespace Deucarian.TemplateGameIdleAutoDefense.Editor
{
    internal static class IdleAutoDefenseAuthoredContentValidationMenu
    {
        public static void ValidateAuthoredContent()
        {
            string report = BuildReport();
            EditorUtility.DisplayDialog("Idle Auto Defense Authored Content", report, "OK");
        }

        public static string BuildReport()
        {
            string[] guids = AssetDatabase.FindAssets("t:GameContentSetAsset");
            var builder = new StringBuilder();
            int contentSetCount = 0;
            int playerExperienceCount = 0;
            int errorCount = 0;
            int warningCount = 0;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                GameContentSetAsset contentSet = AssetDatabase.LoadAssetAtPath<GameContentSetAsset>(path);
                if (contentSet == null) continue;

                contentSetCount++;
                GameContentSetValidationReport validation = GameContentSetValidator.Validate(contentSet);
                errorCount += validation.ErrorCount;
                warningCount += validation.WarningCount;
                builder
                    .Append(validation.IsValid ? "PASS " : "FAIL ")
                    .Append(contentSet.Id)
                    .Append(" (")
                    .Append(path)
                    .Append(") errors=")
                    .Append(validation.ErrorCount.ToString(CultureInfo.InvariantCulture))
                    .Append(" warnings=")
                    .Append(validation.WarningCount.ToString(CultureInfo.InvariantCulture))
                    .AppendLine();

                for (int issueIndex = 0; issueIndex < validation.Issues.Count; issueIndex++)
                {
                    GameContentSetValidationIssue issue = validation.Issues[issueIndex];
                    builder
                        .Append("  ")
                        .Append(issue.Severity)
                        .Append(": ")
                        .Append(issue.Path)
                        .Append(" - ")
                        .Append(issue.Message)
                        .AppendLine();
                }
            }

            if (contentSetCount == 0)
                builder.AppendLine("No GameContentSetAsset instances were found in the project.");

            string[] playerExperienceGuids = AssetDatabase.FindAssets("t:IdleAutoDefensePlayerExperienceAsset");
            for (int i = 0; i < playerExperienceGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(playerExperienceGuids[i]);
                IdleAutoDefensePlayerExperienceAsset experience = AssetDatabase.LoadAssetAtPath<IdleAutoDefensePlayerExperienceAsset>(path);
                if (experience == null) continue;
                playerExperienceCount++;
                var issues = experience.Validate();
                errorCount += issues.Count;
                builder.Append(issues.Count == 0 ? "PASS " : "FAIL ")
                    .Append(experience.Id)
                    .Append(" (")
                    .Append(path)
                    .Append(") errors=")
                    .Append(issues.Count.ToString(CultureInfo.InvariantCulture))
                    .AppendLine();
                for (int issueIndex = 0; issueIndex < issues.Count; issueIndex++)
                    builder.Append("  Error: Presentation - ").Append(issues[issueIndex]).AppendLine();
            }

            builder.Insert(
                0,
                "Idle Auto Defense authored content validation: "
                + (errorCount == 0 ? "PASS" : "FAIL")
                + " | sets=" + contentSetCount.ToString(CultureInfo.InvariantCulture)
                + " | player-experiences=" + playerExperienceCount.ToString(CultureInfo.InvariantCulture)
                + " | errors=" + errorCount.ToString(CultureInfo.InvariantCulture)
                + " | warnings=" + warningCount.ToString(CultureInfo.InvariantCulture)
                + "\n\n");
            return builder.ToString();
        }
    }
}
