using System.IO;
using System.IO.Compression;


namespace DProjects.Utils {


    public static class GzipUtils {


        //gzip
        public static byte[] Gzip(string data) {
            return Gzip(System.Text.Encoding.UTF8.GetBytes(data));
        }
        public static byte[] Gzip(byte[] data) {
            using (var output = new MemoryStream()) {
                using (var gzipStream = new GZipStream(output, CompressionMode.Compress)) {
                    gzipStream.Write(data, 0, data.Length);
                }
                return output.ToArray();
            }
        }
        public static void GzipFile(string fileName, string gzFileName) {
            using (var fileStream = System.IO.File.OpenRead(fileName))
            using (var gzFileStream = System.IO.File.OpenWrite(gzFileName)) {
                var gzStream = new System.IO.Compression.GZipStream(gzFileStream, System.IO.Compression.CompressionMode.Compress);
                StreamUtils.Copy(fileStream, gzStream);
                gzStream.Dispose();
            }
        }

        //ungzip
        public static byte[] UnGzip(byte[] data) {
            using (var gzipStream = new GZipStream(new MemoryStream(data), CompressionMode.Decompress)) {
                return StreamUtils.ReadBytes(gzipStream);
            }
        }
        public static void UnGzipFile(string gzFileName, string fileName) {
            using (var gzFileStream = System.IO.File.OpenRead(gzFileName))
            using (var fileStream = System.IO.File.OpenWrite(fileName)) {
                var gzStream = new System.IO.Compression.GZipStream(gzFileStream, System.IO.Compression.CompressionMode.Decompress);
                StreamUtils.Copy(gzStream, fileStream);
                gzStream.Dispose();
            }
        }

    }


}


