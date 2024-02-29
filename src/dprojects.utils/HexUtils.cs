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
    }

}


