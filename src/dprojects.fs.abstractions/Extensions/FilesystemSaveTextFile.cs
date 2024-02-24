
using System.IO;
using System.Threading.Tasks;

namespace DProjects.Fs.Extensions {


    public static class FilesystemSaveTextFile {


        //methods
        public static Entry SaveTextFile(this IFilesystemSync fs, string path, string text, System.Text.Encoding encoding) {
            using (var memoryStream = new MemoryStream(encoding.GetBytes(text))) {
                return fs.SaveFile(path, memoryStream);
            }
        }
        public static async Task<Entry> SaveTextFileAsync(this IFilesystemAsync fs, string path, string text, System.Text.Encoding encoding) {
            using (var memoryStream = new MemoryStream(encoding.GetBytes(text))) {
                return await fs.SaveFileAsync(path, memoryStream);
            }
        }


    }


}