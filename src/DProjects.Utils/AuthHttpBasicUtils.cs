using System;
using System.Net;
using System.Security.Claims;


namespace DProjects.Utils {


    public static class AuthHttpBasicUtils {

        //base64
        public static string CreateHeader(NetworkCredential credentials) {
            return "Basic " + Base64Utils.ToBase64(credentials.UserName + ":" + credentials.Password);
        }

    }


}


