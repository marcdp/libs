
using DProjects.Utils;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DProjects.Fs.Extensions {


    public static class FilesystemLoadTextFile {


        //methods
        public static string LoadTextFile(this IFilesystemSync fs, string path, Encoding? encoding = null) {
            var buffer = fs.LoadBinaryFile(path);
            if (encoding == null)  return EncodingUtils.GetBufferAsString(buffer);
            return encoding.GetString(buffer);
        }
        public static async Task<string> LoadTextFileAsync(this IFilesystemAsync fs, string path, Encoding? encoding = null) {
            var buffer = await fs.LoadBinaryFileAsync(path);
            if (encoding == null) return EncodingUtils.GetBufferAsString(buffer);
            return encoding.GetString(buffer);
        }


    }


}