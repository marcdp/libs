using System;
using System.Net;


namespace DProjects.Utils {


    public static class AuthHttpHmacUtils {
         
        //Hmac
        public static string CreateHeader(string login, byte[] key, string method, string path, string query, string contentType, DateTime dateHeader, DateTime dateHeaderToUse) {
            var dateHeaderStr = (dateHeaderToUse != default) ? (dateHeaderToUse.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ")) : (dateHeader.ToUniversalTime().ToString("r"));
            if (contentType.IndexOf(";") != -1) {
                contentType = contentType.Substring(0, contentType.IndexOf(";"));
            }
            var unsignedData = method.ToLower() + CharUtils.CHAR_LF +
                path + CharUtils.CHAR_LF + 
                ((query.StartsWith("?")) ? (query.Substring(1)) : query) + CharUtils.CHAR_LF +
                contentType.ToLower() + CharUtils.CHAR_LF +
                dateHeaderStr + CharUtils.CHAR_LF;
            var unsignedDataBuffer = System.Text.Encoding.UTF8.GetBytes(unsignedData);
            var hmacSha = new System.Security.Cryptography.HMACSHA256 {
                Key = key
            };
            byte[] signedDataBufer = hmacSha.ComputeHash(unsignedDataBuffer);
            var signedData = Base64Utils.ToBase64(signedDataBufer);
            var authorization = login + ":" + signedData;
            var authorizationBase64 = Base64Utils.ToBase64(System.Text.Encoding.UTF8.GetBytes(authorization));
            return "hmac " + authorizationBase64;
        }
        
    }


}


