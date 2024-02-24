using DProjects.Utils;
using System.Threading.Tasks;

namespace DProjects.Fs.Extensions {


    public static class FilesystemGetLastFileEntry {


        //methods
        public static Entry? GetLastFileEntry(this IFilesystemSync fs, string path, string pattern) {
            Entry? lastEntry = null;
            foreach (var entry in fs.GetEntries(path, GetModes.Files)) {
                if (StringUtils.Like(entry.Name, pattern)) {
                    if (lastEntry == null || PathUtils.CompareName(lastEntry.Name, entry.Name) < 0) {
                        lastEntry = entry;
                    }

                }
            }
            return lastEntry;
        }
        public static async Task<Entry?> GetLastFileEntryAsync(this IFilesystemAsync fs, string path, string pattern) {
            Entry? lastEntry = null;
            await foreach (var entry in fs.GetEntriesAsync(path, GetModes.Files)) {
                if (StringUtils.Like(entry.Name, pattern)) {
                    if (lastEntry == null || PathUtils.CompareName(lastEntry.Name, entry.Name) < 0) {
                        lastEntry = entry;
                    }

                }
            }
            return lastEntry;
        }


    }


}