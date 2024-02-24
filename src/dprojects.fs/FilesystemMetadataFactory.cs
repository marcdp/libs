using DProjects.Factories;
using DProjects.Factories.Attributes;

namespace DProjects.Fs {

    [Protocol("metadata", "")]
    [ProtocolUsage("metadata:FSURL")]
    [ProtocolExample("metadata:file:///path/to/folder", "")]
    public class FilesystemMetadataFactory(IFactoryByUrl<IFilesystem> fsFactory) : IFactoryByUrl<IFilesystem> {

        public IFilesystem Create(string src) {
            var subSrc = src.Substring(src.IndexOf(":") + 1);
            var filesystem = fsFactory.Create(subSrc);
            return new FilesystemMetadata(filesystem);
        }

    }

}
