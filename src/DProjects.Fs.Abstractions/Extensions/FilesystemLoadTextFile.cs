
using DProjects.Utils;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Fs.Extensions {


    public static class FilesystemLoadTextFile {


        //methods
        public static string LoadTextFile(this IFilesystemSync fs, string path, Encoding? encoding = null) {
            var buffer = fs.LoadBinaryFile(path);
            if (encoding == null) encoding = EncodingUtils.DetectEncoding(buffer);
            return encoding.GetString(buffer);
        }
        public static async Task<string> LoadTextFileAsync(this IFilesystemAsync fs, string path, Encoding? encoding = null, CancellationToken cancellationToken = default) {
            var buffer = await fs.LoadBinaryFileAsync(path, new(), cancellationToken);
            if (encoding == null) encoding = EncodingUtils.DetectEncoding(buffer);
            return encoding.GetString(buffer);
        }


    }


}