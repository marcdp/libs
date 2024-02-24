using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;


namespace DProjects.Utils {


    public static class HashUtils {


        //md5
        public static string ToHashMD5Base64(string value) {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value.ToCharArray());
            return Convert.ToBase64String(ToHashMD5(bytes));
        }
        public static string ToHashMD5Base64UrlSafe(string value) {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value.ToCharArray());
            return Base64Utils.ToBase64UrlSafe(ToHashMD5(bytes));
        }
        public static string ToHashMD5Base64(byte[] bytes) {
            return Convert.ToBase64String(ToHashMD5(bytes));
        }
        public static string ToHashMD5Hex(byte[] bytes) {
            return ConvertUtils.ToHexString(ToHashMD5(bytes));
        }
        public static byte[] ToHashMD5(byte[] bytes) {
            using (var algorithm = MD5.Create()) {
                return algorithm.ComputeHash(bytes);
            }
        }
        public static byte[] ToHashMD5(Stream input) {
            using (var algorithm = MD5.Create()) {
                return algorithm.ComputeHash(input);
            }
        }

        //sha1
        public static string ToHashSHA1Base64(byte[] bytes) {
            var algorithm = SHA1.Create();
            byte[] result = algorithm.ComputeHash(bytes);
            algorithm.Dispose();
            return Convert.ToBase64String(result);
        }
        public static byte[] ToHashSHA1(Stream data) {
            using (var sha1 = SHA1.Create()) {
                return sha1.ComputeHash(data);
            }
        }
        public static string ToHashSHA1Base64(string value) {
            var algorithm = SHA1.Create();
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value.ToCharArray());
            byte[] result = algorithm.ComputeHash(bytes);
            algorithm.Dispose();
            return Convert.ToBase64String(result);
        }
        public static string ToHashSHA1Hex(byte[] buffer) {
            return ConvertUtils.ToHexString(ToHashSHA1(buffer));
        }
        public static string ToHashSHA1Hex(string value) {
            return ConvertUtils.ToHexString(ToHashSHA1(System.Text.Encoding.UTF8.GetBytes(value)));
        }
        public static byte[] ToHashSHA1(byte[] buffer) {
            var algorithm = SHA1.Create();
            byte[] result = algorithm.ComputeHash(buffer);
            algorithm.Dispose();
            return result;
        }


        //sha256
        public static string ToHashSHA256Base64(byte[] bytes) {
            var algorithm = SHA256.Create();
            byte[] result = algorithm.ComputeHash(bytes);
            algorithm.Dispose();
            return Convert.ToBase64String(result);
        }
        public static byte[] ToHashSHA256(Stream data) {
            using (var sha256 = SHA256.Create()) {
                return sha256.ComputeHash(data);
            }
        }
        public static string ToHashSHA256Base64(string value) {
            var algorithm = SHA256.Create();
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value.ToCharArray());
            byte[] result = algorithm.ComputeHash(bytes);
            algorithm.Dispose();
            return Convert.ToBase64String(result);
        }
        public static string ToHashSHA256hex(byte[] value) {
            var algorithm = SHA256.Create();
            byte[] result = algorithm.ComputeHash(value);
            algorithm.Dispose();
            return ConvertUtils.ToHexString(result);
        }
        public static string ToHashSHA256hex(string value) {
            var algorithm = SHA256.Create();
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value.ToCharArray());
            byte[] result = algorithm.ComputeHash(bytes);
            algorithm.Dispose();
            return ConvertUtils.ToHexString(result);
        }
        public static byte[] ToHashSHA256(string value) {
            var algorithm = SHA256.Create();
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value.ToCharArray());
            byte[] hash = algorithm.ComputeHash(bytes);
            algorithm.Dispose();
            return hash;
        }
        public static byte[] ToHashSHA256(byte[] bytes) {
            var algorithm = SHA256.Create();
            byte[] hash = algorithm.ComputeHash(bytes);
            algorithm.Dispose();
            return hash;
        }


        //sha512
        public static string ToHashSHA512Base64(byte[] bytes) {
            var algorithm = SHA512.Create();
            byte[] result = algorithm.ComputeHash(bytes);
            algorithm.Dispose();
            return Convert.ToBase64String(result);
        }
        public static byte[] ToHashSHA512(Stream data) {
            using (var sha512 = SHA512.Create()) {
                return sha512.ComputeHash(data);
            }
        }
        public static string ToHashSHA512Base64(string value) {
            var algorithm = SHA512.Create();
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value.ToCharArray());
            byte[] result = algorithm.ComputeHash(bytes);
            algorithm.Dispose();
            return Convert.ToBase64String(result);
        }
        public static byte[] ToHashSHA512(string value) {
            var algorithm = SHA512.Create();
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value.ToCharArray());
            byte[] hash = algorithm.ComputeHash(bytes);
            algorithm.Dispose();
            return hash;
        }
        public static byte[] ToHashSHA512(byte[] bytes) {
            var algorithm = SHA512.Create();
            byte[] hash = algorithm.ComputeHash(bytes);
            algorithm.Dispose();
            return hash;
        }


        //hmac256
        public static string ToHashHmacSha256Base64(byte[] bytes, byte[] data) {
            return Convert.ToBase64String(ToHashHmacSha256(bytes, data));
        }
        public static byte[] ToHashHmacSha256(byte[] key, Stream input) {
            using (var hmacSHA256 = new HMACSHA256()) {
                hmacSHA256.Key = key;
                return hmacSHA256.ComputeHash(input);
            }
        }
        public static byte[] ToHashHmacSha256(byte[] key, byte[] data) {
            HMACSHA256 hmacSHA256 = new HMACSHA256();
            hmacSHA256.Key = key;
            byte[] result = hmacSHA256.ComputeHash(data);
            hmacSHA256.Dispose();
            return result;
        }
        public static byte[] ToHashHmacSha256(byte[] key, string data) {
            HMACSHA256 algorithm = new HMACSHA256();
            algorithm.Key = key;
            byte[] result = algorithm.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
            algorithm.Dispose();
            return result;
        }
        public static byte[] ToHashHmacSha256(string key, string data) {
            HMACSHA256 algorithm = new HMACSHA256();
            algorithm.Key = System.Text.Encoding.UTF8.GetBytes(key);
            byte[] result = algorithm.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
            algorithm.Dispose();
            return result;
        }


    }

}


