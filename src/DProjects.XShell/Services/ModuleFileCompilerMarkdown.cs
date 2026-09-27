namespace DProjects.XShell.Services {

    public class ModuleFileCompilerMarkdown {

        // methods
        public ModuleFileCompiler.FileContent Compile(ModuleFileCompilerContext context, string markdown) {
            // TODO ...
            return new ModuleFileCompiler.FileContent { ContentType = "text/markdown", Content = markdown };
        }
    }
}
