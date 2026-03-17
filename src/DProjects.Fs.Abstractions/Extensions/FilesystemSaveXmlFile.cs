
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace DProjects.Fs.Extensions {


    public static class FilesystemSaveXmlFile {


        //methods
        public static Entry SaveXmlFile(this IFilesystemSync fs, string path, XmlDocument xmlDocument, XmlWriterSettings? settings = null) {
            return fs.SaveBinaryFile(path, XmlDocumentToBuffer(xmlDocument, settings));
        }
        public static async Task<Entry> SaveXmlFileAsync(this IFilesystemAsync fs, string path, XmlDocument xmlDocument, XmlWriterSettings? settings = null, CancellationToken cancellationToken = default) {
            return await fs.SaveBinaryFileAsync(path, XmlDocumentToBuffer(xmlDocument, settings), cancellationToken);
        }

        private static byte[] XmlDocumentToBuffer(XmlDocument xmlDocument, XmlWriterSettings? settings) {
            var sb = new StringBuilder();
            if (settings==null) settings = new XmlWriterSettings {
                Encoding = new System.Text.UTF8Encoding(false),
                OmitXmlDeclaration = true,
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace
            };
            using (XmlWriter writer = XmlWriter.Create(sb, settings)) {
                xmlDocument.Save(writer);
            }
            return settings.Encoding.GetBytes(sb.ToString());
        }

    }


}