using DProjects.Utils;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Fs.Extensions {


    public static class FilesystemCreateDirectoryRecursive {


        //methods
        public static void CreateDirectoryRecursive(this IFilesystemSync fs, string path) {
            if (path.Length > 1 && !fs.ExistsDirectory(path)) {
                fs.CreateDirectoryRecursive(PathUtils.GetPathParent(path));
                fs.CreateDirectory(path);
            }
        }
        public async static Task CreateDirectoryRecursiveAsync(this IFilesystemAsync fs, string path, CancellationToken cancellationToken) {
            if (path.Length > 1 && ! await fs.ExistsDirectoryAsync(path, cancellationToken)) {
                await fs.CreateDirectoryRecursiveAsync(PathUtils.GetPathParent(path), cancellationToken);
                await fs.CreateDirectoryAsync(path, cancellationToken);
            }
        }


    }


}