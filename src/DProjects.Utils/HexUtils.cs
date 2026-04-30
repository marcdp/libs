using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;


namespace DProjects.Utils {


    public static class HexUtils {
        public static string Hex(int number) {
            return number.ToString("X");
        }
        public static string Hex(long number) {
            return number.ToString("X");
        }
        public static string Hex(byte number) {
            return number.ToString("X");
        }
        public static string Hex(byte[] buffer) {
            var result = new StringBuilder();
            foreach (var b in buffer) {
                result.Append(b.ToString("X"));
            }
            return result.ToString();
        }
        public static byte[] HexToBytes(string hex) {
            if (hex.Length % 2 != 0) {
                throw new Exception("Invalid _xvault meta: salt is not valid hex.");
            }
            var bytes = new byte[hex.Length / 2];
            for (var i = 0; i < bytes.Length; i++) {
                var byteValue = hex.Substring(i * 2, 2);
                bytes[i] = Convert.ToByte(byteValue, 16);
            }
            return bytes;
        }
    }

}


