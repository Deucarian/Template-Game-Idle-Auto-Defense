using System;
using static Deucarian.TemplateGameIdleAutoDefense.BasicIdleAutoDefenseGame;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    // Preserves the template operation and mount identity normalization contract.
    internal static class IdleAutoDefenseContentIdentity
    {
        internal static string SanitizeContentSetOperationSegment(string value)
        {
            return SanitizeRuntimeSegment(value);
        }

        internal static string SanitizeRuntimeSegment(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "unnamed";
            var chars = new char[value.Length];
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                chars[i] = char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '_' ? c : '-';
            }

            return new string(chars).Trim('-', '.', '_');
        }
    }
}
