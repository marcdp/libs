
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Fs;


namespace DProjects.Identity.Membership {

    [Protocol("fs", "")]
    [ProtocolExample("fs:///path/to/dir", "")]
    public class MembershipFsDirFactory(IFilesystem filesystem) : IFactoryByUrl<IMembership> {
        public IMembership Create(string src) {
            var url = new System.Uri(src);
            return new MembershipFsDir(filesystem, url.AbsolutePath);
        }

    }

}
