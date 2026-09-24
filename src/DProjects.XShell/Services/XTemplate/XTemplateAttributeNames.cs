using System.Text;

namespace DProjects.XShell.Services.XTemplate {

    internal static class XTemplateAttributeNames {

        // methods
        public static bool IsValid(string name) => name.Length > 0 && name.EnumerateRunes().All(IsValidRune);

        // methods (private)
        private static bool IsValidRune(Rune rune) {
            var value = rune.Value;
            return !Rune.IsWhiteSpace(rune) && value is not '"' and not '\'' and not '<' and not '>' and not '=' and not '/' and not '\0' && value > 0x1F && value != 0x7F && !IsUnicodeNoncharacter(value);
        }
        private static bool IsUnicodeNoncharacter(int value) => value is >= 0xFDD0 and <= 0xFDEF || (value & 0xFFFF) is 0xFFFE or 0xFFFF;
    }
}
