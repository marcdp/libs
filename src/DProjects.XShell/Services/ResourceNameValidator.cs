namespace DProjects.XShell.Services {

    internal static class ResourceNameValidator {

        // methods
        public static void Validate(string value) {
            // require one safe filesystem directory name below the bundled Resources directory
            if (string.IsNullOrWhiteSpace(value) || value is "." or ".." || value.IndexOfAny(['/', '\\']) >= 0 ||
                value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) {
                throw new ArgumentException($"Resources name '{value}' must be a single valid directory name.");
            }
        }

    }

}
