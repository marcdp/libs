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

        //ungzip
        public static byte[] UnGzip(byte[] data) {
            using (var gzipStream = new GZipStream(new MemoryStream(data), CompressionMode.Decompress)) {
                return StreamUtils.ReadBytes(gzipStream);
            }
        }

    }


}


