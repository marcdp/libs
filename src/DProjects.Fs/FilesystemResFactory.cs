
using DProjects.Factories;
using DProjects.Factories.Attributes;
using System;
using System.Linq;

namespace DProjects.Fs {

    [Protocol("res", "")]
    [ProtocolUsage("res://ASSEMBLY_NAME/PREFIX")]
    [ProtocolExample("res://MyAssembly", "")]
    [ProtocolExample("res://MyAssembly/Prefix", "")]
    public class FilesystemResFactory : IFactoryByUrl<IFilesystem> {

        public IFilesystem Create(string src) {
            var url = new Uri(src);
            var assemblyName = url.Host;
            var assembly = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(assembly => assembly.GetName().Name.Equals(assemblyName, StringComparison.OrdinalIgnoreCase));
            var path = url.AbsolutePath;
            return new FilesystemRes(assembly, path);
        }

    }
    

}
