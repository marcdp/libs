
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;

namespace DProjects.Fs {

    [Protocol("os", "")]
    [ProtocolUsage("os:")]
    [ProtocolExample("os:", "")]
    public class FilesystemOsFactory : IFactoryByUrl<IFilesystem> {

        public IFilesystem Create(string src) {
            if (EnvironmentUtils.IsWindows()) {
                var fsMounter = new FilesystemMounter();
                fsMounter.Mount("/", new FilesystemMem(true, false), true);
                foreach (var drive in System.IO.DriveInfo.GetDrives()) {
                    var letter = drive.Name.Replace(":", "").Replace("\\", "").ToLower();
                    fsMounter.Mount("/" + letter, new FilesystemLocalFactory().Create("file:///" + letter + "/"), true);
                }
                return fsMounter;
            } else {
                return new FilesystemLocalFactory().Create("file:///");   
            }
        }

    }
    

}
