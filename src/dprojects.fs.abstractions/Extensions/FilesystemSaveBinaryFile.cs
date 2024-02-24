
using System.IO;
using System.Threading.Tasks;

namespace DProjects.Fs.Extensions {


    public static class FilesystemSaveBinaryFile {


        //methods
        public static Entry SaveBinaryFile(this IFilesystemSync fs, string path, byte[] bytes) {
            using (var memoryStream = new MemoryStream(bytes)) {
                return fs.SaveFile(path, memoryStream);
            }
        }
        public static async Task<Entry> SaveBinaryFileAsync(this IFilesystemAsync fs, string path, byte[] bytes) {
            using (var memoryStream = new MemoryStream(bytes)) {
                return await fs.SaveFileAsync(path, memoryStream);
            }
        }


    }


}