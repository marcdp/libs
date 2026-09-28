using System.Text;

namespace DProjects.XShell.Services {

    public class ModuleFileCompilerHtml {

        // methods
        public ModuleFileCompiler.FileContent Compile(ModuleFileCompilerContext context, string html) {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(html);

            // todo ...
            // replace relative urls in html with absolute urls based on context.ModulePath and context.RelativePath

            // return ...
            return new ModuleFileCompiler.FileContent { ContentType = "text/html", Content = html };
        }

    }
}