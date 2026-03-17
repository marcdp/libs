
using System;
using System.Text;

namespace DProjects.Utils {


    public static class EncodingUtils {


        //static initializer
        static EncodingUtils() {
            RegisterDefaultProvider();
        }

        //constants
        public const string ENCODING_UTF8 = "utf-8";
        public const string ENCODING_WINDOWS_1252 = "windows-1252";
        public const string ENCODING_ISO_8859_1 = "ISO-8859-1";


        //methods
        public static void RegisterDefaultProvider() {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
        public static Encoding GetDefault() {
            return Encoding.UTF8;
        }
        public static EncodingInfo[] GetEncodings() {
            return Encoding.GetEncodings();
        }
        public static bool GetStringContainsUnicodeCharsUpperThan(string s, int value) {
            for (int i = 0; i <= s.Length - 1; i++) {
                char c = s[i];
                int ci = Convert.ToInt32(c);
                if (ci >= value) {
                    return true;
                }
            }
            return false;
        }
        public static Encoding DetectEncoding(byte[] buffer) {
            return DetectEncoding(buffer, out _);
        }
        public static Encoding DetectEncoding(byte[] buffer, out int bomLength, Encoding? enc = null) {
            if (enc == null) {
                enc = EncodingUtils.GetDefault();
                if (EnvironmentUtils.IsWindows()) enc = Encoding.GetEncoding(EncodingUtils.ENCODING_WINDOWS_1252);
            }
            if (buffer.Length >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF) { // EF BB BF = UTF8
                bomLength = 3;
                enc = Encoding.UTF8;
            } else if (buffer.Length >= 4 && buffer[0] == 0 && buffer[1] == 0 && buffer[2] == 0xFE && buffer[3] == 0xFF) { // 00 00 FE FF =  	UTF-32, big-endian
                bomLength = 4;
                return Encoding.GetEncoding("utf-32BE");
            } else if (buffer.Length >= 4 && buffer[0] == 0xFF && buffer[1] == 0xFE && buffer[2] == 0x0 && buffer[3] == 0x0) { //FF FE 00 00 = UTF-32, little-endian
                bomLength = 4;
                enc = Encoding.UTF32;
            } else if (buffer.Length >= 3 && buffer[0] == 0x2B && buffer[1] == 0x2F && buffer[2] == 0x76) { //2B 2F 76 = UTF7
                bomLength = 3;
                enc = Encoding.UTF7;
            } else if (buffer.Length >= 2 && buffer[0] == 0xFE && buffer[1] == 0xFF) { //FE FF = UTF-16, big-endian
                bomLength = 2;
                enc = Encoding.BigEndianUnicode;
            } else if (buffer.Length >= 2 && buffer[0] == 0xFF && buffer[1] == 0xFE) { //FF FE = UTF-16, little-endian
                bomLength = 2;
                return Encoding.GetEncoding("utf-16LE");
            } else {
                bomLength = 0;
                int i = 0;
                // Some text files are encoded in UTF8, but have no BOM/signature. Hence
                // the below manually checks for a UTF8 pattern. This code is based off
                // the top answer at: http://stackoverflow.com/questions/6555015/check-for-invalid-utf8
                // For our purposes, an unnecessarily strict (and terser/slower)
                // implementation is shown at: http://stackoverflow.com/questions/1031645/how-to-detect-utf-8-in-plain-c
                // For the below, false positives should be exceedingly rare (and would
                // be either slightly malformed UTF-8 (which would suit our purposes
                // anyway) or 8-bit extended ASCII/UTF-16/32 at a vanishingly long shot).
                bool utf8 = false;
                byte[] b = buffer;
                while (i < b.Length - 4) {
                    if (b[i] <= 0x7F) {
                        i++;
                        continue;
                    }
                    // If all characters are below 0x80, then it is valid UTF8, but UTF8 is not 'required' (and therefore the text is more desirable to be treated as the default codepage of the computer). Hence, there's no "utf8 = true;" code unlike the next three checks.
                    if (b[i] >= 0xC2 && b[i] <= 0xDF && b[i + 1] >= 0x80 && b[i + 1] < 0xC0) {
                        i += 2;
                        utf8 = true;
                        continue;
                    }
                    if (b[i] >= 0xE0 && b[i] <= 0xF0 && b[i + 1] >= 0x80 && b[i + 1] < 0xC0 && b[i + 2] >= 0x80 && b[i + 2] < 0xC0) {
                        i += 3;
                        utf8 = true;
                        continue;
                    }
                    if (b[i] >= 0xF0 && b[i] <= 0xF4 && b[i + 1] >= 0x80 && b[i + 1] < 0xC0 && b[i + 2] >= 0x80 && b[i + 2] < 0xC0 && b[i + 3] >= 0x80 && b[i + 3] < 0xC0) {
                        i += 4;
                        utf8 = true;
                        continue;
                    }
                    utf8 = false;
                    break;
                }
                if (utf8 == true) {
                    return Encoding.UTF8;
                }
                // The next check is a heuristic attempt to detect UTF-16 without a BOM.
                // We simply look for zeroes in odd or even byte places, and if a certain
                // threshold is reached, the code is 'probably' UF-16.
                double threshold = 0.1;
                // proportion of chars step 2 which must be zeroed to be diagnosed as utf-16. 0.1 = 10%
                int count = 0;
                for (int n = 0; n <= b.Length - 1; n += 2) {
                    if (b[n] == 0) {
                        count++;
                    }
                }
                if (b.Length > 0 && (count) / b.Length > threshold) {
                    return Encoding.BigEndianUnicode;
                }
                count = 0;
                for (int n = 1; n <= b.Length - 1; n += 2) {
                    if (b[n] == 0) {
                        count++;
                    }
                }
                if (b.Length > 0 && (count) / b.Length > threshold) {
                    return System.Text.Encoding.Unicode; // (little-endian)
                }
                // Finally, a long shot - let's see if we can find "charset=xyz" or
                // "encoding=xyz" to identify the encoding:
                for (int n = 0; n <= b.Length - 10; n++) {
                    if ((Convert.ToChar(b[n + 0]) == 'c' || Convert.ToChar(b[n + 0]) == 'C') && (Convert.ToChar(b[n + 1]) == 'h' ||
                            Convert.ToChar(b[n + 1]) == 'H') && (Convert.ToChar(b[n + 2]) == 'a' || Convert.ToChar(b[n + 2]) == 'A') &&
                            (Convert.ToChar(b[n + 3]) == 'r' || Convert.ToChar(b[n + 3]) == 'R') && (Convert.ToChar(b[n + 4]) == 's' ||
                            Convert.ToChar(b[n + 4]) == 'S') && (Convert.ToChar(b[n + 5]) == 'e' || Convert.ToChar(b[n + 5]) == 'E') &&
                            (Convert.ToChar(b[n + 6]) == 't' || Convert.ToChar(b[n + 6]) == 'T' && (Convert.ToChar(b[n + 7]) == '=')) ||
                            ((Convert.ToChar(b[n + 0]) == 'e' || Convert.ToChar(b[n + 0]) == 'E') && (Convert.ToChar(b[n + 1]) == 'n' || Convert.ToChar(b[n + 1]) == 'N') &&
                            (Convert.ToChar(b[n + 2]) == 'c' || Convert.ToChar(b[n + 2]) == 'C') && (Convert.ToChar(b[n + 3]) == 'o' || Convert.ToChar(b[n + 3]) == 'O') &&
                            (Convert.ToChar(b[n + 4]) == 'd' || Convert.ToChar(b[n + 4]) == 'D') && (Convert.ToChar(b[n + 5]) == 'i' || Convert.ToChar(b[n + 5]) == 'I') &&
                            (Convert.ToChar(b[n + 6]) == 'n' || Convert.ToChar(b[n + 6]) == 'N') && (Convert.ToChar(b[n + 7]) == 'g' || Convert.ToChar(b[n + 7]) == 'G') &&
                            (Convert.ToChar(b[n + 8]) == '='))) {
                        if (Convert.ToChar(b[n + 0]) == 'c' || Convert.ToChar(b[n + 0]) == 'C') {
                            n += 8;
                        } else {
                            n += 9;
                        }
                        if (Convert.ToChar(b[n]) == '\"' || Convert.ToChar(b[n]) == '\'') {
                            n++;
                        }
                        int oldn = n;
                        while (n < b.Length && (Convert.ToChar(b[n]) == '_' || Convert.ToChar(b[n]) == '-' || (Convert.ToChar(b[n]) >= '0' && Convert.ToChar(b[n]) <= '9') || (Convert.ToChar(b[n]) >= 'a' && Convert.ToChar(b[n]) <= 'z') || (Convert.ToChar(b[n]) >= 'A' && Convert.ToChar(b[n]) <= 'Z'))) {
                            n++;
                        }
                        byte[] nb = new byte[n - oldn - 1 + 1];
                        Array.Copy(b, oldn, nb, 0, n - oldn);
                        try {
                            string internalEnc = System.Text.Encoding.ASCII.GetString(nb);
                            return System.Text.Encoding.GetEncoding(internalEnc);
                        } catch {
                            // If C# doesn't recognize the name of the encoding, break.
                        }
                    }
                }
            }
            return enc;
        }
        public static string GetBufferAsString(byte[] buffer) {
            var encoding = DetectEncoding(buffer, out int bomLength);
            return encoding.GetString(buffer, bomLength, buffer.Length - bomLength);
        }
    }

}


