using Deucarian.Attacks.Authoring;
using Deucarian.DefenseGames;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    // Preserves first-error diagnostics at strict template conversion boundaries.
    internal static class IdleAutoDefenseContentValidation
    {
        internal static string GetFirstValidationError(AttackRecipeValidationReport report)
        {
            for (int i = 0; i < report.Issues.Count; i++)
                if (report.Issues[i].IsError)
                    return report.Issues[i].Path + ": " + report.Issues[i].Message;
            return "Unknown validation error.";
        }

        internal static string GetFirstValidationError(ContentAuthoringValidationReport report)
        {
            for (int i = 0; i < report.Issues.Count; i++)
                if (report.Issues[i].IsError)
                    return report.Issues[i].Path + ": " + report.Issues[i].Message;
            return "Unknown validation error.";
        }
    }
}
