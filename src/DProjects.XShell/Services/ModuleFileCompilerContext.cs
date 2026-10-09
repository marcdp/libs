namespace DProjects.XShell.Services {

    internal sealed class ModuleFileCompilerContext {

        // props
        public ModuleFileCompiler.Config Config { get; }
        public string ModuleId { get; }
        public string ModulePath { get; }
        public string FilePath { get; }
        public string RelativePath { get; }

        // ctor
        public ModuleFileCompilerContext(ModuleFileCompiler.Config config, string moduleId, string modulePath, string filePath) {
            ArgumentNullException.ThrowIfNull(config);
            if (string.IsNullOrWhiteSpace(moduleId)) throw new ArgumentException("Module ID is required.", nameof(moduleId));
            if (string.IsNullOrWhiteSpace(modulePath)) throw new ArgumentException("Module path is required.", nameof(modulePath));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.", nameof(filePath));

            // normalize physical paths
            var normalizedModulePath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(modulePath));
            var normalizedFilePath = Path.GetFullPath(filePath);
            var relativePath = Path.GetRelativePath(normalizedModulePath, normalizedFilePath);

            // require a resource located below the module root
            if (relativePath == "." || Path.IsPathRooted(relativePath) || relativePath == ".." || relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
                relativePath.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal)) {
                throw new ArgumentException("File path must be inside the module path.", nameof(filePath));
            }

            Config = config;
            ModuleId = moduleId;
            ModulePath = normalizedModulePath;
            FilePath = normalizedFilePath;
            RelativePath = "/" + relativePath.Replace('\\', '/');
        }
    }
}
