
using System.Text;
using System.Threading.Tasks;

namespace DProjects.Fs.Extensions {


    public static class FilesystemLoadFirstTextLine {


        //methods
        public static string LoadFirstTextLine(this IFilesystemSync fs, string path, Encoding encoding, int maxLength = 1024) {
            using (var stream = new System.IO.StreamReader(fs.LoadReadStream(path, new LoadReadStreamSettings() { Offset = 0, Length = maxLength }), encoding)) {
                return stream.ReadLine();
            }
        }
        public static async Task<string> LoadFirstTextLineAsync(this IFilesystemAsync fs, string path, Encoding encoding, int maxLength = 1024) {
            using (var stream = new System.IO.StreamReader(await fs.LoadReadStreamAsync(path, new LoadReadStreamSettings() { Offset = 0, Length = maxLength }), encoding)) {
                return await stream.ReadLineAsync();
            }
        }


    }


}