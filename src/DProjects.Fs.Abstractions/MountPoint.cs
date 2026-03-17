namespace DProjects.Fs {


    public class MountPoint {

        //constructor
        public MountPoint(string path, IFilesystem filesystem, string prefix, bool owned) {
            this.Path = path;
            this.Filesystem = filesystem;
            this.Prefix = prefix;
            this.Owned = owned;
            //this.Url = filesystem.Url;
        }

        //properties
        public string Path { get; }
        public IFilesystem Filesystem { get; }
        public string Prefix { get; }
        public bool Owned  { get; }
        //public string Url { get; }

    }

}