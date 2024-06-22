
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace DProjects.Fs.Extensions {


    public static class FilesystemAppendFile {


        //methods
        public static Entry AppendFile(this IFilesystemSync fs, string path, Stream stream) {
            return fs.SaveFile(path, stream, new SaveFileSettings() { Append = true });
        }
        public static Entry AppendFile(this IFilesystemSync fs, string path, string text, Encoding encoding) {
            using (var memoryStream = new MemoryStream(encoding.GetBytes(text))) {
                return fs.AppendFile(path, memoryStream);
            }
        }
        public static Entry AppendFile(this IFilesystemSync fs, string path, byte[] buffer) {
            return fs.AppendFile(path, new MemoryStream(buffer));
        }
        public static async Task<Entry> AppendFileAsync(this IFilesystemAsync fs, string path, Stream stream, CancellationToken cancellationToken) {
            return await fs.SaveFileAsync(path, stream, new SaveFileSettings() { Append = true }, cancellationToken);
        }
        public static async Task<Entry> AppendFileAsync(this IFilesystemAsync fs, string path, string text, Encoding encoding, CancellationToken cancellationToken) {
            using (var memoryStream = new MemoryStream(encoding.GetBytes(text))) {
                return await fs.AppendFileAsync(path, memoryStream, cancellationToken);
            }
        }
        public static async Task<Entry> AppendFileAsync(this IFilesystemAsync fs, string path, byte[] buffer, CancellationToken cancellationToken) {
            return await fs.AppendFileAsync(path, new MemoryStream(buffer), cancellationToken);
        }


    }


}