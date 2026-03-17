
namespace DProjects.Fs.Extensions {

    public static class EntryToJson {

        //methods
        public static string ToJson(this Entry entry, string? path = null, bool writePath = true, bool writeName = false, bool noEndElement = false) {
            return EntryDTO.FromEntry(entry).ToJson(path, writePath, writeName, noEndElement);
        }
         
    }

}