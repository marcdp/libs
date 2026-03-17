
using DProjects.Utils;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Fs.Extensions {


    public static class FilesystemLoadBinaryFile {


        //methods
        public static byte[] LoadBinaryFile(this IFilesystemSync fs, string path) {
            using (var stream = fs.LoadReadStream(path, new())) {
                return StreamUtils.ReadBytes(stream);
            }
        }
        public async static Task<byte[]> LoadBinaryFileAsync(this IFilesystemAsync fs, string path, LoadReadStreamSettings settings, CancellationToken cancellationToken = default) {
            using (var stream = await fs.LoadReadStreamAsync(path, settings, cancellationToken)) {
                return await StreamUtils.ReadBytesAsync(stream, cancellationToken);
            }
        }


    }


}