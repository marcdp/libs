using System;
using System.Net;


namespace DProjects.Utils {


    public static class AuthUtils {

        //base64
        public static string CreateBasic(NetworkCredential credentials) {
            return "Basic " + Base64Utils.ToBase64(credentials.UserName + ":" + credentials.Password);
        }

        //Hmac
        public static string CreateHmac(NetworkCredential credentials, string method, string path, string query, string contentType, DateTime dateHeader, DateTime dateHeaderToUse) {
            string dateHeaderStr = (dateHeaderToUse != default) ? (dateHeaderToUse.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ")) : (dateHeader.ToUniversalTime().ToString("r"));
            string login = credentials.UserName;
            string password = credentials.Password;
            if (contentType.IndexOf(";") != -1) {
                contentType = contentType.Substring(0, contentType.IndexOf(";"));
            }
            string unsignedData = method.ToLower() + CharUtils.CHAR_LF +
                path + CharUtils.CHAR_LF + 
                ((query.StartsWith("?")) ? (query.Substring(1)) : query) + CharUtils.CHAR_LF +
                contentType.ToLower() + CharUtils.CHAR_LF +
                dateHeaderStr + CharUtils.CHAR_LF;
            byte[] unsignedDataBuffer = System.Text.Encoding.UTF8.GetBytes(unsignedData);
            var hmacSha = new System.Security.Cryptography.HMACSHA256();
            hmacSha.Key = System.Text.Encoding.UTF8.GetBytes(password);
            byte[] signedDataBufer = hmacSha.ComputeHash(unsignedDataBuffer);
            string signedData = Base64Utils.ToBase64(signedDataBufer);
            string authorization = login + ":" + signedData;
            string authorizationBase64 = Base64Utils.ToBase64(System.Text.Encoding.UTF8.GetBytes(authorization));
            return "hmac " + authorizationBase64;
        }
    }


}


