using Microsoft.AspNetCore.Http;

namespace DProjects.XShell.Middlewares {
    public sealed class CSPMiddleware {


        // fields
        private readonly RequestDelegate mNext;


        // ctor
        public CSPMiddleware(RequestDelegate next) {
            mNext = next;
        }


        // methods
        public async Task InvokeAsync(HttpContext context) {
            context.Response.Headers.ContentSecurityPolicy =
                "default-src 'self'; " +
                "script-src 'self'; " +
                "style-src 'self'; " +
                "img-src 'self' data:; " +
                "font-src 'self'; " +
                "connect-src 'self'; " +
                "object-src 'none'; " +
                "base-uri 'self'; " +
                "frame-ancestors 'self'";

            await mNext(context);
        }


    }
}