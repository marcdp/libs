namespace DProjects.Utils {


    public static class MimeTypeUtils {

        //variables
        public class MimeType {
            public readonly string Name;
            public readonly string[] Extensions;
            public readonly byte[] MagicNumber;
            public readonly bool Compressible;
            public MimeType(string name) {
                Name = name;
                Extensions = [];
                MagicNumber = [];
                Compressible = false;
            }
            public MimeType(string name, string[] extensions, byte[]? magicNumber = null, bool compressible = false) {
                Name = name;
                Extensions = extensions;
                MagicNumber = (magicNumber == null ? [] : magicNumber);
                Compressible = compressible;
            }
        }
        public static readonly MimeType[] MimeTypes = new MimeType[] {
            new MimeType("application/exe", new string[] {".exe"}, new byte[] { 0x4D, 0x5A}),
            new MimeType("application/gzip", new string[] {".gzip"}),
            new MimeType("application/java-archive", new string[] {".jar"}),
            new MimeType("application/javascript", new string[] {".js"}, compressible: true),
            new MimeType("application/json", new string[] {".json"}, compressible: true),
            new MimeType("application/mac-binhex40", new string[] {".hqx"}),
            new MimeType("application/manifest+json", new string[] {".webmanifest"}),
            new MimeType("application/mathematica", new string[] {".nb"}),
            new MimeType("application/msaccess", new string[] {".mdb"}),
            new MimeType("application/msword", new string[] {".doc", ".dot"}),
            new MimeType("application/octet-stream", new string[] {".bak", ".bin", ".dll", ".com", ".bat"}),
            new MimeType("application/oda", new string[] {".oda"}),
            new MimeType("application/ogg", new string[] {".ogg"}),
            new MimeType("application/pdf", new string[] {".pdf"}, new byte[] { 0x25, 0x50, 0x44, 0x46 }),
            new MimeType("application/pgp-keys", new string[] {".key"}),
            new MimeType("application/pgp-signature", new string[] {".pgp"}),
            new MimeType("application/pics-rules", new string[] {".prf"}),
            new MimeType("application/postscript", new string[] {".ps",".ai",".eps"}, new byte[] { 0x25, 0x21}),
            new MimeType("application/rss+xml", new string[] {".rss"}),
            new MimeType("application/rtf", new string[] {".rtf"}),
            new MimeType("application/shortcut", new string[] {".url", ".lnk"}),
            new MimeType("application/smil", new string[] {".smil", ".smi"}),
            new MimeType("application/vnd.cinderella", new string[] {".cdy"}),
            new MimeType("application/vnd.dish.dpkg", new string[] {".dpkg"}),
            new MimeType("application/vnd.mif", new string[] {".mif"}),
            new MimeType("application/vnd.mozilla.xul+xml", new string[] {".xul"}),
            new MimeType("application/vnd.ms-excel", new string[] {".xls", ".xlb", ".xlt"}),
            new MimeType("application/vnd.ms-excel.addin.macroEnabled.12", new string[] {".xlam"}),
            new MimeType("application/vnd.ms-excel.sheet.binary.macroEnabled.12", new string[] {".xlsb"}),
            new MimeType("application/vnd.ms-excel.sheet.macroEnabled.12", new string[] {".xlsm"}),
            new MimeType("application/vnd.ms-excel.template.macroEnabled.12", new string[] {".xltm"}),
            new MimeType("application/vnd.ms-pki.seccat", new string[] {".cat"}),
            new MimeType("application/vnd.ms-pki.stl", new string[] {".stl"}),
            new MimeType("application/vnd.ms-powerpoint", new string[] {".ppt", ".pps"}),
            new MimeType("application/vnd.ms-powerpoint.addin.macroEnabled.12", new string[] {".ppam"}),
            new MimeType("application/vnd.ms-powerpoint.presentation.macroEnabled.12", new string[] {".pptm"}),
            new MimeType("application/vnd.ms-powerpoint.slideshow.macroEnabled.12", new string[] {".ppsm"}),
            new MimeType("application/vnd.ms-powerpoint.template.macroEnabled.12", new string[] {".potm"}),
            new MimeType("application/vnd.ms-word.document.macroEnabled.12", new string[] {".docm"}),
            new MimeType("application/vnd.ms-word.template.macroEnabled.12", new string[] {".dotm"}),
            new MimeType("application/vnd.openxmlformats-officedocument.presentationml.presentation", new string[] {".pptx"}),
            new MimeType("application/vnd.openxmlformats-officedocument.presentationml.slideshow", new string[] {".ppsx"}),
            new MimeType("application/vnd.openxmlformats-officedocument.presentationml.template", new string[] {".potx"}),
            new MimeType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", new string[] {".xlsx"}),
            new MimeType("application/vnd.openxmlformats-officedocument.spreadsheetml.template", new string[] {".xltx"}),
            new MimeType("application/vnd.openxmlformats-officedocument.wordprocessingml.document", new string[] {".docx"}),
            new MimeType("application/vnd.openxmlformats-officedocument.wordprocessingml.template", new string[] {".dotx"}),
            new MimeType("application/vnd.smaf", new string[] {".mmf"}),
            new MimeType("application/vnd.sun.xml.calc", new string[] {".sxc"}),
            new MimeType("application/vnd.sun.xml.calc.template", new string[] {".stc"}),
            new MimeType("application/vnd.sun.xml.draw", new string[] {".sxd"}),
            new MimeType("application/vnd.sun.xml.draw.template", new string[] {".std"}),
            new MimeType("application/vnd.sun.xml.impress", new string[] {".sxi"}),
            new MimeType("application/vnd.sun.xml.math", new string[] {".sxm"}),
            new MimeType("application/vnd.sun.xml.writer", new string[] {".sxw"}),
            new MimeType("application/vnd.sun.xml.writer.global", new string[] {".sxg"}),
            new MimeType("application/vnd.sun.xml.writer.template", new string[] {".stw"}),
            new MimeType("application/vnd.symbian.install", new string[] {".sis"}),
            new MimeType("application/vnd.wap.wbxml", new string[] {".wbxml"}),
            new MimeType("application/vnd.wap.wmlc", new string[] {".wmlc"}),
            new MimeType("application/vnd.wap.wmlscriptc", new string[] {".wmlsc"}),
            new MimeType("application/warc", new string[] {".warc"}),
            new MimeType("application/wordperfect5.1", new string[] {".wp5"}),
            new MimeType("application/x-123", new string[] {".wk"}),
            new MimeType("application/x-apple-diskimage", new string[] {".dmg"}),
            new MimeType("application/x-bcpio", new string[] {".bcpio"}),
            new MimeType("application/x-bittorrent", new string[] {".torrent"}),
            new MimeType("application/x-cdf", new string[] {".cdf"}),
            new MimeType("application/x-cdlink", new string[] {".vcd"}),
            new MimeType("application/x-chess-pgn", new string[] {".pgn"}),
            new MimeType("application/x-cpio", new string[] {".cpio"}),
            new MimeType("application/x-csh", new string[] {".csh"}),
            new MimeType("application/x-debian-package", new string[] {".deb"}),
            new MimeType("application/x-director", new string[] {".dxr", ".dcr", ".dir"}),
            new MimeType("application/x-dms", new string[] {".dms"}),
            new MimeType("application/x-doom", new string[] {".wad"}),
            new MimeType("application/x-dvi", new string[] {".dvi"}),
            new MimeType("application/x-font", new string[] {".pfa", ".pfb", ".gsf", ".pcf"}),
            new MimeType("application/x-futuresplash", new string[] {".spl"}),
            new MimeType("application/x-gnumeric", new string[] {".gnumeric"}),
            new MimeType("application/x-go-sgf", new string[] {".sgf"}),
            new MimeType("application/x-graphing-calculator", new string[] {".gcf"}),
            new MimeType("application/x-gtar", new string[] {".tgz", ".taz", ".gtar"}),
            new MimeType("application/x-gzip", new string[] {".gz"}),
            new MimeType("application/x-hdf", new string[] {".hdf"}),
            new MimeType("application/xhtml+xml", new string[] {".xht"}),
            new MimeType("application/x-httpd-php", new string[] {".php", ".phtml", ".pht"}),
            new MimeType("application/x-httpd-php3", new string[] {".php3"}),
            new MimeType("application/x-httpd-php3-preprocessed", new string[] {".php3p"}),
            new MimeType("application/x-httpd-php4", new string[] {".php4"}),
            new MimeType("application/x-httpd-php-source", new string[] {".phps"}),
            new MimeType("application/x-ica", new string[] {".ica"}),
            new MimeType("application/x-internet-signup", new string[] {".ins", ".isp"}),
            new MimeType("application/x-iphone", new string[] {".iii"}),
            new MimeType("application/x-java-jnlp-file", new string[] {".jnlp"}),
            new MimeType("application/x-java-serialized-object", new string[] {".ser"}),
            new MimeType("application/x-java-vm", new string[] {".class"}),
            new MimeType("application/x-kchart", new string[] {".chrt"}),
            new MimeType("application/x-killustrator", new string[] {".kil"}),
            new MimeType("application/x-kpresenter", new string[] {".kpr", ".kpt"}),
            new MimeType("application/x-kword", new string[] {".kwd",".kwt"}),
            new MimeType("application/x-latex", new string[] {".latex"}),
            new MimeType("application/x-lha", new string[] {".lha"}),
            new MimeType("application/x-lzh", new string[] {".lzh"}),
            new MimeType("application/x-lzx", new string[] {".lzx"}),
            new MimeType("application/x-makeself", new string[] {".run"}),
            new MimeType("application/x-mif", new string[] {".mif"}),
            new MimeType("application/xml-dtd", new string[] {".dtd"}),
            new MimeType("application/x-ms-application", new string[] {".application", ".manifest"}),
            new MimeType("application/x-msi", new string[] {".msi"}),
            new MimeType("application/x-ms-wmd", new string[] {".wmd"}),
            new MimeType("application/x-ms-wmz", new string[] {".wmz"}),
            new MimeType("application/x-netcdf", new string[] {".nc"}),
            new MimeType("application/x-ns-proxy-autoconfig", new string[] {".pac"}),
            new MimeType("application/x-nwc", new string[] {".nwc"}),
            new MimeType("application/x-object", new string[] {".o"}),
            new MimeType("application/x-oz-application", new string[] {".oza"}),
            new MimeType("application/x-pkcs7-certreqresp", new string[] {".p7r"}),
            new MimeType("application/x-pkcs7-crl", new string[] {".crl"}),
            new MimeType("application/x-quicktimeplayer", new string[] {".qtl"}),
            new MimeType("application/x-rar", new string[] {".rar"}),
            new MimeType("application/x-redhat-package-manager", new string[] {".rpm"}),
            new MimeType("application/x-sh", new string[] {".sh"}, new byte[] { 0x23, 0x21 } ),
            new MimeType("application/x-shar", new string[] {".shar"}),
            new MimeType("application/x-shockwave-flash", new string[] {".swf", ".swfl"}),
            new MimeType("application/x-sqlar", new string[] {".sqlar"}, compressible: false),
            new MimeType("application/x-sqlite3", new string[] {".sqlite"}, compressible: true),
            new MimeType("application/x-stuffit", new string[] {".sit"}),
            new MimeType("application/x-sv4cpio", new string[] {".sv4cpio"}),
            new MimeType("application/x-sv4crc", new string[] {".sv4crc"}),
            new MimeType("application/x-tar", new string[] {".tar"}),
            new MimeType("application/x-tcl", new string[] {".tcl"}),
            new MimeType("application/x-tex-gf", new string[] {".gf"}),
            new MimeType("application/x-tex-pk", new string[] {".pk"}),
            new MimeType("application/x-troff", new string[] {".tr", ".roff", ".t"}),
            new MimeType("application/x-troff-man", new string[] {".man"}),
            new MimeType("application/x-troff-me", new string[] {".me"}),
            new MimeType("application/x-troff-ms", new string[] {".ms"}),
            new MimeType("application/x-ustar", new string[] {".ustar"}),
            new MimeType("application/x-wais-source", new string[] {".src"}),
            new MimeType("application/x-wingz", new string[] {".wz"}),
            new MimeType("application/x-x509-ca-cert", new string[] {".crt"}),
            new MimeType("application/x-xfig", new string[] {".fig"}),
            new MimeType("application/zip", new string[] {".zip"}, new byte[] { 0x50, 0x4B }),
            new MimeType("audio/basic", new string[] {".au", ".snd"}),
            new MimeType("audio/midi", new string[] {".mid", ".midi"}, new byte[] { 0x4D, 0x54, 0x68, 0x64 }),
            new MimeType("audio/mpeg", new string[] {".mpga", ".mpega", ".mp2", ".mp3", ".m4a"}),
            new MimeType("audio/ogg", new string[] {".oga"}),
            new MimeType("audio/prs.sid", new string[] {".sid"}),
            new MimeType("audio/x-aiff", new string[] {".aif", ".aiff", ".aifc"}),
            new MimeType("audio/x-gsm", new string[] {".gsm"}),
            new MimeType("audio/x-mpegurl", new string[] {".m3u"}),
            new MimeType("audio/x-ms-wax", new string[] {".wax"}),
            new MimeType("audio/x-ms-wma", new string[] {".wma"}),
            new MimeType("audio/x-pn-realaudio", new string[] {".ra", ".rm", ".ram"}),
            new MimeType("audio/x-realaudio", new string[] {".ra"}),
            new MimeType("audio/x-scpls", new string[] {".pls"}),
            new MimeType("audio/x-sd2", new string[] {".sd2"}),
            new MimeType("audio/x-wav", new string[] {".wav"}),
            new MimeType("chemical/x-pdb", new string[] {".pdb"}),
            new MimeType("chemical/x-xyz", new string[] {".xyz"}),
            new MimeType("font/eot", new string[] {".eot"}),
            new MimeType("font/otf", new string[] {".otf"}),
            new MimeType("font/ttf", new string[] {".ttf"}),
            new MimeType("font/woff", new string[] {".woff"}),
            new MimeType("font/woff2", new string[] {".woff2"}),
            new MimeType("image/bmp", new string[] {".bmp"}, compressible: true),
            new MimeType("image/gif", new string[] {".gif"}, new byte[] { 0x47, 0x49, 0x46, 0x38 }),
            new MimeType("image/jpeg", new string[] {".jpg", ".jpeg", ".jpe"}, new byte[] { 0xFF, 0xD8 }),
            new MimeType("image/pcx", new string[] {".pcx"}),
            new MimeType("image/png", new string[] {".png"}, new byte[] { 0x89, 0x50, 0x4E, 0x47 }),
            new MimeType("image/webp", new string[] {".webp"}, new byte[] { 0x52, 0x49, 0x46, 0x46 }),
            new MimeType("image/svg+xml", new string[] {".svg", ".svgz"}, compressible: true),
            new MimeType("image/tiff", new string[] {".tiff", ".tif"}),
            new MimeType("image/vnd.wap.wbmp", new string[] {".wbmp"}),
            new MimeType("image/x-cmu-raster", new string[] {".ras"}),
            new MimeType("image/x-coreldraw", new string[] {".cdr"}),
            new MimeType("image/x-coreldrawpattern", new string[] {".pat"}),
            new MimeType("image/x-coreldrawtemplate", new string[] {".cdt"}),
            new MimeType("image/x-corelphotopaint", new string[] {".cpt"}),
            new MimeType("image/x-djvu", new string[] {".djv", ".djvu"}),
            new MimeType("image/x-icon", new string[] {".ico"}, compressible: true),
            new MimeType("image/x-jg", new string[] {".art"}),
            new MimeType("image/x-jng", new string[] {".jng"}),
            new MimeType("image/x-photoshop", new string[] {".psd"}),
            new MimeType("image/x-portable-anymap", new string[] {".pnm"}),
            new MimeType("image/x-portable-bitmap", new string[] {".pbm"}),
            new MimeType("image/x-portable-graymap", new string[] {".pgm"}),
            new MimeType("image/x-portable-pixmap", new string[] {".ppm"}),
            new MimeType("image/x-rgb", new string[] {".rgb"}),
            new MimeType("image/x-xbitmap", new string[] {".xbm"}),
            new MimeType("image/x-xpixmap", new string[] {".xpm"}),
            new MimeType("image/x-xwindowdump", new string[] {".xwd"}),
            new MimeType("model/iges", new string[] {".iges", ".igs"}),
            new MimeType("model/mesh", new string[] {".mesh", ".msh", ".silo"}),
            new MimeType("text/asp", new string[] {".asp"}, compressible: true),
            new MimeType("text/comma-separated-values", new string[] {".csv"}, compressible: true),
            new MimeType("text/config", new string[] {".config"}, compressible: true),
            new MimeType("text/x-vb", new string[] {".vb"}, compressible: true),
            new MimeType("text/css", new string[] {".css"}, compressible: true),
            new MimeType("text/csv", new string[] {".csv"}, compressible: true),
            new MimeType("text/h323", new string[] {".323"}, compressible: true),
            new MimeType("text/html", new string[] {".html", ".htm", ".shtml"}, compressible: true),
            new MimeType("text/iuls", new string[] {".uls"}, compressible: true),
            new MimeType("text/markdown", new string[] {".md"}, compressible: true),
            new MimeType("text/mathml", new string[] {".mml"}, compressible: true),
            new MimeType("text/plain", new string[] {".txt", ".log", ".asc", ".pot", ".diff", ".text"}, compressible: true),
            new MimeType("text/richtext", new string[] {".rtx"}, compressible: true),
            new MimeType("text/rtf", new string[] {".rtf"}, compressible: true),
            new MimeType("text/scriptlet", new string[] {".wsc", ".sct"}, compressible: true),
            new MimeType("text/tab-separated-values", new string[] {".tsv"}, compressible: true),
            new MimeType("text/texmacs", new string[] {".ts", ".tm"}, compressible: true),
            new MimeType("text/vnd.sun.j2me.app-descriptor", new string[] {".jad"}, compressible: true),
            new MimeType("text/vnd.wap.wml", new string[] {".wml"}, compressible: true),
            new MimeType("text/vnd.wap.wmlscript", new string[] {".wmls"}, compressible: true),
            new MimeType("text/x-csharp", new string[] {".cs"}, compressible: true),
            new MimeType("text/x-aspx", new string[] {".aspx"}, compressible: true),
            new MimeType("text/x-ashx", new string[] {".ashx"}, compressible: true),
            new MimeType("text/x-asax", new string[] {".asax"}, compressible: true),
            new MimeType("text/x-chdr", new string[] {".h"}, compressible: true),
            new MimeType("text/x-csh", new string[] {".csh"}, compressible: true),
            new MimeType("text/x-csrc", new string[] {".c"}, compressible: true),
            new MimeType("text/x-htaccess", new string[] {".htaccess"}, compressible: true),
            new MimeType("text/xhtml", new string[] {".xhtml"}, compressible: true),
            new MimeType("text/x-java", new string[] {".java"}, compressible: true),
            new MimeType("text/xml", new string[] {".xml", ".xsl"}, compressible: true),
            new MimeType("text/x-moc", new string[] {".moc"}, compressible: true),
            new MimeType("text/x-pascal", new string[] {".pas", ".p"}, compressible: true),
            new MimeType("text/x-pcs-gcd", new string[] {".gcd"}, compressible: true),
            new MimeType("text/x-perl", new string[] {".pl", ".pm"}, compressible: true),
            new MimeType("text/x-python", new string[] {".py"}, compressible: true),
            new MimeType("text/x-setext", new string[] {".etx"}, compressible: true),
            new MimeType("text/x-sh", new string[] {".sh"}, compressible: true),
            new MimeType("text/x-tcl", new string[] {".tcl", ".tk"}, compressible: true),
            new MimeType("text/x-tex", new string[] {".tex", ".ltx", ".sty", ".cls"}, compressible: true),
            new MimeType("text/x-vcalendar", new string[] {".vcs"}, compressible: true),
            new MimeType("text/x-vcard", new string[] {".vcf"}, compressible: true),
            new MimeType("text/yaml", new string[] {".yml"}, compressible: true),
            new MimeType("video/dl", new string[] {".dl"}),
            new MimeType("video/fli", new string[] {".fli"}),
            new MimeType("video/gl", new string[] {".gl"}),
            new MimeType("video/mp4", new string[] {".mp4"}),
            new MimeType("video/mpeg", new string[] {".mpeg", ".mpg", ".mpe"}),
            new MimeType("video/ogg", new string[] {".ogv"}),
            new MimeType("video/quicktime", new string[] {".mov", ".qt"}),
            new MimeType("video/vnd.mpegurl", new string[] {".mxu"}),
            new MimeType("video/webm", new string[] {".webm"}),
            new MimeType("video/x-dv", new string[] {".dv"}),
            new MimeType("video/x-flv", new string[] {".flv"}),
            new MimeType("video/x-la-asf", new string[] {".lsf"}),
            new MimeType("video/x-mng", new string[] {".mng"}),
            new MimeType("video/x-ms-asf", new string[] {".asf", ".asx"}),
            new MimeType("video/x-msvideo", new string[] {".avi"}),
            new MimeType("video/x-ms-wm", new string[] {".wm"}),
            new MimeType("video/x-ms-wmv", new string[] {".wmv"}),
            new MimeType("video/x-ms-wmx", new string[] {".wmx"}),
            new MimeType("video/x-ms-wvx", new string[] {".wvx"}),
            new MimeType("video/x-sgi-movie", new string[] {".movie"}),
            new MimeType("x-conference/x-cooltalk", new string[] {".ice"}),
            new MimeType("x-world/x-vrml", new string[] {".vrml", ".vrm}" })
        };


        //constants
        public const string TEXT_PLAIN = "text/plain";
        public const string TEXT_HTML = "text/html";
        public const string TEXT_XML = "text/xml";
        public const string TEXT_CSV = "text/csv";
        public const string TEXT_CSS = "text/css";
        public const string TEXT_MARKDOWN = "text/markdown";
        public const string IMAGE_PNG = "image/png";
        public const string IMAGE_GIF = "image/gif";
        public const string IMAGE_JPG = "image/jpg";
        public const string FONT_WOFF = "font/woff";
        public const string FONT_WOFF2 = "font/woff2";
        public const string FONT_OTF = "font/otf";
        public const string FONT_TTF = "font/ttf";
        public const string FONT_EOT = "font/eot";
        public const string APPLICATION_PDF = "application/pdf";
        public const string APPLICATION_JAVASCRIPT = "application/javascript";
        public const string APPLICATION_JSON = "application/json";
        public const string APPLICATION_JSON_RPC = "application/json-rpc";
        public const string APPLICATION_XML = "application/xml";
        public const string APPLICATION_OCTET_STREAM = "application/octet-stream";
        public const string APPLICATION_X_WWW_FORM_URLENCODED = "application/x-www-form-urlencoded";
        public const string APPLICATION_X_ZIP_COMPRESSED = "application/x-zip-compressed";
        public const string APPLICATION_X_SHELL_SCRIPT = "application/x-sh";
        public const string MULTIPART_MIXED = "multipart/mixed";
        public const string MULTIPART_ALTERNATIVE = "multipart/alternative";


        //methods
        public static bool IsCompressible(string mimeType) {
            foreach (var aMimeType in MimeTypes) {
                if (aMimeType.Name.Equals(mimeType)) {
                    return aMimeType.Compressible;
                }
            }
            return false;
        }
        public static bool IsText(string mimeType) {
            return mimeType.StartsWith("text/") || mimeType.Equals("application/javascript") || mimeType.Equals("application/json") || mimeType.Equals("image/svg+xml");
        }
        public static bool IsFont(string mimeType) {
            return mimeType.StartsWith("font/") || mimeType.StartsWith("application/font-") || mimeType.StartsWith("application/x-font");
        }
        public static bool IsAudio(string mimeType) {
            return mimeType.StartsWith("audio/");
        }
        public static bool IsImage(string mimeType) {
            return mimeType.StartsWith("image/");
        }
        public static bool IsVideo(string mimeType) {
            return mimeType.StartsWith("video/");
        }
        public static MimeType? Get(string mimeType) {
            foreach (var aMimeType in MimeTypes) {
                if (aMimeType.Name.Equals(mimeType)) {
                    return aMimeType;
                }
            }
            return null;
        }
        public static string GetMimeType(string filename) {
            string extension = System.IO.Path.GetExtension(filename).ToLower();
            foreach (var aMimeType in MimeTypes) {
                if (System.Array.IndexOf(aMimeType.Extensions, extension) != -1) {
                    return aMimeType.Name;
                }
            }
            return APPLICATION_OCTET_STREAM;
        }
        public static string GetMimeTypeByMagicNumber(byte[] buffer) {
            foreach (var aMimeType in MimeTypes) {
                if (aMimeType.MagicNumber.Length > 0) {
                    if (ByteUtils.IndexOf(buffer, aMimeType.MagicNumber) == 0) {
                        return aMimeType.Name;
                    }
                }
            }
            return APPLICATION_OCTET_STREAM;
        }
        public static string[] GetExtensions(string mimeType) {
            foreach (var aMimeType in MimeTypes) {
                if (aMimeType.Name.Equals(mimeType)) {
                    return aMimeType.Extensions;
                }
            }
            return [];
        }

    }


}