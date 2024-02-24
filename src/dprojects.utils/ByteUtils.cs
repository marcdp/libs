using System.Linq;

namespace DProjects.Utils {


    public static class ByteUtils {


        //euqlas + compare
        public static bool Compare(byte[] a, byte[] b) {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) {
                if (a[i] != b[i]) return false;
            }
            return true;
        }
        public static int IndexOf(byte[] array, byte[] pattern) {
            if (pattern.Length > array.Length) return -1;
            for (int i = 0; i < array.Length - pattern.Length; i++) {
                bool found = true;
                for (int j = 0; j < pattern.Length; j++) {
                    if (array[i + j] != pattern[j]) {
                        found = false;
                        break;
                    }
                }
                if (found) {
                    return i;
                }
            }
            return -1;
        }
        public static byte[] Concat(byte[] a, byte[] b) {
            return a.Concat(b).ToArray();
        }

    }


}


