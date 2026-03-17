namespace DProjects.Fs {


    public interface IFilesystemMounter : IFilesystem {

        //properties
        //bool PrivateMounts { get; }

        //methods
        void Mount(string path, IFilesystem filesystem, bool takeOwnership, string prefix = "");
        bool Unmount(string path);
        MountPoint? GetMountPoint(ref string path);
        bool IsMountPoint(string path);
        string? GetNativeMountPath(string path);
        MountPoint[] GetMountPoints();

    }


}

