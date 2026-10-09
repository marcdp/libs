using System.Net;
using System.Text.Json;

namespace DProjects.XShell.Services {

    internal sealed class BootstrapFilesBuilder {

        internal Dictionary<string, string> Build(Extensions.Configuration config, string environment) {
            var result = new Dictionary<string, string>();

            // serialize app params
            var appParams = string.Join("&", config.App.Params.Select(kv => Uri.EscapeDataString(kv.Key) + "=" + Uri.EscapeDataString(kv.Value)));

            // index
            result["/index.html"] = $"""
                    <!DOCTYPE html>
                    <html lang="en">
                    <head>

                        <!-- general -->
                        <meta charset="utf-8">
                        <meta name="viewport" content="width=device-width, initial-scale=1.0">
                        <meta http-equiv="Content-Security-Policy" content="{EncodeAttribute(config.Server.HeaderCSP)}">
                        <meta name="description" content="{EncodeAttribute(config.App.Description)}">

                        <!-- config xshell -->
                        <meta name="xshell:app.basePath"       content="{EncodeAttribute(config.App.BasePath)}">
                        <meta name="xshell:app.configPath"     content="{EncodeAttribute(config.App.ConfigPath)}">
                        <meta name="xshell:app.params"         content="{EncodeAttribute(appParams)}">
                        <meta name="xshell:xshell.environment" content="{EncodeAttribute(environment)}">
                        <meta name="xshell:xshell.temp.url"    content="{EncodeAttribute(config.Temp.BasePath)}">

                        <!-- bootstrap xshell -->
                        <script src="{EncodeAttribute(config.XShell.BasePath + "/bootstrap.js")}" ></script>

                    </head>
                    <body>
                    </body>
                    </html>
                    """;

            // sw.js
            result["/sw.js"] = $"""
                    // import real service worker script from xshell cdn
                    importScripts({JsonSerializer.Serialize(config.XShell.BasePath + "/sw.js")}); 
                    """;

            // return
            return result;
        }

        // methods (private)
        private static string EncodeAttribute(string value) {
            return WebUtility.HtmlEncode(value);
        }
    }
}
