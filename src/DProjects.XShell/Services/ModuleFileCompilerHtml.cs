namespace DProjects.XShell.Services {

    public class ModuleFileCompilerHtml {

        // methods
        public ModuleFileCompiler.FileContent Compile(ModuleFileCompilerContext context, string html) {
            // TODO ...
            return new ModuleFileCompiler.FileContent { ContentType = "text/html", Content = html };
        }
    }
}
