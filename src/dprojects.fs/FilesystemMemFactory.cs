using DProjects.Factories;
using DProjects.Factories.Attributes;

namespace DProjects.Fs {

    [Protocol("mem", "")]
    [ProtocolUsage("mem:")]
    [ProtocolExample("mem:", "")]
    public class FilesystemMemFactory : IFactoryByUrl<IFilesystem> {

        public IFilesystem Create(string src) {
            return new FilesystemMem(false, false);
        }

    }

}
