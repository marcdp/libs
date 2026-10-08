namespace DProjects.XShell.Services {

    internal static class ModuleFileCompilerResourceUrl {

        // methods
        public static string Normalize(string relativePath, string url) {
            // physical configuration URLs are not valid resource references
            if (!string.IsNullOrEmpty(url) && url.StartsWith("url:", StringComparison.OrdinalIgnoreCase)) {
                throw new InvalidOperationException($"The 'url:' scheme is not supported in CSS or template resource references in '{relativePath}'.");
            }
            if (!string.IsNullOrEmpty(url) && url.StartsWith("app:", StringComparison.OrdinalIgnoreCase)) {
                throw new InvalidOperationException($"The 'app:' scheme is not supported in CSS or template resource references in '{relativePath}'.");
            }
            // leave non-local references untouched while checking module-root paths for traversal
            if (string.IsNullOrEmpty(url) || url[0] == '#' || url.StartsWith("//", StringComparison.Ordinal) || HasExplicitScheme(url)) return url;

            // separate suffixes that are not part of logical path traversal
            var queryIndex = url.IndexOf('?');
            var fragmentIndex = url.IndexOf('#');
            var suffixIndex = queryIndex < 0 ? fragmentIndex : fragmentIndex < 0 ? queryIndex : Math.Min(queryIndex, fragmentIndex);
            var path = suffixIndex < 0 ? url : url[..suffixIndex];
            var suffix = suffixIndex < 0 ? "" : url[suffixIndex..];
            if (path.Length == 0) return url;

            // resolve against the logical source directory using URL separators
            var lastSlash = relativePath.LastIndexOf('/');
            var directory = path.StartsWith('/') || lastSlash <= 0 ? "" : relativePath[1..lastSlash];
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
                if (!IsAsciiLetter(character) && character is not (>= '0' and <= '9') && character != '+' && character != '-' && character != '.') return false;
            }
            return false;
        }
        private static bool IsAsciiLetter(char character) {
            return character is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
        }
    }
}
