using DProjects.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;

namespace DProjects.Auth {

    public class AuthRequest {


        //ctor
        public AuthRequest() {
            Headers = new Dictionary<string, string>();
        } 


        //props
        public IDictionary<string, string> Headers { get; }


        ////methods
        //public static AuthRequest FromHttpBasic(string login, string password) {
        //    return new AuthRequest(new Dictionary<string, string> {
        //        [Utils.HttpUtils.HEADER_AUTHORIZATION] =  "Basic " + Base64Utils.ToBase64(login + ":" + password)
        //    });
        //}
    }

}