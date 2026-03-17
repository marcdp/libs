using System;


namespace DProjects.Utils {


    public static class Base64Utils {


        //to methods
        public static string ToBase64(byte[] buffer, int offset, int length, Base64FormattingOptions options = default) {
            return Convert.ToBase64String(buffer, offset, length, options);
        }
        public static string ToBase64(string text, Base64FormattingOptions options = default) {
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(text);
            return ToBase64(buffer, 0, buffer.Length, options);
        }
        public static string ToBase64(byte[] buffer, Base64FormattingOptions options = default) {
            return ToBase64(buffer, 0, buffer.Length, options);
        }
        public static string ToBase64UrlSafe(string text, Base64FormattingOptions options = default) {
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(text);
            return ToBase64UrlSafe(buffer, options);
        }
        public static string ToBase64UrlSafe(byte[] buffer, Base64FormattingOptions options = default) {
            var aux = ToBase64(buffer, 0, buffer.Length, options);
            if (aux.IndexOf("=") != -1) aux = aux.Substring(0, aux.IndexOf("="));
            if (aux.IndexOf("+") != -1) aux = aux.Replace("+", "-");
            if (aux.IndexOf("/") != -1) aux = aux.Replace("/", "_");
            return aux;
        }


        //from methods
        public static byte[] FromBase64(string value) {
            return Convert.FromBase64String(value);
        }
        public static byte[] FromBase64UrlSafe(string value) {
            while (value.Length % 4 != 0) value += "=";
            if (value.IndexOf("-") != -1) value = value.Replace("-", "+");
            if (value.IndexOf("_") != -1) value = value.Replace("_", "/");
            return Convert.FromBase64String(value);
        }

        //is
        public static bool IsBase64(string value) {
            try {
                Convert.FromBase64String(value);
                return true;
            } catch (Exception) {
                return false;
            }
        }

    }

}


