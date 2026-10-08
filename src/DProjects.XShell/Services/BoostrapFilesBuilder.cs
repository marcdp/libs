using System.Net;

namespace DProjects.XShell.Services {

    public sealed class BoostrapFilesBuilder {

        public Dictionary<string, string> Build(Extensions.Configuration config, string environment) {
            var result = new Dictionary<string, string>();

            // serialize app params
            var appParams = string.Join("&", config.App.Params.Select(kv => Uri.EscapeDataString(kv.Key) + "=" + Uri.EscapeDataString(kv.Value)));
            var appParamsHtml = WebUtility.HtmlEncode(appParams);

            // index
            result["/index.html"] = $"""
                    <!DOCTYPE html>
                    <html lang="en">
                    <head>

                        <!-- general -->
                        <meta charset="utf-8">
                        <meta name="viewport" content="width=device-width, initial-scale=1.0">
                        <meta http-equiv="Content-Security-Policy" content="{config.Server.HeaderCSP}">
                        <meta name="description" content="{config.App.Description.Replace("\"", "&quot;")}">

                        <!-- config xshell -->
                        <meta name="xshell:app.basePath"       content="{config.App.BasePath}">
                        <meta name="xshell:app.configPath"     content="{config.App.ConfigPath}">
                        <meta name="xshell:app.params"         content="{appParamsHtml}">
                        <meta name="xshell:xshell.environment" content="{(environment)}">
                        <meta name="xshell:xshell.temp.url"    content="{(config.Temp.BasePath)}">

                        <!-- bootstrap xshell -->
                        <script src="{config.XShell.BasePath}/bootstrap.js" ></script>

                    </head>
                    <body>
                    </body>
                    </html>
                    """;

            // sw.js
            result["/sw.js"] = $"""
                    // import real service worker script from xshell cdn
                    importScripts("{config.XShell.BasePath}/sw.js"); 
                    """;

            // return
            return result;
        }
    }
}
