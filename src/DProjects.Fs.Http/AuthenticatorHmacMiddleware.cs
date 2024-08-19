//using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.Http;
//using System;
//using System.Threading.Tasks;

//namespace DProjects.Fs.Http {


//    //factory
//    public static class AuthenticatorHmacExtensions {
//        public static IApplicationBuilder UseAuthenticatorHmac(this IApplicationBuilder builder, AuthenticatorHmacOptions options) {
//            return builder.UseMiddleware<AuthenticatorHmacMiddleware>(options);
//        }
//    }
//    //options
//    public class AuthenticatorHmacOptions {
//        public IAuthenticator Authenticator { get; set; }
//        public AuthenticatorHmacOptions(IAuthenticator authenticator) {  
//            Authenticator = authenticator;
//        }
//    }

//    //implementation
//    public class AuthenticatorHmacMiddleware {
//        //variables
//        private readonly RequestDelegate mNext;
//        private readonly AuthenticatorHmacOptions mOptions;
//        //constructor  
//        public AuthenticatorHmacMiddleware(RequestDelegate next, AuthenticatorHmacOptions options) {
//            mNext = next;
//            mOptions = options;
//        }  
//        //handle
//        public async Task Invoke(HttpContext context, User user, ILogger<AuthenticatorHmacMiddleware> log) {
//            string? authorization = null;
//            if (context.Request.Headers.ContainsKey(HttpUtils.HEADER_AUTHORIZATION)) {
//                var aux = context.Request.Headers[HttpUtils.HEADER_AUTHORIZATION];
//                if (aux.Count == 1 && (aux[0] ?? "").StartsWith("Hmac ", StringComparison.InvariantCultureIgnoreCase)) {
//                    authorization = aux[0];
//                }
//            }
//            var authToken = context.Request.Query["authtoken"].ToString();
//            if (authorization != null || authToken != null) {
//                var httpRequestFeature = context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpRequestFeature>();
//                if (httpRequestFeature != null) {
//                    var path = httpRequestFeature.RawTarget;
//                    if (path.IndexOf("?") != -1) path = path.Substring(0, path.IndexOf("?"));
//                    var date = context.Request.Headers["Date"].ToString();
//                    var dateToUse = context.Request.Headers["X-Date"].ToString();
//                    var queryUndecoded = context.Request.QueryString.Value ?? "";
//                    queryUndecoded = queryUndecoded.Replace("%26", "____AMPERSAND____"); //to avoid UrlDecode decode this ampersand (is in a variable value, its not a separator)
//                    var queryDecoded = System.Web.HttpUtility.UrlDecode(queryUndecoded);
//                    if (queryDecoded.IndexOf("+") != -1) queryDecoded = queryDecoded.Replace("+", "%2B");
//                    if (queryDecoded.IndexOf("____AMPERSAND____") != -1) queryDecoded = queryDecoded.Replace("____AMPERSAND____", "%26");
//                    var contentType = context.Request.Headers[HttpUtils.HEADER_CONTENT_TYPE].ToString();
//                    mOptions.Authenticator.AuthenticateHmac(
//                        user,
//                        context.Request.Method,
//                        path,
//                        date,
//                        dateToUse,
//                        authorization ?? null,
//                        authToken,
//                        contentType,
//                        queryDecoded,
//                        log);

//                }
//            }
//            await mNext.Invoke(context);
//        }
//    }






//}