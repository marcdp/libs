
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Fs;


namespace DProjects.Identity.Store {

    [Protocol("fs", "")]
    [ProtocolExample("fs:///path/to/dir", "")]
    public class PrincipalStoreFsDirFactory(IFilesystem filesystem) : IFactoryByUrl<IPrincipalStore> {
        public IPrincipalStore Create(string src) {
            var url = new System.Uri(src);
            return new PrincipalStoreFsDir(filesystem, url.AbsolutePath);
        }

    }

}
