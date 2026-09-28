namespace DProjects.XShell.Services {

    internal static class ModuleFileCompilerUrl {

        // methods
        public static string Normalize(string relativePath, string url) {
            // leave non-local and already module-root-relative references untouched
            if (string.IsNullOrEmpty(url) || url[0] == '#' || url[0] == '/' || HasExplicitScheme(url)) return url;

            // separate suffixes that are not part of logical path traversal
            var queryIndex = url.IndexOf('?');
            var fragmentIndex = url.IndexOf('#');
            var suffixIndex = queryIndex < 0 ? fragmentIndex : fragmentIndex < 0 ? queryIndex : Math.Min(queryIndex, fragmentIndex);
            var path = suffixIndex < 0 ? url : url[..suffixIndex];
            var suffix = suffixIndex < 0 ? "" : url[suffixIndex..];
            if (path.Length == 0) return url;

            // resolve against the logical source directory using URL separators
            var lastSlash = relativePath.LastIndexOf('/');
            var directory = lastSlash <= 0 ? "" : relativePath[1..lastSlash];
            var segments = new List<string>();
            if (directory.Length > 0) segments.AddRange(directory.Split('/', StringSplitOptions.RemoveEmptyEntries));
            foreach (var segment in path.Split('/')) {
                if (segment.Length == 0 || segment == ".") continue;
                if (segment == "..") {
                    if (segments.Count == 0) {
                        throw new InvalidOperationException($"Resource reference '{url}' in '{relativePath}' escapes the module root.");
                    }
                    segments.RemoveAt(segments.Count - 1);
                } else {
                    segments.Add(segment);
                }
            }
            return "/" + string.Join('/', segments) + suffix;
        }

        // methods (private)
        private static bool HasExplicitScheme(string url) {
            // recognize the URI scheme grammar without treating drive-like physical paths specially
            if (!IsAsciiLetter(url[0])) return false;
            for (var index = 1; index < url.Length; index++) {
                var character = url[index];
                if (character == ':') return true;
                if (!IsAsciiLetter(character) && !char.IsDigit(character) && character != '+' && character != '-' && character != '.') return false;
            }
            return false;
        }
        private static bool IsAsciiLetter(char character) {
            return character is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
        }
    }
}
